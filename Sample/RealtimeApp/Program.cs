using System;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Realtime;

using static System.Console;

namespace RealtimeApp {
    class Program {
        static void Main(string[] args) {
            WriteLine("Hello World!");

            SingleThreadSynchronizationContext.Run(async () => {
                LCLogger.LogDelegate += Print;
                LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");

                LCIMClient client = new LCIMClient("lean") {
                    OnPaused = () => {
                        WriteLine("~~~~~~~~~~~~~~~ disconnected");
                    },
                    OnResume = () => {
                        WriteLine("~~~~~~~~~~~~~~~ reconnected");
                    }
                };

                await client.Open();

                int count = 0;
                while (count < 2) {
                    WriteLine($"pause : {count}");

                    await Task.Delay(5 * 1000);
                    LCRealtime.Pause();

                    await Task.Delay(5 * 1000);
                    LCRealtime.Resume();

                    await Task.Delay(5 * 1000);
                    count++;
                }

                try {
                    await client.Close();
                    // Done
                } catch (Exception e) {
                    WriteLine($"xxxxxxxxxxxx {e.Message}");
                }
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
