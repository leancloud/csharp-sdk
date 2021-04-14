namespace LeanCloud.Storage {

    public class LCRelation<T> where T : LCObject {
        public string Key {
            get; set;
        }

        public LCObject Parent {
            get; set;
        }

        public string TargetClass {
            get; set;
        }

        public LCRelation() {
        }

        public LCQuery<T> Query {
            get {
                LCQuery<T> query = new LCQuery<T>(TargetClass);
                query.WhereRelatedTo(Parent, Key);
                return query;
            }
        }
    }
}
