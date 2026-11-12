using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>宇宙能量:借原版夜光的贴图(36×36 白色细竖条),缩小并按形态换色;先减速散开再追踪。ai[0] 目标 NPC 下标,ai[1] 形态</summary>
    public class CelestialSlimeStaffEnergy : ModProjectile
    {
        #region 调参旋钮
        //判定边长(像素)、整体缩放(夜光略缩小,策划)、本体缩放(原版夜光按 0.9 倍画)
        public const int Size = 14;
        public const float DrawScale = 0.8f;
        public const float BodyScale = 0.9f;
        //寿命(更新次数,每帧2次更新)、最后几次更新渐隐
        public const int Lifetime = 240;
        public const int FadeUpdates = 16;
        //先减速漂移的更新次数与每次的速度保留比例,之后开始追踪
        public const int DriftUpdates = 24;
        public const float DriftDrag = 0.93f;
        //追踪:速度(像素/更新)、转向系数从小到大、过渡用的更新次数
        public const float HomingSpeed = 16f;
        public const float HomingLerpMin = 0.04f;
        public const float HomingLerpMax = 0.22f;
        public const int HomingRampUpdates = 20;
        //原目标没了以后重新找目标的距离(像素);没有目标时每次更新的加速比例
        public const float RetargetRange = 1200f;
        public const float IdleAccel = 1.04f;
        //照明强度
        public const float LightStrength = 0.45f;
        //拖尾:记录的旧位置数、强度、从尾到头的缩放(最细处不小于本体的一半)
        public const int TrailLength = 16;
        public const float TrailOpacity = 0.6f;
        public const float TrailScaleMin = 0.45f;
        public const float TrailScaleMax = 0.8f;
        //光晕:强度、直径(像素)
        public const float GlowOpacity = 0.45f;
        public const float GlowDiameter = 34f;
        //十字星芒:长轴、短轴缩放,横向那条的强度与长度比例;闪烁幅度与角速度(弧度/更新)
        public const float SparkLong = 0.7f;
        public const float SparkShort = 0.35f;
        public const float SparkCrossOpacity = 0.8f;
        public const float SparkCrossLength = 0.7f;
        public const float PulseDepth = 0.15f;
        public const float PulseSpeed = 0.3f;
        //本体颜色向白色靠的比例
        public const float BodyWhiten = 0.3f;
        //消失时的白色粒子:数量(策划:一点点)、速度(像素/帧)、缩放、寿命(帧)
        public const int KillSparks = 4;
        public const float KillSparkSpeed = 2.5f;
        public const float KillSparkScale = 0.25f;
        public const int KillSparkLife = 14;
        #endregion

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FairyQueenMagicItemShot;

        private int Form => (int)Projectile.ai[1];

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            float age = ++Projectile.localAI[0];
            NPC target = Target();
            if (age <= DriftUpdates) {
                Projectile.velocity *= DriftDrag;
            }
            else if (target != null) {
                float lerp = MathHelper.Lerp(HomingLerpMin, HomingLerpMax, GodDraw.Smooth((age - DriftUpdates) / HomingRampUpdates));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * HomingSpeed, lerp);
            }
            else {
                if (Projectile.owner == Main.myPlayer) {
                    NPC next = Projectile.FindTargetWithinRange(RetargetRange);
                    if (next != null) {
                        Projectile.ai[0] = next.whoAmI;
                        Projectile.netUpdate = true;
                    }
                }
                if (Projectile.velocity.Length() < HomingSpeed) {
                    Projectile.velocity *= IdleAccel;
                }
            }
            if (Projectile.velocity != Vector2.Zero) {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            Lighting.AddLight(Projectile.Center, CelestialSlimeStaffSlime.MainColor(Form).ToVector3() * LightStrength);
        }

        private NPC Target() {
            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs) {
                return null;
            }
            NPC npc = Main.npc[index];
            return npc.CanBeChasedBy(Projectile) ? npc : null;
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < KillSparks; i++) {
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center, Main.rand.NextVector2Circular(KillSparkSpeed, KillSparkSpeed), Color.White, KillSparkScale)
                    .Configure(1f, lifetime: KillSparkLife);
            }
        }

        //拖尾、光晕和十字星芒走加法;本体用普通混合画一层实色,亮背景上也不发虚
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D spark = GodDraw.Spindle;
            Texture2D soft = GodDraw.Glow;
            Color main = CelestialSlimeStaffSlime.MainColor(Form);
            Color trail = CelestialSlimeStaffSlime.TrailColor(Form);
            Vector2 origin = tex.Size() / 2f;
            float fade = Utils.GetLerpValue(0f, FadeUpdates, Projectile.timeLeft, true);
            float pulse = 1f + PulseDepth * MathF.Sin(Projectile.localAI[0] * PulseSpeed);

            GodDraw.BeginAdditive();
            int count = Projectile.oldPos.Length;
            for (int i = count - 1; i >= 1; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)count;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, GodDraw.Add(trail, TrailOpacity * t * fade), Projectile.oldRot[i], origin,
                    DrawScale * MathHelper.Lerp(TrailScaleMin, TrailScaleMax, t), SpriteEffects.None, 0f);
            }
            GodDraw.Centered(soft, Projectile.Center, GodDraw.Add(trail, GlowOpacity * fade), 0f, GlowDiameter / soft.Width);
            GodDraw.Centered(spark, Projectile.Center, GodDraw.Add(main, fade), 0f, new Vector2(SparkShort, SparkLong) * DrawScale * pulse);
            GodDraw.Centered(spark, Projectile.Center, GodDraw.Add(main, SparkCrossOpacity * fade), MathHelper.PiOver2,
                new Vector2(SparkShort, SparkLong * SparkCrossLength) * DrawScale * pulse);
            GodDraw.End();

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Lerp(main, Color.White, BodyWhiten) * fade, Projectile.rotation, origin,
                DrawScale * BodyScale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
