using CommunityToolkit.Maui.Storage;
using SpreadsheetUtilities;
using SS;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetGUI;

/// <summary>
/// Example of using a SpreadsheetGUI object
/// </summary>
public partial class MainPage : ContentPage {
    private Spreadsheet sheet;
    private int versionNumber;

    /// <summary>
    /// Constructor for the demo
    /// </summary>
	public MainPage() {
        InitializeComponent();

        spreadsheetGrid.SelectionChanged += displaySelection;
        spreadsheetGrid.SetSelection(0, 0);

        sheet = new Spreadsheet(IsNameWithinCellRange, s => s.ToUpper(), "ps6");
        versionNumber = 1;

        SelectColorOptionsAtStart();
    }

    /// <summary>
    /// Displays the contents and values of the selected cell in the text boxes at the top
    /// </summary>
    /// <param name="grid"></param>
    private void displaySelection(ISpreadsheetGrid grid) {
        spreadsheetGrid.GetSelection(out int col, out int row);
        cellNameEntry.Text = ((char)(col + 65)) + "" + (row + 1);

        if (sheet.GetCellContents(cellNameEntry.Text) is Formula) {
            contentEntryField.Text = "=" + sheet.GetCellContents(cellNameEntry.Text).ToString();
        } else {
            contentEntryField.Text = sheet.GetCellContents(cellNameEntry.Text).ToString();
        }

        if (sheet.GetCellValue(cellNameEntry.Text) is FormulaError) {
            valueEntry.Text = "#ERROR";
        } else {
            valueEntry.Text = sheet.GetCellValue(cellNameEntry.Text).ToString();
        }
    }

    /// <summary>
    /// Handles the event when the user clicks on the "Confirm" button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentsOfCellSubmitted(object sender, EventArgs e) {
        string content = contentEntryField.Text;
        string name = cellNameEntry.Text;

        int selectedColumn = name[0] - 65;
        int selectedRow = name[1] - 49;

        content ??= "";
        IList<string> cellsToUpdate = new List<string>();

