using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 三阶段的巨型龙卷风:立在竞技场中线的沙面上,高 80 格,底窄顶宽,顶宽 40 格,150 帧从地面卷到顶
    /// ai[0] 本体下标;localAI[0] 成形帧,localAI[1] 消散帧;位置每帧从本体的竞技场事实重算
    /// 判定按可见的漏斗轮廓逐高度算,只到已经卷起来的高度,半宽取可见半宽的 0.8
    /// </summary>
    public class SandseaMaelstrom : SandseaHostileProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SandnadoHostile;
        public override bool Persistent => true;

        /// <summary>判定半宽占可见半宽的比例</summary>
        private const float HitShrink = 0.8f;

        private ref float Form => ref Projectile.localAI[0];
        private ref float Fade => ref Projectile.localAI[1];
        private float Grown => MathHelper.Clamp(Form / SandseaDirector.MaelstromFormFrames, 0f, 1f);
        private float Opacity => 1f - Fade / SandseaDirector.MaelstromFadeFrames;
        private float FloorY => Projectile.Center.Y + SandseaDirector.MaelstromHeight / 2f;

        /// <summary>高度比例 t 处的可见半宽,绘制和判定共用</summary>
        public static float HalfWidthAt(float t)
            => MathHelper.Lerp(SandseaDirector.MaelstromBottomHalfWidth, SandseaDirector.MaelstromTopHalfWidth, MathF.Pow(MathHelper.Clamp(t, 0f, 1f), 0.8f));

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 100;
            Projectile.height = 100;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Projectile.velocity = Vector2.Zero;
            SandseaDragonHead head = OwnerHead(Projectile.ai[0]);
            if (head == null || head.Context.Dying) {
                Fade++;
                if (Fade >= SandseaDirector.MaelstromFadeFrames && Projectile.owner == Main.myPlayer) {
                    Projectile.Kill();
                }
                return;
            }
            Projectile.timeLeft = 600;
            if (Form < SandseaDirector.MaelstromFormFrames) {
                Form++;
            }
            SandseaStateContext ctx = head.Context;
            Projectile.Center = new Vector2(ctx.ArenaCenterX, ctx.ArenaFloorY - SandseaDirector.MaelstromHeight / 2f);

            if (Main.dedServ) {
                return;
            }
            if (Form == 1f) {
                SandseaVfx.Sound(SoundID.Item60 with { Pitch = -0.6f, Volume = 1f, MaxInstances = 2 }, Projectile.Center);
            }
            //沿漏斗外缘甩沙,只在已经卷起来的高度内
            for (int i = 0; i < 3; i++) {
                float t = Main.rand.NextFloat() * Grown;
                float half = HalfWidthAt(t);
                float side = Main.rand.NextBool() ? -1f : 1f;
                Vector2 pos = new Vector2(Projectile.Center.X + side * half * Main.rand.NextFloat(0.7f, 1.05f), FloorY - t * SandseaDirector.MaelstromHeight);
                Dust d = Dust.NewDustPerfect(pos, DustID.Sandnado, new Vector2(-side * Main.rand.NextFloat(2f, 6f), -Main.rand.NextFloat(2f, 7f)), 0, default, Main.rand.NextFloat(1f, 1.8f));
                d.noGravity = true;
            }
            //只在卷起的过程中震,成形后画面要静下来
            if (Form < SandseaDirector.MaelstromFormFrames && (int)Form % 6 == 0) {
                SandseaVfx.Shake(new Vector2(Projectile.Center.X, Main.LocalPlayer.Center.Y), 2.5f * Grown, 1800f);
            }
        }

        public override bool? CanDamage() => Fade <= 0f && Form >= 20f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float t = (FloorY - targetHitbox.Center.Y) / SandseaDirector.MaelstromHeight;
            if (t < -0.02f || t > Grown) {
                return false;
            }
            float half = HalfWidthAt(t) * HitShrink;
            return targetHitbox.Right > Projectile.Center.X - half && targetHitbox.Left < Projectile.Center.X + half;
        }

        public override bool PreDraw(ref Color lightColor) {
            float opacity = Opacity;
            if (opacity <= 0.01f) {
                return false;
            }
            SandseaTwisterDraw.DrawFunnel(new Vector2(Projectile.Center.X, FloorY), SandseaDirector.MaelstromHeight, HalfWidthAt, Grown, opacity);
            return false;
        }
    }
}
