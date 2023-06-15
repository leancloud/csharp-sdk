using System.Text;
using StackExchange.Redis;
using MySql.Data.MySqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using LeanCloud.Engine;
using LeanCloud;
using web.LeanDB;

namespace web {
    public partial class App {
        // Function
        [LCEngineFunction("setRedis")]
        public static void SetRedis([LCEngineFunctionParam("key")] string key,
            [LCEngineFunctionParam("value")] string val) {
            if (string.IsNullOrEmpty(key)) {
                throw new LCException(100, "Invalid key");
            }

            IDatabase db = RedisHelper.Redis.GetDatabase();
            db.StringSet(key, val);
        }

        [LCEngineFunction("getRedis")]
        public static string GetRedis([LCEngineFunctionParam("key")] string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new LCException(100, "Invalid key");
            }

            IDatabase db = RedisHelper.Redis.GetDatabase();
            string val = db.StringGet(key);
            return val;
        }

        [LCEngineFunction("queryMySQL")]
        public static string QueryMySQL() {
            string sql = "SELECT * FROM hello";
            MySqlCommand command = new MySqlCommand(sql, MySQLHelper.Conn);
            MySqlDataReader reader = command.ExecuteReader();
            StringBuilder sb = new StringBuilder();
            while (reader.Read()) {
                sb.AppendLine($"{reader[0]} -- {reader[1]}");
            }
            return sb.ToString();
        }

        [LCEngineFunction("insertMongo")]
        public static void InserMongo([LCEngineFunctionParam("id")] string id) {
            IMongoCollection<BsonDocument> collection = MongoHelper.Client
                .GetDatabase("leancloud")
                .GetCollection<BsonDocument>("hello");
            BsonDocument doc = new BsonDocument {
                { "id", id }
            };
            collection.InsertOne(doc);
        }

        [LCEngineFunction("queryMongo")]
        public static string QueryMongo() {
            IMongoCollection<BsonDocument> collection = MongoHelper.Client
                .GetDatabase("leancloud")
                .GetCollection<BsonDocument>("hello");
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Empty;
            return collection.Find(filter).ToList().ToJson();
        }
    }
}
