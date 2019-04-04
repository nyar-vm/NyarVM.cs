using System.Text;

namespace Std.DL.Flux;

/// <summary>
///     分词器接口 —— 文本 ↔ token ID 转换
///     所有分词器的统一抽象
/// </summary>
public interface ITokenizer
{
    /// <summary>
    ///     词表大小
    /// </summary>
    int VocabSize { get; }

    /// <summary>
    ///     填充 token ID
    /// </summary>
    int PadTokenId { get; }

    /// <summary>
    ///     序列开始 token ID
    /// </summary>
    int BosTokenId { get; }

    /// <summary>
    ///     序列结束 token ID
    /// </summary>
    int EosTokenId { get; }

    /// <summary>
    ///     未知 token ID
    /// </summary>
    int UnkTokenId { get; }

    /// <summary>
    ///     将文本编码为 token ID 序列
    /// </summary>
    /// <param name="text">输入文本</param>
    /// <returns>token ID 数组</returns>
    int[] Encode(string text);

    /// <summary>
    ///     将 token ID 序列解码为文本
    /// </summary>
    /// <param name="tokenIds">token ID 数组</param>
    /// <returns>解码文本</returns>
    string Decode(int[] tokenIds);

    /// <summary>
    ///     将文本编码为 ArrayND [1, seqLen]
    /// </summary>
    /// <param name="text">输入文本</param>
    /// <returns>token ID 张量</returns>
    ArrayND EncodeToTensor(string text)
    {
        var ids = Encode(text);
        var tensor = ArrayND.Zeros(1, ids.Length);
        var span = tensor.AsWriteSpan();
        for (var i = 0; i < ids.Length; i++) span[i] = ids[i];
        return tensor;
    }

    /// <summary>
    ///     批量编码为 ArrayND [batch, maxLen]（自动填充）
    /// </summary>
    /// <param name="texts">文本列表</param>
    /// <returns>填充后的 token ID 张量 + 长度数组</returns>
    (ArrayND inputIds, ArrayND lengths) EncodeBatch(List<string> texts)
    {
        var allIds = new List<int[]>(texts.Count);
        var maxLen = 0;
        foreach (var text in texts)
        {
            var ids = Encode(text);
            allIds.Add(ids);
            if (ids.Length > maxLen) maxLen = ids.Length;
        }

        return DataLoader.PadSequences(allIds, maxLen, PadTokenId);
    }
}

/// <summary>
///     字符级分词器 —— 每个字符映射为一个 token
///     适用于小词表场景（如字符级语言模型、简单序列任务）
/// </summary>
public sealed class CharacterTokenizer : ITokenizer
{
    private readonly Dictionary<char, int> _charToId;
    private readonly Dictionary<int, char> _idToChar;

    /// <summary>
    ///     创建字符级分词器
    /// </summary>
    /// <param name="alphabet">字符集（如 "abcdefghijklmnopqrstuvwxyz "）</param>
    /// <param name="addSpecialTokens">是否添加特殊 token（PAD/BOS/EOS/UNK）</param>
    public CharacterTokenizer(string alphabet, bool addSpecialTokens = true)
    {
        _charToId = new Dictionary<char, int>();
        _idToChar = new Dictionary<int, char>();

        var nextId = 0;

        if (addSpecialTokens)
        {
            PadTokenId = nextId++;
            BosTokenId = nextId++;
            EosTokenId = nextId++;
            UnkTokenId = nextId++;

            _idToChar[PadTokenId] = '\0';
            _idToChar[BosTokenId] = '\x01';
            _idToChar[EosTokenId] = '\x02';
            _idToChar[UnkTokenId] = '\x03';
        }
        else
        {
            PadTokenId = -1;
            BosTokenId = -1;
            EosTokenId = -1;
            UnkTokenId = -1;
        }

        foreach (var ch in alphabet)
            if (_charToId.TryAdd(ch, nextId))
            {
                _idToChar[nextId] = ch;
                nextId++;
            }

        VocabSize = nextId;
    }

