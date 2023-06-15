using System;
using MySql.Data.MySqlClient;

namespace web.LeanDB {
    public class MySQLHelper {
        public static MySqlConnection Conn { get; set; }

        public static void Init() {
            string host = Environment.GetEnvironmentVariable("MYSQL_HOST_mysql");
            if (string.IsNullOrEmpty(host)) {
                return;
            }

            string port = Environment.GetEnvironmentVariable("MYSQL_PORT_mysql");
            string uid = Environment.GetEnvironmentVariable("MYSQL_ADMIN_USER_mysql");
            string password = Environment.GetEnvironmentVariable("MYSQL_ADMIN_PASSWORD_mysql");
            string connectionString = $"server={host};port={port};uid={uid};pwd={password};database=leancloud";
            Console.WriteLine(connectionString);

            Conn = new MySqlConnection(connectionString);
            Conn.Open();
        }
    }
}
