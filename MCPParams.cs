using System.Text.Json.Serialization;

namespace Klacks.MCP.Server;

public class MCPParams
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("arguments")]
    public System.Text.Json.JsonElement? Arguments { get; set; }
    
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}