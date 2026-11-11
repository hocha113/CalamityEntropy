using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 星体残骸,从引力场边沿慢慢加速飞向黑洞,贴近后被吸入;黑洞散掉时还在路上的就地淡出
    /// ai[0] 本体下标,ai[1] 贴图帧(0..2),localAI[0] 存活帧,localAI[1] 淡出计时
    /// </summary>
    public class VSlimeDebris : VSlimeHostileProjectile
    {
        private const int FadeFrames = 20;
        /// <summary>进到黑洞半径的这个比例就算被吸入</summary>
        private const float AbsorbRatio = 0.55f;

        public override int DefaultTimeLeft => VSlimeDirector.DebrisLife;

        private float Age => Projectile.localAI[0];
        private bool Fading => Projectile.localAI[1] > 0f;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 28;
            Projectile.height = 28;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            Projectile.frame = Math.Clamp((int)Projectile.ai[1], 0, 2);
            Projectile.rotation += 0.03f + 0.01f * Projectile.frame;

            VoidSlimeNPC boss = OwnerBoss(Projectile.ai[0]);
            bool holeAlive = boss != null && (Age <= StateGraceFrames || boss.CurrentStateIndex == VSlimeStateIndex.BlackHoleBurst) && boss.HazardRadius > 0f;
            if (!holeAlive && Age > StateGraceFrames && !Fading) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, FadeFrames);
            }
            if (Fading) {
                Projectile.velocity *= 0.94f;
            }
            else if (boss != null) {
                Vector2 toHole = boss.NPC.Center - Projectile.Center;
                float dist = toHole.Length();
                if (dist <= Math.Max(40f, boss.HazardRadius * AbsorbRatio)) {
                    Projectile.Kill();
                    return;
                }
                float speed = Math.Min(VSlimeDirector.DebrisStartSpeed + VSlimeDirector.DebrisAccel * Age, VSlimeDirector.DebrisMaxSpeed);
                Projectile.velocity = toHole / dist * speed;
            }
            Projectile.Opacity = MathHelper.Clamp(Age / 12f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / (float)FadeFrames, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 0.4f, 0.16f, 0.12f);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f && !Fading ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircleHits(Projectile.Center, 13f, targetHitbox);

        public override void OnKill(int timeLeft) {
            if (timeLeft > 0) {
                VSlimeVfx.Scatter(Projectile.Center, DustID.RedTorch, 6, 1.5f, 4f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            DrawBolt(tex, frame, VSlimeVfx.HoleEmber, Projectile.Opacity, Projectile.scale, 0.36f);
            return false;
        }
    }
}
