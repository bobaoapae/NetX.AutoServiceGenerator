using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetX.AutoServiceGenerator
{
    public static class AutoServiceUtils
    {
        public static Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> GetAllClassAndSubTypesWithAttribute(List<INamedTypeSymbol> candidateClasses, INamedTypeSymbol attribute, bool includeSelf = false)
        {
            var result = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
            foreach (var classSymbol in candidateClasses)
            {
                var handlerCollection = classSymbol?.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Name == attribute.Name);
                if (handlerCollection != null)
                {
                    if (includeSelf)
                    {
                        result.Add(classSymbol, new List<INamedTypeSymbol> { classSymbol });
                    }
                    else
                    {
                        result.Add(classSymbol, new List<INamedTypeSymbol>());
                    }
                }
            }

            foreach (var classSymbol in candidateClasses)
            {
                var baseType = classSymbol?.BaseType;
                if (baseType != null && result.ContainsKey(baseType))
                    result[baseType].Add(classSymbol);
            }

            return result;
        }

        public static List<INamedTypeSymbol> GetAllClassWithInterface(List<INamedTypeSymbol> candidateClasses, INamedTypeSymbol interfaceType)
        {
            var result = new List<INamedTypeSymbol>();
            foreach (var classSymbol in candidateClasses)
            {
                if (classSymbol?.Interfaces.Any(symbol => symbol.Name == interfaceType.Name) ?? false)
                    result.Add(classSymbol);
            }

            return result;
        }

        public static List<INamedTypeSymbol> GetAllClassWithInterfaceWithAttribute(List<INamedTypeSymbol> candidateClasses, INamedTypeSymbol attribute)
        {
            var result = new List<INamedTypeSymbol>();
            foreach (var classSymbol in candidateClasses)
            {
                foreach (var classSymbolInterface in classSymbol.Interfaces)
                {
                    if (classSymbolInterface.GetAttributes().Any(data => data.AttributeClass?.Name == attribute.Name))
                    {
                        result.Add(classSymbol);
                        break;
                    }
                }
            }

            return result;
        }

        public static bool CheckClassIsPartial(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var declaringSyntaxReference in namedTypeSymbol.DeclaringSyntaxReferences)
            {
                foreach (var syntaxToken in ((ClassDeclarationSyntax)declaringSyntaxReference.GetSyntax()).Modifiers)
                {
                    if (syntaxToken.Text == "partial")
                        return true;
                }
            }

            return false;
        }

        public static string GetResource(Assembly assembly, GeneratorExecutionContext context, string resourceName)
        {
            using (var resourceStream = assembly.GetManifestResourceStream($"NetX.AutoServiceGenerator.Resources.{resourceName}.g"))
            {
                if (resourceStream != null)
                    return new StreamReader(resourceStream).ReadToEnd();

                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ASG0001",
                        "Invalid Resource",
                        $"Cannot find {resourceName}.g resource",
                        "",
                        DiagnosticSeverity.Error,
                        true),
                    null));
                return "";
            }
        }

        public static bool IsValidTypeForArgumentOrReturn(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsValueType && namedTypeSymbol.EnumUnderlyingType == null)
                {
                    return true;
                }
                if (namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoSerializeAttribute") && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoDeserializeAttribute"))
                {
                    return true;
                }
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                return IsValidTypeForArgumentOrReturn(arrayTypeSymbol.ElementType);
            }
            return false;
        }

        public static bool NeedUseAutoSerializeOrDeserialize(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoSerializeAttribute") && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoDeserializeAttribute"))
            {
                return true;
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                return NeedUseAutoSerializeOrDeserialize(arrayTypeSymbol.ElementType);
            }
            return false;
        }

        public static string Capitalize(this string source)
        {
            return source.First().ToString().ToUpper() + source.Substring(1);
        }

        public static string DeCapitalize(this string source)
        {
            return source.First().ToString().ToLower() + source.Substring(1);
        }
    }
}