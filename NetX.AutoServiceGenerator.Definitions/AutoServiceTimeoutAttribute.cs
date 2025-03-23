using System;

namespace NetX.AutoServiceGenerator.Definitions;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class AutoServiceTimeoutAttribute : Attribute
{
    public int Timeout { get; set; }
}