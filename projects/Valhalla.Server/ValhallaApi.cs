using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Valhalla.Audit;
using Valhalla.Authorization;
using Valhalla.Server.Storage;

namespace Valhalla.Server;

/// <summary>
///     瓦尓哈拉 REST API 路由注册
/// </summary>
public static class ValhallaApi
{
    /// <summary>
    ///     注册所有 API 路由
    /// </summary>
    /// <param name="app">Web 应用</param>
    /// <param name="storage">存储后端</param>
    public static void map_api_routes(this WebApplication app, IStorage storage)
    {
        #region 包检索

        app.MapGet("/api/packages", async (
            [FromQuery] int page,
            [FromQuery] int size,
            [FromQuery] string? query,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            size = size is <= 0 or > 100 ? 20 : size;

            var allKeys = await storage.list(_manifest_prefix, ct);
            var manifests = new List<PackageManifest>();
            foreach (var key in allKeys)
            {
                if (!key.EndsWith("/manifest.json")) continue;

                var content = await storage.read_string(key, ct);
                if (content is null) continue;

                try
                {
                    var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
                    if (manifest is not null) manifests.Add(manifest);
                }
                catch
                {
                    // 跳过损坏的 manifest
                }
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.ToLowerInvariant();
                manifests =
                [
                    .. manifests
                        .Where(m => m.name.Contains(q, StringComparison.OrdinalIgnoreCase))
                ];
            }

            var total = manifests.Count;
            var pageItems = manifests
                .Skip((page - 1) * size)
                .Take(size)
                .Select(m =>
                {
                    var latestVersion = m.versions.Values
                        .Where(v => v.status == VersionStatus.active)
                        .MaxBy(v => v.published_at);

                    return new
                    {
                        m.name,
                        description = "",
                        latestVersion = latestVersion?.version ?? "0.0.0",
                        m.publisher,
                        m.incarnation,
                        status = m.status.ToString().ToLowerInvariant(),
                        downloadCount = 0L,
                        createdAt = m.registered_at,
                        updatedAt = latestVersion?.published_at ?? m.registered_at
                    };
                })
                .ToList();

            return Results.Json(new
            {
                packages = pageItems,
                total,
                page,
                size
            });
        });

        app.MapGet("/api/search", async (
            [FromQuery] string q,
            [FromQuery] string? category,
            [FromQuery] string? author,
            [FromQuery] int page,
            [FromQuery] int size,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            size = size is <= 0 or > 100 ? 20 : size;

            var allKeys = await storage.list(_manifest_prefix, ct);
            var results = new List<object>();

            foreach (var key in allKeys)
            {
                if (!key.EndsWith("/manifest.json")) continue;

                var content = await storage.read_string(key, ct);
                if (content is null) continue;

                try
                {
                    var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
                    if (manifest is null) continue;

                    var matches = manifest.name.Contains(q, StringComparison.OrdinalIgnoreCase);
                    if (!string.IsNullOrWhiteSpace(category) && matches)
                        matches = manifest.name.Contains(category, StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(author) && matches)
                        matches = manifest.publisher.Contains(author, StringComparison.OrdinalIgnoreCase);

                    if (matches)
                    {
                        var latestVersion = manifest.versions.Values
                            .Where(v => v.status == VersionStatus.active)
                            .MaxBy(v => v.published_at);

                        results.Add(new
                        {
                            manifest.name,
                            manifest.publisher,
                            latestVersion = latestVersion?.version ?? "0.0.0",
                            description = "",
                            author = manifest.publisher,
                            registry = "valhalla"
                        });
                    }
                }
                catch
                {
                    // 跳过损坏的 manifest
                }
            }

            var total = results.Count;
            var pageItems = results.Skip((page - 1) * size).Take(size).ToList();

            return Results.Json(new { packages = pageItems, total, page, size });
        });

        app.MapGet("/api/packages/{name}/manifest", async (
            string name,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 的 manifest 已损坏"));

            return Results.Json(manifest);
        });

        app.MapGet("/api/packages/{name}/versions", async (
            string name,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 的 manifest 已损坏"));

            var versions = manifest.versions.Values.Select(v => new
            {
                v.version,
                status = v.status.ToString().ToLowerInvariant(),
                packageDigest = v.package_digest,
                packageSize = v.package_size,
                publishedAt = v.published_at,
                hasSource = v.source_digest is not null
            }).ToList();

            return Results.Json(versions);
        });

        app.MapGet("/api/packages/{name}/versions/{version}", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.versions.TryGetValue(version, out var versionEntry) == true)
                return Results.Json(versionEntry);

            return Results.NotFound(ValhallaErrorResponse.not_found($"版本 {name}@{version} 不存在"));
        });

        app.MapGet("/api/packages/{name}/versions/{version}/download", async (
            string name,
            string version,
            HttpContext context,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.versions.TryGetValue(version, out var versionEntry) != true || versionEntry is null)
                return Results.NotFound(ValhallaErrorResponse.not_found($"版本 {name}@{version} 不存在"));

            var packagePath = $"{_binary_prefix}/{name}/{version}/package.nyar";
            var packageData = await storage.read_bytes(packagePath, ct);
            if (packageData is null)
                return Results.NotFound(ValhallaErrorResponse.not_found($"二进制 {name}@{version} 不存在"));

            var sourcePathKey = $"{_binary_prefix}/{name}/{version}/source.tar.gz";
            var sourceData = await storage.read_bytes(sourcePathKey, ct);

            var output = new MemoryStream();
            var sizeBytes = BitConverter.GetBytes(packageData.Length);
            output.Write(sizeBytes, 0, 4);
            output.Write(packageData, 0, packageData.Length);

            if (sourceData is not null)
            {
                var srcSizeBytes = BitConverter.GetBytes(sourceData.Length);
                output.Write(srcSizeBytes, 0, 4);
                output.Write(sourceData, 0, sourceData.Length);
            }
            else
            {
                var zero = BitConverter.GetBytes(0);
                output.Write(zero, 0, 4);
            }

            output.Position = 0;

            context.Response.Headers["X-Content-SHA256"] = versionEntry.package_digest ?? string.Empty;
            if (!string.IsNullOrEmpty(versionEntry.source_digest))
                context.Response.Headers["X-Source-SHA256"] = versionEntry.source_digest;

            return Results.File(output, "application/octet-stream");
        });

        #endregion

        #region 发布

        app.MapPost("/api/packages", async (
            HttpContext context,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            if (!body.RootElement.TryGetProperty("name", out var nameElement)
                || string.IsNullOrWhiteSpace(nameElement.get_string()))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少包名"));

            if (!body.RootElement.TryGetProperty("publisher", out var publisherElement)
                || string.IsNullOrWhiteSpace(publisherElement.get_string()))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少发布者指纹"));

            var packageName = nameElement.get_string();
            var publisher = publisherElement.get_string();

            var name = new PackageName(packageName!);
            var manifestPath = $"{_manifest_prefix}/{name.canonical}/manifest.json";

            if (await storage.exists(manifestPath, ct))
                return Results.Conflict(ValhallaErrorResponse.conflict($"包 {name.canonical} 已存在"));

            var manifest = new PackageManifest
            {
                name = name.canonical,
                incarnation = 1,
                publisher = publisher ?? "unknown",
                registered_at = DateTime.UtcNow,
                status = PackageStatus.active
            };

            var manifestJson = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.write_string(manifestPath, manifestJson, ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.register_package,
                package_name = name.canonical,
                actor = publisher ?? "unknown",
                actor_role = "publisher",
                details = new Dictionary<string, string> { ["action"] = "register" }
            }, ct);

            return Results.Json(manifest, statusCode: 201);
        });

        app.MapPost("/api/packages/{name}/versions", async (
            string name,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (!context.Request.HasFormContentType)
                return Results.BadRequest(ValhallaErrorResponse.validation_error("请求必须是 multipart/form-data 格式"));

            var form = await context.Request.ReadFormAsync(ct);

            string? manifestJson = form["manifest"];
            if (string.IsNullOrWhiteSpace(manifestJson))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少 manifest 字段"));

            var publishMeta = JsonSerializer.Deserialize<JsonDocument>(manifestJson);
            if (publishMeta is null
                || !publishMeta.RootElement.TryGetProperty("version", out var versionElement)
                || string.IsNullOrWhiteSpace(versionElement.get_string()))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少版本号"));

            var version = versionElement.get_string()!;

            var packageFile = form.Files.GetFile("package");
            if (packageFile is null) return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少 package 文件"));

            using var packageMs = new MemoryStream();
            await packageFile.CopyToAsync(packageMs, ct);
            var packageData = packageMs.ToArray();
            var packageDigest = ValhallaDigest.compute(packageData);

            byte[]? sourceData = null;
            string? sourceDigestHex = null;

            var sourceFile = form.Files.GetFile("source");
            if (sourceFile is not null)
            {
                using var sourceMs = new MemoryStream();
                await sourceFile.CopyToAsync(sourceMs, ct);
                sourceData = sourceMs.ToArray();
                var sourceDigest = ValhallaDigest.compute(sourceData);
                sourceDigestHex = sourceDigest.hex_string;
            }

            // 更新 manifest
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var existingContent = await storage.read_string(manifestPath, ct);
            if (existingContent is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(existingContent);
            if (manifest is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 的 manifest 已损坏"));

            manifest.versions[version] = new VersionEntry
            {
                version = version,
                status = VersionStatus.active,
                package_digest = packageDigest.hex_string,
                source_digest = sourceDigestHex,
                package_size = packageData.Length,
                source_size = sourceData?.Length,
                published_at = DateTime.UtcNow
            };

            await storage.write_string(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            // 存储二进制文件
            var packageStoragePath = $"{_binary_prefix}/{name}/{version}/package.nyar";
            await storage.write_bytes(packageStoragePath, packageData, ct);

            if (sourceData is not null)
            {
                var sourceStoragePath = $"{_binary_prefix}/{name}/{version}/source.tar.gz";
                await storage.write_bytes(sourceStoragePath, sourceData, ct);
            }

            var publisherFingerprint = manifest.publisher;

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.publish,
                package_name = name,
                version = version,
                sha256 = packageDigest.hex_string,
                actor = publisherFingerprint ?? "unknown",
                actor_role = "publisher",
                details = new Dictionary<string, string>
                {
                    ["version"] = version,
                    ["size"] = packageData.Length.ToString()
                }
            }, ct);

            return Results.Json(new
            {
                name,
                version,
                sha256 = packageDigest.hex_string,
                sourceSha256 = sourceDigestHex,
                size = packageData.Length,
                publishedAt = DateTime.UtcNow
            }, statusCode: 201);
        });

        #endregion

        #region 管理操作

        app.MapPost("/api/packages/{name}/shield/{version}", async (
            string name,
            string version,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.versions.TryGetValue(version, out var entry) != true || entry is null)
                return Results.NotFound(ValhallaErrorResponse.not_found($"版本 {name}@{version} 不存在"));

            entry.status = VersionStatus.shielded;
            entry.shield_reason = body.RootElement.TryGetProperty("reason", out var reason)
                ? reason.get_string() ?? string.Empty
                : string.Empty;
            entry.shielded_at = DateTime.UtcNow;

            await storage.write_string(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.shield,
                package_name = name,
                version = version,
                actor = "admin",
                actor_role = "admin",
                details = new Dictionary<string, string> { ["reason"] = entry.shield_reason ?? "" }
            }, ct);

            return Results.Json(new { name, version, status = "shielded" });
        });

        app.MapPost("/api/packages/{name}/unshield/{version}", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest?.versions.TryGetValue(version, out var unshieldEntry) != true || unshieldEntry is null)
                return Results.NotFound(ValhallaErrorResponse.not_found($"版本 {name}@{version} 不存在"));

            unshieldEntry.status = VersionStatus.active;
            unshieldEntry.shield_reason = string.Empty;
            unshieldEntry.shielded_at = null;

            await storage.write_string(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.unshield,
                package_name = name,
                version = version,
                actor = "admin",
                actor_role = "admin"
            }, ct);

            return Results.Json(new { name, version, status = "active" });
        });

        app.MapDelete("/api/packages/{name}", async (
            string name,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 的 manifest 已损坏"));

            manifest.status = PackageStatus.purged;
            manifest.purge_reason = body.RootElement.TryGetProperty("reason", out var purgeReason)
                ? purgeReason.get_string()
                : null;
            manifest.purged_at = DateTime.UtcNow;

            await storage.write_string(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.purge,
                package_name = name,
                actor = "admin",
                actor_role = "admin",
                details = new Dictionary<string, string>
                {
                    ["reason"] = manifest.purge_reason ?? "",
                    ["incarnation"] = manifest.incarnation.ToString()
                }
            }, ct);

            return Results.Json(new
            {
                name,
                manifest.incarnation,
                status = "purged",
                purgedAt = manifest.purged_at,
                purgedBy = "admin"
            });
        });

        #endregion

        #region 审计日志

        app.MapGet("/api/packages/{name}/audit", async (
            string name,
            CancellationToken ct) =>
        {
            var auditPath = $"{_audit_prefix}/{name}/audit.jsonl";
            var content = await storage.read_string(auditPath, ct);
            if (content is null) return Results.Json(Array.Empty<AuditEntry>());

            var entries = new List<AuditEntry>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
                try
                {
                    var entry = JsonSerializer.Deserialize<AuditEntry>(line);
                    if (entry is not null) entries.Add(entry);
                }
                catch
                {
                    // 跳过损坏的行
                }

            return Results.Json(entries);
        });

        #endregion

        #region 组织

        app.MapGet("/api/orgs", async (CancellationToken ct) =>
        {
            var orgKeys = await storage.list(_orgs_prefix, ct);
            var orgs = new List<object>();
            foreach (var key in orgKeys)
                if (key.EndsWith("/org.json"))
                {
                    var orgName = key.Replace($"{_orgs_prefix}/", "").Replace("/org.json", "");
                    orgs.Add(new { name = orgName });
                }

            return Results.Json(orgs);
        });

        app.MapPost("/api/orgs", async (
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            if (!body.RootElement.TryGetProperty("name", out var nameElement)
                || string.IsNullOrWhiteSpace(nameElement.get_string()))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少组织名"));

            if (!body.RootElement.TryGetProperty("publisher", out var publisherElement)
                || string.IsNullOrWhiteSpace(publisherElement.get_string()))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少发布者指纹"));

            var orgName = nameElement.get_string();
            var publisher = publisherElement.get_string();

            var name = new PackageName(orgName!);
            var orgPath = $"{_orgs_prefix}/{name.canonical}/org.json";

            if (await storage.exists(orgPath, ct))
                return Results.Conflict(ValhallaErrorResponse.conflict($"组织 {name.canonical} 已存在"));

            var orgJson = JsonSerializer.Serialize(new
            {
                name = name.canonical,
                publisher,
                registeredAt = DateTime.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true });

            await storage.write_string(orgPath, orgJson, ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.register_org,
                package_name = name.canonical,
                actor = publisher ?? "unknown",
                actor_role = "publisher",
                details = new Dictionary<string, string> { ["org"] = name.canonical }
            }, ct);

            return Results.Json(new
            {
                name = name.canonical,
                publisher,
                registeredAt = DateTime.UtcNow
            }, statusCode: 201);
        });

        #endregion

        #region 元信息

        app.MapGet("/api/packages/{name}/meta", async (
            string name,
            CancellationToken ct) =>
        {
            var metaPath = $"{_meta_prefix}/{name}/package-meta.json";
            var content = await storage.read_string(metaPath, ct);
            if (content is null) return Results.Json(new ValhallaPackageMeta());

            return Results.Json(JsonSerializer.Deserialize<ValhallaPackageMeta>(content));
        });

        app.MapGet("/api/packages/{name}/versions/{version}/meta", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            var metaPath = $"{_meta_prefix}/{name}/{version}/version-meta.json";
            var content = await storage.read_string(metaPath, ct);
            if (content is null) return Results.Json(new ValhallaVersionMeta { version = version });

            return Results.Json(JsonSerializer.Deserialize<ValhallaVersionMeta>(content));
        });

        #endregion

        #region 元信息更新

        app.MapPut("/api/packages/{name}/meta", async (
            string name,
            [FromBody] ValhallaPackageMeta body,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            if (!await storage.exists(manifestPath, ct))
                return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var meta = body;
            meta.updated_at = DateTime.UtcNow;

            var metaPath = $"{_meta_prefix}/{name}/package-meta.json";
            var metaJson = JsonSerializer.Serialize(meta,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.write_string(metaPath, metaJson, ct);

            return Results.Json(meta);
        });

        app.MapPut("/api/packages/{name}/versions/{version}/meta", async (
            string name,
            string version,
            [FromBody] ValhallaVersionMeta body,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            if (!await storage.exists(manifestPath, ct))
                return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var meta = body;
            meta.version = version;

            var metaPath = $"{_meta_prefix}/{name}/{version}/version-meta.json";
            var metaJson = JsonSerializer.Serialize(meta,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.write_string(metaPath, metaJson, ct);

            return Results.Json(meta);
        });

        #endregion

        #region 授权管理

        app.MapPost("/api/packages/{name}/auth", async (
            string name,
            [FromBody] AuthorizationGrant grant,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            if (!await storage.exists(manifestPath, ct))
                return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            if (string.IsNullOrWhiteSpace(grant.@namespace)) grant.@namespace = name;

            if (string.IsNullOrWhiteSpace(grant.grantee))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少被授权者公钥指纹"));

            grant.issued_at = DateTime.UtcNow;

            var authPath = $"{_manifest_prefix}/{name}/auth/{grant.grantee}/grant.json";
            var grantJson = JsonSerializer.Serialize(grant,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.write_string(authPath, grantJson, ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.authorize,
                package_name = name,
                actor = grant.issuer,
                actor_role = "publisher",
                details = new Dictionary<string, string>
                {
                    ["grantee"] = grant.grantee,
                    ["permissions"] = grant.permissions.ToString(),
                    ["namespace"] = grant.@namespace
                }
            }, ct);

            return Results.Json(grant, statusCode: 201);
        });

        app.MapDelete("/api/packages/{name}/auth", async (
            string name,
            [FromBody] AuthorizationRevocation revocation,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            if (!await storage.exists(manifestPath, ct))
                return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            if (string.IsNullOrWhiteSpace(revocation.@namespace)) revocation.@namespace = name;

            if (string.IsNullOrWhiteSpace(revocation.grantee))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少被撤销者公钥指纹"));

            revocation.issued_at = DateTime.UtcNow;

            var revokePath = $"{_manifest_prefix}/{name}/auth/{revocation.grantee}/revocation.json";
            var revokeJson = JsonSerializer.Serialize(revocation,
                new JsonSerializerOptions { WriteIndented = true });

            await storage.write_string(revokePath, revokeJson, ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.revoke_authorization,
                package_name = name,
                actor = revocation.revoker,
                actor_role = "publisher",
                details = new Dictionary<string, string>
                {
                    ["grantee"] = revocation.grantee,
                    ["namespace"] = revocation.@namespace
                }
            }, ct);

            return Results.Json(new
            {
                name,
                revocation.grantee,
                namespace_ = revocation.@namespace,
                revokedAt = revocation.issued_at,
                revokedBy = revocation.revoker
            });
        });

        #endregion

        #region 版本删除

        app.MapDelete("/api/packages/{name}/versions/{version}", async (
            string name,
            string version,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null || !manifest.versions.Remove(version, out _))
                return Results.NotFound(ValhallaErrorResponse.not_found($"版本 {name}@{version} 不存在"));

            await storage.write_string(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            var packageBinaryPath = $"{_binary_prefix}/{name}/{version}/package.nyar";
            await storage.delete(packageBinaryPath, ct);

            var sourcePath = $"{_binary_prefix}/{name}/{version}/source.tar.gz";
            if (await storage.exists(sourcePath, ct)) await storage.delete(sourcePath, ct);

            var versionMetaPath = $"{_meta_prefix}/{name}/{version}/version-meta.json";
            if (await storage.exists(versionMetaPath, ct)) await storage.delete(versionMetaPath, ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.purge,
                package_name = name,
                version = version,
                actor = "admin",
                actor_role = "admin",
                details = new Dictionary<string, string>
                {
                    ["action"] = "delete-version",
                    ["version"] = version
                }
            }, ct);

            return Results.Json(new
            {
                name,
                version,
                deletedAt = DateTime.UtcNow,
                remainingVersions = manifest.versions.Count
            });
        });

        #endregion

        #region 发布者转移

        app.MapPost("/api/packages/{name}/transfer", async (
            string name,
            [FromBody] JsonDocument body,
            CancellationToken ct) =>
        {
            var manifestPath = $"{_manifest_prefix}/{name}/manifest.json";
            var content = await storage.read_string(manifestPath, ct);
            if (content is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 不存在"));

            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
            if (manifest is null) return Results.NotFound(ValhallaErrorResponse.not_found($"包 {name} 的 manifest 已损坏"));

            if (!body.RootElement.TryGetProperty("newPublisher", out var newPubElement)
                || string.IsNullOrWhiteSpace(newPubElement.get_string()))
                return Results.BadRequest(ValhallaErrorResponse.validation_error("缺少目标发布者公钥指纹"));

            var oldPublisher = manifest.publisher;
            var newPublisher = newPubElement.get_string()!;

            manifest.publisher = newPublisher;
            manifest.incarnation++;

            await storage.write_string(manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.transfer_publisher,
                package_name = name,
                actor = oldPublisher,
                actor_role = "previous-publisher",
                details = new Dictionary<string, string>
                {
                    ["oldPublisher"] = oldPublisher,
                    ["newPublisher"] = newPublisher,
                    ["incarnation"] = manifest.incarnation.ToString()
                }
            }, ct);

            await write_audit(storage, new AuditEntry
            {
                timestamp = DateTime.UtcNow,
                operation = AuditOperation.transfer_publisher,
                package_name = name,
                actor = newPublisher,
                actor_role = "new-publisher",
                details = new Dictionary<string, string>
                {
                    ["oldPublisher"] = oldPublisher,
                    ["newPublisher"] = newPublisher,
                    ["incarnation"] = manifest.incarnation.ToString()
                }
            }, ct);

            return Results.Json(new
            {
                name,
                manifest.incarnation,
                oldPublisher,
                newPublisher,
                transferredAt = DateTime.UtcNow
            });
        });

        #endregion

        #region 统计

        app.MapGet("/api/stats", async (CancellationToken ct) =>
        {
            var allKeys = await storage.list("", ct);

            var packageCount = 0;
            var versionCount = 0;
            long totalSize = 0;

            foreach (var key in allKeys)
                if (key.StartsWith($"{_manifest_prefix}/") && key.EndsWith("/manifest.json"))
                {
                    packageCount++;
                    var content = await storage.read_string(key, ct);
                    if (content is not null)
                        try
                        {
                            var manifest = JsonSerializer.Deserialize<PackageManifest>(content);
                            if (manifest is not null)
                            {
                                versionCount += manifest.versions.Count;
                                totalSize += manifest.versions.Values.Sum(v => v.package_size);
                            }
                        }
                        catch
                        {
                            // 跳过损坏的 manifest
                        }
                }

            return Results.Json(new
            {
                packages = packageCount,
                versions = versionCount,
                totalSize,
                updatedAt = DateTime.UtcNow
            });
        });

        #endregion
    }

    #region 私有辅助

    /// <summary>
    ///     写入审计日志（追加到 JSONL 文件）
    /// </summary>
    private static async Task write_audit(
        IStorage storage,
        AuditEntry entry,
        CancellationToken ct)
    {
        var auditPath = $"{_audit_prefix}/{entry.package_name}/audit.jsonl";
        var existing = await storage.read_string(auditPath, ct);
        var line = JsonSerializer.Serialize(entry);

        var newContent = existing is null
            ? line + "\n"
            : existing + line + "\n";

        await storage.write_string(auditPath, newContent, ct);
    }

    #endregion

    #region 存储路径常量

    private const string _manifest_prefix = "packages";
    private const string _binary_prefix = "binaries";
    private const string _audit_prefix = "audit";
    private const string _meta_prefix = "meta";
    private const string _orgs_prefix = "orgs";

    #endregion
}