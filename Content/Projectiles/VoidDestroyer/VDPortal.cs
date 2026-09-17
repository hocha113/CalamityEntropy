using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 驱逐舰的传送门演出弹幕(无伤害):ai[0] 模式 0 冲刺门(朝向 = 生成时 rotation,寿命 ai[1]),1 支援投送地面门;
    /// ai[2] 门环亮度倍率(0 = 1 倍;幻影舰队里真身的门更亮,是可读的破绽)。开合曲线由寿命推导,全端一致
    /// </summary>
    public class VDPortal : ModProjectile, IVoidDestroyerProjectile
    {
        public const int ModeDash = 0;
        public const int ModeReinforce = 1;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public int Mode => (int)Projectile.ai[0];
        public int TotalLife => (int)Math.Max(Projectile.ai[1], 20f);
        public float GlowMult => Projectile.ai[2] > 0f ? Projectile.ai[2] : 1f;

        public override void SetDefaults() {
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

        //velocity 只当朝向用(随生成包过线),门本身不动
        public override bool ShouldUpdatePosition() => false;

        /// <summary>开合程度:前 25% 展开,最后 25% 收拢</summary>
        public float Openness() {
            float total = TotalLife;
            float age = total - Projectile.timeLeft;
            float open = MathHelper.Clamp(age / (total * 0.25f), 0f, 1f);
            float close = MathHelper.Clamp(Projectile.timeLeft / (total * 0.25f), 0f, 1f);
            return Math.Min(open, close);
        }

        public override void AI() {
            //首帧同步寿命(客户端拿到的 timeLeft 是 SetDefaults 的 60,按 ai[1] 重定)
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = TotalLife;
                if (!Main.dedServ) {
                    CEUtils.PlaySound("portal_emerge", Mode == ModeDash ? 1.2f : 0.9f, Projectile.Center, 4, 0.8f);
                }
            }
            //支援门开在地面上:短轴竖直,门面是横椭圆;冲刺门短轴指向通过方向
            if (Mode == ModeReinforce) {
                Projectile.rotation = MathHelper.PiOver2;
            }
            else if (Projectile.velocity != Vector2.Zero) {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            Lighting.AddLight(Projectile.Center, VoidDestroyerNPC.VoidPurple.ToVector3() * 0.8f * Openness());
            if (!Main.dedServ && Main.rand.NextBool(2)) {
                float ang = Projectile.rotation + MathHelper.PiOver2;
                float extent = Mode == ModeDash ? 90f : 70f;
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * Main.rand.NextFloat(-extent, extent) * Openness();
                Vector2 v = CEUtils.randomPointInCircle(2f);
                var p = PRTLoader.NewParticle<PRT_Void>(pos, v, Color.White, Main.rand.NextFloat(0.8f, 1.3f));
                p.Opacity = 0.6f;
                p.ad = 0.03f;
            }
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor) {
            float size = Mode == ModeDash ? 120f : 90f;
            VoidDestroyerNPC.DrawPortalAt(Projectile.Center, Openness(), Projectile.rotation, size, Main.screenPosition, GlowMult);
            return false;
        }
    }
}
