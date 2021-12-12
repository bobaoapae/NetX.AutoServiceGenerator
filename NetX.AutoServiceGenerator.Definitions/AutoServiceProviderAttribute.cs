using System;

namespace NetX.AutoServiceGenerator.Definitions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoServiceProviderAttribute : Attribute
    {
        public Type AutoService { get; }

        public AutoServiceProviderAttribute(Type autoService)
        {
            AutoService = autoService;
        }
    }
}