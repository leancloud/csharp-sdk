using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

using IOFile = System.IO.File;

namespace LeanCloud.Common {
    public class PersistenceController {
        private readonly IPersistence persistence;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> pathLocks;

        public PersistenceController(IPersistence persistence) {
            this.persistence = persistence;
            pathLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async Task WriteText(string filename, string text) {
            if (persistence == null) {
                return;
            }

            string path = GetFileFullPath(filename);
            SemaphoreSlim semaphore = GetSemaphore(path);
            await semaphore.WaitAsync();
            try {
                using (FileStream fs = IOFile.Create(path)) {
                    byte[] buffer = Encoding.UTF8.GetBytes(text);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            } finally {
                semaphore.Release();
            }
        }

        public async Task<string> ReadText(string filename) {
            if (persistence == null) {
                return null;
            }

            string path = GetFileFullPath(filename);
            if (!IOFile.Exists(path)) {
                return null;
            }

            SemaphoreSlim semaphore = GetSemaphore(path);
            await semaphore.WaitAsync();
            try {
                using (FileStream fs = IOFile.OpenRead(path)) {
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, (int)fs.Length);
                    return Encoding.UTF8.GetString(buffer);
                }
            } finally {
                semaphore.Release();
            }
        }

        public Task Delete(string filename) {
            if (persistence == null) {
                return Task.CompletedTask;
            }

            string path = GetFileFullPath(filename);
            return Task.Run(() => {
                IOFile.Delete(path);
            });
        }

        private string GetFileFullPath(string filename) {
            if (persistence == null) {
                throw new Exception("no IStrorage.");
            }
            return Path.Combine(persistence.GetPersistencePath(), filename);
        }

        private SemaphoreSlim GetSemaphore(string path) {
            return pathLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        }
    }
}
