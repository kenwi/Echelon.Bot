using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Echelon.Bot.Interfaces;

namespace Echelon.Bot.Services;

public abstract class BaseHttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    protected BaseHttpService(
        HttpClient httpClient,
        ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public abstract string Endpoint { get; }

    public async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending POST request to {Endpoint} with data: {Data}", endpoint, json);

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Successfully sent POST request to {Endpoint}", endpoint);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending POST request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> GetJsonAsync<T>(string endpoint)
    {
        try
        {
            _logger.LogInformation("Sending GET request to {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);

            _logger.LogInformation("Successfully received and deserialized response from {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending GET request to {Endpoint}", endpoint);
            throw;
        }
    }
} 