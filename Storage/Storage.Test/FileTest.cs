using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class FileTest : BaseTest {
        static readonly string AvatarFilePath = "../../../../../assets/hello.png";
        static readonly string APKFilePath = "../../../../../assets/test.apk";

        private LCFile avatar;

        [Test]
        [Order(0)]
        public async Task SaveFromPath() {
            avatar = new LCFile("avatar", AvatarFilePath);
            await avatar.Save((count, total) => {
                TestContext.WriteLine($"progress: {count}/{total}");
            });
            TestContext.WriteLine(avatar.ObjectId);
            Assert.NotNull(avatar.ObjectId);
        }

        [Test]
        [Order(1)]
        public async Task QueryFile() {
            LCQuery<LCFile> query = LCFile.GetQuery();
            LCFile file = await query.Get(avatar.ObjectId);
            Assert.NotNull(file.Url);
            TestContext.WriteLine(file.Url);
            TestContext.WriteLine(file.GetThumbnailUrl(32, 32));
        }

        [Test]
        public async Task SaveFromMemory() {
            string text = "hello, world";
            byte[] data = Encoding.UTF8.GetBytes(text);
            LCFile file = new LCFile("text", data);
            file.PathPrefix = "gamesaves";
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
            Assert.True(file.Url.Contains("gamesaves"));
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
            LCFile file = new LCFile("avatar", APKFilePath);
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        public async Task FileACL() {
            LCUser user = await LCUser.LoginAnonymously();

            LCFile file = new LCFile("avatar", AvatarFilePath);
            LCACL acl = new LCACL();
            acl.SetUserReadAccess(user, true);
            file.ACL = acl;
            await file.Save();

            LCQuery<LCFile> query = LCFile.GetQuery();
            LCFile avatar = await query.Get(file.ObjectId);
            Assert.NotNull(avatar.ObjectId);

            await LCUser.LoginAnonymously();
            try {
                LCFile forbiddenAvatar = await query.Get(file.ObjectId);
            } catch (LCException e) {
                Assert.AreEqual(e.Code, 403);
            }
        }

        //[Test]
        //public async Task AWS() {
        //    LCApplication.Initialize("UlCpyvLm8aMzQsW6KnP6W3Wt-MdYXbMMI", "PyCTYoNoxCVoKKg394PBeS4r");
        //    LCFile file = new LCFile("avatar", AvatarFilePath);
        //    await file.Save((count, total) => {
        //        TestContext.WriteLine($"progress: {count}/{total}");
        //    });
        //    TestContext.WriteLine(file.ObjectId);
        //    Assert.NotNull(file.ObjectId);
        //}
    }
}
