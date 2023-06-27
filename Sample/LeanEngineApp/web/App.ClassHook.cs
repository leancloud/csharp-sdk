using System;
using System.Collections.ObjectModel;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.Engine;

namespace web {
    public partial class App {
        private const string HOOK_CLASS_NAME = "TestHookClass";

        // Class Hook
        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.BeforeSave)]
        public static LCObject BeforeSaveClass(LCEngineRequestContext context, LCObject obj) {
            Console.WriteLine($"Before save from {context.RemoteAddress}");
            if (obj["score"] == null) {
                obj["score"] = 60;
            }
            return obj;
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.AfterSave)]
        public static void AfterSaveClass(LCObject obj) {
            Console.WriteLine($"Saved {obj.ObjectId}");
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.BeforeUpdate)]
        public static LCObject BeforeUpdateClass(LCEngineRequestContext context, LCObject obj) {
            Console.WriteLine($"Before update from {context.RemoteAddress}");
            ReadOnlyCollection<string> updatedKeys = obj.GetUpdatedKeys();
            if (updatedKeys.Contains("score")) {
                int score = (int)obj["score"];
                if (score > 100) {
                    throw new Exception($"Error score: {score}");
                }
            }
            return obj;
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.AfterUpdate)]
        public static void AfterUpdateClass(LCObject obj) {
            Console.WriteLine($"Updated {obj.ObjectId}");
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.BeforeDelete)]
        public static void BeforeDeleteClass(LCEngineRequestContext context, LCObject obj) {
            Console.WriteLine($"Before delete from {context.RemoteAddress}");
            throw new Exception($"Cannot delete {obj.ClassName}");
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.AfterDelete)]
        public static void AfterDeleteClass(LCObject obj) {
            LCLogger.Debug($"Deleted {obj.ObjectId}");
        }
    }
}
