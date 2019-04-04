using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     循环向量化：识别可 SIMD 化的标量运算序列并转换为向量运算
///     TODO: Oa.Constant 类型已移除，此规则待修复
/// </summary>
// public sealed class VectorizeLoopRule : CommonRewriteRule
// {
//     /// <inheritdoc />
//     public override string name => "vectorize-loop";
//
//     /// <inheritdoc />
//     protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
//     {
//         var eclass = egraph.get_class(id);
//         if (eclass is null) yield break;
//
//         foreach (var node in eclass.nodes)
//         {
//             if (node is not Vec { size: 4 } vec) continue;
//             if (vec.components.Count != 4) continue;
//
//             if (TryGetArrayLoadPattern(egraph, vec.components, out var arrayId, out var startIndex))
//                 yield return (node, new VecLoad(arrayId, startIndex, 4));
//         }
//     }
//
//     private static bool TryGetArrayLoadPattern(EGraph<Oa> egraph, IReadOnlyList<Id> components, out Id arrayId,
//         out Id startIndex)
//     {
//         arrayId = default;
//         startIndex = default;
//
//         if (components.Count < 2) return false;
//
//         Id? firstArray = null;
//         long? firstIndex = null;
//
//         for (var i = 0; i < components.Count; i++)
//         {
//             var compClass = egraph.get_class(components[i]);
//             if (compClass is null) return false;
//
//             var loadNode = compClass.nodes.OfType<ArrayLoad>().FirstOrDefault();
//             if (loadNode is null) return false;
//
//             var indexClass = egraph.get_class(loadNode.index);
//             var indexConst = indexClass?.nodes.OfType<Oa.Constant>().FirstOrDefault();
//             if (indexConst is null) return false;
//
//             if (firstArray is null)
//             {
//                 firstArray = loadNode.array;
//                 firstIndex = indexConst.value;
//             }
//             else
//             {
//                 if (!firstArray.Value.Equals(loadNode.array)) return false;
//                 if (indexConst.value != firstIndex.Value + i) return false;
//             }
//         }
//
//         if (firstArray is null) return false;
//
//         arrayId = firstArray.Value;
//         var startIndexClass = egraph.classes.Values
//             .FirstOrDefault(c => c.nodes.OfType<Literal<long>>().Any(k => k.value == firstIndex.Value));
//         if (startIndexClass is null) return false;
//         startIndex = startIndexClass.id;
//
//         return true;
//     }
// }