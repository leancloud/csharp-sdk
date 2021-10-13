using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Storage;
using LeanCloud.Engine;
using LeanCloud;

namespace web {
    public class App {
        private const string HOOK_CLASS_NAME = "TestHookClass";

        // Function
        [LCEngineFunction("ping")]
        public static string Ping() {
            return "pong";
        }

        [LCEngineFunction("hello")]
        public static string Hello([LCEngineFunctionParam("name")] string name) {
            string msg = $"hello, {name}";
            Console.WriteLine(msg);
            return msg;
        }

        [LCEngineFunction("getObject")]
        public static async Task<LCObject> GetObject([LCEngineFunctionParam("className")] string className,
            [LCEngineFunctionParam("id")] string id) {
            LCQuery<LCObject> query = new LCQuery<LCObject>(className);
            return await query.Get(id);
        }

        [LCEngineFunction("getObjects")]
        public static async Task<ReadOnlyCollection<LCObject>> GetObjects() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.WhereGreaterThan("balance", 100);
            return await query.Find();
        }

        [LCEngineFunction("getObjectMap")]
        public static async Task<Dictionary<string, LCObject>> GetObjectMap() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Todo");
            ReadOnlyCollection<LCObject> todos = await query.Find();
            return todos.ToDictionary(t => t.ObjectId);
        }

        [LCEngineFunction("lcexception")]
        public static string LCException() {
            throw new LCException(123, "Runtime exception");
        }

        [LCEngineFunction("exception")]
        public static string Exception() {
            throw new Exception("Hello, exception");
        }

        // Class Hook
        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.BeforeSave)]
        public static LCObject BeforeSaveClass(LCObject obj) {
            if (obj["score"] == null) {
                obj["score"] = 60;
            }
            return obj;
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.AfterSave)]
        public static void AfterSaveClass(LCObject obj) {
            LCLogger.Debug($"Saved {obj.ObjectId}");
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.BeforeUpdate)]
        public static LCObject BeforeUpdateClass(LCObject obj) {
            ReadOnlyCollection<string> updatedKeys = obj.GetUpdatedKeys();
            if (updatedKeys.Contains("score")) {
                int score = (int) obj["score"];
                if (score > 100) {
                    throw new Exception($"Error score: {score}");
                }
            }
            return obj;
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.AfterUpdate)]
        public static void AfterUpdateClass(LCObject obj) {
            LCLogger.Debug($"Updated {obj.ObjectId}");
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.BeforeDelete)]
        public static void BeforeDeleteClass(LCObject obj) {
            throw new Exception($"Cannot delete {obj.ClassName}");
        }

        [LCEngineClassHook(HOOK_CLASS_NAME, LCEngineObjectHookType.AfterDelete)]
        public static void AfterDeleteClass(LCObject obj) {
            LCLogger.Debug($"Deleted {obj.ObjectId}");
        }
    }
}
