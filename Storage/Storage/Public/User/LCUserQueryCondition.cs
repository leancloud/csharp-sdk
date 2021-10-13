using System;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Query;

namespace LeanCloud.Storage {
    public class LCUserQueryCondition : LCCompositionalCondition {
        public new int Skip {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public new int Limit {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public new void WhereMatchesQuery<K>(string key, LCQuery<K> query) where K : LCObject {
            throw new NotImplementedException();
        }

        public LCUserQueryCondition() {
            composition = And;
        }

        public new Dictionary<string, object> BuildParams() {
            Dictionary<string, object> dict = base.BuildParams();
            // LCUserQueryCondition 不支持下面的参数
            dict.Remove("skip");
            dict.Remove("limit");
            return dict;
        }
    }
}
