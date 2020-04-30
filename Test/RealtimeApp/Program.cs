using System;
using LeanCloud;
using LeanCloud.Realtime;

using static System.Console;

namespace RealtimeApp {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");

            SingleThreadSynchronizationContext.Run(async () => {
                LCLogger.LogDelegate += Print;
                LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");

                LCIMClient client = new LCIMClient("lean");
                await client.Open();
                //await client.Close();
            });
        }

        static void Print(LCLogLevel level, string info) {
            switch (level) {
                case LCLogLevel.Debug:
                    WriteLine($"[DEBUG] {DateTime.Now} {info}\n");
                    break;
                case LCLogLevel.Warn:
                    WriteLine($"[WARNING] {DateTime.Now} {info}\n");
                    break;
                case LCLogLevel.Error:
                    WriteLine($"[ERROR] {DateTime.Now} {info}\n");
                    break;
                default:
                    WriteLine(info);
                    break;
            }
        }
    }
}
