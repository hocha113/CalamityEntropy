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
    /// 追踪导弹:径向飞出约 15 格后重新瞄准玩家并加速扑去。命中 312,无减益。ai[0] 为目标玩家索引
    /// </summary>
    public class VDHomingMissile : VDHostileProjectile
    {
        public const float RetargetDistance = 15f * 16f;
        public const float MaxSpeed = 22f;
        public static readonly Color GlowColor = new Color(210, 90, 255);

        public override int DefaultTimeLeft => 240;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
        }

        public override void AI()
        {
            //localAI[0] 累计飞行距离,全端由速度积分得到
            Projectile.localAI[0] += Projectile.velocity.Length();
            if (Projectile.localAI[0] > RetargetDistance)
            {
                Player target = TargetPlayer(0);
                if (target != null)
                {
                    float cur = Projectile.velocity.ToRotation();
                    float want = (target.Center - Projectile.Center).ToRotation();
                    float diff = MathHelper.WrapAngle(want - cur);
                    float turn = MathHelper.Clamp(diff, -0.105f, 0.105f);
                    float speed = Math.Min(Projectile.velocity.Length() * 1.03f + 0.1f, MaxSpeed);
                    Projectile.velocity = (cur + turn).ToRotationVector2() * speed;
                }
                else
                {
                    Projectile.velocity *= 1.01f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.35f);

            if (Main.dedServ)
            {
                return;
            }
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Dust d = Dust.NewDustPerfect(Projectile.Center + back * 16f, DustID.Smoke, back * Main.rand.NextFloat(1f, 3f), 120, default, Main.rand.NextFloat(1f, 1.6f));
            d.noGravity = true;
            if (Main.rand.NextBool(2))
            {
                Vector2 v = back * Main.rand.NextFloat(2f, 5f) + CEUtils.randomPointInCircle(1f);
                var s = PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center + back * 14f, v, GlowColor, Main.rand.NextFloat(0.4f, 0.8f))
                    .Configure(0.9f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 16);
                s.grav = false;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            base.OnHitPlayer(target, info);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            CEUtils.PlaySound("VoidBomb", Main.rand.NextFloat(1.1f, 1.3f), Projectile.Center, 6, 0.45f);
            PRTLoader.NewParticle<PRT_EXPLOSION>(Projectile.Center, Vector2.Zero, Color.White, 0.35f)
                .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 24);
            for (int i = 0; i < 10; i++)
            {
                Vector2 v = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, v, GlowColor, Main.rand.NextFloat(0.5f, 1f))
                    .Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 22);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.UseAdditive();
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                float a = (1f - i / (float)Projectile.oldPos.Length) * 0.3f;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, pos, null, GlowColor * a, Projectile.oldRot[i], origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Texture2D glow = CEUtils.getExtraTex("Glow");
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Main.spriteBatch.Draw(glow, drawPos + back * 18f, null, new Color(255, 140, 255) * 0.8f, 0f, glow.Size() / 2f, 0.14f, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
