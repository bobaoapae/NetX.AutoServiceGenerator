namespace NetX.Options
{
    public interface INetXServerOptionsProcessorBuilder
    {
        INetXServerOptionsBuilder Processor<T>() where T : INetXServerProcessor, new();
        INetXServerOptionsBuilder Processor(INetXServerProcessor processorInstance);
    }
}
