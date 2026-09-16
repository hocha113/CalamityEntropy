using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Utilities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Teletor : ModProjectile
    {
        public LightningAdvanced l1;
        public LightningAdvanced l2;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 32;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.5f;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
            Projectile.ArmorPenetration = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public override bool PreAI() {
            if (Projectile.localAI[1] == 0) {
                Vector2 L1 = Projectile.Center + new Vector2(-11 * Projectile.direction, -12).RotatedBy(Projectile.rotation);
                Vector2 L2 = Projectile.Center + new Vector2(11 * Projectile.direction, -14).RotatedBy(Projectile.rotation);

                l1 = new LightningAdvanced(L1, weaponPos);
                l2 = new LightningAdvanced(L2, weaponPos);
            }
            ++Projectile.localAI[1];
            return base.PreAI();
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.WriteVector2(weaponVel);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            weaponVel = reader.ReadVector2();
        }
        public Vector2 weaponVel = Vector2.Zero;
        public Vector2 weaponPos { get { return new Vector2(Projectile.ai[1], Projectile.ai[2]); } set { Projectile.ai[1] = value.X; Projectile.ai[2] = value.Y; } }
        public override void AI() {
            if (Projectile.localAI[0]++ == 0) {
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.TwoPi * 0.05f) {
                    var dust = Dust.NewDustDirect(Projectile.Center, 0, 0, Main.rand.NextBool() ? DustID.RedTorch : DustID.BlueTorch);
                    dust.position = Projectile.Center;
                    dust.noGravity = true;
                    dust.velocity = i.ToRotationVector2() * 8;
                    dust.scale = 1.1f;

                }
            }
            if (!Main.dedServ)
                Main.instance.LoadItem(4);
            Player player = Main.player[Projectile.owner];
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 3600) {
                Projectile.Center = player.Center;
                Vector2 L1 = Projectile.Center + new Vector2(-11 * Projectile.direction, -12).RotatedBy(Projectile.rotation);
                Vector2 L2 = Projectile.Center + new Vector2(11 * Projectile.direction, -14).RotatedBy(Projectile.rotation);
            }
            if (CEUtils.getDistance(weaponPos, Projectile.Center) > 3000) {
                weaponPos = Projectile.Center;
                Vector2 L1 = Projectile.Center + new Vector2(-11 * Projectile.direction, -12).RotatedBy(Projectile.rotation);
                Vector2 L2 = Projectile.Center + new Vector2(11 * Projectile.direction, -14).RotatedBy(Projectile.rotation);

                l1 = new LightningAdvanced(L1, weaponPos);
                l2 = new LightningAdvanced(L2, weaponPos);
            }
            if (player.HasBuff(ModContent.BuffType<TeletorBuff>())) {
                Projectile.timeLeft = 3;
            }
            NPC target = Projectile.FindMinionTarget(850, true);

            Projectile.ai[0]--;
            Vector2 targetPos = player.Center - new Vector2(0, 120);
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 60) {
                Projectile.velocity += (targetPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.8f;
                Projectile.velocity *= 0.98f;
            }
            if (target != null) {
                Projectile.direction = (target.Center.X > Projectile.Center.X ? 1 : -1);
                if (CEUtils.getDistance(Projectile.Center, weaponPos) < 120) {
                    Projectile.ai[0] = -1;
                }
                if (Projectile.ai[0] >= 0) {
                    weaponVel += (Projectile.Center + new Vector2(0, -60) - weaponPos).SafeNormalize(Vector2.Zero) * 5f;
                }
                else {
                    weaponVel += (target.Center - weaponPos).SafeNormalize(Vector2.Zero) * 5f;
                }
            }
            else {
                weaponVel += (Projectile.Center + new Vector2(0, -60) - weaponPos).SafeNormalize(Vector2.Zero) * 5f;
                Projectile.direction = (Projectile.velocity.X > 0 ? 1 : -1);

            }
            weaponVel *= 0.9f;

            Projectile.rotation = MathHelper.ToRadians(Projectile.velocity.X * 1.4f);

            weaponPos += weaponVel;
            if (Projectile.owner == Main.myPlayer) {
                Projectile.netUpdate = true;
            }


        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.ai[0] <= 0) {
                Projectile.ai[0] = 90;
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return new Rectangle((int)(weaponPos.X - 8), (int)(weaponPos.Y - 8), 16, 16).Intersects(targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 L1 = Projectile.Center + new Vector2(-11 * Projectile.direction, -12).RotatedBy(Projectile.rotation);
            Vector2 L2 = Projectile.Center + new Vector2(11 * Projectile.direction, -14).RotatedBy(Projectile.rotation);
            l1.Update(L1, weaponPos);
            l2.Update(L2, weaponPos);
            List<Vector2> points1 = l1.GetPoints();
            List<Vector2> points2 = l2.GetPoints();
            CEUtils.DrawLines(points1, Color.Red * 0.6f, 2);
            CEUtils.DrawLines(points2, Color.Blue * 0.6f, 2);

            Texture2D weaponTex = TextureAssets.Item[4].Value;
            Main.spriteBatch.Draw(weaponTex, weaponPos - Main.screenPosition, null, Lighting.GetColor((int)weaponPos.X / 16, (int)weaponPos.Y / 16), MathHelper.ToRadians(-45) + MathHelper.ToRadians(weaponVel.X * 1.8f), weaponTex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            Texture2D spt = TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(spt, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, spt.Size() / 2, Projectile.scale, (Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally), 0);
            return false;
        }

    }
}

