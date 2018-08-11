using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FlubuCore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TargetParameterAnalyzer : DiagnosticAnalyzer
    {
        public const string FirstParameterMustBeOfTypeITargetDiagnosticId = "FlubuCore_TargetParameter_001";

        public const string AttributeAndMethodParameterCountMustBeTheSameDiagnosticId = "FlubuCore_TargetParameter_002";

        public const string AttributeAndMethodParameterTypeMustBeTheSameDiagnosticId = "FlubuCore_TargetParameter_003";

        private static readonly LocalizableString FirstTargetParameterTitle  ="Wrong first parameter";

        private static readonly string FirstTargetParameterMessageFormat =
            "First parameter in method '{0}' must be of type ITarget.";

        private static readonly string FirstTargetParameterDescription =
            "First paramter in method must be of type ITarget.";

        private static readonly string ParameterCountTitle = "Wrong parameter count";
      
        private static readonly LocalizableString ParameteCountMessageFormat =
            "Parameters count in attribute and  method '{0}' must be the same.";

        private static readonly LocalizableString ParameterCountDescription =
            "Parametrs count in method and attribute must be the same.";

        private static readonly LocalizableString ParameterTypeNotSameTitle = "Wrong parameter type";

        private static readonly LocalizableString ParameterTypeNotSameMessageFormat =
            "Parameter must be of same type as '{0}' method parameter '{1}'.";

        private static readonly LocalizableString ParameterTypeNotSameDescription =
            "Target parameter must be of same type as method parameter.";

        private const string Category = "TargetDefinition";

        private static DiagnosticDescriptor FirstParameterMustBeOfTypeITarget = new DiagnosticDescriptor(FirstParameterMustBeOfTypeITargetDiagnosticId,
            FirstTargetParameterTitle, FirstTargetParameterMessageFormat, Category, DiagnosticSeverity.Warning,
            isEnabledByDefault: true, description: FirstTargetParameterDescription);

        private static DiagnosticDescriptor AttributeAndMethodParameterCountMustBeTheSame =
            new DiagnosticDescriptor(AttributeAndMethodParameterCountMustBeTheSameDiagnosticId, ParameterCountTitle, ParameteCountMessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: ParameterCountDescription);

        private static DiagnosticDescriptor AttributeAndMethodParameterTypeMustBeTheSame =
            new DiagnosticDescriptor(AttributeAndMethodParameterCountMustBeTheSameDiagnosticId, ParameterTypeNotSameTitle, ParameterTypeNotSameMessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: ParameterTypeNotSameDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(FirstParameterMustBeOfTypeITarget,
                    AttributeAndMethodParameterCountMustBeTheSame, AttributeAndMethodParameterTypeMustBeTheSame);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;
            var attributes = methodSymbol.GetAttributes();

            foreach (var attribute in attributes)
            {
                ImmutableArray<TypedConstant> attributeParameters;
                bool hasAttributeParameters = false;
                if (attribute.ConstructorArguments.Length > 1)
                {
                    hasAttributeParameters = true;
                    attributeParameters = attribute.ConstructorArguments[1].Values;
                }

                if (attribute.AttributeClass.Name != "Target" && attribute.AttributeClass.Name != "TargetAttribute")
                {
                    continue;
                }

                if (methodSymbol.Parameters.Length == 0)
                {
                    var diagnostic = Diagnostic.Create(FirstParameterMustBeOfTypeITarget, methodSymbol.Locations[0],
                        methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                else if (methodSymbol.Parameters[0].Type.Name != "ITarget")
                {
                    var diagnostic = Diagnostic.Create(FirstParameterMustBeOfTypeITarget, methodSymbol.Locations[0],
                        methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }

                else if (hasAttributeParameters && methodSymbol.Parameters.Length != attributeParameters.Length + 1)
                {
                    var diagnostic = Diagnostic.Create(AttributeAndMethodParameterCountMustBeTheSame, methodSymbol.Locations[0], methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    if (!hasAttributeParameters)
                    {
                        continue;
                    }

                    for (int i = 0; i < attributeParameters.Length; i++)
                    {
                        if (attributeParameters[i].Type.Name != methodSymbol.Parameters[i + 1].Type.Name)
                        {
                            var attributeSyntax =
                                (AttributeSyntax) attribute.ApplicationSyntaxReference.GetSyntax(
                                    context.CancellationToken);
                            var argument = attributeSyntax.ArgumentList.Arguments[i + 1];
                            var diagnostic = Diagnostic.Create(AttributeAndMethodParameterTypeMustBeTheSame,
                                argument.GetLocation(), methodSymbol.Name, methodSymbol.Parameters[i + 1].Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }

                }
            }
        }
    }
}