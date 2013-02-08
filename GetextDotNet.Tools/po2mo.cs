using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GettextDotNet.Formats;

namespace GetextDotNet.Tools
{
    public class po2mo : FormatConverter<POFormat, MOFormat>
    {
        static void Main(string[] args)
        {
            new po2mo().Run(args);
        }
    }
}
