using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LeanCloud
{
    /// <summary>
    /// A common base class for AVRelations.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class AVRelationBase : IJsonConvertible
    {
        private AVObject parent;
        private string key;
        private string targetClassName;

        internal AVRelationBase(AVObject parent, string key)
        {
            EnsureParentAndKey(parent, key);
        }

        internal AVRelationBase(AVObject parent, string key, string targetClassName)
          : this(parent, key)
        {
            this.targetClassName = targetClassName;
        }

        internal static ObjectSubclassingController SubclassingController
        {
            get
            {
                return AVPlugins.Instance.SubclassingController;
            }
        }

        internal void EnsureParentAndKey(AVObject parent, string key)
        {
            this.parent = this.parent ?? parent;
            this.key = this.key ?? key;
            Debug.Assert(this.parent == parent, "Relation retrieved from two different objects");
            Debug.Assert(this.key == key, "Relation retrieved from two different keys");
        }

        internal void Add(AVObject obj)
        {
            var change = new AVRelationOperation(new[] { obj }, null);
            parent.PerformOperation(key, change);
            targetClassName = change.TargetClassName;
        }

        internal void Remove(AVObject obj)
        {
            var change = new AVRelationOperation(null, new[] { obj });
            parent.PerformOperation(key, change);
            targetClassName = change.TargetClassName;
        }

        IDictionary<string, object> IJsonConvertible.ToJSON()
        {
            return new Dictionary<string, object> {
                { "__type", "Relation"},
                { "className", targetClassName}
            };
        }

        internal AVQuery<T> GetQuery<T>() where T : AVObject
        {
            if (targetClassName != null)
            {
                return new AVQuery<T>(targetClassName)
                  .WhereRelatedTo(parent, key);
            }

            return new AVQuery<T>(parent.ClassName)
              .RedirectClassName(key)
              .WhereRelatedTo(parent, key);
        }

        internal AVQuery<T> GetReverseQuery<T>(T target) where T : AVObject
        {
            if (target.ObjectId == null)
            {
                throw new ArgumentNullException("target.ObjectId", "can not query a relation without target ObjectId.");
            }

            return new AVQuery<T>(parent.ClassName).WhereEqualTo(key, target);
        }

        internal string TargetClassName
        {
            get
            {
                return targetClassName;
            }
            set
            {
                targetClassName = value;
            }
        }

        /// <summary>
        /// Produces the proper AVRelation&lt;T&gt; instance for the given classname.
        /// </summary>
        internal static AVRelationBase CreateRelation(AVObject parent,
            string key,
            string targetClassName)
        {
            var targetType = SubclassingController.GetType(targetClassName) ?? typeof(AVObject);

            Expression<Func<AVRelation<AVObject>>> createRelationExpr =
                () => CreateRelation<AVObject>(parent, key, targetClassName);
            var createRelationMethod =
                ((MethodCallExpression)createRelationExpr.Body)
                .Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(targetType);
            return (AVRelationBase)createRelationMethod.Invoke(null, new object[] { parent, key, targetClassName });
        }

        private static AVRelation<T> CreateRelation<T>(AVObject parent, string key, string targetClassName)
            where T : AVObject
        {
            return new AVRelation<T>(parent, key, targetClassName);
        }
    }

    /// <summary>
    /// Provides access to all of the children of a many-to-many relationship. Each instance of
    /// AVRelation is associated with a particular parent and key.
    /// </summary>
    /// <typeparam name="T">The type of the child objects.</typeparam>
    public sealed class AVRelation<T> : AVRelationBase where T : AVObject
    {

        internal AVRelation(AVObject parent, string key) : base(parent, key) { }

        internal AVRelation(AVObject parent, string key, string targetClassName)
          : base(parent, key, targetClassName) { }

        /// <summary>
        /// Adds an object to this relation. The object must already have been saved.
        /// </summary>
        /// <param name="obj">The object to add.</param>
        public void Add(T obj)
        {
            base.Add(obj);
        }

        /// <summary>
        /// Removes an object from this relation. The object must already have been saved.
        /// </summary>
        /// <param name="obj">The object to remove.</param>
        public void Remove(T obj)
        {
            base.Remove(obj);
        }

        /// <summary>
        /// Gets a query that can be used to query the objects in this relation.
        /// </summary>
        public AVQuery<T> Query
        {
            get
            {
                return base.GetQuery<T>();
            }
        }
    }
}
