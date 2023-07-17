using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThreadingControl.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MyAnalyzer : DiagnosticAnalyzer
    {
        // Metadata of the analyzer
        public const string DiagnosticId = "CS_SampleAnalyzer";

        private static readonly string Title = "Call this method from appropriate thread only";
        private static readonly string MessageFormat = "Forbidden X-thread invocation";
        private static readonly string ChainMessageFormat = "Forbidden X-thread invocation: '{0}'";
        private static readonly string Description = "Methods with with 'ThreadControlAttribute' cannot be invoked from methods with another ThreadName specified";
        private const string Category = "Usage";
        private const string AttributeSuffix = "Attribute";

        public static readonly DiagnosticDescriptor DirectCallRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        public static readonly DiagnosticDescriptor ChainCallRule = new DiagnosticDescriptor(DiagnosticId, Title, ChainMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            // The AnalyzeNode method will be called for each InvocationExpression of the Syntax tree
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Attribute);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DirectCallRule, ChainCallRule);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;
            if (attributeSyntax != null 
                && IsThreadControlAttribute(attributeSyntax, out var parentMethodThread)
                && IsAttachedToMethod(attributeSyntax, out var methodSyntax))
            {
                var invocations = new List<ThreadControlMethodInvocationChain>();
                GetControlThreadMethodsRecursively(context, methodSyntax, parentMethodThread, invocations,
                    ImmutableList<InvocationExpressionSyntax>.Empty);

                foreach (var chain in invocations.Where(x => !ThreadsAreEqual(parentMethodThread, x.Thread)))
                {
                    var firstInvocation = chain.InvocationChain.First();
                    if (chain.InvocationChain.Any(x =>
                            IsWrappedWithPipelineCall(x, context, out var pipelineThread) &&
                            ThreadsAreEqual(pipelineThread, chain.Thread)))
                    {
                        continue;
                    }

                    if (chain.InvocationChain.Count == 1)
                    {
                        var diagnostic = Diagnostic.Create(DirectCallRule, firstInvocation.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                    else
                    {
                        var diagnostic = Diagnostic.Create(ChainCallRule, firstInvocation.GetLocation(),
                            InvocationChainToString(chain.InvocationChain));
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsWrappedWithPipelineCall(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context,
            out AttributeArgumentSyntax thread)
        {
            thread = default;
            AttributeArgumentSyntax pipelineThread = default;
            if (IsWrappedWith<InvocationExpressionSyntax>(invocation, x => IsPipelineCall(x, context, out pipelineThread),
                    out var result))
            {
                thread = pipelineThread;
                return true;
            }

            return false;
        }

        private static bool IsPipelineCall(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context,
            out AttributeArgumentSyntax thread)
        {
            thread = default;

            if (TryGetDeclaration(invocation, context, out var declaration))
            {
                return IsPipelineMember(declaration, out thread);
            }

            return false;
        }

        private static bool IsWrappedWith<T>(SyntaxNode node, Func<T, bool> predicate, out T result)
        {
            result = default;
            if (node.Parent is T cast)
            {
                if (predicate != null && predicate(cast))
                {
                    result = cast;
                    return true;
                }
            }

            if (node.Parent == null || node.Parent is MethodDeclarationSyntax)
            {
                return false;
            }

            return IsWrappedWith<T>(node.Parent, predicate, out result);
        }

        private static bool ThreadsAreEqual(AttributeArgumentSyntax argument1, AttributeArgumentSyntax argument2)
        {
            return string.Equals(argument1.ToFullString(), argument2.ToFullString());
        }

        private static string InvocationChainToString(IEnumerable<InvocationExpressionSyntax> invocationChain) => string.Join(" -> ", invocationChain.Select(x => x.Expression.ToString()));

        private class ThreadControlMethodInvocationChain
        {
            public AttributeArgumentSyntax Thread { get; private set; }

            public IReadOnlyCollection<InvocationExpressionSyntax> InvocationChain { get; private set; }

            public ThreadControlMethodInvocationChain(AttributeArgumentSyntax thread, IReadOnlyCollection<InvocationExpressionSyntax> invocationChain)
            {
                Thread = thread;
                InvocationChain = invocationChain;
            }
        }

        private static void GetControlThreadMethodsRecursively(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodSyntax, AttributeArgumentSyntax parentMethodThread, List<ThreadControlMethodInvocationChain> result,
            ImmutableList<InvocationExpressionSyntax> invocationChain)
        {
            var invocations = GetAllInvocations(methodSyntax);

            foreach (var invocation in invocations)
            {
                if (TryGetDeclaration(invocation, context, out var declaration))
                {
                    var nestedInvocationChain = invocationChain.Add(invocation);
                    if (IsThreadControlMember(declaration, out var targetMethodThread) &&
                        !parentMethodThread.Equals(targetMethodThread))
                    {
                        result.Add(new ThreadControlMethodInvocationChain(targetMethodThread, nestedInvocationChain));
                    }
                    else
                    {
                        GetControlThreadMethodsRecursively(context, declaration, parentMethodThread, result, nestedInvocationChain);
                    }
                }
            }
        }

        private static bool IsAttachedToMethod(AttributeSyntax attributeSyntax,
            out MethodDeclarationSyntax methodDeclarationSyntax)
        {
            methodDeclarationSyntax = attributeSyntax?.Parent?.Parent as MethodDeclarationSyntax;
            return methodDeclarationSyntax != null;
        }

        private static IEnumerable<InvocationExpressionSyntax> GetAllInvocations(MethodDeclarationSyntax methodSyntax)
        {
            var allStatements = methodSyntax.Body.Statements.SelectMany(GetChildrenRecursive);
            var invocations = allStatements.OfType<InvocationExpressionSyntax>();
            return invocations;
        }

        private static IEnumerable<SyntaxNode> GetChildrenRecursive(SyntaxNode statementSyntax)
        {
            var children = statementSyntax.ChildNodes();
            foreach (var child in children)
            {
                yield return child;
                foreach (var nested in GetChildrenRecursive(child))
                {
                    yield return nested;
                }
            }
        }

        private static bool TryGetDeclaration(ExpressionSyntax invocation, SyntaxNodeAnalysisContext context, out MethodDeclarationSyntax declaration)
        {
            declaration = default;

            var methodSymbol = context
                .SemanticModel
                .GetSymbolInfo(invocation, context.CancellationToken)
                .Symbol as IMethodSymbol;

            if (methodSymbol != null)
            {
                var syntaxReference = methodSymbol
                    .DeclaringSyntaxReferences
                    .FirstOrDefault();

                declaration = syntaxReference?.GetSyntax(context.CancellationToken) as MethodDeclarationSyntax;
            }

            return declaration != default;
        }

        private static bool IsThreadControlMember(MemberDeclarationSyntax methodDeclarationSyntax,
            out AttributeArgumentSyntax thread)
        {
            return HasAttribute(methodDeclarationSyntax, nameof(ThreadControlAttribute), out thread);
        }

        private static bool IsPipelineMember(MemberDeclarationSyntax methodDeclarationSyntax,
            out AttributeArgumentSyntax thread)
        {
            return HasAttribute(methodDeclarationSyntax, nameof(PipelineAttribute), out thread);
        }

        private static bool HasAttribute(MemberDeclarationSyntax methodDeclarationSyntax, string attributeName, out AttributeArgumentSyntax attributeArgument)
        {
            var attributes = methodDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes).ToList();

            var attributeSyntax = attributes.FirstOrDefault(x => AttributeNameMatch(x.Name, attributeName));
            if (attributeSyntax != null)
            {
                var argument = attributeSyntax.ArgumentList.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    attributeArgument = argument;
                    return true;
                }
            }

            attributeArgument = default;
            return false;
        }

        private static bool AttributeNameMatch(NameSyntax nameSyntax, string attributeName)
        {
            var syntax = nameSyntax.ToString();
            if (syntax == attributeName)
            {
                return true;
            }

            if (attributeName.EndsWith(AttributeSuffix))
            {
                var shortAttributeName = attributeName.Substring(0, attributeName.Length - AttributeSuffix.Length);
                return syntax == shortAttributeName;
            }

            return false;
        }

        private static bool IsThreadControlAttribute(AttributeSyntax attribute, out AttributeArgumentSyntax thread)
        {
            if (AttributeNameMatch(attribute.Name, nameof(ThreadControlAttribute)))
            {
                var argument = attribute.ArgumentList.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    thread = argument;
                    return true;
                }
            }

            thread = default;
            return false;
        }
    }
}
