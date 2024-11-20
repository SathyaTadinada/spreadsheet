using SpreadsheetUtilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SS;

/// <summary>
/// A spreadsheet object represents the state of a simple spreadsheet. A 
/// spreadsheet consists of an infinite number of named cells.
/// 
/// A string is a cell name if and only if it consists of a letter or underscore followed by
/// zero or more letters, underscores, or digits, AND it satisfies the predicate IsValid.
/// For example, "_", "A1", and "BC89" are cell names so long as they satisfy IsValid.
/// On the other hand, "0A1", "1+1", and "" are not cell names, regardless of IsValid.
/// 
/// Any valid incoming cell name, whether passed as a parameter or embedded in a formula,
/// must be normalized with the Normalize method before it is used by or saved in 
/// this spreadsheet.  For example, if Normalize is s => s.ToUpper(), then
/// the Formula "x3+a5" should be converted to "X3+A5" before use.
/// 
/// A spreadsheet contains a cell corresponding to every possible cell name.  
/// In addition to a name, each cell has a contents and a value.  The distinction is
/// important.
/// 
/// The contents of a cell can be (1) a string, (2) a double, or (3) a Formula.  If the
/// contents is an empty string, we say that the cell is empty.  (By analogy, the contents
/// of a cell in Excel is what is displayed on the editing line when the cell is selected.)
/// 
/// In a new spreadsheet, the contents of every cell is the empty string.
///  
/// The value of a cell can be (1) a string, (2) a double, or (3) a FormulaError.  
/// (By analogy, the value of an Excel cell is what is displayed in that cell's position
/// in the grid.)
/// 
/// If a cell's contents is a string, its value is that string.
/// 
/// If a cell's contents is a double, its value is that double.
/// 
/// If a cell's contents is a Formula, its value is either a double or a FormulaError,
/// as reported by the Evaluate method of the Formula class.  The value of a Formula,
/// of course, can depend on the values of variables.  The value of a variable is the 
/// value of the spreadsheet cell it names (if that cell's value is a double) or 
/// is undefined (otherwise).
/// 
/// Spreadsheets are never allowed to contain a combination of Formulas that establish
/// a circular dependency.  A circular dependency exists when a cell depends on itself.
/// For example, suppose that A1 contains B1*2, B1 contains C1*2, and C1 contains A1*2.
/// A1 depends on B1, which depends on C1, which depends on A1.  That's a circular
/// dependency.
/// </summary>
public class Spreadsheet : AbstractSpreadsheet {

    [JsonInclude]
    public Dictionary<string, Cell> Cells; // a dictionary that maps names to cells

    private DependencyGraph graph; // a graph of dependencies between cells

    protected Func<string, bool> IsValid { get; private set; } // a function that checks if a name is valid
    protected Func<string, string> Normalize { get; private set; } // a function that normalizes a name

    /// <summary>
    /// Constructs a basic spreadsheet with version information. This
    /// constructor is only for use by extending classes, which will certainly
    /// want the possibility of accepting more parameters, for example for
    /// validity checking, normalization, etc.
    /// </summary>
    public Spreadsheet() : base("default") {
        Cells = new Dictionary<string, Cell>();
        graph = new DependencyGraph();

        IsValid = s => true;
        Normalize = s => s;

        Changed = false;
    }

    /// <summary>
    /// A three-argument constructor that accepts a validity delegate, a normalization delegate, and a version.
    /// </summary>
    /// <param name="isValid"></param>
    /// <param name="normalize"></param>
    /// <param name="version"></param>
    public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(version) {
        Cells = new Dictionary<string, Cell>();
        graph = new DependencyGraph();

        IsValid = isValid;
        Normalize = normalize;

        Changed = false;
    }

