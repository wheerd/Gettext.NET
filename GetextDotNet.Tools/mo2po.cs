using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GettextDotNet.Formats;

namespace GetextDotNet.Tools
{
    public class mo2po : FormatConverter<MOFormat,POFormat>
    {
        static void Main(string[] args)
        {
            new mo2po().Run(args);
        }
    }
}
