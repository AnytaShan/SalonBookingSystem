using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalonBookingSystem.Tests
{
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new Dictionary<string, byte[]>();

        public string Id => "test-session-id";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _storage.Keys;

        public void Clear() => _storage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _storage.Remove(key);
        public void Set(string key, byte[] value) => _storage[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            return _storage.TryGetValue(key, out value);
        }

        // Удобный метод для установки int
        public void SetInt32(string key, int value)
        {
            _storage[key] = System.BitConverter.GetBytes(value);
        }
    }
}