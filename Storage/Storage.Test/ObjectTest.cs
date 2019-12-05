using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud.Common;

namespace LeanCloud.Test {
    public class ObjectTests {
        [SetUp]
        public void SetUp() {
            Logger.LogDelegate += Utils.Print;
            Utils.InitNorthChina();
        }

        [TearDown]
        public void TearDown() {
            Logger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task Save() {
            AVObject obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            obj["list"] = new List<int> { 1, 1, 2, 3, 5, 8 };
            obj["dict"] = new Dictionary<string, int> {
                { "hello", 1 },
                { "world", 2 }
            };
            await obj.Save();
            Assert.NotNull(obj.ObjectId);
            Assert.NotNull(obj.CreatedAt);
            Assert.NotNull(obj.UpdatedAt);
        }

        [Test]
        public async Task SaveWithOptions() {
            AVObject account = AVObject.CreateWithoutData("Account", "5d65fa5330863b008065e476");
            account["balance"] = 100;
            await account.SaveAsync();
            AVQuery<AVObject> query = new AVQuery<AVObject>("Account");
            query.WhereGreaterThan("balance", 80);
            account["balance"] = 50;
            await account.SaveAsync(true, query);
            TestContext.Out.WriteLine($"balance: {account["balance"]}");
        }

        //[Test]
        //public async Task SaveWithPointer() {
        //    AVObject comment = new AVObject("Comment") {
        //        { "content", "Hello, Comment" }
        //    };

        //    AVObject post = new AVObject("Post") {
        //        { "name", "New Post" },
        //        { "category", new AVObject("Category") {
        //            { "name", "new post category" }
        //        } }
        //    };
        //    comment["post"] = post;

        //    AVObject testPost = new AVObject("Post") {
        //        { "name", "Test Post" },
        //        { "category", new AVObject("Category") {
        //            { "name", "test post category" }
        //        } }
        //    };
        //    comment["test_post"] = testPost;

        //    await comment.Save();
        //    TestContext.Out.WriteLine(post);
        //    TestContext.Out.WriteLine(testPost);
        //    TestContext.Out.WriteLine(comment);
        //}

        [Test]
        public async Task SaveWithPointer() {
            AVObject parent = new AVObject("Parent");
            AVObject c1 = new AVObject("C1");
            AVObject c2 = new AVObject("C2");
            parent["c1"] = c1;
            parent["c2"] = c2;
            await parent.Save();
        }

        [Test]
        public async Task SaveWithPointerArray() {
            AVObject parent = new AVObject("Parent");
            AVObject c1 = new AVObject("C1");
            AVObject c2 = new AVObject("C2");
            parent["iList"] = new List<int> { 1, 1, 2, 3 };
            parent["cList"] = new List<AVObject> { c1, c2 };
            parent["cDict"] = new Dictionary<string, AVObject> {
                { "c1", c1 },
                { "c2", c2 }
            };
            await parent.SaveAsync();

        }

        [Test]
        public async Task SaveBatch() {
            List<AVObject> objList = new List<AVObject>();
            for (int i = 0; i < 5; i++) {
                AVObject obj = AVObject.Create("Foo");
                obj["content"] = "batch object";
                objList.Add(obj);
            }
            try {
                await objList.SaveAllAsync();
                objList.ForEach(obj => {
                    Assert.NotNull(obj.ObjectId);
                });
            } catch (Exception e) {
                TestContext.Out.WriteLine(e.Message);
            }
        }

        [Test]
        public async Task Fetch() {
            AVObject obj = AVObject.CreateWithoutData("Todo", "5d5f6039d5de2b006cf29c8f");
            await obj.FetchAsync();
            Assert.NotNull(obj["title"]);
            Assert.NotNull(obj["content"]);
            TestContext.Out.WriteLine($"{obj["title"]}, {obj["content"]}");
        }

        [Test]
        public async Task FetchWithKeys() {
            AVObject obj = AVObject.CreateWithoutData("Post", "5d3abfa530863b0068e1b326");
            await obj.FetchAsync(new List<string> { "pubUser" });
            TestContext.Out.WriteLine($"{obj["pubUser"]}");
        }

        [Test]
        public async Task FetchWithIncludes() {
            AVObject obj = AVObject.CreateWithoutData("Post", "5d3abfa530863b0068e1b326");
            await obj.FetchAsync(includes: new List<string> { "tag" });
            AVObject tag = obj["tag"] as AVObject;
            TestContext.Out.WriteLine($"{tag["name"]}");
        }

        [Test]
        public async Task FetchAll() {
            List<AVObject> objList = new List<AVObject> {
                AVObject.CreateWithoutData("Tag", "5d64e5ebc05a8000730340ba"),
                AVObject.CreateWithoutData("Tag", "5d64e5eb12215f0073db271c"),
                AVObject.CreateWithoutData("Tag", "5d64e57f43e78c0068a14315")
            };
            await objList.FetchAllAsync();
            objList.ForEach(obj => {
                Assert.NotNull(obj.ObjectId);
                TestContext.Out.WriteLine($"{obj.ObjectId}, {obj["name"]}");
            });
        }

        [Test]
        public async Task Delete() {
            AVObject obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            await obj.SaveAsync();
            Assert.NotNull(obj);
            await obj.DeleteAsync();
        }

        [Test]
        public async Task DeleteWithCondition() {
            AVObject account = new AVObject("Account") {
                { "balance", 100 },
            };
            account.ACL = new AVACL {
                PublicWriteAccess = true,
                PublicReadAccess = true
            };
            await account.SaveAsync();
            AVQuery<AVObject> condition = new AVQuery<AVObject>();
            condition.WhereGreaterThan("balance", 10);
            await account.DeleteAsync(condition);

            account = new AVObject("Account") {
                { "balance", 8 },
            };
            account.ACL = new AVACL {
                PublicWriteAccess = true,
                PublicReadAccess = true
            };
            await account.SaveAsync();
            condition = new AVQuery<AVObject>();
            condition.WhereGreaterThan("balance", 10);
            try {
                await account.DeleteAsync(condition);
            } catch (AVException e) {
                Assert.AreEqual(e.Code, AVException.ErrorCode.NoEffectOnUpdatingOrDeleting);
            }
        }

        [Test]
        public async Task DeleteAll() {
            List<AVObject> objList = new List<AVObject>();
            for (int i = 0; i < 5; i++) {
                AVObject obj = AVObject.Create("Foo");
                obj.ACL = new AVACL {
                    PublicReadAccess = true,
                    PublicWriteAccess = i % 2 == 0
                };
                obj["content"] = "batch object";
                objList.Add(obj);
            }
            await objList.SaveAllAsync();
            try {
                await AVObject.DeleteAllAsync(objList);
            } catch (AggregateException e) {
                foreach (AVException ie in e.InnerExceptions) {
                    TestContext.Out.WriteLine($"{ie.Code} : {ie.Message}");
                }
            }            
        }

        [Test]
        public async Task TestMassiveRequest() {
            ThreadPool.SetMaxThreads(1, 1);
            await Task.Run(() => {
                for (int i = 0; i < 10; i++) {
                    for (int j = 0; j < 50; j++) {
                        AVObject obj = AVObject.Create("Foo");
                        obj.SaveAsync().ContinueWith(_ => {
                            TestContext.Out.WriteLine($"{obj.ObjectId} saved at {Thread.CurrentThread.ManagedThreadId}");
                        });
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        [Test]
        public void SimpleCircleReference() {
            AVObject a = new AVObject("A");
            AVObject b = new AVObject("B");
            a["b"] = b;
            b["a"] = a;

            Assert.ThrowsAsync<AVException>(async () => await a.Save());
        }

        [Test]
        public void IndirectCircleReference() {
            AVObject a = new AVObject("A");
            AVObject b = new AVObject("B");
            AVObject c = new AVObject("C");
            a["b"] = b;
            b["c"] = c;
            c["a"] = a;

            Assert.ThrowsAsync<AVException>(async () => await a.Save());
        }

        [Test]
        public void SimpleCollectionPointerCircleReference() {
            AVObject a = new AVObject("A");
            AVObject b = new AVObject("B");
            a["children"] = new List<object> { 1, b };
            b["children"] = new Dictionary<string, object> {
                { "c", a }
            };

            Assert.ThrowsAsync<AVException>(async () => await a.Save());
        }

        [Test]
        public void IndirectCollectionPointerCircleReference() {
            AVObject a = new AVObject("A");
            AVObject b = new AVObject("B");
            AVObject c = new AVObject("C");

            a["children"] = new List<object> { 1, b };
            b["children"] = new List<object> { 2, c };
            c["children"] = new Dictionary<string, object> {
                { "c", a }
            };

            Assert.ThrowsAsync<AVException>(async () => await a.Save());
        }
    }
}