    /// <summary>
    /// Allows the user to provide a string representing a path to a file (first parameter), 
    /// a validity delegate (second parameter), a normalization delegate (third parameter), 
    /// and a version (fourth parameter). 
    /// 
    /// It will read a saved spreadsheet from the file and use it to construct a new spreadsheet. 
    /// The new spreadsheet will use the provided validity delegate, normalization delegate, and version.
    /// 
    /// When you load a spreadsheet from a file, you will have two spreadsheet objects. 
    /// One will be the spreadsheet you deserialized, which should only contain strings. 
    /// The other will be the spreadsheet you wish to construct. Keep this distinction in mind, and think 
    /// about how you can use the information in the string-only spreadsheet to properly construct 
    /// everything in your real spreadsheet with dependencies, Formulas, etc. 
    /// Hint: you can mostly use methods you already have.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="isValid"></param>
    /// <param name="normalize"></param>
    /// <param name="version"></param>
    public Spreadsheet(string filePath, Func<string, bool> isValid, Func<string, string> normalize, string version) : base(version) {
        Cells = new Dictionary<string, Cell>();
        graph = new DependencyGraph();
        IsValid = isValid;
        Normalize = normalize;

        // reads the spreadsheet from the file
        try {
            string text = File.ReadAllText(filePath);
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.IncludeFields = true;
            Spreadsheet? tempSheet = JsonSerializer.Deserialize<Spreadsheet>(text, jso)
                ?? throw new SpreadsheetReadWriteException("Deserialization failed.");

            // checks versions
            if (tempSheet.Version != version) {
                throw new SpreadsheetReadWriteException("Mismatched versions.");
            }

            // copy the tempSheet variables into this spreadsheet
            Cells = tempSheet.Cells;

            // evaluate the value of each cell in the dictionary
            foreach (var cell in Cells) {
                SetContentsOfCell(cell.Key, cell.Value.StringForm);
            }

            // add the dependees to the graph
            foreach (var cell in Cells) {
                if (cell.Value.Contents is Formula formula) {
                    graph.ReplaceDependees(cell.Key, formula.GetVariables());
                }
            }

            // sets Changed to false
            Changed = false;

        } catch (Exception e) {
            throw new SpreadsheetReadWriteException(e.Message);
        }
    }

    /// <summary>
    /// A constructor used for JSON deserialization.
    /// </summary>
    /// <param name="Cells"></param>
    /// <param name="Version"></param>
    [JsonConstructor]
    public Spreadsheet(Dictionary<string, Cell> Cells, string Version) : base(Version) {
        this.Cells = Cells;
        Normalize = s => s;
        IsValid = s => true;
        graph = new DependencyGraph();
    }

    /// <summary>
    /// If name is invalid, throws an InvalidNameException.
    /// 
    /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
    /// value should be either a string, a double, or a Formula.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException"></exception>
    public override object GetCellContents(string name) {
        // checks if the name is valid
        if (!(IsNameValid(name) && IsValid(name))) {
            throw new InvalidNameException();
        }

        name = Normalize(name); // normalize the name

        // returns the contents of the cell, or an empty string if the cell doesn't exist
        return Cells.ContainsKey(name) ? Cells[name].Contents : "";
    }

    /// <summary>
    /// If name is invalid, throws an InvalidNameException.
    /// 
    /// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
    /// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
    /// </summary>
    public override object GetCellValue(string name) {
        // checks if the name is valid
        if (!(IsNameValid(name) && IsValid(name))) {
            throw new InvalidNameException();
        }

        name = Normalize(name); // normalize the name

        if (Cells.ContainsKey(name)) { // if the cell exists, return its value
            return Cells[name].Value;
        } else { // else, return an empty string
            return "";
        }
    }

    /// <summary>
    /// Enumerates the names of all the non-empty cells in the spreadsheet.
    /// </summary>
    /// <returns></returns>
    public override IEnumerable<string> GetNamesOfAllNonemptyCells() {
        foreach (var cell in Cells) { // for each cell in the map...
            if (cell.Value.Contents.ToString() != "") { // if the cell is not empty, return the name
                yield return cell.Key;
            }
        }
    }

    /// <summary>
    /// Writes the contents of this spreadsheet to the named file using a JSON format.
    /// The JSON object should have the following fields:
    /// "Cells" - a data structure containing 0 or more cell entries
    ///           Each cell entry has a field (or key) named after the cell itself 
    ///           The value of that field is another object representing the cell's contents
    ///               The contents object has a single field called "StringForm",
    ///               representing the string form of the cell's contents
    ///               - If the contents is a string, the value of StringForm is that string
    ///               - If the contents is a double d, the value of StringForm is d.ToString()
    ///               - If the contents is a Formula f, the value of StringForm is "=" + f.ToString()
    /// "Version" - the version of the spreadsheet software (a string)
    /// 
    /// For example, if this spreadsheet has a version of "default" 
    /// and contains a cell "A1" with contents being the double 5.0 
    /// and a cell "B3" with contents being the Formula("A1+2"), 
    /// a JSON string produced by this method would be:
    /// 
    /// {
    ///   "Cells": {
    ///     "A1": {
    ///       "StringForm": "5"
    ///     },
    ///     "B3": {
    ///       "StringForm": "=A1+2"
    ///     }
    ///   },
    ///   "Version": "default"
    /// }
    /// 
    /// If there are any problems opening, writing, or closing the file, the method should throw a
    /// SpreadsheetReadWriteException with an explanatory message.
    /// </summary>
    public override void Save(string filename) {
        JsonSerializerOptions jso = new JsonSerializerOptions();
        jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        jso.WriteIndented = true;

        // writes the spreadsheet to the file
        try {
            File.WriteAllText(filename, JsonSerializer.Serialize(this, jso));
        } catch (Exception e) {
            throw new SpreadsheetReadWriteException(e.Message);
        }

        // sets Changed to false
        Changed = false;
    }

