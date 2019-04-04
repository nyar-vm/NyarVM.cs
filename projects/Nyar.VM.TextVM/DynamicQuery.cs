namespace Nyar.VM.TextVM;

/// <summary>
/// 动态查询。代表用户在运行时输入的查询，通过 TextVM 运行时环境执行。
/// 利用按键间隙进行投机编译。
/// </summary>
public class DynamicQuery
{
    private readonly String _pattern;
    private readonly TextEncoding _encoding;
    private readonly TvmOperation _operation;
    private readonly Int64 _token;

    /// <summary>
    /// 获取模式字符串。
    /// </summary>
    public String Pattern => _pattern;

    /// <summary>
    /// 获取目标编码。
    /// </summary>
    public TextEncoding Encoding => _encoding;

    /// <summary>
    /// 获取操作类型。
    /// </summary>
    public TvmOperation Operation => _operation;

    /// <summary>
    /// 获取版本令牌，用于撤销过时的后台编译任务。
    /// </summary>
    public Int64 Token => _token;

    /// <summary>
    /// 创建动态查询。仅做词法检查，不编译。
    /// </summary>
    /// <param name="pattern">模式字符串。</param>
    /// <param name="encoding">目标编码。</param>
    /// <param name="operation">操作类型。</param>
    /// <param name="token">版本令牌。</param>
    public DynamicQuery(String pattern, TextEncoding encoding, TvmOperation operation, Int64 token)
    {
        _pattern = pattern;
        _encoding = encoding;
        _operation = operation;
        _token = token;
    }

    /// <summary>
    /// 在指定 VM 上执行查询。
    /// </summary>
    /// <param name="vm">TextVM 运行时环境实例。</param>
    /// <param name="input">待搜索的输入字节切片。</param>
    /// <returns>所有匹配结果的枚举。</returns>
    public IEnumerable<Match> Execute(TextVM vm, ReadOnlySpan<Byte> input)
    {
        return vm.Execute(this, input);
    }
}
