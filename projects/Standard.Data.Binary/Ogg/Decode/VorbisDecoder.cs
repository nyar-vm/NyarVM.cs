using System.Buffers.Binary;
using Std.Data.Binary.Ogg.Data;

namespace Std.Data.Binary.Ogg.Decode;

/// <summary>
///     Vorbis I 音频解码器——完整实现，的C#，无第三方依赖的
/// </summary>
/// <remarks>
///     实现的Vorbis I 规范（RFC 5215）的核心解码流程的
///     码书 Huffman 解码 的地板解码（Type 1）→ 残差解码（Type 0/1/2）→ 信道解的的IMDCT 的加窗 的重叠相加的
///     支持单声道和立体声，采样的8kHz~48kHz，块大小 64~8192的
/// </remarks>
public sealed class VorbisDecoder
{
    #region 音频包解的

    private float[][]? decode_audio_packet(byte[] packet)
    {
        if (packet.Length < 1) return null;

        var reader = new BitReader(packet, 0);

        if (reader.read_bits(1) != 0) return null;

        var modeNumber = (int)reader.read_bits((uint)i_log(_modes.Length));
        if (modeNumber >= _modes.Length) return null;

        var mode = _modes[modeNumber];
        var isLongBlock = mode.block_flag != 0;
        var blockSize = isLongBlock ? block_size1 : block_size0;
        var halfBlock = blockSize / 2;

        var previousWindowFlag = false;
        var nextWindowFlag = false;
        if (isLongBlock)
        {
            previousWindowFlag = reader.read_bits(1) != 0;
            nextWindowFlag = reader.read_bits(1) != 0;
        }

        var mapping = _mappings[mode.mapping];
        var submaps = mapping.submaps;

        var floorPackets = new bool[channels];
        var floorDecoded = new float[channels][];

        for (var ch = 0; ch < channels; ch++)
        {
            var submap = mapping.channel_submap[ch];
            var floorIndex = mapping.submap_floor[System.Math.Min(submap, mapping.submap_floor.Length - 1)];
            floorPackets[ch] = reader.read_bits(1) != 0;

            if (floorPackets[ch])
                floorDecoded[ch] = decode_floor1(ref reader, _floors[floorIndex], halfBlock);
            else
                floorDecoded[ch] = new float[halfBlock];
        }

        var residueBuffers = new float[channels][];
        for (var ch = 0; ch < channels; ch++)
        {
            residueBuffers[ch] = new float[halfBlock];
            if (floorPackets[ch]) Array.Copy(floorDecoded[ch], residueBuffers[ch], halfBlock);
        }

        for (var sub = 0; sub < submaps; sub++)
        {
            var doNotDecode = new bool[channels];
            var channelsInSubmap = 0;

            for (var ch = 0; ch < channels; ch++)
                if (mapping.channel_submap[ch] == sub)
                {
                    doNotDecode[ch] = !floorPackets[ch];
                    channelsInSubmap++;
                }
                else
                {
                    doNotDecode[ch] = true;
                }

            if (channelsInSubmap == 0) continue;

            var residueIndex = mapping.submap_residue[System.Math.Min(sub, mapping.submap_residue.Length - 1)];
            decode_residue(ref reader, _residues[residueIndex], residueBuffers, doNotDecode, channels, halfBlock);
        }

        if (mapping.coupling_steps > 0)
            for (var i = 0; i < mapping.coupling_steps; i++)
            {
                var magCh = mapping.magnitude[i];
                var angCh = mapping.angle[i];
                if (magCh < channels && angCh < channels) inverse_couple(residueBuffers[magCh], residueBuffers[angCh]);
            }

        var output = new float[channels][];
        for (var ch = 0; ch < channels; ch++)
        {
            var mdctInput = new float[halfBlock];
            for (var i = 0; i < halfBlock; i++) mdctInput[i] = residueBuffers[ch][i];

            var timeDomain = inverse_mdct(mdctInput, halfBlock);

            var window = get_window(blockSize, isLongBlock, previousWindowFlag, nextWindowFlag);
            for (var i = 0; i < blockSize; i++) timeDomain[i] *= window[i];

            var leftOverlap = System.Math.Min(halfBlock, _previous_window[ch].Length);
            for (var i = 0; i < leftOverlap; i++) timeDomain[i] += _previous_window[ch][i];

            output[ch] = new float[halfBlock];
            Array.Copy(timeDomain, output[ch], halfBlock);

            _previous_window[ch] = new float[halfBlock];
            Array.Copy(timeDomain, halfBlock, _previous_window[ch], 0, halfBlock);
        }

        return output;
    }

