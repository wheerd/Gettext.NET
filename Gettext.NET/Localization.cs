using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GettextDotNet.Formats;

namespace GettextDotNet
{
    /// <summary>
    /// A collection of localized/translated strings
    /// </summary>
    public class Localization
    {
        private Dictionary<string, Message> Messages { get; set; }

        private string GetKey(string id, string context = null)
        {
            return (String.IsNullOrEmpty(context) ? "" : context + "\x04") + id;
        }

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

        /// <summary>
        /// Gets a translation message from the localization.
        /// </summary>
        /// <param name="id">The id of the message.</param>
        /// <param name="context">The context of the message.</param>
        /// <returns>The message.</returns>
        public Message GetMessage(string id, string context = null)
        {
            return Messages[GetKey(id, context)];
        }

        /// <summary>
        /// Adds the specified message to the localization.
        /// </summary>
        /// <param name="msg">The message.</param>
        public void Add(Message msg)
        {
            var key = GetKey(msg.Id, msg.Context);

            if (!Messages.ContainsKey(key))
            {
                Messages.Add(key, msg);
            }
            else
            {
                Messages[key] = msg;
            }

            msg.loc = this;
        }

        /// <summary>
        /// Determines whether a translation for the specified id exists in the localization.
        /// </summary>
        /// <param name="id">The id of the message.</param>
        /// <param name="context">The context of the message.</param>
        /// <returns>
        ///   <c>true</c> if a translation for the specified id exists in the localization; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string id, string context = null)
        {
            return Messages.ContainsKey(GetKey(id, context));
        }

        /// <summary>
        /// Determines whether the translation message exists in the localization.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns>
        ///   <c>true</c> if the translation message exists in the localization; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(Message msg)
        {
            return Messages.ContainsValue(msg);
        }

        /// <summary>
        /// Removes the specified translation message.
        /// </summary>
        /// <param name="id">The id of the message.</param>
        /// <param name="context">The context of the message.</param>
        /// <returns>
        ///   <c>true</c> if the translation message was removed from the localization; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(string id, string context = null)
        {
            var key = GetKey(id, context);

            if (Messages.ContainsKey(key))
            {
                Messages[key].loc = null;
            }

            return Messages.Remove(key);
        }

        /// <summary>
        /// Removes the specified translation message.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <returns>
        ///   <c>true</c> if the translation message was removed from the localization; otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(Message msg)
        {
            return Remove(msg.Id, msg.Context);
        }

        /// <summary>
        /// Gets all the messages in the localization.
        /// </summary>
        /// <returns>All the messages in the localization</returns>
        public IEnumerable<Message> GetMessages()
        {
            return Messages.Values;
        }

        /// <summary>
        /// Gets the <see cref="Message"/> with the specified id and context.
        /// </summary>
        /// <value>
        /// The <see cref="Message"/>.
        /// </value>
        /// <param name="Id">The id of the message.</param>
        /// <param name="Context">The context of the message.</param>
        /// <returns></returns>
        public Message this[string Id, string Context = null]
        {
            get
            {
                return GetMessage(Id, Context);
            }
        }

        /// <summary>
        /// Clears all translation messages and the headers.
        /// </summary>
        public void Clear()
        {
            foreach(var msg in Messages.Values)
            {
                msg.loc = null;
            }

            Messages.Clear();
            Headers.Clear();
        }

        /// <summary>
        /// Gets the count of translation messages.
        /// </summary>
        /// <value>
        /// The count of translation messages.
        /// </value>
        public int Count
        {
            get { return Messages.Count; }
        }

        internal void UpdateMessage(Message message, string oldId, string oldContext)
        {
            var key = GetKey(oldId, oldContext);
            Messages.Remove(key);

            key = GetKey(message.Id, message.Context);
            Messages.Add(key, message);
        }
    }
}