        try {
            cellsToUpdate = sheet.SetContentsOfCell(name, content);

            if (content.Equals("")) {
                spreadsheetGrid.SetValue(selectedColumn, selectedRow, content);
                valueEntry.Text = content;
            }

            //UpdateSpreadsheetGrid(selectedColumn, selectedRow);
            statusMenuItem.Text = "Status: Unsaved | Data last modified " + DateTime.Now.ToLocalTime().ToString("T");
        } catch (FormulaFormatException exception) {
            DisplayAlert("Formula Format Exception", exception.Message, "OK");
        } catch (CircularException) {
            DisplayAlert("Circular Exception", "There are one or more circular references where a formula " +
                "refers to its own cell either directly or indirectly.", "OK");
        } catch (InvalidNameException) {
            DisplayAlert("Invalid Name Exception", "One or more variable names are invalid. " +
                "Please ensure that variable names are in the range A1-Z99.", "OK");
        } finally {
            UpdateCells(cellsToUpdate, selectedColumn, selectedRow);
        }

    }

    /// <summary>
    /// Handles the event when the user clicks on the "New" menu dropdown item
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void NewClicked(Object sender, EventArgs e) {
        if (sheet.Changed) {
            bool selection = await DisplayAlert("Save changes?", "The current spreadsheet has unsaved data. " +
                "Do you want to save before you create a new spreadsheet?", "Save", "Don't Save");

            // saves the current state of spreadsheet (using SaveClicked)
            if (selection) {
                using var stream = new MemoryStream(Encoding.Default.GetBytes(""));
                FileSaverResult fileSaverResult = await FileSaver.Default.SaveAsync("sheet" + versionNumber + ".sprd", stream, new CancellationToken());
                if (fileSaverResult.IsSuccessful) {
                    sheet.Save(fileSaverResult.FilePath);
                    statusMenuItem.Text = "Status: Saved";
                    versionNumber++; // updates the version number for each time the user saves the spreadsheet
                    CreateNewSpreadsheet();
                } else {
                    return;
                }
            }
        }

        CreateNewSpreadsheet();
        statusMenuItem.Text = "Status: Saved";
    }

    /// <summary>
    /// Handles the event when the user clicks on the "Save" menu dropdown item
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SaveClicked(Object sender, EventArgs e) {
        using var stream = new MemoryStream(Encoding.Default.GetBytes(""));
        int versionNumber = 1;
        FileSaverResult fileSaverResult = await FileSaver.Default.SaveAsync("sheet" + versionNumber + ".sprd", stream, new CancellationToken());
        if (fileSaverResult.IsSuccessful) {
            sheet.Save(fileSaverResult.FilePath);
            statusMenuItem.Text = "Status: Saved";
            versionNumber++; // updates the version number for each time the user saves the spreadsheet
        }
    }

    /// <summary>
    /// Handles the event when the user clicks on the "Open" menu dropdown item
    /// </summary>
    private async void OpenClicked(Object sender, EventArgs e) {
        if (sheet.Changed) { // prompts the user to save a modified spreadsheet before opening a new one
            bool selection = await DisplayAlert("Save changes?", "The current spreadsheet has unsaved data. " +
                "Do you want to save before you open a new spreadsheet?", "Save", "Don't Save");

            // saves the current state of spreadsheet
            if (selection) {
                using var stream = new MemoryStream(Encoding.Default.GetBytes(""));
                FileSaverResult fileSaverResult = await FileSaver.Default.SaveAsync("sheet" + versionNumber + ".sprd", stream, new CancellationToken());
                if (fileSaverResult.IsSuccessful) {
                    sheet.Save(fileSaverResult.FilePath);
                    statusMenuItem.Text = "Status: Saved";
                    versionNumber++; // updates the version number for each time the user saves the spreadsheet
                } else {
                    return;
                }
            }
        }

        try { // try to open a file
            PickOptions options = new() { // sets some options for the file picker (header + file types)
                PickerTitle = "Select a spreadsheet",
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>> {
                        { DevicePlatform.WinUI, new[] {".sprd" } },
                        { DevicePlatform.macOS, new[] {".sprd" } }
                    })
            };
            FileResult fileResult = await FilePicker.Default.PickAsync(options);
            if (fileResult != null) { // if the user picked a file and it matches the file type...
                if (fileResult.FileName.EndsWith("sprd", StringComparison.OrdinalIgnoreCase)) {
                    sheet = new Spreadsheet(fileResult.FullPath, IsNameWithinCellRange, s => s.ToUpper(), "ps6");
                    spreadsheetGrid.SetSelection(0, 0);
                    cellNameEntry.Text = "A1";
                    spreadsheetGrid.Clear();
                    UpdateCells(sheet.GetNamesOfAllNonemptyCells().ToList(), 0, 0);
                    displaySelection(spreadsheetGrid);
                }

            }
        } catch (Exception) {

        }
    }

    /// <summary>
    /// Updates the color of the header and header text in the spreadsheet grid.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ColorModificationConfirmSubmitted(Object sender, EventArgs e) {
        spreadsheetGrid.RedrawCanvas(headerColorPicker.SelectedIndex, textColorPicker.SelectedIndex);

    }

    /// <summary>
    /// Displays a help box describing how to navigate the spreadsheet
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigatingSpreadsheetClicked(Object sender, EventArgs e) {
        DisplayAlert("Navigating the Spreadsheet",
            "Click on a cell in the grid to select it.\n" +
            "You can horizontally and vertically scroll through 26 columns and 99 rows, respectively.\n" +
            "The File menu in the top left corner has spreadsheet saving and loading features.\n\n" +
            "The value of the selected cell will appear in the top right corner, next to the \"Value: \" label.\n" +
            "The contents of the selected cell will appear in the editable text box, next to the \"Contents:\" label.\n\n" +
            "Note: Download and install the MAUI Community Toolkit for the \"Save\" and \"Open\" functions to work properly.",
            "OK");
    }

    /// <summary>
    /// Displays a help box describing how to modify data in the spreadsheet
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ModifyingSpreadsheetDataClicked(Object sender, EventArgs e) {
        DisplayAlert("Modifying the Spreadsheet",
            "Step 1: Select a cell by clicking on it in the grid.\n" +
            "Step 2: Enter the contents of the cell in the Cell Contents entry field.\n" +
            "Step 3: Click the Confirm button.",
            "OK");
    }

    /// <summary>
    /// Displays a help box describing how to use the spreadsheet saving and loading features.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SavingLoadingSpreadsheetClicked(Object sender, EventArgs e) {
        DisplayAlert("Saving and Loading the Spreadsheet",
            "To open a new spreadsheet, navigate to File > New. \n" +
            "To open an existing spreadsheet, navigate to File > Open. \n" +
            "To save the current spreadsheet, navigate to File > Save. \n\n" +
            "Note: If you have not saved your current spreadsheet before creating or opening another spreadsheet, you will be prompted to do so.\n\n" +
            "The status label at the top of the spreadsheet indicates whether or not the latest version of the spreadsheet has been saved. If the latest " +
            "version of the spreadsheet is not saved, the time of the last data modification is displayed.",
            "OK");
    }

    /// <summary>
    /// Displays a help box describing how to use the special feature.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpecialFeatureClicked(Object sender, EventArgs e) {
        DisplayAlert("Customizing the Spreadsheet",
            "There are two dropdowns below the Cell Modification bar. The dropdown to the right of the \"Header Background Color: \" label changes " +
            "the background color of the column and row headers. The dropdown to the right of the \"Header Text Color: \" label changes the text color of " +
            "the column and row headers.",
            "OK");
    }

    /// <summary>
    /// A private helper method that determines if a variable is within the range A1 - Z99.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private bool IsNameWithinCellRange(string s) {
        if (Regex.IsMatch(s[..1], "[A-Z]") &&
            Regex.IsMatch(s[1..], "[0-9]*")) {
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Helper method that clears the spreadsheet grid and creates a new spreadsheet.
    /// </summary>
    /// <param name="filename"></param>
    private void CreateNewSpreadsheet() {
        // clears the front-end GUI
        spreadsheetGrid.Clear();
        contentEntryField.Text = "";
        valueEntry.Text = "";
        cellNameEntry.Text = "A1";

        // makes a blank spreadsheet
        sheet = new Spreadsheet(IsNameWithinCellRange, s => s.ToUpper(), "ps6");
    }

    /// <summary>
    /// Helper method that updates the spreadsheet grid with the current values of the spreadsheet.
    /// </summary>
    /// <param name="selectedColumn"></param>
    /// <param name="selectedRow"></param>
    private void UpdateCells(IList<string> cellsToUpdate, int selectedColumn, int selectedRow) {
        foreach (string s in cellsToUpdate) {
            object value = sheet.GetCellValue(s);
            int col = s[0] - 65;
            int row = s[1] - 49;

            if (value is FormulaError) {
                spreadsheetGrid.SetValue(col, row, "#ERROR");
                if (col == selectedColumn && row == selectedRow) {
                    valueEntry.Text = "#ERROR";
                }
            } else {
                spreadsheetGrid.SetValue(col, row, sheet.GetCellValue(s).ToString());
                if (col == selectedColumn && row == selectedRow) {
                    valueEntry.Text = sheet.GetCellValue(s).ToString();
                }
            }
        }
    }

    /// <summary>
    /// A helper method that initializes the color options for the spreadsheet grid.
    /// </summary>
    private void SelectColorOptionsAtStart() {
        textColorPicker.SelectedIndex = 0;
        headerColorPicker.SelectedIndex = 1;
    }
}