    #endregion

    #region 残差解码

    private void decode_residue(ref BitReader reader, VorbisResidue residue, float[][] buffers,
        bool[] doNotDecode, int channels, int halfBlock)
    {
        var type = residue.type;
        var begin = residue.begin;
        var end = System.Math.Min(residue.end, halfBlock);
        var partitionSize = residue.partition_size;
        var classifications = residue.classifications;
        var classbookIndex = residue.classbook;

        var nToRead = end - begin;
        var partitionsToRead = nToRead / partitionSize;

        if (partitionsToRead <= 0) return;

        if (classbookIndex < 0 || classbookIndex >= _codebooks.Length) return;

        var classbook = _codebooks[classbookIndex];
        var classwordsPerCodeword = classbook.dimensions;

        var usedChannels = type == 2 ? 1 : channels;
        var classificationArray = new int[usedChannels, partitionsToRead];

        for (var pass = 0; pass < 8; pass++)
        {
            var partitionCount = 0;

            while (partitionCount < partitionsToRead)
            {
                if (pass == 0)
                    for (var ch = 0; ch < usedChannels; ch++)
                    {
                        var p = partitionCount;
                        var valsToRead = System.Math.Min(classwordsPerCodeword, partitionsToRead - p);

                        var temp = classbook.decode_symbol(ref reader);
                        if (temp < 0) temp = 0;

                        for (var i = valsToRead - 1; i >= 0; i--)
                        {
                            classificationArray[ch, p + i] = temp % classifications;
                            temp /= classifications;
                        }
                    }

                for (var ch = 0; ch < usedChannels; ch++)
                {
                    if (type != 2 && doNotDecode[ch]) continue;

                    var vqClass = classificationArray[ch, partitionCount];

                    if ((residue.cascade[vqClass] & (1 << pass)) == 0) continue;

                    var vqbook = residue.books[vqClass, pass];
                    if (vqbook <= 0 || vqbook >= _codebooks.Length) continue;

                    var book = _codebooks[vqbook];
                    var offset = begin + partitionCount * partitionSize;

                    if (type == 2)
                        for (var j = 0; j < partitionSize;)
                        {
                            var symbol = book.decode_symbol(ref reader);
                            if (symbol < 0) break;

                            var vecLen = System.Math.Min(book.dimensions, partitionSize - j);
                            for (var d = 0; d < vecLen; d++)
                            {
                                var sampleOffset = offset + j + d;
                                var chIdx = sampleOffset % channels;
                                var sampleIdx = sampleOffset / channels;
                                if (chIdx < channels && sampleIdx < buffers[chIdx].Length)
                                    buffers[chIdx][sampleIdx] += book.get_value(symbol, d);
                            }

                            j += book.dimensions;
                        }
                    else
                        for (var j = 0; j < partitionSize;)
                        {
                            var symbol = book.decode_symbol(ref reader);
                            if (symbol < 0) break;

                            var vecLen = System.Math.Min(book.dimensions, partitionSize - j);
                            for (var d = 0; d < vecLen; d++)
                                if (offset + j + d < buffers[ch].Length)
                                    buffers[ch][offset + j + d] += book.get_value(symbol, d);

                            j += book.dimensions;
                        }
                }

                partitionCount++;
            }
        }
    }

    #endregion

    #region 信道解的

    private static void inverse_couple(float[] magnitude, float[] angle)
    {
        var len = System.Math.Min(magnitude.Length, angle.Length);
        for (var i = 0; i < len; i++)
        {
            var m = magnitude[i];
            var a = angle[i];

            if (m > 0)
            {
                if (a > 0)
                {
                    magnitude[i] = m;
                    angle[i] = m - a;
                }
                else
                {
                    magnitude[i] = m + a;
                    angle[i] = a;
                }
            }
            else
            {
                if (a > 0)
                {
                    magnitude[i] = m;
                    angle[i] = m + a;
                }
                else
                {
                    magnitude[i] = m - a;
                    angle[i] = a;
                }
            }
        }
    }

