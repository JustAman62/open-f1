using Spectre.Console;

namespace OpenF1.Console;

public record State
{
    private Screen _screen = Screen.Main;

    public Screen CurrentScreen { 
        get => _screen;
        set {
            AnsiConsole.Clear();
            _screen = value;
        }
    }

    public int CursorOffset { get; set; } = 0;

    public List<string> SelectedDrivers { get; set; } = [];
}
