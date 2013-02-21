using GettextDotNet.Formats;
using Microsoft.CSharp;
using NDesk.Options;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Razor;
using System.Xml.Linq;

namespace GettextDotNet.MessageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Localization localization;
            List<string> files;
            List<LocalizationMethod> methods = new List<LocalizationMethod>();
            HashSet<string> projects = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            string solution_path = null;
            bool show_help = false;
            bool show_location = true;
            bool extract_comments = false;
            string comment_prefix = null;
            string output_file = null;
            var base_path = "";

            var options = new OptionSet() {
                { "p|project=", "add a {PROJECT} to extract messages from",
                   v => projects.Add (v) },
                { "s|solution=", 
                   "path the solution {FILE} to extract messages from",
                    (string v) => solution_path = v },
                { "o|output=", 
                   "the output .po {FILE}. If empty the generated .po file is printed to the console.",
                    (string v) => output_file = v },
                { "k=", "add a {KEYWORD} to be recognized as a translation method",
                   v => methods.Add(ParseKeyword(v)) },
                { "c|add-comments:",  "extract comments (if set only the ones which start with the given {PREFIX})", 
                   v => {comment_prefix = v; extract_comments = true; } },
                { "no-location",  "omit file references for messages", 
                   v => show_location = v != null },
                { "h|help",  "show this message and exit", 
                   v => show_help = v != null },
            };            

            // Parse options
            try
            {
                files = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("MessageExtractor: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `MessageExtractor --help' for more information.");
                return;
            }

            if (methods.Count == 0)
            {
                methods.AddRange(MethodCallCollector.DefaultMethods);
            }

            var collector = new MethodCallCollector(methods);

            // Use files from solution/projects
            if (!show_help && solution_path != null)
            {
                var solution = Solution.Load(Path.GetFullPath(solution_path));
                var filtered_projects = solution.Projects;
                if (projects.Count > 0)
                {
                    filtered_projects = filtered_projects.Where(pr => projects.Contains(pr.Name));
                }

                foreach (var project in filtered_projects)
                {
                    var project_file = XDocument.Load(project.FilePath);
                    var ns = project_file.Root.GetDefaultNamespace();

                    var view_files = project_file.Descendants(ns + "Content").Select(c => c.Attribute("Include").Value).Where(i => i.EndsWith(".cshtml"));
                    
                    var project_base_path = Path.GetDirectoryName(project.FilePath);
                    // var host = WebRazorHostFactory.CreateHostFromConfig("/");


                    RazorEngineHost host = new RazorEngineHost(new CSharpRazorCodeLanguage());
                    host.DefaultClassName = "TestClass";
                    host.DefaultNamespace = "TestNamespace";

                    var engine = new RazorTemplateEngine(host);

                    foreach (var view in view_files)
                    {
                        // Parse the file using the razor engine
                        GeneratorResults results = null;
                        using (var fstream = File.OpenRead(Path.Combine(project_base_path, view)))
                        {
                            using (TextReader reader = new StreamReader(fstream))
                            {
                                results = engine.GenerateCode(reader, className: null, rootNamespace: null, sourceFileName: Path.Combine(project_base_path, view));
                            }
                        }

                        if (results.Success)
                        {
                            // Use CodeDom to generate source code from the CodeCompileUnit
                            var codeDomProvider = new CSharpCodeProvider();
                            var srcFileWriter = new StringWriter();
                            codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, srcFileWriter, new CodeGeneratorOptions());

                            var code = srcFileWriter.ToString();

                            SyntaxTree syntaxTree = SyntaxTree.ParseText(code, view);

                            var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

                            collector.Visit(root);
                        }
                    }

                    foreach (var document in project.Documents)
                    {
                        var root = (CompilationUnitSyntax)document.GetSyntaxRoot();

                        collector.Visit(root);
                    }
                }

                base_path = Path.GetDirectoryName(solution.FilePath);
            }
            // Use the files passed as arguments
            else if (!show_help && files.Any())
            {
                files = files.Select(f => Path.GetFullPath(f)).ToList();

                foreach (var file in files)
                {
                    var root = (CompilationUnitSyntax)SyntaxTree.ParseFile(file).GetRoot();

                    collector.Visit(root);
                } 
                
                // Find common base path of the files
                var common_paths =
                        from len in Enumerable.Range(0, files.Min(s => s.Length)).Reverse()
                        let possibleMatch = files.First().Substring(0, len)
                        where files.All(f => f.StartsWith(possibleMatch))
                        select possibleMatch;

                base_path = Path.GetDirectoryName(common_paths.First());
            }
            // Show help message
            else
            {
                Console.WriteLine("MessageExtractor [options] [file1] [file2] ...");
                Console.WriteLine("");
                Console.WriteLine("Options: ");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            // Extract all messages
            localization = ExtractMessages(collector);

            // Remove extracted comments if -c is not set
            if (!extract_comments)
            {
                foreach (var message in localization.GetMessages())
                {
                    message.Comments.Clear();
                }
            }
            // Remove comments which do not start with the specified prefix
            else if (!String.IsNullOrEmpty(comment_prefix))
            {
                var length = comment_prefix.Length;
                foreach (var message in localization.GetMessages())
                {
                    message.Comments = (from comment in message.Comments
                                        where comment.StartsWith(comment_prefix)
                                        select comment.Substring(length).Trim()).ToList();
                }
            }

            // Remove extracted references if they should be excluded
            if (!show_location)
            {
                foreach (var message in localization.GetMessages())
                {
                    message.References.Clear();
                }
            }
            // Remove common base from the beginning of each reference
            else
            {
                var base_length = base_path.Length + 1;
                foreach (var message in localization.GetMessages())
                {
                    message.References = message.References.Select(r => r.StartsWith(base_path) ? r.Substring(base_length) : r).ToList();
                }
                    
            }

            Stream stream;
            if (!String.IsNullOrEmpty(output_file))
            {
                stream = File.Create(output_file);
            }
            else
            {
                stream = Console.OpenStandardOutput();
            }

            // Write to .po file
            // TODO: Support for other formats
            new POFormat().Write(localization, stream, true);

            stream.Close();
        }

        private static Localization ExtractMessages(MethodCallCollector collector)
        {
            var localization = new Localization();

            foreach (var occurence in collector.Occurences)
            {
                foreach (var method in collector.MethodMapping[occurence.Item1])
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

                return GetString(expr);
            }

            return null;
        }

        private static string GetString(SyntaxNode expr)
        {
            if (expr is LiteralExpressionSyntax)
            {
                var token = ((LiteralExpressionSyntax)expr).Token;

                /*
                // Only parse string literals
                if (token.Kind == SyntaxKind.StringLiteralToken)
                {
                    return token.Value as string;
                }
                */

                return token.ValueText;
            }
            else if (expr is BinaryExpressionSyntax)
            {
                if (expr.Kind == SyntaxKind.AddExpression)
                {
                    var left = GetString(((BinaryExpressionSyntax)expr).Left);
                    var right = GetString(((BinaryExpressionSyntax)expr).Right);

                    if (left != null && right != null)
                    {
                        return left + right;
                    }
                }
            }
            else if (expr is ParenthesizedExpressionSyntax)
            {
                return GetString(((ParenthesizedExpressionSyntax)expr).Expression);
            }

            return null;
        }

        private static void ParseMessage(Localization localization, LocalizationMethod method, SyntaxReference reference)
        {
            var node = reference.GetSyntax() as InvocationExpressionSyntax;

            // For some weird reason lines are zero-based
            var line = node.SyntaxTree.GetLineSpan(node.Span, false).StartLinePosition.Line + 1;
            var fname = node.SyntaxTree.FilePath;

            // Correct line numbers for view (.cshtml) files using the #line directive
            var parent = node.Ancestors().First(p => p.Parent is BlockSyntax);
            var line_trivia = parent.GetLeadingTrivia().FirstOrDefault(t => t.Kind == SyntaxKind.LineDirectiveTrivia);
            if (line_trivia != null && line_trivia.Kind != SyntaxKind.None)
            {
                var info = line_trivia.ToString().Substring(line_trivia.ToString().IndexOf("#line") + 5).TrimStart();
                var parts = info.Split(new char[] { ' ', '\t', '\r', '\n' }).Where(p => !String.IsNullOrEmpty(p)).ToArray();

                line = int.Parse(parts[0]);
            }

            var refr = fname + ":" + line;

            var expression = node.Ancestors().First(p => p.Parent is BlockSyntax);
            var comments = (from trivia in expression.GetLeadingTrivia()
                            where trivia.Kind == SyntaxKind.SingleLineCommentTrivia
                                || trivia.Kind == SyntaxKind.DocumentationCommentTrivia
                            select trivia.ToFullString().Substring(2).Trim()).ToList();

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
                    message.Comments.AddRange(comments);
                }
                else
                {
                    message = new Message
                    {
                        Id = id,
                        Context = context,
                        Comments = comments,
                        Plural = plural
                    };
                    message.References.Add(refr);
                    message.Flags.Add("csharp-format");

                    localization.Add(message);
                }
            }
        }

        private static LocalizationMethod ParseKeyword(string kw)
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

                foreach (var spec in specs)
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
                    Name = method,
                    IdArg = idArg,
                    PluralArg = pluralArg,
                    ContextArg = contextArg
                };
        }
    }

    internal class LocalizationMethod
    {
        public string Name { get; set; }
        public int IdArg { get; set; }
        public int? PluralArg { get; set; }
        public int? ContextArg { get; set; }
    }

    internal class MethodCallCollector : SyntaxWalker
    {
        public static readonly LocalizationMethod[] DefaultMethods = new LocalizationMethod[] {
            new LocalizationMethod
            {
                Name = "_",
                IdArg = 0
            }
        };

        public readonly IEnumerable<LocalizationMethod> Methods;

        public readonly Dictionary<string, List<LocalizationMethod>> MethodMapping;

        public readonly List<Tuple<string, SyntaxReference>> Occurences = new List<Tuple<string, SyntaxReference>>();

        public MethodCallCollector(IEnumerable<LocalizationMethod> methods)
        {
            Methods = methods;
            MethodMapping = Methods.Select(m => m.Name).Distinct().ToDictionary(m => "\\b" + m + "$", m => Methods.Where(m2 => m2.Name.Equals(m)).ToList());
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax)
            {
                // var name = ((MemberAccessExpressionSyntax)node.Expression).Name.ToString();
                var name = node.Expression.ToString();
                var key = MethodMapping.Keys.FirstOrDefault(
                    m => Regex.IsMatch(name, m)
                );

                if (key != null)
                {
                    Occurences.Add(Tuple.Create(key, node.GetReference()));
                }
            }
            else if (node.Expression is IdentifierNameSyntax)
            {
                var name = ((IdentifierNameSyntax)node.Expression).Identifier.ToString();

                var key = MethodMapping.Keys.FirstOrDefault(
                    m => Regex.IsMatch(name, m)
                );

                if (key != null)
                {
                    Occurences.Add(Tuple.Create(key, node.GetReference()));
                }
            }

            base.VisitInvocationExpression(node);
        }
    }
}
