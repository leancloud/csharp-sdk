using NUnit.Framework;
using LeanCloud;
using System.IO;
using System.Threading.Tasks;

namespace LeanCloudTests {
    public class FileTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
            //Utils.InitEastChina();
            //Utils.InitUS();
        }

        [Test]
        public async Task SaveImage() {
            AVFile file = new AVFile("hello.png", File.ReadAllBytes("../../../assets/hello.png"));
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            TestContext.Out.WriteLine($"file: {file.ObjectId}, {file.Url}");
        }

        [Test]
        public async Task SaveBigFile() {
            AVFile file = new AVFile("test.apk", File.ReadAllBytes("../../../assets/test.apk"));
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            TestContext.Out.WriteLine($"file: {file.ObjectId}, {file.Url}");
        }
    }
}
