using Spectre.Console;

namespace OpenF1.Console;

internal static class TableExtensions
{
    public static void RemoveColumnPadding(this Table table)
    {
        foreach (var col in table.Columns)
        {
            col.Padding = new Padding(0);
        }
    }
}
