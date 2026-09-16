using CalamityEntropy.Content.NPCs.VoidDestroyer;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 轨道轰炸的落点标记(纯演出,无伤害):ai[0] 寿命。外环收缩、内十字旋转,末 12 帧由紫转白闪烁;
    /// 寿命结束即光柱砸落的那一帧,标记就是承诺
    /// </summary>
    public class VDTargetReticle : ModProjectile, IVoidDestroyerProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public int Life => (int)Math.Max(Projectile.ai[0], 10f);
        public int Age => Life - Projectile.timeLeft;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 60;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = Life;
                if (!Main.dedServ)
                {
                    CEUtils.PlaySound("vbapear", 1.4f, Projectile.Center, 6, 0.5f);
                }
            }
            float p = Age / (float)Life;
            Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * (0.3f + 0.6f * p));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float p = MathHelper.Clamp(Age / (float)Life, 0f, 1f);
            int left = Projectile.timeLeft;
            float flicker = left <= 12 ? 0.55f + 0.45f * (float)Math.Sin(Age * 1.5f) : 1f;
            Color c = Color.Lerp(VDVfx.VoidPurple, Color.White, MathF.Pow(p, 3f)) * flicker;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D ring = CEUtils.getExtraTex("BloomRing");
            Texture2D glow = CEUtils.getExtraTex("Glow");

            Main.spriteBatch.UseAdditive();
            //外环收缩到落点宽度
            float outer = MathHelper.Lerp(2.2f, 0.9f, VDVfx.EaseOut(p));
            Main.spriteBatch.Draw(ring, pos, null, c * 0.9f, Age * 0.03f, ring.Size() / 2f, outer, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, pos, null, c * 0.5f, -Age * 0.05f, ring.Size() / 2f, outer * 0.6f, SpriteEffects.None, 0f);
            //旋转十字
            float rot = Age * 0.06f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 d = (rot + MathHelper.PiOver2 * i).ToRotationVector2();
                CEUtils.drawLine(Projectile.Center + d * 20f, Projectile.Center + d * (60f + 40f * (1f - p)), c * 0.8f, 2f);
            }
            //竖直落点线:越接近落地越长越亮(光柱会沿它砸下来)
            Vector2 up = Projectile.Center + new Vector2(0, -900f * p);
            CEUtils.drawLineBetter(Projectile.Center, up, c * (0.35f * p), 4f + 8f * p);
            Main.spriteBatch.Draw(glow, pos, null, c * (0.5f + 0.5f * p), 0f, glow.Size() / 2f, 0.25f + 0.25f * p, SpriteEffects.None, 0f);
            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
