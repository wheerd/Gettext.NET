using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GettextDotNet.Plurals
{
    internal class Operation
    {
        private readonly int _precedence;
        private readonly bool _isLeftAssociative;

        internal static readonly Operation Not = new UnaryOperation(7, Expression.Not, ExpressionType.Boolean, false);

        internal static readonly Operation Multiplication = new BinaryOperation(6, Expression.Multiply, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation Division = new BinaryOperation(6, Expression.Divide, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation Modulo = new BinaryOperation(6, Expression.Modulo, ExpressionType.Integer, ExpressionType.Integer, true);

        internal static readonly Operation Addition = new BinaryOperation(5, Expression.Add, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation Subtraction = new BinaryOperation(5, Expression.Subtract, ExpressionType.Integer, ExpressionType.Integer, true);

        internal static readonly Operation LessThanOrEqual = new BinaryOperation(4, Expression.LessThanOrEqual, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation LessThan = new BinaryOperation(4, Expression.LessThan, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation GreaterThanOrEqual = new BinaryOperation(4, Expression.GreaterThanOrEqual, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation GreaterThan = new BinaryOperation(4, Expression.GreaterThan, ExpressionType.Integer, ExpressionType.Integer, true);

        internal static readonly Operation Equal = new BinaryOperation(3, Expression.Equal, ExpressionType.Integer, ExpressionType.Integer, true);
        internal static readonly Operation NotEqual = new BinaryOperation(3, Expression.NotEqual, ExpressionType.Integer, ExpressionType.Integer, true);

        internal static readonly Operation And = new BinaryOperation(2, Expression.And, ExpressionType.Boolean, ExpressionType.Boolean, true);
        internal static readonly Operation Or = new BinaryOperation(1, Expression.Or, ExpressionType.Boolean, ExpressionType.Boolean, true);

        internal static readonly Operation IfThenElse = new TrinaryOperation(0, Expression.Condition, ExpressionType.Boolean, ExpressionType.Integer, ExpressionType.Integer, false);

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

        protected Operation(int precedence, bool isLeftAssociative)
        {
            _precedence = precedence;
            _isLeftAssociative = isLeftAssociative;
        }

        /// <summary>
        /// Gets the precedence.
        /// </summary>
        /// <value>
        /// The precedence.
        /// </value>
        public int Precedence
        {
            get { return _precedence; }
        }

        /// <summary>
        /// Gets a value indicating whether this operator is left associative.
        /// </summary>
        /// <value>
        /// <c>true</c> if this operator is left associative;
        /// <c>true</c> if this operator is right associative.
        /// </value>
        public bool IsLeftAssociative
        {
            get { return _isLeftAssociative; }
        }

        /// <summary>
        /// Turns an operator string into the corresponding operation.
        /// </summary>
        /// <param name="operation">The operator (e.g. "!=" or "+").</param>
        /// <returns>The operation for the operator</returns>
        /// <exception cref="System.InvalidCastException">Thrown if the string is not a valid operator.</exception>
        public static Operation FromString(string operation)
        {
            Operation result;

            if (Operations.TryGetValue(operation, out result))
            {
                return result;
            }
            throw new InvalidCastException();
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

        public bool ShouldBeProcessedBefore(Operation other)
        {
            return (IsLeftAssociative && Precedence <= other.Precedence) || (Precedence < other.Precedence);
        }
    }

    internal sealed  class UnaryOperation : Operation
    {
        private readonly Func<Expression, Expression> _operation;
        public readonly ExpressionType ArgumentType;

        public UnaryOperation(int precedence, Func<Expression, Expression> operation, ExpressionType argumentType, bool isLeftAssociative) : base(precedence, isLeftAssociative)
        {
            _operation = operation;
            ArgumentType = argumentType;
        }

        public Expression Apply(Expression arg)
        {
            return _operation(arg);
        }
    }

    internal sealed class BinaryOperation : Operation
    {
        private readonly Func<Expression, Expression, Expression> _operation;
        public readonly ExpressionType ArgumentType1;
        public readonly ExpressionType ArgumentType2;

        public BinaryOperation(int precedence, Func<Expression, Expression, Expression> operation, ExpressionType argumentType1, ExpressionType argumentType2, bool isLeftAssociative)
            : base(precedence, isLeftAssociative)
        {
            _operation = operation;
            ArgumentType1 = argumentType1;
            ArgumentType2 = argumentType2;
        }

        public Expression Apply(Expression arg1, Expression arg2)
        {
            return _operation(arg1, arg2);
        }
    }

    internal sealed class TrinaryOperation : Operation
    {
        private readonly Func<Expression, Expression, Expression, Expression> _operation;
        public readonly ExpressionType ArgumentType1;
        public readonly ExpressionType ArgumentType2;
        public readonly ExpressionType ArgumentType3;

        public TrinaryOperation(int precedence, Func<Expression, Expression, Expression, Expression> operation, ExpressionType argumentType1, ExpressionType argumentType2, ExpressionType argumentType3, bool isLeftAssociative)
            : base(precedence, isLeftAssociative)
        {
            _operation = operation;
            ArgumentType1 = argumentType1;
            ArgumentType2 = argumentType2;
            ArgumentType3 = argumentType3;
        }

        public Expression Apply(Expression arg1, Expression arg2, Expression arg3)
        {
            return _operation(arg1, arg2, arg3);
        }
    }
}