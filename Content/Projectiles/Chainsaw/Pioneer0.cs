using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Chainsaw
{
    public class Pioneer0 : ModProjectile
    {
        //链锯帧动画数组(序号 0 起),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Chainsaw/Pioneer", 0, 2, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1.6f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 2;
            Projectile.ArmorPenetration = 1024;
        }
        int frame = 2;
        public override void OnSpawn(IEntitySource source) {
            Projectile.scale += Projectile.owner.ToPlayer().Entropy().WeaponBoost * 0.8f;
        }
        public override void AI() {
            Player player = Main.player[Projectile.owner];
            Projectile.ai[0]++;

            if (Projectile.ai[0] % 8 == 0) {
                SoundEngine.PlaySound(SoundID.Item22, Projectile.Center);
            }
            if (Projectile.ai[0] % 14 == 0) {
                if (Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity * 160, Projectile.velocity, ModContent.ProjectileType<Pioneer1>(), Projectile.damage / 2, 0, Projectile.owner);
                }
            }
            Vector2 playerRotatedPoint = player.RotatedRelativePoint(player.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner) {
                HandleChannelMovement(player, playerRotatedPoint);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.localAI[0]++ > 3 ? 0 : Projectile.GetOwner().direction * -MathHelper.ToRadians(4 - Projectile.localAI[0]) * 48);
            if (Projectile.localAI[0] == 4) {
                CEUtils.PlaySound("chainsawHit", 1, Projectile.Center, volume: 0.4f * CEUtils.WeapSound);
            }
            Projectile.Center = player.Center + player.gfxOffY * Vector2.UnitY + Projectile.rotation.ToRotationVector2() * 64 * Projectile.scale;
            if (Projectile.Entropy().OnProj != -1) {
                Projectile.Center = Projectile.Entropy().OnProj.ToProj().Center + Projectile.rotation.ToRotationVector2() * 64 * Projectile.scale;
            }
            if (Projectile.velocity.X > 0) {
                player.direction = 1;
            }
            else {
                player.direction = -1;
            }
            player.itemRotation = (Projectile.velocity * player.direction).ToRotation();
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            if (!player.channel) {
                Projectile.timeLeft = 1;
            }
            soundCd--;
        }

        public int soundCd = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player Owner = Main.player[Projectile.owner];
            SoundStyle hitSound = new SoundStyle("CalamityEntropy/Assets/Sounds/chainsaw", SoundType.Ambient) { Volume = 0.3f };
            if (soundCd <= 0) {
                SoundEngine.PlaySound(hitSound, Projectile.Center);
                soundCd = 16;
            }
            if (Projectile.owner == Main.myPlayer && ModContent.GetInstance<Config>().ChainsawShakeScreen) {
                CalamityEntropy.Instance.screenShakeAmp = 1;
            }
            float sparkCount = 6;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = new Vector2(16, 0).RotatedByRandom(3.14159f) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(7, 10);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = Color.DarkBlue;

                float velc = 2.6f;
                if (Main.rand.NextBool()) {
                    //PRT_AltSpark跟LineCal随机混用,旧Calamity spark/Lines二选一
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, sparkColor2, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                }
                else {
                    //LineCal Configure(false,lifetime)对齐Calamity LineParticle
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f, sparkVelocity2 * velc, Main.rand.NextBool() ? Color.LightBlue : Color.Purple, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                }
            }
        }
        public override bool? CanHitNPC(NPC target) {
            return Projectile.localAI[0] < 5 ? false : null;
        }
        public void HandleChannelMovement(Player player, Vector2 playerRotatedPoint) {
            float speed = 1f;
            Vector2 newVelocity = (Main.MouseWorld - playerRotatedPoint).SafeNormalize(Vector2.UnitX * player.direction) * speed;

            if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y) {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = newVelocity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Player player = Main.player[Projectile.owner];
            int bsize = ((int)(170 * Projectile.scale));
            Vector2 c = player.Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
            if (Projectile.Entropy().OnProj != -1) {
                c = Projectile.Entropy().OnProj.ToProj().Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
            }
            return new Rectangle((int)c.X - bsize / 2, (int)c.Y - bsize / 2, bsize, bsize).Intersects(targetHitbox);

        }
        public override bool PreDraw(ref Color dc) {
            Texture2D tx = Frames[((int)(Projectile.ai[0] / 4)) % frame];
            var rand = Main.rand;
            SpriteEffects ef = SpriteEffects.None;
            if (Projectile.velocity.X < 0) {
                ef = SpriteEffects.FlipVertically;
            }
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2(rand.Next(-2, 3), rand.Next(-2, 3)), null, Color.White, Projectile.rotation, tx.Size() / 2, new Vector2(Projectile.scale, Projectile.scale), ef, 0);
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
    }


}