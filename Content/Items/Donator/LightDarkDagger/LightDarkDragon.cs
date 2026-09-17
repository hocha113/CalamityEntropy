using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.LightDarkDagger
{
    /// <summary>
    /// 光暗之龙:收藏光暗龙匕时跟随玩家的朦胧幻影,不造成伤害。
    /// ai[0] = 清晰度(暴击层数占比,0~1),ai[1] = 1 时播放攻击帧;两者由所有者写入并同步。
    /// 所有者端在请求消失时主动 Kill,其余端只管跟随与绘制。
    /// </summary>
    public class LightDarkDragon : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/LightDarkDagger/LightDarkDragonIdle";

        public const int Frames = 5;
        public const int FrameTicks = 7;
        //最朦胧时的不透明度,层数满时到 1
        public const float MinOpacity = 0.16f;
        public const float FollowLerp = 0.09f;
        public static readonly Vector2 HoverOffset = new Vector2(-64f, -54f);

        private float bob;
        private int frameTimer;
        private float shownOpacity;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
        }

        public override void SetDefaults() {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 5;
        }

        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            Player owner = Projectile.GetOwner();
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            LDDaggerPlayer mp = owner.GetModPlayer<LDDaggerPlayer>();
            if (Main.myPlayer == Projectile.owner) {
                if (!mp.CompanionRequested) {
                    Projectile.Kill();
                    return;
                }
                float intensity = mp.CritStack / (float)LightDarkDragonDagger.MaxCritStack;
                float attacking = mp.AttackAnimTime > 0 && owner.HeldItem.ModItem is LightDarkDragonDagger ? 1f : 0f;
                if (Math.Abs(intensity - Projectile.ai[0]) > 0.01f || attacking != Projectile.ai[1]) {
                    Projectile.ai[0] = intensity;
                    Projectile.ai[1] = attacking;
                    Projectile.netUpdate = true;
                }
            }
            Projectile.timeLeft = 5;

            bob += 0.045f;
            Vector2 target = owner.MountedCenter + new Vector2(HoverOffset.X * owner.direction, HoverOffset.Y + (float)Math.Sin(bob) * 7f);
            Vector2 delta = target - Projectile.Center;
            if (delta.Length() > 900f) {
                Projectile.Center = target;
                Projectile.velocity = Vector2.Zero;
            }
            else {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, delta * FollowLerp, 0.5f);
            }
            Projectile.spriteDirection = owner.direction;
            Projectile.rotation = MathHelper.Clamp(Projectile.velocity.X * 0.02f, -0.25f, 0.25f);

            float targetOpacity = MinOpacity + (1f - MinOpacity) * Projectile.ai[0];
            shownOpacity = MathHelper.Lerp(shownOpacity, targetOpacity, 0.08f);

            int ticks = Projectile.ai[1] > 0.5f ? FrameTicks - 2 : FrameTicks;
            if (++frameTimer >= ticks) {
                frameTimer = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }

            if (Projectile.ai[0] > 0.05f) {
                Lighting.AddLight(Projectile.Center, new Vector3(0.6f, 0.45f, 0.8f) * Projectile.ai[0] * 0.6f);
                if (Main.rand.NextFloat() < Projectile.ai[0] * 0.5f) {
                    bool light = Main.rand.NextBool();
                    Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(34f);
                    PRTLoader.NewParticle<PRT_Light>(pos, new Vector2(0, -0.6f) + CEUtils.randomPointInCircle(0.4f), LDPalette.Tone(light) * shownOpacity, Main.rand.NextFloat(0.2f, 0.4f)).Configure(0.6f * shownOpacity, lifetime: 24);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.ai[1] > 0.5f ? CEUtils.RequestTex("CalamityEntropy/Content/Items/Donator/LightDarkDagger/LightDarkDragonAttack") : Projectile.GetTexture();
            int frameHeight = tex.Height / Frames;
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            //贴图本体朝左,玩家朝右时水平翻转
            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float a = shownOpacity;
            float glowA = Projectile.ai[0] * a;

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            if (glowA > 0.02f) {
                //层数越高,身后的金紫双色光晕越亮
                Texture2D glow = CEExtraAssets.Glow2;
                Main.EntitySpriteDraw(glow, pos + new Vector2(-14f * Projectile.spriteDirection, -10f), null, LDPalette.LightMain * glowA * 0.45f, 0f, glow.Size() / 2f, 0.5f, SpriteEffects.None);
                Main.EntitySpriteDraw(glow, pos + new Vector2(14f * Projectile.spriteDirection, 12f), null, LDPalette.DarkMain * glowA * 0.5f, 0f, glow.Size() / 2f, 0.55f, SpriteEffects.None);
            }
            //朦胧幻影:先铺一层加法幽光,再叠半透明本体
            Main.EntitySpriteDraw(tex, pos, frame, new Color(120, 90, 200) * a * 0.5f, Projectile.rotation, origin, Projectile.scale * 1.03f, effects);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, pos, frame, Color.White * a, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }
}
