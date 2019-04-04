namespace Std.AI;

/// <summary>
///     智能体角色枚举。
/// </summary>
public enum AgentRole
{
    /// <summary>
    ///     规划师，负责分解任务。
    /// </summary>
    planner,

    /// <summary>
    ///     执行者，负责执行任务。
    /// </summary>
    executor,

    /// <summary>
    ///     编码者，负责编写代码。
    /// </summary>
    coder,

    /// <summary>
    ///     评审者，负责审查方案。
    /// </summary>
    critic,

    /// <summary>
    ///     总结者，负责提炼要点。
    /// </summary>
    summarizer,

    /// <summary>
    ///     研究者，负责调研信息。
    /// </summary>
    researcher,

    /// <summary>
    ///     写作者，负责撰写内容。
    /// </summary>
    writer,

    /// <summary>
    ///     翻译者，负责翻译内容。
    /// </summary>
    translator,

    /// <summary>
    ///     分析师，负责数据分析。
    /// </summary>
    analyst
}

/// <summary>
///     智能体间传递的消息。
/// </summary>
public sealed class AgentMessage
{
    /// <summary>
    ///     消息发送者标识。
    /// </summary>
    public string from { get; set; } = string.Empty;

    /// <summary>
    ///     消息接收者标识。
    /// </summary>
    public string to { get; set; } = string.Empty;

    /// <summary>
    ///     消息内容。
    /// </summary>
    public string content { get; set; } = string.Empty;

    /// <summary>
    ///     消息时间戳。
    /// </summary>
    public DateTime timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     智能体接口，定义智能体的基本能力。
/// </summary>
public interface IAgent
{
    /// <summary>
    ///     智能体唯一标识。
    /// </summary>
    string id { get; }

    /// <summary>
    ///     智能体角色。
    /// </summary>
    AgentRole role { get; }

    /// <summary>
    ///     执行任务。
    /// </summary>
    /// <param name="task">任务描述</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<string> execute(string task, CancellationToken ct = default);

    /// <summary>
    ///     发送消息给智能体并获取回复。
    /// </summary>
    /// <param name="message">消息实例</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>回复消息</returns>
    Task<AgentMessage> send_message(AgentMessage message, CancellationToken ct = default);
}

/// <summary>
///     智能体协调器接口，管理智能体的注册和工作流执行。
/// </summary>
public interface IAgentCoordinator
{
    /// <summary>
    ///     注册智能体。
    /// </summary>
    /// <param name="agent">智能体实例</param>
    /// <returns>智能体标识</returns>
    Task<string> register_agent(IAgent agent);

    /// <summary>
    ///     获取已注册的智能体。
    /// </summary>
    /// <param name="agentId">智能体标识</param>
    /// <returns>智能体实例，不存在返回 null</returns>
    Task<IAgent?> get_agent(string agentId);

    /// <summary>
    ///     执行工作流。
    /// </summary>
    /// <param name="workflow">工作流定义</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>工作流执行结果</returns>
    Task<WorkflowResult> execute_workflow(Workflow workflow, CancellationToken ct = default);
}

/// <summary>
///     工作流定义，由一系列步骤组成。
/// </summary>
public sealed class Workflow
{
    /// <summary>
    ///     工作流步骤列表。
    /// </summary>
    public List<WorkflowStep> steps { get; set; } = [];
}

/// <summary>
///     工作流步骤，指定由哪个智能体执行什么任务。
/// </summary>
public sealed class WorkflowStep
{
    /// <summary>
    ///     执行步骤的智能体标识。
    /// </summary>
    public string agent_id { get; set; } = string.Empty;

    /// <summary>
    ///     步骤任务描述。
    /// </summary>
    public string task { get; set; } = string.Empty;
}

/// <summary>
///     工作流执行结果。
/// </summary>
public sealed class WorkflowResult
{
    /// <summary>
    ///     工作流是否全部成功。
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     最终结果。
    /// </summary>
    public string? final_result { get; set; }

    /// <summary>
    ///     各步骤执行结果。
    /// </summary>
    public List<WorkflowStepResult> step_results { get; set; } = [];
}

/// <summary>
///     工作流步骤执行结果。
/// </summary>
public sealed class WorkflowStepResult
{
    /// <summary>
    ///     执行步骤的智能体标识。
    /// </summary>
    public string agent_id { get; set; } = string.Empty;

    /// <summary>
    ///     步骤是否成功。
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     步骤执行结果。
    /// </summary>
    public string? result { get; set; }

    /// <summary>
    ///     步骤执行错误信息。
    /// </summary>
    public string? error { get; set; }
}