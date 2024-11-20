# Spreadsheet Application

## Author Information

- Authors: Sathya Tadinada and Carter Ma
- Date: October 23, 2023

## Summary

This README provides essential information for the Spreadsheet Application. It documents design decisions, external code resources, implementation notes, and any other relevant details.

## Design Decisions

- **Grid Layout:** The spreadsheet grid has 26 columns and 99 rows with the addresses following Excel conventions (A1 in top-left corner, Z99 in bottom-right corner).
- **Cell Handling:** Each cell has associated content (string, double, or formula) and value (string, double, or error).
- **User Interface:** Designed a user-friendly UI with a highlighted selected cell, non-editable text boxes displaying cell name and value, and an editable text box for cell contents.
- **File Handling:** Used JSON format for saving and reading spreadsheets. Default file extension for saving and reading: ".sprd".
- **Safety Features:** Implemented warning dialogs for potential data loss during operations and file overwrites.

## Additional Features

- The theme of the spreadsheet can be personalized by using the dropdown menu underneath the cell text boxes. The "Header Background Color" option changes the background color of the header row, and the "Cell Background Color" option changes the background color of the selected cell.
- Next to the "Help" menu is a "Status" symbol, which displays the current status of the spreadsheet. The status is updated whenever the user makes a change to the spreadsheet, and it is also updated when the user saves the spreadsheet.

## Implementation Notes

- Initially implemented a VerticalStackLayout for the various components of the spreadsheet, but was changed to a Grid layout instead, so that the SpreadsheetGrid object could be scrolled through. (2023-10-18)
- When working on the special feature, we attempted to modify the SpreadsheetGrid object so that we could input custom colors, fonts, and font sizes for the grid itself. However, some of the properties were seemingly unchangeable without ruining additional functionality within the SpreadsheetGrid. (2023-10-21)

## External Code Resources

- The MAUI Community Toolkit (i.e. the FileSaver class) is used in this application, specifically for the Save operation.

## Problems Encountered

- Implemented the validator and normalizer for the variables, because initially any legal variable was passing through the Formula constructor, leading to FormulaErrors instead of the expected exception. (2023-10-13)
- A bug that we encountered (which we soon resolved) was when we did the operation File > New > Save > Exit Save Dialog, the spreadsheet would clear and remove all contents, regardless of whether or not the user saved the file. We resolved this by adding a check to see if the user saved the file, and if not, the spreadsheet would not clear. (2023-10-19)
- Fixed a bug where the spreadsheet would not update the dependent value if a circular exception was triggered and a parent cell was modified. (2023-10-21)
- A problem that occurred was that the spreadsheet would take a bit of time to refresh all of the cells in the spreadsheet if a parent cell was modified. To fix this, instead of updating every cell, we only update the dependent cells. (2023-10-21)
