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

        [Test]
        public async Task SaveUrl() {
            AVFile file = new AVFile("test.jpg", "http://pic33.nipic.com/20131007/13639685_123501617185_2.jpg");
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            TestContext.Out.WriteLine($"file: {file.ObjectId}, {file.Url}");
        }

        [Test]
        public async Task Thumbnail() {
            AVFile file = await AVFile.GetFileWithObjectIdAsync("5d64ac55d5de2b006c1fe3d8");
            Assert.NotNull(file);
            TestContext.Out.WriteLine($"url: {file.Url}");
            TestContext.Out.WriteLine($"thumbnail url: {file.GetThumbnailUrl(28, 28)}");
        }

        [Test]
        public async Task DeleteFile() {
            await AVUser.LogInAsync("111111", "111111");
            AVFile file = new AVFile("hello.png", File.ReadAllBytes("../../../assets/hello.png"));
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            await file.DeleteAsync();
        }
    }
}
