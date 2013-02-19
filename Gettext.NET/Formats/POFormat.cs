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
    /// Provides functions for using files in the .po format.
    /// </summary>
    public class POFormat : ILocalizationFormat
    {
        /// <summary>
        /// Gets the file extensions supported for the .po format.
        /// </summary>
        /// <value>
        /// The file extensions supported for the .po format.
        /// </value>
        public string[] FileExtensions { get { return new string[] { ".po", ".pot" }; } }

        /// <summary>
        /// Dumps the specified localization to the stream in the .po format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="writeComments">If set to <c>true</c>, comments will be included in the ouput.</param>
        public void Write(Localization localization, Stream stream, bool writeComments = false)
        {
            using (var writer = new StreamWriter(stream))
            {
                // Headers
                writer.Write("msgid \"\"\n");
                writer.Write("msgstr \"\"\n");
                writer.Write("\"");
                writer.Write(String.Join("\\n\"\n\"", localization.GetHeaders().Select(h => String.Format("{0}: {1}", h.Key, h.Value))));
                writer.Write("\"\n\n");

                // Messages
                writer.Write(String.Join("\n", localization.GetMessages().Select(m => GetMessagePOBlock(m))));
            }
        }

        /// <summary>
        /// Attempts to read messages and headers from the stream in the .po format.
        /// </summary>
        /// <param name="localization">The localization.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="loadComments">If set to <c>true</c>, comments will be loaded from the stream.</param>
        /// <exception cref="System.Exception"></exception>
        public void Read(Localization localization, Stream stream, bool loadComments = false)
        {
            using (var reader = new StreamReader(stream))
            {
                int lineNo = 0;
                string line;

                // Detects message strings
                var StringRegex = new Regex("\"(.*)\"", RegexOptions.Compiled);

                // Detects msgstr[n]
                var MsgidRegex = new Regex(@"^msgstr\[(\d+)\]", RegexOptions.Compiled);

                // String for the last command
                var lastString = "";

                // Last command (for multi line strings) and the one on the current line (if any)
                CommandType lastType = CommandType.none, newType = CommandType.none;
                int lastMsgNo = 0, newMsgNo = 0;

                // All the translations for the current message
                var translations = new Dictionary<int, string>();

                // Currently parsed message
                var message = new Message();

                // Read all the lines
                while (null != (line = reader.ReadLine()))
                {
                    lineNo++;
                    line = line.Trim();

                    // Empty line ends block
                    if (String.IsNullOrWhiteSpace(line))
                    {
                        // Do nothing if the line before was empty
                        if (lastType != CommandType.none)
                        {
                            // Handle last string
                            switch (lastType)
                            {
                                case CommandType.msgctxt:
                                    message.Context = lastString;
                                    break;
                                case CommandType.msgid:
                                    message.Id = lastString;
                                    break;
                                case CommandType.msgid_plural:
                                    message.Plural = lastString;
                                    break;
                                case CommandType.msgstr:
                                    translations[lastMsgNo] = lastString;
                                    break;
                            }

                            // Empty msgid -> headers
                            if (String.IsNullOrEmpty(message.Id))
                            {
                                if (translations.ContainsKey(0))
                                {
                                    foreach (var header in translations[0].Trim().Split('\n').Select(s => s.Split(new[] { ':' }, 2)))
                                    {
                                        localization.SetHeader(header[0].Trim(), header[1].Trim());
                                    }
                                }
                            }
                            else
                            {
                                // Transform translation dictionary to array
                                var t = new string[translations.Keys.Max() + 1];

                                foreach (var kv in translations)
                                {
                                    t[kv.Key] = kv.Value;
                                }

                                message.Translations = t;

                                localization.Add(message);
                            }

                            // Reset message information
                            message = new Message();
                            lastString = "";
                            lastType = CommandType.none;
                            translations = new Dictionary<int, string>();
                        }
                    }
                    // Comment lines
                    else if (line.StartsWith("#"))
                    {
                        if (loadComments)
                        {
                            // Extracted comments
                            if (line.StartsWith("#."))
                            {
                                message.Comments.Add(line.Substring(2).Trim());
                            }
                            // References
                            else if (line.StartsWith("#:"))
                            {
                                message.References.Add(line.Substring(2).Trim());
                            }
                            // Flags
                            else if (line.StartsWith("#,"))
                            {
                                message.Flags.UnionWith(line.Substring(2).Split(',').Select(s => s.Trim()));
                            }
                            // Old values
                            else if (line.StartsWith("#|"))
                            {
                                line = line.Substring(2).Trim();

                                // Old id
                                if (line.StartsWith("msgid"))
                                {
                                    var match = StringRegex.Match(line);

                                    if (match.Success)
                                    {
                                        message.PreviousId = ProcessMessageString(match.Groups[1].Value);
                                    }
                                }

                                // Old context
                                if (line.StartsWith("msgctxt"))
                                {
                                    var match = StringRegex.Match(line);

                                    if (match.Success)
                                    {
                                        message.PreviousContext = ProcessMessageString(match.Groups[1].Value);
                                    }
                                }
                            }
                            // Translator comments
                            else
                            {
                                message.TranslatorComments.Add(line.Substring(1).Trim());
                            }
                        }
                    }
                    else
                    {
                        //Plural
                        if (line.StartsWith("msgid_plural"))
                        {
                            newType = CommandType.msgid_plural;
                        }
                        // Id
                        else if (line.StartsWith("msgid"))
                        {
                            newType = CommandType.msgid;
                        }
                        // Translation
                        else if (line.StartsWith("msgstr"))
                        {
                            var msgstrMatch = MsgidRegex.Match(line);

                            // Multiple translations?
                            if (msgstrMatch.Success)
                            {
                                newMsgNo = int.Parse(msgstrMatch.Groups[1].Value);
                            }
                            else
                            {
                                newMsgNo = 0;
                            }

                            newType = CommandType.msgstr;
                        }
                        // Context
                        else if (line.StartsWith("msgctxt"))
                        {
                            newType = CommandType.msgctxt;
                        }

                        // Extract string
                        var match = StringRegex.Match(line);

                        if (match.Success)
                        {
                            line = ProcessMessageString(match.Groups[1].Value);

                            // Process previous command
                            if (newType != lastType || newMsgNo != lastMsgNo)
                            {
                                switch (lastType)
                                {
                                    case CommandType.msgctxt:
                                        message.Context = lastString;
                                        break;
                                    case CommandType.msgid:
                                        message.Id = lastString;
                                        break;
                                    case CommandType.msgid_plural:
                                        message.Plural = lastString;
                                        break;
                                    case CommandType.msgstr:
                                        translations[lastMsgNo] = lastString;
                                        break;
                                }

                                lastMsgNo = newMsgNo;
                                lastType = newType;
                                lastString = line;
                            }
                            else
                            {
                                lastString += line;
                            }
                        }
                        else
                        {
                            throw new Exception(String.Format("PO Parse Error: Syntax error on line {0}.", lineNo));
                        }
                    }
                }

                // Process last command (if any)
                if (lastType != CommandType.none)
                {
                    switch (lastType)
                    {
                        case CommandType.msgctxt:
                            message.Context = lastString;
                            break;
                        case CommandType.msgid:
                            message.Id = lastString;
                            break;
                        case CommandType.msgid_plural:
                            message.Plural = lastString;
                            break;
                        case CommandType.msgstr:
                            translations[lastMsgNo] = lastString;
                            break;
                    }

                    // Process last message
                    if (!String.IsNullOrEmpty(message.Id))
                    {
                        var t = new string[translations.Keys.Max() + 1];

                        foreach (var kv in translations)
                        {
                            t[kv.Key] = kv.Value;
                        }

                        message.Translations = t;

                        localization.Add(message);
                    }
                }
            }
        }

        private enum CommandType { none, msgid, msgid_plural, msgstr, msgctxt, domain }

        private string GetMessagePOBlock(Message msg, bool includeComments = true)
        {
            StringBuilder builder = new StringBuilder();

            if (includeComments)
            {
                foreach (var Comment in msg.TranslatorComments)
                {
                    builder.Append("#  " + Comment + "\n");
                }

                foreach (var Comment in msg.Comments)
                {
                    builder.Append("#. " + Comment + "\n");
                }

                foreach (var Reference in msg.References)
                {
                    builder.Append("#: " + Reference + "\n");
                }

                if (msg.Flags.Count > 0)
                {
                    builder.Append("#, " + String.Join(", ", msg.Flags) + "\n");
                }

                if (!String.IsNullOrEmpty(msg.PreviousContext))
                {
                    builder.Append(String.Format("#| msgctxt \"{0}\"\n", msg.PreviousContext));
                }

                if (!String.IsNullOrEmpty(msg.PreviousId))
                {
                    builder.Append(String.Format("#| msgid \"{0}\"\n", msg.PreviousId));
                }
            }

            if (!String.IsNullOrEmpty(msg.Context))
            {
                builder.Append(String.Format("msgctxt \"{0}\"\n", msg.Context.Replace("\n", "\\n\"\n\"")));
            }

            builder.Append(String.Format("msgid \"{0}\"\n", msg.Id.Replace("\n", "\\n\"\n\"")));

            if (!String.IsNullOrEmpty(msg.Plural))
            {
                builder.Append(String.Format("msgid_plural \"{0}\"\n", msg.Plural.Replace("\n", "\\n\"\n\"")));
            }

            if (!String.IsNullOrEmpty(msg.Plural) && msg.loc.NumPlurals > 1)
            {
                int i = 0;
                foreach (var Translation in msg.Translations.Concat(Enumerable.Repeat("", msg.loc.NumPlurals - msg.Translations.Length)))
                {
                    builder.Append(String.Format("msgstr[{0}] \"{1}\"\n", i++, Translation.Replace("\n", "\\n\"\n\"")));
                }
            }
            else
            {
                builder.Append(String.Format("msgstr \"{0}\"\n", msg.Translations[0].Replace("\n", "\\n\"\n\"")));
            }

            return builder.ToString();
        }
        
        private string ProcessMessageString(string str)
        {
            Regex EscapeRegex = new Regex(@"\\([\\""n])", RegexOptions.Compiled);

            return EscapeRegex.Replace(str, new MatchEvaluator(
                m => m.Groups[1].Value == "n" ? "\n" : m.Groups[1].Value
            ));
        }
    }
}