    #endregion

    #region IMDCT

    private static float[] inverse_mdct(float[] input, int n)
    {
        var output = new float[n * 2];

        for (var i = 0; i < n * 2; i++)
        {
            var sum = 0.0;
            for (var k = 0; k < n; k++)
                sum += input[k] * System.Math.Cos(System.Math.PI * (2.0 * i + n + 1) * (2.0 * k + 1) / (4.0 * n));

            output[i] = (float)sum;
        }

        return output;
    }

    #endregion

    #region PCM 交错

    private static byte[] interleave_float_to_pcm16(float[][] channelSamples, int channels, int totalSamples)
    {
        var pcmData = new byte[totalSamples * channels * 2];

        for (var i = 0; i < totalSamples; i++)
        for (var ch = 0; ch < channels; ch++)
        {
            var sample = i < channelSamples[ch].Length ? channelSamples[ch][i] : 0f;
            var clamped = System.Math.Clamp(sample, -1f, 1f);
            var pcm16 = (short)(clamped * 32767);
            BinaryPrimitives.WriteInt16LittleEndian(pcmData.AsSpan((i * channels + ch) * 2), pcm16);
        }

        return pcmData;
    }

    #endregion

    #region 字段

    private VorbisMode[] _modes = [];
    private VorbisCodebook[] _codebooks = [];
    private VorbisFloor[] _floors = [];
    private VorbisResidue[] _residues = [];
    private VorbisMapping[] _mappings = [];
    private float[][] _previous_window = [];

    #endregion

    #region 属的

    /// <summary>声道的/summary>
    public int channels { get; private set; }

    /// <summary>采样的/summary>
    public int sample_rate { get; private set; }

    /// <summary>短块大小。</summary>
    public int block_size0 { get; private set; }

    /// <summary>长块大小。</summary>
    public int block_size1 { get; private set; }

    #endregion

    #region 公开方法

    /// <summary>
    ///     的OGG 音频数据解码 Vorbis 音频的PCM16 字节数据
    /// </summary>
    public (byte[] PcmData, int Channels, int SampleRate) decode_to_pcm16(OggAudioData oggData)
    {
        if (oggData.codec_type != OggCodecType.vorbis)
            throw new ArgumentException($"OGG 容器中的编解码类型不的Vorbis，实际：{oggData.codec_type}");

        channels = oggData.channels;
        sample_rate = oggData.sample_rate;

        if (oggData.packets.Count < 3) throw new InvalidDataException("Vorbis 流至少需的3 个头部数据包");

        parse_identification_header(oggData.packets[0]);
        parse_comment_header(oggData.packets[1]);
        parse_setup_header(oggData.packets[2]);

        _previous_window = new float[channels][];
        for (var ch = 0; ch < channels; ch++) _previous_window[ch] = new float[block_size1 / 2];

        var allChannelSamples = new List<float>[channels];
        for (var ch = 0; ch < channels; ch++) allChannelSamples[ch] = [];

        for (var i = 3; i < oggData.packets.Count; i++)
        {
            var decoded = decode_audio_packet(oggData.packets[i]);
            if (decoded == null || decoded.Length != channels) continue;

            var sampleCount = decoded[0].Length;
            for (var ch = 0; ch < channels; ch++) allChannelSamples[ch].AddRange(decoded[ch]);
        }

        var totalSamples = allChannelSamples[0].Count;
        var channelArrays = new float[channels][];
        for (var ch = 0; ch < channels; ch++) channelArrays[ch] = [.. allChannelSamples[ch]];

        var pcmData = interleave_float_to_pcm16(channelArrays, channels, totalSamples);
        return (pcmData, channels, sample_rate);
    }

    /// <summary>
    ///     的OGG 音频数据解码 Vorbis 音频为浮点采的
    /// </summary>
    public (float[] Samples, int Channels, int SampleRate) decode_to_float(OggAudioData oggData)
    {
        var (pcmData, channels, sampleRate) = decode_to_pcm16(oggData);
        var samples = new float[pcmData.Length / 2];

        for (var i = 0; i < samples.Length; i++)
        {
            var raw = BinaryPrimitives.ReadInt16LittleEndian(pcmData.AsSpan(i * 2));
            samples[i] = raw / 32768f;
        }

        return (samples, channels, sampleRate);
    }

