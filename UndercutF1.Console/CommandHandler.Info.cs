using Spectre.Console;
using Spectre.Console.Advanced;
using UndercutF1.Console.Graphics;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task GetInfo(
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
            dataDirectory: dataDirectory,
            logDirectory: logDirectory,
            isVerbose: isVerbose,
            notifyEnabled: notifyEnabled,
            preferFfmpeg: preferFfmpeg,
            preventDisplaySleep: preventDisplaySleep,
            forceGraphicsProtocol: forceGraphicsProtocol
        );

        builder.Services.AddSingleton<State>().AddDisplays().AddSingleton<TerminalInfoProvider>();

        var app = builder.Build();

        try
        {
            Terminal.EnableRawMode();

            var infoDisplay = app.Services.GetRequiredService<InfoDisplay>();
            var content = await infoDisplay.GetContentAsync();

            if (content is Panel panel)
            {
                panel.Collapse().NoBorder();
            }

            var output = AnsiConsole.Console.ToAnsi(content).Replace(Environment.NewLine, "\r\n");
            await Terminal.OutAsync(output);
        }
        finally
        {
            Terminal.DisableRawMode();
        }
    }
}
