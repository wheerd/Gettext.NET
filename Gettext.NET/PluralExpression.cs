using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace GettextDotNet
{
    /// <summary>
    /// Used to parse the C expressions used to specify plural rules in gettext.
    /// The parsing is quite rudimentary but works for all the default plural formulas.
    /// It uses the shunting-yard algorithm to handle braces and operator precedence.
    /// Most expressions translate directly, but for boolean expressions, which are eiter 0 or 1 in C,
    /// the boolean values in C# have to be converted via <c>expr ? 1 : 0</c> or <c>expr != 0</c> for
    /// the opposite direction.
    /// 
    /// Some ideas taken from:
    /// http://www.aboutmycode.com/net-framework/building-expression-evaluator-with-expression-trees-in-csharp-part-1/
    /// 
    /// GNU Gettext page on plural forms:
    /// http://www.gnu.org/savannah-checkouts/gnu/gettext/manual/html_node/Plural-forms.html
    /// </summary>
    public class PluralExpression
    {
        /// <summary>
        /// The original string expression that was parsed
        /// </summary>
        public readonly string Source;
        
        /// <summary>
        /// The parsed expression
        /// </summary>
        public readonly Expression<Func<int, int>> Expr;

        /// <summary>
        /// The plural expression as compiled function
        /// </summary>
        private readonly Func<int, int> Compiled;

        /// <summary>
        /// The expression stack contains all the parsed expressions and is used to
        /// collect operands for the operators.
        /// </summary>
        private Stack<Expression> expressionStack = new Stack<Expression>();

        /// <summary>
        /// The operator stack contains all the unprocessed operators which are processed
        /// based on their precedence after their operands have been collected.
        /// </summary>
        private Stack<string> operatorStack = new Stack<string>();

        /// <summary>
        /// Processes the operators on the stack while the given condition holds true.
        /// To process an operator, the needed amount of arguments is grabbed from the
        /// expression stack and converted to the right type if needed
        /// </summary>
        /// <param name="condition">
        /// The condition delegate which gets the operator to process as a string and
        /// decides if it should be processed.
        /// </param>
        private void ProcessOperatorWhile(Func<string,bool> condition)
        {
            while (operatorStack.Count > 0 && condition(operatorStack.Peek()))
            {
                var op = (Operation)operatorStack.Pop();

                if (op.NumArgs == 1)
                {
                    var arg = ConvertExpression(expressionStack.Pop(), op.IsBooleanArgument(0));

                    expressionStack.Push(op.Apply(arg));
                }
                else if (op.NumArgs == 2)
                {
                    var right = ConvertExpression(expressionStack.Pop(), op.IsBooleanArgument(1));
                    var left = ConvertExpression(expressionStack.Pop(), op.IsBooleanArgument(0));

                    expressionStack.Push(op.Apply(left, right));
                }
                else if (op.NumArgs == 3)
                {
                    var third = ConvertExpression(expressionStack.Pop(), op.IsBooleanArgument(2));
                    var second = ConvertExpression(expressionStack.Pop(), op.IsBooleanArgument(1));
                    var first = ConvertExpression(expressionStack.Pop(), op.IsBooleanArgument(0));

                    expressionStack.Push(op.Apply(first, second, third));
                }
            }
        }

        /// <summary>
        /// Converts the an expression to the expected type if necessary.
        /// </summary>
        /// <param name="expr">The expression to convert.</param>
        /// <param name="hasToBeBoolean">If true, the expected type is <c>boolean</c>, and <c>int</c> otherwise.</param>
        /// <returns></returns>
        private Expression ConvertExpression(Expression expr, bool hasToBeBoolean = false)
        {
            // Expected boolean, found int -> Convert
            if (hasToBeBoolean && expr.Type == typeof(int))
            {
                expr = Expression.NotEqual(expr, Expression.Constant(0, typeof(int)));
            }
            // Expected int, found boolean -> Convert
            else if (!hasToBeBoolean && expr.Type == typeof(bool))
            {
                expr = Expression.Condition(expr, Expression.Constant(1, typeof(int)), Expression.Constant(0, typeof(int)));
            }

            return expr;
        }

        /// <summary>
        /// The regular expression for whitespace
        /// </summary>
        private static readonly Regex wsRegex = new Regex(@"\s+");

        /// <summary>
        /// The regular expression for integers
        /// </summary>
        private static readonly Regex numberRegex = new Regex(@"\d+");

        /// <summary>
        /// The regular expression for operators (except ?: which is processed separately)
        /// </summary>
        private static readonly Regex operatorRegex = new Regex(@"(==|!=|>=|<=|&&|\|\||[+\-*/<>%!])");

        /// <summary>
        /// Initializes a new instance of the <see cref="PluralExpression"/> class.
        /// </summary>
        /// <param name="expression">The plural expression as specified in the gettext format.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the expression is not a valid C plural expression as specified in the gettext format.
        /// </exception>
        public PluralExpression(string expression)
        {
            Source = expression;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return;
            }

            // The amount parameter `n`
            ParameterExpression numParam = Expression.Parameter(typeof(int), "n");

            expressionStack.Clear();
            operatorStack.Clear();

            // Run through the whole expression
            var pos = 0;
            while (pos < expression.Length)
            {
                // Skip whitespace
                var match = wsRegex.Match(expression, pos);
                if (match.Success && match.Index == pos)
                {
                    pos += match.Length;
                }

                // Variable
                if (expression[pos] == 'n')
                {
                    expressionStack.Push(numParam);
                    pos++;
                    continue;
                }

                // Number -> expression stack
                match = numberRegex.Match(expression, pos);
                if (match.Success && match.Index == pos)
                {
                    expressionStack.Push(Expression.Constant(int.Parse(match.Value), typeof(int)));

                    pos += match.Length;
                    continue;
                }

                // Operator -> operator stack (and process other operators)
                match = operatorRegex.Match(expression, pos);
                if (match.Success && match.Index == pos)
                {
                    Operation currentOperation = (Operation)match.Value;

                    // Evaluate all 
                    ProcessOperatorWhile(
                        o => o != "(" && o != "?" && (
                            (
                                currentOperation.IsLeftAssoc &&
                                currentOperation.Precedence <= ((Operation)o).Precedence
                            ) || (
                                currentOperation.Precedence < ((Operation)o).Precedence
                            )
                        )
                    );

                    operatorStack.Push(match.Value);

                    pos += match.Length;
                }
                else if (expression[pos] == '(')
                {
                    pos++;
                    operatorStack.Push("(");
                }
                else if (expression[pos] == ')')
                {
                    pos++;
                    ProcessOperatorWhile(o => operatorStack.Count > 0 && o != "(");
                    operatorStack.Pop();
                }
                else if (expression[pos] == '?')
                {
                    pos++;
                    ProcessOperatorWhile(o => operatorStack.Count > 0 && o != "(" && o != "?" && o != ":");
                    operatorStack.Push("?");
                }
                else if (expression[pos] == ':')
                {
                    pos++;

                    // Swap '?' with ':' (which is then handled as a normal operator)
                    var tmpStack = new Stack<string>();
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "?")
                    {
                        tmpStack.Push(operatorStack.Pop());
                    }
                    if (operatorStack.Count == 0)
                    {
                        throw new ArgumentException(string.Format("Found ':' without '?'"), "expression");
                    }
                    operatorStack.Pop();
                    operatorStack.Push(":");
                    while (tmpStack.Count > 0)
                    {
                        operatorStack.Push(tmpStack.Pop());
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Encountered invalid character {0}", expression[pos]), "expression");
                }
            }

            // Evalute remaining operators
            ProcessOperatorWhile(o => true);

            // Make sure the return type is int, so if it is a boolean, wrap it
            var e = ConvertExpression(expressionStack.Pop(), false);

            // Turn into delegate and compile
            Expr = Expression.Lambda<Func<int, int>>(e, numParam);
            Compiled = Expr.Compile();
        }

        /// <summary>
        /// Evaluates the plural expression for the specified n.
        /// </summary>
        /// <param name="n">The number to decide the plural form upon.</param>
        /// <returns>The index of the plural form to use.</returns>
        public int Evaluate(int n)
        {
            return Compiled(n);
        }
    }

    internal sealed class Operation
    {
        private readonly int precedence;
        private readonly string name;
        private readonly object operation;
        private readonly bool isLeftAssoc;
        private readonly bool[] argsAreBoolean;

        internal static readonly Operation Not = new Operation(7, Expression.Not, "Modulo", new bool[] { true }, false);

        internal static readonly Operation Multiplication = new Operation(6, Expression.Multiply, "Multiplication");
        internal static readonly Operation Division = new Operation(6, Expression.Divide, "Division");
        internal static readonly Operation Modulo = new Operation(6, Expression.Modulo, "Modulo");

        internal static readonly Operation Addition = new Operation(5, Expression.Add, "Addition");
        internal static readonly Operation Subtraction = new Operation(5, Expression.Subtract, "Subtraction");

        internal static readonly Operation LessThanOrEqual = new Operation(4, Expression.LessThanOrEqual, "LessThanOrEqual");
        internal static readonly Operation LessThan = new Operation(4, Expression.LessThan, "LessThan");
        internal static readonly Operation GreaterThanOrEqual = new Operation(4, Expression.GreaterThanOrEqual, "GreaterThanOrEqual");
        internal static readonly Operation GreaterThan = new Operation(4, Expression.GreaterThan, "GreaterThan");

        internal static readonly Operation Equal = new Operation(3, Expression.Equal, "Equal");
        internal static readonly Operation NotEqual = new Operation(3, Expression.NotEqual, "NotEqual");

        internal static readonly Operation And = new Operation(2, Expression.And, "And", new bool[] { true, true });
        internal static readonly Operation Or = new Operation(1, Expression.Or, "Or", new bool[] { true, true });

        internal static readonly Operation IfThenElse = new Operation(0, Expression.Condition, "IfThenElse", new bool[] { true, false, false }, false);

        private static readonly Dictionary<string, Operation> Operations = new Dictionary<string, Operation>
        {
            { "!", Not },
            { "*", Multiplication},
            { "/", Division },
            { "%", Modulo },
            { "+", Addition },
            { "-", Subtraction },
            { "<=", LessThanOrEqual },
            { "<", LessThan },
            { ">=", GreaterThanOrEqual },
            { ">", GreaterThan },
            { "==", Equal },
            { "!=", NotEqual },
            { "&&", And },
            { "||", Or },
            { ":", IfThenElse }
        };

        private Operation(int precedence, Func<Expression, Expression> operation, string name, bool[] argsAreBoolean = null, bool isLeftAssoc = true)
        {
            this.precedence = precedence;
            this.operation = operation;
            this.name = name;
            this.isLeftAssoc = isLeftAssoc;
            this.argsAreBoolean = argsAreBoolean ?? new bool[] { false };
        }

        private Operation(int precedence, Func<Expression, Expression, Expression> operation, string name, bool[] argsAreBoolean = null, bool isLeftAssoc = true)
        {
            this.precedence = precedence;
            this.operation = operation;
            this.name = name;
            this.isLeftAssoc = isLeftAssoc;
            this.argsAreBoolean = argsAreBoolean ?? new bool[] { false, false };
        }

        private Operation(int precedence, Func<Expression, Expression, Expression, Expression> operation, string name, bool[] argsAreBoolean = null, bool isLeftAssoc = true)
        {
            this.precedence = precedence;
            this.operation = operation;
            this.name = name;
            this.isLeftAssoc = isLeftAssoc;
            this.argsAreBoolean = argsAreBoolean ?? new bool[] { false, false, false };
        }

        /// <summary>
        /// Gets the precedence.
        /// </summary>
        /// <value>
        /// The precedence.
        /// </value>
        public int Precedence
        {
            get { return precedence; }
        }

        /// <summary>
        /// Gets the number of operands/arguments.
        /// </summary>
        /// <value>
        /// The number of operands/arguments.
        /// </value>
        public int NumArgs
        {
            get { return argsAreBoolean.Length; }
        }

        /// <summary>
        /// Gets a value indicating whether this operator is left associative.
        /// </summary>
        /// <value>
        /// <c>true</c> if this operator is left associative;
        /// <c>true</c> if this operator is right associative.
        /// </value>
        public bool IsLeftAssoc
        {
            get { return isLeftAssoc; }
        }

        /// <summary>
        /// Turns an operator string into the corresponding operation.
        /// </summary>
        /// <param name="operation">The operator (e.g. "!=" or "+").</param>
        /// <returns>The operation for the operator</returns>
        /// <exception cref="System.InvalidCastException">Thrown if the string is not a valid operator.</exception>
        public static explicit operator Operation(string operation)
        {
            Operation result;

            if (Operations.TryGetValue(operation, out result))
            {
                return result;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        /// <summary>
        /// Applies the unary operation to the argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>An expression reflecting the application of the operation to the argument.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the operation is not applicable to the argument.</exception>
        public Expression Apply(Expression arg)
        {
            if (NumArgs == 1)
            {
                return ((Func<Expression, Expression>)operation)(arg);
            }
            else
            {
                throw new InvalidOperationException(String.Format("This operation takes {0} operators, got 1.", NumArgs));
            }
        }

        /// <summary>
        /// Applies the binary operation to the arguments.
        /// </summary>
        /// <param name="left">The first argument.</param>
        /// <param name="right">The second argument.</param>
        /// <returns>An expression reflecting the application of the operation to the arguments.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the operation is not applicable to the arguments.</exception>
        public Expression Apply(Expression left, Expression right)
        {
            if (NumArgs == 2)
            {
                return ((Func<Expression, Expression, Expression>)operation)(left, right);
            }
            else
            {
                throw new InvalidOperationException(String.Format("This operation takes {0} operators, got 2.", NumArgs));
            }
        }

        /// <summary>
        /// Applies the ternary operation to the arguments.
        /// </summary>
        /// <param name="first">The first argument.</param>
        /// <param name="second">The second argument.</param>
        /// <param name="third">The third argument.</param>
        /// <returns>An expression reflecting the application of the operation to the arguments.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the operation is not applicable to the arguments.</exception>
        public Expression Apply(Expression first, Expression second, Expression third)
        {
            if (NumArgs == 3)
            {
                return ((Func<Expression, Expression, Expression, Expression>)operation)(first, second, third);
            }
            else
            {
                throw new InvalidOperationException(String.Format("This operation takes {0} operators, got 3.", NumArgs));
            }
        }

        /// <summary>
        /// Determines whether the specified operator is defined.
        /// </summary>
        /// <param name="operation">The operator.</param>
        /// <returns>
        ///   <c>true</c> if the specified operator is defined; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDefined(string operation)
        {
            return Operations.ContainsKey(operation);
        }

        /// <summary>
        /// Determines whether the i-th argument of the operation is supposed to be boolean.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns>
        ///   <c>true</c> if the i-th argument of the operation is supposed to be boolean;
        ///   <c>false</c> if the i-th argument of the operation is supposed to be int.
        /// </returns>
        public bool IsBooleanArgument(int i)
        {
            return argsAreBoolean[i];
        }
    }
}
