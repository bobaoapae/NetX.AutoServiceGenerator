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

            var autoServiceClientResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClient");
            var autoServiceClientManagerResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManager");
            var autoServiceClientManagerProcessorResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManagerProcessor");
            var autoServiceClientManagerProcessorMethodProxyResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceClientManagerProcessorMethodProxy");
            var autoServiceServerConsumerResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceServerConsumer");
            var autoServiceServerConsumerMethodResource = AutoServiceUtils.GetResource(assembly, context, "Client.AutoServiceServerConsumerMethod");


            if (
                autoServiceClientConsumerResource == ""
                || autoServiceClientConsumerMethodResource == ""
                || autoServiceServerResource == ""
                || autoServiceServerManagerResource == ""
                || autoServiceServerManagerSessionResource == ""
                || autoServiceServerProcessorResource == ""
                || autoServiceServerProcessorMethodProxyResource == ""
                || autoServiceClientResource == ""
                || autoServiceClientManagerResource == ""
                || autoServiceClientManagerProcessorResource == ""
                || autoServiceClientManagerProcessorMethodProxyResource == ""
                || autoServiceServerConsumerResource == ""
                || autoServiceServerConsumerMethodResource == ""
            )
                return;

            #endregion

            #region ValidateCorrectlyImplementation

            var allImplementedServices = AutoServiceUtils.GetAllClassWithInterfaceWithAttribute(candidateClasses, autoServiceAttributeDefinition);

            foreach (var implementedServerService in allImplementedServices)
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

                foreach (var member in implementedServerService.GetMembers())
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
                                    "",
                                    DiagnosticSeverity.Error,
                                    true),
                                methodSymbol.Locations[0]));
                            return;
                        }

                        var methodReturnTypeGeneric = ((INamedTypeSymbol)methodSymbol.ReturnType).TypeArguments[0];
                        if (!AutoServiceUtils.IsValidTypeForArgumentOrReturn(methodReturnTypeGeneric))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "ASG0006",
                                    $"Non supported generic type <{methodReturnTypeGeneric}>",
                                    $"Non supported generic type <{methodReturnTypeGeneric}>",
                                    "",
                                    DiagnosticSeverity.Error,
                                    true),
                                methodSymbol.Locations[0]));
                            return;
                        }
                    }
                }
            }

            #endregion

            #region AutoServiceServerGenerator

            {
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

                if (autoServiceServerManagers.Count == 1)
                {
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

                        autoServiceClientConsumerDeclarations.Append('\t', 1).AppendLine($"public {autoServiceClientConsumerInterface.Name} {autoServiceClientConsumerInterface.Name.Substring(1)} {{ get; }}");
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
                                var readResult = new StringBuilder();

                                foreach (var parameterSymbol in methodSymbol.Parameters)
                                {
                                    if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(parameterSymbol.Type))
                                    {
                                        if (parameterSymbol.Type is IArrayTypeSymbol)
                                        {
                                            writeParameters
                                                .Append('\t', 2)
                                                .AppendLine($"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name}.Length);");
                                            writeParameters.Append('\t', 2)
                                                .AppendLine($"for (var x = 0; x < {parameterSymbol.Name}.Length; x++)")
                                                .Append('\t', 2).Append("{").AppendLine()
                                                .Append('\t', 3).Append($"{parameterSymbol.Name}[x].Serialize({autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream);")
                                                .Append('\t', 2).Append("}").AppendLine();
                                        }
                                        else
                                        {
                                            writeParameters.Append('\t', 2).Append($"{parameterSymbol.Name}.Serialize({autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream);");
                                        }
                                    }
                                    else
                                    {
                                        writeParameters
                                            .Append('\t', 2)
                                            .AppendLine($"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name});");
                                    }

                                    parameters.Append($"{parameterSymbol.Type} {parameterSymbol.Name}, ");
                                }

                                var resultVariableName = $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_Result";
                                var resultBufferVariableName = $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result";
                                var offsetVariableName = $"{autoServiceClientConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result_Offset";

                                if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(methodReturnTypeGeneric))
                                {
                                    if (methodReturnTypeGeneric is IArrayTypeSymbol arrayTypeSymbol)
                                    {
                                        readResult
                                            .Append('\t', 2)
                                            .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out int arraySize_{methodReturnTypeGeneric.Name});");
                                        readResult
                                            .Append('\t', 2)
                                            .AppendLine($"var {resultVariableName} = new {arrayTypeSymbol.ElementType}[arraySize_{methodReturnTypeGeneric.Name}]);");

                                        readResult.Append('\t', 2)
                                            .AppendLine($"for (var x = 0; x < arraySize_{methodReturnTypeGeneric.Name}; x++)")
                                            .Append('\t', 2).Append("{").AppendLine()
                                            .Append('\t', 3).Append($"var instance_{arrayTypeSymbol.ElementType} = new {arrayTypeSymbol.ElementType}();")
                                            .Append('\t', 3).Append($"instance_{arrayTypeSymbol.ElementType}.Deserialize(in {resultBufferVariableName}, ref {offsetVariableName});")
                                            .Append('\t', 3).Append($"{resultVariableName}[x] = instance_{arrayTypeSymbol.ElementType};")
                                            .Append('\t', 2).Append("}").AppendLine();
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
                                        .Append('\t', 2)
                                        .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out {methodReturnTypeGeneric} {resultVariableName});");
                                }

                                parameters.Length -= 2;


                                serviceImplementations
                                    .Append('\t', 2)
                                    .AppendLine(string.Format(autoServiceClientConsumerMethodResource, autoServiceClientConsumerInterface.Name, methodSymbol.Name, methodReturnType, methodReturnTypeGeneric, parameters, writeParameters, readResult, methodCode++));
                            }
                        }

                        var autoServiceClientConsumerSource = string.Format(autoServiceClientConsumerResource, namespaceAutoServiceServerManager, autoServiceClientConsumerInterface.Name.Substring(1), autoServiceServerManagerName, autoServiceClientConsumerInterface.ContainingNamespace,
                            serviceImplementations);
                        context.AddSource($"{autoServiceClientConsumerInterface.Name.Substring(1)}ClientConsumer.g.cs", SourceText.From(autoServiceClientConsumerSource, Encoding.UTF8));
                    }

                    var autoServiceServerManagerSessionSource = string.Format(autoServiceServerManagerSessionResource, namespaceAutoServiceServerManager, autoServiceServerManagerName, autoServiceClientConsumerDeclarations, autoServicesClientConsumerInitializations,
                        autoServiceServerManagerSessionSourceUsings);
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
                        autoServiceServerProcessorInitializers.Append('\t', 2).AppendLine($"_{implementedServerService.Name.DeCapitalize()} = new {implementedServerService.ContainingNamespace}.{implementedServerService.Name}(TryGetCallingSessionProxy, _sessions.TryGetValue);");

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
                                                .Append('\t', 3).Append($"{parameterSymbol.Name}[x] = instance_{arrayTypeSymbol.ElementType}")
                                                .Append('\t', 2).Append("}").AppendLine();
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

                                var resultVariableName = $"{implementedServerService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name}_Result";

                                if (methodReturnGenericType != null && AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(methodReturnGenericType))
                                {
                                    if (methodReturnGenericType is IArrayTypeSymbol)
                                    {
                                        writeResult
                                            .AppendLine($"stream.ExWrite({resultVariableName}.Length);");
                                        writeResult.Append('\t', 3)
                                            .AppendLine($"for (var x = 0; x < {resultVariableName}.Length; x++)")
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

                                parameters.Length -= 2;

                                autoServiceServerProcessorProxies
                                    .Append('\t')
                                    .AppendLine(string.Format(autoServiceServerProcessorMethodProxyResource, implementedServerService.Name, interfaceServer.Name, methodCode, methodSymbol.Name, autoServiceServerManagerName, implementedServerService.Name.DeCapitalize(), readParameters, parameters,
                                        writeResult));

                                methodCode++;
                            }
                        }
                    }

                    var autoServiceServerProcessorSource = string.Format(autoServiceServerProcessorResource, namespaceAutoServiceServerManager, autoServiceServerManagerName, autoServiceServerProcessorDeclarations, autoServiceServerProcessorInitializers, autoServiceServerProcessorLoaders,
                        autoServiceServerProcessorProxies, autoServiceServerProcessorUsings);
                    context.AddSource($"{autoServiceServerManagerName}Processor.g.cs", SourceText.From(autoServiceServerProcessorSource, Encoding.UTF8));
                }
            }

            #endregion

            #region AutoServiceClientGenerator

            var autoServiceClientManagers = AutoServiceUtils.GetAllClassWithInterface(candidateClasses, autoServiceClientManagerInterfaceDefinition);

            if (autoServiceClientManagers.Count > 1)
            {
                foreach (var serviceClientManager in autoServiceClientManagers)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0003",
                            "Only one AutoServiceServerManager is allowed per project",
                            "Only one AutoServiceServerManager is allowed per project",
                            "",
                            DiagnosticSeverity.Error,
                            true),
                        serviceClientManager.Locations[0]));
                }

                return;
            }

            if (autoServiceClientManagers.Count == 1)
            {
                var autoServiceClientManager = autoServiceClientManagers[0];

                if (!AutoServiceUtils.CheckClassIsPartial(autoServiceClientManager))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0004",
                            "Class need to be partial",
                            "Class need to be partial",
                            "",
                            DiagnosticSeverity.Error,
                            true),
                        autoServiceClientManager.Locations[0]));
                    return;
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
                    autoServicesServerConsumerInitializations.Append('\t', 2).AppendLine($"{autoServiceServerConsumerInterface.Name.Substring(1)} = new {autoServiceServerConsumerInterface.Name.Substring(1)}ServerConsumer(_netXClient, manager);");


                    var serviceImplementations = new StringBuilder();

                    var methodCode = 0;
                    foreach (var member in autoServiceServerConsumerInterface.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            var methodReturnType = methodSymbol.ReturnType;
                            var methodReturnTypeGeneric = ((INamedTypeSymbol)methodReturnType).TypeArguments[0];
                            var writeParameters = new StringBuilder();
                            var parameters = new StringBuilder();
                            var readResult = new StringBuilder();

                            foreach (var parameterSymbol in methodSymbol.Parameters)
                            {
                                if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(parameterSymbol.Type))
                                {
                                    if (parameterSymbol.Type is IArrayTypeSymbol)
                                    {
                                        writeParameters
                                            .Append('\t', 2)
                                            .AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name}.Length);");
                                        writeParameters.Append('\t', 2)
                                            .AppendLine($"for (var x = 0; x < {parameterSymbol.Name}.Length; x++)")
                                            .Append('\t', 2).Append("{").AppendLine()
                                            .Append('\t', 3).Append($"{parameterSymbol.Name}[x].Serialize({autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream);")
                                            .Append('\t', 2).Append("}").AppendLine();
                                    }
                                    else
                                    {
                                        writeParameters.Append('\t', 2).Append($"{parameterSymbol.Name}.Serialize({autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream);");
                                    }
                                }
                                else
                                {
                                    writeParameters
                                        .Append('\t', 2)
                                        .AppendLine($"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_stream.ExWrite({parameterSymbol.Name});");
                                }

                                parameters.Append($"{parameterSymbol.Type} {parameterSymbol.Name}, ");
                            }


                            var resultVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Result";
                            var resultBufferVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result";
                            var offsetVariableName = $"{autoServiceServerConsumerInterface.Name}_{methodSymbol.Name}_Buffer_Result_Offset";

                            if (AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(methodReturnTypeGeneric))
                            {
                                if (methodReturnTypeGeneric is IArrayTypeSymbol arrayTypeSymbol)
                                {
                                    readResult
                                        .Append('\t', 2)
                                        .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out int arraySize_{methodReturnTypeGeneric.Name});");
                                    readResult
                                        .Append('\t', 2)
                                        .AppendLine($"var {resultVariableName} = new {arrayTypeSymbol.ElementType}[arraySize_{methodReturnTypeGeneric.Name}]);");

                                    readResult.Append('\t', 2)
                                        .AppendLine($"for (var x = 0; x < arraySize_{methodReturnTypeGeneric.Name}; x++)")
                                        .Append('\t', 2).Append("{").AppendLine()
                                        .Append('\t', 3).Append($"var instance_{arrayTypeSymbol.ElementType} = new {arrayTypeSymbol.ElementType}();")
                                        .Append('\t', 3).Append($"instance_{arrayTypeSymbol.ElementType}.Deserialize(in {resultBufferVariableName}, ref {offsetVariableName});")
                                        .Append('\t', 3).Append($"{resultVariableName}[x] = instance_{arrayTypeSymbol.ElementType};")
                                        .Append('\t', 2).Append("}").AppendLine();
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
                                    .Append('\t', 2)
                                    .AppendLine($"{resultBufferVariableName}.Read(ref {offsetVariableName}, out {methodReturnTypeGeneric} {resultVariableName});");
                            }

                            parameters.Length -= 2;


                            serviceImplementations
                                .Append('\t', 2)
                                .AppendLine(string.Format(autoServiceServerConsumerMethodResource, autoServiceServerConsumerInterface.Name, methodSymbol.Name, methodReturnType, methodReturnTypeGeneric, parameters, writeParameters, readResult, methodCode++));
                        }
                    }

                    var autoServiceServerConsumerSource = string.Format(autoServiceServerConsumerResource, namespaceAutoServiceClientManager, autoServiceServerConsumerInterface.Name.Substring(1), autoServiceServerConsumerInterface.ContainingNamespace, serviceImplementations);
                    context.AddSource($"{autoServiceServerConsumerInterface.Name.Substring(1)}ServerConsumer.g.cs", SourceText.From(autoServiceServerConsumerSource, Encoding.UTF8));
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

                            var resultVariableName = $"{implementedService.Name}_{interfaceServer.Name}_{methodCode}_{methodSymbol.Name}_Result";

                            if (methodReturnGenericType != null && AutoServiceUtils.NeedUseAutoSerializeOrDeserialize(methodReturnGenericType))
                            {
                                if (methodReturnGenericType is IArrayTypeSymbol)
                                {
                                    writeResult
                                        .AppendLine($"stream.ExWrite({resultVariableName}.Length);");
                                    writeResult.Append('\t', 3)
                                        .AppendLine($"for (var x = 0; x < {resultVariableName}.Length; x++)")
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

                            parameters.Length -= 2;

                            autoServiceClientProcessorProxies
                                .Append('\t')
                                .AppendLine(string.Format(autoServiceClientManagerProcessorMethodProxyResource, implementedService.Name, interfaceServer.Name, methodCode, methodSymbol.Name, implementedService.Name.DeCapitalize(), readParameters, parameters, writeResult));

                            methodCode++;
                        }
                    }
                }

                var autoServiceClientProcessorSource = string.Format(autoServiceClientManagerProcessorResource, namespaceAutoServiceClientManager, autoServiceClientManagerName, autoServiceClientProcessorDeclarations, autoServiceClientProcessorInitializers, autoServiceClientProcessorLoaders,
                    autoServiceClientProcessorProxies, autoServiceClientProcessorUsings);
                context.AddSource($"{autoServiceClientManagerName}Processor.g.cs", SourceText.From(autoServiceClientProcessorSource, Encoding.UTF8));
            }

            #endregion
        }
    }
}