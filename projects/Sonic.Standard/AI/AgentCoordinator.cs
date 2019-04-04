using System.Collections.Concurrent;

namespace Std.AI;

/// <summary>
///     智能体协调器实现，管理智能体的注册和工作流执行。
/// </summary>
public sealed class AgentCoordinator : IAgentCoordinator
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    /// <inheritdoc />
    public Task<string> register_agent(IAgent agent)
    {
        _agents[agent.id] = agent;
        return Task.FromResult(agent.id);
    }

    /// <inheritdoc />
    public Task<IAgent?> get_agent(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public async Task<WorkflowResult> execute_workflow(Workflow workflow, CancellationToken ct = default)
    {
        var results = new List<WorkflowStepResult>();
        string? lastResult = null;

        foreach (var step in workflow.steps)
        {
            if (!_agents.TryGetValue(step.agent_id, out var agent))
            {
                results.Add(new WorkflowStepResult
                {
                    agent_id = step.agent_id,
                    success = false,
                    error = $"Agent {step.agent_id} 未注册"
                });
                break;
            }

            var task = lastResult != null
                ? $"{step.task}\n\n上一步结果:\n{lastResult}"
                : step.task;

            try
            {
                var result = await agent.execute(task, ct);
                lastResult = result;
                results.Add(new WorkflowStepResult
                {
                    agent_id = step.agent_id,
                    success = true,
                    result = result
                });
            }
            catch (Exception ex)
            {
                results.Add(new WorkflowStepResult
                {
                    agent_id = step.agent_id,
                    success = false,
                    error = ex.Message
                });
                break;
            }
        }

        return new WorkflowResult
        {
            success = results.All(r => r.success),
            final_result = lastResult,
            step_results = results
        };
    }
}