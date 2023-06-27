using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;
using LeanCloud.Engine;

namespace web {
    public partial class App {
        // Function
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

        [LCEngineFunction("engineException")]
        public static string EngineException() {
            throw new LCEngineException(403, 111, "Engine exception");
        }

        [LCEngineFunction("session")]
        public static async Task<string> Session(LCEngineRequestContext context) {
            await Task.Delay(1000);

            if (string.IsNullOrEmpty(context.SessionToken)) {
                Console.WriteLine($"session: empty at {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                return string.Empty;
            }

            Console.WriteLine($"session: {context.SessionToken} at {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            return context.SessionToken;
        }
    }
}
