using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityEntropy.Core.ChatTags
{
    /// <summary>
    /// 聊天标签处理器基类：实现 <see cref="ITagHandler"/> 并借助 <see cref="ILoadable"/> 自动注册标签别名。
    /// 原先继承灾厄的 AbstractTagHandler，脱离灾厄后本地实现同等行为。
    /// </summary>
    /// <typeparam name="TSelf">要注册的标签处理器自身类型。</typeparam>
    public abstract class CETagHandler<TSelf> : ITagHandler, ILoadable
        where TSelf : CETagHandler<TSelf>, new()
    {
        /// <summary>标签别名列表。</summary>
        protected abstract string[] TagNames { get; }

        /// <inheritdoc cref="ITagHandler.Parse"/>
        public abstract TextSnippet Parse(string text, Color baseColor = new(), string options = null);

        public virtual void Load(Mod mod) {
            ChatManager.Register<TSelf>(TagNames);
        }

        public virtual void Unload() {
            // 无需反注册：tML 卸载模组后会重建 ChatManager 的标签表
        }
    }
}
