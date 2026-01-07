using Spectre.Console;
using Spectre.Console.Rendering;
using UndercutF1.Data;

namespace UndercutF1.Console;

public sealed class ManageAccountDisplay(Formula1Account accountService) : IDisplay
{
    public Screen Screen => Screen.ManageAccount;

    public Task<IRenderable> GetContentAsync()
    {
        var markupText = accountService.IsAuthenticated switch
        {
            Formula1Account.AuthenticationResult.Success => $"""
                An access token is already configured in [bold]{ConsoleOptions.ConfigFilePath}[/].
                [dim]{accountService.Payload}[/]
                This token will expire on [bold]{accountService.Payload?.Expiry:yyyy-MM-dd}[/], at which point you'll need to login again.
                """,
            Formula1Account.AuthenticationResult.NoToken => $"""
                Login to your Formula 1 Account (which has any level of F1 TV subscription) to access all the Live Timing feeds and unlock all features of undercut-f1.

                An account is [bold]NOT[/] needed for undercut-f1 to function, it only unlocks the following features:
                - Driver Tracker (live position of cars on track)
                - Pit Stop times
                - Championship tables with live prediction
                - DRS active indicator on Timing Screen

                Additionally, logging in is [bold]NOT[/] required for the above features if you import data for already completed sessions, as all data feeds are available after a session is complete.

                Once logged in, your access token will be stored in [bold]{ConsoleOptions.ConfigFilePath}[/]. Your actual account credentials will not be stored anywhere.
                Simply remove the token entry from this file, or run [bold]undercutf1 logout[/] to remove usage of your token.

                See https://github.com/JustAman62/undercut-f1#f1-tv-account-login for details


                Press [bold]L[/] to open a browser to login to your F1 TV account.
                """,
            Formula1Account.AuthenticationResult.InvalidToken => """
                The token is invalid.
                """,
            Formula1Account.AuthenticationResult.InvalidSubscriptionStatus => $"""
                Your Formula 1 Access Token does not have an active (paid) subscription status.
                If you've recently subscribed, you may want to try logging in again.

                [dim]{accountService.Payload}[/]
                """,
            Formula1Account.AuthenticationResult.ExpiredToken => $"""
                [yellow]Formula 1 account token has expired![/]
                Please press [bold]L[/] to open a browser window to log back in
                """,
            _ => "Unknwon Error",
        };
        return Task.FromResult<IRenderable>(new Panel(new Markup(markupText)).Expand());
    }
}
