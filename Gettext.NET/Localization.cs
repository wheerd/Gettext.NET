using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GettextDotNET
{
    public class Localization
    {
        public Dictionary<string, Message> Messages { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool LoadComments { get; set; }
        public string FileName { get; set; }

        public Localization()
        {
            Messages = new Dictionary<string, Message>();
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            LoadComments = false;
        }

        public Localization(string fileName, bool loadComments = false)
            : this()
        {
            LoadComments = loadComments;
            Load(fileName, loadComments);
        }

        private enum CommandType { none, msgid, msgid_plural, msgstr, msgctxt, domain }

        private void LoadFromReader(TextReader reader, bool loadComments = false)
        {
            using (reader)
            {
                Messages.Clear();
                Headers.Clear();

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
                                    ParseHeaders(translations[0]);
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

                                // Add message
                                if (!Messages.ContainsKey(message.Id))
                                {
                                    Messages.Add(message.Id, message);
                                }
                                else
                                {
                                    Messages[message.Id] = message;
                                }
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
                }

                // Process last message
                if (!String.IsNullOrEmpty(message.Id) && !Messages.ContainsKey(message.Id))
                {
                    var t = new string[translations.Keys.Max() + 1];

                    foreach (var kv in translations)
                    {
                        t[kv.Key] = kv.Value;
                    }

                    message.Translations = t;

                    Messages.Add(message.Id, message);
                }
            }
        }

        private string ProcessMessageString(string str)
        {
            Regex EscapeRegex = new Regex(@"\\([\\""n])", RegexOptions.Compiled);

            return EscapeRegex.Replace(str, new MatchEvaluator(
                m => m.Groups[1].Value == "n" ? "\n" : m.Groups[1].Value
            ));
        }

        private void ParseHeaders(string headerString)
        {
            Headers = headerString.Split('\n').Select(s => s.Split(new[] { ':' }, 2)).ToDictionary(v => v[0].Trim(), v => v[1].Trim());
        }

        public void Load(string fileName, bool loadComments = false)
        {
            LoadFromReader(new StreamReader(fileName), loadComments);

            FileName = fileName;
        }

        public void LoadFromString(string str, bool loadComments = false)
        {
            LoadFromReader(new StringReader(str), loadComments);
        }

        public string ToPOBlock()
        {
            StringBuilder builder = new StringBuilder();

            // Headers
            builder.Append("msgid \"\"\n");
            builder.Append("msgstr \"\"\n");
            builder.Append("\"");
            builder.Append(String.Join("\\n\"\n\"", Headers.Select(h => String.Format("{0}: {1}", h.Key, h.Value))));
            builder.Append("\"\n\n");

            // Messages
            builder.Append(String.Join("\n", Messages.Values.Select(m => m.ToPOBlock())));

            return builder.ToString();
        }

        public void Save(string fileName = null)
        {
            if (fileName == null)
            {
                if (FileName == null)
                {
                    throw new Exception("Localization was not created from a file, please provide a file name for saving.");
                }

                fileName = FileName;
            }

            using (var s = new StreamWriter(fileName))
            {
                s.Write(ToPOBlock());
            }
        }
    }
}
