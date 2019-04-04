using System;

namespace Core.Chrono.Scheduling;

/// <summary>
///     标记方法为 Cron 定时任务
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CronJobAttribute : Attribute
{
    /// <summary>
    ///     初始化 Cron 定时任务特性
    /// </summary>
    /// <param name="expression">Cron 表达式</param>
    public CronJobAttribute(string expression)
    {
        this.expression = expression;
    }

    /// <summary>
    ///     Cron 表达式
    /// </summary>
    public string expression { get; }
}