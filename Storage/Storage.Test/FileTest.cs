using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.Test {
    public class FileTest {
        static readonly string AvatarFilePath = "../../../assets/hello.png";

        [SetUp]
        public void SetUp() {
            Logger.LogDelegate += Utils.Print;
            LeanCloud.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            Logger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task QueryFile() {
            LCQuery<LCFile> query = LCFile.GetQuery();
            LCFile file = await query.Get("5e0dbfa0562071008e21c142");
            Assert.NotNull(file.Url);
            TestContext.WriteLine(file.Url);
            TestContext.WriteLine(file.GetThumbnailUrl(32, 32));
        }

        [Test]
        public async Task SaveFromPath() {
            LCFile file = new LCFile("avatar", AvatarFilePath);
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        public async Task SaveFromMemory() {
            string text = "hello, world";
            byte[] data = Encoding.UTF8.GetBytes(text);
            LCFile file = new LCFile("text", data);
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        public async Task SaveFromUrl() {
            LCFile file = new LCFile("scene", new Uri("http://img95.699pic.com/photo/50015/9034.jpg_wh300.jpg"));
            file.AddMetaData("size", 1024);
            file.AddMetaData("width", 128);
            file.AddMetaData("height", 256);
            file.MimeType = "image/jpg";
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        public async Task Qiniu() {
            LCFile file = new LCFile("avatar", AvatarFilePath);
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        public async Task AWS() {
            Logger.LogDelegate += Utils.Print;
            LeanCloud.Initialize("UlCpyvLm8aMzQsW6KnP6W3Wt-MdYXbMMI", "PyCTYoNoxCVoKKg394PBeS4r", "https://ulcpyvlm.api.lncldglobal.com");
            LCFile file = new LCFile("avatar", "../../../assets/hello.png");
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }
    }
}
