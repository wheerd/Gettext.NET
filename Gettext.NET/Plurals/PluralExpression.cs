using System;

namespace GettextDotNet.Plurals
{
    /// <summary>
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
        /// The plural expression as compiled function
        /// </summary>
        private readonly Func<int, int> _compiled;

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
                throw new ArgumentException("Empty or null expression", "expression");
            }

            _compiled = new PluralExpressionParser(expression).Parse();
        }

        /// <summary>
        /// Evaluates the plural expression for the specified n.
        /// </summary>
        /// <param name="n">The number to decide the plural form upon.</param>
        /// <returns>The index of the plural form to use.</returns>
        public int Evaluate(int n)
        {
            return _compiled(n);
        }

        public override string ToString()
        {
            return Source;
        }
    }
}
