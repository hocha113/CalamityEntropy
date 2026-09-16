using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class IlmerisWaterBullet : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.alpha = 0;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            float limit = 5f;
            float scaleFactor = 6f;
            if (Projectile.ai[1] == 0f) {
                Projectile.ai[1] = 1f;
                Projectile.localAI[0] = (float)-(float)Main.rand.Next(48);
            }
            else if (Projectile.ai[1] == 1f && Projectile.owner == Main.myPlayer) {
                int targetIdx = -1;
                float npcRange = 350f;
                foreach (NPC n in Main.ActiveNPCs) {
                    if (n.CanBeChasedBy(Projectile, false)) {
                        Vector2 npcPos = n.Center;
                        float npcDist = Vector2.Distance(npcPos, Projectile.Center);
                        if (npcDist < npcRange && targetIdx == -1 && Collision.CanHitLine(Projectile.Center, 1, 1, npcPos, 1, 1)) {
                            npcRange = npcDist;
                            targetIdx = n.whoAmI;
                        }
                    }
                }
                if (targetIdx != -1) {
                    Projectile.ai[1] = limit + 1f;
                    Projectile.ai[0] = (float)targetIdx;
                    Projectile.netUpdate = true;
                }
            }
            else if (Projectile.ai[1] > limit) {
                Projectile.ai[1] += 1f;
                int idx = (int)Projectile.ai[0];
                if (!Main.npc[idx].active || !Main.npc[idx].CanBeChasedBy(Projectile, false)) {
                    Projectile.ai[1] = 1f;
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                else {
                    Projectile.velocity.ToRotation();
                    Vector2 toNPC = Main.npc[idx].Center - Projectile.Center;
                    if (toNPC.Length() < 20f) {
                        Projectile.Kill();
                        return;
                    }
                    if (toNPC != Vector2.Zero) {
                        toNPC.Normalize();
                        toNPC *= scaleFactor;
                    }
                    float homingSpeed = 20f;
                    Projectile.velocity = (Projectile.velocity * (homingSpeed - 1f) + toNPC) / homingSpeed;
                }
            }
            if (Projectile.ai[1] >= 1f && Projectile.ai[1] < limit) {
                Projectile.ai[1] += 1f;
                if (Projectile.ai[1] == limit) {
                    Projectile.ai[1] = 1f;
                }
            }

            int dustTypeRand = Utils.SelectRandom(Main.rand, new int[]
            {
                DustID.Water,
                DustID.Water_Snow,
                DustID.Water_GlowingMushroom
            });
            int dustType = DustID.Water_GlowingMushroom;
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] == 48f) {
                Projectile.localAI[0] = 0f;
            }
            else if (Projectile.alpha == 0) {
                for (int i = 0; i < 2; i++) {
                    Vector2 offset = Vector2.UnitX * -30f;
                    offset = -Vector2.UnitY.RotatedBy((double)(Projectile.localAI[0] * 0.1308997f + i * MathHelper.Pi), default) * new Vector2(10f, 20f) - Projectile.rotation.ToRotationVector2() * 10f;
                    int idx = Dust.NewDust(Projectile.Center, 0, 0, DustID.MagicMirror, 0f, 0f, 160, default, 1f);
                    Main.dust[idx].scale = 1f;
                    Main.dust[idx].noGravity = true;
                    Main.dust[idx].position = Projectile.Center + offset * 0.3f + Projectile.velocity * 2f;
                    Main.dust[idx].velocity = Vector2.Normalize(Projectile.Center + Projectile.velocity * 2f * 8f - Main.dust[idx].position) * 2f + Projectile.velocity * 2f;
                }
            }
            if (Main.rand.NextBool(12)) {
                Vector2 offset = -Vector2.UnitX.RotatedByRandom(0.2).RotatedBy((double)Projectile.velocity.ToRotation(), default);
                int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, 0f, 0f, 100, default, 1f);
                Main.dust[idx].velocity *= 0.1f;
                Main.dust[idx].noGravity = true;
                Main.dust[idx].position = Projectile.Center + offset * (float)Projectile.width / 2f + Projectile.velocity * 2f;
                Main.dust[idx].fadeIn = 0.9f;
            }
            if (Main.rand.NextBool(64)) {
                Vector2 offset = -Vector2.UnitX.RotatedByRandom(0.4).RotatedBy((double)Projectile.velocity.ToRotation(), default);
                int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, 0f, 0f, 155, default, 0.8f);
                Main.dust[idx].velocity *= 0.3f;
                Main.dust[idx].noGravity = true;
                Main.dust[idx].position = Projectile.Center + offset * (float)Projectile.width / 2f;
                if (Main.rand.NextBool()) {
                    Main.dust[idx].fadeIn = 1.4f;
                }
            }
            if (Main.rand.NextBool(4)) {
                for (int i = 0; i < 2; i++) {
                    Vector2 offset = -Vector2.UnitX.RotatedByRandom(0.8).RotatedBy((double)Projectile.velocity.ToRotation(), default);
                    int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustTypeRand, 0f, 0f, 0, default, 1.2f);
                    Main.dust[idx].velocity *= 0.3f;
                    Main.dust[idx].noGravity = true;
                    Main.dust[idx].position = Projectile.Center + offset * (float)Projectile.width / 2f;
                    if (Main.rand.NextBool()) {
                        Main.dust[idx].fadeIn = 1.4f;
                    }
                }
            }
            if (Main.rand.NextBool(3)) {
                Vector2 offset = -Vector2.UnitX.RotatedByRandom(0.2).RotatedBy((double)Projectile.velocity.ToRotation(), default);
                int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1f);
                Main.dust[idx].velocity *= 0.3f;
                Main.dust[idx].position = Projectile.Center + offset * (float)Projectile.width / 2f;
                Main.dust[idx].fadeIn = 1.2f;
                Main.dust[idx].scale = 1.5f;
                Main.dust[idx].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.25f / 255f, (255 - Projectile.alpha) * 0f / 255f, (255 - Projectile.alpha) * 0.25f / 255f);
            for (int i = 0; i < 2; i++) {
                int sizeFactor = 14;
                int idx = Dust.NewDust(Projectile.position, Projectile.width - sizeFactor * 2, Projectile.height - sizeFactor * 2, DustID.PortalBolt, 0f, 0f, 100, default, 1.35f);
                Main.dust[idx].noGravity = true;
                Main.dust[idx].velocity *= 0.1f;
                Main.dust[idx].velocity += Projectile.velocity * 0.5f;
            }
            if (Main.rand.NextBool(8)) {
                int sizeFactor = 16;
                int idx = Dust.NewDust(Projectile.position, Projectile.width - sizeFactor * 2, Projectile.height - sizeFactor * 2, DustID.PortalBolt, 0f, 0f, 100, default, 1f);
                Main.dust[idx].velocity *= 0.25f;
                Main.dust[idx].noGravity = true;
                Main.dust[idx].velocity += Projectile.velocity * 0.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

    }
}
