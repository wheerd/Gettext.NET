using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class StringLengthAttribute : System.ComponentModel.DataAnnotations.StringLengthAttribute
    {
        public StringLengthAttribute(int maximumLength)
            : base(maximumLength)
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
            var msg = Internationalization.GetText(ErrorMessage ?? "The field {0} must be a string with a maximum length of {1}.");

            return string.Format(msg, _displayName, MaximumLength, MinimumLength);
        }
    }
}
