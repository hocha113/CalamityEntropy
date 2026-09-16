using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 虚空核弹:缓慢追踪玩家,3 秒引信后在半径 50 格内爆炸(伤害由 VDNukeExplosion 承担,本体不撞人)。
    /// 全程绘制爆炸范围,最后 1 秒加速闪烁。ai[0] 目标玩家索引,ai[1] 爆炸半径(像素)
    /// </summary>
    public class VDVoidNuke : VDHostileProjectile
    {
        public const int FuseTime = 180;
        public const float DefaultRadius = 50f * 16f;
        public static readonly Color GlowColor = new Color(200, 70, 255);

        public float Radius => Projectile.ai[1] > 0 ? Projectile.ai[1] : DefaultRadius;
        public override int DefaultTimeLeft => FuseTime;

        public override void SetExtraDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override void AI()
        {
            Player target = TargetPlayer(0);
            if (target != null)
            {
                float cur = Projectile.velocity.ToRotation();
                float want = (target.Center - Projectile.Center).ToRotation();
                float diff = MathHelper.WrapAngle(want - cur);
                float turn = MathHelper.Clamp(diff, -0.021f, 0.021f);
                Projectile.velocity = (cur + turn).ToRotationVector2() * Math.Max(Projectile.velocity.Length(), 6f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.6f);

            if (Main.dedServ)
            {
                return;
            }
            //最后 60 帧闪烁加快,并给一声警报
            if (Projectile.timeLeft == 60 || Projectile.timeLeft == 30 || Projectile.timeLeft == 12)
            {
                CEUtils.PlaySound("VoidAnticipation", 1.4f, Projectile.Center, 4, 0.7f);
            }
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Dust d = Dust.NewDustPerfect(Projectile.Center + back * 24f, DustID.Smoke, back * Main.rand.NextFloat(1f, 2f), 140, default, Main.rand.NextFloat(1.2f, 1.8f));
            d.noGravity = true;
            var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center + back * 20f, back * Main.rand.NextFloat(1f, 3f), Color.White, 1.2f);
            p.Opacity = 0.7f;
            p.ad = 0.035f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;

        public override void OnKill(int timeLeft)
        {
            //只有引信走完才爆;阶段切换的清场 Kill 不引爆
            if (IsServer && timeLeft <= 1)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VDNukeExplosion>(), Projectile.damage, 0f, Main.myPlayer, Radius);
            }
        }

        /// <summary>闪烁强度:0~1,随引信剩余时间越短闪得越快</summary>
        private float Blink()
        {
            float t = Projectile.timeLeft;
            float freq = t > 60 ? 4f : t > 30 ? 9f : 18f;
            return 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * freq * MathHelper.TwoPi);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float blink = Blink();
            float radius = Radius;

            //爆炸范围:淡紫填充 + 外圈,最后一秒闪烁
            Main.spriteBatch.UseAdditive();
            Texture2D circle = CEUtils.getExtraTex("Circle");
            float fill = 0.06f + 0.06f * blink * (Projectile.timeLeft <= 60 ? 1f : 0.3f);
            Main.spriteBatch.Draw(circle, drawPos, null, GlowColor * fill, 0f, circle.Size() / 2f, radius * 2f / 300f, SpriteEffects.None, 0f);
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            float ringAlpha = 0.35f + 0.45f * blink;
            Main.spriteBatch.Draw(ring, drawPos, null, new Color(230, 150, 255) * ringAlpha, 0f, ring.Size() / 2f, radius * 2f / 140f, SpriteEffects.None, 0f);
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Main.spriteBatch.Draw(glow, drawPos, null, GlowColor * (0.5f + 0.5f * blink), 0f, glow.Size() / 2f, 0.3f + 0.1f * blink, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();

            Main.spriteBatch.Draw(tex, drawPos, null, Color.Lerp(lightColor, Color.White, blink * 0.6f), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>核弹爆炸:半径 ai[0] 的圆形判定,前 6 帧有伤害,命中 480 + 虚空之火 3 秒</summary>
    public class VDNukeExplosion : VDHostileProjectile
    {
        public const int Lifetime = 26;
        public const int DamageFrames = 6;
        public static readonly Color GlowColor = new Color(200, 70, 255);

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";
        public override int DebuffType => ModContent.BuffType<Buffs.VoidFire>();
        public override int DefaultTimeLeft => Lifetime;
        public float Radius => Projectile.ai[0];

        public override void SetExtraDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (!Main.dedServ)
                {
                    CEUtils.PlaySound("VoidBomb", 0.8f, Projectile.Center, 3, 1.2f);
                    CEUtils.SetShake(Projectile.Center, 14f, 3000f);
                    PRTLoader.NewParticle<PRT_EXPLOSION>(Projectile.Center, Vector2.Zero, Color.White, Radius / 260f)
                        .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 40);
                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6f, 26f);
                        PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.8f, 1.6f))
                            .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 40);
                    }
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4f, 14f);
                        var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, v, Color.White, Main.rand.NextFloat(1.2f, 2.2f));
                        p.Opacity = 0.9f;
                        p.ad = 0.02f;
                    }
                }
            }
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 2f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.timeLeft < Lifetime - DamageFrames)
            {
                return false;
            }
            Vector2 closest = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(closest, Projectile.Center) <= Radius * Radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.UseAdditive();
            Texture2D circle = CEUtils.getExtraTex("Circle");
            float scale = Radius * 2f / 300f * (0.6f + 0.4f * progress);
            Main.spriteBatch.Draw(circle, drawPos, null, GlowColor * (0.55f * (1f - progress)), 0f, circle.Size() / 2f, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(circle, drawPos, null, Color.White * (0.35f * (1f - progress)), 0f, circle.Size() / 2f, scale * 0.6f * (1f - progress * 0.5f), SpriteEffects.None, 0f);
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Main.spriteBatch.Draw(ring, drawPos, null, new Color(240, 170, 255) * (1f - progress), 0f, ring.Size() / 2f, Radius * 2f / 140f * (0.8f + 0.4f * progress), SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
