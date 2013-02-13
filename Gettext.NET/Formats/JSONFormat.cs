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
    /// Provides functions for using files in the JSON format.
    /// </summary>
    public class JSONFormat : ILocalizationFormat
    {
        /// <summary>
        /// Gets the file extensions supported for the JSON format.
        /// </summary>
        /// <value>
        /// The file extensions supported for the JSON format.
        /// </value>
        public string[] FileExtensions
        {
            get { return new string[] { ".json" }; }
        }

        /// <summary>
        /// Dumps the specified localization to the stream in this format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="writeComments">If set to <c>true</c>, comments will be included in the ouput.</param>
        public void Write(Localization localization, Stream stream, bool writeComments = false)
        {
            // Own crude json implementation to avoid dependencies
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(@"{""Headers"":{");

                writer.Write(String.Join(",", localization.GetHeaders().Select(h => String.Format(@"{0}:{1}", Escape(h.Key), Escape(h.Value)))));

                writer.Write(@"},""Messages"":{");

                var i = localization.Count;

                foreach (var message in localization.GetMessages())
                {
                    WriteMessage(writer, message, writeComments);

                    if (--i > 0)
                    {
                        writer.Write(",");
                    }

                }
                writer.Write("}}");
            }
        }
        /// <summary>
        /// Attempts to read messages and headers from the stream in JSON format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="loadComments">If set to <c>true</c>, comments will be loaded from the stream.</param>
        /// <exception cref="JSONFormatError">
        /// Thrown when the stream does not contain valid JSON or the JSON data does not have the expected form.
        /// </exception>
        public void Read(Localization localization, Stream stream, bool loadComments = false)
        {
            try
            {
                line = 1;
                offset = 0;

                // Own crude json implementation to avoid dependencies
                using (StreamReader reader = new StreamReader(stream))
                {
                    SkipWhitespace(reader);

                    if (reader.Read() != '{')
                    {
                        throw new JSONFormatError("Expected '{'");
                    }
                    offset++;

                    SkipWhitespace(reader);

                    var moreKeys = true;

                    while (moreKeys)
                    {
                        var type = ReadString(reader);

                        SkipWhitespace(reader);
                        if (reader.Read() != ':')
                        {
                            throw new JSONFormatError("Expected ':'");
                        }
                        SkipWhitespace(reader);

                        if (reader.Read() != '{')
                        {
                            throw new JSONFormatError("Expected '{'");
                        }
                        offset++;
                        SkipWhitespace(reader);

                        if (type == "Headers")
                        {
                            ReadHeaders(localization, reader);
                        }
                        else if (type == "Messages")
                        {
                            ReadMessages(localization, reader);
                        }
                        else
                        {
                            throw new JSONFormatError("Invalid top-level key, only 'Messages' and 'Headers' are allowed.");
                        }

                        SkipWhitespace(reader);

                        if (reader.Read() != '}')
                        {
                            throw new JSONFormatError("Expected '}'");
                        }
                        offset++;
                        SkipWhitespace(reader);
                        if (reader.Peek() != ',')
                        {
                            moreKeys = false;
                        }
                        else
                        {
                            reader.Read();
                            offset++;
                            SkipWhitespace(reader);
                        }
                    }
                    SkipWhitespace(reader);

                    if (reader.Read() != '}')
                    {
                        throw new JSONFormatError("Expected '}'");
                    }
                    offset++;
                    SkipWhitespace(reader);
                    if (reader.Read() != -1)
                    {
                        throw new JSONFormatError("Expected EOF");
                    }
                }
            }
            catch (JSONFormatError e)
            {
                throw new JSONFormatError(String.Format("JSON Format Error: {0} (Line {1}, offset {2}", e.Message, line, offset));
            }
        }


        // ------------ PRIVATE ------------
        

        private string Escape(string str, bool quotes = true)
        {
            return (quotes ? "\"" : "") + str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + (quotes ? "\"" : "");
        }

        private void WriteMessage(StreamWriter writer, Message message, bool writeComments)
        {
            var key = (String.IsNullOrEmpty(message.Context) ? "" : Escape(message.Context, false)) + "\\u0004" + Escape(message.Id, false);

            writer.Write(String.Format("\"{0}\":{{", key));

            if (writeComments)
            {
                writer.Write(String.Format(@"""TranslatorComments"":[{0}],", String.Join(",", message.TranslatorComments.Select(c => Escape(c)))));

                writer.Write(String.Format(@"""Comments"":[{0}],", String.Join(",", message.Comments.Select(c => Escape(c)))));

                writer.Write(String.Format(@"""References"":[{0}],", String.Join(",", message.References.Select(c => Escape(c)))));

                writer.Write(String.Format(@"""Flags"":[{0}],", String.Join(",", message.Flags.Select(c => Escape(c)))));

                if (!String.IsNullOrEmpty(message.PreviousContext))
                {
                    writer.Write(String.Format(@"""PreviousContext"":{0},", Escape(message.PreviousContext)));
                }

                if (!String.IsNullOrEmpty(message.PreviousId))
                {
                    writer.Write(String.Format(@"""PreviousId"":{0},", Escape(message.PreviousId)));
                }
            }

            if (!String.IsNullOrEmpty(message.Context))
            {
                writer.Write(String.Format(@"""Context"":{0},", Escape(message.Context)));
            }

            writer.Write(String.Format(@"""Id"":{0},", Escape(message.Id)));

            if (!String.IsNullOrEmpty(message.Plural))
            {
                writer.Write(String.Format(@"""Plural"":{0},", Escape(message.Plural)));
            }

            writer.Write(String.Format(@"""Translations"":[{0}]", String.Join(",", message.Translations.Select(c => Escape(c)))));

            writer.Write("}");
        }

        private int line = 1;
        private int offset = 0;

        private void SkipWhitespace(StreamReader reader)
        {
            while (reader.Peek() != -1)
            {
                switch (reader.Peek())
                {
                    case '\n':
                        line++;
                        offset = 0;
                        reader.Read();
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                        reader.Read();
                        offset++;
                        break;
                    default:
                        return;
                }

            }
        }

        private List<string> ReadStringList(StreamReader reader)
        {
            List<string> list = new List<string>();

            if (reader.Read() != '[')
            {
                throw new JSONFormatError("Expected '['");
            }
            offset++;
            SkipWhitespace(reader);

            var moreItems = reader.Peek() != ']';

            while (moreItems)
            {
                list.Add(ReadString(reader));
                SkipWhitespace(reader);
                if (reader.Peek() != ',')
                {
                    moreItems = false;
                }
                else
                {
                    reader.Read();
                    offset++;
                    SkipWhitespace(reader);
                }
            }

            if (reader.Read() != ']')
            {
                throw new JSONFormatError("Expected ']'");
            }
            offset++;

            return list;
        }

        private string ReadString(StreamReader reader)
        {
            string str = "";

            if (reader.Read() != '"')
            {
                throw new JSONFormatError("Expected beginning of string");
            }
            offset++;

            while (reader.Peek() != '"')
            {
                if (reader.Peek() == -1)
                {
                    throw new JSONFormatError("Unexpected end");
                }

                if (reader.Peek() == '\\')
                {
                    reader.Read();
                    offset++;

                    char c = (char)reader.Read();
                    offset++;

                    switch (c)
                    {
                        case 'n':
                            str += "\n";
                            break;
                        case 't':
                            str += "\t";
                            break;
                        case 'r':
                            str += "\r";
                            break;
                        default:
                            str += new string(c, 1);
                            if (c == '\n')
                            {
                                line++;
                                offset = 0;
                            }
                            break;
                    }
                }
                else
                {
                    str += new string((char)reader.Read(), 1);
                    offset++;
                }
            }

            reader.Read();
            offset++;

            return str;
        }

        /// <summary>
        /// This is an error which is thrown if a malformed JSON string cannot be parsed.
        /// </summary>
        public class JSONFormatError : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="JSONFormatError"/> class.
            /// </summary>
            /// <param name="error">The error message.</param>
            internal JSONFormatError(string error)
                : base(error)
            {

            }
        }

        private void ReadHeaders(Localization localization, StreamReader reader)
        {
            var moreHeaders = true;

            while (moreHeaders)
            {
                var key = ReadString(reader);
                SkipWhitespace(reader);
                if (reader.Read() != ':')
                {
                    throw new JSONFormatError("Expected ':'");
                }
                offset++;
                SkipWhitespace(reader);
                var value = ReadString(reader);
                localization.SetHeader(key, value);
                SkipWhitespace(reader);
                if (reader.Peek() != ',')
                {
                    moreHeaders = false;
                }
                else
                {
                    reader.Read();
                    offset++;
                    SkipWhitespace(reader);
                }
            }
        }

        private void ReadMessages(Localization localization, StreamReader reader)
        {
            var moreMessages = true;

            while (moreMessages)
            {
                ReadString(reader);
                SkipWhitespace(reader);
                if (reader.Read() != ':')
                {
                    throw new JSONFormatError("Expected ':'");
                }
                offset++;
                SkipWhitespace(reader);
                if (reader.Read() != '{')
                {
                    throw new JSONFormatError("Expected '{'");
                }
                SkipWhitespace(reader);

                ReadMessage(localization, reader);

                offset++;
                if (reader.Read() != '}')
                {
                    throw new JSONFormatError("Expected '{'");
                }
                offset++;
                SkipWhitespace(reader);
                if (reader.Peek() != ',')
                {
                    moreMessages = false;
                }
                else
                {
                    reader.Read();
                    offset++;
                    SkipWhitespace(reader);
                }
            }
        }

        private void ReadMessage(Localization localization, StreamReader reader)
        {
            var moreKeys = true;
            var message = new Message();

            while (moreKeys)
            {
                var key = ReadString(reader);
                SkipWhitespace(reader);
                if (reader.Read() != ':')
                {
                    throw new JSONFormatError("Expected ':'");
                }
                offset++;
                SkipWhitespace(reader);

                object value = null;

                switch (key)
                {
                    case "Context":
                    case "Id":
                    case "Plural":
                    case "PreviousId":
                    case "PreviousContext":
                        value = ReadString(reader);
                        break;
                    case "TranslatorComments":
                    case "Comments":
                    case "References":
                    case "Flags":
                    case "Translations":
                        value = ReadStringList(reader);
                        break;
                    default:
                        throw new JSONFormatError(String.Format("Invalid message attribute '{0}'", key));
                }

                switch (key)
                {
                    case "Context":
                        message.Context = (string)value;
                        break;
                    case "Id":
                        message.Id = (string)value;
                        break;
                    case "Plural":
                        message.Plural = (string)value;
                        break;
                    case "PreviousId":
                        message.PreviousId = (string)value;
                        break;
                    case "PreviousContext":
                        message.PreviousContext = (string)value;
                        break;
                    case "TranslatorComments":
                        message.TranslatorComments = (List<string>)value;
                        break;
                    case "Comments":
                        message.Comments = (List<string>)value;
                        break;
                    case "References":
                        message.References = (List<string>)value;
                        break;
                    case "Flags":
                        message.Flags = new HashSet<string>((List<string>)value);
                        break;
                    case "Translations":
                        message.Translations = ((List<string>)value).ToArray();
                        break;
                }
                SkipWhitespace(reader);
                if (reader.Peek() != ',')
                {
                    moreKeys = false;
                }
                else
                {
                    reader.Read();
                    offset++;
                    SkipWhitespace(reader);
                }
            }

            localization.Add(message);
        }
    }
}
