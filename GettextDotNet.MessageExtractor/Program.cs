using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using NDesk.Options;
using Roslyn.Compilers.Common;
using System.IO;
using GettextDotNet.Formats;

namespace GettextDotNet.MessageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            HashSet<string> projects = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            string solution_path = null;
            List<LocalizationMethod> methods = new List<LocalizationMethod>();
            bool show_help = false;

            var p = new OptionSet() {
                { "p|project=", "add a project to extract messages from",
                   v => projects.Add (v) },
                { "s|solution=", 
                   "path the solution file to extract messages from",
                    (string v) => solution_path = v },
                { "k=", "add a keyword to be recognized as a translation method",
                   v => methods.Add(ParseKeyword(v)) },
                { "h|help",  "show this message and exit", 
                   v => show_help = v != null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);

                if (!show_help && solution_path != null)
                {
                    var solution = Solution.Load(Path.GetFullPath(solution_path));
                    var filtered_projects = solution.Projects;
                    if (projects.Count > 0)
                    {
                        filtered_projects = filtered_projects.Where(pr => projects.Contains(pr.Name));
                    }

                    if (methods.Count == 0)
                    {
                        methods.AddRange(MethodCallCollector.DefaultMethods);
                    }

                    var localization = ExtractMessages(filtered_projects, methods);

                    Stream stream;

                    if (extra.Any())
                    {
                        stream = File.OpenRead(extra[0]);
                    }
                    else
                    {
                        stream = Console.OpenStandardOutput();
                    }

                    new POFormat().Write(localization, stream, true);

                    stream.Close();
                }
                else
                {
                    Console.Write("MessageExtractor: ");
                    p.WriteOptionDescriptions(Console.Out);
                }
            }
            catch (OptionException e)
            {
                Console.Write("MessageExtractor: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `MessageExtractor --help' for more information.");
                return;
            }
        }

        public static Localization ExtractMessages(IEnumerable<IProject> projects, List<LocalizationMethod> methods)
        {
            var localization = new Localization();
            var collector = new MethodCallCollector(methods);

            foreach(var project in projects)
            {
                foreach (var document in project.Documents)
                {
                    var root = (CompilationUnitSyntax)document.GetSyntaxRoot();

                    collector.Visit(root);
                }
            }

            foreach (var occurence in collector.Occurences)
            {
                foreach( var method in collector.MethodMapping[occurence.Item1])
                {
                    ParseMessage(localization, method, occurence.Item2);
                }
            }

            return localization;
        }

        private static string GetStringArg(ArgumentListSyntax args, int? n)
        {
            if (n != null)
            {
                var expr = args.Arguments[(int)n].Expression;

                if (expr is LiteralExpressionSyntax)
                {
                    var token = ((LiteralExpressionSyntax)expr).Token;

                    if (token.Kind == SyntaxKind.StringLiteralToken)
                    {
                        return token.Value as string;
                    }
                }
            }

            return null;
        }

        public static void ParseMessage(Localization localization, LocalizationMethod method, SyntaxReference reference)
        {
            var node = reference.GetSyntax() as InvocationExpressionSyntax;
            var line = node.SyntaxTree.GetLineSpan(node.FullSpan, false).StartLinePosition.Line;
            var fname = node.SyntaxTree.FilePath;
            var refr = fname + ":" + line;

            var id = GetStringArg(node.ArgumentList, method.IdArg);
            var context = GetStringArg(node.ArgumentList, method.ContextArg);
            var plural = GetStringArg(node.ArgumentList, method.PluralArg);

            if (id != null)
            {
                var message = localization.GetMessage(id, context);

                if (message != null)
                {
                    message.References.Add(refr);
                    message.Plural = message.Plural ?? plural;
                }
                else
                {
                    message = new Message
                    {
                        Id = id,
                        Context = context,
                        Plural = plural
                    };
                    message.References.Add(refr);
                    message.Flags.Add("csharp-format");

                    localization.Add(message);
                }
            }
        }

        public static LocalizationMethod ParseKeyword(string kw)
        {
            var name = "";
            var idArg = 0;
            int? pluralArg = null;
            int? contextArg = null;

            var t = kw.Split(':');

            if (t.Length == 1)
            {
                name = kw;
            }
            else if (t.Length == 2)
            {
                name = t[0];
                var specs = t[1].Split(',');

                int i = 0;

                foreach(var spec in specs)
                {
                    if (spec.EndsWith("c"))
                    {
                        contextArg = int.Parse(spec.Substring(0, spec.Length - 1));
                    }
                    else
                    {
                        if (i == 0)
                        {
                            idArg = int.Parse(spec);
                        }
                        else if (i == 1)
                        {
                            pluralArg = int.Parse(spec);
                        }
                        i++;
                    }
                }
            }

            string method, typename = "", ns = "";

            if (name.Contains('.'))
            {
                var parts = name.Split('.');

                method = parts[parts.Length - 1];
                typename = parts[parts.Length - 2];
                ns = String.Join(".", parts.Take(parts.Length - 2));
            }
            else
            {
                method = name;
            }

            return new LocalizationMethod
                {
                    Namespace = ns,
                    TypeName = typename,
                    MethodName = method,
                    IdArg = idArg,
                    PluralArg = pluralArg,
                    ContextArg = contextArg
                };
        }
    }

    internal class LocalizationMethod
    {
        public string Namespace { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public int IdArg { get; set; }
        public int? PluralArg { get; set; }
        public int? ContextArg { get; set; }
    }

    internal class MethodCallCollector : SyntaxWalker
    {
        public static readonly LocalizationMethod[] DefaultMethods = new LocalizationMethod[] {
            new LocalizationMethod
            {
                Namespace = "GettextDotNet",
                TypeName = "Internationalization",
                MethodName = "_",
                IdArg = 0
            }
        };

        public readonly IEnumerable<LocalizationMethod> Methods;

        public readonly Dictionary<string, List<LocalizationMethod>> MethodMapping;

        public MethodCallCollector(IEnumerable<LocalizationMethod> methods)
        {
            Methods = methods;
            MethodMapping = Methods.Select(m => m.MethodName).Distinct().ToDictionary(m => m, m => Methods.Where(m2 => m2.MethodName.Equals(m)).ToList());
        }

        public List<Tuple<string, SyntaxReference>> Occurences = new List<Tuple<string, SyntaxReference>>();

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax)
            {
                var name = ((MemberAccessExpressionSyntax)node.Expression).Name.ToString();

                if (MethodMapping.ContainsKey(name))
                {
                    Occurences.Add(Tuple.Create(name, node.GetReference()));
                }
            }
            else  if (node.Expression is IdentifierNameSyntax)
            {
                var name = ((IdentifierNameSyntax)node.Expression).Identifier.ToString();

                if (MethodMapping.ContainsKey(name))
                {
                    Occurences.Add(Tuple.Create(name, node.GetReference()));
                }

            }
        }
    }
}
