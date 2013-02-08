using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettextDotNet.Formats
{
    /// <summary>
    /// Provides functions for using files in the .mo format.
    /// </summary>
    public class MOFormat : ILocalizationFormat
    {
        public string[] FileExtensions { get { return new string[] { ".mo" }; } }

        /// <summary>
        /// Dumps the specified localization to the stream in the .mo format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="writeComments">If set to <c>true</c>, comments will be included in the ouput.</param>
        public void Write(Localization localization, Stream stream, bool writeComments = false)
        {
            // BinaryWriter to the MO file
            using (var writer = new BinaryWriter(stream))
            {
                // Write magic bytes and version number
                writer.Write(0x950412de);
                writer.Write(0u);

                uint n = (uint)(localization.Count + 1);

                writer.Write(n);            // Write number of strings
                writer.Write(28);           // Start offset for the original string table (right after the header)
                writer.Write(28 + n * 8);   // Start offset for the translated string table (right after the previous table)
                writer.Write(0u);           // No hash table
                writer.Write(0u);           // No hash table

                // Used for the actual strings to append at the end of the file
                var sb = new StringBuilder();

                // Start offset after the tables
                uint pos = 28 + 16 * n;

                // Format headers as message string
                var headers = String.Join("\n", localization.Headers.Select(h => String.Format("{0}: {1}", h.Key, h.Value)));

                // Write the original table entry for the header string (empty id)
                writer.Write(0u);
                writer.Write(pos);
                pos++;
                sb.Append("\x00");

                // Sort messages lexicographically
                var messages = localization.GetMessages().OrderBy(
                    v => (v.Context != null ? v.Context + "\x04" : "") + v.Id
                ).ToArray();

                // Write table entry for the original strings (including context and plural) and append the strings to the end
                foreach (var message in messages)
                {
                    // Prepend context separated by \x04
                    var key = String.IsNullOrEmpty(message.Context) ? message.Id : message.Context + "\x04" + message.Id;

                    // Append plural separated by \x00
                    if (!String.IsNullOrEmpty(message.Plural))
                    {
                        key += "\x00" + message.Plural;
                    }

                    // Write table entry (size + offset)
                    writer.Write((uint)key.Length);
                    writer.Write(pos);

                    // Advance the position
                    pos += (uint)(key.Length + 1);

                    // Add string add the end
                    sb.Append(key + "\x00");
                }

                // Write the table entry for the header and add the header string to the end
                writer.Write((uint)headers.Length);
                writer.Write(pos);
                pos += (uint)headers.Length + 1;
                sb.Append(headers + "\x00");

                // Write table entry for the translated strings and append the strings to the end
                foreach (var message in messages)
                {
                    // translations are spearated by \x00
                    var translations = String.Join("\x00", message.Translations);

                    // Write table entry (size + offset)
                    writer.Write((uint)translations.Length);
                    writer.Write(pos);

                    // Advance the position
                    pos += (uint)(translations.Length + 1);

                    // Add string add the end
                    sb.Append(translations + "\x00");
                }

                // Append the collected strings
                var data = Encoding.ASCII.GetBytes(sb.ToString());
                writer.BaseStream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Attempts to read messages and headers from the stream in the .mo format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="loadComments">If set to <c>true</c>, comments will be loaded from the stream.</param>
        /// <exception cref="System.Exception">
        /// Not a valid .mo file.
        /// or
        /// Unsupported .mo version.
        /// </exception>
        public void Read(Localization localization, Stream stream, bool loadComments = false)
        {
            using (var reader = new BinaryReader(stream))
            {
                // Check the magic bytes to see if it is an .mo file
                var magic = reader.ReadUInt32();
                if (magic != 0x950412de)
                {
                    throw new Exception("Not a valid .mo file.");
                }

                // Check the version of the file 
                var revision = reader.ReadUInt32();
                if (revision != 0)
                {
                    throw new Exception("Unsupported .mo version.");
                }

                // Read number of strings, and the offset of the string tables
                var n = reader.ReadUInt32();
                var originalOff = reader.ReadUInt32();
                var translatedOff = reader.ReadUInt32();

                // Positions of string tables
                var originalPositions = new Tuple<uint, uint>[n];
                var translatedPositions = new Tuple<uint, uint>[n];

                // Go to the beginning of the original table
                reader.BaseStream.Seek(originalOff, SeekOrigin.Begin);

                // Read original string positions
                for (int i = 0; i < n; i++)
                {
                    var size = reader.ReadUInt32();
                    var off = reader.ReadUInt32();
                    originalPositions[i] = new Tuple<uint, uint>(size, off);
                }

                // Go to the beginning of the translated table
                reader.BaseStream.Seek(translatedOff, SeekOrigin.Begin);

                // Read translated string positions
                for (int i = 0; i < n; i++)
                {
                    var size = reader.ReadUInt32();
                    var off = reader.ReadUInt32();
                    translatedPositions[i] = new Tuple<uint, uint>(size, off);
                }

                // Read messages
                for (int i = 0; i < n; i++)
                {
                    var origPos = originalPositions[i];
                    var transPos = translatedPositions[i];

                    // Read original
                    reader.BaseStream.Seek(origPos.Item2, SeekOrigin.Begin);
                    var orig = UTF8Encoding.UTF8.GetString(reader.ReadBytes((int)origPos.Item1)); // TODO: Support encoding

                    // Read translations
                    reader.BaseStream.Seek(transPos.Item2, SeekOrigin.Begin);
                    var trans = UTF8Encoding.UTF8.GetString(reader.ReadBytes((int)transPos.Item1)); // TODO: Support encoding

                    // Headers -> parse them
                    if (String.IsNullOrEmpty(orig))
                    {
                        localization.Headers = trans.Trim().Split('\n').Select(s => s.Split(new[] { ':' }, 2)).ToDictionary(v => v[0].Trim(), v => v[1].Trim());
                    }
                    else
                    {
                        var message = new Message();

                        // Extract plural
                        var parts = orig.Split(new[] { '\x00' }, 2);
                        if (parts.Length > 1)
                        {
                            message.Plural = parts[1];
                            orig = parts[0];
                        }

                        var id = orig;

                        // Extract context
                        parts = orig.Split(new[] { '\x04' }, 2);
                        if (parts.Length > 1)
                        {
                            message.Context = parts[0];
                            orig = parts[1];
                        }

                        message.Id = orig;
                        message.Translations = trans.Split('\x00');

                        localization.Add(message);
                    }
                }
            }
        }
    }
}
