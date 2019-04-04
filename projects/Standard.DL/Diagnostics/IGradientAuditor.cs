using Std.DL.Flux;

namespace Std.DL.Diagnostics;

/// <summary>梯度健康审计</summary>
public interface IGradientAuditor
{
    /// <summary>审计梯度</summary>
    GradientStats Audit(ArrayND gradient);

    /// <summary>判断梯度是否健康</summary>
    bool IsHealthy(GradientStats stats);

    /// <summary>获取梯度报告</summary>
    string GetReport(GradientStats stats);
}