using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Collections;
using System.Xml.Linq;
using System.ComponentModel.Design;

namespace GettextDotNet.Formats
{
    public class ResXFormat : ILocalizationFormat
    {
        public string[] FileExtensions
        {
            get { return new string[] { ".resx" }; }
        }

        public void Write(Localization localization, System.IO.Stream stream, bool writeComments = false)
        {
            using (var writer = new ResXResourceWriter(stream))
            {
                foreach (var message in localization.GetMessages())
                {
                    var key = (String.IsNullOrEmpty(message.Context) ? "" : message.Context + "\x04") + message.Id;

                    var comments = new string[7];

                    comments[0] = message.Plural ?? "";
                    comments[1] = String.Join("\n", message.Comments);
                    comments[2] = String.Join("\n", message.TranslatorComments);
                    comments[3] = String.Join("\n", message.References);
                    comments[4] = String.Join("\n", message.Flags);
                    comments[5] = String.Join("\n", message.PreviousId);
                    comments[6] = String.Join("\n", message.PreviousContext);

                    for (int i = 0; i < message.Translations.Length; i++)
                    {
                        if (i != 0)
                        {
                            writer.AddResource(key + "\x03" + i, message.Translations[i]);
                        }
                        else
                        {
                            var node = new ResXDataNode(key, message.Translations[i]);

                            node.Comment = String.Join("\x04", comments);

                            writer.AddResource(node);
                        }
                    }
                }

                writer.AddMetadata("IsGetTextFormat", "true");

                foreach (var header in localization.GetHeaders())
                {
                    writer.AddMetadata(header.Key, header.Value);
                }
            }
        }

        public void Read(Localization localization, System.IO.Stream stream, bool loadComments = false)
        {
            ITypeResolutionService t = null;

            {
                var metareader = new ResXResourceReader(stream);
                IDictionaryEnumerator dict = metareader.GetMetadataEnumerator();

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                while(dict.MoveNext())
                {
                    var key = dict.Key as string;
                    var value = dict.Value as string;

                    if (key != null && value != null)
                    {
                        headers.Add(key, value);
                    }
                }

                stream.Seek(0, System.IO.SeekOrigin.Begin);
                var reader = new ResXResourceReader(stream);

                reader.UseResXDataNodes = true;
                bool isGetTextFormat = headers.ContainsKey("IsGetTextFormat") && headers["IsGetTextFormat"].Equals("true", StringComparison.OrdinalIgnoreCase);

                dict = reader.GetEnumerator();

                if (isGetTextFormat)
                {
                    foreach (var header in headers)
                    {
                        localization.SetHeader(header.Key, header.Value);
                    }
                }

                while(dict.MoveNext())
                {
                    var key = dict.Key as string;
                    var node = dict.Value as ResXDataNode;
                    var value = node.GetValue(t) as string;

                    if (headers.ContainsKey(key) && headers[key] == value)
                    {
                        continue;
                    }

                    if (isGetTextFormat)
                    {
                        string context = null;
                        string id = "";
                        int number = 0;

                        var key2 = key.Split('\x04');

                        if (key2.Length == 1)
                        {
                            id = key2[0];
                        }
                        else if (key2.Length == 2)
                        {
                            context = key2[0];
                            id = key2[1];
                        }
                        else
                        {
                            throw new ArgumentException("Invalid format inside the .resx file");
                        }

                        if (id.Contains('\x03'))
                        {
                            var tmp = id.Split('\x03');

                            if (tmp.Length != 2)
                            {
                                throw new ArgumentException("Invalid format inside the .resx file");
                            }

                            id = tmp[0];
                            number = int.Parse(tmp[1]);
                        }

                        var message = localization.GetMessage(id, context);

                        if (number == 0 && loadComments)
                        {
                            var comments = node.Comment.Split('\x04');

                            if (comments.Length != 7)
                            {
                                throw new ArgumentException("Invalid format inside the .resx file");
                            }

                            if (message == null)
                            {
                                message = new Message()
                                {
                                    Id = id,
                                    Context = context,
                                    Plural = comments[0],
                                    Comments = comments[1].Split('\n').ToList(),
                                    TranslatorComments = comments[2].Split('\n').ToList(),
                                    References = comments[3].Split('\n').ToList(),
                                    Flags = new HashSet<string>(comments[4].Split('\n')),
                                    PreviousId = comments[5],
                                    PreviousContext = comments[6]
                                };

                                localization.Add(message);
                            }
                            else
                            {
                                message.Plural = comments[0];
                                message.Comments = comments[1].Split('\n').ToList();
                                message.TranslatorComments = comments[2].Split('\n').ToList();
                                message.References = comments[3].Split('\n').ToList();
                                message.Flags = new HashSet<string>(comments[4].Split('\n'));
                                message.PreviousId = comments[5];
                                message.PreviousContext = comments[6];
                            }
                        }
                        else if (message == null)
                        {
                            message = new Message()
                            {
                                Id = id,
                                Context = context
                            };

                            localization.Add(message);
                        }

                        message.SetTranslation(number, value);
                    }
                    else
                    {
                        localization.Add(new Message { Id = key, Translations = new string[] { value } });
                    }
                }

                metareader.Close();
                reader.Close();
            }
        }
    }
}
