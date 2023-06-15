using System;
using MongoDB.Driver;
using MongoDB.Bson;

namespace web.LeanDB {
    public class MongoHelper {
        public static MongoClient Client { get; set; }

        public static void Init() {
            string url = Environment.GetEnvironmentVariable("MONGODB_URL_mongo");
            if (string.IsNullOrEmpty(url)) {
                return;
            }

            Client = new MongoClient(url);
        }
    }
}
