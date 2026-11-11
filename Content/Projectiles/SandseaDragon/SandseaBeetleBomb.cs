using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 甲虫炸弹,弹体用原版甲虫炸弹的贴图:从本体嘴边抛向悬停点,40 帧到位,抛出满 180 帧爆炸
    /// ai[0] ai[1] 悬停点,ai[2] 爆炸半径;localAI[0] 存活帧
    /// 预警圈从抛出那一帧就画在悬停点上,半径与爆炸判定是同一个数,越接近爆炸闪得越快;只有爆炸的 8 帧有判定
    /// </summary>
    public class SandseaBeetleBomb : SandseaHostileProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ScarabBomb;
        public override int DefaultTimeLeft => SandseaDirector.BombFuse + SandseaDirector.BombBlastFrames;

        private static readonly Color WarnBlue = new(110, 200, 255);

        private ref float Age => ref Projectile.localAI[0];
        private Vector2 Dest => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        private float Radius => Projectile.ai[2];
        private bool Exploding => Age >= SandseaDirector.BombFuse;

        /// <summary>起点与闪烁相位,各端自己记</summary>
        private Vector2 start;
        private float blinkPhase;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                start = Projectile.Center;
                Projectile.timeLeft = SandseaDirector.BombFuse + SandseaDirector.BombBlastFrames;
                SandseaVfx.Sound(SoundID.Item10 with { Volume = 0.6f, PitchVariance = 0.2f, MaxInstances = 4 }, Projectile.Center);
            }
            Age++;
            Projectile.velocity = Vector2.Zero;

            float travel = MathHelper.Clamp(Age / SandseaDirector.BombTravelFrames, 0f, 1f);
            float ease = 1f - (1f - travel) * (1f - travel);
            //抛出时带一点上拱的弧,到位后轻轻浮动
            Vector2 arc = new Vector2(0f, -70f * MathF.Sin(travel * MathHelper.Pi));
            Vector2 bob = new Vector2(0f, 4f * MathF.Sin(Age * 0.08f)) * travel;
            Projectile.Center = Vector2.Lerp(start, Dest, ease) + arc + bob;
            Projectile.rotation = (1f - travel) * Age * 0.25f;

            float fuse = MathHelper.Clamp(Age / SandseaDirector.BombFuse, 0f, 1f);
            blinkPhase += 0.1f + 0.55f * fuse * fuse;
            if (++Projectile.frameCounter >= 6) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.45f, 0.6f);

            if (Age == SandseaDirector.BombFuse) {
                Explode();
            }
        }

        private void Explode() {
            if (Main.dedServ) {
                return;
            }
            Vector2 center = Dest;
            SandseaVfx.Sound(SoundID.Item14 with { Volume = 0.9f, PitchVariance = 0.15f, MaxInstances = 4 }, center);
            SandseaVfx.Shake(center, 5f, 1600f);
            //脉冲环贴图宽 156,终值停在爆炸半径上
            SandseaVfx.Ring(center, WarnBlue, Radius * 2f / 156f, 16);
            int count = (int)(Radius * 0.35f);
            for (int i = 0; i < count; i++) {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                float speed = Main.rand.NextFloat(3f, Radius / 9f);
                Dust d = Dust.NewDustPerfect(center + dir * Main.rand.NextFloat(0f, Radius * 0.3f), Main.rand.NextBool(3) ? DustID.Sandnado : DustID.BlueTorch, dir * speed, 0, default, Main.rand.NextFloat(1.2f, 2.2f));
                d.noGravity = true;
            }
        }

        public override bool? CanDamage() => Exploding ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircleHits(Dest, Radius, targetHitbox);

        public override bool PreDraw(ref Color lightColor) {
            Vector2 dest = Dest - Main.screenPosition;
            float appear = MathHelper.Clamp(Age / 12f, 0f, 1f);
            float blink = 0.5f + 0.5f * MathF.Sin(blinkPhase);

            if (Exploding) {
                //爆炸:圆面整个亮起再收掉,亮起的范围就是判定范围
                float burst = 1f - MathHelper.Clamp((Age - SandseaDirector.BombFuse) / SandseaDirector.BombBlastFrames, 0f, 1f);
                Texture2D blastDisc = CEUtils.getExtraTex("BasicCircle");
                Main.EntitySpriteDraw(blastDisc, dest, null, WarnBlue * (0.75f * burst), 0f, blastDisc.Size() / 2f, Radius * 2f / blastDisc.Width, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(blastDisc, dest, null, Color.White * (0.6f * burst), 0f, blastDisc.Size() / 2f, Radius * 1.2f / blastDisc.Width, SpriteEffects.None, 0);
                return false;
            }

            //预警圈:淡蓝圆面加一圈慢转的虚线边,用带 alpha 的实色画
            Texture2D disc = CEUtils.getExtraTex("BasicCircle");
            Main.EntitySpriteDraw(disc, dest, null, WarnBlue * ((0.1f + 0.14f * blink) * appear), 0f, disc.Size() / 2f, Radius * 2f / disc.Width, SpriteEffects.None, 0);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle one = new Rectangle(0, 0, 1, 1);
            const int Dashes = 40;
            float spin = Main.GlobalTimeWrappedHourly * 0.6f;
            //圈边是这招唯一的范围预告,亮天空下也要一眼看得见:先垫一道深蓝的粗边再画亮边
            Color edge = WarnBlue * ((0.6f + 0.4f * blink) * appear);
            Color under = new Color(24, 70, 140) * (0.5f * appear);
            float dashLength = MathHelper.TwoPi * Radius / Dashes * 0.6f;
            for (int i = 0; i < Dashes; i++) {
                float angle = spin + MathHelper.TwoPi * i / Dashes;
                Vector2 at = dest + angle.ToRotationVector2() * Radius;
                Main.EntitySpriteDraw(pixel, at, one, under, angle + MathHelper.PiOver2, new Vector2(0.5f, 0.5f), new Vector2(dashLength + 2f, 6f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(pixel, at, one, edge, angle + MathHelper.PiOver2, new Vector2(0.5f, 0.5f), new Vector2(dashLength, 4f), SpriteEffects.None, 0);
            }

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            //临爆时弹体跟着闪白
            Color body = Color.Lerp(Color.Lerp(lightColor, Color.White, 0.5f), Color.White, blink * MathHelper.Clamp(Age / SandseaDirector.BombFuse, 0f, 1f));
            //每帧上方 10 像素是引信,原点落在虫身中心
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, body * appear, Projectile.rotation, new Vector2(frame.Width / 2f, 26f), 1.25f, SpriteEffects.None, 0);
            return false;
        }
    }
}