    #endregion

    #region 头部解析

    private void parse_identification_header(byte[] packet)
    {
        if (packet.Length < 30 || packet[0] != 0x01) throw new InvalidDataException("Vorbis 识别头部无效");

        channels = packet[11];
        sample_rate = (int)read_le32(packet, 12);

        var blockSizesByte = packet[29];
        block_size0 = 1 << (blockSizesByte & 0x0F);
        block_size1 = 1 << ((blockSizesByte >> 4) & 0x0F);

        if (block_size0 > block_size1) throw new InvalidDataException("Vorbis 块大小无效：blockSize0 不能大于 blockSize1");
    }

    private static void parse_comment_header(byte[] packet)
    {
        if (packet.Length < 7 || packet[0] != 0x03) throw new InvalidDataException("Vorbis 注释头部无效");
    }

    private void parse_setup_header(byte[] packet)
    {
        if (packet.Length < 7 || packet[0] != 0x05) throw new InvalidDataException("Vorbis 设置头部无效");

        var reader = new BitReader(packet, 56);

        var codebookCount = (int)reader.read_bits(8) + 1;
        _codebooks = new VorbisCodebook[codebookCount];
        for (var i = 0; i < codebookCount; i++) _codebooks[i] = parse_codebook(ref reader);

        var floorCount = (int)reader.read_bits(6) + 1;
        _floors = new VorbisFloor[floorCount];
        for (var i = 0; i < floorCount; i++) _floors[i] = parse_floor(ref reader);

        var residueCount = (int)reader.read_bits(6) + 1;
        _residues = new VorbisResidue[residueCount];
        for (var i = 0; i < residueCount; i++) _residues[i] = parse_residue(ref reader);

        var mappingCount = (int)reader.read_bits(6) + 1;
        _mappings = new VorbisMapping[mappingCount];
        for (var i = 0; i < mappingCount; i++) _mappings[i] = parse_mapping(ref reader);

        var modeCount = (int)reader.read_bits(6) + 1;
        _modes = new VorbisMode[modeCount];
        for (var i = 0; i < modeCount; i++)
        {
            var blockFlag = (int)reader.read_bits(1);
            var windowType = (int)reader.read_bits(16);
            var transformType = (int)reader.read_bits(16);
            var mapping = (int)reader.read_bits(8);
            _modes[i] = new VorbisMode(blockFlag, mapping);
        }

        reader.read_bits(1);
    }

    private VorbisCodebook parse_codebook(ref BitReader reader)
    {
        var sync = (int)reader.read_bits(24);
        var dimensions = (int)reader.read_bits(16);
        var entries = (int)reader.read_bits(24);

        var entryLengths = new int[entries];
        var sparse = reader.read_bits(1) != 0;

        if (!sparse)
            for (var i = 0; i < entries; i++)
                entryLengths[i] = (int)reader.read_bits(5);
        else
            for (var i = 0; i < entries; i++)
            {
                var hasEntry = reader.read_bits(1) != 0;
                entryLengths[i] = hasEntry ? (int)reader.read_bits(5) : -1;
            }

        var lookupType = (int)reader.read_bits(4);
        var minimumValue = 0f;
        var deltaValue = 0f;
        var sequenceP = false;
        var multiplicands = Array.Empty<float>();

        if (lookupType is 1 or 2)
        {
            minimumValue = reader.read_float32();
            deltaValue = reader.read_float32();
            var valueBits = (int)reader.read_bits(4) + 1;
            sequenceP = reader.read_bits(1) != 0;

            var lookupValues = lookupType == 1
                ? lookup1_values(entries, dimensions)
                : entries * dimensions;

            multiplicands = new float[lookupValues];
            for (var i = 0; i < lookupValues; i++)
                multiplicands[i] = minimumValue + reader.read_bits((uint)valueBits) * deltaValue;
        }

        return new VorbisCodebook(dimensions, entries, entryLengths, lookupType,
            minimumValue, deltaValue, sequenceP, multiplicands);
    }

