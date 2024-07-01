using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using OpenF1.Data;

namespace OpenF1.Console;

public static partial class CommandHandler
{
    public static async Task Root(bool isApiEnabled, string dataDirectory, bool isVerbose)
    {
        var builder = GetBuilder(isApiEnabled, dataDirectory, isVerbose);

        builder
            .Services.AddSingleton<ConsoleLoop>()
            .AddSingleton<State>()
            .AddInputHandlers()
            .AddDisplays()
            .AddLiveTiming(builder.Configuration)
            .AddSingleton<INotifyHandler, NotifyHandler>()
            .AddSingleton<ConsoleLoop>()
            .AddHostedService(sp => sp.GetRequiredService<ConsoleLoop>());

        var options = builder.Configuration.Get<LiveTimingOptions>() ?? new();

        if (options.ApiEnabled)
        {
            builder.WebHost.UseKestrel(opt => opt.ListenAnyIP(0xF1F1)); // listens on 61937

            builder
                .Services.AddRouting()
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(c =>
                {
                    c.CustomSchemaIds(type =>
                        type.FullName!.Replace("OpenF1.Data.", string.Empty)
                            .Replace("+", string.Empty)
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

        if (options.ApiEnabled)
        {
            app.UseSwagger().UseSwaggerUI();

            app.MapSwagger();

            app.MapTimingEndpoints();
        }

        await app.RunAsync();
    }
}
