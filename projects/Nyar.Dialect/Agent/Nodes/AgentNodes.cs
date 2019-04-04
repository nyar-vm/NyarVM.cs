using Nyar.IR.Intent;

namespace Nyar.Dialect.Agent.Nodes;

#region 智能体定义节点

[AlgebraNode]
public sealed partial record Agent(string agentId, IReadOnlyList<string> capabilities, Id beliefs, Id goals) : AlgebraNode;

[AlgebraNode]
public sealed partial record AgentSet(IReadOnlyList<Id> agents, AgentRelation relation) : AlgebraNode;

public enum AgentRelation
{
    Cooperative,
    Competitive,
    Mixed,
    Hierarchical
}

[AlgebraNode]
public sealed partial record BeliefState(IReadOnlyDictionary<string, Id> facts) : AlgebraNode;

[AlgebraNode]
public sealed partial record Goal(Id targetState, GoalPriority priority, bool isMaintenance) : AlgebraNode;

public enum GoalPriority
{
    Critical,
    High,
    Normal,
    Low,
    Optional
}

#endregion

#region 规划节点

[AlgebraNode]
public sealed partial record ActionDef(
    string actionName,
    IReadOnlyList<Id> preconditions,
    IReadOnlyList<Id> effects,
    Id cost) : AlgebraNode;

[AlgebraNode]
public sealed partial record Plan(IReadOnlyList<Id> actions, IReadOnlyList<Id> orderingConstraints) : AlgebraNode;

[AlgebraNode]
public sealed partial record PlanRequest(Id initialState, Id goals, IReadOnlyList<Id> availableActions) : AlgebraNode;

[AlgebraNode]
public sealed partial record HtnTask(string taskName, IReadOnlyList<Id> methods) : AlgebraNode;

[AlgebraNode]
public sealed partial record HtnMethod(string methodName, Id preconditions, IReadOnlyList<Id> subtasks) : AlgebraNode;

#endregion

#region 协调与通信节点

[AlgebraNode]
public sealed partial record SendMessage(string senderId, string receiverId, string messageType, Id payload) : AlgebraNode;

[AlgebraNode]
public sealed partial record ReceiveMessage(string receiverId, string? messagePattern, Id handler) : AlgebraNode;

[AlgebraNode]
public sealed partial record Broadcast(string senderId, string messageType, Id payload) : AlgebraNode;

[AlgebraNode]
public sealed partial record Commitment(string agentId, Id obligation, Id? deadline) : AlgebraNode;

[AlgebraNode]
public sealed partial record Negotiation(IReadOnlyList<Id> participants, Id subject, NegotiationProtocol protocol)
    : AlgebraNode;

public enum NegotiationProtocol
{
    ContractNet,
    Auction,
    Argumentation,
    Consensus
}

#endregion

#region 感知与推理节点

[AlgebraNode]
public sealed partial record Observation(string agentId, Id sensorInput, float confidence) : AlgebraNode;

[AlgebraNode]
public sealed partial record BeliefUpdate(Id currentBeliefs, Id observation, Id updateFunction) : AlgebraNode;

[AlgebraNode]
public sealed partial record IntentionFormation(Id goals, Id beliefs, IReadOnlyList<Id> availablePlans) : AlgebraNode;

#endregion