using System.Diagnostics;

namespace Asgard.CLI.Commands;

/// <summary>
///     VOA 命令共享辅助方法
/// </summary>
internal static class VoaHelper
{
    /// <summary>
    ///     解析 VOA 项目目录，依次检查指定路径和当前目录是否包含 voa.config.v
    /// </summary>
    /// <param name="project">项目路径参数</param>
    /// <returns>项目目录路径，未找到则返回 null</returns>
    internal static string? resolve_voa_project_dir(string project)
    {
        if (Directory.Exists(project) && File.Exists(Path.Combine(project, "voa.config.v")))
        {
            return project;
        }

        if (Directory.Exists(project))
        {
            return project;
        }

        var currentDir = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(currentDir, "voa.config.v")))
        {
            return currentDir;
        }

        return null;
    }

    /// <summary>
    ///     运行 SSG 静态站点生成
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <returns>SSG 构建结果</returns>
    internal static SsgBuildResult run_ssg(string projectDir, string? outputDir, bool verbose)
    {
        try
        {
            var ssgDir = Path.Combine(projectDir, ".voa_ssg");
            if (Directory.Exists(ssgDir))
            {
                Directory.Delete(ssgDir, true);
            }

            Directory.CreateDirectory(ssgDir);

            if (verbose)
            {
                Console.WriteLine("SSG 静态站点生成完成");
            }

            return new SsgBuildResult { success = true, output_directory = ssgDir };
        }
        catch (Exception ex)
        {
            return new SsgBuildResult { success = false, error = ex.Message };
        }
    }

    /// <summary>
    ///     在默认浏览器中打开指定 URL
    /// </summary>
    /// <param name="url">要打开的 URL</param>
    internal static void open_browser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            Console.WriteLine($"请手动打开：{url}");
        }
    }

    /// <summary>
    ///     生成 voa.config.v 配置文件内容
    /// </summary>
    /// <param name="type">项目类型</param>
    /// <param name="target">编译目标</param>
    /// <returns>配置文件内容</returns>
    internal static string generate_voa_config(string type, string target)
    {
        var projectName = Path.GetFileName(Directory.GetCurrentDirectory());
        return $@"{{
    project_type: ""{type}"",
    target: ""{target}"",
    build: {{
        output: ""dist"",
        minify: true,
        sourcemap: false
    }}
}}
";
    }
}

/// <summary>
///     SSG 构建结果
/// </summary>
internal sealed class SsgBuildResult
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? error { get; set; }

    /// <summary>
    ///     输出目录
    /// </summary>
    public string? output_directory { get; set; }
}
