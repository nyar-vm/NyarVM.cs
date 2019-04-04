using BenchmarkDotNet.Attributes;
using LightDB.Core;

namespace LightDB.Benchmarks.Core;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class KeyComparisonBenchmark
{
    private LightKey _keyA;
    private LightKey _keyB;
    private LightKey _keyEqual;
    private LightKey _keyLong;
    private LightKey _keyPrefix;

    [GlobalSetup]
    public void Setup()
    {
        var shortBytes = "symbols:/project/main.gg:Player:Class"u8.ToArray();
        var shortBytes2 = "symbols:/project/main.gg:Enemy:Class"u8.ToArray();
        var equalBytes = "symbols:/project/main.gg:Player:Class"u8.ToArray();
        var prefixBytes = "symbols:/project/main.gg:"u8.ToArray();
        var longBytes = new byte[512];
        Random.Shared.NextBytes(longBytes);
        longBytes[0] = (byte)'z';
        var longBytes2 = new byte[512];
        Random.Shared.NextBytes(longBytes2);
        longBytes2[0] = (byte)'z';
        longBytes2[256] ^= 0xFF;

        _keyA = new LightKey(shortBytes);
        _keyB = new LightKey(shortBytes2);
        _keyEqual = new LightKey(equalBytes);
        _keyPrefix = new LightKey(prefixBytes);
        _keyLong = new LightKey(longBytes);
    }

    [Benchmark(Description = "CompareTo - 短键不同")]
    public int CompareTo_ShortDifferent()
    {
        return _keyA.CompareTo(_keyB);
    }

    [Benchmark(Description = "CompareTo - 短键相同")]
    public int CompareTo_ShortEqual()
    {
        return _keyA.CompareTo(_keyEqual);
    }

    [Benchmark(Description = "StartsWith - 前缀匹配")]
    public bool StartsWith_Prefix()
    {
        return _keyA.StartsWith(_keyPrefix);
    }

    [Benchmark(Description = "CompareTo - 长键")]
    public int CompareTo_Long()
    {
        return _keyLong.CompareTo(_keyA);
    }
}