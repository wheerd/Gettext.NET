using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GettextDotNET.Formats;

namespace GettextDotNET
{
    /// <summary>
    /// A collection of localized/translated strings
    /// </summary>
    public class Localization
    {
        /// <summary>
        /// Gets or sets the translation messages.
        /// </summary>
        /// <value>
        /// The messages.
        /// </value>
        public Dictionary<string, Message> Messages { get; set; }

        /// <summary>
        /// Gets or sets the localization meta information (headers).
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> class.
        /// </summary>
        public Localization()
        {
            Messages = new Dictionary<string, Message>();
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Loads translated strings from a file.
        /// </summary>
        /// <typeparam name="Format">The type of the file format (.po/.mo).</typeparam>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="loadComments">Comments and meta data are only loaded when set to true.</param>
        public void LoadFromFile<Format>(string fileName, bool loadComments = false)
            where Format : ILocalizationFormat, new()
        {
            using (var stream = File.OpenRead(fileName))
            {
                new Format().Read(this, stream, loadComments);
            }
        }

        /// <summary>
        /// Saves the translated strings to a file.
        /// </summary>
        /// <typeparam name="Format">The type of the file format (.po/.mo).</typeparam>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="writeComments">Comments and meta data are only written when set to true.</param>
        public void SaveToFile<Format>(string fileName, bool writeComments = false)
            where Format : ILocalizationFormat, new()
        {
            using (var stream = File.Create(fileName))
            {
                new Format().Write(this, stream, writeComments);
            }
        }

        /// <summary>
        /// Formats the translated strings in the specified format and returns it as string.
        /// </summary>
        /// <typeparam name="Format">The type of the format.</typeparam>
        /// <param name="writeComments">Comments and meta data are only written when set to true.</param>
        /// <returns>
        /// A formatted version of this localization.
        /// </returns>
        public string ToString<Format>(bool writeComments = false)
            where Format : ILocalizationFormat, new()
        {
            var stream = new MemoryStream();

            new Format().Write(this, stream, writeComments);

            return System.Text.Encoding.Default.GetString(stream.GetBuffer());
        }

        /// <summary>
        /// Loads translated strings from string in a specific format.
        /// </summary>
        /// <typeparam name="Format">The type of the format.</typeparam>
        /// <param name="str">The string used for loading.</param>
        /// <param name="loadComments">Comments and meta data are only loaded when set to true.</param>
        public void LoadFromString<Format>(string str, bool loadComments = false)
            where Format : ILocalizationFormat, new()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(str);
                writer.Flush();
                stream.Position = 0;

                new Format().Read(this, stream, loadComments);
            }
        }
    }
}
