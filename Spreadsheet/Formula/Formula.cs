// Skeleton written by Profs Zachary, Kopta and Martin for CS 3500
// Read the entire skeleton carefully and completely before you
// do anything else!
// Last updated: August 2023 (small tweak to API)

using System.Text.RegularExpressions;

namespace SpreadsheetUtilities;

/// <summary>
/// Represents formulas written in standard infix notation using standard precedence
/// rules.  The allowed symbols are non-negative numbers written using double-precision
/// floating-point syntax (without unary preceding '-' or '+');
/// variables that consist of a letter or underscore followed by
/// zero or more letters, underscores, or digits; parentheses; and the four operator
/// symbols +, -, *, and /.
///
/// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
/// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable;
/// and "x 23" consists of a variable "x" and a number "23".
///
/// Associated with every formula are two delegates: a normalizer and a validator.  The
/// normalizer is used to convert variables into a canonical form. The validator is used to
/// add extra restrictions on the validity of a variable, beyond the base condition that
/// variables must always be legal: they must consist of a letter or underscore followed
/// by zero or more letters, underscores, or digits.
/// Their use is described in detail in the constructor and method comments.
/// </summary>
public class Formula {
    private List<string> formulaExpression; // a list of tokens that make up the formula

    /// <summary>
    /// Creates a Formula from a string that consists of an infix expression written as
    /// described in the class comment.  If the expression is syntactically invalid,
    /// throws a FormulaFormatException with an explanatory Message.
    ///
    /// The associated normalizer is the identity function, and the associated validator
    /// maps every string to true.
    /// </summary>
    public Formula(string formula) :
        this(formula, s => s, s => true) {
    }

    /// <summary>
    /// Creates a Formula from a string that consists of an infix expression written as
    /// described in the class comment.  If the expression is syntactically incorrect,
    /// throws a FormulaFormatException with an explanatory Message.
    ///
    /// The associated normalizer and validator are the second and third parameters,
    /// respectively.
    ///
    /// If the formula contains a variable v such that normalize(v) is not a legal variable,
    /// throws a FormulaFormatException with an explanatory message.
    ///
    /// If the formula contains a variable v such that isValid(normalize(v)) is false,
    /// throws a FormulaFormatException with an explanatory message.
    ///
    /// Suppose that N is a method that converts all the letters in a string to upper case, and
    /// that V is a method that returns true only if a string consists of one letter followed
    /// by one digit.  Then:
    ///
    /// new Formula("x2+y3", N, V) should succeed
    /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
    /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
    /// </summary>
    public Formula(string formula, Func<string, string> normalize, Func<string, bool> isValid) {

        // make sure the formula is not null or empty
        // ensures there is at least one token
        if (formula == null || formula.Length == 0) {
            throw new FormulaFormatException("Formula cannot be empty. Check your input and try again.");
        }

        formulaExpression = new List<string>(); // initializes the formula expression list

        // normalize the formula, then check if it contains legal variables
        formula = normalize(formula);

        if (!ContainsLegalVariables(formula)) {
            throw new FormulaFormatException("Formula contains illegal variables. " +
                "Check the validator to ensure that you are inputting the correct type of variable.");
        }

        List<string> formulaExpressionVariables = GetVariables().ToList();

        foreach (var variable in formulaExpressionVariables) {
            if (!isValid(variable)) {
                throw new FormulaFormatException("Formula contains invalid variables. " +
                                       "Check the validator to ensure that you are inputting the correct type of variable.");
            }
        }

        // check the formula for syntactic correctness
        int leftParenCount = 0;
        int rightParenCount = 0;

        string firstChar = formulaExpression[0];
        string lastChar = formulaExpression[formulaExpression.Count - 1];

        // go through the rest of the tokens, making sure to check for right parentheses rule,
        // parenthesis/operator following rule, and extra following rule
        for (int i = 0; i < formulaExpression.Count - 1; i++) {
            string token = formulaExpression[i];
            string nextToken = formulaExpression[i + 1];

            // checks starting token rule
            if (i == 0) {
                if (!IsNumVarOrOpenParen(firstChar)) {
                    throw new FormulaFormatException("The first token of an expression must be a number, a variable, " +
                        "or an opening parenthesis.");
                } else if (firstChar.Equals("(")) {
                    leftParenCount++;
                    if (!IsNumVarOrOpenParen(nextToken)) {
                        throw new FormulaFormatException("A token that follows an operator or opening parenthesis " +
                            "must be a number, variable, or an opening parenthesis.");
                    }
                    continue;
                }
            }

            // checks for parenthesis/operator following rule
            if (IsOperator(token) || token.Equals("(")) {
                if (token.Equals("(")) {
                    leftParenCount++;
                }
                if (!IsNumVarOrOpenParen(nextToken)) {
                    throw new FormulaFormatException("A token that follows an operator or opening parenthesis " +
                        "must be a number, variable, or an opening parenthesis.");
                }
            }

            // checks for extra following rule
            if (IsNumVarOrCloseParen(token)) {
                // checks for right parentheses rule
                if (token.Equals(")")) {
                    rightParenCount++;
                    if (rightParenCount > leftParenCount) {
                        throw new FormulaFormatException("The number of closing parentheses is greater than " +
                            "the number of opening parentheses.");
                    }
                }

                if (!(IsOperator(nextToken) || nextToken.Equals(")"))) {
                    throw new FormulaFormatException("A number, variable, or closing parenthesis must be followed by " +
                        "an operator or closing parenthesis.");
                }
            }
        }

        // checks ending token rule
        if (!IsNumVarOrCloseParen(lastChar)) {
            throw new FormulaFormatException("The last token of an expression must be a number, a variable, " +
                               "or a closing parenthesis.");
        } else if (lastChar.Equals(")")) {
            rightParenCount++;
        }

        // checks for balanced parentheses rule
        if (leftParenCount != rightParenCount) {
            throw new FormulaFormatException("Parenthesis mismatch.");
        }

    }

