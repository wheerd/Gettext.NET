using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GettextDotNet.MessageExtractor
{
    internal class KeywordExtractor : SyntaxWalker
    {
        public static readonly LocalizationKeyword[] DefaultKeywords = new LocalizationKeyword[] {
            // -k_!m
            new LocalizationKeyword
            {
                Name = "_",
                IdArg = 0,
                MethodAllowed = true
            },
            // -k_c!m:0c,1
            new LocalizationKeyword
            {
                Name = "_c",
                IdArg = 1,
                ContextArg = 0,
                MethodAllowed = true
            },
            // -k_n!m:0,1 
            new LocalizationKeyword
            {
                Name = "_n",
                IdArg = 0,
                PluralArg = 0,
                MethodAllowed = true
            },
            // -kDisplay!a:!Name:DisplayName
            new LocalizationKeyword
            {
                Name = "Display",
                IdName = "Name",
                DefaultContext = "DisplayName",
                AttributeAllowed = true
            },
            // -kStringLength!a:!ErrorMessage
            new LocalizationKeyword
            {
                Name = "StringLength",
                IdName = "ErrorMessage",
                AttributeAllowed = true
            },
            // -kCompare!a:!ErrorMessage
            new LocalizationKeyword
            {
                Name = "Compare",
                IdName = "ErrorMessage",
                AttributeAllowed = true
            }
        };

        private readonly Dictionary<string, List<LocalizationKeyword>> Keywords;

        private readonly Queue<Tuple<string, SyntaxReference>> Occurences = new Queue<Tuple<string, SyntaxReference>>();

        public KeywordExtractor(IEnumerable<LocalizationKeyword> methods)
        {
            Keywords = methods.Select(m => m.Name).Distinct().ToDictionary(m => "\\b" + m + "$", m => methods.Where(m2 => m2.Name.Equals(m)).ToList());
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax)
            {
                // var name = ((MemberAccessExpressionSyntax)node.Expression).Name.ToString();
                var name = node.Expression.ToString();
                var key = Keywords.Keys.FirstOrDefault(
                    m => Regex.IsMatch(name, m)
                );

                if (key != null)
                {
                    Occurences.Enqueue(Tuple.Create(key, node.GetReference()));
                }
            }
            else if (node.Expression is IdentifierNameSyntax)
            {
                var name = ((IdentifierNameSyntax)node.Expression).Identifier.ToString();

                var key = Keywords.Keys.FirstOrDefault(
                    m => Regex.IsMatch(name, m)
                );

                if (key != null)
                {
                    Occurences.Enqueue(Tuple.Create(key, node.GetReference()));
                }
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitAttributeList(AttributeListSyntax node)
        {
            base.VisitAttributeList(node);

            foreach (var attribute in node.Attributes)
            {
                var name = attribute.Name.ToString();
                var key = Keywords.Keys.FirstOrDefault(
                    m => Regex.IsMatch(name, m)
                );

                if (key != null)
                {
                    Occurences.Enqueue(Tuple.Create(key, attribute.GetReference()));
                }
            }
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            base.VisitObjectCreationExpression(node);

            var name = node.Type.ToString();
            var key = Keywords.Keys.FirstOrDefault(
                m => Regex.IsMatch(name, m)
            );

            if (key != null)
            {
                Occurences.Enqueue(Tuple.Create(key, node.GetReference()));
            }
        }

        public Localization ExtractMessages()
        {
            var localization = new Localization();

            foreach (var occurence in Occurences)
            {
                foreach (var method in Keywords[occurence.Item1])
                {
                    ParseMessage(localization, method, occurence.Item2);
                }
            }

            return localization;
        }

        private static string GetStringArg(ArgumentListSyntax args, int? n, string name = null)
        {
            if (args == null)
            {
                return null;
            }

            if (name != null)
            {
                var arg = args.Arguments.FirstOrDefault(a => (a.NameColon != null && name.Equals(a.NameColon.Name.Identifier.Value)));

                if (arg != null)
                {
                    return GetString(arg.Expression);
                }
            }

            if (n != null)
            {
                var expr = args.Arguments[(int)n].Expression;

                return GetString(expr);
            }

            return null;
        }

        private static string GetStringArg(AttributeArgumentListSyntax args, int? n, string name = null)
        {
            if (args == null)
            {
                return null;
            }

            if (name != null)
            {
                var arg = args.Arguments.FirstOrDefault(a => (a.NameEquals != null && name.Equals(a.NameEquals.Name.Identifier.Value)) || (a.NameColon != null && name.Equals(a.NameColon.Name.Identifier.Value)));

                if (arg != null)
                {
                    return GetString(arg.Expression);
                }
            }

            if (n != null)
            {
                var expr = args.Arguments[(int)n].Expression;

                return GetString(expr);
            }

            return null;
        }

        private static string GetStringArg(InitializerExpressionSyntax args, string name = null)
        {
            if (args == null)
            {
                return null;
            }

            var assigns = args.Expressions.Where(e => e is BinaryExpressionSyntax && e.Kind == SyntaxKind.AssignExpression).Select(e => (BinaryExpressionSyntax)e);
            var val = assigns.FirstOrDefault(e => e.Left.ToString().Equals(name));

            if (val != null)
            {
                return GetString(val.Right);
            }

            return null;
        }

        private static string GetString(SyntaxNode expr)
        {
            if (expr is LiteralExpressionSyntax)
            {
                return ((LiteralExpressionSyntax)expr).Token.ValueText;
            }
            // Strings combined by +
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
            // Needed to parse the <text> blocks used in Razor templates
            // which are wrapped in some complicated lambda expression syntax
            else if (expr is SimpleLambdaExpressionSyntax)
            {
                var lambda = (SimpleLambdaExpressionSyntax)expr;
                var param = lambda.Parameter.Identifier.ToString();
                var body = lambda.Body as ObjectCreationExpressionSyntax;

                // Lambda expression: item => new HelperResult
                if (param.Equals("item") && body != null)
                {
                    var block = ((SimpleLambdaExpressionSyntax)body.ArgumentList.Arguments[0].Expression).Body as BlockSyntax;

                    var methodCalls = block.Statements
                        .Where(e => e is ExpressionStatementSyntax)
                        .Select(e => ((ExpressionStatementSyntax)e).Expression)
                        .Where(e => e is InvocationExpressionSyntax)
                        .Select(e => (InvocationExpressionSyntax)e);

                    // Collect all strings from WriteLiteralTo()-calls
                    var str = "";
                    foreach (var mc in methodCalls)
                    {
                        if (mc.Expression.ToString().Equals("WriteLiteralTo"))
                        {
                            str += GetStringArg(mc.ArgumentList, 1);
                        }
                    }

                    // Trim each line to allow html indentation while not completely screwing up the key
                    return String.Join("\n", str.Split('\n').Select(s => s.Trim())).Trim();
                }
            }

            return null;
        }

        private static void ParseMessage(Localization localization, LocalizationKeyword method, SyntaxReference reference)
        {
            var node = reference.GetSyntax();

            // For some weird reason lines are zero-based
            var line = node.SyntaxTree.GetLineSpan(node.Span, false).StartLinePosition.Line + 1;
            var fname = node.SyntaxTree.FilePath;

            // Correct line numbers for view (.cshtml) files using the #line directive
            var expression = node.Ancestors().First(p => p.Parent is BlockSyntax || p is ClassDeclarationSyntax);
            var line_trivia = expression.GetLeadingTrivia().FirstOrDefault(t => t.Kind == SyntaxKind.LineDirectiveTrivia);
            if (line_trivia != null && line_trivia.Kind != SyntaxKind.None)
            {
                var info = line_trivia.ToString().Substring(line_trivia.ToString().IndexOf("#line") + 5).TrimStart();
                var parts = info.Split(new char[] { ' ', '\t', '\r', '\n' }).Where(p => !String.IsNullOrEmpty(p)).ToArray();

                line = int.Parse(parts[0]);
            }

            var refr = fname + ":" + line;


            var comments = (from trivia in expression.GetLeadingTrivia()
                            where trivia.Kind == SyntaxKind.SingleLineCommentTrivia
                                || trivia.Kind == SyntaxKind.DocumentationCommentTrivia
                            select trivia.ToFullString().Substring(2).Trim()).ToList();

            string id = "", context = null, plural = null;

            if (node is InvocationExpressionSyntax && method.MethodAllowed)
            {
                id = GetStringArg(((InvocationExpressionSyntax)node).ArgumentList, method.IdArg, method.IdName);
                context = GetStringArg(((InvocationExpressionSyntax)node).ArgumentList, method.ContextArg, method.ContextName);
                plural = GetStringArg(((InvocationExpressionSyntax)node).ArgumentList, method.PluralArg, method.PluralName);
            }
            else if (node is AttributeSyntax && method.AttributeAllowed)
            {
                id = GetStringArg(((AttributeSyntax)node).ArgumentList, method.IdArg, method.IdName);
                context = GetStringArg(((AttributeSyntax)node).ArgumentList, method.ContextArg, method.ContextName);
                plural = GetStringArg(((AttributeSyntax)node).ArgumentList, method.PluralArg, method.PluralName);
            }
            else if (node is ObjectCreationExpressionSyntax && method.ClassAllowed)
            {
                var oce = (ObjectCreationExpressionSyntax)node;

                id = GetStringArg(oce.ArgumentList, method.IdArg, method.IdName) ?? GetStringArg(oce.Initializer, method.IdName);
                context = GetStringArg(oce.ArgumentList, method.ContextArg, method.ContextName) ?? GetStringArg(oce.Initializer, method.ContextName);
                plural = GetStringArg(oce.ArgumentList, method.PluralArg, method.PluralName) ?? GetStringArg(oce.Initializer, method.PluralName);
            }

            context = context ?? method.DefaultContext;

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
    }
}
