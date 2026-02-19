using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RetailFixIT.Infrastructure.AI;

public class GeminiAIProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiAIProvider> _logger;

    private const string DefaultModel = "gemini-2.0-flash";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiAIProvider(IHttpClientFactory httpFactory, IConfiguration config, ILogger<GeminiAIProvider> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<AIRecommendationResult> GenerateRecommendationAsync(AIJobContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var modelName = _config["AI:Gemini:Model"] ?? DefaultModel;

        try
        {
            var apiKey = _config["AI:Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                _logger.LogWarning("Gemini API key not configured, returning mock recommendation");
                return CreateMockResult(context, modelName, 100);
            }

            var prompt = BuildPrompt(context);
            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.3, maxOutputTokens = 1024 }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{BaseUrl}/{modelName}:generateContent?key={apiKey}";

            var client = _httpFactory.CreateClient("Gemini");

            // Single attempt with 20-second timeout — fast fail on quota/auth errors
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            var response = await client.PostAsync(url, content, cts.Token);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Gemini returned HTTP {Status} for job {JobId}: {Error}",
                    (int)response.StatusCode, context.JobId, errorBody[..Math.Min(200, errorBody.Length)]);
                return new AIRecommendationResult(
                    Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                    ProviderName: "Gemini", ModelVersion: modelName, LatencyMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: $"Gemini API error {(int)response.StatusCode}: {GetErrorMessage(errorBody)}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return ParseGeminiResponse(responseJson, context, modelName, (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Gemini AI recommendation failed for job {JobId}", context.JobId);
            return new AIRecommendationResult(
                Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                ProviderName: "Gemini", ModelVersion: modelName, LatencyMs: (int)sw.ElapsedMilliseconds,
                ErrorMessage: ex.Message);
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
        return errorJson[..Math.Min(100, errorJson.Length)];
    }

    private string BuildPrompt(AIJobContext context)
    {
        var vendorList = string.Join("\n", context.AvailableVendors.Select(v =>
            $"- ID: {v.VendorId}, Name: {v.Name}, Area: {v.ServiceArea ?? "Any"}, Skills: {v.Specializations ?? "General"}, Rating: {v.Rating?.ToString("F1") ?? "N/A"}, Available slots: {v.AvailableCapacity}"));

        return $$"""
You are a field service dispatch AI. Analyze the job and recommend the best vendors.

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
If no vendors match the service type, return an empty array.
""";
    }

    private AIRecommendationResult ParseGeminiResponse(string json, AIJobContext context, string modelName, int latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end < 0) throw new FormatException("No JSON found in response");

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
                ProviderName: "Gemini",
                ModelVersion: modelName,
                LatencyMs: latencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini response");
            return new AIRecommendationResult(
                Success: false, RecommendedVendorIds: null, Reasoning: null, JobSummary: null,
                ProviderName: "Gemini", ModelVersion: modelName, LatencyMs: latencyMs,
                ErrorMessage: "Failed to parse AI response");
        }
    }

    private static AIRecommendationResult CreateMockResult(AIJobContext context, string modelName, int latencyMs)
    {
        var vendorIds = context.AvailableVendors.Take(2).Select(v => v.VendorId).ToList();
        return new AIRecommendationResult(
            Success: true,
            RecommendedVendorIds: vendorIds,
            Reasoning: $"[MOCK] Based on the {context.ServiceType} service requirement, these vendors have matching specializations and available capacity.",
            JobSummary: $"[MOCK] Job requires {context.ServiceType} services at {context.ServiceAddress}. {context.Description}",
            ProviderName: "Gemini-Mock",
            ModelVersion: modelName,
            LatencyMs: latencyMs);
    }
}
