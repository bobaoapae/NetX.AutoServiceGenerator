namespace NetX
{
    public interface ISessionFactory<T>
        where T : INetXSession
    {
        public T CreateSession();
    }
}
