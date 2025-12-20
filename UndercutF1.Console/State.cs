namespace UndercutF1.Console;

public record State
{
    public Screen PreviousScreen = Screen.Main;

    public Screen CurrentScreen
    {
        get;
        set
        {
            PreviousScreen = field;
            field = value;
        }
    } = Screen.Main;

    public int CursorOffset { get; set; } = 0;

    public (string? First, string? Second) CompareDrivers { get; set; } = (null, null);
}
