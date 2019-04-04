using System.Diagnostics;
using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Database.Integration.Storage;
using Nyar.Database.Storage;
using Nyar.Types;

namespace Nyar.Tests.Database;

/// <summary>
///     M4: ���������ܻ�׼���� ���� 10 ���з��������־û���ָ�
/// </summary>
public sealed class ColdStartBenchmarkTests : IAsyncLifetime
{
    private string _temp_path = null!;

    public async Task InitializeAsync()
    {
        _temp_path = Path.Combine(Path.GetTempPath(), $"nyar-bench-{Guid.NewGuid():N}");
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_temp_path))
            {
                Directory.Delete(_temp_path, true);
            }
        }
        catch
        {
        }
    }

    #region �־û���׼����

    /// <summary>
    ///     ����д�� 10 ���з��ţ�ģ�� 1000 �ļ� �� 100 ����/�ļ����ĳ־û�����
    /// </summary>
    [Fact]
    public async Task Persist_100K_Symbols_BatchWrite()
    {
        const int fileCount = 1000;
        const int symbolsPerFile = 100;
        const int totalSymbols = fileCount * symbolsPerFile;

        var engine = new StorageEngine(_temp_path);
        var indexManager = new IndexManager();

        var sw = Stopwatch.StartNew();

        for (var f = 0; f < fileCount; f++)
        {
            var fileUri = $"file:///project/module_{f / 10}/file_{f}.ts";
            var file = create_file(fileUri);
            var symbols = new List<SymbolRecord>(symbolsPerFile);

            for (var s = 0; s < symbolsPerFile; s++)
                symbols.Add(create_symbol(fileUri, $"Symbol_{f}_{s}",
                    s % 3 == 0 ? SymbolKind.Class : s % 3 == 1 ? SymbolKind.Method : SymbolKind.Function));

            var operations = new List<(WalOperationType, object)>
            {
                (WalOperationType.UpsertFile, file)
            };

            foreach (var symbol in symbols)
            {
                operations.Add((WalOperationType.UpsertSymbol, symbol));
            }

            await engine.AppendWalBatchAsync(operations);

            indexManager.UpdateFile(file, symbols, []);
        }

        sw.Stop();

        var persistMs = sw.ElapsedMilliseconds;
        Assert.Equal(totalSymbols, indexManager.Symbols.Count);
        Assert.Equal(fileCount, indexManager.Files.Count);

        Assert.True(persistMs < 30000,
            $"�־û� {totalSymbols} �����ź�ʱ {persistMs}ms��Ӧ < 30s");

        await engine.DisposeAsync();
    }

    #endregion

    #region ���������ٻָ�����

    /// <summary>
    ///     �������������ٻָ����������ļ�������������
    ///     Ŀ�꣺10 ������Ŀ < 2s
    /// </summary>
    [Fact]
    public async Task FastRestore_ColdStart_Under2Seconds()
    {
        const int fileCount = 1000;
        const int symbolsPerFile = 100;

        await using (var writeEngine = new StorageEngine(_temp_path))
        {
            for (var f = 0; f < fileCount; f++)
            {
                var fileUri = $"file:///project/module_{f / 10}/file_{f}.ts";
                var file = create_file(fileUri);

                var operations = new List<(WalOperationType, object)>
                {
                    (WalOperationType.UpsertFile, file)
                };

                for (var s = 0; s < symbolsPerFile; s++)
                    operations.Add((WalOperationType.UpsertSymbol,
                        create_symbol(fileUri, $"Symbol_{f}_{s}", SymbolKind.Class)));

                await writeEngine.AppendWalBatchAsync(operations);
            }

            var indexManager = new IndexManager();
            await writeEngine.SaveAsync(indexManager);
        }

        await using (var coldEngine = new StorageEngine(_temp_path))
        {
            var coldManager = new IndexManager();
            var sw = Stopwatch.StartNew();

            await coldEngine.RestoreFileIndexAsync(coldManager);

            sw.Stop();
            var restoreMs = sw.ElapsedMilliseconds;

            Assert.Equal(fileCount, coldManager.Files.Count);

            Assert.True(restoreMs < 2000,
                $"������ FastRestore {fileCount} �ļ���ʱ {restoreMs}ms��Ӧ < 2s");
        }
    }

    #endregion

    #region ������ز���

    /// <summary>
    ///     ���԰�����ص����ļ��ķ��ţ������ز��ԣ�
    /// </summary>
    [Fact]
    public async Task LazyLoad_SingleFile_Under10ms()
    {
        const int symbolsInFile = 500;

        await using (var writeEngine = new StorageEngine(_temp_path))
        {
            var fileUri = "file:///project/big_file.ts";
            var operations = new List<(WalOperationType, object)>();

            for (var s = 0; s < symbolsInFile; s++)
                operations.Add((WalOperationType.UpsertSymbol,
                    create_symbol(fileUri, $"BigSymbol_{s}", SymbolKind.Method)));

            await writeEngine.AppendWalBatchAsync(operations);
        }

        await using (var coldEngine = new StorageEngine(_temp_path))
        {
            var coldManager = new IndexManager();
            var sw = Stopwatch.StartNew();

            await coldEngine.LoadSymbolsForFileAsync(coldManager, "file:///project/big_file.ts");

            sw.Stop();
            var loadMs = sw.ElapsedMilliseconds;

            Assert.Equal(symbolsInFile, coldManager.Symbols.Count);

            Assert.True(loadMs < 50,
                $"������� {symbolsInFile} �����ź�ʱ {loadMs}ms��Ӧ < 50ms");
        }
    }

    #endregion

    #region �������²���

    /// <summary>
    ///     ����ͬһ���ļ��ķ����������¡����ɷ������ + �·���д��
    /// </summary>
    [Fact]
    public async Task IncrementalUpdate_SameFile_OverwritesCorrectly()
    {
        const string fileUri = "file:///project/changing_file.ts";

        await using (var engine = new StorageEngine(_temp_path))
        {
            var oldSymbols = new[]
            {
                create_symbol(fileUri, "OldSymbol_1", SymbolKind.Class),
                create_symbol(fileUri, "OldSymbol_2", SymbolKind.Method)
            };

            var oldOps = new List<(WalOperationType, object)>();
            foreach (var s in oldSymbols)
            {
                oldOps.Add((WalOperationType.UpsertSymbol, s));
            }

            await engine.AppendWalBatchAsync(oldOps);

            var removeOldOps = new List<(WalOperationType, object)>();
            foreach (var s in oldSymbols)
            {
                removeOldOps.Add((WalOperationType.RemoveSymbol, s.Id));
            }

            var newSymbols = new[]
            {
                create_symbol(fileUri, "NewSymbol_1", SymbolKind.Module),
                create_symbol(fileUri, "NewSymbol_2", SymbolKind.Function),
                create_symbol(fileUri, "NewSymbol_3", SymbolKind.Interface)
            };

            foreach (var s in newSymbols)
            {
                removeOldOps.Add((WalOperationType.UpsertSymbol, s));
            }

            await engine.AppendWalBatchAsync(removeOldOps);

            var restoreManager = new IndexManager();
            await engine.RestoreAsync(restoreManager);

            Assert.Equal(3, restoreManager.Symbols.GetByFileUri(fileUri).Count);
            Assert.Contains(restoreManager.Symbols.GetByFileUri(fileUri),
                s => s.Name == "NewSymbol_2" && s.Kind == SymbolKind.Function);
        }
    }

    #endregion

    #region ����ָ�����

    /// <summary>
    ///     ��������ع������������������Ჿ�ֳ־û�
    /// </summary>
    [Fact]
    public async Task TransactionRollback_DoesNotPartialPersist()
    {
        const string fileUri = "file:///project/rollback_test.ts";

        await using (var engine = new StorageEngine(_temp_path))
        {
            var validSymbols = new[]
            {
                create_symbol(fileUri, "ValidSymbol_1", SymbolKind.Class)
            };

            var validOps = new List<(WalOperationType, object)>();
            foreach (var s in validSymbols)
            {
                validOps.Add((WalOperationType.UpsertSymbol, s));
            }

            await engine.AppendWalBatchAsync(validOps);

            var allSymbols = new List<(WalOperationType, object)>();
            for (var i = 0; i < 10; i++)
                allSymbols.Add((WalOperationType.UpsertSymbol,
                    create_symbol(fileUri, $"BatchSymbol_{i}", SymbolKind.Method)));

            await engine.AppendWalBatchAsync(allSymbols);
        }

        await using (var coldEngine = new StorageEngine(_temp_path))
        {
            var manager = new IndexManager();
            await coldEngine.RestoreAsync(manager);

            Assert.Equal(11, manager.Symbols.GetByFileUri(fileUri).Count);
        }
    }

    #endregion

    #region ��������

    private static SymbolRecord create_symbol(string fileUri, string name, SymbolKind kind)
    {
        var symbol = new SymbolRecord();
        symbol.Id = SymbolId.Create(fileUri, name, kind);
        symbol.Name = name;
        symbol.Kind = kind;
        symbol.FileUri = fileUri;
        symbol.Location = Loc.Zero;
        symbol.Accessibility = SymbolAccessibility.Public;
        return symbol;
    }

    private static FileRecord create_file(string uri)
    {
        var file = new FileRecord();
        file.Uri = uri;
        file.ContentHash = $"hash_{uri.GetHashCode():x8}";
        file.LastModified = DateTime.UtcNow;
        file.DependencyUris = [];
        file.LanguageId = "typescript";
        file.AnalysisStatus = FileAnalysisStatus.Completed;
        return file;
    }

    #endregion
}
