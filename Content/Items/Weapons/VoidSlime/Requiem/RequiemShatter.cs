using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Requiem
{
    /// <summary>安息弹留下的永久破甲,不叠加,减防在 RequiemShatterGlobalNPC</summary>
    public class RequiemShatter : ModBuff
    {
        public const int DefenseReduction = 30;
        public const int PinTime = 18000;           //帧,NPC 增益时长按 short 同步,每帧钉回此值即永久

        public override string Texture => "Terraria/Images/Buff_" + BuffID.BrokenArmor;

        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex) {
            npc.buffTime[buffIndex] = PinTime;
        }
    }

    public class RequiemShatterGlobalNPC : GlobalNPC
    {
        //只对免疫一切增益的目标无效,常规免疫与指定免疫都放开
        public override void SetDefaults(NPC npc) {
            if (!NPCID.Sets.ImmuneToAllBuffs[npc.type])
                npc.buffImmune[ModContent.BuffType<RequiemShatter>()] = false;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
            if (Shattered(npc))
                modifiers.Defense.Flat -= RequiemShatter.DefenseReduction;
        }

        private static bool Shattered(NPC npc) {
            int type = ModContent.BuffType<RequiemShatter>();
            if (npc.HasBuff(type))
                return true;
            int head = npc.realLife;
            return head >= 0 && head < Main.maxNPCs && Main.npc[head].active && Main.npc[head].HasBuff(type);
        }
    }
}
