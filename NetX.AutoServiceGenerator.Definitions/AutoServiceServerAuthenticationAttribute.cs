using System;
using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public class AutoServiceServerAuthenticationAttribute<TImplementation, TProto, TReturn> : Attribute where TImplementation : IAutoServiceAuthentication<TProto, TReturn>
    where TProto : IAutoSerialize, IAutoDeserialize
    where TReturn : AutoServiceAuthenticationProto
{
}