using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RetailFixIT.Infrastructure.AI;

/// <summary>
/// AI provider using Azure OpenAI Service (chat completions REST API).
/// Activated by setting AI:Provider = "AzureOpenAI" in appsettings.
/// Required config: AI:AzureOpenAI:Endpoint, AI:AzureOpenAI:ApiKey, AI:AzureOpenAI:DeploymentName
/// </summary>
public class AzureOpenAIProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AzureOpenAIProvider> _logger;

    private const string ApiVersion = "2025-01-01-preview";

    public AzureOpenAIProvider(IHttpClientFactory httpFactory, IConfiguration config, ILogger<AzureOpenAIProvider> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<AIRecommendationResult> GenerateRecommendationAsync(AIJobContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var deploymentName = _config["AI:AzureOpenAI:DeploymentName"] ?? "gpt-4o";

        try
        {
            var endpoint = _config["AI:AzureOpenAI:Endpoint"]?.TrimEnd('/');
            var apiKey = _config["AI:AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Azure OpenAI not configured (missing Endpoint or ApiKey)");
                return new AIRecommendationResult(
                    Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                    ProviderName: "AzureOpenAI", ModelVersion: deploymentName, LatencyMs: 0,
                    ErrorMessage: "Azure OpenAI endpoint or API key not configured.");
            }

            var url = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version={ApiVersion}";
            var prompt = BuildPrompt(context);

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a field service dispatch AI. You respond only with valid JSON." },
                    new { role = "user", content = prompt }
                },
                max_completion_tokens = 1024
            };

            var json = JsonSerializer.Serialize(requestBody);
            var client = _httpFactory.CreateClient("AzureOpenAI");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("api-key", apiKey);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(httpRequest, cts.Token);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Azure OpenAI returned HTTP {Status} for job {JobId}: {Error}",
                    (int)response.StatusCode, context.JobId, errorBody[..Math.Min(300, errorBody.Length)]);
                return new AIRecommendationResult(
                    Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                    ProviderName: "AzureOpenAI", ModelVersion: deploymentName, LatencyMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: $"Azure OpenAI error {(int)response.StatusCode}: {GetErrorMessage(errorBody)}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return ParseResponse(responseJson, context, deploymentName, (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Azure OpenAI recommendation failed for job {JobId}", context.JobId);
            return new AIRecommendationResult(
                Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                ProviderName: "AzureOpenAI", ModelVersion: deploymentName, LatencyMs: (int)sw.ElapsedMilliseconds,
                ErrorMessage: ex.Message);
        }
    }

    private string BuildPrompt(AIJobContext context)
    {
        var vendorList = string.Join("\n", context.AvailableVendors.Select(v =>
            $"- ID: {v.VendorId}, Name: {v.Name}, Area: {v.ServiceArea ?? "Any"}, Skills: {v.Specializations ?? "General"}, Rating: {v.Rating?.ToString("F1") ?? "N/A"}, Available slots: {v.AvailableCapacity}"));

        return $$"""
Analyze the following field service job and recommend the best vendors from the list.

Job Details:
- Title: {{context.Title}}
- Description: {{context.Description}}
- Service Type: {{context.ServiceType}}
- Location: {{context.ServiceAddress}}

Available Vendors:
{{vendorList}}

Respond ONLY with valid JSON in this exact format:
{
  "jobSummary": "2-3 sentence summary of the job and what needs to be done",
  "recommendedVendorIds": ["vendor-guid-1", "vendor-guid-2"],
  "reasoning": "Brief explanation of why these vendors were chosen based on skills, location, and availability"
}

Select 1-3 best-fit vendors based on service type match, service area, rating, and availability.
If no vendors match the service type, return an empty array for recommendedVendorIds.
""";
    }

    private AIRecommendationResult ParseResponse(string json, AIJobContext context, string deploymentName, int latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end < 0) throw new FormatException("No JSON object found in response");

            var cleanJson = text[start..(end + 1)];
            using var resultDoc = JsonDocument.Parse(cleanJson);
            var root = resultDoc.RootElement;

            var vendorIds = root.TryGetProperty("recommendedVendorIds", out var idsEl)
                ? idsEl.EnumerateArray()
                    .Select(e => Guid.TryParse(e.GetString(), out var g) ? (Guid?)g : null)
                    .Where(g => g.HasValue && context.AvailableVendors.Any(v => v.VendorId == g.Value))
                    .Select(g => g!.Value)
                    .ToList()
                : new List<Guid>();

            return new AIRecommendationResult(
                Success: true,
                RecommendedVendorIds: vendorIds,
                Reasoning: root.TryGetProperty("reasoning", out var r) ? r.GetString() : null,
                JobSummary: root.TryGetProperty("jobSummary", out var s) ? s.GetString() : null,
                ProviderName: "AzureOpenAI",
                ModelVersion: deploymentName,
                LatencyMs: latencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Azure OpenAI response");
            return new AIRecommendationResult(
                Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                ProviderName: "AzureOpenAI", ModelVersion: deploymentName, LatencyMs: latencyMs,
                ErrorMessage: "Failed to parse AI response: " + ex.Message);
        }
    }

    private static string GetErrorMessage(string errorJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorJson);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
                return msg.GetString() ?? "Unknown error";
        }
        catch { }
        return errorJson[..Math.Min(150, errorJson.Length)];
    }
}
