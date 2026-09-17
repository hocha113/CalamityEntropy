using System;
using System.Collections.Generic;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 原版 On_* 钩子的订阅登记处。
    /// 订阅与退订各传一个无参 lambda:两者必须成对写出,少写一边编译不过,
    /// 于是"Unload 漏退订"这类泄漏在结构上不再可能发生。
    /// 退订由 <see cref="CalamityEntropy.Unload"/> 统一调 <see cref="UndoAll"/> 完成,
    /// 各钩子模块只管在 LoadData 里登记,不需要各自写卸载。
    /// </summary>
    internal static class CEDetourRegistry
    {
        //模块用 GetUninitializedObject 创建,实例字段初始化器不执行,所以登记表必须是静态的
        private static readonly List<Action> UndoActions = new List<Action>();

        /// <summary>已登记的钩子数,供加载期自查用</summary>
        public static int Count => UndoActions.Count;

        /// <summary>
        /// 登记一个钩子:立即订阅,同时记下与之配对的退订动作。
        /// 两个参数必须写成 <c>() =&gt; On_Xxx.Yyy += Handler</c> 的形式,而不是接一个处理器参数:
        /// tML 的 On_* 事件类型是具名委托(如 <c>On_Player.hook_AddBuff</c>),
        /// 若让泛型从处理器实参去推断,方法组只会被推断成 Action/Func,转不成具名委托。
        /// 把 += 写进 lambda 体内,方法组则直接对着事件的 add 访问器转换,类型自然吻合。
        /// </summary>
        public static void Add(Action subscribe, Action unsubscribe) {
            subscribe();
            UndoActions.Add(unsubscribe);
        }

        /// <summary>按登记的逆序全部退订。可重复调用</summary>
        public static void UndoAll() {
            for (int i = UndoActions.Count - 1; i >= 0; i--) {
                UndoActions[i]();
            }
            UndoActions.Clear();
        }
    }
}
