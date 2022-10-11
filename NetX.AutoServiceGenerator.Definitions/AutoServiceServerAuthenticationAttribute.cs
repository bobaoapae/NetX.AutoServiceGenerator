using System;
using AutoSerializer.Definitions;

namespace NetX.AutoServiceGenerator.Definitions;

[AttributeUsage(AttributeTargets.Class)]
public class AutoServiceServerAuthenticationAttribute<T, TK> : Attribute where T : IAutoServiceAuthentication<TK> where TK : IAutoSerialize,IAutoDeserialize
{

}