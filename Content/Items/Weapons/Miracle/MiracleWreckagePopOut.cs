using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Miracle
{
    public class MiracleWreckagePopOut : ModProjectile
    {
        //竖向拖影贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/VerticalSmearLarge")]
        internal static Asset<Texture2D> VerticalSmearLargeTex;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Miracle/MiracleWreckage";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 90;
            Projectile.timeLeft = 1200;
            Projectile.light = 1;
            Projectile.MaxUpdates = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public Vector2 shake = new Vector2(16, 16);
        public override void AI() {

            if (Projectile.localAI[0]++ == 0)
                Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.localAI[0] < Projectile.MaxUpdates * 60) {
                Projectile.velocity *= 0.996f;
                Projectile.rotation += Projectile.velocity.Length() * 0.08f * (Projectile.velocity.X > 0 ? 1 : -1);
                if (Projectile.localAI[0] % 8 == 0)
                    //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                    PRTLoader.NewParticle<PRT_CritSparkCal>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 80, Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * (Projectile.velocity.X > 0 ? -1 : 1)) * 4, new Color(255, 200, 255), 1f).Configure(Color.Violet, 18);
            }
            else {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 3000);
                Vector2 targetPos = target != null ? target.Center : Projectile.GetOwner().Center;
                if (Projectile.localAI[0] < 80 * Projectile.MaxUpdates) {
                    shake.Y *= 0.98f;
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (targetPos - Projectile.Center).ToRotation(), 0.02f, false);
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (targetPos - Projectile.Center).ToRotation(), 0.005f, true);
                }
                if (Projectile.localAI[0] == 80 * Projectile.MaxUpdates) {
                    CEUtils.PlaySound("DemonSwordSwing2", Main.rand.NextFloat(1.2f, 1.5f), Projectile.Center);
                    Projectile.ResetLocalNPCHitImmunity();
                    Projectile.velocity = (targetPos - Projectile.Center).normalize() * 26;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
                if (Projectile.localAI[0] >= 80 * Projectile.MaxUpdates) {
                    if (Projectile.ai[0] % 20 == 0) {
                        //AbyssalLine/Abyssal有的走CEAbyssScreen RT合成,Configure只管常规参数
                        PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, Projectile.velocity * 0.1f, Color.MediumVioletRed, 0.05f).Configure(false, 16, new Vector2(0.32f, 1));
                        var p = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.White, 1);
                        p.lx = 0.8f;
                        p.xadd = 1f;
                        p.spawnColor = Main.rand.NextBool() ? Color.MediumVioletRed : Color.MediumPurple;
                        p.endColor = Color.Red * 0.2f;
                        p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation, 40);
                    }
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("energyImpact", Main.rand.NextFloat(2.8f, 3.2f), target.Center, 4, 0.4f * CEUtils.WeapSound);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 180, 255), 1.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
        }
        public override bool PreDraw(ref Color lightColor) {
            Vector2 offset = new Vector2(Main.rand.NextFloat(-shake.X, shake.X), Main.rand.NextFloat(-shake.Y, shake.Y)).RotatedBy(Projectile.rotation);
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Vector2 origin = tex.Size() / 2f;
            float rotation = Projectile.rotation + MathHelper.PiOver4;
            for (float i = 0; i < 360; i += 60) {
                float rot = MathHelper.ToRadians(i) + Main.GlobalTimeWrappedHourly * 16;
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + offset + CEUtils.randomPointInCircle(2) + rot.ToRotationVector2() * 4, null, Color.White * StrokeAlpha * Projectile.Opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + offset, null, lightColor * Projectile.Opacity, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            if (Projectile.localAI[0] < Projectile.MaxUpdates * 60) {
                Texture2D vs = VerticalSmearLargeTex.Value;
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(vs, Projectile.Center - Main.screenPosition + offset, null, Color.MediumVioletRed, Projectile.rotation + MathHelper.PiOver2, vs.Size() / 2f, Projectile.scale * 0.36f, SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
        public float StrokeAlpha = 0;
    }
}
