using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NetX.AutoServiceGenerator
{
    [Generator]
    public class AutoServiceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            #region InitializeCandidateClass

            var assembly = Assembly.GetExecutingAssembly();

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;


            var candidateClasses = new List<INamedTypeSymbol>();

            foreach (var cls in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(cls.SyntaxTree);

                var classSymbol = model.GetDeclaredSymbol(cls);
                candidateClasses.Add(classSymbol);
            }

            if (candidateClasses.Count == 0)
                return;

            #endregion

            #if DEBUG
            if (!Debugger.IsAttached)
                Debugger.Launch();
            #endif

            #region LoadDefinitions

            var autoServiceConsumerAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceConsumer");
            var autoServiceAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceAttribute");
            var autoServiceClientManagerInterfaceDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceClientManager");
            var autoServiceServerManagerInterfaceDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceServerManager");

            if (autoServiceConsumerAttributeDefinition == null || autoServiceAttributeDefinition == null || autoServiceClientManagerInterfaceDefinition == null || autoServiceServerManagerInterfaceDefinition == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ASG0001",
                        "Missing reference to NetX.AutoServiceGenerator.Definitions",
                        "Missing reference to NetX.AutoServiceGenerator.Definitions",
                        "",
                        DiagnosticSeverity.Error,
                        true),
                    null));
                return;
            }

            #endregion

            #region LoadResources

            var autoServiceClientConsumerResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceClientConsumer");
            var autoServiceClientConsumerMethodResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceClientConsumerMethod");
            var autoServiceServerResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServer");
            var autoServiceServerManagerResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerManager");
            var autoServiceServerManagerSessionResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerManagerSession");
            var autoServiceServerProcessorResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerProcessor");
            var autoServiceServerProcessorMethodProxyResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerProcessorMethodProxy");


            if (
                autoServiceClientConsumerResource == ""
                || autoServiceClientConsumerMethodResource == ""
                || autoServiceServerResource == ""
                || autoServiceServerManagerResource == ""
                || autoServiceServerManagerSessionResource == ""
                || autoServiceServerProcessorResource == ""
                || autoServiceServerProcessorMethodProxyResource == ""
            )
                return;

            #endregion

            #region ValidateCorrectlyImplementation

            var autoServiceServerManagers = AutoServiceUtils.GetAllClassWithInterface(candidateClasses, autoServiceServerManagerInterfaceDefinition);

            if (autoServiceServerManagers.Count > 1)
            {
                foreach (var serviceServerManager in autoServiceServerManagers)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0003",
                            "Only one AutoServiceServerManager is allowed per project",
                            "Only one AutoServiceServerManager is allowed per project",
                            "",
                            DiagnosticSeverity.Error,
                            true),
                        serviceServerManager.Locations[0]));
                }

                return;
            }

            var autoServiceServerManager = autoServiceServerManagers[0];

            if (!AutoServiceUtils.CheckClassIsPartial(autoServiceServerManager))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ASG0004",
                        "Class need to be partial",
                        "Class need to be partial",
                        "",
                        DiagnosticSeverity.Error,
                        true),
                    autoServiceServerManager.Locations[0]));
                return;
            }

            var allImplementedServerServices = AutoServiceUtils.GetAllClassWithInterfaceWithAttribute(candidateClasses, autoServiceAttributeDefinition);

            foreach (var implementedServerService in allImplementedServerServices)
            {
                if (!AutoServiceUtils.CheckClassIsPartial(implementedServerService))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0004",
                            "Class need to be partial",
                            "Class need to be partial",
                            "",
                            DiagnosticSeverity.Error,
                            true),
                        implementedServerService.Locations[0]));
                    return;
                }
            }

            #endregion

            #region AutoServiceServerGenerator

            var namespaceAutoServiceServerManager = autoServiceServerManager.ContainingNamespace.ToDisplayString();
            var autoServiceServerManagerName = autoServiceServerManager.Name;

            var autoServiceServerManagerSource = string.Format(autoServiceServerManagerResource, namespaceAutoServiceServerManager, autoServiceServerManagerName);
            context.AddSource($"{autoServiceServerManagerName}.g.cs", SourceText.From(autoServiceServerManagerSource, Encoding.UTF8));

            var autoServicesClientConsumerInitializations = new StringBuilder();
            var autoServiceClientConsumerDeclarations = new StringBuilder();

            var autoServiceClientConsumerInterfaces = new List<INamedTypeSymbol>();

            foreach (var attributeData in autoServiceServerManager.GetAttributes())
            {
                if (attributeData.AttributeClass?.Name == autoServiceConsumerAttributeDefinition.Name)
                {
                    autoServiceClientConsumerInterfaces.Add((INamedTypeSymbol)attributeData.ConstructorArguments[0].Value);
                }
            }
            
            var checkDuplicateAutoServiceServerManagerSessionSource = new List<string>();

            var autoServiceServerManagerSessionSourceUsings = new StringBuilder();

            foreach (var autoServiceClientConsumerInterface in autoServiceClientConsumerInterfaces)
            {
                if (!checkDuplicateAutoServiceServerManagerSessionSource.Contains(autoServiceClientConsumerInterface.ContainingNamespace.ToString()))
                {
                    checkDuplicateAutoServiceServerManagerSessionSource.Add(autoServiceClientConsumerInterface.ContainingNamespace.ToString());
                    autoServiceServerManagerSessionSourceUsings.AppendLine($"using {autoServiceClientConsumerInterface.ContainingNamespace};");
                }
                
                autoServiceClientConsumerDeclarations.Append('\t', 1).AppendLine($"{autoServiceClientConsumerInterface.Name} {autoServiceClientConsumerInterface.Name.Substring(1)} {{ get; }}");
                autoServicesClientConsumerInitializations.Append('\t', 2).AppendLine($"{autoServiceClientConsumerInterface.Name.Substring(1)} = new {autoServiceClientConsumerInterface.Name.Substring(1)}ClientConsumer(this, streamManager);");

                var serviceImplementations = new StringBuilder();

                var methodCode = 0;
                foreach (var member in autoServiceClientConsumerInterface.GetMembers())
                {
                    if (member is IMethodSymbol methodSymbol)
                    {
                        var methodReturnType = methodSymbol.ReturnType;
                        var methodReturnTypeGeneric = ((INamedTypeSymbol)methodReturnType).TypeArguments[0];
                        var writeParameters = new StringBuilder();
                        var parameters = new StringBuilder();

                        foreach (var parameterSymbol in methodSymbol.Parameters)
                        {
                            //implementedServerService.Name, interfaceServer.Name, methodCode, methodSymbol.Name, autoServiceServerManagerName
                            writeParameters
                                .Append('\t', 2)
                                .AppendLine($"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.Write({parameterSymbol.Name});");
                            parameters.Append($"{parameterSymbol.Type} {parameterSymbol.Name}, ");
                        }

                        parameters.Length -= 2;


                        serviceImplementations
                            .Append('\t', 2)
                            .AppendLine(string.Format(autoServiceClientConsumerMethodResource, autoServiceClientConsumerInterface.Name, methodSymbol.Name, methodReturnType, methodReturnTypeGeneric, parameters, writeParameters, methodCode++));
                    }
                }

                var autoServiceClientConsumerSource = string.Format(autoServiceClientConsumerResource, namespaceAutoServiceServerManager, autoServiceClientConsumerInterface.Name.Substring(1), autoServiceServerManagerName, autoServiceClientConsumerInterface.ContainingNamespace,
                    serviceImplementations);
                context.AddSource($"{autoServiceClientConsumerInterface.Name.Substring(1)}Consumer.g.cs", SourceText.From(autoServiceClientConsumerSource, Encoding.UTF8));
            }

            var autoServiceServerManagerSessionSource = string.Format(autoServiceServerManagerSessionResource, namespaceAutoServiceServerManager, autoServiceServerManagerName, autoServiceClientConsumerDeclarations, autoServicesClientConsumerInitializations, autoServiceServerManagerSessionSourceUsings);
            context.AddSource($"{autoServiceServerManagerName}Session.g.cs", SourceText.From(autoServiceServerManagerSessionSource, Encoding.UTF8));

            var checkDuplicateAutoServiceServerProcessorUsings = new List<string>();

            var autoServiceServerProcessorUsings = new StringBuilder();
            var autoServiceServerProcessorDeclarations = new StringBuilder();
            var autoServiceServerProcessorInitializers = new StringBuilder();
            var autoServiceServerProcessorLoaders = new StringBuilder();
            var autoServiceServerProcessorProxies = new StringBuilder();

            foreach (var implementedServerService in allImplementedServerServices)
            {
                var interfaceServer = implementedServerService.Interfaces[0];

                if (!checkDuplicateAutoServiceServerProcessorUsings.Contains(interfaceServer.ContainingNamespace.ToString()))
                {
                    checkDuplicateAutoServiceServerProcessorUsings.Add(interfaceServer.ContainingNamespace.ToString());
                    autoServiceServerProcessorUsings.AppendLine($"using {interfaceServer.ContainingNamespace};");
                }

                var autoServiceServerSource = string.Format(autoServiceServerResource, implementedServerService.ContainingNamespace.ToDisplayString(), implementedServerService.Name, autoServiceServerManagerName);
                context.AddSource($"{implementedServerService.Name}.g.cs", SourceText.From(autoServiceServerSource, Encoding.UTF8));

                autoServiceServerProcessorDeclarations.Append('\t', 2).AppendLine($"private {interfaceServer.Name} _{implementedServerService.Name.DeCapitalize()};");
                autoServiceServerProcessorInitializers.Append('\t', 2).AppendLine($"_{implementedServerService.Name.DeCapitalize()} = new {implementedServerService.Name}(TryGetCallingSessionProxy, _sessions.TryGetValue);");

                autoServiceServerProcessorLoaders.Append('\t', 2).AppendLine($"_serviceProxies.Add(\"{implementedServerService.Interfaces[0].Name}\", new Dictionary<ushort, InternalProxy>());");

                ushort methodCode = 0;
                foreach (var member in interfaceServer.GetMembers())
                {
                    if (member is IMethodSymbol methodSymbol)
                    {
                        autoServiceServerProcessorLoaders
                            .Append('\t', 2)
                            .AppendLine($"_serviceProxies[\"{interfaceServer.Name}\"].Add({methodCode}, InternalProxy_{implementedServerService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name});");

                        var readParameters = new StringBuilder();
                        var parameters = new StringBuilder();

                        foreach (var parameterSymbol in methodSymbol.Parameters)
                        {
                            readParameters
                                .Append('\t', 2)
                                .AppendLine($"inputBuffer.Read(ref offset, out {parameterSymbol.Type} {parameterSymbol.Name});");
                            parameters.Append($"{parameterSymbol.Name}, ");
                        }

                        parameters.Length -= 2;

                        autoServiceServerProcessorProxies
                            .Append('\t')
                            .AppendLine(string.Format(autoServiceServerProcessorMethodProxyResource, implementedServerService.Name, interfaceServer.Name, methodCode, methodSymbol.Name, autoServiceServerManagerName, readParameters, parameters));

                        methodCode++;
                    }
                }
            }

            var autoServiceServerProcessorSource = string.Format(autoServiceServerProcessorResource, namespaceAutoServiceServerManager, autoServiceServerManagerName, autoServiceServerProcessorDeclarations, autoServiceServerProcessorInitializers, autoServiceServerProcessorLoaders,
                autoServiceServerProcessorProxies, autoServiceServerProcessorUsings);
            context.AddSource($"{autoServiceServerManagerName}Processor.g.cs", SourceText.From(autoServiceServerProcessorSource, Encoding.UTF8));

            #endregion
        }
    }
}