using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Nemesis
{
    internal class NemesisProj : ModProjectile
    {
        private bool canHeal;
        private bool spwanProj;
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 54;
            Projectile.timeLeft = 120;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.penetrate = 10;
            Projectile.extraUpdates = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            Projectile.tileCollide = Projectile.position.Y > player.position.Y;
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (Projectile.ai[0] == 1 && Projectile.IsOwnedByLocalPlayer() && !canHeal
                && Projectile.Center.Distance(player.Center) < Projectile.width) {
                int num = Main.rand.Next(10, 15);
                player.Entropy().TryHealMeWithCd(num, 5);
                SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact);
                canHeal = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player player = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 320);
            target.AddBuff(ModContent.BuffType<LifeOppress>(), 320);
            player.ApplyDamageToNPC(target, player.GetShootState().WeaponDamage / 3, 0f, 0
                , false, DamageClass.Default, true);
            float thirdDustScale = Main.rand.NextFloat(2, 4);
            Vector2 dustRotation = (target.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * target.velocity.Length() / 6;
            _ = SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int j = 0; j < 60; j++) {
                thirdDustScale = Main.rand.NextFloat(2, 4);
                bool noGvk = true;
                int dustId = DustID.InfernoFork;
                if (Main.rand.NextBool(2)) {
                    noGvk = false;
                    dustId = DustID.FireworkFountain_Red;
                    thirdDustScale /= 6f;
                }
                int contactDust2 = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width
                    , target.height, dustId, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[contactDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi)
                    .RotatedBy(target.velocity.ToRotation()) * target.width / 3f * Main.rand.NextFloat();
                dust.noGravity = noGvk;
                dust.velocity.Y -= 3f;
                dust.velocity *= 1.5f;
                dust.velocity += dustVelocity * (0.6f + 13.6f * Main.rand.NextFloat());
            }
            Projectile.Explode(360);
            if (Projectile.ai[0] == 0 && !spwanProj && player.ownedProjectileCounts[Type] < 136) {
                Vector2 ver = player.Center.To(Projectile.Center).UnitVector() * -18;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, ver.RotatedByRandom(0.22f)
                    , Type, Projectile.damage / 2, Projectile.knockBack / 2, Projectile.owner, 1);
                spwanProj = true;
            }
        }

        public override void OnKill(int timeLeft) {
            Projectile target = Projectile;
            float thirdDustScale = Main.rand.NextFloat(2, 4);
            Vector2 dustRotation = (target.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * target.velocity.Length() / 6;
            _ = SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int j = 0; j < 60; j++) {
                thirdDustScale = Main.rand.NextFloat(2, 4);
                bool noGvk = true;
                int dustId = DustID.InfernoFork;
                if (Main.rand.NextBool(2)) {
                    thirdDustScale /= 6f;
                    noGvk = false;
                    dustId = DustID.FireworkFountain_Red;
                }
                int contactDust2 = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width
                    , target.height, dustId, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[contactDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi)
                    .RotatedBy(target.velocity.ToRotation()) * target.width / 3f;
                dust.noGravity = noGvk;
                dust.velocity.Y -= 3f;
                dust.velocity *= 1.5f;
                dust.velocity += dustVelocity * (0.6f + 13.6f * Main.rand.NextFloat());
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = texture.Size() / 2;
            for (int k = 0; k < Projectile.oldPos.Length; k++) {
                Vector2 offsetPos = Projectile.oldPos[k].To(Projectile.position);
                Vector2 drawPos = Projectile.Center - Main.screenPosition - offsetPos;
                Color color = Projectile.GetAlpha(Color.Pink) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }
            if (Projectile.ai[0] == 0) {
                VaultUtils.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Red, Projectile.rotation, drawOrigin, Projectile.scale, 0);
            }
            else {
                VaultUtils.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Gold, Projectile.rotation, drawOrigin, Projectile.scale * 1.05f, 0);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor)
                , Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
