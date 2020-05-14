namespace LeanCloud.Storage {
    /// <summary>
    /// 关系类
    /// </summary>
    public class LCRelation<T> where T : LCObject {
        /// <summary>
        /// 字段名
        /// </summary>
        public string Key {
            get; set;
        }

        /// <summary>
        /// 父对象
        /// </summary>
        public LCObject Parent {
            get; set;
        }

        /// <summary>
        /// 关联类型名
        /// </summary>
        public string TargetClass {
            get; set;
        }

        public LCRelation() {
        }

        /// <summary>
        /// 获取 Relation 的查询对象
        /// </summary>
        public LCQuery<T> Query {
            get {
                LCQuery<T> query = new LCQuery<T>(TargetClass);
                query.WhereRelatedTo(Parent, Key);
                return query;
            }
        }
    }
}
