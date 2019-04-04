using System.Collections.Concurrent;

namespace Std.State;

/// <summary>
///     Cron 调度器实现，管理定时任务的注册和按计划执行。
/// </summary>
public sealed class CronScheduler : IScheduler, IDisposable
{
    private readonly ConcurrentDictionary<string, JobState> _jobs = new();
    private readonly TimeSpan _tick_interval;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private Task? _loop_task;

    /// <summary>
    ///     初始化 Cron 调度器。
    /// </summary>
    /// <param name="tickInterval">调度器检查间隔（默认 30 秒）</param>
    public CronScheduler(TimeSpan? tickInterval = null)
    {
        _tick_interval = tickInterval ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    ///     释放调度器资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <inheritdoc />
    public void register_job(ICronJob job)
    {
        if (!CronExpressionParser.is_valid(job.cron_expression))
            throw new ArgumentException($"Cron 表达式格式无效：{job.cron_expression}");

        _jobs.TryAdd(job.name, new JobState(job));
    }

    /// <inheritdoc />
    public Task start(CancellationToken cancellationToken = default)
    {
        if (_loop_task is not null) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (var (_, state) in _jobs)
            state.next_run_time = CronExpressionParser.get_next_run_time(state.job.cron_expression);

        _loop_task = run_loop(_cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task stop(CancellationToken cancellationToken = default)
    {
        if (_cts is null) return;

        await _cts.CancelAsync();

        if (_loop_task is not null)
            try
            {
                await _loop_task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

        _loop_task = null;
    }

    /// <inheritdoc />
    public IReadOnlyList<CronJobState> get_job_states()
    {
        return
        [
            .. _jobs.Values.Select(state => new CronJobState
            {
                name = state.job.name,
                cron_expression = state.job.cron_expression,
                is_active = state.is_active,
                last_run_time = state.last_run_time,
                last_run_success = state.last_run_success,
                next_run_time = state.next_run_time,
                run_count = state.run_count
            })
        ];
    }

    /// <summary>
    ///     直接统计任务执行次数，便于测试验证。
    /// </summary>
    /// <param name="jobName">任务名称</param>
    /// <returns>执行次数</returns>
    internal long get_run_count(string jobName)
    {
        if (_jobs.TryGetValue(jobName, out var state)) return state.run_count;

        return 0;
    }

    private async Task run_loop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var (_, state) in _jobs)
                if (state.next_run_time.HasValue && state.next_run_time <= now)
                    await execute_job(state, cancellationToken);

            try
            {
                await Task.Delay(_tick_interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task execute_job(JobState state, CancellationToken cancellationToken)
    {
        state.is_active = true;

        try
        {
            state.last_run_time = DateTimeOffset.UtcNow;
            await state.job.execute(cancellationToken);
            state.last_run_success = true;
        }
        catch
        {
            state.last_run_success = false;
        }
        finally
        {
            state.run_count++;
            state.is_active = false;
            state.next_run_time = CronExpressionParser.get_next_run_time(state.job.cron_expression);
        }
    }

    private sealed class JobState
    {
        public JobState(ICronJob job)
        {
            this.job = job;
        }

        public ICronJob job { get; }
        public bool is_active { get; set; }
        public DateTimeOffset? last_run_time { get; set; }
        public bool? last_run_success { get; set; }
        public DateTimeOffset? next_run_time { get; set; }
        public long run_count { get; set; }
    }
}