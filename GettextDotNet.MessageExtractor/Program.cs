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
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Xml.Linq;

namespace GettextDotNet.MessageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Localization localization;
            List<string> files;
            List<LocalizationKeyword> methods = new List<LocalizationKeyword>();
            HashSet<string> projects = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            string solution_path = null;
            bool show_help = false;
            bool show_location = true;
            bool extract_comments = false;
            string comment_prefix = null;
            string output_file = null;
            var base_path = "";
            var encoding = Encoding.UTF8;

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
                   v => methods.Add(LocalizationKeyword.Parse(v)) },
                { "c|add-comments:",  "extract comments (if set only the ones which start with the given {PREFIX})", 
                   v => {comment_prefix = v; extract_comments = true; } },
                { "no-location",  "omit file references for messages", 
                   v => show_location = v != null },
                { "from-code=",  "omit file references for messages", 
                   v => encoding = Encoding.GetEncoding(v) },
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
                methods.AddRange(KeywordExtractor.DefaultKeywords);
            }

            var collector = new KeywordExtractor(methods);

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


                    RazorEngineHost host = new RazorEngineHost(new CSharpRazorCodeLanguage())
                    {
                        DefaultClassName = "TestClass",
                        DefaultNamespace = "TestNamespace",
                        DefaultBaseClass = "DynamicObject",
                        GeneratedClassContext = new GeneratedClassContext(
                             GeneratedClassContext.DefaultExecuteMethodName,
                             GeneratedClassContext.DefaultWriteMethodName,
                             GeneratedClassContext.DefaultWriteLiteralMethodName,
                             "WriteTo",
                             "WriteLiteralTo",
                             "HelperResult")
                    };

                    var engine = new RazorTemplateEngine(host);

                    foreach (var view in view_files)
                    {
                        // Parse the file using the razor engine
                        GeneratorResults results = null;
                        var vpath = Path.Combine(project_base_path, view);
                        using (TextReader reader = new StreamReader(File.OpenRead(vpath)))
                        {
                            results = engine.GenerateCode(reader, null, null, vpath);
                        }

                        if (results != null && results.Success)
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
            localization = collector.ExtractMessages();

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

            localization.SetHeader("MIME-Version", "1.0");
            localization.SetHeader("Content-Type", String.Format("text/plain; charset={0}", encoding.WebName));
            localization.SetHeader("Content-Transfer-Encoding", "8bit");

            // Write to .po file
            // TODO: Support for other formats
            new POFormat().Write(localization, stream, true);

            stream.Close();
        }
    }
}
