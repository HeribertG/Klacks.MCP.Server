using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Klacks.Docs;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.MCP.Server;

public class MCPServerService : BackgroundService
{
    private readonly ILogger<MCPServerService> _logger;
    private readonly KlacksApiClient _apiClient;

    public MCPServerService(ILogger<MCPServerService> logger, KlacksApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
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
                    var request = JsonSerializer.Deserialize<MCPRequest>(line);
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

    private async Task<MCPResponse> ProcessRequestAsync(MCPRequest request)
    {
        _logger.LogInformation("Processing MCP request: {Method}", request.Method);

        try
        {
            return request.Method switch
            {
                "initialize" => await HandleInitializeAsync(request),
                "tools/list" => await HandleToolsListAsync(request),
                "tools/call" => await HandleToolCallAsync(request),
                "resources/list" => await HandleResourcesListAsync(request),
                "resources/read" => await HandleResourceReadAsync(request),
                _ => new MCPResponse
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

    private async Task<MCPResponse> HandleInitializeAsync(MCPRequest request)
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

    private async Task<MCPResponse> HandleToolsListAsync(MCPRequest request)
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
                    Properties = new Dictionary<string, object>
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
                    Properties = new Dictionary<string, object>
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
                    Properties = new Dictionary<string, object>
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
                    Properties = new Dictionary<string, object>()
                }
            },
            new
            {
                Name = "validate_calendar_rule",
                Description = "Validiert eine Feiertagsregel und berechnet das resultierende Datum. Unterstützt feste Daten (MM/DD), Oster-bezogene (EASTER+XX) und SubRules (SA+2;SU+1)",
                InputSchema = new
                {
                    Type = "object",
                    Properties = new Dictionary<string, object>
                    {
                        ["rule"] = new { type = "string", description = "Die Regel (z.B. '01/01', 'EASTER+39', '11/22+00+TH')" },
                        ["subRule"] = new { type = "string", description = "Optionale SubRule für Wochenend-Verschiebung (z.B. 'SA+2;SU+1')" },
                        ["year"] = new { type = "integer", description = "Jahr für die Berechnung (Standard: aktuelles Jahr)" }
                    },
                    Required = new[] { "rule" }
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

    private async Task<MCPResponse> HandleToolCallAsync(MCPRequest request)
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

    private async Task<MCPResponse> HandleResourcesListAsync(MCPRequest request)
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
            },
            new
            {
                Uri = "klacks://docs/general",
                Name = "Allgemeine Hilfe",
                Description = "Übersicht über Klacks-Funktionen und Navigation",
                MimeType = "text/markdown"
            },
            new
            {
                Uri = "klacks://docs/clients",
                Name = "Mitarbeiter-Dokumentation",
                Description = "Hilfe zur Mitarbeiterverwaltung, Import und Verträgen",
                MimeType = "text/markdown"
            },
            new
            {
                Uri = "klacks://docs/shifts",
                Name = "Schichtplanung-Dokumentation",
                Description = "Hilfe zur Schichtplanung, Vorlagen und Regeln",
                MimeType = "text/markdown"
            },
            new
            {
                Uri = "klacks://docs/identity-providers",
                Name = "Identity Provider Dokumentation",
                Description = "Hilfe zu LDAP, Active Directory und OAuth2 Konfiguration",
                MimeType = "text/markdown"
            },
            new
            {
                Uri = "klacks://docs/macros",
                Name = "Makro-Dokumentation",
                Description = "BASIC-ähnliche Skriptsprache für Berechnungen (Zuschläge, Stunden, etc.)",
                MimeType = "text/markdown"
            },
            new
            {
                Uri = "klacks://docs/calendar-rules",
                Name = "Ewigkeitskalender Feiertagsregeln",
                Description = "Regelformate für die automatische Berechnung von Feiertagen",
                MimeType = "text/markdown"
            },
            new
            {
                Uri = "klacks://docs/ai-system",
                Name = "AI-System Dokumentation",
                Description = "Soul, Memory & Guidelines - Persönlichkeit und Wissen des KI-Assistenten",
                MimeType = "text/markdown"
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

    private async Task<MCPResponse> HandleResourceReadAsync(MCPRequest request)
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

        var (content, mimeType) = await ReadResourceAsync(request.Params.Uri);

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
                        MimeType = mimeType,
                        Text = content
                    }
                }
            }
        };
    }

    private async Task<string> ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        _logger.LogInformation("Executing tool: {ToolName}", toolName);

        return toolName switch
        {
            "create_client" => await CreateClientAsync(arguments),
            "search_clients" => await SearchClientsAsync(arguments),
            "create_contract" => await CreateContractAsync(arguments),
            "get_system_info" => await GetSystemInfoAsync(),
            "validate_calendar_rule" => await ValidateCalendarRuleAsync(arguments),
            _ => $"Unknown tool: {toolName}"
        };
    }

    private async Task<(string Content, string MimeType)> ReadResourceAsync(string uri)
    {
        _logger.LogInformation("Reading resource: {Uri}", uri);

        if (uri.StartsWith("klacks://docs/"))
        {
            var docName = uri.Replace("klacks://docs/", "");
            var content = await ReadEmbeddedDocAsync(docName);
            return (content, "text/markdown");
        }

        var jsonContent = uri switch
        {
            "klacks://clients" => await GetClientsResourceAsync(),
            "klacks://system/status" => await GetSystemStatusAsync(),
            "klacks://contracts" => await GetContractsResourceAsync(),
            _ => $"Unknown resource: {uri}"
        };

        return (jsonContent, "application/json");
    }

    private async Task<string> ReadEmbeddedDocAsync(string docName)
    {
        if (!DocsReader.DocExists(docName))
        {
            _logger.LogWarning("Documentation not found: {DocName}", docName);
            var availableDocs = string.Join("\n- ", DocsReader.GetAvailableDocs().Keys);
            return $"# Dokumentation nicht gefunden\n\nDie Dokumentation '{docName}' wurde nicht gefunden.\n\nVerfügbare Dokumentationen:\n- {availableDocs}";
        }

        return await DocsReader.ReadDocAsync(docName);
    }

    private async Task<string> CreateClientAsync(JsonElement arguments)
    {
        var firstName = arguments.GetProperty("firstName").GetString()!;
        var lastName = arguments.GetProperty("lastName").GetString()!;
        var email = arguments.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        var canton = arguments.TryGetProperty("canton", out var cantonProp) ? cantonProp.GetString() : null;

        _logger.LogInformation("Creating client via API: {FirstName} {LastName}", firstName, lastName);

        try
        {
            return await _apiClient.CreateClientAsync(firstName, lastName, email, canton);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create client via API");
            return $"Error creating employee: {ex.Message}";
        }
    }

    private async Task<string> SearchClientsAsync(JsonElement arguments)
    {
        var searchTerm = arguments.GetProperty("searchTerm").GetString()!;
        var canton = arguments.TryGetProperty("canton", out var cantonProp) ? cantonProp.GetString() : null;
        var limit = arguments.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 10;

        _logger.LogInformation("Searching clients via API: {SearchTerm}", searchTerm);

        try
        {
            return await _apiClient.SearchClientsAsync(searchTerm, canton, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search clients via API");
            return $"Error searching employees: {ex.Message}";
        }
    }

    private async Task<string> CreateContractAsync(JsonElement arguments)
    {
        var clientId = arguments.GetProperty("clientId").GetString()!;
        var contractType = arguments.GetProperty("contractType").GetString()!;
        var canton = arguments.GetProperty("canton").GetString()!;

        _logger.LogInformation("Creating contract via API: {ContractType} for client {ClientId}", contractType, clientId);

        try
        {
            return await _apiClient.CreateContractAsync(clientId, contractType, canton);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create contract via API");
            return $"Error creating contract: {ex.Message}";
        }
    }

    private async Task<string> GetSystemInfoAsync()
    {
        try
        {
            return await _apiClient.GetSystemInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system info via API");
            return JsonSerializer.Serialize(new
            {
                System = "Klacks Planning System",
                Status = "degraded",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow.ToString("O")
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private async Task<string> ValidateCalendarRuleAsync(JsonElement arguments)
    {
        var rule = arguments.GetProperty("rule").GetString();
        var subRule = arguments.TryGetProperty("subRule", out var subRuleProp) ? subRuleProp.GetString() : null;
        var year = arguments.TryGetProperty("year", out var yearProp) ? yearProp.GetInt32() : DateTime.Now.Year;

        _logger.LogInformation("Validating calendar rule: {Rule}, SubRule: {SubRule}, Year: {Year}", rule, subRule, year);

        if (string.IsNullOrWhiteSpace(rule))
        {
            return JsonSerializer.Serialize(new
            {
                IsValid = false,
                ErrorMessage = "Rule cannot be empty"
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        try
        {
            var calculator = new HolidaysListCalculator();
            calculator.CurrentYear = year;

            var testRule = new Klacks.Api.Domain.Models.Settings.CalendarRule
            {
                Rule = rule,
                SubRule = subRule ?? string.Empty,
                IsMandatory = true
            };

            calculator.Add(testRule);
            calculator.ComputeHolidays();

            if (calculator.HolidayList.Count > 0)
            {
                var holiday = calculator.HolidayList[0];
                return JsonSerializer.Serialize(new
                {
                    IsValid = true,
                    Year = year,
                    Rule = rule,
                    SubRule = subRule,
                    CalculatedDate = holiday.CurrentDate.ToString("yyyy-MM-dd"),
                    FormattedDate = holiday.FormatDate,
                    DayOfWeek = holiday.CurrentDate.DayOfWeek.ToString()
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    IsValid = false,
                    ErrorMessage = "Rule did not produce a valid date"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating calendar rule: {Rule}", rule);
            return JsonSerializer.Serialize(new
            {
                IsValid = false,
                ErrorMessage = $"Invalid rule format: {ex.Message}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private async Task<string> GetClientsResourceAsync()
    {
        try
        {
            return await _apiClient.GetClientsListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch clients resource via API");
            return JsonSerializer.Serialize(new
            {
                Error = $"Failed to fetch clients: {ex.Message}",
                LastUpdated = DateTime.UtcNow.ToString("O")
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private async Task<string> GetSystemStatusAsync()
    {
        try
        {
            return await _apiClient.GetSystemStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch system status via API");
            return JsonSerializer.Serialize(new
            {
                Status = "degraded",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Error = ex.Message,
                Health = new { Database = "unknown", API = "unreachable", MCP = "healthy" }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private async Task<string> GetContractsResourceAsync()
    {
        return JsonSerializer.Serialize(new
        {
            Message = "Contract listing requires client-specific API call. Use search_clients first, then create_contract.",
            Hint = "Contract resources are accessed through the client management API.",
            LastUpdated = DateTime.UtcNow.ToString("O")
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
