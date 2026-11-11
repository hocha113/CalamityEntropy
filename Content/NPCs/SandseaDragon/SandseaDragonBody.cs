using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using CalamityEntropy.Core.AI;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>
    /// 沙海冥龙的身体节:锚定型部件,位置每帧从头部的脊柱直接读,velocity 保持 0,只清平滑不进预测器
    /// ai[0] 头部下标,ai[1] 节序 1..4,血量经 realLife 记在头部账上,绘制由头部集中做
    /// </summary>
    public class SandseaDragonBody : ModNPC
    {
        public override string Texture => "CalamityEntropy/Content/NPCs/SandseaDragon/SandseaDragonBody1";

        protected virtual int SegmentDefense => SandseaDirector.DefenseBody;

        /// <summary>客户端连续找不到头部的帧数,收包顺序不定,留一段宽限再消失</summary>
        private int orphanFrames;

        public int SegmentIndex => (int)NPC.ai[1];

        public SandseaDragonHead Head {
            get {
                int index = (int)NPC.ai[0];
                if (index < 0 || index >= Main.maxNPCs) {
                    return null;
                }
                NPC npc = Main.npc[index];
                return npc.active && npc.ModNPC is SandseaDragonHead head ? head : null;
            }
        }

        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults() {
            NPC.aiStyle = -1;
            NPC.width = 96;
            NPC.height = 96;
            NPC.damage = SandseaDirector.BaseDamage;
            NPC.defense = SegmentDefense;
            NPC.lifeMax = SandseaDirector.BaseLife;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.netAlways = true;
            NPC.dontCountMe = true;
            NPC.npcSlots = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = null;
        }

        /// <summary>死亡演出里从尾部起逐节炸掉,炸掉的节不再有判定</summary>
        private bool Gone(SandseaDragonHead head)
            => head.Context != null && head.Context.SegmentsGone >= SandseaDirector.SegmentCount - SegmentIndex;

        public override void AI() {
            CEBossHost.RunAnchoredPartFrame(NPC);
            SandseaDragonHead head = Head;
            if (head == null) {
                if (!VaultUtils.isClient || ++orphanFrames > 90) {
                    NPC.active = false;
                }
                return;
            }
            orphanFrames = 0;

            NPC host = head.NPC;
            bool gone = Gone(head);
            NPC.realLife = host.whoAmI;
            NPC.life = host.life;
            NPC.lifeMax = host.lifeMax;
            NPC.target = host.target;
            NPC.dontTakeDamage = host.dontTakeDamage || gone;
            NPC.damage = head.ContactDamageActive() && !gone ? NPC.defDamage : 0;
            NPC.velocity = Vector2.Zero;
            if (head.TryGetSegment(SegmentIndex, out Vector2 center, out float rotation)) {
                NPC.Center = center;
                NPC.rotation = rotation;
            }
            else {
                NPC.Center = host.Center;
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) => Head?.ApplyHitRules(ref modifiers);

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            SandseaDragonHead head = Head;
            return head != null && head.ContactDamageActive() && !Gone(head);
        }

        public override void HitEffect(NPC.HitInfo hit) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 3; i++) {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sand, hit.HitDirection * 2f, -1.5f);
            }
        }

        public override bool CheckActive() => false;

        /// <summary>只随头部一起消失,自己不单独死</summary>
        public override bool CheckDead() {
            if (Head == null) {
                return true;
            }
            NPC.life = 1;
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
    }

    /// <summary>尾部,防御更低,判定更小</summary>
    public class SandseaDragonTail : SandseaDragonBody
    {
        public override string Texture => "CalamityEntropy/Content/NPCs/SandseaDragon/SandseaDragonTail";

        protected override int SegmentDefense => SandseaDirector.DefenseTail;

        public override void SetDefaults() {
            base.SetDefaults();
            NPC.width = 84;
            NPC.height = 72;
        }
    }
}
