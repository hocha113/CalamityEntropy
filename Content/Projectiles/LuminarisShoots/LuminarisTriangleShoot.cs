using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.LuminarisShoots
{

    public class LuminarisTriangleShootBlue : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 20);
        }
        public override void SetDefaults() {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 120;
        }
        public float counter = 0;
        public override void AI() {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += Projectile.velocity.X * 0.01f;
        }
        public override void OnKill(int timeLeft) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                List<int> rots = new List<int>() { 0, 120, 240 };
                foreach (int i in rots) {
                    float r = MathHelper.ToRadians(i);
                    float a = r + Projectile.rotation;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, a.ToRotationVector2() * 12, ModContent.ProjectileType<LuminarisSpikeBlue>(), Projectile.damage, Projectile.knockBack, -1, 1);
                }
            }
            CEUtils.PlaySound("FractalHit", 1, Projectile.Center, 16, 0.6f);
            CEUtils.PlaySound("HammerShoot2", 1, Projectile.Center, 16);

        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D l = CEExtraAssets.LTLine;
            List<int> rots = new List<int>() { 0, 120, 240 };
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            foreach (int i in rots) {
                float r = MathHelper.ToRadians(i);
                float a = r + Projectile.rotation;
                Main.spriteBatch.Draw(l, Projectile.Center - Main.screenPosition + CEUtils.randomVec(6f * (1 - Projectile.timeLeft / 120f)), null, Color.White * (1 - Projectile.timeLeft / 120f), a, new Vector2(0, l.Height / 2f), 0.6f * Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.ToRadians(90), tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
    public class LuminarisTriangleShootRed : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 120;
        }
        public float counter = 0;
        public override void AI() {
            Projectile.velocity *= 0.98f;
            Projectile.rotation += Projectile.velocity.X * 0.01f;
        }
        public override void OnKill(int timeLeft) {
            List<int> rots = new List<int>() { 0, 120, 240 };
            foreach (int i in rots) {
                float r = MathHelper.ToRadians(i);
                float a = r + Projectile.rotation;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, a.ToRotationVector2() * 12, ModContent.ProjectileType<LuminarisSpikeRed>(), Projectile.damage, Projectile.knockBack, -1, 1);

            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D l = CEExtraAssets.LTLine;
            List<int> rots = new List<int>() { 0, 120, 240 };
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            foreach (int i in rots) {
                float r = MathHelper.ToRadians(i);
                float a = r + Projectile.rotation;
                Main.spriteBatch.Draw(l, Projectile.Center - Main.screenPosition + CEUtils.randomVec(6f * (1 - Projectile.timeLeft / 120f)), null, Color.White * (1 - Projectile.timeLeft / 120f), a, new Vector2(0, l.Height / 2f), 0.6f * Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.ToRadians(90), tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}