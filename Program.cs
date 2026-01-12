using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Klacks.MCP.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddSingleton&lt;MCPServerService&gt;();
builder.Services.AddHostedService&lt;MCPServerService&gt;();

var host = builder.Build();

await host.RunAsync();