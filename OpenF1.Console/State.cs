namespace OpenF1.Console;

public record State
{
    public Screen CurrentScreen { get; set; } = Screen.Main;
    public int CursorOffset { get; set; } = 0;
}
