using Nyar.Dialect.Agent.Nodes;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Agent;

/// <summary>
///     Agent 方言的 OA 接口定义。
///     该接口声明智能体建模、规划、协作与推理相关操作。
/// </summary>
[Dialect("agent")]
public interface IAgent<T>
{
    /// <summary>
    ///     智能体定义。
    /// </summary>
    [Operator("agent")]
    Term<T> Agent(string agentId, IReadOnlyList<string> capabilities, Term<T> beliefs, Term<T> goals);

    /// <summary>
    ///     智能体集合。
    /// </summary>
    [Operator("agent_set")]
    Term<T> AgentSet(IReadOnlyList<Term<T>> agents, AgentRelation relation);

    /// <summary>
    ///     信念状态。
    /// </summary>
    [Operator("belief_state")]
    Term<T> BeliefState(IReadOnlyDictionary<string, Term<T>> facts);

    /// <summary>
    ///     目标定义。
    /// </summary>
    [Operator("goal")]
    Term<T> Goal(Term<T> targetState, GoalPriority priority, bool isMaintenance);

    /// <summary>
    ///     动作定义。
    /// </summary>
    [Operator("action_def")]
    Term<T> ActionDef(string actionName, IReadOnlyList<Term<T>> preconditions, IReadOnlyList<Term<T>> effects,
        Term<T> cost);

    /// <summary>
    ///     规划结果。
    /// </summary>
    [Operator("plan")]
    Term<T> Plan(IReadOnlyList<Term<T>> actions, IReadOnlyList<Term<T>> orderingConstraints);

    /// <summary>
    ///     规划请求。
    /// </summary>
    [Operator("plan_request")]
    Term<T> PlanRequest(Term<T> initialState, Term<T> goals, IReadOnlyList<Term<T>> availableActions);

    /// <summary>
    ///     HTN 任务。
    /// </summary>
    [Operator("htn_task")]
    Term<T> HtnTask(string taskName, IReadOnlyList<Term<T>> methods);

    /// <summary>
    ///     HTN 方法。
    /// </summary>
    [Operator("htn_method")]
    Term<T> HtnMethod(string methodName, Term<T> preconditions, IReadOnlyList<Term<T>> subtasks);

    /// <summary>
    ///     发送消息。
    /// </summary>
    [Operator("send_message")]
    Term<T> SendMessage(string senderId, string receiverId, string messageType, Term<T> payload);

    /// <summary>
    ///     接收消息。
    /// </summary>
    [Operator("receive_message")]
    Term<T> ReceiveMessage(string receiverId, string? messagePattern, Term<T> handler);

    /// <summary>
    ///     广播消息。
    /// </summary>
    [Operator("broadcast")]
    Term<T> Broadcast(string senderId, string messageType, Term<T> payload);

    /// <summary>
    ///     承诺约束。
    /// </summary>
    [Operator("commitment")]
    Term<T> Commitment(string agentId, Term<T> obligation, Term<T>? deadline);

    /// <summary>
    ///     协商过程。
    /// </summary>
    [Operator("negotiation")]
    Term<T> Negotiation(IReadOnlyList<Term<T>> participants, Term<T> subject, NegotiationProtocol protocol);

    /// <summary>
    ///     观测结果。
    /// </summary>
    [Operator("observation")]
    Term<T> Observation(string agentId, Term<T> sensorInput, float confidence);

    /// <summary>
    ///     信念更新。
    /// </summary>
    [Operator("belief_update")]
    Term<T> BeliefUpdate(Term<T> currentBeliefs, Term<T> observation, Term<T> updateFunction);

    /// <summary>
    ///     意图形成。
    /// </summary>
    [Operator("intention_formation")]
    Term<T> IntentionFormation(Term<T> goals, Term<T> beliefs, IReadOnlyList<Term<T>> availablePlans);
}