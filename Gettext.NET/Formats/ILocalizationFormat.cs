using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GettextDotNet.Formats
{
    /// <summary>
    /// Provides access to translated strings in a specific format and offers methods to read/write this format.
    /// </summary>
    public interface ILocalizationFormat
    {
        /// <summary>
        /// Gets the file extensions supported by this format.
        /// </summary>
        /// <value>
        /// The file extensions supported by this format.
        /// </value>
        string[] FileExtensions { get; }

        /// <summary>
        /// Dumps the specified localization to the stream in this format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="writeComments">If set to <c>true</c>, comments will be included in the ouput.</param>
        void Write(Localization localization, Stream stream, bool writeComments = false);

        /// <summary>
        /// Attempts to read messages and headers from the stream in the specified format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="loadComments">If set to <c>true</c>, comments will be loaded from the stream.</param>
        void Read(Localization localization, Stream stream, bool loadComments = false);
    }    
}
