using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class RegularExpressionAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute
    {
        public RegularExpressionAttribute(string pattern)
            : base(pattern)
        {

        }

        private string _displayName;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            _displayName = validationContext.DisplayName;

            return base.IsValid(value, validationContext);
        }

        public override string FormatErrorMessage(string name)
        {
            var msg = Internationalization.GetText(ErrorMessage ?? "The field {0} must match the regular expression '{1}'.");

            return string.Format(msg, _displayName, Pattern);
        }
    }
}
