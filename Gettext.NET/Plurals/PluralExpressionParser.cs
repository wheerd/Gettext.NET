using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace GettextDotNet.Plurals
{
    internal enum ExpressionType
    {
        Integer, Boolean
    }

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
    /// </summary>
    public class PluralExpressionParser
    {
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+");

        private static readonly Regex NumberRegex = new Regex(@"\d+");

        private static readonly Regex OperatorRegex = new Regex(@"(==|!=|>=|<=|&&|\|\||[+\-*/<>%!])");

        private readonly String _expression;

        private readonly Stack<Expression> _expressionStack = new Stack<Expression>();

        private readonly ParameterExpression _numParam = Expression.Parameter(typeof (int), "n");

        private readonly Stack<string> _operatorStack = new Stack<string>();

        private int _position;

        public PluralExpressionParser(String expression)
        {
            _expression = expression;
        }

        public Func<int, int> Parse()
        {
            ResetStacksAndPosition();
            ParseExpression();
            return ConvertAndCompileExpression();
        }

        private void ResetStacksAndPosition()
        {
            _position = 0;
            _expressionStack.Clear();
            _operatorStack.Clear();
        }

        private Func<int, int> ConvertAndCompileExpression()
        {
            var expression = ConvertExpressionType(_expressionStack.Pop(), ExpressionType.Integer);
            return Expression.Lambda<Func<int, int>>(expression, _numParam).Compile();
        }

        private void ParseExpression()
        {
            while (_position < _expression.Length)
            {
                SkipWhitespaceIfAny();
                if (TryToParseNextToken()) continue;
                throw new ParseError(string.Format("Encountered invalid character {0}", _expression[_position]));
            }
            ProcessRemainingOperators();
        }

        private bool TryToParseNextToken()
        {
            if (TryToParseVariable())
                return true;
            if (TryToParseNumber())
                return true;
            if (TryToParseOperator())
                return true;
            if (TryToParseOpeningBrace())
                return true;
            if (TryToParseClosingBrace())
                return true;
            if (TryToParseQuestionMark())
                return true;
            if (TryToParseColon())
                return true;
            return false;
        }

        private bool TryToParseOperator()
        {
            var match = OperatorRegex.Match(_expression, _position);
            if (IsInvalidMatch(match))
                return false;
            ParseOperator(match.Value);
            return true;
        }

        private bool IsInvalidMatch(Match match)
        {
            return !match.Success || match.Index != _position;
        }

        private bool TryToParseColon()
        {
            if (_expression[_position] != ':')
                return false;
            _position++;
            try
            {
                _operatorStack.ReplaceFirstOccurence("?", ":");
            }
            catch (ArgumentException)
            {
                throw new ParseError("Found ':' without '?'");
            }
            return true;
        }

        private bool TryToParseQuestionMark()
        {
            if (_expression[_position] != '?')
                return false;
            _position++;
            ProcessStackOperatorsUntilTopmostIs("(", "?", ":");
            _operatorStack.Push("?");
            return true;
        }

        private bool TryToParseClosingBrace()
        {
            if (_expression[_position] != ')')
                return false;
            _position++;
            ProcessStackOperatorsUntilTopmostIs("(");
            _operatorStack.Pop();
            return true;
        }

        private bool TryToParseOpeningBrace()
        {
            if (_expression[_position] != '(') return false;
            _position++;
            _operatorStack.Push("(");
            return true;
        }

        private void ParseOperator(string @operator)
        {
            Operation currentOperation = Operation.FromString(@operator);
            ProcessStackOperatorsConsinderingPrecedenceExcept(currentOperation, "(", "?");
            _operatorStack.Push(@operator);
            _position += @operator.Length;
        }

        private bool TryToParseNumber()
        {
            var match = NumberRegex.Match(_expression, _position);
            if (IsInvalidMatch(match)) return false;
            _expressionStack.Push(MakeConstantExpression(match));
            _position += match.Length;
            return true;
        }

        private static ConstantExpression MakeConstantExpression(Match match)
        {
            return Expression.Constant(int.Parse(match.Value), typeof (int));
        }

        private bool TryToParseVariable()
        {
            if (_expression[_position] != 'n') return false;
            _expressionStack.Push(_numParam);
            _position++;
            return true;
        }

        private void SkipWhitespaceIfAny()
        {
            var match = WhitespaceRegex.Match(_expression, _position);
            if (!IsInvalidMatch(match))
                _position += match.Length;
        }

        private void ProcessStackOperatorsUntilTopmostIs(params string[] forbiddenOperators)
        {
            while (_operatorStack.Count > 0 && !forbiddenOperators.Contains(_operatorStack.Peek()))
            {
                ProcessOperation(Operation.FromString(_operatorStack.Pop()));
            }
        }

        private void ProcessStackOperatorsConsinderingPrecedenceExcept(Operation currentOperation, params string[] forbiddenOperators)
        {
            while (_operatorStack.Count > 0)
            {
                var @operator = _operatorStack.Peek();
                if (forbiddenOperators.Contains(@operator)) break;
                var operation = Operation.FromString(@operator);
                if (!currentOperation.ShouldBeProcessedBefore(operation)) break;
                _operatorStack.Pop();
                ProcessOperation(operation);
            }
        }

        private void ProcessRemainingOperators()
        {
            while (_operatorStack.Count > 0)
            {
                ProcessOperation(Operation.FromString(_operatorStack.Pop()));
            }
        }

        private void ProcessOperation(Operation operation)
        {
            if (operation is UnaryOperation)
            {
                ProcessUnaryOperation((UnaryOperation)operation);
            }
            else if (operation is BinaryOperation)
            {
                ProcessBinaryOperation((BinaryOperation)operation);
            }
            else if (operation is TrinaryOperation)
            {
                ProcessTrinaryOperation((TrinaryOperation)operation);
            }
            else
            {
                throw new Exception("Unknown operation type: " + operation.GetType().Name);
            }
        }

        private void ProcessTrinaryOperation(TrinaryOperation operation)
        {
            var third = ConvertExpressionType(_expressionStack.Pop(), operation.ArgumentType3);
            var second = ConvertExpressionType(_expressionStack.Pop(), operation.ArgumentType2);
            var first = ConvertExpressionType(_expressionStack.Pop(), operation.ArgumentType1);

            _expressionStack.Push(operation.Apply(first, second, third));
        }

        private void ProcessBinaryOperation(BinaryOperation operation)
        {
            var right = ConvertExpressionType(_expressionStack.Pop(), operation.ArgumentType2);
            var left = ConvertExpressionType(_expressionStack.Pop(), operation.ArgumentType1);

            _expressionStack.Push(operation.Apply(left, right));
        }

        private void ProcessUnaryOperation(UnaryOperation operation)
        {
            var arg = ConvertExpressionType(_expressionStack.Pop(), operation.ArgumentType);

            _expressionStack.Push(operation.Apply(arg));
        }

        private Expression ConvertExpressionType(Expression expr, ExpressionType targetExpressionType)
        {
            if (targetExpressionType == ExpressionType.Boolean && expr.Type == typeof(int))
                return ConvertIntegerToBooleanExpression(expr);
            if (targetExpressionType == ExpressionType.Integer && expr.Type == typeof(bool))
                return ConvertBooleanToIntegerExpression(expr);

            return expr;
        }

        private static BinaryExpression ConvertIntegerToBooleanExpression(Expression expr)
        {
            return Expression.NotEqual(expr, Expression.Constant(0, typeof (int)));
        }

        private static ConditionalExpression ConvertBooleanToIntegerExpression(Expression expr)
        {
            return Expression.Condition(expr, Expression.Constant(1, typeof (int)), Expression.Constant(0, typeof (int)));
        }

        public class ParseError : Exception
        {
            public ParseError(string message) : base(message)
            {
            }
        }
    }
}