using CalamityEntropy.Assets.Register;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.VoidBlade
{
    public class VoidBladeHit : ModProjectile
    {
        //原先是实例字段初始化器逐实例请求,改为加载期就位的静态字段
        [VaultLoaden("CalamityEntropy/Content/Projectiles/VoidBlade/VoidBladeHit")]
        internal static Asset<Texture2D> HitTex;
        public List<Vector2> points1 = new List<Vector2>();
        public List<Vector2> points2 = new List<Vector2>();
        private float r1;
        private float r2;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 6000;

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1.2f;
            Projectile.damage = 0;
            Projectile.timeLeft = 20;
            var r = Main.rand;
            Projectile.scale = (float)r.Next(80, 120) / 100f;
            Projectile.rotation = (float)(r.NextDouble() * Math.PI * 2);

        }

        public override void AI() {
            if (Projectile.ai[0] == 0) {
                var var = Main.rand;
                r1 = (Projectile.ai[1] + ((float)(Main.rand.Next(-80, 80)) / 100));
                r2 = (Projectile.ai[1] + ((float)(Main.rand.Next(-80, 80)) / 100));
                for (int i = 0; i < 20; i++) {
                    points1.Add(r1.ToRotationVector2() * i * 14);
                    points2.Add(r2.ToRotationVector2() * i * 14);
                }
            }
            Projectile.ai[0] += 1;
            for (int i = 0; i < points1.Count; i++) {
                points1[i] *= 0.9f;
                points2[i] *= 0.9f;
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 1f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool PreDraw(ref Color dc) {
            Texture2D vbh = HitTex.Value;
            Texture2D tx = CEExtraAssets.BasicCircle;
            float s = 1f;
            for (int i = 0; i < points1.Count; i++) {
                float ds = s * 0.3f;
                Main.spriteBatch.Draw(tx, Projectile.Center + points1[i] - Main.screenPosition, null, new Color(0, 0, 0), r1, new Vector2(tx.Width / 2, tx.Height / 2), new Vector2(ds, ds * ((float)Projectile.timeLeft / 20)), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tx, Projectile.Center + points2[i] - Main.screenPosition, null, new Color(0, 0, 0), r2, new Vector2(tx.Width / 2, tx.Height / 2), new Vector2(ds, ds * ((float)Projectile.timeLeft / 20)), SpriteEffects.None, 0);
                s -= 0.05f;
            }
            Main.spriteBatch.Draw(vbh, Projectile.Center - Main.screenPosition, null, new Color(53, 62, 174) * ((float)Projectile.timeLeft / 20), Projectile.rotation, new Vector2(50, 50), new Vector2(3 * Projectile.scale * (1 - ((float)Projectile.timeLeft / 20)), 1 * Projectile.scale), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(vbh, Projectile.Center - Main.screenPosition, null, new Color(33, 32, 144) * ((float)Projectile.timeLeft / 20), Projectile.rotation, new Vector2(50, 50), new Vector2(5 * Projectile.scale * (1 - ((float)Projectile.timeLeft / 20)), 0.3f * Projectile.scale), SpriteEffects.None, 0);
            return false;
        }
    }


}