    private static VorbisFloor parse_floor(ref BitReader reader)
    {
        var floorType = (int)reader.read_bits(16);

        if (floorType == 1)
        {
            var partitions = (int)reader.read_bits(5);
            var maximumClass = -1;
            var partitionClassList = new int[partitions];

            for (var i = 0; i < partitions; i++)
            {
                partitionClassList[i] = (int)reader.read_bits(4);
                if (partitionClassList[i] > maximumClass) maximumClass = partitionClassList[i];
            }

            var classDimensions = new int[maximumClass + 1];
            var classSubclasses = new int[maximumClass + 1];
            var classMasterBooks = new int[maximumClass + 1];
            var subclassBooks = new int[maximumClass + 1][];

            for (var i = 0; i <= maximumClass; i++)
            {
                classDimensions[i] = (int)reader.read_bits(3) + 1;
                classSubclasses[i] = (int)reader.read_bits(2);
                if (classSubclasses[i] != 0) classMasterBooks[i] = (int)reader.read_bits(8);

                subclassBooks[i] = new int[1 << classSubclasses[i]];
                for (var j = 0; j < subclassBooks[i].Length; j++) subclassBooks[i][j] = (int)reader.read_bits(8) - 1;
            }

            var floor1Multiplier = (int)reader.read_bits(2) + 1;
            var rangeBits = (int)reader.read_bits(4);
            var values = 2;
            for (var i = 0; i < partitions; i++) values += classDimensions[partitionClassList[i]];

            var floorValues = new int[values];
            floorValues[0] = 0;
            floorValues[1] = (int)reader.read_bits((uint)rangeBits);
            for (var i = 2; i < values; i++) floorValues[i] = (int)reader.read_bits((uint)rangeBits);

            return new VorbisFloor(1, partitions, partitionClassList, classDimensions,
                classSubclasses, classMasterBooks, subclassBooks, floor1Multiplier, rangeBits, floorValues);
        }

        reader.read_bits(8);
        reader.read_bits(16);
        reader.read_bits(16);
        reader.read_bits(2);
        reader.read_bits(4);
        return new VorbisFloor(0);
    }

    private static VorbisResidue parse_residue(ref BitReader reader)
    {
        var residueType = (int)reader.read_bits(16);
        var begin = (int)reader.read_bits(24);
        var end = (int)reader.read_bits(24);
        var partitionSize = (int)reader.read_bits(24) + 1;
        var classifications = (int)reader.read_bits(6) + 1;
        var classbook = (int)reader.read_bits(8);

        var cascade = new int[classifications];
        for (var i = 0; i < classifications; i++)
        {
            var lowBits = (int)reader.read_bits(3);
            var bitFlag = reader.read_bits(1);
            cascade[i] = bitFlag != 0 ? lowBits | ((int)reader.read_bits(5) << 3) : lowBits;
        }

        var books = new int[classifications, 8];
        for (var i = 0; i < classifications; i++)
        for (var j = 0; j < 8; j++)
            if ((cascade[i] & (1 << j)) != 0)
                books[i, j] = (int)reader.read_bits(8);

        return new VorbisResidue(residueType, begin, end, partitionSize, classifications, classbook, books, cascade);
    }

    private VorbisMapping parse_mapping(ref BitReader reader)
    {
        var mappingType = (int)reader.read_bits(16);
        if (mappingType != 0) throw new InvalidDataException($"Vorbis 映射类型 {mappingType} 不支持。");

        var submaps = reader.read_bits(1) != 0 ? (int)reader.read_bits(4) + 1 : 1;

        var couplingSteps = 0;
        var magnitude = Array.Empty<int>();
        var angle = Array.Empty<int>();

        if (reader.read_bits(1) != 0)
        {
            couplingSteps = (int)reader.read_bits(8) + 1;
            magnitude = new int[couplingSteps];
            angle = new int[couplingSteps];
            for (var i = 0; i < couplingSteps; i++)
            {
                magnitude[i] = (int)reader.read_bits(8);
                angle[i] = (int)reader.read_bits(8);
            }
        }

        if (reader.read_bits(2) != 0) throw new InvalidDataException("Vorbis 保留映射字段不为 0。");

        var channelSubmap = new int[channels];
        if (submaps > 1)
            for (var ch = 0; ch < channels; ch++)
                channelSubmap[ch] = (int)reader.read_bits(4);

        var submapFloor = new int[submaps];
        var submapResidue = new int[submaps];

        for (var i = 0; i < submaps; i++)
        {
            reader.read_bits(8);
            submapFloor[i] = (int)reader.read_bits(8);
            submapResidue[i] = (int)reader.read_bits(8);
        }

        return new VorbisMapping(submaps, couplingSteps, magnitude, angle,
            channelSubmap, submapFloor, submapResidue);
    }

