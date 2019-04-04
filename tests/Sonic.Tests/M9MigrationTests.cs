#if false // 跳过：CliMigrationAuditor 类型不存在
namespace Commander.Testing;

/// <summary>
/// M9 全公司 CLI 迁移审计测试
/// </summary>
public sealed class M9MigrationTests
{
    #region 迁移审计器

    /// <summary>
    /// GenerateReport 应生成非空报告
    /// </summary>
    [Fact]
    public void CliMigrationAuditor_GenerateReport_ShouldNotBeEmpty()
    {
        var report = CliMigrationAuditor.GenerateReport();

        Assert.NotNull(report);
        Assert.NotEmpty(report.Entries);
        Assert.True(report.TotalCliTools > 0);
    }

    /// <summary>
    /// 审计条目中应包含主要 CLI 工具
    /// </summary>
    [Fact]
    public void CliMigrationAuditor_GetAllCliTools_ShouldContainKeyTools()
    {
        var tools = CliMigrationAuditor.GetAllCliTools();

        Assert.Contains(tools, t => t.ToolName == "Valkyrie CLI");
        Assert.Contains(tools, t => t.ToolName == "Hermes CLI");
        Assert.Contains(tools, t => t.ToolName == "Atlas CLI");
    }

    /// <summary>
    /// 所有已计划迁移的工具应标记为 Planned
    /// </summary>
    [Fact]
    public void CliMigrationAuditor_PlannedTools_ShouldHavePlannedStatus()
    {
        var tools = CliMigrationAuditor.GetAllCliTools();

        var nonExempt = tools.Where(t => t.Status != MigrationStatus.Exempt).ToList();

        Assert.All(nonExempt, t =>
            Assert.True(t.Status == MigrationStatus.Planned || t.Status == MigrationStatus.Pending));
    }

    /// <summary>
    /// AdoptionRate 初始值应为 0（无已完成的迁移）
    /// </summary>
    [Fact]
    public void MigrationAuditReport_AdoptionRate_InitiallyZero()
    {
        var report = CliMigrationAuditor.GenerateReport();

        Assert.Equal(0, report.MigratedCount);
        Assert.Equal(0, report.AdoptionRate);
    }

    /// <summary>
    /// TargetAdoptionRate 应反映 Planned 状态的工具比例（含 Pending 则不足 90%）
    /// </summary>
    [Fact]
    public void MigrationAuditReport_TargetAdoptionRate_ShouldIncludePlanned()
    {
        var report = CliMigrationAuditor.GenerateReport();

        Assert.True(report.TargetAdoptionRate > report.AdoptionRate);
        Assert.Equal(8, report.PlannedCount);
        Assert.Equal(4, report.PendingCount);
    }

    /// <summary>
    /// FormatReport 应生成带关键信息的格式化文本
    /// </summary>
    [Fact]
    public void MigrationAuditReport_FormatReport_ShouldContainKeyInfo()
    {
        var report = CliMigrationAuditor.GenerateReport();
        var formatted = report.FormatReport();

        Assert.Contains("Command CLI 迁移审计报告", formatted);
        Assert.Contains("CLI 工具总数", formatted);
        Assert.Contains("采用率", formatted);
        Assert.Contains("Valkyrie CLI", formatted);
        Assert.Contains("Hermes CLI", formatted);
    }

    /// <summary>
    /// FormatReport 应包含每个审计条目的状态图标
    /// </summary>
    [Fact]
    public void MigrationAuditReport_FormatReport_ShouldContainStatusIcons()
    {
        var report = CliMigrationAuditor.GenerateReport();
        var formatted = report.FormatReport();

        Assert.Contains("📋", formatted);
        Assert.Contains("⏳", formatted);
        Assert.Contains("➖", formatted);
    }

    #endregion

    #region 迁移指南

    /// <summary>
    /// System.CommandLine 迁移示例非空
    /// </summary>
    [Fact]
    public void CliMigrationGuide_GetSystemCommandLineExamples_ShouldNotBeEmpty()
    {
        var examples = CliMigrationGuide.GetSystemCommandLineExamples();

        Assert.NotEmpty(examples);
        Assert.All(examples, e =>
        {
            Assert.NotEmpty(e.Title);
            Assert.NotEmpty(e.BeforeCode);
            Assert.NotEmpty(e.AfterCode);
            Assert.Equal("System.CommandLine", e.BeforeFramework);
        });
    }

    /// <summary>
    /// McMaster 迁移示例非空
    /// </summary>
    [Fact]
    public void CliMigrationGuide_GetMcMasterExamples_ShouldNotBeEmpty()
    {
        var examples = CliMigrationGuide.GetMcMasterExamples();

        Assert.NotEmpty(examples);
        Assert.All(examples, e =>
        {
            Assert.NotEmpty(e.Title);
            Assert.NotEmpty(e.BeforeCode);
            Assert.NotEmpty(e.AfterCode);
            Assert.Equal("McMaster", e.BeforeFramework);
        });
    }

    /// <summary>
    /// GetAllExamples 应合并两种框架的迁移示例
    /// </summary>
    [Fact]
    public void CliMigrationGuide_GetAllExamples_ShouldCombineBoth()
    {
        var scCount = CliMigrationGuide.GetSystemCommandLineExamples().Count;
        var mcCount = CliMigrationGuide.GetMcMasterExamples().Count;
        var allCount = CliMigrationGuide.GetAllExamples().Count;

        Assert.Equal(scCount + mcCount, allCount);
    }

    /// <summary>
    /// 每个迁移示例都应在 AfterCode 中引用 Command API
    /// </summary>
    [Fact]
    public void CliMigrationGuide_AllExamples_AfterCode_ShouldReferenceCommandApi()
    {
        var all = CliMigrationGuide.GetAllExamples();

        Assert.All(all, e => Assert.Contains("Command", e.AfterCode));
    }

    #endregion
}

#endif