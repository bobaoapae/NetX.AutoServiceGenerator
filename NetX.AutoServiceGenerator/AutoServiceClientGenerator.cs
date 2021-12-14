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
    public static void Generate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();

        var distinctClasses = classes.Distinct();

        var candidateClasses = new List<INamedTypeSymbol>();

        foreach (var classDeclarationSyntax in distinctClasses)
        {
            var model = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            var classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax);
            candidateClasses.Add((INamedTypeSymbol)classSymbol);
        }

        var autoServiceConsumerAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceConsumerAttribute");
        var autoServiceProviderAttributeDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.AutoServiceProviderAttribute");
        var autoServiceClientManagerInterfaceDefinition = compilation.GetTypeByMetadataName("NetX.AutoServiceGenerator.Definitions.IAutoServiceClientManager");

        if (autoServiceConsumerAttributeDefinition == null || autoServiceProviderAttributeDefinition == null || autoServiceClientManagerInterfaceDefinition == null)
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


        if (
            autoServiceClientResource == ""
            || autoServiceClientManagerResource == ""
            || autoServiceClientManagerProcessorResource == ""
            || autoServiceClientManagerProcessorMethodProxyResource == ""
            || autoServiceClientManagerProcessorMethodProxyResourceVoid == ""
            || autoServiceServerConsumerResource == ""
            || autoServiceServerConsumerMethodResource == ""
            || autoServiceServerConsumerMethodResourceVoid == ""
        )
            return;

        var autoServiceClientManagers = AutoServiceUtils.GetAllClassWithInterface(candidateClasses, autoServiceClientManagerInterfaceDefinition);

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

            var allImplementedServices = autoServiceClientManager.GetAttributes().Where(data => data.AttributeClass?.Name == autoServiceProviderAttributeDefinition.Name).Select(data => ((INamedTypeSymbol)data.ConstructorArguments[0].Value)).ToList();

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
                                        $"Non supported generic type <{methodReturnTypeGeneric}>",
                                        $"Non supported generic type <{methodReturnTypeGeneric}>",
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
                autoServicesServerConsumerInitializations.Append('\t', 2).AppendLine($"{autoServiceServerConsumerInterface.Name.Substring(1)} = new {autoServiceServerConsumerInterface.Name.Substring(1)}{autoServiceClientManagerName}ServerConsumer(_netXClient, _logger, manager);");


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
                            if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(parameterSymbol.Type))
                            {
                                if (parameterSymbol.Type is IArrayTypeSymbol || AutoServiceUtils.IsList(parameterSymbol.Type))
                                {
                                    var sizeProperty = parameterSymbol.Type is IArrayTypeSymbol ? "Length" : "Count";
                                    writeParameters
                                        .Append('\t', 3)
                                        .AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name}.{sizeProperty});");
                                    writeParameters.Append('\t', 3)
                                        .AppendLine($"for (var x = 0; x < {parameterSymbol.Name}.{sizeProperty}; x++)")
                                        .Append('\t', 3).Append("{").AppendLine()
                                        .Append('\t', 4).Append($"{parameterSymbol.Name}[x].Serialize({autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream);")
                                        .Append('\t', 3).Append("}").AppendLine();
                                }
                                else
                                {
                                    writeParameters.Append('\t', 3).Append($"{parameterSymbol.Name}.Serialize({autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream);");
                                }
                            }
                            else
                            {
                                writeParameters
                                    .Append('\t', 3)
                                    .AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name});");
                            }

                            parameters.Append($"{parameterSymbol.Type} {parameterSymbol.Name}, ");
                        }


                        var resultVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Result";
                        var resultBufferVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result";
                        var offsetVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result_Offset";

                        if (methodReturnTypeGeneric != null)
                        {
                            if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(methodReturnTypeGeneric))
                            {
                                if (methodReturnTypeGeneric is IArrayTypeSymbol arrayTypeSymbol)
                                {
                                    readResult
                                        .Append('\t', 3)
                                        .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out int arraySize_{methodReturnTypeGeneric.Name});");
                                    readResult
                                        .Append('\t', 3)
                                        .AppendLine($"var {resultVariableName} = new {arrayTypeSymbol.ElementType}[arraySize_{methodReturnTypeGeneric.Name}]);");

                                    readResult.Append('\t', 3)
                                        .AppendLine($"for (var x = 0; x < arraySize_{methodReturnTypeGeneric.Name}; x++)")
                                        .Append('\t', 3).Append("{").AppendLine()
                                        .Append('\t', 4).Append($"var instance_{arrayTypeSymbol.ElementType} = new {arrayTypeSymbol.ElementType}();")
                                        .Append('\t', 4).Append($"instance_{arrayTypeSymbol.ElementType}.Deserialize(in {resultBufferVariableName}, ref {offsetVariableName});")
                                        .Append('\t', 4).Append($"{resultVariableName}[x] = instance_{arrayTypeSymbol.ElementType};")
                                        .Append('\t', 3).Append("}").AppendLine();
                                }
                                else if (AutoServiceUtils.IsList(methodReturnTypeGeneric))
                                {
                                    var listTypeSymbol = ((INamedTypeSymbol)methodReturnTypeGeneric).TypeArguments[0];

                                    readResult
                                        .Append('\t', 3)
                                        .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out int listSize_{methodReturnTypeGeneric.Name});");
                                    readResult
                                        .Append('\t', 3)
                                        .AppendLine($"var {resultVariableName} = new List<{listTypeSymbol}>(listSize_{methodReturnTypeGeneric.Name});");

                                    readResult.Append('\t', 3)
                                        .AppendLine($"for (var x = 0; x < listSize_{methodReturnTypeGeneric.Name}; x++)")
                                        .Append('\t', 3).Append("{").AppendLine()
                                        .Append('\t', 4).AppendLine($"var instance_{listTypeSymbol} = new {listTypeSymbol}();")
                                        .Append('\t', 4).AppendLine($"instance_{listTypeSymbol}.Deserialize(in {resultBufferVariableName}, ref {offsetVariableName});")
                                        .Append('\t', 4).AppendLine($"{resultVariableName}.Add(instance_{listTypeSymbol});")
                                        .Append('\t', 3).AppendLine("}");
                                }
                                else
                                {
                                    readResult
                                        .Append('\t', 3).Append($"var {resultVariableName} = new {methodReturnTypeGeneric}();")
                                        .Append('\t', 3).Append($"{resultVariableName}.Deserialize(in {resultBufferVariableName}, ref {offsetVariableName});");
                                }
                            }
                            else
                            {
                                readResult
                                    .Append('\t', 3)
                                    .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out {methodReturnTypeGeneric} {resultVariableName});");
                            }
                        }

                        parameters.Length -= 2;


                        serviceImplementations
                            .Append('\t', 1)
                            .AppendLine(string.Format(methodReturnTypeGeneric != null ? autoServiceServerConsumerMethodResource : autoServiceServerConsumerMethodResourceVoid, autoServiceServerConsumerInterface.Name, methodSymbol.Name, methodReturnType, methodReturnTypeGeneric, parameters,
                                writeParameters, readResult, methodCode++));
                    }
                }

                var autoServiceServerConsumerSource = string.Format(autoServiceServerConsumerResource, namespaceAutoServiceClientManager, autoServiceServerConsumerInterface.Name.Substring(1), autoServiceClientManagerName, serviceImplementations,
                    autoServiceServerConsumerInterface.ContainingNamespace);
                context.AddSource($"{autoServiceServerConsumerInterface.Name.Substring(1)}{autoServiceClientManagerName}ServerConsumer.g.cs", SourceText.From(autoServiceServerConsumerSource, Encoding.UTF8));
            }

            var autoServiceClientManagerSource = string.Format(autoServiceClientManagerResource, namespaceAutoServiceClientManager, autoServiceClientManagerName, autoServiceServerConsumerDeclarations, autoServicesServerConsumerInitializations, autoServiceClientManagerUsings);
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
                            if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(parameterSymbol.Type))
                            {
                                if (parameterSymbol.Type is IArrayTypeSymbol arrayTypeSymbol)
                                {
                                    readParameters
                                        .Append('\t', 2)
                                        .AppendLine($"inputBuffer.Read(ref offset, out int arraySize_{parameterSymbol.Name});");
                                    readParameters
                                        .Append('\t', 2)
                                        .AppendLine($"var {parameterSymbol.Name} = new {arrayTypeSymbol.ElementType}[arraySize_{parameterSymbol.Name}]);");

                                    readParameters.Append('\t', 2)
                                        .AppendLine($"for (var x = 0; x < arraySize_{parameterSymbol.Name}; x++)")
                                        .Append('\t', 2).Append("{").AppendLine()
                                        .Append('\t', 3).Append($"var instance_{arrayTypeSymbol.ElementType} = new {arrayTypeSymbol.ElementType}();")
                                        .Append('\t', 3).Append($"instance_{arrayTypeSymbol.ElementType}.Deserialize(in inputBuffer, ref offset);")
                                        .Append('\t', 3).Append($"{parameterSymbol.Name}[x] = instance_{arrayTypeSymbol.ElementType};")
                                        .Append('\t', 2).Append("}").AppendLine();
                                }
                                else if (AutoServiceUtils.IsList(parameterSymbol.Type))
                                {
                                    var listTypeSymbol = ((INamedTypeSymbol)parameterSymbol.Type).TypeArguments[0];

                                    readParameters
                                        .Append('\t', 2)
                                        .AppendLine($"inputBuffer.Read(ref offset, out int listSize_{parameterSymbol.Name});");
                                    readParameters
                                        .Append('\t', 2)
                                        .AppendLine($"var {parameterSymbol.Name} = new List<{listTypeSymbol}>(listSize_{parameterSymbol.Name});");

                                    readParameters.Append('\t', 2)
                                        .AppendLine($"for (var x = 0; x < listSize_{parameterSymbol.Name}; x++)")
                                        .Append('\t', 2).Append("{").AppendLine()
                                        .Append('\t', 3).AppendLine($"var instance_{listTypeSymbol} = new {listTypeSymbol}();")
                                        .Append('\t', 3).AppendLine($"instance_{listTypeSymbol}.Deserialize(in inputBuffer, ref offset);")
                                        .Append('\t', 3).AppendLine($"{parameterSymbol.Name}.Add(instance_{listTypeSymbol});")
                                        .Append('\t', 2).AppendLine("}");
                                }
                                else
                                {
                                    readParameters
                                        .Append('\t', 3).Append($"var {parameterSymbol.Name} = new {parameterSymbol.Type}();")
                                        .Append('\t', 3).Append($"{parameterSymbol.Name}.Deserialize(in inputBuffer, ref offset);");
                                }
                            }
                            else
                            {
                                readParameters
                                    .Append('\t', 2)
                                    .AppendLine($"inputBuffer.Read(ref offset, out {parameterSymbol.Type} {parameterSymbol.Name});");
                            }

                            parameters.Append($"{parameterSymbol.Name}, ");
                        }

                        var methodReturnType = methodSymbol.ReturnType;
                        var methodReturnGenericType = ((INamedTypeSymbol)methodReturnType).TypeArguments.Length > 0 ? ((INamedTypeSymbol)methodReturnType).TypeArguments[0] : null;

                        if (methodReturnGenericType != null)
                        {
                            var resultVariableName = $"{implementedService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name}_Result";

                            if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(methodReturnGenericType))
                            {
                                if (methodReturnGenericType is IArrayTypeSymbol || AutoServiceUtils.IsList(methodReturnGenericType))
                                {
                                    var sizeProperty = methodReturnGenericType is IArrayTypeSymbol ? "Length" : "Count";
                                    writeResult
                                        .AppendLine($"stream.ExWrite({resultVariableName}.{sizeProperty});");
                                    writeResult.Append('\t', 3)
                                        .AppendLine($"for (var x = 0; x < {resultVariableName}.{sizeProperty}; x++)")
                                        .Append('\t', 3).Append("{").AppendLine()
                                        .Append('\t', 4).Append($"{resultVariableName}[x].Serialize(stream);")
                                        .Append('\t', 3).Append("}").AppendLine();
                                }
                                else
                                {
                                    writeResult.Append($"{resultVariableName}.Serialize(stream);");
                                }
                            }
                            else
                            {
                                writeResult.Append($"stream.ExWrite({resultVariableName});");
                            }
                        }

                        parameters.Length -= 2;

                        autoServiceClientProcessorProxies
                            .Append('\t')
                            .AppendLine(string.Format(methodReturnGenericType != null ? autoServiceClientManagerProcessorMethodProxyResource : autoServiceClientManagerProcessorMethodProxyResourceVoid, implementedService.Name, interfaceServer.Name, methodCode, methodSymbol.Name,
                                implementedService.Name.DeCapitalize(), readParameters, parameters, writeResult));

                        methodCode++;
                    }
                }
            }

            var autoServiceClientProcessorSource = string.Format(autoServiceClientManagerProcessorResource, namespaceAutoServiceClientManager, autoServiceClientManagerName, autoServiceClientProcessorDeclarations, autoServiceClientProcessorInitializers, autoServiceClientProcessorLoaders,
                autoServiceClientProcessorProxies, autoServiceClientProcessorUsings);
            context.AddSource($"{autoServiceClientManagerName}Processor.g.cs", SourceText.From(autoServiceClientProcessorSource, Encoding.UTF8));
        }
    }
}