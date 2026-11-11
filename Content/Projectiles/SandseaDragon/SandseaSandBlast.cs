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
    /// 沙弹,两种飞法共用一个弹体
    /// ai[0] = 0 砸地扇面:竖直速度在 ai[1] 帧内线性减到零,横向速度同步减到两成,之后缓慢下落,下落时碰到物块才消失
    /// ai[0] = 1 龙卷合拢的一圈:匀速直飞,出生 6 帧后碰到物块消失
    /// 初速只在生成包里同步一次,各端在第一帧记下,之后的轨迹按存活帧自算
    /// </summary>
    public class SandseaSandBlast : SandseaHostileProjectile
    {
        public override int DefaultTimeLeft => SandseaDirector.SandBlastLife;

        private ref float Age => ref Projectile.localAI[0];
        private bool Straight => Projectile.ai[0] == 1f;

        private Vector2 launch;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
        }

        public override void AI() {
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                launch = Projectile.velocity;
            }
            Age++;

            bool falling;
            if (Straight) {
                falling = Age > 6f;
            }
            else {
                float rise = Math.Max(1f, Projectile.ai[1]);
                falling = Age > rise;
                if (!falling) {
                    float left = 1f - Age / rise;
                    Projectile.velocity = new Vector2(launch.X * (0.2f + 0.8f * left), launch.Y * left);
                }
                else {
                    Projectile.velocity.X *= 0.97f;
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + SandseaDirector.SandBlastFallAccel, SandseaDirector.SandBlastFallMax);
                }
            }

            Projectile.rotation += 0.12f * (launch.X >= 0f ? 1f : -1f);
            if (!Main.dedServ && Main.rand.NextBool(5)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.Sand, -Projectile.velocity * 0.2f, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }

            //与原版的物块碰撞一样各端自己判,物块数据各端一致
            if (falling && Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Sand, Main.rand.NextVector2Circular(2.5f, 2.5f), 0, default, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            //出生淡入,寿命将尽淡出;弹体不吃环境光,暗处也要看得清
            float fade = MathHelper.Clamp(Age / 6f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            Color body = Color.Lerp(lightColor, Color.White, 0.7f) * fade;

            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float k = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, body * (0.35f * k), Projectile.rotation, origin, 0.55f + 0.45f * k, SpriteEffects.None, 0);
            }

            Texture2D glow = CEUtils.getExtraTex("Glow");
            Color halo = SandseaVfx.Sand * (0.3f * fade);
            halo.A = 0;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, halo, 0f, glow.Size() / 2f, 0.2f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, body, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}
