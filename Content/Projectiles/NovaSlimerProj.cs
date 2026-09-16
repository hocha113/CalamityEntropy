using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NovaSlimerProj : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public override void AI() {
            Projectile.ai[0]++;
            Projectile.pushByOther(1.4f);
            NPC target = Projectile.FindTargetWithinRange(3000);
            if (target != null) {
                Projectile.velocity *= 0.98f;
                Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1f * (CEUtils.getDistance(Projectile.Center, target.Center) > 360 ? 1 : -1.4f);
                if (CEUtils.getDistance(Projectile.Center, target.Center) < 400 && Projectile.ai[0] > 40) {
                    //外部模组软集成裁撤: 原星云矛联动改为自有星辉弹
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 14, ModContent.ProjectileType<FriendlyAstralShoot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    if (p.WithinBounds(Main.maxProjectiles)) {
                        Main.projectile[p].DamageType = DamageClass.Magic;
                    }
                    Projectile.Kill();
                }
            }
            else {
                if (Projectile.GetOwner().Distance(Projectile.Center) > 160) {
                    Projectile.velocity *= 0.99f;
                    Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * 0.8f;
                }


            }
            Projectile.rotation += 0.2f;
        }
        public override bool PreDraw(ref Color lightColor) {
            //外部模组贴图联动裁撤, 改自有星辉星云绘制
            Texture2D tex = CEExtraAssets.StarTexture;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Color c1 = new Color(157, 100, 183);
            Color c2 = new Color(255, 105, 234);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, c1, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * 0.55f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, c2 * 0.8f, -Projectile.rotation * 0.7f, tex.Size() * 0.5f, Projectile.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * 0.6f, Projectile.rotation * 1.3f, tex.Size() * 0.5f, Projectile.scale * 0.3f, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
            return false;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
    }


}