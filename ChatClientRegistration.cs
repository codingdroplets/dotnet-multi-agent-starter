using System.ClientModel;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace CodingDroplets.MultiAgentStarter;

/// <summary>
/// Registers a provider-agnostic <see cref="IChatClient"/> that every agent in the workflow
/// shares. Choose the provider in configuration (Ai:Provider = "ollama" or "githubmodels");
/// the agents never know which model is behind it.
/// </summary>
public static class ChatClientRegistration
{
    public static IServiceCollection AddConfiguredChatClient(
        this IServiceCollection services, IConfiguration config)
    {
        string provider = (config["Ai:Provider"] ?? "ollama").ToLowerInvariant();

        IChatClient inner = provider switch
        {
            "ollama" => CreateOllama(config),
            "githubmodels" => CreateGitHubModels(config),
            _ => throw new InvalidOperationException(
                $"Unknown Ai:Provider '{provider}'. Use 'ollama' or 'githubmodels'.")
        };

        // UseFunctionInvocation so agents that expose tools can call them automatically.
        services.AddChatClient(inner).UseFunctionInvocation();
        return services;
    }

    private static IChatClient CreateOllama(IConfiguration config)
    {
        string endpoint = config["Ai:Ollama:Endpoint"] ?? "http://localhost:11434/";
        string model = config["Ai:Ollama:Model"] ?? "llama3.2";

        return new OllamaApiClient(new Uri(endpoint), model);
    }

    private static IChatClient CreateGitHubModels(IConfiguration config)
    {
        string endpoint = config["Ai:GitHubModels:Endpoint"] ?? "https://models.github.ai/inference";
        string model = config["Ai:GitHubModels:Model"] ?? "openai/gpt-4o-mini";

        string? token = config["Ai:GitHubModels:Token"]
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "No GitHub Models token found. Set it with user secrets or the GITHUB_TOKEN environment variable.");
        }

        OpenAIClient client = new(
            new ApiKeyCredential(token),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return client.GetChatClient(model).AsIChatClient();
    }
}
