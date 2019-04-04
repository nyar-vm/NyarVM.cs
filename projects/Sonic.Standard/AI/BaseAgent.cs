using Core.AI;

namespace Std.AI;

/// <summary>
///     智能体抽象基类，提供基于 <see cref="IAiService" /> 的通用执行能力。
/// </summary>
public abstract class BaseAgent : IAgent
{
    private readonly IAiService _ai_service;

    /// <summary>
    ///     初始化智能体基类。
    /// </summary>
    /// <param name="aiService">AI 服务实例</param>
    /// <param name="id">智能体标识</param>
    /// <param name="role">智能体角色</param>
    protected BaseAgent(IAiService aiService, string id, AgentRole role)
    {
        _ai_service = aiService;
        this.id = id;
        this.role = role;
    }

    /// <inheritdoc />
    public string id { get; }

    /// <inheritdoc />
    public AgentRole role { get; }

    /// <inheritdoc />
    public async Task<string> execute(string task, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = get_system_prompt() },
            new() { Role = "user", Content = task }
        };

        return await _ai_service.ChatAsync(messages, ct);
    }

    /// <inheritdoc />
    public async Task<AgentMessage> send_message(AgentMessage message, CancellationToken ct = default)
    {
        var result = await execute(message.content, ct);
        return new AgentMessage
        {
            from = id,
            to = message.from,
            content = result
        };
    }

    /// <summary>
    ///     获取智能体的系统提示词。
    /// </summary>
    /// <returns>系统提示词</returns>
    protected abstract string get_system_prompt();
}

/// <summary>
///     规划师智能体，负责分析任务需求并分解为可执行的子任务。
/// </summary>
public sealed class PlannerAgent : BaseAgent
{
    /// <summary>
    ///     初始化规划师智能体。
    /// </summary>
    /// <param name="aiService">AI 服务实例</param>
    public PlannerAgent(IAiService aiService) : base(aiService, "planner", AgentRole.planner)
    {
    }

    /// <inheritdoc />
    protected override string get_system_prompt()
    {
        return "你是一位专业的任务规划师。分析任务需求，将复杂任务分解为可执行的子任务，制定详细的执行步骤。";
    }
}

/// <summary>
///     编码者智能体，负责编写高质量代码。
/// </summary>
public sealed class CoderAgent : BaseAgent
{
    /// <summary>
    ///     初始化编码者智能体。
    /// </summary>
    /// <param name="aiService">AI 服务实例</param>
    public CoderAgent(IAiService aiService) : base(aiService, "coder", AgentRole.coder)
    {
    }

    /// <inheritdoc />
    protected override string get_system_prompt()
    {
        return "你是一位资深的软件工程师。根据需求编写高质量代码，遵循最佳实践和设计模式。";
    }
}

/// <summary>
///     评审者智能体，负责审查方案的合理性和可行性。
/// </summary>
public sealed class CriticAgent : BaseAgent
{
    /// <summary>
    ///     初始化评审者智能体。
    /// </summary>
    /// <param name="aiService">AI 服务实例</param>
    public CriticAgent(IAiService aiService) : base(aiService, "critic", AgentRole.critic)
    {
    }

    /// <inheritdoc />
    protected override string get_system_prompt()
    {
        return "你是一位严格的评审专家。审查方案的合理性和可行性，指出潜在问题，提供改进建议。";
    }
}

/// <summary>
///     总结者智能体，负责提炼关键结果和要点。
/// </summary>
public sealed class SummarizerAgent : BaseAgent
{
    /// <summary>
    ///     初始化总结者智能体。
    /// </summary>
    /// <param name="aiService">AI 服务实例</param>
    public SummarizerAgent(IAiService aiService) : base(aiService, "summarizer", AgentRole.summarizer)
    {
    }

    /// <inheritdoc />
    protected override string get_system_prompt()
    {
        return "你是一位专业的总结师。分析任务执行的全过程，提炼关键结果和要点，生成清晰简洁的总结报告。";
    }
}