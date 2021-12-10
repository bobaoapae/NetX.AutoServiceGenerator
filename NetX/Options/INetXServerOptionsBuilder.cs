namespace NetX.Options
{
    public interface INetXServerOptionsBuilder : INetXConnectionOptionsBuilder<INetXServer>
    {
        INetXServerOptionsBuilder UseProxy(bool useProxy);
        INetXServerOptionsBuilder Backlog(int backlog);
    }
}
