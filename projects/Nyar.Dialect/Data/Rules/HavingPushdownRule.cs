using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     Having 谓词下推：Having(GroupBy(keys, aggs, Filter(pred, data)), having_pred)
///     → GroupBy(keys, aggs, Filter(pred, data)) + Having(having_pred)
///     当 Having 条件只引用分组键时，可下推为 Filter
///     TODO: Oa.Symbol 类型已移除，此规则待修复
/// </summary>
// public sealed class HavingPushdownRule : CommonRewriteRule
// {
//     /// <inheritdoc />
//     public override string name => "having-pushdown";
//
//     /// <inheritdoc />
//     protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
//     {
//         var eclass = egraph.get_class(id);
//         if (eclass is null) yield break;
//
//         foreach (var node in eclass.nodes)
//         {
//             if (node is not Having having) continue;
//
//             var dataClass = egraph.get_class(having.data);
//             if (dataClass is null) continue;
//
//             foreach (var dataNode in dataClass.nodes)
//             {
//                 if (dataNode is not GroupBy gb) continue;
//
//                 if (ReferencesOnlyGroupKeys(having.predicate, gb.keys, egraph))
//                 {
//                     var pushedFilter = egraph.add(new Filter(having.predicate, gb.data));
//                     yield return (node, new GroupBy(gb.keys, gb.aggregates, pushedFilter));
//                 }
//             }
//         }
//     }
//
//     /// <summary>
//     ///     检查谓词是否只引用分组键（不引用聚合结果）
//     /// </summary>
//     private static bool ReferencesOnlyGroupKeys(Id predicate, IReadOnlyList<string> groupKeys, EGraph<Oa> egraph)
//     {
//         var predClass = egraph.get_class(predicate);
//         if (predClass is null) return false;
//
//         foreach (var predNode in predClass.nodes)
//         {
//             if (predNode is Oa.Symbol symbol && !groupKeys.Contains(symbol.name))
//             {
//                 return false;
//             }
//         }
//
//         return true;
//     }
// }