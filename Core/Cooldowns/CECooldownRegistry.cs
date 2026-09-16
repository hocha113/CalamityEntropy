using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 冷却注册表:加载完成后反射收集所有 <see cref="CECooldownHandler"/> 子类,
    /// 按其静态 ID 属性建 字符串 ID → 处理器类型 映射。存档、同步均直接用字符串 ID。
    /// </summary>
    public sealed class CECooldownRegistry : ModSystem
    {
        private static Dictionary<string, Type> handlerTypes;

        public override void PostSetupContent() {
            handlerTypes = new Dictionary<string, Type>(64);
            foreach (var handler in ModContent.GetContent<CECooldownHandler>()) {
                Type type = handler.GetType();
                string id = (string)type.GetProperty("ID", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                id ??= handler.FullName;
                handlerTypes[id] = type;
            }
        }

        public override void Unload() {
            handlerTypes = null;
        }

        /// <summary>按 ID 查处理器类型。未注册返回 false。</summary>
        public static bool TryGetHandlerType(string id, out Type handlerType) {
            handlerType = null;
            return handlerTypes != null && handlerTypes.TryGetValue(id, out handlerType);
        }

        /// <summary>按 ID 创建一个新的处理器实例。未注册返回 null。</summary>
        public static CECooldownHandler CreateHandler(string id) {
            if (!TryGetHandlerType(id, out Type type))
                return null;
            return Activator.CreateInstance(type) as CECooldownHandler;
        }
    }
}
