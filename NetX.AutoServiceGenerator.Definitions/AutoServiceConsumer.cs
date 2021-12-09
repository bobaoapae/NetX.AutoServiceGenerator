using System;

namespace NetX.AutoServiceGenerator.Definitions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoServiceConsumer : Attribute
    {
        public Type AutoService { get; }

        public AutoServiceConsumer(Type autoService)
        {
            AutoService = autoService;
        }
    }
}