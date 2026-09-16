using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VoidWraith : ModProjectile
    {
        //幽灵帧动画(VoidWraith0~5),加载期就位,绘制时不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/vw/VoidWraith", 0, 6, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        public List<Vector2> odp = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1.4f;
            Projectile.timeLeft = 600;
            Projectile.scale = 1.2f;
        }
        public override void AI() {
            if (Main.GameUpdateCount % 5 == 0) {
                Projectile.frame++;
                if (Projectile.frame > 5) {
                    Projectile.frame = 0;
                }
            }
            NPC target = Projectile.FindTargetWithinRange(1000, false);
            if (target != null && Main.myPlayer == Projectile.owner) {
                if (Projectile.ai[1] <= 0) {
                    Projectile.ai[1] = 20;
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center).SafeNormalize(Vector2.One) * 18, ModContent.ProjectileType<VoidBullet>(), Projectile.damage.ApplyAccArmorDamageBonus(Projectile.GetOwner()), 4, Projectile.owner);
                    if (!Projectile.owner.ToPlayer().HeldItem.IsAir) {
                        p.ToProj().DamageType = Projectile.owner.ToPlayer().HeldItem.DamageType;
                    }

                }
            }
            Projectile.direction = Projectile.owner.ToPlayer().direction;
            Projectile.Center = Projectile.owner.ToPlayer().Center - new Vector2(0, 60);
            Projectile.ai[1]--;
            for (int i = 0; i < 1; i++) {
                Vector2 direction = new Vector2(0, 1).RotatedBy(Projectile.rotation);
                Vector2 smokeSpeed = direction.RotatedByRandom(MathHelper.PiOver4 * 0.1f) * Main.rand.NextFloat(10f, 30f) * 0.9f;
                //HeavySmokeCal Configure是Calamity原构造顺序,跟PRT/EParticle统一尾参不是一回事
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, smokeSpeed + Projectile.owner.ToPlayer().velocity, Color.Lerp(Color.Purple, Color.Indigo, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f)), Main.rand.NextFloat(0.6f, 1.2f)).Configure(0.8f, 15, 0, false, 0, true);

                if (Main.rand.NextBool(3)) {
                    //HeavySmokeCal Configure是Calamity原构造顺序,跟EParticle统一尾参不是一回事
                    PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center, smokeSpeed + Projectile.owner.ToPlayer().velocity, Main.hslToRgb(0.85f, 1, 0.8f), Main.rand.NextFloat(0.4f, 0.7f)).Configure(0.8f, 10, 0.01f, true, 0.01f, true);
                }
            }
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public void draw() {
            Texture2D draw = Frames[Projectile.frame];
            Vector2 drawPos = Projectile.Center + new Vector2(-7 * (Projectile.owner.ToPlayer().velocity.X > 0 ? 1 : -1), 4);
            Main.EntitySpriteDraw(draw, drawPos - Main.screenPosition, null, Color.White, MathHelper.ToRadians(Projectile.owner.ToPlayer().velocity.X * 1.4f), draw.Size() / 2, Projectile.scale, (Projectile.owner.ToPlayer().velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));

        }
    }

}