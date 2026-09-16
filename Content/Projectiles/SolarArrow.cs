using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    //脱离灾厄死代码裁决:SolarArrowSpawner全仓零引用(WorshipRelic新效果直接生成SolarArrow),整类删除
    public class SolarArrow : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 12;
            Projectile.extraUpdates = 1;
        }
        public int counter = 0;
        public bool std = false;
        public int homingTime = 60;
        public PRT_StarTrailParticle spt = null;
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] < 9) {
                return false;
            }
            return null;
        }
        public override void AI() {
            if (spt == null) {
                //StarTrailParticle星尘拖尾,旧EParticle StarTrail
                spt = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                spt.maxLength = 18;
            }
            spt.Velocity = Projectile.velocity * 0.2f;
            spt.Lifetime = 30;
            spt.Position = Projectile.Center;
            counter++;
            Projectile.ai[0]++;


            if (counter > 9 && Projectile.ai[2] == 0) {
                if (Projectile.velocity.Length() > 3) {
                    Projectile.velocity *= 0.995f - homing * 0.018f;
                }
                if (homing < 8) {
                    homing += 0.14f;
                }
                NPC targett = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 2400);

                if (targett != null) {
                    if (Projectile.timeLeft < 60) {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (targett.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing * 2;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        float homing = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile.ai[2] = 1;
            Projectile.netUpdate = true;
            //DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, Color.OrangeRed, 0.02f).Configure(new Vector2(2f, 2f), 0, 0.6f * 0.4f, 18);
            CEUtils.PlaySound(Main.rand.NextBool() ? "scholarStaffImpact" : "scholarStaffImpact2", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
        }

        public int tofs;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }

    }
}
