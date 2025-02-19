using Spectre.Console;

namespace OpenF1.Console;

internal static class TableExtensions
{
    public static Table RemoveColumnPadding(this Table table, int leftPadding = 0)
    {
        foreach (var col in table.Columns)
        {
            col.Padding = new Padding(left: leftPadding, 0, 0, 0);
        }
        // Reset the leftmost column padding to 0
        table.Columns[0].Padding(new Padding(0));
        return table;
    }
}
