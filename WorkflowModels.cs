using System.Collections.Concurrent;

namespace CodingDroplets.MultiAgentStarter;

/// <summary>The JSON body for POST /workflow/triage.</summary>
public record WorkflowRequest(string Message);

/// <summary>
/// The result of running the workflow: the internal triage assessment, the drafted customer
/// reply, and its approval status. The draft is NOT sent until a human approves it.
/// </summary>
public record DraftedReply(string Id, string Triage, string Draft, string Status);

/// <summary>
/// Holds drafted replies awaiting human approval (the human-in-the-loop gate). In a real system
/// this would be a database and a review queue.
/// </summary>
public sealed class ApprovalStore
{
    private readonly ConcurrentDictionary<string, DraftedReply> _drafts = new();

    public DraftedReply Add(string triage, string draft)
    {
        string id = $"DRF-{Guid.NewGuid().ToString("N")[..8]}";
        DraftedReply reply = new(id, triage, draft, "AwaitingApproval");
        _drafts[id] = reply;
        return reply;
    }

    public DraftedReply? Approve(string id)
    {
        if (!_drafts.TryGetValue(id, out DraftedReply? draft))
        {
            return null;
        }

        DraftedReply approved = draft with { Status = "Approved" };
        _drafts[id] = approved;
        return approved;
    }

    public DraftedReply? Get(string id) => _drafts.GetValueOrDefault(id);
}
