using System;
using StackExchange.Redis;

namespace web.LeanDB {
    public static class RedisHelper {
        public static ConnectionMultiplexer Redis { get; set; }

        public static void Init() {
            string host = Environment.GetEnvironmentVariable("REDIS_HOST_RNbho98Wzd");
            if (string.IsNullOrEmpty(host)) {
                return;
            }

            string port = Environment.GetEnvironmentVariable("REDIS_PORT_RNbho98Wzd");
            string user = Environment.GetEnvironmentVariable("REDIS_USER_RNbho98Wzd");
            string password = Environment.GetEnvironmentVariable("REDIS_PASSWORD_RNbho98Wzd");
            
            ConfigurationOptions config = new ConfigurationOptions {
                EndPoints = {
                    { host, int.Parse(port) }
                },
                User = user,
                Password = password
            };
            Redis = ConnectionMultiplexer.Connect(config);
        }
    }
}
