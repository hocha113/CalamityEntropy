using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SunMoonGods.Sage
{
    /// <summary>
    /// 星魂,弹体用聚星之灵的贴图;沿出生时的航向前进,可以带左右摆动
    /// 轨迹由出生点、航向和存活帧解析算出,各端一致
    /// ai[0] 摆幅(带符号,0 为直飞),ai[1] 寿命覆盖(0 用默认),localAI[0] 存活帧
    /// </summary>
    public class SageStarWisp : GodHostileProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/WraithSoulEssence";
        public override GodKind Kind => GodKind.Moon;
        public override int DefaultTimeLeft => SageDirector.TorrentLife;

        private const float DrawScale = 0.62f;

        private Vector2 origin;
        private Vector2 heading;
        private float speed;
        private bool initialized;

        private float Age => Projectile.localAI[0];
        private float Amplitude => Projectile.ai[0];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (!initialized) {
                initialized = true;
                origin = Projectile.Center;
                speed = Projectile.velocity.Length();
                heading = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                //timeLeft 不过线,首帧各端自己定
                Projectile.timeLeft = Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : SageDirector.TorrentLife;
            }
            Projectile.localAI[0]++;
            float sway = Amplitude * MathF.Sin(MathHelper.TwoPi * Age / SageDirector.TorrentWeavePeriod);
            Projectile.Center = origin + heading * (speed * Age) + heading.RotatedBy(MathHelper.PiOver2) * sway;
            //尾焰朝后:贴图的须朝上,头朝下
            Projectile.rotation = heading.ToRotation() - MathHelper.PiOver2;
            Projectile.Opacity = MathHelper.Clamp(Age / 8f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 14f, 0f, 1f);
            if (++Projectile.frameCounter >= 5) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Lighting.AddLight(Projectile.Center, 0.35f, 0.45f, 0.9f);
            if (!Main.dedServ && Main.rand.NextBool(7)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.MagicMirror, -heading * 1.5f, 100, default, Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override void OnKill(int timeLeft) {
            if (timeLeft > 0) {
                SageVfx.Stardust(Projectile.Center, 5, 1.5f, 4f);
            }
        }

        /// <summary>残影和弹体同一张图,残影宽度不低于弹体的一半</summary>
        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0.01f || !initialized) {
                return false;
            }
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 pivot = frame.Size() / 2f;
            float scale = Projectile.scale * DrawScale;

            GodDraw.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, GodDraw.Add(SageVfx.StarBlue, 0.45f * t * opacity),
                    Projectile.rotation, pivot, scale * (0.55f + 0.4f * t), SpriteEffects.None, 0f);
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(SageVfx.StarBlue, 0.5f * opacity), 0f, 0.36f * Projectile.scale);
            GodDraw.End();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * opacity, Projectile.rotation, pivot, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}