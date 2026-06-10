using CodingDroplets.MultiAgentStarter;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredChatClient(builder.Configuration);
builder.Services.AddSingleton<AgentWorkflow>();
builder.Services.AddSingleton<ApprovalStore>();

WebApplication app = builder.Build();

// POST /workflow/triage - run the two-agent workflow; returns a DRAFT awaiting human approval.
app.MapPost("/workflow/triage", async (
    WorkflowRequest request,
    AgentWorkflow workflow,
    ApprovalStore approvals,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest("Message must not be empty.");
    }

    (string triage, string draft) = await workflow.RunAsync(request.Message, cancellationToken);
    DraftedReply reply = approvals.Add(triage, draft);

    return Results.Ok(reply);
});

// POST /workflow/{id}/approve - the human-in-the-loop gate: approve a drafted reply before sending.
app.MapPost("/workflow/{id}/approve", (string id, ApprovalStore approvals) =>
{
    DraftedReply? approved = approvals.Approve(id);
    return approved is null ? Results.NotFound($"No draft with id '{id}'.") : Results.Ok(approved);
});

// GET /workflow/{id} - inspect a drafted reply.
app.MapGet("/workflow/{id}", (string id, ApprovalStore approvals) =>
{
    DraftedReply? draft = approvals.Get(id);
    return draft is null ? Results.NotFound() : Results.Ok(draft);
});

app.Run();
