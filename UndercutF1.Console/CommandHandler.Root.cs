using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using UndercutF1.Console.Api;
using UndercutF1.Console.Audio;
using UndercutF1.Console.ExternalPlayerSync;
using UndercutF1.Console.Graphics;
using UndercutF1.Data;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task Root(
        bool? isApiEnabled,
        DirectoryInfo? dataDirectory,
        DirectoryInfo? logDirectory,
        bool? isVerbose,
        bool? notifyEnabled,
        bool? preferFfmpeg,
        bool? preventDisplaySleep,
        GraphicsProtocol? forceGraphicsProtocol
    )
    {
        var builder = GetBuilder(
            isApiEnabled: isApiEnabled,
            dataDirectory: dataDirectory,
            logDirectory: logDirectory,
            isVerbose: isVerbose,
            notifyEnabled: notifyEnabled,
            preferFfmpeg: preferFfmpeg,
            preventDisplaySleep: preventDisplaySleep,
            forceGraphicsProtocol: forceGraphicsProtocol
        );

        builder
            .Services.AddSingleton<ConsoleLoop>()
            .AddSingleton<State>()
            .AddInputHandlers()
            .AddDisplays()
            .AddSingleton<INotifyHandler, NotifyHandler>()
            .AddSingleton<TerminalInfoProvider>()
            .AddSingleton<AudioPlayer>()
            .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>())
            .AddHostedService(sp => sp.GetRequiredService<WebSocketSynchroniser>());

        var options = builder.Configuration.Get<ConsoleOptions>() ?? new();

        if (options.ApiEnabled)
        {
            builder.WebHost.UseKestrel(opt => opt.ListenAnyIP(0xF1F1)); // listens on 61937

            builder.Services.AddOpenApi(
                "v1",
                opts =>
                {
                    opts.CreateSchemaReferenceId = CreateSchemaReferenceId;
                    opts.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
                }
            );

            builder.Services.AddRouting().AddEndpointsApiExplorer();
        }

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(x =>
            ConfigureJsonSerializer(x.SerializerOptions)
        );

        // The Swagger UI only respects the Mvc JsonOptions, so set both even though we only need the Http.Json one for minimal APIs
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(x =>
            ConfigureJsonSerializer(x.JsonSerializerOptions)
        );

        var app = builder.Build();

        if (options.ApiEnabled)
        {
            app.MapOpenApi();

            app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/openapi/v1.json", "v1"));

            app.MapControlEndpoints().MapTimingEndpoints();
        }

        app.Logger.LogInformation(
            "{Options}",
            options with
            {
                // Redact the token from logs
                Formula1AccessToken = options.Formula1AccessToken is null
                    ? "<missing>"
                    : "<present>",
            }
        );

        Whisper.net.Logger.LogProvider.AddLogger(
            (level, msg) =>
            {
                switch (level)
                {
                    case Whisper.net.Logger.WhisperLogLevel.Error:
                        app.Logger.LogError("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    case Whisper.net.Logger.WhisperLogLevel.Warning:
                        app.Logger.LogDebug("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    case Whisper.net.Logger.WhisperLogLevel.Debug:
                        app.Logger.LogDebug("Whisper: {Message}", msg?.Trim('\n'));
                        break;
                    default:
                        app.Logger.LogDebug("Whisper {Level}: {Message}", level, msg?.Trim('\n'));
                        break;
                }
            }
        );

        await EnsureConfigFileExistsAsync(app.Logger);
        await app.RunAsync();
    }

    private static string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        var name = typeInfo.Type.FullName;

        // Don't create references for simple types like bools
        return name is null || name.StartsWith("System")
            ? null
            : name.Replace("UndercutF1.Data.", string.Empty)
                .Replace("UndercutF1.Console.Api.", string.Empty)
                .Replace("+", string.Empty);
    }

    private static void ConfigureJsonSerializer(JsonSerializerOptions options)
    {
        foreach (var converter in TimingDataSerializerContext.Pretty.Options.Converters)
        {
            options.Converters.Add(converter);
        }
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.TypeInfoResolver = JsonTypeInfoResolver.Combine(
            ConsoleSerializerContext.Pretty,
            TimingDataSerializerContext.Pretty
        );
    }
}
