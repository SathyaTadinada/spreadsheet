using System.Text.RegularExpressions;

namespace FormulaEvaluator {
    /// <summary>
    /// A utility class that evaluates string expressions containing integers, variables, and operators.
    /// </summary>
    public static class Evaluator {

        /// <summary>
        /// A delegate that takes a string and returns an integer.
        /// </summary>
        /// <param name="v"></param>
        /// <returns>
        /// Returns an integer.
        /// </returns>
        public delegate int Lookup(string v);

        /// <summary>
        /// A static method that evaluates a string expression and returns an integer result.
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="variableEvaluator"></param>
        /// <returns> Returns an integer outputted from the full expression computation.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static int Evaluate(string exp, Lookup variableEvaluator) {

            // if the expression or the variable evaluator delegate is null, throw an exception
            if (exp == null || exp == "" || variableEvaluator == null) {
                throw new ArgumentException("Parameters cannot be null/empty.");
            }

            Stack<int> valueStack = new Stack<int>();
            Stack<string> operatorStack = new Stack<string>();

            // removes leading and trailing whitespace from the main expression
            exp = exp.Replace(" ", "");
            string[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");

            //int a = 1;
            //foreach (string s in substrings) {
            //    Console.WriteLine(a + ": " + s);
            //}

            // for each token in the expression...
            foreach (string token in substrings) {

                // if token is empty (removes any whitespace still persisting after regex split)
                if (token.Equals("")) {
                    continue;
                }

                // if the token is an integer...
                if (int.TryParse(token, out int value)) {

                    // if * or / is on top of the operator stack, pop value stack, pop operator stack,
                    // compute expression, then push result onto value stack
                    if (operatorStack.IsOnTop("*") || operatorStack.IsOnTop("/")) {
                        valueStack.Push(Calculate(value, valueStack.Pop(), operatorStack.Pop()));
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
                    // removes the ( from the top of the operator stack
                    if (operatorStack.IsOnTop("(")) {
                        operatorStack.Pop();
                    } else {
                        throw new ArgumentException("Parenthesis mismatch.");
                    }
                    if (operatorStack.IsOnTop("*") || operatorStack.IsOnTop("/")) {
                        valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
                    }

                }

                // if token is a variable...
                else {
                    // store the value of the variable
                    if (char.IsLetter(token[0]) && char.IsNumber(token[token.Length - 1])) {
                        Console.WriteLine(token);
                        value = variableEvaluator(token);
                        Console.WriteLine(value);
                    } else {
                        throw new ArgumentException("Invalid variable.");
                    }
                    Console.WriteLine(value);
                    // (same case as if token is an integer)
                    if (operatorStack.IsOnTop("*") || operatorStack.IsOnTop("/")) {
                        valueStack.Push(Calculate(value, valueStack.Pop(), operatorStack.Pop()));
                    } else { // otherwise, push token onto value stack
                        valueStack.Push(value);
                    }
                }
            }
            // after the last token is processed...
            while (operatorStack.Count > 0) {

                // while there are still operators on the stack, pop value stack twice and operator stack once,
                // compute and push result onto value stack
                if (valueStack.Count < 2) {
                    throw new ArgumentException("Invalid expression.");
                }
                valueStack.Push(Calculate(valueStack.Pop(), valueStack.Pop(), operatorStack.Pop()));
            }

            if (valueStack.Count == 1) {
                return valueStack.Pop();
            } else {
                throw new ArgumentException("Invalid expression.");
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
        private static int Calculate(int left, int right, string op) {
            switch (op) {
                case "+":
                    return left + right;
                case "-":
                    return right - left; // order matters for subtraction
                case "*":
                    return left * right;
                case "/":
                    try { // try to divide by zero
                        return right / left;
                    } catch (Exception) {
                        throw new ArgumentException("Cannot divide by zero.");
                    }
                default: // operator was not +, -, *, or /
                    if (op.Equals("(") || op.Equals(")"))
                        throw new ArgumentException("Parenthesis mismatch.");
                    else {
                        throw new ArgumentException("The operator " + op + " is invalid.");
                    }
            }
        }
    }

    /// <summary>
    /// A static class that contains extension methods for the Stack class.
    /// </summary>
    public static class StackExtensions {

        /// <summary>
        /// An extension method that checks if the top of the stack is equal to the operator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stack"></param>
        /// <param name="op"></param>
        /// <returns>
        /// True if the top of the stack is equal to the operator, false otherwise.
        /// </returns>
        public static bool IsOnTop<T>(this Stack<T> stack, T op) {

            // is stack empty? if not, is the top of the stack equal to the operator?
            return (stack.Count > 0) && stack.Peek().Equals(op);
        }
    }
}