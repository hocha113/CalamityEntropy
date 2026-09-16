using CalamityEntropy.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Buffs.PortsDoT
{
    /// <summary>
    /// PortsDoT 移植减益公共基类。
    /// DoT 结算集中在 <see cref="CEDoTGlobalNPC"/>，此处只负责注册参数与减益标志。
    /// </summary>
    public abstract class CEPortDebuff : ModBuff
    {
        /// <summary>DoT 结算参数；null 表示无 DoT（纯减益或纯标记）</summary>
        public virtual CEDoTEntry Entry => null;

        /// <summary>专家模式减益时长延长（对齐灾厄各类原设置）</summary>
        public virtual bool LongerExpert => true;

        /// <summary>护士不可移除</summary>
        public virtual bool NurseCannotRemove => false;

        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            if (LongerExpert)
                BuffID.Sets.LongerExpertDebuff[Type] = true;
            if (NurseCannotRemove)
                BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;

            var entry = Entry;
            if (entry != null)
                CEDoTGlobalNPC.Register(Type, entry);
        }
    }
}
