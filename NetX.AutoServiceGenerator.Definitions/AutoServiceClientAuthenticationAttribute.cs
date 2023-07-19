using System;
using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public class AutoServiceClientAuthenticationAttribute<T, K> : Attribute where T : IAutoSerialize,IAutoDeserialize where K: AutoServiceAuthenticationProto
{

}