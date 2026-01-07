using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Data;

namespace UndercutF1.Console;

public class MainDisplay(
    IHttpClientFactory httpClientFactory,
    Formula1Account accountService,
    ILogger<MainDisplay> logger
) : IDisplay
{
    public Screen Screen => Screen.Main;

    private Task<string?>? _fetchVersionTask = null;
    private string _currentVersion = $"v{ThisAssembly.NuGetPackageVersion}";

    public Task<IRenderable> GetContentAsync()
    {
        var title = new Text(
            """
               __  __  _   __  ____    ______  ____    ______  __  __  ______        ______  ___
              / / / / / | / / / __ \  / ____/ / __ \  / ____/ / / / / /_  __/       / ____/ <  /
             / / / / /  |/ / / / / / / __/   / /_/ / / /     / / / /   / /         / /_     / / 
            / /_/ / / /|  / / /_/ / / /___  / _, _/ / /___  / /_/ /   / /         / __/    / /  
            \____/ /_/ |_/ /_____/ /_____/ /_/ |_|  \____/  \____/   /_/         /_/      /_/   
            """
        ).Centered();

        var accountDetail = accountService.IsAuthenticated switch
        {
            Formula1Account.AuthenticationResult.Success => $"""
                [green]Logged in to F1 TV account.[/] Token will expire on [bold]{accountService.Payload!.Expiry:yyyy-MM-dd}[/]
                """,
            Formula1Account.AuthenticationResult.ExpiredToken => """
                [yellow]Formula 1 account token has expired! Please press [bold]A[/] or run the following to log back in:[/]
                [italic]> undercutf1 login[/]
                """,
            _ => """
                [yellow]Some features (like Driver Tracker) require an F1 TV subscription.[/] Press [bold]A[/] or run the following to login:
                [italic]> undercutf1 login[/]
                See https://github.com/JustAman62/undercut-f1#f1-tv-account-login for details
                """,
        };

        var content = new Markup(
            $"""
            Welcome to [bold italic]Undercut F1[/].

            To start a live timing session, press [bold]S[/] then [bold]L[/].
            To start a replay a previously recorded/imported session, press [bold]S[/] then [bold]F[/].

            Once a session is started, navigate to the Timing Tower using [bold]T[/]
            Then use the Arrow Keys [bold]◄[/]/[bold]►[/] to switch between timing pages.
            Use [bold]N[/]/[bold]M[/]/[bold],[/]/[bold].[/] to adjust the stream delay, and [bold]▲[/]/[bold]▼[/] keys to use the cursor.
            Press Shift with these keys to adjust by a higher amount.

            You can download old session data from Formula 1 by running:
            [italic]> undercutf1 import[/]

            {accountDetail}
            """
        );

        var latestVersion = GetLatestVersion();

        var newVersionText =
            latestVersion == _currentVersion
                ? string.Empty
                : $"[green italic]A newer version is available: {latestVersion}[/]";

        var footer = new Markup(
            $"""
            GitHub: https://github.com/JustAman62/undercut-f1
            Version: {ThisAssembly.AssemblyInformationalVersion} {newVersionText}
            """
        );

        var layout = new Layout("Content").SplitRows(
            new Layout("Title", title).Size(8),
            new Layout("Content", content),
            new Layout("Footer", footer).Size(2)
        );
        var panel = new Panel(layout).Expand().RoundedBorder();

        return Task.FromResult<IRenderable>(panel);
    }

    private string GetLatestVersion()
    {
        // We don't want to block anything when fetching the latest version from GitHub
        // So we trigger a background task to fetch, and return the current version until we have a result available.
        switch (_fetchVersionTask)
        {
            case null:
                _fetchVersionTask = Task.Run(GetLatestVersionAsync);
                return _currentVersion;
            case { IsCompletedSuccessfully: true }:
                return _fetchVersionTask.Result ?? _currentVersion;
            default:
                return _currentVersion;
        }
    }

    private async Task<string?> GetLatestVersionAsync()
    {
        var httpClient = httpClientFactory.CreateClient("Default");
        var res = await httpClient.GetFromJsonAsync(
            "https://api.github.com/repos/justaman62/undercut-f1/tags",
            ConsoleSerializerContext.Default.GitHubTagEntryArray
        );
        var tag = res?.FirstOrDefault()?.Name;
        logger.LogInformation("Latest tag from GitHub: {Tag}", tag);
        return tag;
    }

    internal record GitHubTagEntry(string Name);
}
