using System.Text.Json;
using System.Text.Json.Serialization;
using InMemLogger;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using OpenF1.Console;
using OpenF1.Data;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateEmptyBuilder(new() { ApplicationName = "openf1-console" });

var switchMapping = new Dictionary<string, string>
{
    ["-v"] = "Verbose",
    ["--verbose"] = "Verbose",
};

builder
    .Configuration.AddJsonFile(Path.Join(AppContext.BaseDirectory, "appsettings.json"))
    .AddJsonFile(Path.Join(LiveTimingOptions.BaseDirectory, "config.json"), optional: true)
    .AddEnvironmentVariables("OPENF1_")
    .AddCommandLine(args, switchMapping)
    .Build();

var (inMemoryLogLevel, fileLogLevel) = builder.Configuration.GetSection("Verbose").Get<bool>()
    ? (LogLevel.Trace, LogEventLevel.Verbose)
    : (LogLevel.Information, LogEventLevel.Information);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(fileLogLevel)
    .WriteTo.File(
        path: Path.Join(LiveTimingOptions.BaseDirectory, "logs/openf1-console.log"),
        rollOnFileSizeLimit: true,
        rollingInterval: RollingInterval.Hour,
        retainedFileCountLimit: 5
    )
    .CreateLogger();

builder
    .Services.AddOptions()
    .AddLogging(configure =>
        configure.ClearProviders().SetMinimumLevel(inMemoryLogLevel).AddInMemory().AddSerilog()
    )
    .AddSingleton<ConsoleLoop>()
    .AddSingleton<State>()
    .AddInputHandlers()
    .AddDisplays()
    .AddLiveTiming(builder.Configuration)
    .AddSingleton<INotifyHandler, NotifyHandler>()
    .AddSingleton<ConsoleLoop>()
    .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>());

var enableApi = builder.Configuration.GetSection("ApiEnabled").Get<bool>();

if (enableApi)
{
    builder.WebHost.UseKestrel(opt => opt.ListenAnyIP(0xF1F1)); // listens on 61937

    builder
        .Services.AddRouting()
        .AddEndpointsApiExplorer()
        .AddSwaggerGen(c =>
        {
            c.CustomSchemaIds(type =>
                type.FullName!.Replace("OpenF1.Data.", string.Empty).Replace("+", string.Empty)
            );
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Open F1 API", Version = "v1" });
        });
}
else
{
    builder.WebHost.UseServer(new NullServer());
}

builder.Services.Configure<JsonOptions>(x =>
{
    x.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    x.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

if (enableApi)
{
    app.UseSwagger().UseSwaggerUI();

    app.MapSwagger();

    app.MapTimingEndpoints();
}

await app.RunAsync();
