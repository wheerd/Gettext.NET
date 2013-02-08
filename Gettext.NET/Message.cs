using System;
using System.Collections.Generic;
using System.Text;

namespace GettextDotNet
{
    /// <summary>
    /// The translation(s) (<see cref="Translations"/>) of a specific string (<see cref="Id"/>) and its
    /// plural (<see cref="Plural"/>) in a specific (optional) context (<see cref="Context"/>).
    /// </summary>
    public class Message
    {
        internal Localization loc;
        private string _id;
        private string _context;

        /// <summary>
        /// Gets or sets the id of the message.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public string Id {
            get { return _id; }
            set {
                var oldId = _id;
                _id = value;
                if (loc != null)
                {
                    loc.UpdateMessage(this, oldId, _context);
                }
            }
        }

        /// <summary>
        /// Gets or sets the (optional) plural of the message.
        /// </summary>
        /// <value>
        /// The plural.
        /// </value>
        public string Plural { get; set; }

        /// <summary>
        /// Gets or sets the (optional) context of the message.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        public string Context
        {
            get { return _context; }
            set
            {
                var oldcontext = _context;
                _context = value;
                if (loc != null)
                {
                    loc.UpdateMessage(this, _id, oldcontext);
                }
            }
        }

        /// <summary>
        /// Gets or sets the translations of the message.
        /// Multiple translations are used for plural forms as defined in the <see cref="Localization"/>
        /// </summary>
        /// <value>
        /// The translations.
        /// </value>
        public string[] Translations { get; set; }

        /// <summary>
        /// Gets or sets the extracted comments of the message.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        public List<string> Comments { get; set; }

        /// <summary>
        /// Gets or sets the translator comments of the message.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        public List<string> TranslatorComments { get; set; }

        /// <summary>
        /// Gets or sets the code references of the message.
        /// </summary>
        /// <value>
        /// The references.
        /// </value>
        public List<string> References { get; set; }

        /// <summary>
        /// Gets or sets the flags of the message.
        /// Possible flags are documented in the GNU manual for gettext.
        /// </summary>
        /// <value>
        /// The flags.
        /// </value>
        public HashSet<string> Flags { get; set; }

        /// <summary>
        /// Gets or sets the previous id of the message.
        /// </summary>
        /// <value>
        /// The previous id.
        /// </value>
        public string PreviousId { get; set; }

        /// <summary>
        /// Gets or sets the previous context of the message.
        /// </summary>
        /// <value>
        /// The previous context.
        /// </value>
        public string PreviousContext { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        public Message()
        {
            Id = "";
            Context = "";
            Translations = new string[] { "" };
            Comments = new List<string>();
            TranslatorComments = new List<string>();
            References = new List<string>();
            Flags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
