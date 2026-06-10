using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace CodingDroplets.MultiAgentStarter;

/// <summary>
/// A minimal multi-agent workflow you can build on. Two agents are wired as a graph: a Triage
/// agent assesses an incoming ticket, then a Drafter agent writes the customer reply from that
/// assessment. A single edge connects them, so Triage's output flows into Drafter as its input.
///
/// ----------------------------------------------------------------------------------------
///  THIS IS A STARTER. The shape is what matters - swap in your own agents and edges.
///  To extend:
///    - Change the instructions below, or add more ChatClientAgents.
///    - Add edges in RunAsync to build sequential, branching, or fan-out graphs.
///    - Give an agent tools (AIFunctionFactory.Create) for it to take real actions.
///  The full reference solution (sequential + concurrent + handoff + group chat) is the
///  course's Patreon module - this skeleton is the on-ramp.
/// ----------------------------------------------------------------------------------------
/// </summary>
public sealed class AgentWorkflow
{
    private const string TriageInstructions =
        """
        You are a support triage analyst for an online electronics store.
        Read the customer's ticket and produce a brief INTERNAL assessment (not sent to the customer):
        - Category (e.g. Order, Returns, Billing, Technical).
        - Urgency: Low, Medium, High, or Urgent.
        - The customer's main issue in one sentence.
        - Whether it needs human escalation (yes/no) and why.
        Keep it to a few short lines.
        """;

    private const string DrafterInstructions =
        """
        You are a customer support writer for an online electronics store. You receive an internal
        triage assessment. Using it, write a short, warm, professional reply addressed to the customer
        that acknowledges their issue and explains the next step. Do NOT mention the internal
        assessment, categories, or urgency labels. If escalation is needed, tell the customer a
        specialist will follow up shortly.
        """;

    private readonly AIAgent _triageAgent;
    private readonly AIAgent _drafterAgent;

    public AgentWorkflow(IChatClient chatClient)
    {
        _triageAgent = new ChatClientAgent(chatClient, name: "Triage", instructions: TriageInstructions);
        _drafterAgent = new ChatClientAgent(chatClient, name: "Drafter", instructions: DrafterInstructions);
    }

    public async Task<(string Triage, string Draft)> RunAsync(string ticket, CancellationToken cancellationToken = default)
    {
        // Build the graph: Triage -> Drafter. The Triage agent is the entry point.
        Workflow workflow = new WorkflowBuilder(_triageAgent)
            .AddEdge(_triageAgent, _drafterAgent)
            .Build();

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
            workflow, new ChatMessage(ChatRole.User, ticket), cancellationToken: cancellationToken);

        // Agents cache their input and only run when they receive a TurnToken.
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        // Collect each agent's streamed output, keyed by the executor that produced it.
        Dictionary<string, StringBuilder> outputs = [];
        List<string> orderSeen = [];

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync().WithCancellation(cancellationToken))
        {
            if (workflowEvent is AgentResponseUpdateEvent update)
            {
                if (!outputs.TryGetValue(update.ExecutorId, out StringBuilder? sb))
                {
                    sb = new StringBuilder();
                    outputs[update.ExecutorId] = sb;
                    orderSeen.Add(update.ExecutorId);
                }

                sb.Append(update.Update.Text);
            }
        }

        // In a sequential workflow the first executor to speak is Triage, the last is Drafter.
        string triage = orderSeen.Count > 0 ? outputs[orderSeen[0]].ToString() : string.Empty;
        string draft = orderSeen.Count > 0 ? outputs[orderSeen[^1]].ToString() : string.Empty;

        return (triage.Trim(), draft.Trim());
    }
}
