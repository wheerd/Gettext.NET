using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettextDotNet.MessageExtractor
{
    public static class Default
    {
        public readonly static string[] Messages = new[]
        {
            "'{0}' and '{1}' do not match.",
            "The field {0} is invalid.",
            "The field {0} must be a string or array type with a maximum length of '{1}'.",
            "The field {0} must be a string or array type with a minimum length of '{1}'.",
            "The field {0} must be between {1} and {2}.",
            "The field {0} must match the regular expression '{1}'.",
            "The field {0} must be a string with a maximum length of {1}.",
            "The {0} field is required."
        };
    }
}
