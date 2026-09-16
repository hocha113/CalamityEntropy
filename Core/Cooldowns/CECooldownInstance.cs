using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Core.Cooldowns
{
    /// <summary>
    /// 运行时冷却实例,API 形状对齐原灾厄 CooldownInstance(player / duration / timeLeft / Completion / handler)。
    /// 网络与存档标识直接用字符串 ID,不引入 netID。
    /// </summary>
    public class CECooldownInstance
    {
        private const string DurationSaveKey = "duration";
        private const string TimeLeftSaveKey = "timeLeft";

        /// <summary>冷却的字符串 ID。</summary>
        public readonly string ID;

        /// <summary>冷却所属玩家。</summary>
        public Player player;

        /// <summary>冷却总时长(帧)。</summary>
        public int duration;

        /// <summary>冷却剩余时长(帧)。</summary>
        public int timeLeft;

        /// <summary>该实例的行为处理器。注册表查不到 ID 时为 null,调用方应丢弃此实例。</summary>
        public CECooldownHandler handler;

        /// <summary>剩余比例,1 为刚开始,0 为已结束。</summary>
        public float Completion => duration != 0 ? timeLeft / (float)duration : 0;

        public CECooldownInstance(Player p, string id, int dur) {
            ID = id;
            player = p;
            duration = dur;
            timeLeft = dur;
            handler = CECooldownRegistry.CreateHandler(id);
            if (handler != null)
                handler.instance = this;
        }

        internal CECooldownInstance(Player p, string id, TagCompound tag)
            : this(p, id, tag.GetAsInt(DurationSaveKey)) {
            timeLeft = tag.GetAsInt(TimeLeftSaveKey);
        }

        internal TagCompound Save() {
            return new TagCompound
            {
                { DurationSaveKey, duration },
                { TimeLeftSaveKey, timeLeft }
            };
        }

        /// <summary>网络序列化(供加入同步使用)。</summary>
        internal void Write(BinaryWriter writer) {
            writer.Write(ID);
            writer.Write(duration);
            writer.Write(timeLeft);
        }

        /// <summary>网络反序列化。ID 未注册时 handler 为 null,调用方应丢弃。</summary>
        internal static CECooldownInstance Read(BinaryReader reader, Player player) {
            string id = reader.ReadString();
            int duration = reader.ReadInt32();
            int timeLeft = reader.ReadInt32();
            return new CECooldownInstance(player, id, duration) { timeLeft = timeLeft };
        }
    }
}
