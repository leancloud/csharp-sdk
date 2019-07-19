using System;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal
{
    public interface IObjectState : IEnumerable<KeyValuePair<string, object>>
    {
        bool IsNew { get; }
        string ClassName { get; }
        string ObjectId { get; }
        DateTime? UpdatedAt { get; }
        DateTime? CreatedAt { get; }
        object this[string key] { get; }

        bool ContainsKey(string key);

        IObjectState MutatedClone(Action<MutableObjectState> func);
    }
}
