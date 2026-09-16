using CalamityEntropy.Content.NPCs.VoidInvasion;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 虚空前卫教徒:驱逐舰「支援投送」召来的地面小怪。接近→蓄力→冲斩→收招→撤离的动作与帧图
    /// 全部沿用 VoidCultistAssassin,这里只改数值:免疫击退,冲刺段接触伤害即「虚空斩」
    /// </summary>
    public class VoidVanguardCultist : VoidCultistAssassin
    {
        public const int BaseLife = 5200;
        public const int BaseDefense = 48;
        public const int BaseDamage = 100;
        /// <summary>虚空斩 360(大师显示) = 接触 300 的 1.2 倍</summary>
        public const float SlashDamageMult = 1.2f;

        public override string Texture => "CalamityEntropy/Content/NPCs/VoidInvasion/VoidCultistAssassin";

        public override void SetStaticDefaults() {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.VoidVanguardCultistBestiary")
            });
        }

        public override void SetDefaults() {
            base.SetDefaults();
            NPC.lifeMax = BaseLife;
            NPC.defense = BaseDefense;
            NPC.damage = BaseDamage;
            NPC.knockBackResist = 0f;
            NPC.value = Item.buyPrice(0, 0, 40, 0);
            NPC.Entropy().VoidTouchDR = 0.6f;
        }

        public override void OnSpawn(IEntitySource source) {
            base.OnSpawn(source);
            //投送落地即追击,不走基类的随机发呆
            aiStyle = AIStyle.Closing;
            tryCloseTime = 30;
        }

        public override void PostAI() {
            base.PostAI();
            //冲刺段(attackAnmStyle == 1)接触伤害提升为虚空斩,其余时间回落到普通接触;defDamage 已含难度缩放
            bool slashing = aiStyle == AIStyle.Attack && attackAnmStyle == 1;
            NPC.damage = slashing ? (int)(NPC.defDamage * SlashDamageMult) : NPC.defDamage;

            //驱逐舰不在场(被击杀或脱战消失)时随之散去,不留一地小怪;基类 CheckActive 恒假,得自己收
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[0] % 30 == 0 && !NPC.AnyNPCs(ModContent.NPCType<VoidDestroyer>())) {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                if (Main.netMode == NetmodeID.Server) {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
            }
        }
    }
}