    /// <summary>
    /// If name is invalid, throws an InvalidNameException.
    /// 
    /// Otherwise, the contents of the named cell becomes number. The method returns a
    /// list consisting of name plus the names of all other cells whose value depends, 
    /// directly or indirectly, on the named cell.
    /// 
    /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    /// list {A1, B1, C1} is returned.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="number"></param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException"></exception>
    protected override IList<string> SetCellContents(string name, double number) {
        // checks if the cell already exists, if it does, update it, if not, add it
        if (Cells.ContainsKey(name)) {
            Cells[name] = new Cell(number + "", number, LookupHelper);
        } else {
            Cells.Add(name, new Cell(number + "", number, LookupHelper));
        }

        // replaces the dependees with an empty set
        graph.ReplaceDependees(name, new HashSet<string>());

        // returns the recalculated cells
        return GetCellsToRecalculate(name).ToList();
    }

    /// <summary>
    /// If name is invalid, throws an InvalidNameException.
    /// 
    /// Otherwise, the contents of the named cell becomes text.  The method returns a
    /// list consisting of name plus the names of all other cells whose value depends, 
    /// directly or indirectly, on the named cell.
    /// 
    /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    /// list {A1, B1, C1} is returned.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException"></exception>
    protected override IList<string> SetCellContents(string name, string text) {
        // checks if the cell already exists, if it does, update it, if not, add it
        if (Cells.ContainsKey(name)) {
            Cells[name] = new Cell(text, text, LookupHelper);
        } else {
            Cells.Add(name, new Cell(text, text, LookupHelper));
        }

        // replaces the dependees with an empty set
        graph.ReplaceDependees(name, new HashSet<string>());

        // returns the recalculated cells
        return GetCellsToRecalculate(name).ToList();
    }

    /// <summary>
    /// If name is invalid, throws an InvalidNameException.
    /// 
    /// Otherwise, if changing the contents of the named cell to be the formula would cause a 
    /// circular dependency, throws a CircularException, and no change is made to the spreadsheet.
    /// 
    /// Otherwise, the contents of the named cell becomes formula.  The method returns a
    /// list consisting of name plus the names of all other cells whose value depends,
    /// directly or indirectly, on the named cell.
    /// 
    /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    /// list {A1, B1, C1} is returned.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="formula"></param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException"></exception>
    /// <exception cref="CircularException"></exception>
    protected override IList<string> SetCellContents(string name, Formula formula) {
        // creates a temporary list of the dependees of the given name,
        // then replaces the dependees with the variables of the formula
        IEnumerable<string> tempDependees = graph.GetDependees(name);
        graph.ReplaceDependees(name, formula.GetVariables());

        try {
            // stores the recalculated cells in a list
            IEnumerable<string> recalculatedCells = GetCellsToRecalculate(name);
            if (Cells.ContainsKey(name)) { // if the cell already exists, update it
                Cells[name] = new Cell("=" + formula.ToString(), formula, LookupHelper);
            } else { // if the cell doesn't exist, add it
                Cells.Add(name, new Cell("=" + formula.ToString(), formula, LookupHelper));
            }

            // returns the recalculated cells
            return GetCellsToRecalculate(name).ToList();
        } catch (CircularException) {
            // if there is a circular exception, replace the dependees with the original dependees, 
            // then throw the exception
            graph.ReplaceDependees(name, tempDependees);
            throw new CircularException();
        }
    }

