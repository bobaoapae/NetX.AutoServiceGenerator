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
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarationsServer = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), 
                transform: static (ctx, _) => GetSemanticTargetForGenerationServer(ctx))
            .Where(static m => m is not null);
        
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClassesServer 
            = context.CompilationProvider.Combine(classDeclarationsServer.Collect());

        context.RegisterSourceOutput(compilationAndClassesServer, 
            static (spc, source) => AutoServiceServerGenerator.Generate(source.Item1, source.Item2, spc));
        
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarationsClient = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s), 
                transform: static (ctx, _) => GetSemanticTargetForGenerationClient(ctx))
            .Where(static m => m is not null);
        
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClassesClient 
            = context.CompilationProvider.Combine(classDeclarationsClient.Collect());

        context.RegisterSourceOutput(compilationAndClassesClient, 
            static (spc, source) => AutoServiceClientGenerator.Generate(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;

    private static ClassDeclarationSyntax GetSemanticTargetForGenerationServer(GeneratorSyntaxContext context)
    {
        var autoServiceServerManagerInterfaceDefinition = context.SemanticModel.Compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceServerManager");
        
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        
        var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (model is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.Interfaces.Any(symbol => SymbolEqualityComparer.Default.Equals(symbol, autoServiceServerManagerInterfaceDefinition)))
            {
                return classDeclarationSyntax;
            }
        }

        return null;
    }
    
    private static ClassDeclarationSyntax GetSemanticTargetForGenerationClient(GeneratorSyntaxContext context)
    {
        var autoServiceClientManagerInterfaceDefinition = context.SemanticModel.Compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceClientManager");
        
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        
        var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (model is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.Interfaces.Any(symbol => SymbolEqualityComparer.Default.Equals(symbol, autoServiceClientManagerInterfaceDefinition)))
            {
                return classDeclarationSyntax;
            }
        }

        return null;
    }
}