using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SunAloft
{
    //ai[0] 挥向 ±1, ai[1] 挥砍时长(帧), ai[2] 出手时蓄势已满为 1
    public class SunAloftSwing : ModProjectile
    {
        //时间单位帧, 距离单位 px; 曲线同真皎月刃: 前半程抛物线挥过 SwingArc 并过冲, 后半程余弦回摆
        public const int Updates = 4;
        public const float SwingArc = 3.8f;
        public const float Overshoot = MathHelper.Pi / 3f;
        public const float HitWindowEnd = 0.6f;
        public const float BladeLength = 92f;
        public const float SwingScale = 1.15f;
        public const int HitWidth = 56;
        public const float TrailInner = 0.25f;
        public const int TrailLength = 12;
        public const int HitStopFrames = 3;
        public const float HitShake = 5f;
        public const float ShakeRange = 1600f;
        public const float BladeHit = 1f;
        public const float FirstBladeHit = 2f;
        public const int FlareBurnFrames = 180;
        public const float BlastDamageMult = 0.5f;
        public const float RayDamageMult = 0.5f;
        public const float RayKnockback = 2f;
        public const float RayHeightMin = 600f;
        public const float RayHeightMax = 1400f;
        public const float RayAboveScreen = 120f;

        public static readonly Vector2 HoldPoint = new(25f, 67f);
        public static readonly Color DayTrailDeep = new(255, 150, 50);
        public static readonly Color DayTrailLight = new(255, 235, 150);
        public static readonly Color NightTrailDeep = new(80, 120, 255);
        public static readonly Color NightTrailLight = new(190, 225, 255);

        private readonly List<float> history = new();
        private float counter;
        private int stopTime;
        private bool hitStopUsed;
        private bool init = true;

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Aftermath/SunAloft/SunAloft";

        private float TotalTicks => Math.Max(1f, Projectile.ai[1]) * Updates;
        private float Progress => Math.Min(1f, counter / TotalTicks);
        private float Reach => BladeLength * Projectile.scale;

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = Updates;
        }

        public override void AI() {
            Player owner = Projectile.GetOwner();
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (init) {
                init = false;
                float meleeScale = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref meleeScale);
                Projectile.scale = SwingScale * meleeScale;
                CEUtils.PlaySound(Projectile.ai[0] > 0 ? "HellkiteSwing1" : "HellkiteSwing2", Main.rand.NextFloat(1.1f, 1.2f), owner.Center, 3, 0.8f * CEUtils.WeapSound);
            }
            if (stopTime > 0)
                stopTime--;
            else
                counter++;

            float progress = Progress;
            float dir = Projectile.ai[0];
            float baseRot = Projectile.velocity.ToRotation();
            if (progress <= 0.5f)
                Projectile.rotation = baseRot + (SwingArc * -0.5f + CEUtils.Parabola(progress, SwingArc + Overshoot)) * dir;
            else
                Projectile.rotation = baseRot + (SwingArc * 0.5f + Overshoot - CEUtils.GetRepeatedCosFromZeroToOne(2f * (progress - 0.5f), 1) * Overshoot) * dir;

            Projectile.Center = owner.MountedCenter;
            Projectile.timeLeft = 3;
            owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;

            if (stopTime <= 0) {
                history.Add(Projectile.rotation);
                if (history.Count > TrailLength)
                    history.RemoveAt(0);
            }
            Vector3 light = Main.dayTime ? new Vector3(1f, 0.75f, 0.3f) : new Vector3(0.45f, 0.6f, 1f);
            Lighting.AddLight(Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach * 0.7f, light);

            if (counter >= TotalTicks) {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Progress < HitWindowEnd ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach, targetHitbox, HitWidth);

        public override void CutTiles() {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Reach, HitWidth, DelegateMethods.CutTiles);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.GetOwner().Center.X ? 1 : -1;
        }

        //只在主人端结算; 子弹用玩家源生成, 不继承蓄势来源, 全端的命中声光与首击震屏由子弹首帧播放
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player owner = Projectile.GetOwner();
            float mark = hitStopUsed ? BladeHit : FirstBladeHit;
            if (!hitStopUsed) {
                hitStopUsed = true;
                stopTime = HitStopFrames * Updates;
            }
            IEntitySource source = owner.GetSource_FromThis();
            Vector2 sweep = (Projectile.rotation + MathHelper.PiOver2 * Projectile.ai[0]).ToRotationVector2();
            if (Main.dayTime) {
                target.AddBuff(ModContent.BuffType<FlareBurn>(), FlareBurnFrames);
                Projectile.NewProjectile(source, target.Center, sweep, ModContent.ProjectileType<SunAloftSolarBlast>(), (int)(Projectile.damage * BlastDamageMult), 0f, Projectile.owner, mark);
                return;
            }
            float height = Math.Clamp(target.Center.Y - Main.screenPosition.Y + RayAboveScreen, RayHeightMin, RayHeightMax);
            Projectile.NewProjectile(source, target.Center, new Vector2(0f, -height), ModContent.ProjectileType<SunAloftLunarRay>(), (int)(Projectile.damage * RayDamageMult), RayKnockback, Projectile.owner, 0f, mark);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            bool day = Main.dayTime;
            bool charged = Projectile.ai[2] == 1f;
            if (history.Count >= 2)
                DrawTrail(day, charged);

            int dir = Projectile.ai[0] > 0 ? 1 : -1;
            Vector2 origin = dir > 0 ? HoldPoint : new Vector2(tex.Width - HoldPoint.X, HoldPoint.Y);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = Projectile.rotation + (dir > 0 ? MathHelper.PiOver4 : MathHelper.Pi * 0.75f);
            Vector2 pos = Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition;
            if (charged) {
                Color glow = (day ? SunAloft.DayGlow : SunAloft.NightGlow) with { A = 0 };
                for (int i = 0; i < 4; i++)
                    Main.EntitySpriteDraw(tex, pos + (MathHelper.PiOver2 * i).ToRotationVector2() * 2f, null, glow * 0.8f, rot, origin, Projectile.scale, effect);
            }
            Main.EntitySpriteDraw(tex, pos, null, Color.Lerp(lightColor, Color.White, 0.35f), rot, origin, Projectile.scale, effect);
            return false;
        }

        private void DrawTrail(bool day, bool charged) {
            float alpha = Math.Max(0f, (0.5f - Progress) / 0.5f);
            if (alpha <= 0f)
                return;
            List<ColoredVertex> ve = new();
            Vector2 center = Projectile.Center - Main.screenPosition;
            for (int i = 0; i < history.Count; i++) {
                float t = i / (float)(history.Count - 1);
                Vector2 dir = history[i].ToRotationVector2();
                ve.Add(new ColoredVertex(center + dir * Reach, new Vector3(t, 1, 1), Color.White));
                ve.Add(new ColoredVertex(center + dir * Reach * TrailInner, new Vector3(t, 0, 1), Color.White));
            }
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            Effect shader = CEEffectAssets.SwordTrail2;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            shader.Parameters["color2"].SetValue((day ? DayTrailLight : NightTrailLight).ToVector4());
            shader.Parameters["color1"].SetValue((day ? DayTrailDeep : NightTrailDeep).ToVector4());
            shader.Parameters["alpha"].SetValue(alpha * (charged ? 1f : 0.75f));
            shader.CurrentTechnique.Passes["EffectPass"].Apply();
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 16 * Projectile.ai[0]);
            gd.Textures[1] = CEExtraAssets.B1;
            gd.Textures[0] = CEExtraAssets.MotionTrail2;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            sb.ExitShaderRegion();
        }
    }
}
