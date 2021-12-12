using System;

namespace NetX.AutoServiceGenerator.Definitions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoServiceConsumerAttribute : Attribute
    {
        public Type AutoService { get; }

        public AutoServiceConsumerAttribute(Type autoService)
        {
            AutoService = autoService;
        }
    }
}