using System;
namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// 查询条件接口
    /// IEquatable<IQueryCondition> 用于比对（替换）相同的查询条件
    /// IJsonConvertible 用于生成序列化 Dictionary
    /// </summary>
    internal interface IQueryCondition : IEquatable<IQueryCondition>, IJsonConvertible {
    }
}
