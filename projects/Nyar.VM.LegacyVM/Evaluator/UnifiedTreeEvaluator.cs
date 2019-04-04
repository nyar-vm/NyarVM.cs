using Nyar.VM.LegacyVM.Algebra;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     统一 AST 求值器基类，增强 AstEvaluatorBase 以支持 LanguageRuntimeConfig。
///     所有语言特定的 AST 求值器继承此类，共享内置函数注册和通用基础设施。
/// </summary>
/// <typeparam name="TAstNode">AST 节点类型</typeparam>
public abstract class UnifiedTreeEvaluator<TAstNode> : AstEvaluatorBase<TAstNode>
{
    /// <summary>
    ///     语言运行时配置
    /// </summary>
    protected readonly LanguageRuntimeConfig _config;

    /// <summary>
    ///     创建统一 AST 求值器
    /// </summary>
    /// <param name="config">语言运行时配置</param>
    /// <param name="env">初始变量环境</param>
    protected UnifiedTreeEvaluator(LanguageRuntimeConfig config, Dictionary<string, object> env) : base(env)
    {
        _config = config;
        register_builtin_functions();
    }

    /// <summary>
    ///     注册配置中的内置函数到变量环境
    /// </summary>
    private void register_builtin_functions()
    {
        foreach (var (name, impl) in _config.builtin_functions)
        {
            _core.set_var(name, new BuiltinFunction(name, impl));
        }
    }
}