    /// <summary>
    /// Evaluates this Formula, using the lookup delegate to determine the values of
    /// variables.  When a variable symbol v needs to be determined, it should be looked up
    /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to
    /// the constructor.)
    ///
    /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters
    /// in a string to upper case:
    ///
    /// new Formula("x+7", N, s => true).Evaluate(L) is 11
    /// new Formula("x+7").Evaluate(L) is 9
    ///
    /// Given a variable symbol as its parameter, lookup returns the variable's value
    /// (if it has one) or throws an ArgumentException (otherwise).
    ///
    /// If no undefined variables or divisions by zero are encountered when evaluating
    /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.
    /// The Reason property of the FormulaError should have a meaningful explanation.
    ///
    /// This method should never throw an exception.
    /// </summary>
    public object Evaluate(Func<string, double> lookup) {
        Stack<double> valueStack = new Stack<double>();
        Stack<string> operatorStack = new Stack<string>();

        foreach (string token in formulaExpression) {
            if (double.TryParse(token, out double value)) {

                // if * or / is on top of the operator stack, pop value stack, pop operator stack,
                // compute expression, then push result onto value stack
                if (operatorStack.IsOnTop("*") || operatorStack.IsOnTop("/")) {
                    try {
                        valueStack.Push(Calculate(value, valueStack.Pop(), operatorStack.Pop()));
                    } catch (ArgumentException e) {
                        return new FormulaError(e.Message); // should only be division by zero error
                    }
                } else { // otherwise, push token onto value stack
                    valueStack.Push(value);
                }
            }

            // if the token is a + or - ...
            else if (token.Equals("+") || token.Equals("-")) {

                // if + or - is on top of the stack, pop the value stack twice and operator stack once,
                // then compute expression, push result onto value stack
                if (operatorStack.IsOnTop("+") || operatorStack.IsOnTop("-")) {
                    valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
                }
                operatorStack.Push(token);
            }

            // if token is * or /, push token onto operator stack
            else if (token.Equals("*") || token.Equals("/")) {
                operatorStack.Push(token);
            }

            // if token is (, push token onto operator stack
            else if (token.Equals("(")) {
                operatorStack.Push(token);
            }

            // if token is ) and +, -, *, / is on top of the value stack,
            // pop value stack twice and operator stack once, compute and push result onto value stack
            else if (token.Equals(")")) {
                if (operatorStack.IsOnTop("+") || operatorStack.IsOnTop("-")) {
                    valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
                }

                operatorStack.Pop();

                if (operatorStack.IsOnTop("*") || operatorStack.IsOnTop("/")) {
                    try {
                        valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
                    } catch (ArgumentException e) {
                        return new FormulaError(e.Message); // should only be division by zero error
                    }
                }
            }

            // if token is a variable...
            else {
                // store the value of the variable
                try {
                    value = lookup(token);
                } catch (Exception e) {
                    return new FormulaError(e.Message);
                }
                // (same case as if token is an integer)
                if (operatorStack.IsOnTop("*") || operatorStack.IsOnTop("/")) {
                    try {
                        valueStack.Push(Calculate(value, valueStack.Pop(), operatorStack.Pop()));
                    } catch (ArgumentException e) {
                        return new FormulaError(e.Message); // should only be division by zero error
                    }
                } else { // otherwise, push token onto value stack
                    valueStack.Push(value);
                }
            }
        }

