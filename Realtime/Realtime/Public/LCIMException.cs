
namespace LeanCloud.Realtime {
    public class LCIMException : LCException {
        public int AppCode { get; private set; }

        public string AppMessage { get; private set; }

        public LCIMException(int code, string message, int appCode, string appMessage) :
            base(code, message) {
            AppCode = appCode;
            AppMessage = appMessage;
        }
    }
}
