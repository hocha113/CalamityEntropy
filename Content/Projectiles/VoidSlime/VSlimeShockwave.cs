using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using System;
using Terraria;
using Terraria.ID;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 圆形冲击波,波面匀速外扩,只有波面那一圈有判定
    /// ai[0] 模式(0 苍白砸地,1 星体爆发,2 红巨星爆发),ai[1] 本体下标,localAI[0] 存活帧
    /// 红巨星模式下站在图腾内的玩家不受伤
    /// </summary>
    public class VSlimeShockwave : VSlimeHostileProjectile
    {
        public const float ModeSmash = 0f;
        public const float ModeBurst = 1f;
        public const float ModeGiant = 2f;

        /// <summary>波面扣掉的后沿宽度不少于这个数,慢波面也有足够厚的判定</summary>
        private const float MinBand = 44f;
        private const int FadeFrames = 12;

        public override string Texture => CEUtils.WhiteTexPath;

        private float Age => Projectile.localAI[0];
        private int Mode => (int)Projectile.ai[0];

        private float StartRadius => Mode == 2 ? VSlimeDirector.GiantRadius : 24f;
        private float MaxRadius => Mode == 0 ? VSlimeDirector.SmashShockRadius : Mode == 1 ? VSlimeDirector.RingBurstRadius : VSlimeDirector.GiantShockMaxRadius;
        private float Speed => Mode == 0 ? VSlimeDirector.SmashShockSpeed : Mode == 1 ? VSlimeDirector.RingBurstSpeed : VSlimeDirector.GiantShockSpeed;
        private int TravelFrames => (int)MathF.Ceiling((MaxRadius - StartRadius) / Speed);
        private float Radius => Math.Min(MaxRadius, StartRadius + Speed * Age);
        private Color Tint => Mode == 0 ? VSlimeVfx.PaleLight : Mode == 1 ? VSlimeVfx.VoidBright : VSlimeVfx.GiantRed;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            //timeLeft 不过线,首帧各端自己定
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = TravelFrames + FadeFrames;
            }
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Age > TravelFrames) {
                return false;
            }
            float radius = Radius;
            float band = Math.Max(MinBand, Speed * 1.5f);
            Vector2 target = targetHitbox.Center.ToVector2();
            float dist = Vector2.Distance(Projectile.Center, target);
            if (dist > radius + 10f || dist < radius - band) {
                return false;
            }
            if (Mode == 2) {
                VoidSlimeNPC boss = OwnerBoss(Projectile.ai[1]);
                if (boss != null && boss.InsideTotem(target)) {
                    return false;
                }
            }
            return true;
        }

        public override bool PreDraw(ref Color lightColor) {
            float radius = Radius;
            float travel = MathHelper.Clamp(Age / TravelFrames, 0f, 1f);
            float fade = Age > TravelFrames ? 1f - (Age - TravelFrames) / FadeFrames : 1f;
            //波面越跑越薄,尾部拖一层淡的余波
            float half = MathHelper.Lerp(26f, 12f, travel) * (Mode == 0 ? 0.7f : 1f);
            int segments = Math.Clamp((int)(radius / 10f), 48, 160);
            Color tint = Tint;

            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, Projectile.Center, radius - half * 2.2f, half * 3f, GodDraw.Add(tint, 0.22f * fade), segments: segments);
            GodDraw.RingStrip(GodDraw.SoftLine, Projectile.Center, radius, half, GodDraw.Add(tint, 0.9f * fade), segments: segments);
            GodDraw.RingStrip(GodDraw.SoftLine, Projectile.Center, radius + half * 0.2f, half * 0.4f, GodDraw.Add(Color.White, fade), segments: segments);
            GodDraw.End();
            return false;
        }
    }
}
