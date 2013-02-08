using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GettextDotNET.Formats;

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
        
        public void LoadFromFile<Format>(string fileName, bool loadComments = false)
            where Format : ILocalizationFormat, new()
        {
            using (var stream = File.OpenRead(fileName))
            {
                new Format().Read(this, stream, loadComments);
            }
        }

        public void SaveToFile<Format>(string fileName, bool writeComments = false)
            where Format : ILocalizationFormat, new()
        {
            using (var stream = File.Create(fileName))
            {
                new Format().Write(this, stream, writeComments);
            }
        }

        public string ToString<Format>(bool writeComments = false)
            where Format : ILocalizationFormat, new()
        {
            var stream = new MemoryStream();

            new Format().Write(this, stream, writeComments);

            return System.Text.Encoding.Default.GetString(stream.GetBuffer());
        }

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
