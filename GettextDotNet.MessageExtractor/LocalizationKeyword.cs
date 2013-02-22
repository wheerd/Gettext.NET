using System;
using System.Linq;

namespace GettextDotNet.MessageExtractor
{
    internal class LocalizationKeyword
    {
        public string Name = null;

        public int? IdArg = null;
        public string IdName = null;

        public int? PluralArg = null;
        public string PluralName = null;

        public int? ContextArg = null;
        public string ContextName = null;

        public string DefaultContext = null;

        public bool MethodAllowed = false;
        public bool AttributeAllowed = false;
        public bool ClassAllowed = false;

        public static LocalizationKeyword Parse(string kw)
        {
            var method = new LocalizationKeyword();
            string specs = null;

            var t = kw.Split(':');

            if (t.Length == 1)
            {
                method.Name = kw;
            }
            else if (t.Length == 2)
            {
                method.Name = t[0];
                specs = t[1];
            }
            else if (t.Length == 3)
            {
                method.Name = t[0];
                specs = t[1];
                method.DefaultContext = t[2];
            }
            else
            {
                throw new ArgumentException("Too many colons in the keyword");
            }

            if (method.Name.Contains('!'))
            {
                t = method.Name.Split('!');

                if (t.Length > 2)
                {
                    throw new ArgumentException("Too many exclamation marks in the keyword");
                }

                method.Name = t[0];
                method.MethodAllowed = t[1].Contains('m');
                method.ClassAllowed = t[1].Contains('c');
                method.AttributeAllowed = t[1].Contains('a');
            }
            else
            {
                method.MethodAllowed = true;
                method.ClassAllowed = true;
                method.AttributeAllowed = true;
            }

            if (specs != null)
            {
                int i = 0;

                foreach (var spec in specs.Split(','))
                {
                    string id = spec;
                    string name = null;

                    if (spec.Contains('!'))
                    {
                        t = spec.Split('!');

                        if (t.Length > 2)
                        {
                            throw new ArgumentException("Too many exclamation marks in a parameter of the keyword");
                        }

                        id = t[0];
                        name = t[1];
                    }

                    if (id.EndsWith("c"))
                    {
                        id = id.Substring(0, id.Length - 1);

                        if (!String.IsNullOrEmpty(id))
                        {
                            method.ContextArg = int.Parse(id);
                        }

                        method.ContextName = name;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            if (!String.IsNullOrEmpty(id))
                            {
                                method.IdArg = int.Parse(id);
                            }

                            method.IdName = name;
                        }
                        else if (i == 1)
                        {
                            if (!String.IsNullOrEmpty(id))
                            {
                                method.PluralArg = int.Parse(id);
                            }

                            method.PluralName = name;
                        }
                        else
                        {
                            throw new ArgumentException("Too many parameters for the keyword");
                        }

                        i++;
                    }
                }
            }
            else
            {
                method.IdArg = 0;
            }

            return method;
        }
    }
}
