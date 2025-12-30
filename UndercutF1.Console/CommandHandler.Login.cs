using System.Text.Json.Nodes;
using SharpWebview;
using SharpWebview.Content;
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

        var accountService = app.Services.GetRequiredService<Formula1Account>();
        var existingPayload = accountService.Payload;

        // Allow a relogin on the day of expiry but not before
        if (existingPayload is not null && existingPayload.Expiry.Date >= DateTime.Today)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"""
                An access token is already configured in [bold]{Options.ConfigFilePath}[/].
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

            Once logged in, your access token will be stored in [bold]{Options.ConfigFilePath}[/]. Your actual account credentials will not be stored anywhere.
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

        var token = LoginWithWebView();

        if (token is null)
            return;

        var authResult = accountService.CheckToken(token, out var payload);

        if (authResult != Formula1Account.AuthenticationResult.Success)
        {
            AnsiConsole.MarkupLine(
                $"""
                [red]Invalid token received from login. Please try again.
                Ensure the account you are logging in with has an active F1 TV subscription.
                Auth Result: [bold]{authResult}[/][/]

                [dim]{payload}[/]
                """
            );
            return;
        }

        await EnsureConfigFileExistsAsync(app.Logger);

        // Read in the existing config file, then write out the file including the access token
        // We read the file rather than just save the config to try and avoid changing other contents in the file
        // e.g. we might have config set by environment variables that shouldn't end up in the file
        // or the file might have keys in it that we don't read in to config, but we shouldn't remove from the file.
        var configFileJson = await ReadConfigFileAsync();
        configFileJson[nameof(Options.Formula1AccessToken)] = token;
        await File.WriteAllTextAsync(
            Options.ConfigFilePath,
            configFileJson.ToJsonString(Constants.JsonSerializerOptions)
        );

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"""
            [green]Login Successful.[/] Your access token has been saved in [bold]{Options.ConfigFilePath}[/].
            This token will expire on [bold]{payload?.Expiry:yyyy-MM-dd}[/], at which point you'll need to login again.
            """
        );
    }

    public static async Task LogoutOfFormula1Account()
    {
        AnsiConsole.MarkupLine(
            $"""
            Logging out will remove your access token stored in [bold]{Options.ConfigFilePath}[/].
            To log back in again in the future, simply run [bold]undercutf1 login[/].
            """
        );
        AnsiConsole.WriteLine();

        var configFileJson = await ReadConfigFileAsync();
        configFileJson.Remove(nameof(Options.Formula1AccessToken));

        await File.WriteAllTextAsync(
            Options.ConfigFilePath,
            configFileJson.ToJsonString(Constants.JsonSerializerOptions)
        );

        AnsiConsole.MarkupLine(
            $"""
            [green]Logout successful.[/]
            """
        );
    }

    private static string? LoginWithWebView()
    {
        if (OperatingSystem.IsLinux())
        {
            // Workaround for Nvidia driver issues and Wayland
            // See https://github.com/JustAman62/undercut-f1/issues/144
            Environment.SetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
        }

        using var webView = new Webview(debug: false, interceptExternalLinks: false);

        var cookie = default(string);

        webView
            .SetTitle("Login to Formula 1")
            .SetSize(1024, 768, WebviewHint.None)
            .SetSize(400, 400, WebviewHint.Min)
            .Bind(
                "sendLoginCookie",
                (id, req) =>
                {
                    // The params are sent as an array of strings
                    // We know theres only one element, so strip the array start and end chars to get the element.
                    cookie = req[2..^2];

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine(
                        """
                        [bold green]Received login cookie, you may now close the browser.[/]
                        """
                    );
                    AnsiConsole.WriteLine();
                }
            )
            .InitScript(GetInitScript())
            .Navigate(new UrlContent("https://account.formula1.com/#/en/login"))
            // Run() blocks until the WebView is closed.
            .Run();

        if (cookie is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Failed to retrieve login session cookie, please try again[/]"
            );
            return null;
        }

        return cookie;
    }

    private static string GetInitScript()
    {
        var cookieHandling = """
            function getCookie(name) {
                return (name = (document.cookie + ';').match(new RegExp(name + '=.*;'))) && name[0].split(/=|;/)[1];
            }

            var previousCookie = "";
            setInterval(() => {
                let cookie = getCookie('login-session');
                if (cookie && previousCookie !== cookie) {
                    sendLoginCookie(cookie);
                    previousCookie = cookie;
                    document.body.insertAdjacentText('afterbegin', 'Login Complete, you may now close the browser');
                }
            }, 1000);
            """;

        if (OperatingSystem.IsMacOS())
        {
            // WebView on Mac doesn't handle keyboard shortcuts properly, so add handling manually
            // See https://github.com/webview/webview/issues/403#issuecomment-787569812
            // And https://github.com/facebook/sapling/commit/3c9d72bc43b17abe4a89cef63f22eee8a60269c2
            return $$"""
                {{cookieHandling}}

                window.addEventListener('keypress', (event) => {
                    if (!event.metaKey) { return; }
                    switch (event.key) {
                        case 'c':
                            document.execCommand('copy');
                            event.preventDefault();
                            return;
                        case 'x':
                            document.execCommand('cut');
                            event.preventDefault();
                            return;
                        case 'v':
                            document.execCommand('paste');
                            event.preventDefault();
                            return;
                        case 'a':
                            document.execCommand('selectAll');
                            event.preventDefault();
                            return;
                        case 'z':
                            document.execCommand('undo');
                            event.preventDefault();
                            return;
                    }
                });
                """;
        }
        else
        {
            return cookieHandling;
        }
    }

    private static async Task<JsonObject> ReadConfigFileAsync()
    {
        var configFileContents = await File.ReadAllTextAsync(Options.ConfigFilePath);
        return JsonNode
                .Parse(configFileContents, new() { PropertyNameCaseInsensitive = true })
                ?.AsObject()
            ?? throw new InvalidOperationException("Unable to parse config JSON");
    }
}
