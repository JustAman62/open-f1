using OpenF1.Data;

namespace OpenF1.Console;

public class IncreaseDelayInputHandler(ITimingService timingService) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageSession, Screen.TimingTower];

    public ConsoleKey ConsoleKey => ConsoleKey.RightArrow;

    public string Description => "Delay+";

    public int Sort => 20;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var increaseBy = consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 30 : 5;
        timingService.Delay += TimeSpan.FromSeconds(increaseBy);
        return Task.CompletedTask;
    }
}

public class DecreaseDelayInputHandler(ITimingService timingService) : IInputHandler
{
    public bool IsEnabled => true;

    public Screen[] ApplicableScreens => [Screen.ManageSession, Screen.TimingTower];

    public ConsoleKey ConsoleKey => ConsoleKey.LeftArrow;

    public string Description => "Delay-";

    public int Sort => 21;

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var decreaseBy = consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 30 : 5;
        timingService.Delay -= TimeSpan.FromSeconds(decreaseBy);
        if (timingService.Delay < TimeSpan.Zero)
            timingService.Delay = TimeSpan.Zero;

        return Task.CompletedTask;
    }
}
