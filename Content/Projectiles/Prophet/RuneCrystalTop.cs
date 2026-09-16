using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Prophet
{
    public class RuneCrystalTop : ModProjectile
    {
        //水晶本体贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Prophet/RuneCrystal")]
        internal static Asset<Texture2D> CrystalTex;
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }
        public override void SetDefaults() {
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = Projectile.height = 20;
            Projectile.light = 1;
            Projectile.timeLeft = 100 + 64;
        }
        public List<Vector2> segs = new List<Vector2>();
        public Vector2 orgPos = Vector2.Zero;
        public override void AI() {
            if (orgPos == Vector2.Zero) {
                orgPos = Projectile.Center;
            }
            Projectile.ai[0]++;
            if (Projectile.ai[0] < 64) {
                segs.Add(Projectile.Center);
                Projectile.Center += Projectile.velocity.SafeNormalize(Vector2.One) * 16;
                if (Main.rand.NextBool(3))
                    CEUtils.PlaySound("crystalsound" + Main.rand.Next(1, 4), 2.5f + Main.rand.NextFloat(-0.3f, 0.3f), Projectile.Center, 64, 0.6f);
            }
        }
        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 360; i++) {
                Vector2 pos = Vector2.Lerp(orgPos, Projectile.Center, Main.rand.NextFloat());
                Vector2 vel = CEUtils.randomPointInCircle(10);
                var d = Dust.NewDustDirect(pos, 0, 0, DustID.MagicMirror);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1.4f, 2.6f);
                d.velocity = vel;
                d.position = pos + vel;
                d.frame.Y = 10 * (Main.rand.NextBool() ? 0 : 2);
            }
            CEUtils.PlaySound("CrystalBreak", 1.8f + Main.rand.NextFloat(-0.3f, 0.3f), orgPos);
            CEUtils.PlaySound("CrystalBreak", 1.8f + Main.rand.NextFloat(-0.3f, 0.3f), Projectile.Center);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 8 * 60);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (orgPos == Vector2.Zero) {
                return false;
            }
            return CEUtils.LineThroughRect(orgPos, Projectile.Center, targetHitbox, (int)(20 * Projectile.scale));
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            lightColor = Color.White;
            Texture2D t1 = CrystalTex.Value;
            Texture2D t2 = Projectile.GetTexture();
            float shake = (Projectile.timeLeft < 60) ? ((60 - Projectile.timeLeft) / 60f) : 0;
            List<Vector2> offset = new();
            Vector2 offset_ = CEUtils.randomPointInCircle(shake * 6);
            float light = shake;
            int c;
            Main.spriteBatch.UseAdditive();
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.Pi / 3f) {
                c = 0;
                foreach (var p in segs) {
                    offset.Add(CEUtils.randomPointInCircle(shake * 6));
                    Main.EntitySpriteDraw(t1, p - Main.screenPosition + offset[c] + i.ToRotationVector2() * 4, null, lightColor * light, Projectile.velocity.ToRotation(), t1.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
                    c++;
                }
                Main.EntitySpriteDraw(t2, Projectile.Center - Main.screenPosition + offset_ + i.ToRotationVector2() * 4, null, lightColor * light, Projectile.velocity.ToRotation(), t2.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            c = 0;
            foreach (var p in segs) {
                Main.EntitySpriteDraw(t1, p - Main.screenPosition + offset[c], null, lightColor, Projectile.velocity.ToRotation(), t1.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
                c++;
            }
            Main.EntitySpriteDraw(t2, Projectile.Center - Main.screenPosition + offset_, null, lightColor, Projectile.velocity.ToRotation(), t2.Size() * 0.5f, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }

}