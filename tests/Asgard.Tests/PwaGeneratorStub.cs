using System.Text;

namespace VOA.ToolChain.Tests;

/// <summary>
///     PWA 生成器桩实现（测试用）
/// </summary>
public static class PwaGenerator
{
    /// <summary>
    ///     生成 Service Worker 脚本
    /// </summary>
    /// <param name="cacheName">缓存名称</param>
    /// <param name="assets">预缓存资源列表</param>
    /// <returns>Service Worker 脚本内容</returns>
    public static string GenerateServiceWorker(string cacheName, string[] assets)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"const CACHE_NAME = '{cacheName}-v1';");
        sb.AppendLine($"const ASSETS = {FormatArray(assets)};");
        sb.AppendLine();
        sb.AppendLine("self.addEventListener('install', (event) => {");
        sb.AppendLine("    event.waitUntil(");
        sb.AppendLine("        caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS))");
        sb.AppendLine("    );");
        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("self.addEventListener('activate', (event) => {");
        sb.AppendLine("    event.waitUntil(");
        sb.AppendLine("        caches.keys().then((names) =>");
        sb.AppendLine("            Promise.all(names.filter((n) => n !== CACHE_NAME).map((n) => caches.delete(n)))");
        sb.AppendLine("        )");
        sb.AppendLine("    );");
        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("self.addEventListener('fetch', (event) => {");
        sb.AppendLine("    event.respondWith(");
        sb.AppendLine("        caches.match(event.request).then((cached) => cached || fetch(event.request))");
        sb.AppendLine("    );");
        sb.AppendLine("});");
        return sb.ToString();
    }

    /// <summary>
    ///     生成 PWA Manifest JSON
    /// </summary>
    /// <param name="appName">应用全名</param>
    /// <param name="shortName">应用短名</param>
    /// <returns>Manifest JSON 内容</returns>
    public static string GenerateManifest(string appName, string shortName)
    {
        return $$"""
                 {
                     "name": "{{appName}}",
                     "short_name": "{{shortName}}",
                     "start_url": "/",
                     "display": "standalone",
                     "background_color": "#ffffff",
                     "theme_color": "#000000"
                 }
                 """;
    }

    /// <summary>
    ///     生成 Service Worker 注册脚本
    /// </summary>
    /// <returns>注册脚本内容</returns>
    public static string GenerateRegistrationScript()
    {
        return """
               if ('serviceWorker' in navigator) {
                   window.addEventListener('load', () => {
                       navigator.serviceWorker.register('/sw.js').then((registration) => {
                           console.log('ServiceWorker 注册成功:', registration.scope);
                       }).catch((error) => {
                           console.log('ServiceWorker 注册失败:', error);
                       });
                   });
               }
               """;
    }

    private static string FormatArray(string[] items)
    {
        return $"[{string.Join(", ", items.Select(i => $"'{i}'"))}]";
    }
}