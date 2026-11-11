using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
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
    /// 暗物质球,绕本体匀速公转,生成 2 秒后亮起淡紫描边才有判定
    /// 位置由本体中心、半径、起始角和存活帧解析算出,各端一致
    /// ai[0] 本体下标,ai[1] 带符号的轨道半径(符号是转向),ai[2] 起始角,localAI[0] 存活帧
    /// </summary>
    public class VSlimeDarkOrb : VSlimeHostileProjectile
    {
        private float Age => Projectile.localAI[0];
        private float OrbitRadius => Math.Abs(Projectile.ai[1]);
        private int SpinDir => Projectile.ai[1] < 0f ? -1 : 1;
        /// <summary>武装程度 0..1,武装拍前后各 12 帧过渡</summary>
        private float Armed => GodDraw.Span(Age, VSlimeDirector.RingArmFrames - 12f, VSlimeDirector.RingArmFrames + 12f);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 400;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            VoidSlimeNPC boss = OwnerBoss(Projectile.ai[0]);
            Projectile.localAI[0]++;
            //本体换态或失效时各端自行收掉;客户端的换态包可能晚于生成包,前 StateGraceFrames 帧不判
            if (boss == null || (Age > StateGraceFrames && boss.CurrentStateIndex != VSlimeStateIndex.GravityRings)) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 60;
            float angle = Projectile.ai[2] + SpinDir * VSlimeDirector.RingTangentSpeed / OrbitRadius * Age;
            Projectile.Center = boss.NPC.Center + angle.ToRotationVector2() * OrbitRadius;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.04f * SpinDir;
            Projectile.Opacity = GodDraw.Span(Age, 0f, 20f);
            float armed = Armed;
            Lighting.AddLight(Projectile.Center, 0.35f * armed, 0.2f * armed, 0.6f * armed);
        }

        public override bool? CanDamage() => Age >= VSlimeDirector.RingArmFrames ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircleHits(Projectile.Center, 15f, targetHitbox);

        public override void OnKill(int timeLeft) {
            VSlimeVfx.Stardust(Projectile.Center, 6, 1.5f, 4.5f, 0.9f);
        }

        /// <summary>武装前只有半透明的暗球,武装后叠淡紫环和底光</summary>
        public override bool PreDraw(ref Color lightColor) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0.01f) {
                return false;
            }
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            float armed = Armed;
            float pulse = 0.85f + 0.15f * MathF.Sin(Age * 0.2f + Projectile.whoAmI);

            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(VSlimeVfx.VoidPurple, (0.25f + 0.45f * armed) * opacity), 0f, 0.5f);
            if (armed > 0.01f) {
                GodDraw.Centered(GodDraw.BloomRing, Projectile.Center, GodDraw.Add(VSlimeVfx.Lavender, 0.9f * armed * pulse * opacity), 0f, GodDraw.RingScale(19f));
            }
            GodDraw.End();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * (opacity * (0.55f + 0.45f * armed)), Projectile.rotation,
                tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
