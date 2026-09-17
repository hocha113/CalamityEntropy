using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.LightDarkDagger
{
    /// <summary>光暗龙匕的调色:耀光走金白,黯影走紫黑。</summary>
    internal static class LDPalette
    {
        public static readonly Color LightMain = new Color(255, 225, 140);
        public static readonly Color LightEdge = new Color(255, 250, 220);
        public static readonly Color DarkMain = new Color(150, 60, 220);
        public static readonly Color DarkEdge = new Color(90, 20, 140);

        public static Color Tone(bool light) => light ? LightMain : DarkMain;
        public static Color EdgeTone(bool light) => light ? LightEdge : DarkEdge;

        public static void Debuff(NPC target, bool light) {
            target.AddBuff(light ? BuffID.Ichor : BuffID.ShadowFlame, 180);
        }

        public static void HitBurst(Vector2 pos, bool light, float strength) {
            Color main = Tone(light);
            for (int i = 0; i < 5; i++) {
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 8f) * strength;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(pos, vel, main, 0.045f * strength).Configure(false, 12, new Vector2(0.4f, 1f), true);
            }
            PRTLoader.NewParticle<PRT_ShineParticle>(pos, Vector2.Zero, main, 1.1f * strength).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 8);
        }
    }

    /// <summary>
    /// 普攻飞刃基类。ai[0] = 剩余回转总角,ai[1] = 剩余回转帧数;回转期结束后对近处目标轻微追踪。
    /// 穿墙、穿透 3 个目标,命中落光/暗印记并按印记触发光影斩切。
    /// </summary>
    public abstract class LDKnifeBase : ModProjectile
    {
        public abstract bool IsLight { get; }

        public const int HomingRange = 320;
        public const float HomingTurn = 0.035f;
        public const int TrailLength = 8;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, 3);
            Projectile.width = Projectile.height = 22;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 200;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            if (Projectile.ai[1] > 0) {
                float step = Projectile.ai[0] / Projectile.ai[1];
                Projectile.velocity = Projectile.velocity.RotatedBy(step);
                Projectile.ai[0] -= step;
                Projectile.ai[1]--;
            }
            else {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange);
                if (target != null) {
                    float want = (target.Center - Projectile.Center).ToRotation();
                    float cur = Projectile.velocity.ToRotation();
                    Projectile.velocity = CEUtils.RotateTowardsAngle(cur, want, HomingTurn, true).ToRotationVector2() * Projectile.velocity.Length();
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            Color glow = LDPalette.Tone(IsLight);
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.35f);
            if (Main.rand.NextBool(3)) {
                Vector2 pos = Projectile.Center - Projectile.velocity * 0.5f + CEUtils.randomPointInCircle(4f);
                PRTLoader.NewParticle<PRT_Light>(pos, Projectile.velocity * 0.1f, glow, Main.rand.NextFloat(0.25f, 0.4f)).Configure(0.7f, lifetime: 14);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            LDPalette.Debuff(target, IsLight);
            Projectile.GetOwner().GetModPlayer<LDDaggerPlayer>().RegisterHit(hit.Crit);
            target.GetGlobalNPC<LDMarkNPC>().OnKnifeHit(target, Projectile, IsLight, false);
            LDPalette.HitBurst(target.Center, IsLight, hit.Crit ? 1.3f : 0.8f);
            CEUtils.PlaySound("LightHit", IsLight ? 1.2f : 0.8f, target.Center, 6, 0.5f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            Color main = LDPalette.Tone(IsLight);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, main * fade * 0.5f, Projectile.oldRot[i], origin, Projectile.scale * (0.8f + 0.2f * fade), SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class LDKnifeLight : LDKnifeBase
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/LightDarkDagger/LDKnifeLight";
        public override bool IsLight => true;
    }

    public class LDKnifeDark : LDKnifeBase
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/LightDarkDagger/LDKnifeDark";
        public override bool IsLight => false;
    }

    /// <summary>
    /// 蓄势螺旋刃。ai[0] = 相位,ai[1] = 落后帧数。位置按出发点 + 直线推进 + 正弦横摆解析求得,
    /// 光暗两股相位差 π 即相互纠缠的双螺旋;无限穿透敌怪与物块。
    /// </summary>
    public abstract class LDHelixKnifeBase : ModProjectile
    {
        public abstract bool IsLight { get; }

        public const float Radius = 46f;
        public const float Omega = 0.17f;
        public const int Life = 150;
        public const int TrailLength = 12;

        private bool initialized;
        private Vector2 startPos;
        private Vector2 dir;
        private float speed;
        private int time;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 24;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = Life;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (!initialized) {
                initialized = true;
                startPos = Projectile.Center;
                dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                speed = Math.Max(Projectile.velocity.Length(), 1f);
            }
            time++;
            float t = time - Projectile.ai[1];
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            Vector2 next = startPos + dir * speed * t + perp * Radius * (float)Math.Sin(Omega * t + Projectile.ai[0]);
            Vector2 delta = next - Projectile.Center;
            if (delta.LengthSquared() > 0.01f) {
                Projectile.rotation = delta.ToRotation() + MathHelper.PiOver4;
            }
            Projectile.Center = next;

            Color glow = LDPalette.Tone(IsLight);
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.6f);
            PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(5f), -delta * 0.15f, glow, Main.rand.NextFloat(0.35f, 0.55f)).Configure(0.8f, lifetime: 16);
            if (Projectile.timeLeft < 20) {
                Projectile.Opacity = Projectile.timeLeft / 20f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            LDPalette.Debuff(target, IsLight);
            Projectile.GetOwner().GetModPlayer<LDDaggerPlayer>().RegisterHit(hit.Crit);
            target.GetGlobalNPC<LDMarkNPC>().OnKnifeHit(target, Projectile, IsLight, true);
            LDPalette.HitBurst(target.Center, IsLight, 1.2f);
            CEUtils.PlaySound("LightHit", IsLight ? 1.3f : 0.7f, target.Center, 6, 0.5f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            Color main = LDPalette.Tone(IsLight) * Projectile.Opacity;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, main * fade * 0.6f, Projectile.oldRot[i], origin, Projectile.scale * (0.7f + 0.3f * fade), SpriteEffects.None);
            }
            Texture2D glowTex = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glowTex, Projectile.Center - Main.screenPosition, null, main * 0.6f, 0f, glowTex.Size() / 2f, 0.22f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class LDHelixKnifeLight : LDHelixKnifeBase
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/LightDarkDagger/LDKnifeLight";
        public override bool IsLight => true;
    }

    public class LDHelixKnifeDark : LDHelixKnifeBase
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/LightDarkDagger/LDKnifeDark";
        public override bool IsLight => false;
    }

    /// <summary>
    /// 光影斩切:两种刃交替命中时追加的一次斩击。ai[0] = 目标编号,ai[1] = 1 为强化版(倍率更高、回血翻倍)。
    /// 只对锁定目标生效,跟随目标中心以保证命中。
    /// </summary>
    public class LDSlash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const float EnhancedDamageMult = 1.5f;
        public const int HealNormal = 2;
        public const int HealEnhanced = 4;
        public const int Life = 16;

        private bool Enhanced => Projectile.ai[1] > 0.5f;
        private float rotSeed;

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 70;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Life;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                rotSeed = Main.rand.NextFloat(-0.3f, 0.3f);
                if (Enhanced) {
                    Projectile.scale = 1.5f;
                }
                CEUtils.PlaySound("slice", Enhanced ? 0.9f : 1.15f, Projectile.Center, 6, 0.7f);
                for (int i = 0; i < 2; i++) {
                    bool light = i == 0;
                    PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, LDPalette.Tone(light), 1.6f * Projectile.scale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 10);
                }
            }
            int idx = (int)Projectile.ai[0];
            if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active) {
                Projectile.Center = Main.npc[idx].Center;
            }
            Projectile.Opacity = Projectile.timeLeft / (float)Life;
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0];

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Ichor, 120);
            target.AddBuff(BuffID.ShadowFlame, 120);
            Player owner = Projectile.GetOwner();
            if (Main.myPlayer == Projectile.owner && owner.statLife < owner.statLifeMax2) {
                owner.Heal(Enhanced ? HealEnhanced : HealNormal);
            }
            for (int i = 0; i < 8; i++) {
                bool light = i % 2 == 0;
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(4f, 11f) * Projectile.scale;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, LDPalette.Tone(light), 0.05f).Configure(false, 14, new Vector2(0.4f, 1f), true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float p = 1f - Projectile.Opacity;
            //两道交叉斩痕:金色一道、紫色一道,先短后长再淡出
            float len = 56f * Projectile.scale * (0.4f + 0.6f * (float)Math.Sin(Math.Min(p * 2f, 1f) * MathHelper.PiOver2));
            float width = 9f * Projectile.scale * Projectile.Opacity;
            Vector2 c = Projectile.Center;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 0; i < 2; i++) {
                bool light = i == 0;
                float rot = (light ? MathHelper.PiOver4 : -MathHelper.PiOver4) + rotSeed;
                Vector2 d = rot.ToRotationVector2() * len;
                Color col = LDPalette.Tone(light) * Projectile.Opacity;
                CEUtils.drawLine(c - d, c + d, col, width);
                CEUtils.drawLine(c - d, c + d, LDPalette.EdgeTone(light) * Projectile.Opacity, width * 0.35f);
            }
            Texture2D star = CEUtils.getExtraTex("Star2");
            Main.EntitySpriteDraw(star, c - Main.screenPosition, null, Color.White * Projectile.Opacity * 0.8f, rotSeed, star.Size() / 2f, 0.6f * Projectile.scale * Projectile.Opacity, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    /// <summary>
    /// 闪烁路径斩:从出发点到落点(ai[0], ai[1])的一条线段判定,前几帧可造成伤害,命中者受强化光影斩切。
    /// </summary>
    public class LDBlinkStrike : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 14;
        public const int HitLineWidth = 46;

        private Vector2 End => new Vector2(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 2;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Life;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Vector2 delta = End - Projectile.Center;
                int count = (int)MathHelper.Clamp(delta.Length() / 14f, 6, 60);
                for (int i = 0; i <= count; i++) {
                    Vector2 pos = Projectile.Center + delta * (i / (float)count);
                    bool light = i % 2 == 0;
                    Vector2 side = new Vector2(-delta.Y, delta.X).SafeNormalize(Vector2.Zero) * (float)Math.Sin(i * 0.6f) * 10f;
                    PRTLoader.NewParticle<PRT_Light>(pos + side, delta.SafeNormalize(Vector2.Zero) * 2f, LDPalette.Tone(light), Main.rand.NextFloat(0.5f, 0.8f)).Configure(0.9f, lifetime: 18);
                }
            }
            Projectile.Opacity = Projectile.timeLeft / (float)Life;
        }

        public override bool? CanDamage() => Projectile.timeLeft > Life - 6;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, End, targetHitbox, HitLineWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Ichor, 180);
            target.AddBuff(BuffID.ShadowFlame, 180);
            Projectile.GetOwner().GetModPlayer<LDDaggerPlayer>().RegisterHit(hit.Crit);
            if (Main.myPlayer == Projectile.owner) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<LDSlash>(), Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI, 1f);
            }
            LDPalette.HitBurst(target.Center, true, 1.4f);
            LDPalette.HitBurst(target.Center, false, 1.4f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 start = Projectile.Center;
            Vector2 end = End;
            float a = Projectile.Opacity;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            CEUtils.drawLine(start, end, Color.White * a * 0.9f, 6f * a);
            Vector2 side = new Vector2(-(end - start).Y, (end - start).X).SafeNormalize(Vector2.Zero) * 7f;
            CEUtils.drawLine(start + side, end + side, LDPalette.LightMain * a * 0.8f, 4f * a);
            CEUtils.drawLine(start - side, end - side, LDPalette.DarkMain * a * 0.8f, 4f * a);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    /// <summary>
    /// 织影裂隙:闪烁路径上留下的线状裂隙,持续一段时间周期性伤害经过的敌怪并施加灵液与暗影焰。
    /// 出发点为弹幕中心,落点在 ai[0]/ai[1]。
    /// </summary>
    public class LDShadowRift : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 96;
        public const int FadeFrames = 30;
        public const int HitLineWidth = 36;
        public const int HitCooldown = 18;

        private Vector2 End => new Vector2(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 2;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.timeLeft = Life;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Projectile.Opacity = Math.Min(1f, Projectile.timeLeft / (float)FadeFrames);
            Vector2 delta = End - Projectile.Center;
            float len = delta.Length();
            if (len < 1f) {
                return;
            }
            Vector2 unit = delta / len;
            Vector2 side = new Vector2(-unit.Y, unit.X);
            //裂隙两侧各飘一缕光尘,金紫交替
            for (int i = 0; i < 2; i++) {
                bool light = i == 0;
                Vector2 pos = Projectile.Center + unit * Main.rand.NextFloat(len) + side * (light ? 6f : -6f);
                PRTLoader.NewParticle<PRT_Light>(pos, side * (light ? 0.8f : -0.8f) + CEUtils.randomPointInCircle(0.4f), LDPalette.Tone(light) * Projectile.Opacity, Main.rand.NextFloat(0.25f, 0.45f)).Configure(0.8f, lifetime: 20);
            }
            Lighting.AddLight(Projectile.Center + delta * 0.5f, new Vector3(0.5f, 0.3f, 0.7f) * Projectile.Opacity);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, End, targetHitbox, HitLineWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Ichor, 120);
            target.AddBuff(BuffID.ShadowFlame, 120);
            LDPalette.HitBurst(target.Center, Main.rand.NextBool(), 0.7f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Vector2 start = Projectile.Center;
            Vector2 end = End;
            float a = Projectile.Opacity;
            float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);
            Vector2 side = new Vector2(-(end - start).Y, (end - start).X).SafeNormalize(Vector2.Zero);
            //暗底:非预乘黑带,让裂隙看起来是空间被划开
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            CEUtils.drawLine(start, end, new Color(10, 0, 20) * (0.75f * a), 22f * a * pulse);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            CEUtils.drawLine(start + side * 9f * a, end + side * 9f * a, LDPalette.LightMain * a * pulse, 3f);
            CEUtils.drawLine(start - side * 9f * a, end - side * 9f * a, LDPalette.DarkMain * a * pulse, 3f);
            CEUtils.drawLine(start, end, new Color(120, 60, 200) * (0.5f * a), 10f * a * pulse);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
