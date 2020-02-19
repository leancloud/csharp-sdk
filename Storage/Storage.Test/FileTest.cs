using NUnit.Framework;
using LeanCloud;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Test {
    public class FileTest {
        string saveFileId;

        [SetUp]
        public void SetUp() {
            //Utils.InitNorthChina();
            //Utils.InitEastChina();
            //Utils.InitOldEastChina();
            Utils.InitUS();
        }

        [Test, Order(0)]
        public async Task SaveImage() {
            AVFile file = new AVFile("hello.png", File.ReadAllBytes("../../../assets/hello.png"));
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            saveFileId = file.ObjectId;
            TestContext.Out.WriteLine($"file: {file.ObjectId}, {file.Url}");
        }

        [Test, Order(1)]
        public async Task SaveBigFile() {
            AVFile file = new AVFile("test.apk", File.ReadAllBytes("../../../assets/test.apk"));
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            TestContext.Out.WriteLine($"file: {file.ObjectId}, {file.Url}");
        }

        [Test, Order(2)]
        public async Task SaveUrl() {
            AVFile file = new AVFile("test.jpg", "http://pic33.nipic.com/20131007/13639685_123501617185_2.jpg");
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            TestContext.Out.WriteLine($"file: {file.ObjectId}, {file.Url}");
        }

        [Test, Order(3)]
        public async Task Thumbnail() {
            AVQuery<AVFile> query = new AVQuery<AVFile>();
            AVFile file = await query.GetAsync(saveFileId, CancellationToken.None);
            Assert.NotNull(file);
            TestContext.Out.WriteLine($"url: {file.Url}");
            TestContext.Out.WriteLine($"thumbnail url: {file.GetThumbnailUrl(28, 28)}");
        }

        [Test, Order(4)]
        public async Task DeleteFile() {
            AVFile file = new AVFile("hello.png", File.ReadAllBytes("../../../assets/hello.png"));
            await file.SaveAsync();
            Assert.NotNull(file.ObjectId);
            await file.DeleteAsync();
        }
    }
}
