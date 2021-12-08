namespace NetX.Options
{
    public interface INetXClientOptionsProcessorBuilder
    {
        INetXClientOptionsBuilder Processor<T>() where T : INetXClientProcessor, new();
        INetXClientOptionsBuilder Processor(INetXClientProcessor processorInstance);
    }
}
