using UndercutF1.Data;

namespace UndercutF1.Console;

public class SwitchToManageAccount(State state, Formula1Account accountService) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens =>
        accountService.IsAuthenticated == Formula1Account.AuthenticationResult.Success
            ? [Screen.Main]
            // If not currently logged in, how the input on screens where data may be missing
            : [Screen.Main, Screen.DriverTracker, Screen.ChampionshipStats];

    public ConsoleKey[] Keys => [ConsoleKey.A];

    public string Description => "F1 Account";

    public int Sort => 70;

    public async Task ExecuteAsync(
        ConsoleKeyInfo consoleKeyInfo,
        CancellationToken cancellationToken = default
    )
    {
        await Terminal.OutAsync(ControlSequences.ClearScreen(ClearMode.Full), cancellationToken);
        state.CurrentScreen = Screen.ManageAccount;
    }
}
