using CalamityEntropy.Content.NPCs.AbyssalWraith;
using CalamityEntropy.Content.NPCs.VoidInvasion;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidRitualCircle : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public float alpha = 0;
        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.timeLeft = 15000;
            Projectile.penetrate = -1;
            summonCount = 15000;
        }
        public int summonCount;
        public bool summoned = false;
        public override void AI()
        {
            Projectile.light = alpha * 6;
            int count = 0;
            foreach (NPC n in Main.npc)
            {
                if (n.active && n.ModNPC is VoidCultist vc)
                {
                    if (vc.aiStyle == VoidCultist.AIStyle.Summoning)
                    {
                        summonCount -= 1;
                        count++;
                        if (alpha < 0.02f)
                        {
                            alpha = 0.02f;
                        }
                        alpha += 0.98f / 15000f;
                        if (alpha > 1)
                        {
                            alpha = 1;
                        }
                        if (summonCount == 200)
                        {
                            foreach (NPC n2 in Main.npc)
                            {
                                if (n2.active && n2.ModNPC is VoidCultist vc2)
                                {
                                    vc2.noSummon = 46 * 60;
                                    n2.netUpdate = true;
                                }
                            }
                        }
                    }

                }
            }

            if (count == 0)
            {
                summoned = true;
                summonCount = 0;
            }

            if (summonCount <= 0)
            {
                if (!summoned)
                {
                    Projectile.netUpdate = true;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int np = NPC.NewNPC(new EntitySource_WorldEvent(), (int)Projectile.Center.X, (int)Projectile.Center.Y + 42, ModContent.NPCType<AbyssalWraith>());
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, np);
                    }
                    summoned = true;
                }
                alpha -= 0.025f;
                if (alpha <= 0)
                {
                    Projectile.Kill();
                }
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        float rotCount = 0;

        public override bool PreDraw(ref Color lightColor)
        {
            rotCount += 0.16f;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, rotCount, tex.Size() / 2, Projectile.scale * 2 * alpha, SpriteEffects.None, 0);
            return false;
        }
    }


}