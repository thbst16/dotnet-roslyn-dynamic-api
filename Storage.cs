namespace dotnet_roslyn_dynamic_api
{
    public class Storage<T> where T : class
    {
        private Dictionary<Guid, T> storage = new Dictionary<Guid, T>();

        public IEnumerable<T> GetAll() => storage.Values;

        public T GetById(Guid id)
        {
            return storage.FirstOrDefault(x => x.Key == id).Value;
        }

        public void Add(Guid id, T item)
        {
            storage[id] = item;
        }
    }
}