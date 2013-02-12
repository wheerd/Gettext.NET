using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace GettextDotNet
{
    public class PluralExpression
    {
        public readonly string Source;
        public readonly Expression<Func<int, int>> Expr;
        public readonly Func<int, int> Compiled;

        public Stack<Expression> expressionStack = new Stack<Expression>();
        public Stack<string> operatorStack = new Stack<string>();

        private void EvaluateWhile(Func<bool> condition)
        {
            while (condition())
            {
                var op = (Operation)operatorStack.Pop();

                if (op.NumArgs == 1)
                {
                    var arg = expressionStack.Pop();

                    expressionStack.Push(op.Apply(arg));
                }
                else if (op.NumArgs == 2)
                {
                    var right = expressionStack.Pop();
                    var left = expressionStack.Pop();

                    expressionStack.Push(op.Apply(left, right));
                }
                else if (op.NumArgs == 3)
                {
                    var third = expressionStack.Pop();
                    var right = expressionStack.Pop();
                    var left = expressionStack.Pop();

                    expressionStack.Push(op.Apply(left, right, third));
                }
            }
        }

        public PluralExpression(string expression)
        {
            Source = expression;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return;
            }

            ParameterExpression numParam = Expression.Parameter(typeof(int), "n");

            expressionStack.Clear();
            operatorStack.Clear();

            var wsRegex = new Regex(@"\s+");
            var numberRegex = new Regex(@"\d+");
            var operatorRegex = new Regex(@"(==|!=|>=|<=|&&|\|\||[+\-*/<>%!])");
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

                // Number
                match = numberRegex.Match(expression, pos);
                if (match.Success && match.Index == pos)
                {
                    int d = int.Parse(match.Value);

                    expressionStack.Push(Expression.Constant(d, typeof(int)));

                    pos += match.Length;
                    continue;
                }

                // Operator
                match = operatorRegex.Match(expression, pos);
                if (match.Success && match.Index == pos)
                {
                    Operation currentOperation = (Operation)match.Value;

                    EvaluateWhile(() => operatorStack.Count > 0 && operatorStack.Peek() != "(" && operatorStack.Peek() != "?" &&
                                  ((currentOperation.IsLeftAssoc && currentOperation.Precedence <= ((Operation)operatorStack.Peek()).Precedence)
                                  || currentOperation.Precedence < ((Operation)operatorStack.Peek()).Precedence));

                    operatorStack.Push(match.Value);

                    pos += match.Length;
                    continue;
                }

                if (expression[pos] == '(')
                {
                    pos++;
                    operatorStack.Push("(");
                    continue;
                }

                if (expression[pos] == ')')
                {
                    pos++;
                    EvaluateWhile(() => operatorStack.Count > 0 && operatorStack.Peek() != "(");
                    operatorStack.Pop();
                    continue;
                }

                if (expression[pos] == '?')
                {
                    pos++;
                    EvaluateWhile(() => operatorStack.Count > 0 && operatorStack.Peek() != "(" && operatorStack.Peek() != "?" && operatorStack.Peek() != ":");
                    operatorStack.Push("?");
                    continue;
                }

                if (expression[pos] == ':')
                {
                    pos++;

                    // Swap ? with : (which is then handled as a normal operator)
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

                    continue;
                }

                throw new ArgumentException(string.Format("Encountered invalid character {0}", expression[pos]), "expression");
            }

            // Evalute remaining operators
            EvaluateWhile(() => operatorStack.Count > 0);

            // Make sure the return type is int, so if it is a boolean, wrap it
            var e = expressionStack.Pop();
            if (e.Type == typeof(System.Boolean))
            {
                e = Expression.Condition(e, Expression.Constant(1, typeof(int)), Expression.Constant(0, typeof(int)));
            }

            // Turn into delegate
            Expr = Expression.Lambda<Func<int, int>>(e, numParam);
            Compiled = Expr.Compile();
        }
    }

    internal sealed class Operation
    {
        private readonly int precedence;
        private readonly string name;
        private readonly object operation;
        private readonly bool isLeftAssoc;
        private readonly int numArgs;

        public static readonly Operation Not = new Operation(7, Expression.Not, "Modulo", false);
        public static readonly Operation Multiplication = new Operation(6, Expression.Multiply, "Multiplication");
        public static readonly Operation Division = new Operation(6, Expression.Divide, "Division");
        public static readonly Operation Modulo = new Operation(6, Expression.Modulo, "Modulo");
        public static readonly Operation Addition = new Operation(5, Expression.Add, "Addition");
        public static readonly Operation Subtraction = new Operation(5, Expression.Subtract, "Subtraction");
        public static readonly Operation LessThanOrEqual = new Operation(4, Expression.LessThanOrEqual, "LessThanOrEqual");
        public static readonly Operation LessThan = new Operation(4, Expression.LessThan, "LessThan");
        public static readonly Operation GreaterThanOrEqual = new Operation(4, Expression.GreaterThanOrEqual, "GreaterThanOrEqual");
        public static readonly Operation GreaterThan = new Operation(4, Expression.GreaterThan, "GreaterThan");
        public static readonly Operation Equal = new Operation(3, Expression.Equal, "Equal");
        public static readonly Operation NotEqual = new Operation(3, Expression.NotEqual, "NotEqual");
        public static readonly Operation And = new Operation(2, Expression.And, "And");
        public static readonly Operation Or = new Operation(1, Expression.Or, "Or");
        public static readonly Operation IfThenElse = new Operation(0, Expression.Condition, "IfThenElse", false);

        private static readonly Dictionary<string, Operation> Operations = new Dictionary<string, Operation>
        {
            { "+", Addition },
            { "-", Subtraction },
            { "*", Multiplication},
            { "/", Division },
            { "%", Modulo },
            { "!=", NotEqual },
            { "<=", LessThanOrEqual },
            { "==", Equal },
            { ">=", GreaterThanOrEqual },
            { "<", LessThan },
            { ">", GreaterThan },
            { "!", Not },
            { "||", Or },
            { "&&", And },
            { ":", IfThenElse }
        };

        private Operation(int precedence, Func<Expression, Expression> operation, string name, bool isLeftAssoc = true)
        {
            this.precedence = precedence;
            this.operation = operation;
            this.name = name;
            this.isLeftAssoc = isLeftAssoc;
            this.numArgs = 1;
        }

        private Operation(int precedence, Func<Expression, Expression, Expression> operation, string name, bool isLeftAssoc = true)
        {
            this.precedence = precedence;
            this.operation = operation;
            this.name = name;
            this.isLeftAssoc = isLeftAssoc;
            this.numArgs = 2;
        }

        private Operation(int precedence, Func<Expression, Expression, Expression, Expression> operation, string name, bool isLeftAssoc = true)
        {
            this.precedence = precedence;
            this.operation = operation;
            this.name = name;
            this.isLeftAssoc = isLeftAssoc;
            this.numArgs = 3;
        }

        public int Precedence
        {
            get { return precedence; }
        }

        public int NumArgs
        {
            get { return numArgs; }
        }

        public bool IsLeftAssoc
        {
            get { return isLeftAssoc; }
        }

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

        public Expression Apply(Expression arg)
        {
            if (numArgs == 1)
            {
                return ((Func<Expression, Expression>)operation)(arg);
            }
            else
            {
                throw new InvalidOperationException(String.Format("This operation takes {0} operators, got 1.", numArgs));
            }
        }

        public Expression Apply(Expression left, Expression right)
        {
            if (numArgs == 2)
            {
                return ((Func<Expression, Expression, Expression>)operation)(left, right);
            }
            else
            {
                throw new InvalidOperationException(String.Format("This operation takes {0} operators, got 2.", numArgs));
            }
        }

        public Expression Apply(Expression left, Expression right, Expression third)
        {
            if (numArgs == 3)
            {
                if (right.Type == typeof(int) && third.Type == typeof(bool))
                {
                    third = Expression.Condition(third, Expression.Constant(1, typeof(int)), Expression.Constant(0, typeof(int)));
                }
                else if (right.Type == typeof(bool) && third.Type == typeof(int))
                {
                    right = Expression.Condition(right, Expression.Constant(1, typeof(int)), Expression.Constant(0, typeof(int)));
                }

                if (this.name.Equals("IfThenElse") && left.Type == typeof(int))
                {
                    left = Expression.NotEqual(left, Expression.Constant(0, typeof(int)));
                }

                return ((Func<Expression, Expression, Expression, Expression>)operation)(left, right, third);
            }
            else
            {
                throw new InvalidOperationException(String.Format("This operation takes {0} operators, got 3.", numArgs));
            }
        }

        public static bool IsDefined(string operation)
        {
            return Operations.ContainsKey(operation);
        }
    }
}
