using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettextDotNet.DataAnnotations
{
    public class DisplayNameAttribute : System.ComponentModel.DisplayNameAttribute
    {
        public DisplayNameAttribute(string displayName) : base(displayName)
        {

        }

        public override string DisplayName
        {
            get
            {
                return Internationalization.GetText(DisplayNameValue, context: "DisplayName");
            }
        }
    }
}
