using Spectre.Console;
using UndercutF1.Data;

namespace UndercutF1.Console;

public static partial class CommandHandler
{
    public static async Task LoginToFormula1Account(bool? isVerbose)
    {
        var builder = GetBuilder(
            isVerbose: isVerbose,
            useConsoleLogging: isVerbose.GetValueOrDefault()
        );

        var app = builder.Build();

        await EnsureConfigFileExistsAsync(app.Logger);

        var accountService = app.Services.GetRequiredService<Formula1Account>();
        var existingPayload = accountService.Payload;

        // Allow a relogin on the day of expiry but not before
        if (existingPayload is not null && existingPayload.Expiry.Date >= DateTime.Today)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"""
                An access token is already configured in [bold]{ConsoleOptions.ConfigFilePath}[/].
                [dim]{existingPayload}[/]
                This token will expire on [bold]{existingPayload.Expiry:yyyy-MM-dd}[/], at which point you'll need to login again.
                If you'd like to log in again, please first logout using [bold]undercutf1 logout[/].
                """
            );
            return;
        }

        var preamble = $"""
            Login to your Formula 1 Account (which has any level of F1 TV subscription) to access all the Live Timing feeds and unlock all features of undercut-f1.

            An account is [bold]NOT[/] needed for undercut-f1 to function, it only unlocks the following features:
            - Driver Tracker (live position of cars on track)
            - Pit Stop times
            - Championship tables with live prediction
            - DRS active indicator on Timing Screen

            Additionally, logging in is [bold]NOT[/] required for the above features if you import data for already completed sessions, as all data feeds are available after a session is complete.

            Once logged in, your access token will be stored in [bold]{ConsoleOptions.ConfigFilePath}[/]. Your actual account credentials will not be stored anywhere.
            Simply remove the token entry from this file, or run [bold]undercutf1 logout[/] to remove usage of your token.
            """;

        AnsiConsole.MarkupLine(preamble);
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Proceed to login?", defaultValue: false))
        {
            return;
        }

        AnsiConsole.MarkupLine(
            "Opening a browser window for you to login to your Formula 1 account. Once logged in, close the browser window and return here."
        );

        AnsiConsole.WriteLine();

        var accountLogin = app.Services.GetRequiredService<AccountLogin>();
        var payload = await accountLogin.LoginAsync(
            (status) =>
            {
                AnsiConsole.WriteLine();
                switch (status)
                {
                    case AccountLogin.LoginStatus.TokenReceived:
                        AnsiConsole.MarkupLine(
                            """
                            [bold green]Received login cookie, you may now close the browser.[/]
                            """
                        );
                        break;
                    case AccountLogin.LoginStatus.Failed:
                        AnsiConsole.MarkupLine(
                            """
                            [bold red]No or invalid token received from login.[/]
                            """
                        );
                        break;
                }
            }
        );
        if (payload is not null)
        {
            AnsiConsole.MarkupLine(
                $"""
                [green]Login Successful.[/] Your access token has been saved in [bold]{ConsoleOptions.ConfigFilePath}[/].
                This token will expire on [bold]{payload.Expiry:yyyy-MM-dd}[/], at which point you'll need to login again.
                """
            );
        }
    }

    public static async Task LogoutOfFormula1Account()
    {
        AnsiConsole.MarkupLine(
            $"""
            Logging out will remove your access token stored in [bold]{ConsoleOptions.ConfigFilePath}[/].
            To log back in again in the future, simply run [bold]undercutf1 login[/].
            """
        );
        AnsiConsole.WriteLine();

        var builder = GetBuilder();

        var app = builder.Build();

        var accountLogin = app.Services.GetRequiredService<AccountLogin>();
        await accountLogin.LogoutAsync();

        AnsiConsole.MarkupLine(
            $"""
            [green]Logout successful.[/]
            """
        );
    }
}
