using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// Audio message.
    /// </summary>
    [AVIMMessageClassName("_AVIMAudioMessage")]
    [AVIMTypedMessageTypeInt(-3)]
    public class AVIMAudioMessage : AVIMFileMessage
    {

    }

    /// <summary>
    /// Video message.
    /// </summary>
    [AVIMMessageClassName("_AVIMVideoMessage")]
    [AVIMTypedMessageTypeInt(-4)]
    public class AVIMVideoMessage: AVIMFileMessage
    {

    }
}
