using FormulaEvaluator;

namespace SpreadsheetExecutable {
    internal class Program {
        /// <summary>
        /// The main method of the executable.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args) {
            //Console.Write("Enter an expression: ");
            //string expression = Console.ReadLine();

            //while (expression == null || !expression.Equals("")) {
            //    try {
            //        Console.WriteLine(Evaluator.Evaluate(expression, Lookup));
            //    } catch (ArgumentException e) {
            //        Console.WriteLine(e.Message);
            //    }
            //    Console.WriteLine();
            //    Console.Write("Enter an expression (or press Enter to finish): ");
            //    expression = Console.ReadLine();
            //}

            //Console.WriteLine();
            //Console.WriteLine("The following methods use Lookup delegate method by summing the ASCII values of each variable character:");
            //Console.WriteLine();
            //Console.WriteLine("1 + 2 * A1 = " + Evaluator.Evaluate("1 + 2 * A1", Lookup)); // 1 + 2 * 114 = 229
            //Console.WriteLine("(2 + 3) / 2 + 2 = " + Evaluator.Evaluate("(2 + 3) / 2 + 2", Lookup)); // (2 + 3) / 2 + 2 = 4
            //Console.WriteLine("(A2 * B4) + (C3 - (D1 / E5)) = " + Evaluator.Evaluate("(A2 * B4) + (C3 - (D1 / E5))", Lookup)); // (115 * 118) + (118 - (117 / 122)) = 13688
            //Console.WriteLine("(6 / 3) * (2 + (3 / 6)) = " + Evaluator.Evaluate("(6 / 3) * (2 + (3 / 6))", Lookup)); // (6 / 3) * (2 + (3 / 6)) = 4
            //Console.WriteLine("6 / 2 * (1 + 2) = " + Evaluator.Evaluate("6 / 2 * (1 + 2)", Lookup)); // 6 / 2 * (1 + 2) = 9
            //Console.WriteLine("(3*   3 + 9) * ((1 + 2) * 2) = " + Evaluator.Evaluate("(3*   3 + 9) * ((1 + 2) * 2)", Lookup)); // (3 * 3 + 9) * ((1 + 2) * 2) = 108
            //Console.WriteLine("A1 + A1 - A1 * (A1 / A1) = " + Evaluator.Evaluate("A1 + A1 - A1 * (A1 / A1)", Lookup)); // 114 + 114 - 114 * (114 / 114) = 114
            //Console.WriteLine("42 = " + Evaluator.Evaluate("42", Lookup)); // 42 = 42
            //Console.WriteLine("2-3 - (4 - 5) - (6 - 7) + (8 - 9) = " + Evaluator.Evaluate("2-3 - (4 - 5) - (6 - 7) + (8 - 9)", Lookup)); // 2 - 3 - (4 - 5) - (6 - 7) + (8 - 9) = 0

            //Console.WriteLine();
            //Console.WriteLine("The following methods return different exceptions (or use BasicLookup delegate, which only allows the variable \"A1\"):");
            //Console.WriteLine();

            //try {
            //    Console.WriteLine("3 /    0 = " + Evaluator.Evaluate("3 /    0", Lookup)); // 3 / 0 = Argument exception.
            //} catch (ArgumentException e) {
            //    Console.WriteLine("3 /    0 = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("5+7+(5)8", Lookup); // 5+7+(5)8 = Argument exception.
            //} catch (ArgumentException e) {
            //    Console.WriteLine("5+7+(5)8 = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("2 / 0.25 = " + Evaluator.Evaluate("2 / 0.25", Lookup)); // 2 / 0.25 = Argument exception.
            //} catch (ArgumentException e) {
            //    Console.WriteLine("2 / 0.25 = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("(null expression) = " + Evaluator.Evaluate("", Lookup)); // "" = Argument exception.
            //} catch (ArgumentException e) {
            //    Console.WriteLine("(null expression) = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("(3*4 ) + ((5+3) / (4) = " + Evaluator.Evaluate("(3*4 ) + ((5+3) / (4)", Lookup)); // (3*4) + ((5+3) / (4) = Argument exception.
            //} catch (ArgumentException e) {
            //    Console.WriteLine("(3*4 ) + ((5+3) / (4) = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("3 * 4 * * 5 = " + Evaluator.Evaluate("3 * 4 * * 5", Lookup)); // 3 * 4 * * 5 = Argument exception.
            //} catch (Exception e) {
            //    Console.WriteLine("3 * 4 * * 5 = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("3 * 4) = " + Evaluator.Evaluate("3 * 4)", Lookup)); // 3 * 4) = Argument exception.
            //} catch (Exception e) {
            //    Console.WriteLine("3 * 4) = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("3 ) = " + Evaluator.Evaluate("3 )", Lookup)); // 3 ) = Argument exception.
            //} catch (Exception e) {
            //    Console.WriteLine("3 ) = " + e.Message);
            //}

            //try {
            //    Console.WriteLine("A1 * B2 = " + Evaluator.Evaluate("A1 * B2", BasicLookup)); // A1 * B2 = Argument exception.
            //} catch (Exception e) {
            //    Console.WriteLine("A1 * B2 (using BasicLookup) = " + e.Message);
            //}
        }

        ///// <summary>
        ///// An implementation of the delegate Lookup that returns the integer value of a variable.
        ///// Uses the sum of the ASCII values of each character in the string to compute the value.
        ///// </summary>
        ///// <param name="s"></param>
        ///// <returns>The sum of the ASCII values of each character</returns>
        ///// <exception cref="ArgumentException"></exception>
        //public static int Lookup(string s) {
        //    int value = 0;
        //    //try {
        //    //    if (!s.All(Char.IsLetterOrDigit) || !char.IsLetter(s[0])) {
        //    //        throw new ArgumentException("Argument is not a valid variable.");
        //    //    }
        //    //    foreach (char c in s) {
        //    //        value += System.Convert.ToInt32(c);
        //    //    }

        //    //    return value;
        //    //} catch (Exception) {
        //    //    throw new ArgumentException("Argument is not a valid variable.");
        //    //}

        //    foreach (char c in s) {
        //        value += System.Convert.ToInt32(c);
        //    }

        //    return value;
        //}

        ///// <summary>
        ///// A (very) basic implementation of the delegate Lookup that returns 5 for the string "A1"
        ///// </summary>
        ///// <param name="t"></param>
        ///// <returns> The integer 5 for the specific string "A1".</returns>
        //public static int BasicLookup(string t) {
        //    if (t.Equals("A1")) {
        //        return 5;
        //    } else {
        //        throw new ArgumentException("Argument is not a valid variable.");
        //    }
        //}




    }
}