using System;
using System.Threading.Tasks;
using LeanCloud.Common;

namespace LeanCloud.Storage {
    public class LCInstallation : LCObject {
        public const string CLASS_NAME = "_Installation";
        public const string ENDPOINT = "installations";

        private const string DEVICE_DATA = ".devicedata";

        private static LCInstallation currentInstallation;

        public LCInstallation() : base(CLASS_NAME) {
        }

        public static async Task<LCInstallation> GetCurrent() {
            if (currentInstallation != null) {
                return currentInstallation;
            }

            string data = await LCCore.PersistenceController.ReadText(DEVICE_DATA);
            if (!string.IsNullOrEmpty(data)) {
                try {
                    currentInstallation = ParseObject(data) as LCInstallation;
                    return currentInstallation;
                } catch (Exception e) {
                    LCLogger.Error(e);
                    await LCCore.PersistenceController.Delete(DEVICE_DATA);
                }
            }

            currentInstallation = new LCInstallation();
            return currentInstallation;
        }

        public new async Task<LCObject> Save(bool fetchWhenSave = false, LCQuery<LCObject> query = null) {
            await base.Save(fetchWhenSave, query);

            await SaveToLocal();
            return this;
        }

        private static async Task SaveToLocal() {
            try {
                string json = currentInstallation.ToString();
                await LCCore.PersistenceController.WriteText(DEVICE_DATA, json);
            } catch (Exception e) {
                LCLogger.Error(e);
            }
        }

        public static LCQuery<LCInstallation> GetQuery() {
            return new LCQuery<LCInstallation>(CLASS_NAME);
        }
    }
}
