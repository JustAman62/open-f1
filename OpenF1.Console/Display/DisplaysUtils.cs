using OpenF1.Data;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace OpenF1.Console;

public static class DisplayUtils
{
    public static readonly Style STYLE_NORMAL = new(foreground: Color.White);
    public static readonly Style STYLE_INVERT = new(
        foreground: Color.Black,
        background: Color.White
    );
    public static readonly Style STYLE_PB = new(
        foreground: Color.White,
        background: new Color(0, 118, 0)
    );
    public static readonly Style STYLE_BEST = new(
        foreground: Color.White,
        background: new Color(118, 0, 118)
    );

    public static IRenderable DriverTag(
        DriverListDataPoint.Driver driver,
        TimingDataPoint.Driver line,
        bool selected
    )
    {
        var lineStyle = selected ? STYLE_INVERT : STYLE_NORMAL;
        if (line.KnockedOut == true || line.Retired == true)
        {
            lineStyle = lineStyle.Combine(new Style(decoration: Decoration.Dim));
        }

        if (line.Retired != true && line.Stopped == true)
        {
            lineStyle = lineStyle.Combine(new Style(background: Color.Red));
        }

        if (
            line.Status.HasValue
            && line.Status.Value.HasFlag(TimingDataPoint.Driver.StatusFlags.ChequeredFlag)
        )
        {
            lineStyle = lineStyle.Combine(new Style(decoration: Decoration.Invert));
        }

        return new Markup($"{line.Line, 2} {MarkedUpDriverNumber(driver)}", lineStyle);
    }

    public static string MarkedUpDriverNumber(DriverListDataPoint.Driver driver) =>
        $"[#{driver.TeamColour ?? "000000"} bold]{driver.RacingNumber, 2} {driver.Tla ?? "UNK"}[/]";

    public static Style GetStyle(
        TimingDataPoint.Driver.Interval? interval,
        bool isComparisonLine,
        CarDataPoint.Entry.Car? car = null,
        Decoration decoration = Decoration.None
    )
    {
        if (interval is null)
            return STYLE_NORMAL;

        var foreground = Color.White;
        var background = default(Color?);

        if (isComparisonLine)
        {
            foreground = Color.Black;
            background = Color.White;
        }

        if (interval.IntervalSeconds() < 1 && interval.IntervalSeconds() > 0)
        {
            foreground = Color.Green3;
        }

        if (car is { Channels: { Drs: > 8 } })
        {
            foreground = Color.White;
            background = Color.Green3;
        }
        return new Style(foreground: foreground, background: background, decoration: decoration);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Harder to read"
    )]
    public static Style GetStyle(TimingAppDataPoint.Driver.Stint? stint)
    {
        if (stint is null)
            return STYLE_NORMAL;

        return GetStyleForTyreCompound(stint.Compound);
    }

    public static Style GetStyleForTyreCompound(string? compound) =>
        compound switch
        {
            "HARD" => new Style(foreground: Color.White, background: Color.Grey),
            "MEDIUM" => new Style(foreground: Color.Black, background: Color.Yellow),
            "SOFT" => new Style(foreground: Color.White, background: Color.Red),
            "INTERMEDIATE" => new Style(foreground: Color.Black, background: Color.Green),
            "WET" => new Style(foreground: Color.Black, background: Color.Blue),
            _ => STYLE_NORMAL,
        };

    public static IRenderable GetGapBetweenLines(
        TimingDataPoint.Driver? from,
        TimingDataPoint.Driver to,
        Decoration decoration = Decoration.None
    )
    {
        if (from == to)
        {
            return new Text("-------", STYLE_INVERT);
        }

        if (from?.GapToLeaderSeconds() is not null && to.GapToLeaderSeconds() is not null)
        {
            var style = STYLE_NORMAL.Combine(new(decoration: decoration));
            var gap = to.GapToLeaderSeconds() - from.GapToLeaderSeconds();
            return new Text($"{(gap > 0 ? "+" : "")}{gap, 3} ".ToFixedWidth(8), style);
        }

        return new Text("");
    }
}
