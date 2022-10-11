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

public static class AutoServiceClientGenerator
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
            var autoServiceClientManagerInterfaceDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceClientManager");
            var autoServiceAuthenticationAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceClientAuthenticationAttribute`1");

            if (autoServiceConsumerAttributeDefinition == null || autoServiceProviderAttributeDefinition == null || autoServiceClientManagerInterfaceDefinition == null || autoServiceAuthenticationAttributeDefinition == null)
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

            var autoServiceClientResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClient");
            var autoServiceClientManagerResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManager");
            var autoServiceClientManagerProcessorResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManagerProcessor");
            var autoServiceClientManagerProcessorMethodProxyResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManagerProcessorMethodProxy");
            var autoServiceClientManagerProcessorMethodProxyResourceVoid = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManagerProcessorMethodProxyVoid");
            var autoServiceServerConsumerResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceServerConsumer");
            var autoServiceServerConsumerMethodResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceServerConsumerMethod");
            var autoServiceServerConsumerMethodResourceVoid = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceServerConsumerMethodVoid");
            var autoServiceAuthenticationSessionConnectResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceAuthenticationSessionConnect");


            if (
                autoServiceClientResource == ""
                || autoServiceClientManagerResource == ""
                || autoServiceClientManagerProcessorResource == ""
                || autoServiceClientManagerProcessorMethodProxyResource == ""
                || autoServiceClientManagerProcessorMethodProxyResourceVoid == ""
                || autoServiceServerConsumerResource == ""
                || autoServiceServerConsumerMethodResource == ""
                || autoServiceServerConsumerMethodResourceVoid == ""
                || autoServiceAuthenticationSessionConnectResource == ""
            )
                return;

            var autoServiceClientManagers = AutoServiceUtils.GetAllClassWithInterface(classes, autoServiceClientManagerInterfaceDefinition);

            var alreadyProvidedService = new List<INamedTypeSymbol>();

            foreach (var autoServiceClientManager in autoServiceClientManagers)
            {
                if (alreadyProvidedService.Contains(autoServiceClientManager))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0007",
                            "This service is already provided by another manager, just one of theses will be available",
                            "This service is already provided by another manager, just one of theses will be available",
                            "AutoServiceGenerator",
                            DiagnosticSeverity.Warning,
                            true),
                        autoServiceClientManager.Locations[0]));
                    return;
                }

                alreadyProvidedService.Add(autoServiceClientManager);
                if (!AutoServiceUtils.CheckClassIsPartial(autoServiceClientManager))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0004",
                            "Class need to be partial",
                            "Class need to be partial",
                            "AutoServiceGenerator",
                            DiagnosticSeverity.Error,
                            true),
                        autoServiceClientManager.Locations[0]));
                    return;
                }

                var allImplementedServices = autoServiceClientManager.GetAttributes().Where(data => data.AttributeClass?.Name == autoServiceProviderAttributeDefinition.Name)
                    .Select(data => ((INamedTypeSymbol)data.ConstructorArguments[0].Value)).ToList();

                var autoServiceServerAuthenticationAttribute = autoServiceClientManager.GetAttributes().FirstOrDefault(data => data.AttributeClass?.Name == autoServiceAuthenticationAttributeDefinition.Name);
                INamedTypeSymbol autoServiceClientAuthenticationAttributeProtoType = null;
                if (autoServiceServerAuthenticationAttribute != null)
                {
                    autoServiceClientAuthenticationAttributeProtoType = (INamedTypeSymbol)autoServiceServerAuthenticationAttribute.AttributeClass.TypeArguments[0];
                }

                foreach (var implementedServerService in allImplementedServices)
                {
                    if (!AutoServiceUtils.CheckClassIsPartial(implementedServerService))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "ASG0004",
                                "Class need to be partial",
                                "Class need to be partial",
                                "AutoServiceGenerator",
                                DiagnosticSeverity.Error,
                                true),
                            implementedServerService.Locations[0]));
                        return;
                    }

                    foreach (var member in implementedServerService.Interfaces[0].GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol && methodSymbol.MethodKind != MethodKind.Constructor)
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

                var namespaceAutoServiceClientManager = autoServiceClientManager.ContainingNamespace.ToDisplayString();
                var autoServiceClientManagerName = autoServiceClientManager.Name;

                var autoServiceServerConsumerInterfaces = new List<INamedTypeSymbol>();

                foreach (var attributeData in autoServiceClientManager.GetAttributes())
                {
                    if (attributeData.AttributeClass?.Name == autoServiceConsumerAttributeDefinition.Name)
                    {
                        autoServiceServerConsumerInterfaces.Add((INamedTypeSymbol)attributeData.ConstructorArguments[0].Value);
                    }
                }

                var autoServicesServerConsumerInitializations = new StringBuilder();
                var autoServiceServerConsumerDeclarations = new StringBuilder();

                var checkDuplicateAutoServiceClientManagerUsings = new List<string>();

                var autoServiceClientManagerUsings = new StringBuilder();

                foreach (var autoServiceServerConsumerInterface in autoServiceServerConsumerInterfaces)
                {
                    if (!checkDuplicateAutoServiceClientManagerUsings.Contains(autoServiceServerConsumerInterface.ContainingNamespace.ToString()))
                    {
                        checkDuplicateAutoServiceClientManagerUsings.Add(autoServiceServerConsumerInterface.ContainingNamespace.ToString());
                        autoServiceClientManagerUsings.AppendLine($"using {autoServiceServerConsumerInterface.ContainingNamespace};");
                    }

                    autoServiceServerConsumerDeclarations.Append('\t', 1).AppendLine($"public {autoServiceServerConsumerInterface.Name} {autoServiceServerConsumerInterface.Name.Substring(1)} {{ get; }}");
                    autoServicesServerConsumerInitializations.Append('\t', 2)
                        .AppendLine($"{autoServiceServerConsumerInterface.Name.Substring(1)} = new {autoServiceServerConsumerInterface.Name.Substring(1)}{autoServiceClientManagerName}ServerConsumer(_netXClient, _logger, manager);");


                    var serviceImplementations = new StringBuilder();

                    var methodCode = 0;
                    foreach (var member in autoServiceServerConsumerInterface.GetMembers())
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
                                        .AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name} == null ? 0 : System.Text.Encoding.UTF8.GetByteCount({parameterSymbol.Name}));");
                                }
                                else if (parameterSymbol.Type is IArrayTypeSymbol || AutoServiceUtils.IsList(parameterSymbol.Type))
                                {
                                    var sizeProperty = parameterSymbol.Type is IArrayTypeSymbol || parameterSymbol.Type.ToString() == "string" ? "Length" : "Count";
                                    writeParameters.Append('\t', 3).AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name}.{sizeProperty});");
                                }

                                if (parameterSymbol.Type is INamedTypeSymbol { EnumUnderlyingType: { } } nameSymbol)
                                {
                                    writeParameters.Append('\t', 3)
                                        .AppendLine(
                                            $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({(nameSymbol.EnumUnderlyingType != null ? $"({nameSymbol.EnumUnderlyingType})" : "") + parameterSymbol.Name});");
                                }
                                else
                                {
                                    writeParameters.Append('\t', 3).AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name});");
                                }

                                parameters.Append($"{parameterSymbol.Type} {parameterSymbol.Name}, ");
                            }


                            var resultVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Result";
                            var resultBufferVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result";
                            var offsetVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result_Offset";

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
                                .AppendLine(string.Format(methodReturnTypeGeneric != null ? autoServiceServerConsumerMethodResource : autoServiceServerConsumerMethodResourceVoid, autoServiceServerConsumerInterface.Name, methodSymbol.Name,
                                    methodReturnType, methodReturnTypeGeneric, parameters,
                                    writeParameters, readResult, methodCode++));
                        }
                    }

                    var autoServiceServerConsumerSource = string.Format(autoServiceServerConsumerResource, namespaceAutoServiceClientManager, autoServiceServerConsumerInterface.Name.Substring(1), autoServiceClientManagerName,
                        serviceImplementations,
                        autoServiceServerConsumerInterface.ContainingNamespace);
                    context.AddSource($"{autoServiceServerConsumerInterface.Name.Substring(1)}{autoServiceClientManagerName}ServerConsumer.g.cs", SourceText.From(autoServiceServerConsumerSource, Encoding.UTF8));
                }

                var authenticationParameters = "";
                var autoServiceAuthenticationSessionConnect = "";

                if (autoServiceClientAuthenticationAttributeProtoType != null)
                {
                    authenticationParameters = $"{autoServiceClientAuthenticationAttributeProtoType.ContainingNamespace}.{autoServiceClientAuthenticationAttributeProtoType.Name} ipsInternalAuthenticationProto, ";
                    autoServiceAuthenticationSessionConnect = string.Format(autoServiceAuthenticationSessionConnectResource);
                }

                var autoServiceClientManagerSource = string.Format(
                    autoServiceClientManagerResource,
                    namespaceAutoServiceClientManager,
                    autoServiceClientManagerName,
                    autoServiceServerConsumerDeclarations,
                    autoServicesServerConsumerInitializations,
                    autoServiceClientManagerUsings,
                    authenticationParameters,
                    autoServiceAuthenticationSessionConnect);
                context.AddSource($"{autoServiceClientManagerName}.g.cs", SourceText.From(autoServiceClientManagerSource, Encoding.UTF8));


                var checkDuplicateAutoServiceClientProcessorUsings = new List<string>();

                var autoServiceClientProcessorUsings = new StringBuilder();
                var autoServiceClientProcessorDeclarations = new StringBuilder();
                var autoServiceClientProcessorInitializers = new StringBuilder();
                var autoServiceClientProcessorLoaders = new StringBuilder();
                var autoServiceClientProcessorProxies = new StringBuilder();

                foreach (var implementedService in allImplementedServices)
                {
                    var interfaceServer = implementedService.Interfaces[0];

                    if (!checkDuplicateAutoServiceClientProcessorUsings.Contains(interfaceServer.ContainingNamespace.ToString()))
                    {
                        checkDuplicateAutoServiceClientProcessorUsings.Add(interfaceServer.ContainingNamespace.ToString());
                        autoServiceClientProcessorUsings.AppendLine($"using {interfaceServer.ContainingNamespace};");
                    }

                    var serviceClientSource = string.Format(autoServiceClientResource, implementedService.ContainingNamespace, implementedService.Name);
                    context.AddSource($"{implementedService.Name}.g.cs", SourceText.From(serviceClientSource, Encoding.UTF8));

                    autoServiceClientProcessorDeclarations.Append('\t', 2).AppendLine($"private {interfaceServer.Name} _{implementedService.Name.DeCapitalize()};");
                    autoServiceClientProcessorInitializers.Append('\t', 2).AppendLine($"_{implementedService.Name.DeCapitalize()} = new {implementedService.ContainingNamespace}.{implementedService.Name}();");

                    autoServiceClientProcessorLoaders.Append('\t', 2).AppendLine($"_serviceProxies.Add(\"{implementedService.Interfaces[0].Name}\", new Dictionary<ushort, InternalProxy>());");

                    var methodCode = 0;
                    foreach (var member in interfaceServer.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            autoServiceClientProcessorLoaders
                                .Append('\t', 2)
                                .AppendLine($"_serviceProxies[\"{interfaceServer.Name}\"].Add({methodCode}, InternalProxy_{implementedService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name});");

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
                                var resultVariableName = $"{implementedService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name}_Result";

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

                            autoServiceClientProcessorProxies
                                .Append('\t')
                                .AppendLine(string.Format(methodReturnGenericType != null ? autoServiceClientManagerProcessorMethodProxyResource : autoServiceClientManagerProcessorMethodProxyResourceVoid, implementedService.Name,
                                    interfaceServer.Name, methodCode, methodSymbol.Name,
                                    implementedService.Name.DeCapitalize(), readParameters, parameters, writeResult));

                            methodCode++;
                        }
                    }
                }

                var autoServiceClientProcessorSource = string.Format(autoServiceClientManagerProcessorResource, namespaceAutoServiceClientManager, autoServiceClientManagerName, autoServiceClientProcessorDeclarations,
                    autoServiceClientProcessorInitializers, autoServiceClientProcessorLoaders,
                    autoServiceClientProcessorProxies, autoServiceClientProcessorUsings);
                context.AddSource($"{autoServiceClientManagerName}Processor.g.cs", SourceText.From(autoServiceClientProcessorSource, Encoding.UTF8));
            }
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "ASG0007",
                    "Unexpected error",
                    "A error occured on AutoServiceClientGenerator: ({0}  - {1})",
                    "AutoServiceGenerator",
                    DiagnosticSeverity.Error,
                    true),
                null, e.Message, e));
        }
    }
}