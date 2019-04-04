using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace LightDB.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddColumn(StatisticColumn.P95);
        AddColumn(StatisticColumn.StdDev);
    }
}