    #endregion

    #region 地板解码

    private float[] decode_floor1(ref BitReader reader, VorbisFloor floor, int halfBlock)
    {
        if (floor.type != 1) return new float[halfBlock];

        var range = floor.floor1_multiplier switch
        {
            1 => 256,
            2 => 128,
            3 => 86,
            4 => 64,
            _ => 256
        };

        var yList = new int[floor.floor_values.Length];
        Array.Copy(floor.floor_values, yList, yList.Length);

        var partitions = floor.partitions;

        for (var i = 0; i < partitions; i++)
        {
            var classNum = floor.partition_class_list[i];
            var cDim = floor.class_dimensions[classNum];
            var cSub = floor.class_subclasses[classNum];
            var cVal = 0;

            if (cSub != 0) cVal = decode_codebook_symbol(ref reader, floor.class_master_books[classNum]);

            for (var j = 0; j < cDim; j++)
            {
                var bookIndex = cSub != 0 ? cVal & ((1 << cSub) - 1) : 0;
                cVal >>= cSub;

                var book = floor.subclass_books[classNum][bookIndex];
                if (book >= 0 && book < _codebooks.Length)
                {
                    var val = decode_codebook_scalar(ref reader, book);
                    var idx = 2 + i * cDim + j;
                    if (idx < yList.Length) yList[idx] = val;
                }
            }
        }

        return synthesize_floor1(yList, halfBlock, range);
    }

    private int decode_codebook_symbol(ref BitReader reader, int codebookIndex)
    {
        if (codebookIndex < 0 || codebookIndex >= _codebooks.Length) return 0;

        return _codebooks[codebookIndex].decode_symbol(ref reader);
    }

    private int decode_codebook_scalar(ref BitReader reader, int codebookIndex)
    {
        if (codebookIndex < 0 || codebookIndex >= _codebooks.Length) return 0;

        return _codebooks[codebookIndex].decode_symbol(ref reader);
    }

    private static float[] synthesize_floor1(int[] yList, int halfBlock, int range)
    {
        var output = new float[halfBlock];

        if (yList.Length < 2) return output;

        var hyList = new float[yList.Length];
        for (var i = 0; i < yList.Length; i++) hyList[i] = yList[i] / (float)range;

        for (var i = 0; i < halfBlock; i++)
        {
            var x = (float)i / halfBlock * (yList.Length - 1);
            var idx = (int)x;
            var frac = x - idx;

            if (idx >= yList.Length - 1)
                output[i] = hyList[yList.Length - 1];
            else
                output[i] = hyList[idx] * (1 - frac) + hyList[idx + 1] * frac;
        }

        for (var i = 0; i < halfBlock; i++) output[i] = (float)System.Math.Exp(output[i] * 11.539 - 11.539);

        return output;
    }

    #endregion

    #region 加窗

    private float[] get_window(int blockSize, bool isLongBlock, bool prevFlag, bool nextFlag)
    {
        var window = new float[blockSize];
        var half = blockSize / 2;
        var prevSize = isLongBlock || prevFlag ? blockSize : block_size0;
        var nextSize = isLongBlock || nextFlag ? blockSize : block_size0;

        var leftStart = (blockSize - prevSize) / 4;
        var leftEnd = leftStart + prevSize / 2;
        var rightStart = leftEnd;
        var rightEnd = rightStart + nextSize / 2;

        for (var i = 0; i < blockSize; i++)
            if (i < leftStart)
            {
                window[i] = 0f;
            }
            else if (i < leftEnd)
            {
                var x = (float)(i - leftStart) / (leftEnd - leftStart);
                window[i] = (float)System.Math.Sin(0.5 * System.Math.PI * vorbis_window_func(x));
            }
            else if (i < rightStart)
            {
                window[i] = 1f;
            }
            else if (i < rightEnd)
            {
                var x = (float)(i - rightStart) / (rightEnd - rightStart);
                window[i] = (float)System.Math.Cos(0.5 * System.Math.PI * vorbis_window_func(x));
            }
            else
            {
                window[i] = 0f;
            }

        return window;
    }

