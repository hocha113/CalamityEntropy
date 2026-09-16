using CalamityEntropy.Core.AI;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Apsychos
{
    /// <summary>
    /// 尾尖碰撞体。位置由本体每帧写入,自身不做运动积分。
    /// 锚定部件:清掉原版平滑,不进预测纠偏器(下一帧位置不是 position + velocity)
    /// </summary>
    public class ApsychosTail : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 50;
            NPC.height = 50;
            NPC.damage = 32;
            NPC.dontTakeDamage = true;
            NPC.lifeMax = 1400;
            NPC.DeathSound = null;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 5;
            NPC.aiStyle = -1;
        }

        public NPC owner => ((int)NPC.ai[0]).ToNPC();

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => true;

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override void AI()
        {
            CEBossHost.RunAnchoredPartFrame(NPC);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!owner.active || owner == null || owner.ModNPC == null || owner.ModNPC is not Apsychos)
                {
                    NPC.active = false;
                    if (Main.dedServ)
                    {
                        NPC.netUpdate = true;
                        NPC.netSpam = 0;
                    }
                }
            }
        }

        public override bool CheckActive() => !owner.active;

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            npcHitbox = npcHitbox.Center.ToVector2().getRectCentered(npcHitbox.Width * NPC.scale, npcHitbox.Height * NPC.scale);
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;
    }
}
