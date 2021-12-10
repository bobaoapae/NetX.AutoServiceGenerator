using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Networking.AutoHandlerGenerator
{
    public static class ContextExtension
    {
        public static Compilation AddSourceAndGetCompilation(ref this GeneratorExecutionContext context, Compilation compilation, string name, SourceText text)
        {
            context.AddSource(name, text);
            var syntax = CSharpSyntaxTree.ParseText(text, (CSharpParseOptions)context.ParseOptions);
            return compilation.AddSyntaxTrees(syntax);
        }
    }
}