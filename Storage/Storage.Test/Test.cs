using NUnit.Framework;
using System;
using System.Reflection;
using System.Diagnostics;
using LeanCloud;

namespace Storage.Test {
    [TestFixture()]
    public class Test {
        [Test()]
        public void TestCase() {
            Assembly assembly = Assembly.GetEntryAssembly();
            var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            Console.WriteLine(attr.InformationalVersion);

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            String version = versionInfo.FileVersion;
            Console.WriteLine(version);
        }
    }
}
