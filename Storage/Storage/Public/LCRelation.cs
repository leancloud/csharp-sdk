namespace LeanCloud.Storage {
    /// <summary>
    /// LCRelation provides access to all of the children of a many-to-many
    /// relationship.
    /// </summary>
    /// <typeparam name="T">The type of the child objects.</typeparam>
    public class LCRelation<T> where T : LCObject {
        /// <summary>
        /// The key of this LCRelation.
        /// </summary>
        public string Key {
            get; set;
        }

        /// <summary>
        /// The parent of this LCRelation.
        /// </summary>
        public LCObject Parent {
            get; set;
        }

        /// <summary>
        /// The className of this LCRelation.
        /// </summary>
        public string TargetClass {
            get; set;
        }

        /// <summary>
        /// Constructs a LCRelation.
        /// </summary>
        public LCRelation() {
        }

        /// <summary>
        /// Gets a query that can be used to query the objects in this relation.
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
