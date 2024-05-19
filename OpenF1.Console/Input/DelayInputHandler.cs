using OpenF1.Data;

namespace OpenF1.Console;

public class IncreaseDelayInputHandler(IDateTimeProvider dateTimeProvider) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageSession, Screen.TimingTower, Screen.RaceControl];

    public ConsoleKey ConsoleKey => ConsoleKey.RightArrow;

    public string Description => "Delay+";

    public int Sort => 20;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var increaseBy = consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 30 : 5;
        dateTimeProvider.Delay += TimeSpan.FromSeconds(increaseBy);
        return Task.CompletedTask;
    }
}

public class DecreaseDelayInputHandler(IDateTimeProvider dateTimeProvider) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageSession, Screen.TimingTower, Screen.RaceControl];

    public ConsoleKey ConsoleKey => ConsoleKey.LeftArrow;

    public string Description => "Delay-";

    public int Sort => 21;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var decreaseBy = consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 30 : 5;
        dateTimeProvider.Delay -= TimeSpan.FromSeconds(decreaseBy);
        if (dateTimeProvider.Delay < TimeSpan.Zero)
            dateTimeProvider.Delay = TimeSpan.Zero;

        return Task.CompletedTask;
    }
}
