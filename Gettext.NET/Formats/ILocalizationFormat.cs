using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GettextDotNET.Formats
{
    public interface ILocalizationFormat
    {
        void Write(Localization localization, Stream stream, bool writeComments = false);
        void Read(Localization localization, Stream stream, bool loadComments = false);
    }    
}
