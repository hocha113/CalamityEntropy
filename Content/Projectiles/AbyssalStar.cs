using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AbyssalStar : ModProjectile
    {
        //星芒拖尾贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Extra/StarTrail")]
        internal static Asset<Texture2D> StarTrailTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.MaxUpdates = 16;
            Projectile.timeLeft = 50 * Projectile.MaxUpdates;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 20, 1f, 1000, 16);
            CalamityEntropy.SpawnHeavenSpark(target.Center, CEUtils.randomRot(), 0.6f, 0.6f, new Color(40, 40, 186));
        }
        public override void AI() {
            //HeavenfallStar拖尾,旧PRT/EParticle HeavenfallStar数值照抄
            PRTLoader.NewParticle<PRT_HeavenfallStar>(Projectile.Center - Projectile.velocity * 6, Projectile.velocity.normalize(), new Color(26, 26, 180), Main.rand.NextFloat(0.6f, 1.3f) * 0.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 14);  //HeavenfallStar拖尾,旧EParticle HeavenfallStar数值照抄

            Projectile.rotation += 0.16f;

        }


        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.timeLeft < 30) {
                lightColor *= ((float)Projectile.timeLeft / 30f);
            }
            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D t = StarTrailTex.Value;
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, lightColor * 0.6f, Projectile.velocity.ToRotation(), t.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tx.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }


}