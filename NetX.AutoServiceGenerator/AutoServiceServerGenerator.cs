using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NetX.AutoServiceGenerator;

public static class AutoServiceServerGenerator
{
    public static void Generate(Compilation compilation, ImmutableArray<INamedTypeSymbol> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            var autoServiceConsumerAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceConsumerAttribute");
            var autoServiceProviderAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceProviderAttribute");
            var autoServiceServerManagerInterfaceDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceServerManager");

            if (autoServiceConsumerAttributeDefinition == null || autoServiceProviderAttributeDefinition == null || autoServiceServerManagerInterfaceDefinition == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ASG0001",
                        "Missing reference to NetX.AutoServiceGenerator.Definitions",
                        "Missing reference to NetX.AutoServiceGenerator.Definitions",
                        "AutoServiceGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    null));
                return;
            }

            var autoServiceClientConsumerResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceClientConsumer");
            var autoServiceClientConsumerMethodResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceClientConsumerMethod");
            var autoServiceClientConsumerMethodResourceVoid = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceClientConsumerMethodVoid");
            var autoServiceServerResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServer");
            var autoServiceServerManagerResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerManager");
            var autoServiceServerManagerSessionResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerManagerSession");
            var autoServiceServerProcessorResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerProcessor");
            var autoServiceServerProcessorMethodProxyResource = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerProcessorMethodProxy");
            var autoServiceServerProcessorMethodProxyResourceVoid = AutoServiceUtils.GetResource(assembly, context, "Server.AutoServiceServerProcessorMethodProxyVoid");

            if (
                autoServiceClientConsumerResource == ""
                || autoServiceClientConsumerMethodResource == ""
                || autoServiceClientConsumerMethodResourceVoid == ""
                || autoServiceServerResource == ""
                || autoServiceServerManagerResource == ""
                || autoServiceServerManagerSessionResource == ""
                || autoServiceServerProcessorResource == ""
                || autoServiceServerProcessorMethodProxyResource == ""
                || autoServiceServerProcessorMethodProxyResourceVoid == ""
            )
                return;


            var autoServiceServerManagers = AutoServiceUtils.GetAllClassWithInterface(classes, autoServiceServerManagerInterfaceDefinition);
            var alreadyProvidedService = new List<INamedTypeSymbol>();
            foreach (var autoServiceServerManager in autoServiceServerManagers)
            {
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

                var allImplementedServices = autoServiceServerManager.GetAttributes()
                    .Where(data => data.AttributeClass?.Name == autoServiceProviderAttributeDefinition.Name)
                    .Where(data => data.ConstructorArguments.Length == 1 && data.ConstructorArguments[0].Value != null)
                    .Select(data => (INamedTypeSymbol)data.ConstructorArguments[0].Value).ToList();

                foreach (var implementedServerService in allImplementedServices)
                {
                    if (alreadyProvidedService.Contains(implementedServerService))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "ASG0007",
                                "This service is already provided by another manager, just one of theses will be available",
                                "This service is already provided by another manager, just one of theses will be available",
                                "",
                                DiagnosticSeverity.Warning,
                                true),
                            implementedServerService.Locations[0]));
                        return;
                    }

                    alreadyProvidedService.Add(implementedServerService);
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

                    foreach (var member in implementedServerService.Interfaces[0].GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            var methodReturnType = methodSymbol.ReturnType;

                            if (methodReturnType.Name != "Task" && methodReturnType.Name != "ValueTask")
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        "ASG0005",
                                        "Method return type must be Task or ValueTask",
                                        "Method return type must be Task or ValueTask",
                                        "AutoServiceGenerator",
                                        DiagnosticSeverity.Error,
                                        true),
                                    methodSymbol.Locations[0]));
                                return;
                            }

                            if (((INamedTypeSymbol)methodSymbol.ReturnType).TypeArguments.Length == 1)
                            {
                                var methodReturnTypeGeneric = ((INamedTypeSymbol)methodSymbol.ReturnType).TypeArguments[0];
                                if (!AutoServiceUtils.IsValidTypeForArgumentOrReturn(methodReturnTypeGeneric))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(
                                        new DiagnosticDescriptor(
                                            "ASG0006",
                                            $"Non supported generic type <{methodReturnTypeGeneric}> for method return type",
                                            $"Non supported generic type <{methodReturnTypeGeneric}> for method return type",
                                            "AutoServiceGenerator",
                                            DiagnosticSeverity.Error,
                                            true),
                                        methodSymbol.Locations[0]));
                                    return;
                                }
                            }
                        }
                    }
                }

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
                        if (attributeData.ConstructorArguments.Length == 1 && attributeData.ConstructorArguments[0].Value != null)
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

                    autoServiceClientConsumerDeclarations.Append('\t', 1).AppendLine($"public {autoServiceClientConsumerInterface.Name} {autoServiceClientConsumerInterface.Name.Substring(1)} {{ get; }}");
                    autoServicesClientConsumerInitializations.Append('\t', 2)
                        .AppendLine($"{autoServiceClientConsumerInterface.Name.Substring(1)} = new {autoServiceClientConsumerInterface.Name.Substring(1)}{autoServiceServerManagerName}ClientConsumer(this, _logger, streamManager);");

                    var serviceImplementations = new StringBuilder();

                    var methodCode = 0;
                    foreach (var member in autoServiceClientConsumerInterface.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            var methodReturnType = methodSymbol.ReturnType;
                            var methodReturnTypeGeneric = ((INamedTypeSymbol)methodReturnType).TypeArguments.Length > 0 ? ((INamedTypeSymbol)methodReturnType).TypeArguments[0] : null;
                            var writeParameters = new StringBuilder();
                            var parameters = new StringBuilder();
                            var readResult = new StringBuilder();

                            foreach (var parameterSymbol in methodSymbol.Parameters)
                            {
                                if (parameterSymbol.Type.ToString() == "string")
                                {
                                    writeParameters.Append('\t', 3)
                                        .AppendLine($"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name} == null ? 0 : System.Text.Encoding.UTF8.GetByteCount({parameterSymbol.Name}));");
                                }
                                else if (parameterSymbol.Type is IArrayTypeSymbol || AutoServiceUtils.IsList(parameterSymbol.Type))
                                {
                                    var sizeProperty = parameterSymbol.Type is IArrayTypeSymbol || parameterSymbol.Type.ToString() == "string" ? "Length" : "Count";
                                    writeParameters.Append('\t', 3).AppendLine($"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name}.{sizeProperty});");
                                }

                                if (parameterSymbol.Type is INamedTypeSymbol { EnumUnderlyingType: { } } nameSymbol)
                                {
                                    writeParameters.Append('\t', 3)
                                        .AppendLine(
                                            $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({(nameSymbol.EnumUnderlyingType != null ? $"({nameSymbol.EnumUnderlyingType})" : "") + parameterSymbol.Name});");
                                }
                                else
                                {
                                    writeParameters.Append('\t', 3).AppendLine($"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name});");
                                }

                                parameters.Append($"{parameterSymbol.Type} {parameterSymbol.Name}, ");
                            }

                            var resultVariableName = $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_Result";
                            var resultBufferVariableName = $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result";
                            var offsetVariableName = $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result_Offset";

                            if (methodReturnTypeGeneric != null)
                            {
                                var hasSizeProperty = methodReturnTypeGeneric.ToString() == "string" || methodReturnTypeGeneric is IArrayTypeSymbol || AutoServiceUtils.IsList(methodReturnTypeGeneric);

                                if (hasSizeProperty)
                                {
                                    readResult.Append('\t', 3).AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out int len_{methodReturnTypeGeneric.Name});");
                                    readResult.Append('\t', 3).AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, in len_{methodReturnTypeGeneric.Name}, out {methodReturnTypeGeneric} {resultVariableName});");
                                }
                                else
                                {
                                    readResult.Append('\t', 3).AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out {methodReturnTypeGeneric} {resultVariableName});");
                                }
                            }

                            if (parameters.Length >= 2)
                                parameters.Length -= 2;


                            serviceImplementations
                                .Append('\t', 1)
                                .AppendLine(string.Format(methodReturnTypeGeneric != null ? autoServiceClientConsumerMethodResource : autoServiceClientConsumerMethodResourceVoid, autoServiceClientConsumerInterface.Name, methodSymbol.Name,
                                    methodReturnType, methodReturnTypeGeneric, parameters,
                                    writeParameters, readResult, methodCode++));
                        }
                    }

                    var autoServiceClientConsumerSource = string.Format(autoServiceClientConsumerResource, namespaceAutoServiceServerManager, autoServiceClientConsumerInterface.Name.Substring(1), autoServiceServerManagerName,
                        autoServiceClientConsumerInterface.ContainingNamespace,
                        serviceImplementations);
                    context.AddSource($"{autoServiceClientConsumerInterface.Name.Substring(1)}{autoServiceServerManagerName}ClientConsumer.g.cs", SourceText.From(autoServiceClientConsumerSource, Encoding.UTF8));
                }

                var autoServiceServerManagerSessionSource = string.Format(autoServiceServerManagerSessionResource, namespaceAutoServiceServerManager, autoServiceServerManagerName, autoServiceClientConsumerDeclarations,
                    autoServicesClientConsumerInitializations, autoServiceServerManagerSessionSourceUsings);
                context.AddSource($"{autoServiceServerManagerName}Session.g.cs", SourceText.From(autoServiceServerManagerSessionSource, Encoding.UTF8));

                var checkDuplicateAutoServiceServerProcessorUsings = new List<string>();

                var autoServiceServerProcessorUsings = new StringBuilder();
                var autoServiceServerProcessorDeclarations = new StringBuilder();
                var autoServiceServerProcessorInitializers = new StringBuilder();
                var autoServiceServerProcessorLoaders = new StringBuilder();
                var autoServiceServerProcessorProxies = new StringBuilder();

                foreach (var implementedServerService in allImplementedServices)
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
                    autoServiceServerProcessorInitializers.Append('\t', 2)
                        .AppendLine($"_{implementedServerService.Name.DeCapitalize()} = new {implementedServerService.ContainingNamespace}.{implementedServerService.Name}(TryGetCallingSessionProxy, _sessions.TryGetValue);");

                    autoServiceServerProcessorLoaders.Append('\t', 2).AppendLine($"_serviceProxies.Add(\"{implementedServerService.Interfaces[0].Name}\", new Dictionary<ushort, InternalProxy>());");

                    var methodCode = 0;
                    foreach (var member in interfaceServer.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            autoServiceServerProcessorLoaders
                                .Append('\t', 2)
                                .AppendLine($"_serviceProxies[\"{interfaceServer.Name}\"].Add({methodCode}, InternalProxy_{implementedServerService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name});");

                            var readParameters = new StringBuilder();
                            var parameters = new StringBuilder();
                            var writeResult = new StringBuilder();

                            foreach (var parameterSymbol in methodSymbol.Parameters)
                            {
                                var hasSizeProperty = parameterSymbol.Type.ToString() == "string" || parameterSymbol.Type is IArrayTypeSymbol || AutoServiceUtils.IsList(parameterSymbol.Type);

                                if (hasSizeProperty)
                                {
                                    readParameters.Append('\t', 2).AppendLine($"inputBuffer.Read(ref offset, out int len_{parameterSymbol.Name});");
                                    readParameters.Append('\t', 2).AppendLine($"inputBuffer.Read(ref offset, in len_{parameterSymbol.Name}, out {parameterSymbol} {parameterSymbol.Name});");
                                }
                                else
                                {
                                    readParameters.Append('\t', 2).AppendLine($"inputBuffer.Read(ref offset, out {parameterSymbol} {parameterSymbol.Name});");
                                }

                                parameters.Append($"{parameterSymbol.Name}, ");
                            }

                            var methodReturnType = methodSymbol.ReturnType;
                            var methodReturnGenericType = ((INamedTypeSymbol)methodReturnType).TypeArguments.Length > 0 ? ((INamedTypeSymbol)methodReturnType).TypeArguments[0] : null;

                            if (methodReturnGenericType != null)
                            {
                                var resultVariableName = $"{implementedServerService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name}_Result";

                                if (methodReturnGenericType.ToString() == "string")
                                {
                                    writeResult.Append('\t', 3).AppendLine($"stream.ExWrite({resultVariableName} == null ? 0 : System.Text.Encoding.UTF8.GetByteCount({resultVariableName}));");
                                }
                                else if (methodReturnGenericType is IArrayTypeSymbol || AutoServiceUtils.IsList(methodReturnGenericType))
                                {
                                    var sizeProperty = methodReturnGenericType is IArrayTypeSymbol || methodReturnGenericType.ToString() == "string" ? "Length" : "Count";
                                    writeResult.Append('\t', 3).AppendLine($"stream.ExWrite({resultVariableName}.{sizeProperty});");
                                }

                                if (methodReturnGenericType is INamedTypeSymbol { EnumUnderlyingType: { } } nameSymbol)
                                {
                                    writeResult.Append('\t', 3).AppendLine($"stream.ExWrite({(nameSymbol.EnumUnderlyingType != null ? $"({nameSymbol.EnumUnderlyingType})" : "") + resultVariableName});");
                                }
                                else
                                {
                                    writeResult.Append('\t', 3).AppendLine($"stream.ExWrite({resultVariableName});");
                                }
                            }

                            if (parameters.Length >= 2)
                                parameters.Length -= 2;

                            autoServiceServerProcessorProxies
                                .Append('\t')
                                .AppendLine(string.Format(methodReturnGenericType != null ? autoServiceServerProcessorMethodProxyResource : autoServiceServerProcessorMethodProxyResourceVoid, implementedServerService.Name,
                                    interfaceServer.Name,
                                    methodCode, methodSymbol.Name,
                                    autoServiceServerManagerName, implementedServerService.Name.DeCapitalize(), readParameters, parameters,
                                    writeResult));

                            methodCode++;
                        }
                    }
                }

                var autoServiceServerProcessorSource = string.Format(autoServiceServerProcessorResource, namespaceAutoServiceServerManager, autoServiceServerManagerName, autoServiceServerProcessorDeclarations,
                    autoServiceServerProcessorInitializers, autoServiceServerProcessorLoaders,
                    autoServiceServerProcessorProxies, autoServiceServerProcessorUsings);
                context.AddSource($"{autoServiceServerManagerName}Processor.g.cs", SourceText.From(autoServiceServerProcessorSource, Encoding.UTF8));
            }
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "ASG0007",
                    "Unexpected error",
                    "A error occured on AutoServiceGenerator: ({0}  - {1})",
                    "AutoServiceGenerator",
                    DiagnosticSeverity.Error,
                    true),
                null, e.Message, e));
        }
    }
}