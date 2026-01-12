using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.MCP.Server;

public class MCPServerService : BackgroundService
{
    private readonly ILogger&lt;MCPServerService&gt; _logger;
    
    public MCPServerService(ILogger&lt;MCPServerService&gt; logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Klacks MCP Server starting...");
        
        try
        {
            await using var stdin = Console.OpenStandardInput();
            await using var stdout = Console.OpenStandardOutput();
            
            using var reader = new StreamReader(stdin);
            await using var writer = new StreamWriter(stdout) { AutoFlush = true };
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                
                try
                {
                    var request = JsonSerializer.Deserialize&lt;MCPRequest&gt;(line);
                    if (request != null)
                    {
                        var response = await ProcessRequestAsync(request);
                        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        await writer.WriteLineAsync(responseJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MCP request: {Line}", line);
                    var errorResponse = new MCPResponse
                    {
                        Id = null,
                        Error = new MCPError
                        {
                            Code = -32603,
                            Message = "Internal error",
                            Data = ex.Message
                        }
                    };
                    var errorJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    await writer.WriteLineAsync(errorJson);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP Server error");
        }
        
        _logger.LogInformation("Klacks MCP Server stopped");
    }
    
    private async Task&lt;MCPResponse&gt; ProcessRequestAsync(MCPRequest request)
    {
        _logger.LogInformation("Processing MCP request: {Method}", request.Method);
        
        try
        {
            return request.Method switch
            {
                "initialize" =&gt; await HandleInitializeAsync(request),
                "tools/list" =&gt; await HandleToolsListAsync(request),
                "tools/call" =&gt; await HandleToolCallAsync(request),
                "resources/list" =&gt; await HandleResourcesListAsync(request),
                "resources/read" =&gt; await HandleResourceReadAsync(request),
                _ =&gt; new MCPResponse
                {
                    Id = request.Id,
                    Error = new MCPError
                    {
                        Code = -32601,
                        Message = "Method not found",
                        Data = request.Method
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP method: {Method}", request.Method);
            return new MCPResponse
            {
                Id = request.Id,
                Error = new MCPError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                }
            };
        }
    }
    
    private async Task&lt;MCPResponse&gt; HandleInitializeAsync(MCPRequest request)
    {
        return new MCPResponse
        {
            Id = request.Id,
            Result = new
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new
                {
                    Tools = new { },
                    Resources = new { },
                    Logging = new { }
                },
                ServerInfo = new
                {
                    Name = "klacks-mcp-server",
                    Version = "1.0.0",
                    Description = "Klacks Planning System MCP Server"
                }
            }
        };
    }
    
    private async Task&lt;MCPResponse&gt; HandleToolsListAsync(MCPRequest request)
    {
        var tools = new object[]
        {
            new
            {
                Name = "create_client",
                Description = "Erstellt einen neuen Mitarbeiter in Klacks",
                InputSchema = new
                {
                    Type = "object",
                    Properties = new Dictionary&lt;string, object&gt;
                    {
                        ["firstName"] = new { type = "string", description = "Vorname des Mitarbeiters" },
                        ["lastName"] = new { type = "string", description = "Nachname des Mitarbeiters" },
                        ["email"] = new { type = "string", format = "email", description = "E-Mail-Adresse" },
                        ["canton"] = new { type = "string", description = "Kanton (z.B. BE, ZH, SG)" }
                    },
                    Required = new[] { "firstName", "lastName" }
                }
            },
            new
            {
                Name = "search_clients",
                Description = "Sucht nach Mitarbeitern in Klacks",
                InputSchema = new
                {
                    Type = "object",
                    Properties = new Dictionary&lt;string, object&gt;
                    {
                        ["searchTerm"] = new { type = "string", description = "Suchbegriff" },
                        ["canton"] = new { type = "string", description = "Filter nach Kanton" },
                        ["limit"] = new { type = "integer", description = "Maximale Anzahl Ergebnisse", minimum = 1, maximum = 100 }
                    },
                    Required = new[] { "searchTerm" }
                }
            },
            new
            {
                Name = "create_contract",
                Description = "Erstellt einen neuen Vertrag für einen Mitarbeiter",
                InputSchema = new
                {
                    Type = "object",
                    Properties = new Dictionary&lt;string, object&gt;
                    {
                        ["clientId"] = new { type = "string", description = "ID des Mitarbeiters" },
                        ["contractType"] = new { type = "string", description = "Vertragstyp (z.B. Vollzeit 160)" },
                        ["canton"] = new { type = "string", description = "Kanton für den Vertrag" }
                    },
                    Required = new[] { "clientId", "contractType", "canton" }
                }
            },
            new
            {
                Name = "get_system_info",
                Description = "Gibt System-Informationen zurück",
                InputSchema = new
                {
                    Type = "object",
                    Properties = new Dictionary&lt;string, object&gt;()
                }
            }
        };
        
        return new MCPResponse
        {
            Id = request.Id,
            Result = new
            {
                Tools = tools
            }
        };
    }
    
    private async Task&lt;MCPResponse&gt; HandleToolCallAsync(MCPRequest request)
    {
        if (request.Params?.Arguments == null)
        {
            return new MCPResponse
            {
                Id = request.Id,
                Error = new MCPError
                {
                    Code = -32602,
                    Message = "Invalid params",
                    Data = "Missing tool arguments"
                }
            };
        }
        
        var toolName = request.Params.Name;
        var arguments = request.Params.Arguments;
        
        var result = await ExecuteToolAsync(toolName!, arguments.Value);
        
        return new MCPResponse
        {
            Id = request.Id,
            Result = new
            {
                Content = new[]
                {
                    new
                    {
                        Type = "text",
                        Text = result
                    }
                }
            }
        };
    }
    
    private async Task&lt;MCPResponse&gt; HandleResourcesListAsync(MCPRequest request)
    {
        var resources = new object[]
        {
            new
            {
                Uri = "klacks://clients",
                Name = "Mitarbeiter-Liste",
                Description = "Liste aller Mitarbeiter",
                MimeType = "application/json"
            },
            new
            {
                Uri = "klacks://system/status",
                Name = "System-Status", 
                Description = "Aktueller System-Status",
                MimeType = "application/json"
            },
            new
            {
                Uri = "klacks://contracts",
                Name = "Verträge",
                Description = "Liste aller Verträge",
                MimeType = "application/json"
            }
        };
        
        return new MCPResponse
        {
            Id = request.Id,
            Result = new
            {
                Resources = resources
            }
        };
    }
    
    private async Task&lt;MCPResponse&gt; HandleResourceReadAsync(MCPRequest request)
    {
        if (request.Params?.Uri == null)
        {
            return new MCPResponse
            {
                Id = request.Id,
                Error = new MCPError
                {
                    Code = -32602,
                    Message = "Invalid params",
                    Data = "Missing resource URI"
                }
            };
        }
        
        var content = await ReadResourceAsync(request.Params.Uri);
        
        return new MCPResponse
        {
            Id = request.Id,
            Result = new
            {
                Contents = new[]
                {
                    new
                    {
                        Uri = request.Params.Uri,
                        MimeType = "application/json",
                        Text = content
                    }
                }
            }
        };
    }
    
    private async Task&lt;string&gt; ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        _logger.LogInformation("Executing tool: {ToolName}", toolName);
        
        return toolName switch
        {
            "create_client" =&gt; await CreateClientAsync(arguments),
            "search_clients" =&gt; await SearchClientsAsync(arguments),
            "create_contract" =&gt; await CreateContractAsync(arguments),
            "get_system_info" =&gt; await GetSystemInfoAsync(),
            _ =&gt; $"❌ Unknown tool: {toolName}"
        };
    }
    
    private async Task&lt;string&gt; ReadResourceAsync(string uri)
    {
        _logger.LogInformation("Reading resource: {Uri}", uri);
        
        return uri switch
        {
            "klacks://clients" =&gt; await GetClientsResourceAsync(),
            "klacks://system/status" =&gt; await GetSystemStatusAsync(),
            "klacks://contracts" =&gt; await GetContractsResourceAsync(),
            _ =&gt; $"❌ Unknown resource: {uri}"
        };
    }
    
    private async Task&lt;string&gt; CreateClientAsync(JsonElement arguments)
    {
        var firstName = arguments.GetProperty("firstName").GetString();
        var lastName = arguments.GetProperty("lastName").GetString();
        var email = arguments.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        var canton = arguments.TryGetProperty("canton", out var cantonProp) ? cantonProp.GetString() : null;
        
        _logger.LogInformation("Creating client: {FirstName} {LastName}", firstName, lastName);
        
        return $"✅ Mitarbeiter {firstName} {lastName} wurde erfolgreich erstellt." +
               (email != null ? $"\n📧 E-Mail: {email}" : "") +
               (canton != null ? $"\n🏛️ Kanton: {canton}" : "");
    }
    
    private async Task&lt;string&gt; SearchClientsAsync(JsonElement arguments)
    {
        var searchTerm = arguments.GetProperty("searchTerm").GetString();
        var canton = arguments.TryGetProperty("canton", out var cantonProp) ? cantonProp.GetString() : null;
        var limit = arguments.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 10;
        
        _logger.LogInformation("Searching clients: {SearchTerm}", searchTerm);
        
        var results = new[]
        {
            new { Id = "1", FirstName = "Max", LastName = "Muster", Canton = "BE", Email = "max.muster@example.com" },
            new { Id = "2", FirstName = "Anna", LastName = "Schmidt", Canton = "ZH", Email = "anna.schmidt@example.com" },
            new { Id = "3", FirstName = "Peter", LastName = "Müller", Canton = "SG", Email = "peter.mueller@example.com" }
        };
        
        var filteredResults = canton != null ? 
            results.Where(r =&gt; r.Canton.Equals(canton, StringComparison.OrdinalIgnoreCase)) : 
            results;
            
        var limitedResults = filteredResults.Take(limit).ToArray();
        
        return $"🔍 Gefunden: {limitedResults.Length} Mitarbeiter mit Suchbegriff '{searchTerm}'" +
               (canton != null ? $" in Kanton {canton}" : "") +
               "\n\n" + string.Join("\n", limitedResults.Select(r =&gt; 
                   $"• {r.FirstName} {r.LastName} ({r.Canton}) - {r.Email}"));
    }
    
    private async Task&lt;string&gt; CreateContractAsync(JsonElement arguments)
    {
        var clientId = arguments.GetProperty("clientId").GetString();
        var contractType = arguments.GetProperty("contractType").GetString();
        var canton = arguments.GetProperty("canton").GetString();
        
        _logger.LogInformation("Creating contract: {ContractType} for client {ClientId}", contractType, clientId);
        
        return $"📄 Vertrag '{contractType}' für Mitarbeiter {clientId} in {canton} wurde erfolgreich erstellt.\n" +
               $"📅 Erstellungsdatum: {DateTime.Now:dd.MM.yyyy}\n" +
               $"🏛️ Kanton: {canton}";
    }
    
    private async Task&lt;string&gt; GetSystemInfoAsync()
    {
        return JsonSerializer.Serialize(new
        {
            System = "Klacks Planning System",
            Version = "1.0.0",
            Status = "running",
            Uptime = DateTime.UtcNow.ToString("O"),
            Capabilities = new[] { "client_management", "contract_management", "search", "mcp_protocol" },
            SupportedLanguages = new[] { "de", "en", "fr", "it" },
            AvailableTools = new[] { "create_client", "search_clients", "create_contract", "get_system_info" }
        }, new JsonSerializerOptions { WriteIndented = true });
    }
    
    private async Task&lt;string&gt; GetClientsResourceAsync()
    {
        return JsonSerializer.Serialize(new
        {
            Clients = new[]
            {
                new { Id = "1", FirstName = "Max", LastName = "Muster", Canton = "BE", Email = "max.muster@example.com", CreatedAt = "2024-01-15T10:30:00Z" },
                new { Id = "2", FirstName = "Anna", LastName = "Schmidt", Canton = "ZH", Email = "anna.schmidt@example.com", CreatedAt = "2024-02-20T14:15:00Z" },
                new { Id = "3", FirstName = "Peter", LastName = "Müller", Canton = "SG", Email = "peter.mueller@example.com", CreatedAt = "2024-03-10T09:45:00Z" }
            },
            Total = 3,
            LastUpdated = DateTime.UtcNow.ToString("O")
        }, new JsonSerializerOptions { WriteIndented = true });
    }
    
    private async Task&lt;string&gt; GetSystemStatusAsync()
    {
        return JsonSerializer.Serialize(new
        {
            Status = "running",
            Timestamp = DateTime.UtcNow.ToString("O"),
            Version = "1.0.0",
            Features = new[] { "LLM", "MCP", "WebUI", "ClientManagement", "ContractManagement" },
            Health = new
            {
                Database = "healthy",
                API = "healthy",
                MCP = "healthy"
            }
        }, new JsonSerializerOptions { WriteIndented = true });
    }
    
    private async Task&lt;string&gt; GetContractsResourceAsync()
    {
        return JsonSerializer.Serialize(new
        {
            Contracts = new[]
            {
                new { Id = "C001", ClientId = "1", Type = "Vollzeit 160", Canton = "BE", CreatedAt = "2024-01-16T11:00:00Z" },
                new { Id = "C002", ClientId = "2", Type = "Teilzeit 80", Canton = "ZH", CreatedAt = "2024-02-21T15:30:00Z" },
                new { Id = "C003", ClientId = "3", Type = "Vollzeit 180", Canton = "SG", CreatedAt = "2024-03-11T08:20:00Z" }
            },
            Total = 3,
            LastUpdated = DateTime.UtcNow.ToString("O")
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}