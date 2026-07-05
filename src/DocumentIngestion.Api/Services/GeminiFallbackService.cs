using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentIngestion.Api.Models;
using Microsoft.Extensions.Configuration;

namespace DocumentIngestion.Api.Services;

public class GeminiFallbackService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public GeminiFallbackService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ExtractedMetadata?> ExtractMetadataAsync(string rawText, Stream? fileStream, string? mimeType)
    {
        if (!IsConfigured)
        {
            Console.WriteLine("[GEMINI FALLBACK] Service skipped: API Key is not configured in settings/env.");
            return null;
        }

        try
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            
            string schemaPrompt = 
                "Extract structured metadata from the provided document. " +
                "You must respond with a single, valid JSON object matching EXACTLY this schema:\n" +
                "{\n" +
                "  \"documentDate\": \"string (normalized date formatted in ISO YYYY-MM-DD format, or null if not found)\",\n" +
                "  \"referenceNumber\": \"string (document reference ID, invoice number, Aadhar card number, or null if not found)\",\n" +
                "  \"totalAmount\": 0.0 (decimal total value of the invoice/receipt, or null if not a financial receipt)\n" +
                "  \"attributes\": { \"key\": \"value\" } (dictionary/map of any other key-value metadata attributes found, e.g. Name, Gender, Address, District, PIN, Mobile, etc.)\n" +
                "}\n" +
                "Do not include any markdown format tags (like ```json or ```). Return ONLY the raw JSON text.";

            object requestPayload;

            // Multimodal fallback if text is empty but image bytes are available
            if (string.IsNullOrWhiteSpace(rawText) && fileStream != null && !string.IsNullOrEmpty(mimeType))
            {
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await fileStream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                string base64Data = Convert.ToBase64String(bytes);

                requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = schemaPrompt },
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = mimeType,
                                        data = base64Data
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };
            }
            else
            {
                // Simple text extraction from raw OCR text
                requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = $"{schemaPrompt}\n\nDocument text contents:\n{rawText}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };
            }

            string requestJson = JsonSerializer.Serialize(requestPayload);
            using var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GEMINI ERROR] HTTP call failed with code {response.StatusCode}. Response: {errorResponse}");
                return null;
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            
            // Extract generation text content from candidates[0].content.parts[0].text
            var textElement = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text");

            string innerJson = textElement.GetString() ?? string.Empty;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<ExtractedMetadata>(innerJson, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GEMINI EXCEPTION] Extraction process failed: {ex.Message}");
            return null;
        }
    }
}