    /// <summary>
    ///     获取字符到 ID 的映射
    /// </summary>
    public IReadOnlyDictionary<char, int> CharToId => _charToId;

    /// <summary>
    ///     获取 ID 到字符的映射
    /// </summary>
    public IReadOnlyDictionary<int, char> IdToChar => _idToChar;

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     填充 token ID
    /// </summary>
    public int PadTokenId { get; }

    /// <summary>
    ///     序列开始 token ID
    /// </summary>
    public int BosTokenId { get; }

    /// <summary>
    ///     序列结束 token ID
    /// </summary>
    public int EosTokenId { get; }

    /// <summary>
    ///     未知 token ID
    /// </summary>
    public int UnkTokenId { get; }

    /// <summary>
    ///     编码文本为 token ID
    /// </summary>
    public int[] Encode(string text)
    {
        var ids = new List<int>(text.Length + 2);

        if (BosTokenId >= 0) ids.Add(BosTokenId);

        foreach (var ch in text) ids.Add(_charToId.GetValueOrDefault(ch, UnkTokenId));

        if (EosTokenId >= 0) ids.Add(EosTokenId);

        return [.. ids];
    }

    /// <summary>
    ///     解码 token ID 为文本
    /// </summary>
    public string Decode(int[] tokenIds)
    {
        var sb = new StringBuilder(tokenIds.Length);

        foreach (var id in tokenIds)
        {
            if (id == PadTokenId || id == BosTokenId || id == EosTokenId || id == UnkTokenId) continue;

            if (_idToChar.TryGetValue(id, out var ch)) sb.Append(ch);
        }

        return sb.ToString();
    }
}

/// <summary>
///     BPE（Byte Pair Encoding）分词器 —— LLM 标准分词方案
///     通过迭代合并最高频字符对来构建词表
/// </summary>
public sealed class BPETokenizer : ITokenizer
{
    private readonly Dictionary<int, string> _idToToken;
    private readonly List<(string a, string b)> _mergeRules;
    private readonly Dictionary<string, int> _tokenToId;

    /// <summary>
    ///     创建 BPE 分词器
    /// </summary>
    /// <param name="vocabSize">目标词表大小</param>
    /// <param name="trainingText">训练文本（用于学习合并规则）</param>
    /// <param name="padTokenId">填充 token ID</param>
    /// <param name="bosTokenId">序列开始 token ID</param>
    /// <param name="eosTokenId">序列结束 token ID</param>
    /// <param name="unkTokenId">未知 token ID</param>
    public BPETokenizer(int vocabSize, string trainingText,
        int padTokenId = 0, int bosTokenId = 1, int eosTokenId = 2, int unkTokenId = 3)
    {
        PadTokenId = padTokenId;
        BosTokenId = bosTokenId;
        EosTokenId = eosTokenId;
        UnkTokenId = unkTokenId;

        _tokenToId = new Dictionary<string, int>();
        _idToToken = new Dictionary<int, string>();
        _mergeRules = [];

        var nextId = 0;
        var specialTokens = new[] { padTokenId, bosTokenId, eosTokenId, unkTokenId };
        foreach (var st in specialTokens)
        {
            var name = $"<special_{st}>";
            _tokenToId[name] = st;
            _idToToken[st] = name;
            if (st >= nextId) nextId = st + 1;
        }

        var charFreq = new Dictionary<string, int>();
        foreach (var ch in trainingText)
        {
            var s = ch.ToString();
            charFreq.TryAdd(s, 0);
            charFreq[s]++;
        }

        foreach (var kv in charFreq.OrderByDescending(k => k.Value))
            if (_tokenToId.TryAdd(kv.Key, nextId))
            {
                _idToToken[nextId] = kv.Key;
                nextId++;
            }

        var numMerges = vocabSize - nextId;
        if (numMerges > 0) TrainBPE(trainingText, numMerges, ref nextId);

        VocabSize = nextId;
    }

