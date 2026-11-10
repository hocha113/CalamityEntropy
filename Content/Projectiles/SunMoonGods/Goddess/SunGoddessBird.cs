using CalamityEntropy.Content.Items.Armor.Sunblaze;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Goddess
{
    /// <summary>
    /// 太阳女神的太阳神鸟:先散开减速,再在追踪窗内限速转向,过窗直飞
    /// ai[0] 目标玩家下标,ai[1] 巡航速度,localAI[0] 存活帧
    /// </summary>
    public class SunGoddessBird : GodHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Armor/Sunblaze/Sunbird";
        public override GodKind Kind => GodKind.Sun;
        public override int DebuffType => ModContent.BuffType<SunblazeBurn>();
        public override int DebuffTime => SunDirector.BurnFrames;
        public override int DefaultTimeLeft => SunDirector.BirdLife;

        private float Age => Projectile.localAI[0];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 8;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.scale = 0.85f;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            if (Age == 1f && !Main.dedServ) {
                SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 0.3f, PitchVariance = 0.25f, MaxInstances = 3 }, Projectile.Center);
            }
            if (++Projectile.frameCounter >= 4) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Projectile.Opacity = MathHelper.Clamp(Age / 8f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 15f, 0f, 1f);

            float cruise = Projectile.ai[1] > 0f ? Projectile.ai[1] : SunDirector.BirdCruiseSpeed(1);
            if (Age < SunDirector.BirdGlideFrames) {
                Projectile.velocity *= 0.955f;
            }
            else if (Age < SunDirector.BirdGlideFrames + SunDirector.BirdHomingFrames) {
                Player target = TargetPlayer();
                if (target != null) {
                    float speed = MathHelper.Lerp(Projectile.velocity.Length(), cruise, 0.08f);
                    float current = Projectile.velocity.ToRotation();
                    float want = (target.Center - Projectile.Center).ToRotation();
                    float turn = MathHelper.Clamp(MathHelper.WrapAngle(want - current), -SunDirector.BirdTurnRate, SunDirector.BirdTurnRate);
                    Projectile.velocity = (current + turn).ToRotationVector2() * speed;
                }
            }
            else if (Projectile.velocity.Length() < SunDirector.BirdMaxSpeed) {
                Projectile.velocity *= 1.012f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, 1f, 0.6f, 0.2f);
            if (!Main.dedServ && Main.rand.NextBool(3)) {
                Vector2 tail = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 14f + Main.rand.NextVector2Circular(8f, 8f);
                Dust d = Dust.NewDustPerfect(tail, DustID.SolarFlare, -Projectile.velocity * 0.15f, 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }

        private Player TargetPlayer() {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxPlayers) {
                return null;
            }
            Player player = Main.player[index];
            return player.active && !player.dead ? player : null;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 8; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.SolarFlare, Main.rand.NextVector2Circular(5f, 5f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }

        /// <summary>贴图鸟头朝右,往左飞时竖直翻转保持背朝上,残影用同一张图</summary>
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, tex.Width, frameHeight - 2);
            Vector2 origin = frame.Size() / 2f;
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float opacity = Projectile.Opacity;

            GodDraw.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, GodDraw.Add(SunVfx.Ember, 0.5f * t * opacity),
                    Projectile.oldRot[i], origin, Projectile.scale * (0.8f + 0.2f * t), effects, 0f);
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SunVfx.Ember, 0.55f * opacity), 0f, 0.42f);
            GodDraw.End();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * opacity,
                Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}