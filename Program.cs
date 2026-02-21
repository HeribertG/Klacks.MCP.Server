using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Klacks.MCP.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var apiSettings = builder.Configuration.GetSection("KlacksApi").Get<KlacksApiSettings>() ?? new KlacksApiSettings();
builder.Services.AddSingleton(apiSettings);

builder.Services.AddHttpClient<KlacksApiClient>(client =>
{
    client.BaseAddress = new Uri(apiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services.AddSingleton<MCPServerService>();
builder.Services.AddHostedService<MCPServerService>();

var host = builder.Build();

await host.RunAsync();
