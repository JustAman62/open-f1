using OpenF1.Data;

namespace OpenF1.Console;

public class IncreaseDelayInputHandler(ITimingService timingService) : IInputHandler
{
    public Screen[]? ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey ConsoleKey => ConsoleKey.RightArrow;

    public string Description => "Increase Delay";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var increaseBy = consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 30 : 5;
        timingService.Delay += TimeSpan.FromSeconds(increaseBy);
        return Task.CompletedTask;
    }
}

public class DecreaseDelayInputHandler(ITimingService timingService) : IInputHandler
{
    public Screen[]? ApplicableScreens => Enum.GetValues<Screen>();

    public ConsoleKey ConsoleKey => ConsoleKey.LeftArrow;

    public string Description => "Decrease Delay";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        var decreaseBy = consoleKeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) ? 30 : 5;
        timingService.Delay -= TimeSpan.FromSeconds(decreaseBy);
        if (timingService.Delay < TimeSpan.Zero)
            timingService.Delay = TimeSpan.Zero;

        return Task.CompletedTask;
    }
}
