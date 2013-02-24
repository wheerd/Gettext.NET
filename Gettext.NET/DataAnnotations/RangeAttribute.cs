using System;
using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute
    {
        public RangeAttribute(double minimum, double maximum) : base(minimum, maximum)
        {

        }

        public RangeAttribute(int minimum, int maximum)
            : base(minimum, maximum)
        {

        }

        public RangeAttribute(Type type, string minimum, string maximum)
            : base(type, minimum, maximum)
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
            var msg = Internationalization.GetText(ErrorMessage ?? "The field {0} must be between {1} and {2}.");

            return string.Format(msg, _displayName, Minimum, Maximum);
        }
    }
}
