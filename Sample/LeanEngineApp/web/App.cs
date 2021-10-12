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
    }
}
