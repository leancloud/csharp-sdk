﻿using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;
using System.Security.Cryptography;
using System.IO;
using LeanCloud.Common;

namespace Storage.Test {
    public class FileTest : BaseTest {
        static readonly string AvatarFilePath = "../../../../../assets/hello.png";
        static readonly string APKFilePath = "../../../../../assets/test.apk";
        static readonly string VideoFilePath = "../../../../../assets/video.mp4";

        private LCFile video;

        [Test]
        [Order(0)]
        public async Task SaveFromPath() {
            LCFile avatar = new LCFile("avatar.png", AvatarFilePath);
            await avatar.Save((count, total) => {
                TestContext.WriteLine($"progress: {count}/{total}");
            });
            TestContext.WriteLine(avatar.ObjectId);
            Assert.NotNull(avatar.ObjectId);
        }

        [Test]
        [Order(1)]
        public async Task SaveBigFileFromPath() {
            video = new LCFile("video.mp4", VideoFilePath);
            await video.Save((count, total) => {
                TestContext.WriteLine($"progress: {count}/{total}");
            });
            TestContext.WriteLine(video.ObjectId);
            Assert.NotNull(video.ObjectId);
        }

        [Test]
        [Order(2)]
        public async Task QueryFile() {
            LCQuery<LCFile> query = LCFile.GetQuery();
            LCFile file = await query.Get(video.ObjectId);
            Assert.NotNull(file.Url);
            TestContext.WriteLine(file.Url);
            TestContext.WriteLine(file.GetThumbnailUrl(32, 32));
        }

        [Test]
        [Order(3)]
        public async Task SaveFromMemory() {
            string text = "hello, world";
            byte[] data = Encoding.UTF8.GetBytes(text);
            LCFile file = new LCFile("text.txt", data);
            file.PathPrefix = "gamesaves";
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
            Assert.True(file.Url.Contains("gamesaves"));
        }

        [Test]
        [Order(4)]
        public async Task SaveFromUrl() {
            LCFile file = new LCFile("scene.jpg", new Uri("http://img95.699pic.com/photo/50015/9034.jpg_wh300.jpg"));
            file.AddMetaData("size", 1024);
            file.AddMetaData("width", 128);
            file.AddMetaData("height", 256);
            file.MimeType = "image/jpg";
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        [Order(5)]
        public async Task Qiniu() {
            LCFile file = new LCFile("test.apk", APKFilePath);
            await file.Save();
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        [Order(6)]
        public async Task FileACL() {
            LCUser user = await LCUser.LoginAnonymously();

            LCFile file = new LCFile("avatar.png", AvatarFilePath);
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

        [Test]
        [Order(10)]
        public async Task AWS() {
            LCApplication.Initialize("HudJvWWmAuGMifwxByDVLmQi-MdYXbMMI", "YjoQr1X8wHoFIfsSGXzeJaAM");
            LCFile file = new LCFile("avatar.png", AvatarFilePath);
            await file.Save((count, total) => {
                TestContext.WriteLine($"progress: {count}/{total}");
            });
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        [Order(11)]
        public async Task AWSBigFile() {
            LCApplication.Initialize("HudJvWWmAuGMifwxByDVLmQi-MdYXbMMI", "YjoQr1X8wHoFIfsSGXzeJaAM",
                "https://hudjvwwm.api.lncldglobal.com");
            LCFile file = new LCFile("video.mp4", VideoFilePath);
            await file.Save((count, total) => {
                TestContext.WriteLine($"progress: {count}/{total}");
            });
            TestContext.WriteLine(file.ObjectId);
            Assert.NotNull(file.ObjectId);
        }

        [Test]
        [Order(12)]
        public async Task CheckSum() {
            LCFile avatar = new LCFile("avatar.png", AvatarFilePath);
            await avatar.Save();

            LCQuery<LCFile> query = new LCQuery<LCFile>(LCFile.CLASS_NAME);
            LCFile file = await query.Get(avatar.ObjectId);
            string sum = file.MetaData["_checksum"] as string;

            using (MD5 md5 = MD5.Create()) {
                using (FileStream fs = new FileStream(AvatarFilePath, FileMode.Open)) {
                    fs.Position = 0;
                    byte[] bytes = md5.ComputeHash(fs);
                    string hash = LCUtils.ToHex(bytes);
                    Assert.AreEqual(sum, hash);
                }
            }
        }
    }
}
