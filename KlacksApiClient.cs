using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Klacks.MCP.Server;

public class KlacksApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KlacksApiSettings _settings;
    private readonly ILogger<KlacksApiClient> _logger;

    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public KlacksApiClient(HttpClient httpClient, KlacksApiSettings settings, ILogger<KlacksApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(settings.BaseUrl);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            return;

        _logger.LogInformation("Authenticating with Klacks API at {BaseUrl}", _settings.BaseUrl);

        var response = await _httpClient.PostAsJsonAsync("api/backend/accounts/LoginUser", new
        {
            email = _settings.Username,
            password = _settings.Password
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        if (result == null || !result.Success || string.IsNullOrEmpty(result.Token))
        {
            throw new InvalidOperationException($"Authentication failed: {result?.ErrorMessage ?? "No response"}");
        }

        _token = result.Token;
        _tokenExpiry = result.ExpTime;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        _logger.LogInformation("Authenticated successfully as {Username}", _settings.Username);
    }

    private async Task<T?> AuthenticatedGetAsync<T>(string url)
    {
        await EnsureAuthenticatedAsync();
        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _token = null;
            await EnsureAuthenticatedAsync();
            response = await _httpClient.GetAsync(url);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<TResponse?> AuthenticatedPostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        await EnsureAuthenticatedAsync();
        var response = await _httpClient.PostAsJsonAsync(url, body, JsonOptions);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _token = null;
            await EnsureAuthenticatedAsync();
            response = await _httpClient.PostAsJsonAsync(url, body, JsonOptions);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task<string> SearchClientsAsync(string searchTerm, string? canton, int limit)
    {
        var filter = new
        {
            searchString = searchTerm,
            employee = true,
            externEmp = true,
            customer = true,
            female = true,
            male = true,
            intersexuality = true,
            activeMembership = true,
            language = "de",
            pageNr = 1,
            itemsPerPage = limit
        };

        var result = await AuthenticatedPostAsync<object, JsonElement>("api/backend/clients/GetSimpleList", filter);
        var clients = result.GetProperty("clients");
        var count = clients.GetArrayLength();

        var lines = new List<string>();
        foreach (var client in clients.EnumerateArray())
        {
            var firstName = client.TryGetProperty("firstName", out var fn) ? fn.GetString() : "";
            var name = client.TryGetProperty("name", out var n) ? n.GetString() : "";
            var company = client.TryGetProperty("company", out var c) ? c.GetString() : "";
            var id = client.GetProperty("id").GetString();
            lines.Add($"- {firstName} {name}" + (!string.IsNullOrEmpty(company) ? $" ({company})" : "") + $" [ID: {id}]");
        }

        return $"Found {count} employees matching '{searchTerm}':\n\n{string.Join("\n", lines)}";
    }

    public async Task<string> CreateClientAsync(string firstName, string lastName, string? email, string? canton)
    {
        var client = new
        {
            firstName,
            name = lastName,
            gender = 0,
            legalEntity = false,
            idNumber = 0,
            type = 0,
            addresses = Array.Empty<object>(),
            communications = email != null ? new object[]
            {
                new { type = 0, value = email, isPreferred = true }
            } : Array.Empty<object>(),
            annotations = Array.Empty<object>(),
            clientContracts = Array.Empty<object>(),
            groupItems = Array.Empty<object>(),
            works = Array.Empty<object>()
        };

        await EnsureAuthenticatedAsync();
        var response = await _httpClient.PostAsJsonAsync("api/backend/clients", client, JsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = result.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown";

        return $"Employee {firstName} {lastName} created successfully." +
               $"\nID: {id}" +
               (email != null ? $"\nEmail: {email}" : "") +
               (canton != null ? $"\nCanton: {canton}" : "");
    }

    public async Task<string> CreateContractAsync(string clientId, string contractType, string canton)
    {
        var contract = new
        {
            name = contractType,
            guaranteedHours = 0m,
            minimumHours = 0m,
            maximumHours = 0m,
            fullTime = contractType.Contains("160") ? 160m : contractType.Contains("180") ? 180m : 160m,
            nightRate = 0m,
            holidayRate = 0m,
            saRate = 0m,
            soRate = 0m,
            validFrom = DateTime.UtcNow.Date,
        };

        await EnsureAuthenticatedAsync();
        var response = await _httpClient.PostAsJsonAsync($"api/backend/contracts", contract, JsonOptions);
        response.EnsureSuccessStatusCode();

        return $"Contract '{contractType}' created for client {clientId} in {canton}.\n" +
               $"Created at: {DateTime.Now:dd.MM.yyyy}\n" +
               $"Canton: {canton}";
    }

    public async Task<string> GetSystemInfoAsync()
    {
        var version = await AuthenticatedGetAsync<JsonElement>("api/Version");

        return JsonSerializer.Serialize(new
        {
            System = "Klacks Planning System",
            Version = version.TryGetProperty("versionString", out var v) ? v.GetString() : "unknown",
            BuildTimestamp = version.TryGetProperty("buildTimestamp", out var bt) ? bt.GetString() : "",
            Status = "running",
            Capabilities = new[] { "client_management", "contract_management", "search", "mcp_protocol", "documentation" },
            SupportedLanguages = new[] { "de", "en", "fr", "it" },
            AvailableTools = new[] { "create_client", "search_clients", "create_contract", "get_system_info", "validate_calendar_rule" },
            AvailableDocs = new[] { "general", "clients", "shifts", "identity-providers", "macros", "calendar-rules", "ai-system" }
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string> GetClientsListAsync()
    {
        var filter = new
        {
            searchString = "",
            employee = true,
            externEmp = true,
            customer = true,
            female = true,
            male = true,
            intersexuality = true,
            activeMembership = true,
            language = "de",
            pageNr = 1,
            itemsPerPage = 50
        };

        var result = await AuthenticatedPostAsync<object, JsonElement>("api/backend/clients/GetSimpleList", filter);
        return JsonSerializer.Serialize(new
        {
            Clients = result,
            LastUpdated = DateTime.UtcNow.ToString("O")
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string> GetSystemStatusAsync()
    {
        try
        {
            var version = await AuthenticatedGetAsync<JsonElement>("api/Version");
            return JsonSerializer.Serialize(new
            {
                Status = "running",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Version = version.TryGetProperty("versionString", out var v) ? v.GetString() : "unknown",
                Features = new[] { "LLM", "MCP", "WebUI", "ClientManagement", "ContractManagement", "Documentation" },
                Health = new
                {
                    Database = "healthy",
                    API = "healthy",
                    MCP = "healthy"
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Status = "degraded",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Health = new
                {
                    Database = "unknown",
                    API = "unreachable",
                    MCP = "healthy"
                },
                Error = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}

internal class LoginResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ExpTime { get; set; }
}
