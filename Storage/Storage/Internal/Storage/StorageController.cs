using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;

using IOFile = System.IO.File;

namespace LeanCloud.Storage.Internal.Storage {
    public class StorageController {
        private readonly IStorage storage;

        public StorageController(IStorage storage) {
            this.storage = storage;
        }

        public async Task WriteText(string filename, string text) {
            if (storage == null) {
                return;
            }

            string path = GetFileFullPath(filename);
            LCLogger.Debug($"WRITE: {path}");
            LCLogger.Debug($"WRITE: {text}");
            using (FileStream fs = IOFile.OpenWrite(path)) {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task<string> ReadText(string filename) {
            if (storage == null) {
                return null;
            }

            string path = GetFileFullPath(filename);
            LCLogger.Debug($"READ: {path}");
            if (IOFile.Exists(path)) {
                string text;
                using (FileStream fs = IOFile.OpenRead(path)) {
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, (int)fs.Length);
                    text = Encoding.UTF8.GetString(buffer);
                }
                LCLogger.Debug($"READ: {text}");
                return text;
            }
            return null;
        }

        public Task Delete(string filename) {
            if (storage == null) {
                return Task.CompletedTask;
            }

            string path = GetFileFullPath(filename);
            return Task.Run(() => {
                IOFile.Delete(path);
            });
        }

        private string GetFileFullPath(string filename) {
            if (storage == null) {
                throw new Exception("no IStrorage.");
            }
            return Path.Combine(storage.GetStoragePath(), filename);
        }
    }
}