        // after the last token is processed...
        while (operatorStack.Count > 0) {
            valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
        }

        return valueStack.Pop();
    }

    /// <summary>
    /// Enumerates the normalized versions of all of the variables that occur in this
    /// formula.  No normalization may appear more than once in the enumeration, even
    /// if it appears more than once in this Formula.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///
    /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
    /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
    /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
    /// </summary>
    public IEnumerable<string> GetVariables() {
        HashSet<string> hashSet = new HashSet<string>();

        foreach (string token in formulaExpression) {
            if (!double.TryParse(token, out double value)) { // if token is not a double...
                if (!Regex.IsMatch(token, @"[\+\-*/()]")) { // if token is not an operator/parentheses
                    hashSet.Add(token);
                } else {
                    continue;
                }
            } else {
                continue;
            }
        }

        return new List<string>(hashSet);
    }

    /// <summary>
    /// Returns a string containing no spaces which, if passed to the Formula
    /// constructor, will produce a Formula f such that this.Equals(f).  All of the
    /// variables in the string should be normalized.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///
    /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
    /// new Formula("x + Y").ToString() should return "x+Y"
    /// </summary>
    public override string ToString() {
        string result = "";
        foreach (string s in formulaExpression) {
            if (Double.TryParse(s, out double value)) {
                result += value.ToString();
            } else if (s.Equals("(") || s.Equals(")")) {
                result += s;
            } else if (s.Equals("+") || s.Equals("-") || s.Equals("*") || s.Equals("/")) {
                result += s;
            } else {
                result += s;
            }
        }
        return result;
    }

    /// <summary>
    /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
    /// whether or not this Formula and obj are equal.
    ///
    /// Two Formulae are considered equal if they consist of the same tokens in the
    /// same order.  To determine token equality, all tokens are compared as strings
    /// except for numeric tokens and variable tokens.
    /// Numeric tokens are considered equal if they are equal after being "normalized" by
    /// using C#'s standard conversion from string to double (and optionally back to a string).
    /// Variable tokens are considered equal if their normalized forms are equal, as
    /// defined by the provided normalizer.
    ///
    /// For example, if N is a method that converts all the letters in a string to upper case:
    ///
    /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
    /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
    /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
    /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
    /// </summary>
    public override bool Equals(object? obj) {
        if (obj is null or not Formula) {
            return false;
        }

        List<string> argFormulaExpression = ((Formula)obj).formulaExpression;

        if (formulaExpression.Count != argFormulaExpression.Count) {
            return false;
        }

        for (int i = 0; i < formulaExpression.Count; i++) {
            if (double.TryParse(formulaExpression[i], out double value1) && double.TryParse(argFormulaExpression[i], out double value2)) {
                if (value1 != value2) {
                    return false;
                }
            } else if (formulaExpression[i] != argFormulaExpression[i]) {
                return false;
            }
        }

        return true;

    }

    /// <summary>
    /// Reports whether f1 == f2, using the notion of equality from the Equals method.
    /// Note that f1 and f2 cannot be null, because their types are non-nullable
    /// </summary>
    public static bool operator ==(Formula f1, Formula f2) {
        return f1.Equals(f2);
    }

    /// <summary>
    /// Reports whether f1 != f2, using the notion of equality from the Equals method.
    /// Note that f1 and f2 cannot be null, because their types are non-nullable
    /// </summary>
    public static bool operator !=(Formula f1, Formula f2) {
        return !(f1 == f2);
    }

    /// <summary>
    /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
    /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two
    /// randomly-generated unequal Formulae have the same hash code should be extremely small.
    /// </summary>
    public override int GetHashCode() {
        return ToString().GetHashCode();
    }

