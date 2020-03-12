using NUnit.Framework;
using LeanCloud.Realtime.Protocol;
using Google.Protobuf;

namespace Realtime.Test {
    public class Protobuf {
        [Test]
        public void Serialize() {
            GenericCommand command = new GenericCommand {
                Cmd = CommandType.Session,
                Op = OpType.Open,
                PeerId = "hello"
            };
            SessionCommand session = new SessionCommand {
                Code = 123
            };
            command.SessionMessage = session;
            byte[] bytes = command.ToByteArray();
            TestContext.WriteLine($"length: {bytes.Length}");

            command = GenericCommand.Parser.ParseFrom(bytes);
            Assert.AreEqual(command.Cmd, CommandType.Session);
            Assert.AreEqual(command.Op, OpType.Open);
            Assert.AreEqual(command.PeerId, "hello");
            Assert.NotNull(command.SessionMessage);
            Assert.AreEqual(command.SessionMessage.Code, 123);
        }
    }
}