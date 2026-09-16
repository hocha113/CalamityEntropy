using CalamityEntropy.Content.Buffs.PortsDoT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.LuminarisShoots
{

    public class LuminarisSpikeBlue : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 20);
        }
        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 180;
        }
        public float counter = 0;
        public override void AI() {
            Projectile.velocity *= Projectile.ai[0] == 0 ? 1.04f : 0.987f;
            odp.Add(Projectile.Center);
            if (odp.Count > 16) {
                odp.RemoveAt(0);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Projectile.timeLeft < 30 ? Projectile.timeLeft / 30f : 1;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = this.getTextureGlow();
            List<Vector2> ptd = new List<Vector2>();
            for (int j = 1; j < odp.Count; j++) {
                for (float p = 0; p < 1; p += 0.2f) {
                    ptd.Add(Vector2.Lerp(odp[j - 1], odp[j], p));
                }
            }
            for (int i = 0; i < ptd.Count; i++) {
                Main.spriteBatch.Draw(glow, ptd[i] - Main.screenPosition, null, Color.White * Projectile.Opacity * ((i + 1f) / ptd.Count) * ((i + 1f) / ptd.Count), Projectile.rotation, glow.Size() / 2f, Projectile.scale * ((i + 1f) / ptd.Count) * ((i + 1f) / ptd.Count), SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (CEUtils.LineThroughRect(Projectile.Center, Projectile.Center - Projectile.velocity, targetHitbox, Projectile.width)) {
                return true;
            }
            return null;
        }
    }
    public class LuminarisSpikeRed : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 180;
        }
        public float counter = 0;
        public override void AI() {
            Projectile.velocity *= Projectile.ai[0] == 0 ? 1.04f : 0.987f;
            odp.Add(Projectile.Center);
            if (odp.Count > 16) {
                odp.RemoveAt(0);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Projectile.timeLeft < 30 ? Projectile.timeLeft / 30f : 1;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = this.getTextureGlow();
            List<Vector2> ptd = new List<Vector2>();
            for (int j = 1; j < odp.Count; j++) {
                for (float p = 0; p < 1; p += 0.2f) {
                    ptd.Add(Vector2.Lerp(odp[j - 1], odp[j], p));
                }
            }
            for (int i = 0; i < ptd.Count; i++) {
                Main.spriteBatch.Draw(glow, ptd[i] - Main.screenPosition, null, Color.White * ((i + 1f) / ptd.Count) * ((i + 1f) / ptd.Count) * Projectile.Opacity, Projectile.rotation, glow.Size() / 2f, Projectile.scale * ((i + 1f) / ptd.Count) * ((i + 1f) / ptd.Count), SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}