using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;

using IOFile = System.IO.File;

namespace LeanCloud.Common {
    public class PersistenceController {
        private readonly IPersistence persistence;

        public PersistenceController(IPersistence persistence) {
            this.persistence = persistence;
        }

        public async Task WriteText(string filename, string text) {
            if (persistence == null) {
                return;
            }

            string path = GetFileFullPath(filename);
            using (FileStream fs = IOFile.Create(path)) {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task<string> ReadText(string filename) {
            if (persistence == null) {
                return null;
            }

            string path = GetFileFullPath(filename);
            if (IOFile.Exists(path)) {
                string text;
                using (FileStream fs = IOFile.OpenRead(path)) {
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, (int)fs.Length);
                    text = Encoding.UTF8.GetString(buffer);
                }
                return text;
            }
            return null;
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
    }
}