    private static double vorbis_window_func(double x)
    {
        if (x < 0.5) return 2.0 * x * x;

        var y = 1.0 - x;
        return 1.0 - 2.0 * y * y;
    }

    #endregion

    #region 辅助方法

    private static uint read_le32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static int i_log(int value)
    {
        var result = 0;
        while (1 << result < value) result++;
        return result;
    }

    private static int lookup1_values(int entries, int dimensions)
    {
        var vals = (int)System.Math.Floor(System.Math.Pow(entries, 1.0 / dimensions));
        while (true)
        {
            var acc = 1;
            var acc1 = 1;
            for (var i = 0; i < dimensions; i++)
            {
                acc *= vals;
                acc1 *= vals + 1;
            }

            if (acc <= entries && acc1 > entries) return vals;

            if (acc > entries)
            {
                vals--;
                continue;
            }

            vals++;
        }
    }

    #endregion

    #region 内部类型

    private sealed class HuffmanNode
    {
        public HuffmanNode? one;
        public int symbol = -1;
        public HuffmanNode? zero;
    }

    private sealed class VorbisCodebook
    {
        private readonly HuffmanNode _root;
        private readonly float[] _value_list;
        public readonly int dimensions;
        public readonly int lookup_type;
        public int entries;

        public VorbisCodebook(int dimensions, int entries, int[] entryLengths, int lookupType,
            float minimumValue, float deltaValue, bool sequenceP, float[] multiplicands)
        {
            this.dimensions = dimensions;
            this.entries = entries;
            lookup_type = lookupType;

            _root = build_huffman_tree(entryLengths, entries);
            _value_list = build_value_list(entries, dimensions, lookupType,
                minimumValue, deltaValue, sequenceP, multiplicands);
        }

        public int decode_symbol(ref BitReader reader)
        {
            var node = _root;
            while (node.symbol < 0)
            {
                var bit = reader.read_bits(1);
                node = bit == 0 ? node.zero : node.one;
                if (node == null) return 0;
            }

            return node.symbol;
        }

        public float get_value(int symbol, int dimension)
        {
            if (lookup_type == 0) return symbol;

            var idx = symbol * dimensions + dimension;
            return idx < _value_list.Length ? _value_list[idx] : 0;
        }

        private static HuffmanNode build_huffman_tree(int[] entryLengths, int entries)
        {
            var maxLength = 0;
            for (var i = 0; i < entries; i++)
                if (entryLengths[i] > maxLength)
                    maxLength = entryLengths[i];

            if (maxLength == 0)
            {
                var tree = new HuffmanNode
                {
                    symbol = 0
                };
                return tree;
            }

            var codes = new uint[entries];
            var code = 0u;
            for (var l = 1; l <= maxLength; l++)
            {
                for (var i = 0; i < entries; i++)
                    if (entryLengths[i] == l)
                    {
                        codes[i] = code;
                        code++;
                    }

                code <<= 1;
            }

            var root = new HuffmanNode();
            for (var i = 0; i < entries; i++)
            {
                if (entryLengths[i] <= 0) continue;

                var node = root;
                for (var bit = 0; bit < entryLengths[i]; bit++)
                {
                    var b = (codes[i] >> bit) & 1;
                    if (b == 0)
                    {
                        node.zero ??= new HuffmanNode();
                        node = node.zero;
                    }
                    else
                    {
                        node.one ??= new HuffmanNode();
                        node = node.one;
                    }
                }

                node.symbol = i;
            }

            return root;
        }

