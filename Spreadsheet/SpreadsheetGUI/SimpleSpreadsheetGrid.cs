// Written by Joe Zachary and Travis Martin for CS 3500, September 2011, 2023
namespace SS;

/// <summary>
/// A VERY simple grid that displays a spreadsheet with a small number of rows and columns.
/// Each cell on the grid can display a string.
/// 
/// This class needs lots of work to be a fully functioning spreadsheet, and is
/// intended for experimentation only.
/// 
/// This still needs row and column labels, better selection detection, and much more.
/// It also has significant performance limitations beyond ~100 cells.
/// </summary>
public class SimpleSpreadsheetGrid : ScrollView, ISpreadsheetGrid
{
    public event SelectionChangedHandler SelectionChanged;

    // These constants control the layout of the spreadsheet grid.
    // The height and width measurements are in pixels.
    private const int DATA_COL_WIDTH = 120;
    private const int DATA_ROW_HEIGHT = 40;
    private const int COL_COUNT = 10;
    private const int ROW_COUNT = 10;
    private const int FONT_SIZE = 24;

    // Columns and rows are numbered beginning with 0.  This is the coordinate
    // of the selected cell.
    private int _selectedCol;
    private int _selectedRow;

    private readonly Dictionary<Address, Entry> _cells = new();

    public SimpleSpreadsheetGrid()
    {
        Grid grid = new();
        for (int row = 0; row < ROW_COUNT; row++)
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DATA_ROW_HEIGHT) });
        for (int col = 0; col < COL_COUNT; col++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DATA_COL_WIDTH) });

        for (int row = 0; row < ROW_COUNT; row++)
        {
            for (int col = 0; col < COL_COUNT; col++)
            {
                Entry e = new Entry { FontSize = FONT_SIZE, Text="" };
                e.Focused += SelectionHandlerForEntry(col, row);
                _cells[new Address(col, row)] = e;
                grid.Add(e, col, row);
            }
        }
        Content = grid;
        Orientation = ScrollOrientation.Both;
    }

    private EventHandler<FocusEventArgs> SelectionHandlerForEntry(int col, int row)
    {
        return (object sender, FocusEventArgs e) =>
        {
            SetSelection(col, row);
            if (SelectionChanged != null)
                SelectionChanged(this);
        };
    }

    public void Clear()
    {
        foreach (Entry cell in _cells.Values)
            cell.Text = "";
    }

    public bool SetValue(int col, int row, string c)
    {
        if (InvalidAddress(col, row))
            return false;
        _cells[new Address(col, row)].Text = c;
        return true;
    }

    public bool GetValue(int col, int row, out string c)
    {
        if (InvalidAddress(col, row))
        {
            c = "";
            return false;
        }
        c = _cells[new Address(col, row)].Text;
        return true;
    }

    public bool SetSelection(int col, int row)
    {
        if (InvalidAddress(col, row))
            return false;
        _selectedCol = col;
        _selectedRow = row;
        _cells[new Address(col, row)].Focus();
        return true;
    }

    public void GetSelection(out int col, out int row)
    {
        col = _selectedCol;
        row = _selectedRow;
    }



    private bool InvalidAddress(int col, int row)
    {
        return col < 0 || row < 0 || col >= COL_COUNT || row >= ROW_COUNT;
    }

    /// <summary>
    /// Used internally to keep track of cell addresses
    /// </summary>
    private class Address
    {
        public int Col { get; set; }
        public int Row { get; set; }

        public Address(int c, int r)
        {
            Col = c;
            Row = r;
        }

        public override int GetHashCode()
        {
            return Col.GetHashCode() ^ Row.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is Address))
            {
                return false;
            }
            Address a = (Address)obj;
            return Col == a.Col && Row == a.Row;
        }
    }
}