    /// <summary>
    /// If name is invalid, throws an InvalidNameException.
    /// 
    /// Otherwise, if content parses as a double, the contents of the named
    /// cell becomes that double.
    /// 
    /// Otherwise, if content begins with the character '=', an attempt is made
    /// to parse the remainder of content into a Formula f using the Formula
    /// constructor.  There are then three possibilities:
    /// 
    ///   (1) If the remainder of content cannot be parsed into a Formula, a 
    ///       SpreadsheetUtilities.FormulaFormatException is thrown.
    ///       
    ///   (2) Otherwise, if changing the contents of the named cell to be f
    ///       would cause a circular dependency, a CircularException is thrown,
    ///       and no change is made to the spreadsheet.
    ///       
    ///   (3) Otherwise, the contents of the named cell becomes f.
    /// 
    /// Otherwise, the contents of the named cell becomes content.
    /// 
    /// If an exception is not thrown, the method returns a list consisting of
    /// name plus the names of all other cells whose value depends, directly
    /// or indirectly, on the named cell. The order of the list should be any
    /// order such that if cells are re-evaluated in that order, their dependencies 
    /// are satisfied by the time they are evaluated.
    /// 
    /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    /// list {A1, B1, C1} is returned.
    /// </summary>
    public override IList<string> SetContentsOfCell(string name, string content) {
        // checks if the name is valid
        if (!(IsNameValid(name) && IsValid(name))) {
            throw new InvalidNameException();
        }
        // declares a list to hold the cells that need to be recalculated after the change
        IEnumerable<string> recalculatedCells;

        // checks if the content is a double
        if (double.TryParse(content, out double number)) {
            recalculatedCells = SetCellContents(name, number);
        }

        // else, checks if the content is a formula
        else if (content.StartsWith("=")) {
            recalculatedCells = SetCellContents(name, new Formula(content.Substring(1), Normalize, IsValid));
        }

        // else, set the cell contents to the string
        else {
            recalculatedCells = SetCellContents(name, content);
        }

        // cells have been modified, set Changed to true
        Changed = true;

        foreach (string s in recalculatedCells) {
            if (Cells.ContainsKey(s)) { // if the cell exists, update its value
                if (Cells[s].Contents is Formula) {
                    Cells[s].Value = ((Formula)Cells[s].Contents).Evaluate(LookupHelper);
                } else {
                    Cells[s].Value = Cells[s].Contents;
                }
            }
        }

        // returns a list of the recalculated cells
        return recalculatedCells.ToList();
    }

    /// <summary>
    /// Returns an enumeration, without duplicates, of the names of all cells whose
    /// values depend directly on the value of the named cell.  In other words, returns
    /// an enumeration, without duplicates, of the names of all cells that contain
    /// formulas containing name.
    /// 
    /// For example, suppose that
    /// A1 contains 3
    /// B1 contains the formula A1 * A1
    /// C1 contains the formula B1 + A1
    /// D1 contains the formula B1 - C1
    /// The direct dependents of A1 are B1 and C1
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected override IEnumerable<string> GetDirectDependents(string name) {
        // returns a HashSet of all the dependents of the given name
        return graph.GetDependents(name);
    }

    /// <summary>
    /// A private method that uses the Formula constructor to check if a name is valid.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private bool IsNameValid(string name) {
        return Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z_0-9]*$"); // checks against variable regex
    }

    /// <summary>
    /// A private method that looks up the value of a cell.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private double LookupHelper(string name) {
        // checks if the cell exists
        if (Cells.ContainsKey(name)) {
            // checks if the cell's value is a double
            if (Cells[name].Value is double) {
                return (double)Cells[name].Value;
            }
        }

        throw new ArgumentException("The value of the cell is not a double.");
    }

    /// <summary>
    /// A private class that represents a cell in the spreadsheet.
    /// </summary>
    public class Cell {
        // properties for the contents and value of the cell
        public string StringForm { get; set; }

        [JsonIgnore]
        public object Contents { get; set; }

        [JsonIgnore]
        public object Value { get; set; }

        /// <summary>
        /// A constructor for a cell that takes in an object and a lookup function.
        /// </summary>
        /// <param name="contents"></param>
        public Cell(string name, object contents, Func<string, double> LookupFunc) {
            StringForm = name;
            Contents = contents;
            if (contents is Formula) {
                Value = ((Formula)contents).Evaluate(LookupFunc);
            } else {
                Value = contents;
            }
        }

        /// <summary>
        /// The constructor that the JSON uses to deserialize the spreadsheet.
        /// </summary>
        /// <param name="StringForm"></param>
        [JsonConstructor]
        public Cell(string StringForm) {
            this.StringForm = StringForm;

            if (StringForm.StartsWith("=")) {
                Contents = new Formula(StringForm.Substring(1));
            } else {
                Contents = StringForm;
            }

            Value = StringForm;
        }
    }
}