        private static float[] build_value_list(int entries, int dimensions, int lookupType,
            float minimumValue, float deltaValue, bool sequenceP, float[] multiplicands)
        {
            if (lookupType == 0) return [];

            var valueList = new float[entries * dimensions];

            if (lookupType == 1)
            {
                var lookupValues = lookup1_values(entries, dimensions);
                for (var entry = 0; entry < entries; entry++)
                {
                    var last = 0f;
                    var indexDiv = 1;
                    for (var j = 0; j < dimensions; j++)
                    {
                        var multiplicandOffset = entry / indexDiv % lookupValues;
                        var value = minimumValue + multiplicands[multiplicandOffset] * deltaValue;
                        if (sequenceP)
                        {
                            value += last;
                            last = value;
                        }

                        valueList[entry * dimensions + j] = value;
                        indexDiv *= lookupValues;
                    }
                }
            }
            else if (lookupType == 2)
            {
                for (var entry = 0; entry < entries; entry++)
                {
                    var last = 0f;
                    for (var j = 0; j < dimensions; j++)
                    {
                        var multiplicandOffset = entry * dimensions + j;
                        var value = minimumValue + multiplicands[multiplicandOffset] * deltaValue;
                        if (sequenceP)
                        {
                            value += last;
                            last = value;
                        }

                        valueList[entry * dimensions + j] = value;
                    }
                }
            }

            return valueList;
        }
    }

    private sealed class VorbisFloor
    {
        public readonly int[] class_dimensions;
        public readonly int[] class_master_books;
        public readonly int[] class_subclasses;
        public readonly int floor1_multiplier;
        public readonly int[] floor_values;
        public readonly int[] partition_class_list;
        public readonly int partitions;
        public readonly int[][] subclass_books;
        public readonly int type;
        public int range_bits;

        public VorbisFloor(int type)
        {
            this.type = type;
            partition_class_list = [];
            class_dimensions = [];
            class_subclasses = [];
            class_master_books = [];
            subclass_books = [];
            floor_values = [];
        }

        public VorbisFloor(int type, int partitions, int[] partitionClassList, int[] classDimensions,
            int[] classSubclasses, int[] classMasterBooks, int[][] subclassBooks,
            int floor1Multiplier, int rangeBits, int[] floorValues)
        {
            this.type = type;
            this.partitions = partitions;
            partition_class_list = partitionClassList;
            class_dimensions = classDimensions;
            class_subclasses = classSubclasses;
            class_master_books = classMasterBooks;
            subclass_books = subclassBooks;
            floor1_multiplier = floor1Multiplier;
            range_bits = rangeBits;
            floor_values = floorValues;
        }
    }

    private sealed class VorbisResidue
    {
        public readonly int begin;
        public readonly int[,] books;
        public readonly int[] cascade;
        public readonly int classbook;
        public readonly int classifications;
        public readonly int end;
        public readonly int partition_size;
        public readonly int type;

        public VorbisResidue(int type, int begin, int end, int partitionSize,
            int classifications, int classbook, int[,] books, int[] cascade)
        {
            this.type = type;
            this.begin = begin;
            this.end = end;
            partition_size = partitionSize;
            this.classifications = classifications;
            this.classbook = classbook;
            this.books = books;
            this.cascade = cascade;
        }
    }

    private sealed class VorbisMapping
    {
        public readonly int[] angle;
        public readonly int[] channel_submap;
        public readonly int coupling_steps;
        public readonly int[] magnitude;
        public readonly int[] submap_floor;
        public readonly int[] submap_residue;
        public readonly int submaps;

        public VorbisMapping(int submaps, int couplingSteps, int[] magnitude, int[] angle,
            int[] channelSubmap, int[] submapFloor, int[] submapResidue)
        {
            this.submaps = submaps;
            coupling_steps = couplingSteps;
            this.magnitude = magnitude;
            this.angle = angle;
            channel_submap = channelSubmap;
            submap_floor = submapFloor;
            submap_residue = submapResidue;
        }
    }

    private sealed record VorbisMode(int block_flag, int mapping);

    private ref struct BitReader
    {
        private readonly byte[] _data;
        private int _bit_position;

        public BitReader(byte[] data, int startBit)
        {
            _data = data;
            _bit_position = startBit;
        }

        public uint read_bits(uint count)
        {
            uint result = 0;
            for (var i = 0; i < count; i++)
            {
                var byteIndex = _bit_position / 8;
                var bitIndex = _bit_position % 8;

                if (byteIndex < _data.Length && (_data[byteIndex] & (1 << bitIndex)) != 0) result |= 1u << i;

                _bit_position++;
            }

            return result;
        }

        public float read_float32()
        {
            var bits = read_bits(32);
            return BitConverter.Int32BitsToSingle((int)bits);
        }
    }

    #endregion
}