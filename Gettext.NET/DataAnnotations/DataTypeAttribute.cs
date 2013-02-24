using System.ComponentModel.DataAnnotations;

namespace GettextDotNet.DataAnnotations
{
    public class DataTypeAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public DataTypeAttribute(DataType dataType) : base(dataType)
        {

        }

        public DataTypeAttribute(string customDataType) : base(customDataType)
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
            var msg = Internationalization.GetText(ErrorMessage ?? "The field {0} is invalid.");

            return string.Format(msg, _displayName, DataType, CustomDataType);
        }
    }
}
