using System;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
    public class InstallationIdController {
        private const string InstallationIdKey = "InstallationId";
        private readonly object mutex = new object();
        private Guid? installationId;

        public Task SetAsync(Guid? installationId) {
            lock (mutex) {
                Task saveTask;

                if (installationId == null) {
                    saveTask = AVPlugins.Instance.StorageController
                      .LoadAsync()
                      .OnSuccess(storage => storage.Result.RemoveAsync(InstallationIdKey))
                      .Unwrap();
                } else {
                    saveTask = AVPlugins.Instance.StorageController
                      .LoadAsync()
                      .OnSuccess(storage => storage.Result.AddAsync(InstallationIdKey, installationId.ToString()))
                      .Unwrap();
                }
                this.installationId = installationId;
                return saveTask;
            }
        }

        public Task<Guid?> GetAsync() {
            lock (mutex) {
                if (installationId != null) {
                    return Task.FromResult(installationId);
                }
            }

            return AVPlugins.Instance.StorageController
              .LoadAsync()
              .OnSuccess(s => {
                  object id;
                  s.Result.TryGetValue(InstallationIdKey, out id);
                  try {
                      lock (mutex) {
                          installationId = new Guid((string)id);
                          return Task.FromResult(installationId);
                      }
                  } catch (Exception) {
                      var newInstallationId = Guid.NewGuid();
                      return SetAsync(newInstallationId).OnSuccess<Guid?>(_ => newInstallationId);
                  }
              })
              .Unwrap();
        }

        public Task ClearAsync() {
            return SetAsync(null);
        }
    }
}
