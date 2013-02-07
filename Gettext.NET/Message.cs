using System;
using System.Collections.Generic;
using System.Text;

namespace GettextDotNET
{
    public class Message
    {
        public string Id { get; set; }
        public string Plural { get; set; }
        public string Context { get; set; }
        public string[] Translations { get; set; }
        public List<string> Comments { get; set; }
        public List<string> TranslatorComments { get; set; }
        public List<string> References { get; set; }
        public HashSet<string> Flags { get; set; }
        public string PreviousId { get; set; }
        public string PreviousContext { get; set; }

        public Message()
        {
            Id = "";
            Context = "";
            Translations = new string[] { "" };
            Comments = new List<string>();
            TranslatorComments = new List<string>();
            References = new List<string>();
            Flags = new HashSet<string>();
        }

        public string ToPOBlock()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var Comment in TranslatorComments)
            {
                builder.Append("#  " + Comment + "\n");
            }

            foreach (var Comment in Comments)
            {
                builder.Append("#. " + Comment + "\n");
            }

            foreach (var Reference in References)
            {
                builder.Append("#: " + Reference + "\n");
            }

            if (Flags.Count > 0)
            {
                builder.Append("#, " + String.Join(", ", Flags) + "\n");
            }

            if (!String.IsNullOrEmpty(PreviousContext))
            {
                builder.Append(String.Format("#| msgctxt \"{0}\"\n", PreviousContext));
            }

            if (!String.IsNullOrEmpty(PreviousId))
            {
                builder.Append(String.Format("#| msgid \"{0}\"\n", PreviousId));
            }

            if (!String.IsNullOrEmpty(Context))
            {
                builder.Append(String.Format("msgctxt \"{0}\"\n", Context));
            }

            builder.Append(String.Format("msgid \"{0}\"\n", Id));

            if (!String.IsNullOrEmpty(Plural))
            {
                builder.Append(String.Format("msgid_plural \"{0}\"\n", Plural));
            }

            if (Translations.Length > 1)
            {
                int i = 0;
                foreach (var Translation in Translations)
                {
                    builder.Append(String.Format("msgstr[{0}] \"{1}\"\n", i++, Translation));
                }
            }
            else
            {
                builder.Append(String.Format("msgstr \"{0}\"\n", Translations[0]));
            }

            return builder.ToString();
        }
    }
}
