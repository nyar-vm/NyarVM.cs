using System.Collections.Concurrent;

namespace Std.DL.Engram;

/// <summary>
///     内存印迹存储 —— 支持模型版本管理、分支、回滚、
///     元数据追踪的实验存储后端
/// </summary>
public sealed class InMemoryEngramStore : IEngramStore
{
    private readonly object _branchLock = new();
    private readonly Dictionary<string, List<string>> _branchVersionIds = new();
    private readonly ConcurrentDictionary<string, EngramBranch> _branches = new();
    private readonly ConcurrentDictionary<string, EngramData> _data = new();
    private readonly ConcurrentDictionary<string, EngramVersion> _versions = new();

    /// <summary>
    ///     创建内存印迹存储，默认创建 main 分支
    /// </summary>
    public InMemoryEngramStore()
    {
        var mainBranch = new EngramBranch
        {
            Id = "main",
            Name = "main",
            ParentId = "",
            CreatedAt = DateTime.UtcNow
        };
        _branches["main"] = mainBranch;
    }

    /// <summary>
    ///     获取存储的版本总数
    /// </summary>
    public int VersionCount => _versions.Count;

    /// <summary>
    ///     获取分支数量
    /// </summary>
    public int BranchCount => _branches.Count;

    /// <summary>
    ///     获取数据条目数量
    /// </summary>
    public int DataCount => _data.Count;

    /// <summary>
    ///     获取印迹数据
    /// </summary>
    public Task<EngramData?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        _data.TryGetValue(id, out var data);
        return Task.FromResult(data);
    }

    /// <summary>
    ///     保存印迹数据
    /// </summary>
    public Task<string> SaveAsync(EngramData data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(data.Id))
            data = new EngramData
            {
                Id = Guid.NewGuid().ToString("N"),
                BranchId = data.BranchId,
                ModelData = data.ModelData,
                LoRAConfig = data.LoRAConfig,
                Metadata = data.Metadata,
                CreatedAt = DateTime.UtcNow
            };

        _data[data.Id] = data;

        var version = new EngramVersion
        {
            Id = data.Id,
            BranchId = data.BranchId,
            ParentVersionId = GetBranchHead(GetBranchName(data.BranchId))?.Id ?? "",
            CreatedAt = data.CreatedAt,
            Message = data.Metadata.GetValueOrDefault("message", "")
        };
        _versions[version.Id] = version;

        RecordBranchVersion(version.BranchId, version.Id);

        return Task.FromResult(data.Id);
    }

    /// <summary>
    ///     列出印迹版本
    /// </summary>
    public IAsyncEnumerable<EngramVersion> ListAsync(CancellationToken cancellationToken = default)
    {
        return _versions.Values.ToAsyncEnumerable();
    }

    /// <summary>
    ///     导入印迹
    /// </summary>
    public Task ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     列出指定分支的版本
    /// </summary>
    /// <param name="branchId">分支 ID</param>
    public IAsyncEnumerable<EngramVersion> ListBranchVersionsAsync(string branchId)
    {
        lock (_branchLock)
        {
            if (_branchVersionIds.TryGetValue(branchId, out var ids))
                return ids.Select(id => _versions[id]).ToAsyncEnumerable();
        }

        return AsyncEnumerable.Empty<EngramVersion>();
    }

    /// <summary>
    ///     创建分支
    /// </summary>
    /// <param name="name">分支名称</param>
    /// <param name="fromBranchId">源分支 ID</param>
    /// <returns>新分支</returns>
    public EngramBranch CreateBranch(string name, string fromBranchId = "main")
    {
        var branch = new EngramBranch
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = name,
            ParentId = fromBranchId,
            CreatedAt = DateTime.UtcNow
        };

        _branches[branch.Id] = branch;

        lock (_branchLock)
        {
            _branchVersionIds[branch.Id] = [];
        }

        return branch;
    }

    /// <summary>
    ///     获取分支
    /// </summary>
    /// <param name="branchId">分支 ID</param>
    public EngramBranch? GetBranch(string branchId)
    {
        _branches.TryGetValue(branchId, out var branch);
        return branch;
    }

    /// <summary>
    ///     通过名称查找分支
    /// </summary>
    public EngramBranch? FindBranchByName(string name)
    {
        return _branches.Values.FirstOrDefault(b =>
            string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     列出所有分支
    /// </summary>
    public IReadOnlyList<EngramBranch> ListBranches()
    {
        return [.. _branches.Values];
    }

    /// <summary>
    ///     获取分支最新版本数据
    /// </summary>
    public EngramData? GetBranchHead(string branchId)
    {
        lock (_branchLock)
        {
            if (_branchVersionIds.TryGetValue(branchId, out var ids) && ids.Count > 0)
            {
                var lastId = ids[^1];
                _data.TryGetValue(lastId, out var data);
                return data;
            }
        }

        return null;
    }

    /// <summary>
    ///     回滚到指定版本（在当前分支创建新版本，内容复制自目标版本）
    /// </summary>
    /// <param name="targetVersionId">目标版本 ID</param>
    /// <param name="targetBranchId">目标分支 ID</param>
    /// <param name="message">回滚消息</param>
    /// <returns>新版本 ID</returns>
    public async Task<string> RollbackAsync(
        string targetVersionId, string targetBranchId, string message = "")
    {
        if (!_versions.TryGetValue(targetVersionId, out var targetVersion))
            throw new InvalidOperationException($"版本不存在：{targetVersionId}");

        if (!_data.TryGetValue(targetVersionId, out var targetData))
            throw new InvalidOperationException($"版本数据不存在：{targetVersionId}");

        var rollbackData = new EngramData
        {
            Id = Guid.NewGuid().ToString("N"),
            BranchId = targetBranchId,
            ModelData = targetData.ModelData,
            LoRAConfig = targetData.LoRAConfig,
            Metadata = new Dictionary<string, string>(targetData.Metadata)
            {
                ["message"] = string.IsNullOrEmpty(message)
                    ? $"回滚到版本 {targetVersionId[..8]}"
                    : message,
                ["rollback_source"] = targetVersionId
            },
            CreatedAt = DateTime.UtcNow
        };

        return await SaveAsync(rollbackData);
    }

    /// <summary>
    ///     清空所有存储
    /// </summary>
    public void Reset()
    {
        _data.Clear();
        _versions.Clear();
        _branches.Clear();

        lock (_branchLock)
        {
            _branchVersionIds.Clear();
        }

        var mainBranch = new EngramBranch
        {
            Id = "main",
            Name = "main",
            ParentId = "",
            CreatedAt = DateTime.UtcNow
        };
        _branches["main"] = mainBranch;
    }

    private string GetBranchName(string branchId)
    {
        return _branches.TryGetValue(branchId, out var branch) ? branch.Name : branchId;
    }

    private void RecordBranchVersion(string branchId, string versionId)
    {
        lock (_branchLock)
        {
            if (!_branchVersionIds.TryGetValue(branchId, out var ids))
            {
                ids = [];
                _branchVersionIds[branchId] = ids;
            }

            ids.Add(versionId);
        }
    }
}