    /// <summary>
    /// A private helper method that checks if a formula contains legal variables.
    /// A variable is legal if it starts with a letter or underscore, and is followed
    /// by numbers, letters, or underscores.
    /// </summary>
    /// <param name="formula"></param>
    /// <returns>
    /// True if the formula contains legal variables, false otherwise.
    /// </returns>
    private bool ContainsLegalVariables(string formula) {
        string variablePattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
        string operatorParenthesesPattern = @"[\+\-*/()]";

        IEnumerable<string> formulaTokens = GetTokens(formula);
        foreach (string token in formulaTokens) {
            if (!double.TryParse(token, out double value)) { // if token is not a double...
                if (!Regex.IsMatch(token, operatorParenthesesPattern)) { // if token is not an operator/parentheses
                    if (!Regex.IsMatch(token, variablePattern)) { // if token is not a legal variable
                        return false;
                    }
                } else {
                    continue;
                }
            } else {
                continue;
            }
        }
        formulaExpression = formulaTokens.ToList();
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="formula"></param>
    /// <returns></returns>
    private bool IsNumVarOrOpenParen(string formula) {
        return Double.TryParse(formula, out double value) || Regex.IsMatch(formula, @"^[a-zA-Z_][a-zA-Z0-9_]*$") || formula.Equals("(");
    }

    /// <summary>
    /// A private helper method that checks if a token is a number, variable, or closing parenthesis.
    /// </summary>
    /// <param name="formula"></param>
    /// <returns>
    /// True if the token is a number, variable, or closing parenthesis, false otherwise.
    /// </returns>
    private bool IsNumVarOrCloseParen(string formula) {
        return Double.TryParse(formula, out double value) || Regex.IsMatch(formula, @"^[a-zA-Z_][a-zA-Z0-9_]*$") || formula.Equals(")");
    }

    /// <summary>
    /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
    /// right paren; one of the four operator symbols; a legal variable token;
    /// a double literal; and anything that doesn't match one of those patterns.
    /// There are no empty tokens, and no token contains white space.
    /// </summary>
    private static IEnumerable<string> GetTokens(string formula) {
        // Patterns for individual tokens
        string lpPattern = @"\(";
        string rpPattern = @"\)";
        string opPattern = @"[\+\-*/]";
        string varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        string doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
        string spacePattern = @"\s+";

        // Overall pattern
        string pattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                        lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

        // Enumerate matching tokens that don't consist solely of white space.
        foreach (string s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace)) {
            if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline)) {
                yield return s;
            }
        }

    }

    /// <summary>
    /// A private helper method that does the computation of two integers and an operation
    /// </summary>
    /// <param name="left"></param> The left integer
    /// <param name="right"></param> The right integer
    /// <param name="op"></param> The operator (+, -, *, /) to evaluate the integers with
    /// <returns> Returns an integer of the simple arithmetic computation </returns> 
    /// <exception cref="ArgumentException"></exception>
    private double Calculate(double left, double right, string op) {
        switch (op) {
            case "-":
                return right - left; // order matters for subtraction
            case "*":
                return left * right;
            case "/":
                if (left == 0) {
                    throw new ArgumentException("Cannot divide by zero.");
                } else {
                    return right / left;
                }
            case "+":
            default:
                return left + right; // default case should never run, this should only trigger for "+" operatorf
        }
    }

    /// <summary>
    /// A private helper method that checks if a token is an operator
    /// </summary>
    /// <param name="token"></param>
    /// <returns>
    /// True if the token is an operator, false otherwise.
    /// </returns>
    private bool IsOperator(string token) {
        return token.Equals("+") || token.Equals("-") || token.Equals("*") || token.Equals("/");
    }
}

/// <summary>
/// A static class that contains an extension method for a stack.
/// </summary>
internal static class StackExtensions {
    /// <summary>
    /// An extension method that checks if the top of the stack is equal to the operator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stack"></param>
    /// <param name="op"></param>
    /// <returns>
    /// True if the top of the stack is equal to the operator, false otherwise.
    /// </returns>
    public static bool IsOnTop<T>(this Stack<T> stack, T op) where T : notnull {
        if (stack.Count > 0) {
            return stack.Peek().Equals(op);
        } else {
            return false;
        }
    }
}

/// <summary>
/// Used to report syntactic errors in the argument to the Formula constructor.
/// </summary>
public class FormulaFormatException : Exception {
    /// <summary>
    /// Constructs a FormulaFormatException containing the explanatory message.
    /// </summary>
    public FormulaFormatException(string message) : base(message) {
    }
}

/// <summary>
/// Used as a possible return value of the Formula.Evaluate method.
/// </summary>
public struct FormulaError {
    /// <summary>
    /// Constructs a FormulaError containing the explanatory reason.
    /// </summary>
    /// <param name="reason"></param>
    public FormulaError(string reason) : this() {
        Reason = reason;
    }

    /// <summary>
    ///  The reason why this FormulaError was created.
    /// </summary>
    public string Reason { get; private set; }
}