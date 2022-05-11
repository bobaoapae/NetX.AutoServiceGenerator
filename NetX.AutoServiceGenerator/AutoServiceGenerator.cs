using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetX.AutoServiceGenerator;

[Generator]
public class AutoServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarationsServer = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx));

        var compilationAndClassesServer = context.CompilationProvider.Combine(classDeclarationsServer.Where(static (namedTypeSymbol) => IsNamedTypeForGenerationServer(namedTypeSymbol)).Collect());

        context.RegisterSourceOutput(compilationAndClassesServer,
            static (spc, source) => AutoServiceServerGenerator.Generate(source.Item1, source.Item2, spc));

        var compilationAndClassesClient = context.CompilationProvider.Combine(classDeclarationsServer.Where(static (namedTypeSymbol) => IsNamedTypeForGenerationClient(namedTypeSymbol)).Collect());

        context.RegisterSourceOutput(compilationAndClassesClient,
            static (spc, source) => AutoServiceClientGenerator.Generate(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax && AutoServiceUtils.CheckClassIsPublic(classDeclarationSyntax) && AutoServiceUtils.CheckClassIsPartial(classDeclarationSyntax);
    }

    private static INamedTypeSymbol GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        return (INamedTypeSymbol)model;;
    }

    private static bool IsNamedTypeForGenerationServer(INamedTypeSymbol namedTypeSymbol)
    {
        return AutoServiceUtils.CheckClassIsPublic(namedTypeSymbol) && 
               AutoServiceUtils.CheckClassIsPartial(namedTypeSymbol) &&
               namedTypeSymbol.Interfaces.Any(symbol => symbol.Name == "IAutoServiceServerManager");
    }

    private static bool IsNamedTypeForGenerationClient(INamedTypeSymbol namedTypeSymbol)
    {
        return AutoServiceUtils.CheckClassIsPublic(namedTypeSymbol) && 
               AutoServiceUtils.CheckClassIsPartial(namedTypeSymbol) &&
               namedTypeSymbol.Interfaces.Any(symbol => symbol.Name == "IAutoServiceClientManager");
    }
}