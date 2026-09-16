using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class VoidBomb : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
            Projectile.Kill();
        }
        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 120;

        }
        public override bool CanHitPlayer(Player target) {
            return false;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Projectile.rotation);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Projectile.rotation = reader.ReadSingle();
        }
        public override void AI() {
            if (Projectile.ai[2] == 0) {
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.ai[2]++;
            }
            Projectile.rotation += (Projectile.whoAmI % 2 == 0 ? 1 : -1) * Projectile.velocity.Length() * 0.01f;
            counter++;
            Projectile.velocity *= 0.96f;
            if (Main.dedServ)
                CEUtils.SyncProj(Projectile.whoAmI);
        }
        public float counter = 0;
        public override void OnKill(int timeLeft) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.rotation.ToRotationVector2() * 46, ModContent.ProjectileType<VoidSpike>(), Projectile.damage, 2);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.rotation.ToRotationVector2() * -46, ModContent.ProjectileType<VoidSpike>(), Projectile.damage, 2);
            }
            CEUtils.PlaySound("VoidBomb", Main.rand.NextFloat(0.7f, 1.3f), Projectile.Center, 16);
            //DetailedExplosionCal Configure三参是Calamity explode原样,别套EParticle尾参
            PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, new Color(180, 156, 255) * 0.5f, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 0.46f, 30);
            for (float i = 0; i <= 1; i += 0.1f) {
                //CustomPulse贴图路径现传,走PRTPathTextures缓存,Configure第一个string是TexPath
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Lerp(Color.White, Color.BlueViolet, i), 0.01f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.01f, i * 0.16f, (int)(i * 30));
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D t = CEExtraAssets.a_circle;
            float alpha = 0.52f * (counter / 120f);
            if (Projectile.timeLeft <= 7)
                alpha = Projectile.timeLeft / 7f;
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition, null, new Color(180, 180, 255) * alpha, Projectile.rotation, t.Size() / 2f, new Vector2(36, 0.05f), SpriteEffects.None);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }

}