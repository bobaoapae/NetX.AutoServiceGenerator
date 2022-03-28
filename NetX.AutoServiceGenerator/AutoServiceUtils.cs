using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetX.AutoServiceGenerator
{
    public static class AutoServiceUtils
    {
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

        public static bool CheckClassIsPartial(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var declaringSyntaxReference in namedTypeSymbol.DeclaringSyntaxReferences)
            {
                if (CheckClassIsPartial((ClassDeclarationSyntax) declaringSyntaxReference.GetSyntax()))
                    return true;
            }

            return false;
        }

        public static bool CheckClassIsPublic(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var declaringSyntaxReference in namedTypeSymbol.DeclaringSyntaxReferences)
            {
                if (CheckClassIsPublic((ClassDeclarationSyntax) declaringSyntaxReference.GetSyntax()))
                    return true;
            }

            return false;
        }

        public static bool CheckClassIsPublic(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);
        }

        public static bool CheckClassIsPartial(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        public static string GetResource(Assembly assembly, SourceProductionContext context, string resourceName)
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

                if (IsList(namedTypeSymbol))
                {
                    return IsValidTypeForArgumentOrReturn(namedTypeSymbol.TypeArguments[0]);
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
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoSerializeAttribute") && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoDeserializeAttribute"))
                {
                    return true;
                }

                if (IsList(namedTypeSymbol))
                {
                    return NeedUseAutoSerializeOrDeserialize(namedTypeSymbol.TypeArguments[0]);
                }
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                return NeedUseAutoSerializeOrDeserialize(arrayTypeSymbol.ElementType);
            }

            return false;
        }

        public static bool IsList(ITypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(symbol =>
                symbol.Name == "ICollection" || symbol.Name == "IReadOnlyCollection`1");
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