    /// <summary>
    ///     获取合并规则列表
    /// </summary>
    public IReadOnlyList<(string a, string b)> MergeRules => _mergeRules;

    /// <summary>
    ///     获取 token 到 ID 的映射
    /// </summary>
    public IReadOnlyDictionary<string, int> TokenToId => _tokenToId;

    /// <summary>
    ///     获取 ID 到 token 的映射
    /// </summary>
    public IReadOnlyDictionary<int, string> IdToToken => _idToToken;

    /// <summary>
    ///     词表大小
    /// </summary>
    public int VocabSize { get; }

    /// <summary>
    ///     填充 token ID
    /// </summary>
    public int PadTokenId { get; }

    /// <summary>
    ///     序列开始 token ID
    /// </summary>
    public int BosTokenId { get; }

    /// <summary>
    ///     序列结束 token ID
    /// </summary>
    public int EosTokenId { get; }

    /// <summary>
    ///     未知 token ID
    /// </summary>
    public int UnkTokenId { get; }

    /// <summary>
    ///     编码文本为 token ID
    /// </summary>
    public int[] Encode(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length == 0) return [];

        var tokens = new List<string>();
        foreach (var ch in text) tokens.Add(ch.ToString());

        foreach (var rule in _mergeRules)
        {
            var a = rule.Item1;
            var b = rule.Item2;
            var merged = a + b;
            var newTokens = new List<string>();
            var i = 0;
            while (i < tokens.Count)
                if (i < tokens.Count - 1 && tokens[i] == a && tokens[i + 1] == b)
                {
                    newTokens.Add(merged);
                    i += 2;
                }
                else
                {
                    newTokens.Add(tokens[i]);
                    i++;
                }

            tokens = newTokens;
        }

        var ids = new List<int>(tokens.Count + 2);
        ids.Add(BosTokenId);
        foreach (var t in tokens) ids.Add(_tokenToId.GetValueOrDefault(t, UnkTokenId));

        ids.Add(EosTokenId);
        return [.. ids];
    }

    /// <summary>
    ///     解码 token ID 为文本
    /// </summary>
    public string Decode(int[] tokenIds)
    {
        var sb = new StringBuilder();
        foreach (var id in tokenIds)
        {
            if (id == PadTokenId || id == BosTokenId || id == EosTokenId || id == UnkTokenId) continue;

            if (_idToToken.TryGetValue(id, out var token)) sb.Append(token);
        }

        return sb.ToString();
    }

    private void TrainBPE(string text, int numMerges, ref int nextId)
    {
        var words = new List<List<string>>();
        var i = 0;
        while (i < text.Length)
        {
            var word = new List<string>();
            var end = System.Math.Min(i + 20, text.Length);
            for (var j = i; j < end; j++) word.Add(text[j].ToString());
            words.Add(word);
            i = end;
        }

        for (var merge = 0; merge < numMerges; merge++)
        {
            var pairFreq = new Dictionary<(string, string), int>();
            foreach (var word in words)
                for (var k = 0; k < word.Count - 1; k++)
                {
                    var pair = (word[k], word[k + 1]);
                    pairFreq.TryAdd(pair, 0);
                    pairFreq[pair]++;
                }

            if (pairFreq.Count == 0) break;

            var bestPair = pairFreq.OrderByDescending(kv => kv.Value).First().Key;
            var bestA = bestPair.Item1;
            var bestB = bestPair.Item2;
            var merged = bestA + bestB;

            _mergeRules.Add(bestPair);
            _tokenToId[merged] = nextId;
            _idToToken[nextId] = merged;
            nextId++;

            foreach (var word in words)
            {
                var newWord = new List<string>();
                var k = 0;
                while (k < word.Count)
                    if (k < word.Count - 1 && word[k] == bestA && word[k + 1] == bestB)
                    {
                        newWord.Add(merged);
                        k += 2;
                    }
                    else
                    {
                        newWord.Add(word[k]);
                        k++;
                    }

                word.Clear();
                word.AddRange(newWord);
            }
        }
    }
}