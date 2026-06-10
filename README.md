# dotnet-multi-agent-starter

A minimal **multi-agent workflow** in C# / ASP.NET Core, built on the
[Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview/). Two
agents are wired as a graph - a **Triage** agent assesses an incoming support ticket, then a
**Drafter** agent writes the customer reply from that assessment - with a **human-in-the-loop**
approval gate before anything is "sent."

It runs on a **free local model** (Ollama) out of the box, so you can clone it and watch agents
hand work to each other in a couple of minutes, no API key required.

---

## Why this exists

This is a free starter from the Coding Droplets course
[**AI-Powered APIs in .NET**](https://aiapis.codingdroplets.com) (Chapter 13, "Multi-Agent
Workflows"). It shows the *shape* of an agent workflow - agents as nodes, edges as the flow of
work between them - so you have a working skeleton to build your own on.

> **Go further:** the full reference solution wires all four workflow patterns - **sequential,
> concurrent, handoff, and group chat** - into one support system with human-in-the-loop
> throughout. It's a premium module of the course. This skeleton is the on-ramp; the
> **[course](https://aiapis.codingdroplets.com)** is the whole journey: chat, RAG, tool calling,
> agents, MCP, security, evaluation, observability, and deployment in .NET.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/) running, with a tool-capable model pulled:
  ```
  ollama pull llama3.2
  ```

No API key needed for the default local path. To use a cloud model instead, see
[Switching providers](#switching-providers).

## Quick start

```
git clone https://github.com/codingdroplets/dotnet-multi-agent-starter.git
cd dotnet-multi-agent-starter
dotnet run
```

The API listens on `http://localhost:5080`.

## Try it

Run the workflow (see `MultiAgentStarter.http` for ready-to-send requests):

```
# macOS / Linux
curl -X POST http://localhost:5080/workflow/triage \
     -H "Content-Type: application/json" \
     -d '{"message":"My order A1234 arrived with a cracked screen and I need a replacement before Friday."}'
```

```
# Windows (PowerShell) - call curl.exe and use --% so the JSON passes through verbatim
curl.exe --% -X POST http://localhost:5080/workflow/triage -H "Content-Type: application/json" -d "{\"message\":\"My order A1234 arrived with a cracked screen and I need a replacement before Friday.\"}"
```

You get back a drafted reply with status `AwaitingApproval` and an id like `DRF-1a2b3c4d`. The
draft is **not** sent until a human approves it:

```
curl -X POST http://localhost:5080/workflow/DRF-1a2b3c4d/approve
```

> On a CPU-only machine the local model is slower, so a two-agent run can take a little while.
> That's the model, not the workflow.

## Endpoints

| Method & route | Does |
|----------------|------|
| `POST /workflow/triage` | Runs Triage -> Drafter, returns a draft awaiting approval. |
| `GET /workflow/{id}` | Inspects a drafted reply. |
| `POST /workflow/{id}/approve` | The human-in-the-loop gate: marks a draft approved. |

## How it works

| File | Job |
|------|-----|
| `AgentWorkflow.cs` | The two agents and the graph that connects them. **Start here to extend.** |
| `Program.cs` | The endpoints, including the approval gate. |
| `WorkflowModels.cs` | Request/response records and the in-memory approval store. |
| `ChatClientRegistration.cs` | One provider-agnostic `IChatClient` the agents share. |

The graph is three lines:

```csharp
Workflow workflow = new WorkflowBuilder(_triageAgent)
    .AddEdge(_triageAgent, _drafterAgent)
    .Build();
```

## Make it yours

- **Change the agents:** edit the instructions in `AgentWorkflow.cs`, or add more `ChatClientAgent`s.
- **Change the graph:** add edges to build sequential, branching, or fan-out flows.
- **Give an agent tools:** use `AIFunctionFactory.Create` so an agent can take real actions (look up an order, create a ticket).

## Switching providers

Everything is coded against `IChatClient`, so the model is a config choice. To use the GitHub
Models free tier instead of Ollama, set `Ai:Provider` to `githubmodels` and provide a token:

```
# PowerShell
$env:Ai__Provider = "githubmodels"
$env:GITHUB_TOKEN = "your-token-here"
dotnet run
```

## Built with

- [`Microsoft.Agents.AI`](https://learn.microsoft.com/en-us/agent-framework/overview/) and `Microsoft.Agents.AI.Workflows` - the Microsoft Agent Framework
- [`Microsoft.Extensions.AI`](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) - the provider-agnostic `IChatClient`
- [`OllamaSharp`](https://www.nuget.org/packages/OllamaSharp) - local models via Ollama

## Learn more

- **Course:** [AI-Powered APIs in .NET](https://aiapis.codingdroplets.com) - the full guide this starter belongs to
- **Blog:** [codingdroplets.com](https://codingdroplets.com) - keeping up with the .NET AI stack
- **More free starters:** [github.com/codingdroplets](https://github.com/codingdroplets)

## License

[MIT](LICENSE) - free to use, modify, and build on.
