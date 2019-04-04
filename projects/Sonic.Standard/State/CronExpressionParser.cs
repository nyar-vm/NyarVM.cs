namespace Std.State;

/// <summary>
///     Cron 表达式解析器，解析标准 5 字段 Cron 表达式并计算下一次执行时间。
/// </summary>
public static class CronExpressionParser
{
    /// <summary>
    ///     计算 Cron 表达式的下一次执行时间。
    /// </summary>
    /// <param name="cronExpression">5 字段 Cron 表达式（分 时 日 月 周）</param>
    /// <param name="fromTime">起始参考时间</param>
    /// <returns>下一次执行时间，若无法计算则返回 null</returns>
    public static DateTimeOffset? get_next_run_time(string cronExpression, DateTimeOffset? fromTime = null)
    {
        var fields = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != 5) return null;

        var current = (fromTime ?? DateTimeOffset.UtcNow).AddMinutes(1);
        current = new DateTimeOffset(current.Year, current.Month, current.Day, current.Hour, current.Minute, 0,
            current.Offset);

        for (var i = 0; i < 525600; i++)
        {
            var minute = current.Minute;
            var hour = current.Hour;
            var day = current.Day;
            var month = current.Month;
            var dayOfWeek = (int)current.DayOfWeek;

            if (!field_matches(fields[0], minute))
            {
                current = current.AddMinutes(1);
                continue;
            }

            if (!field_matches(fields[1], hour))
            {
                current = current.AddHours(1);
                current = new DateTimeOffset(current.Year, current.Month, current.Day, current.Hour, 0, 0,
                    current.Offset);
                continue;
            }

            if (!field_matches(fields[2], day))
            {
                current = current.AddDays(1);
                current = new DateTimeOffset(current.Year, current.Month, current.Day, 0, 0, 0, current.Offset);
                continue;
            }

            if (!field_matches(fields[3], month))
            {
                current = current.AddMonths(1);
                current = new DateTimeOffset(current.Year, current.Month, 1, 0, 0, 0, current.Offset);
                continue;
            }

            if (!field_matches(fields[4], dayOfWeek))
            {
                current = current.AddDays(1);
                current = new DateTimeOffset(current.Year, current.Month, current.Day, 0, 0, 0, current.Offset);
                continue;
            }

            return current;
        }

        return null;
    }

    /// <summary>
    ///     验证 Cron 表达式是否合法。
    /// </summary>
    /// <param name="cronExpression">5 字段 Cron 表达式</param>
    /// <returns>是否合法</returns>
    public static bool is_valid(string cronExpression)
    {
        var fields = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length == 5;
    }

    private static bool field_matches(string field, int value)
    {
        if (field == "*") return true;

        foreach (var part in field.Split(','))
            if (part.Contains('/'))
            {
                var stepParts = part.Split('/');
                var rangePart = stepParts[0];
                var step = int.Parse(stepParts[1]);
                int start;
                int end;

                if (rangePart == "*")
                {
                    start = 0;
                    end = 59;
                }
                else if (rangePart.Contains('-'))
                {
                    var range = rangePart.Split('-');
                    start = int.Parse(range[0]);
                    end = int.Parse(range[1]);
                }
                else
                {
                    start = int.Parse(rangePart);
                    end = 59;
                }

                for (var v = start; v <= end; v += step)
                    if (v == value)
                        return true;
            }
            else if (part.Contains('-'))
            {
                var range = part.Split('-');
                var start = int.Parse(range[0]);
                var end = int.Parse(range[1]);

                if (value >= start && value <= end) return true;
            }
            else if (int.TryParse(part, out var exactValue))
            {
                if (exactValue == value) return true;
            }

        return false;
    }
}