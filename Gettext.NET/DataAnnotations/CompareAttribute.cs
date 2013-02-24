using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class CompareAttribute : System.ComponentModel.DataAnnotations.CompareAttribute
    {
        public CompareAttribute(string otherProperty) : base(otherProperty)
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
            var msg = Internationalization.GetText(ErrorMessage ?? "'{0}' and '{1}' do not match.");

            return string.Format(msg, _displayName, OtherPropertyDisplayName);
        }
    }
}
