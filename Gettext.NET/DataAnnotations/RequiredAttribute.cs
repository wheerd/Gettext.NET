using System;
using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class RequiredAttribute : System.ComponentModel.DataAnnotations.RequiredAttribute
    {
        private string _displayName;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            _displayName = validationContext.DisplayName;

            return base.IsValid(value, validationContext);
        }

        public override string FormatErrorMessage(string name)
        {
            var msg = Internationalization.GetText(ErrorMessage ?? "The {0} field is required.");

            return string.Format(msg, _displayName);
        }
    }
}
