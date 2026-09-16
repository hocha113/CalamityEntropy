using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AcropolisTeslaBall : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 1024;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 4;
            Projectile.friendly = false;
        }
        public override void AI() {
            if (Projectile.ai[0] != 0 && Projectile.Entropy().FirstFrames) {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<AcropolisTeslaBallWarn>(), 0, 0, -1, Projectile.ai[0] == 1 ? 0 : 1);
                if (Projectile.ai[0] == -1)
                    Projectile.MaxUpdates *= 2;
            }
            Projectile.tileCollide = Projectile.velocity.Length() > 4;
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 16) {
                oldPos.RemoveAt(0);
            }
            if (Projectile.ai[2]++ > 60) {
                Projectile.velocity.Y += 0.02f;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.AddBuff(ModContent.BuffType<MechanicalTrauma>(), 180);
            Projectile.timeLeft = 4;
            Projectile.Kill();
        }
        public override void OnKill(int timeLeft) {
            if (timeLeft > 0) {
                float v = Projectile.ai[0];
                if (v < 0)
                    v = 0;
                //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, new Color(255, 180, 180), 0.1f).Configure(new Vector2(2f, 2f), 0, 0.6f - v * 0.3f, 16);
                CEUtils.SetShake(Projectile.Center, 4 - v * 1.2f);
                CEUtils.PlaySound("energyImpact", Main.rand.NextFloat(0.7f, 1.3f), Projectile.Center);
                if (v != 1)
                    CEUtils.SpawnExplotionHostile(((int)Projectile.ai[1]).ToNPC().GetSource_FromAI(), Projectile.Center, Projectile.damage, 100);
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 1 * Projectile.scale;
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            for (int i = 0; i < oldPos.Count; i++) {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public static void DrawEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 230, 230) * alpha, 0, tex.Size() * 0.5f, size * 0.24f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 40, 40) * alpha, 0, tex.Size() * 0.5f, size * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
    }

    public class AcropolisTeslaBallWarn : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 800;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 52;
        }
        public override void AI() {
            Projectile.tileCollide = Projectile.velocity.Length() > 4;
            if (Projectile.ai[2]++ > 60) {
                Projectile.velocity.Y += 0.02f;
            }
            //GlowSpark旧EParticle,Configure尾参统一签名那套
            var __prt = PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 0.05f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), (int)(Projectile.ai[2] * (Projectile.ai[0] == 0 ? 0.26f : 0.13f)) + 2);
            __prt.grav = false;
        }

        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override bool? CanDamage() {
            return false;
        }
    }
}
