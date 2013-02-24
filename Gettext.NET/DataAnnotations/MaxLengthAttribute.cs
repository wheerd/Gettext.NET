using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class MaxLengthAttribute : System.ComponentModel.DataAnnotations.MaxLengthAttribute
    {
        public MaxLengthAttribute(int length) : base(length)
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
            var msg = Internationalization.GetText(ErrorMessage ?? "The field {0} must be a string or array type with a maximum length of '{1}'.");

            return string.Format(msg, _displayName, Length);
        }
    }
}
