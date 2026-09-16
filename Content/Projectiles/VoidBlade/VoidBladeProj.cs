using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidBlade
{
    public class VoidBladeProj : ModProjectile
    {
        //刀光帧动画(f0~f12,按 ai[1] 取帧),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/VoidBlade/f", 0, 13, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        SoundStyle hitSound = new("CalamityEntropy/Assets/Sounds/vb_hit");
        SoundStyle hs = new("CalamityEntropy/Assets/Sounds/vbuse");
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 6000;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1.2f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        public int damageo = -1;
        public override void AI() {
            if (Projectile.localAI[0]++ == 0) {
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            hs.Volume = 0.6f * CEUtils.WeapSound;
            hitSound.Volume = 0.2f * CEUtils.WeapSound;
            if (damageo == -1) {
                damageo = Projectile.damage;
            }
            soundCd--;
            Player player = Main.player[Projectile.owner];
            Projectile.ai[0]++;
            if (Projectile.ai[0] % 2 == 0) {
                Projectile.ai[1]++;
                if (Projectile.ai[1] > 12) {
                    Projectile.ai[1] = 0;
                }
            }
            if (player.channel) {
                Projectile.timeLeft = 20;
            }
            else {
                if (Projectile.ai[1] == 4 || Projectile.ai[1] == 12) {
                    Projectile.Kill();
                }
            }
            if (Projectile.ai[1] == 0) {
                hitSound.Pitch = 1.3f;
                hs.Pitch = 1.6f;
            }
            if (Projectile.ai[1] == 6) {
                hitSound.Pitch = 1.1f;
                hs.Pitch = 1.4f;
            }
            if (Projectile.ai[1] > 6) {
                Projectile.damage = (int)(damageo * 1.5f);
            }
            else {
                Projectile.damage = damageo;
            }
            if (Projectile.ai[1] == 7 || Projectile.ai[1] == 1) {
                if (Projectile.ai[0] % 2 == 0) {
                    SoundEngine.PlaySound(hs, Projectile.Center);
                }
            }
            Vector2 playerRotatedPoint = player.RotatedRelativePoint(player.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner) {
                if (Projectile.ai[0] % 2 == 0) {
                    if (Projectile.ai[1] == 6 || Projectile.ai[1] == 0)
                        HandleChannelMovement(player, playerRotatedPoint);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center = player.Center + Projectile.rotation.ToRotationVector2() * 64 * Projectile.scale;
            if (Projectile.velocity.X > 0) {
                player.direction = 1;
            }
            else {
                player.direction = 0;
            }
            player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 1f);
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
            if (Projectile.ai[0] % 2 == 0) {
                Player player = Main.player[Projectile.owner];
                if (Projectile.ai[1] == 1) {
                    int bsize = ((int)(230 * Projectile.scale));
                    Vector2 c = player.Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
                    if (Projectile.Entropy().OnProj != -1) {
                        c = Projectile.Entropy().OnProj.ToProj().Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
                    }
                    return new Rectangle((int)c.X - bsize / 2, (int)c.Y - bsize / 2, bsize, bsize).Intersects(targetHitbox);
                }
                if (Projectile.ai[1] == 7) {
                    int bsize = ((int)(280 * Projectile.scale));
                    Vector2 c = player.Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
                    if (Projectile.Entropy().OnProj != -1) {
                        c = Projectile.Entropy().OnProj.ToProj().Center + Projectile.rotation.ToRotationVector2() * bsize / 2;
                    }
                    return new Rectangle((int)c.X - bsize / 2, (int)c.Y - bsize / 2, bsize, bsize).Intersects(targetHitbox);
                }
            }
            return false;
        }
        int soundCd = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (soundCd <= 0) {
                soundCd = 5;
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/VividClarityBeamAppear") { MaxInstances = 12, Volume = 0.88f * CEUtils.WeapSound, PitchRange = (0.1f, 0.6f) }, target.Center);
                CEUtils.PlaySound("WScytheHit", Main.rand.NextFloat(1, 1.4f), target.Center, 12, 0.45f);
            }
            for (int i = 0; i < 18; i++) {
                Color clr = Projectile.ai[1] >= 7 ? new Color(177, 164, 218) : new Color(186, 80, 212);
                //GlowSparkCal Configure(gravity,lifetime,squash,quickShrink,glow)对齐Calamity构造
                PRTLoader.NewParticle<PRT_GlowSparkCal>(CEUtils.randomPoint(target.Hitbox), Projectile.velocity.normalize().RotatedByRandom(0.02f) * Main.rand.NextFloat(6, 56), clr, Main.rand.NextFloat(0.3f, 1) * 0.08f).Configure(false, 16, new Vector2(0.26f, 1), false, false);
                if (Main.rand.NextBool(4)) {
                    var sd = PRTLoader.NewParticle<PRT_ShadeDashParticle>(CEUtils.randomPoint(target.Hitbox), Projectile.velocity.normalize().RotatedByRandom(0.3f) * Main.rand.NextFloat(16, 46), Color.White);
                    sd.c1 = clr;
                    sd.c2 = clr * 4;
                    sd.TL = 14;
                    sd.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0, 9);
                }
            }

            for (int i = 0; i < 9; i++) {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, Projectile.velocity.normalize().RotatedByRandom(0.6f) * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue, 0.05f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(4f, 0.5f), true);
            }
            for (int i = 0; i < 32; i++) {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(1.6f, 3.4f);
                dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f, 1) * 46;
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue;
                dust.fadeIn = 2f;
            }
        }

        public override bool PreDraw(ref Color dc) {
            Texture2D tx = Frames[(int)Projectile.ai[1]];
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(200, 200), new Vector2(Projectile.scale, Projectile.scale), SpriteEffects.None, 0);
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
    }


}
