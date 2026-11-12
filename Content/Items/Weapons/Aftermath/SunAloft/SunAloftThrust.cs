using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SunAloft
{
    //ai[0] 0 白天 1 夜晚; 出手在蓄势释放窗口内, 自带 Empowered 不回充
    public class SunAloftThrust : ModProjectile
    {
        //时间单位帧, 距离单位 px(192 px = 12 格)
        public const int Duration = 30;
        public const int ExtendFrames = 7;
        public const int StrikeFrame = 5;
        public const int HoldEndFrame = 18;
        public const int LanceFadeFrames = 8;
        public const float Lunge = 56f;
        public const float HandBack = 8f;
        public const float Reach = 192f;
        public const int HitWidth = 64;
        public const float LanceWidth = 40f;
        public const float StrikeShake = 9f;

        private int timer;
        private bool init = true;

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Aftermath/SunAloft/SunAloft";

        private bool Day => Projectile.ai[0] == 0f;
        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private Color Glow => Day ? SunAloft.DayGlow : SunAloft.NightGlow;

        private float Extension {
            get {
                if (timer <= ExtendFrames) {
                    float x = 1f - timer / (float)ExtendFrames;
                    return 1f - x * x * x;
                }
                if (timer <= HoldEndFrame)
                    return 1f;
                return Math.Max(0f, 1f - (timer - HoldEndFrame) / (float)(Duration - HoldEndFrame));
            }
        }

        private float LancePower {
            get {
                if (timer < StrikeFrame)
                    return 0f;
                if (timer <= HoldEndFrame)
                    return 1f;
                return Math.Max(0f, 1f - (timer - HoldEndFrame) / (float)LanceFadeFrames);
            }
        }

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
            Projectile.timeLeft = Duration + 10;
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
                Projectile.scale = SunAloftSwing.SwingScale * meleeScale;
                CEUtils.PlaySound("sf_use", 1.2f, owner.Center, 2, 0.7f * CEUtils.WeapSound);
            }
            timer++;
            Projectile.rotation = Dir.ToRotation();
            Projectile.Center = owner.MountedCenter;
            if (timer == StrikeFrame)
                StrikeVfx();
            owner.direction = Dir.X > 0 ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            Lighting.AddLight(Projectile.Center + Dir * Reach * 0.6f, Glow.ToVector3() * LancePower);
            if (timer >= Duration) {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }

        //各端自行扫一遍枪线上的敌人播命中火花, 不依赖只在主人端跑的 OnHitNPC
        private void StrikeVfx() {
            if (Main.dedServ)
                return;
            Vector2 dir = Dir;
            Vector2 start = Projectile.Center;
            Vector2 end = start + dir * Reach;
            CEUtils.PlaySound("sf_spear_attak", 1.1f, start, 2, 0.9f * CEUtils.WeapSound);
            CEUtils.SetShake(start, StrikeShake, SunAloftSwing.ShakeRange);
            for (int i = 0; i < 14; i++) {
                Vector2 pos = start + dir * Main.rand.NextFloat(40f, Reach);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(pos, dir.RotatedByRandom(0.12f) * Main.rand.NextFloat(10f, 22f), Glow, Main.rand.NextFloat(0.05f, 0.09f)).Configure(false, 12, new Vector2(2.6f, 0.5f), true);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(end, dir * 2f, Glow, 0.1f).Configure(new Vector2(0.4f, 1f), dir.ToRotation(), 1.4f, 16);
            PRTLoader.NewParticle<PRT_SparkleCal>(end, Vector2.Zero, Color.White, 1.6f).Configure(Glow, 14, 0f, 2.4f);
            bool struck = false;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.friendly || npc.dontTakeDamage || !CEUtils.LineThroughRect(start, end, npc.Hitbox, HitWidth))
                    continue;
                struck = true;
                for (int i = 0; i < 8; i++)
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(npc.Center, dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(12f, 24f), Glow, Main.rand.NextFloat(0.05f, 0.08f)).Configure(false, 12, new Vector2(0.3f, 1f), false, false);
            }
            if (struck)
                CEUtils.PlaySound("spearImpact", Main.rand.NextFloat(1f, 1.15f), end, 3, 0.7f * CEUtils.WeapSound);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => timer >= StrikeFrame && timer <= HoldEndFrame ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Dir * Reach, targetHitbox, HitWidth);

        public override void CutTiles() {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Dir * Reach, HitWidth, DelegateMethods.CutTiles);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.HitDirectionOverride = Dir.X > 0 ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Day)
                target.AddBuff(ModContent.BuffType<FlareBurn>(), SunAloftSwing.FlareBurnFrames);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 dir = Dir;
            Vector2 hand = Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY + dir * (Lunge * Extension - HandBack);
            float power = LancePower;
            if (power > 0f) {
                Vector2 from = hand + dir * SunAloftSwing.BladeLength * Projectile.scale * 0.5f;
                Vector2 to = Projectile.Center + dir * Reach;
                float len = Math.Max(1f, Vector2.Distance(from, to));
                Texture2D glow = CEUtils.getExtraTex("Glow");
                Texture2D px = CEUtils.getExtraTex("white");
                Texture2D star = CEUtils.getExtraTex("StarTexture");
                Vector2 mid = (from + to) / 2f - Main.screenPosition;
                float rot = dir.ToRotation();
                Main.EntitySpriteDraw(glow, mid, null, (Glow with { A = 0 }) * 0.8f * power, rot, glow.Size() / 2f, new Vector2(len * 1.2f / glow.Width, LanceWidth * 2.2f / glow.Height), SpriteEffects.None);
                Main.EntitySpriteDraw(px, from - Main.screenPosition, null, Color.Lerp(Glow, Color.White, 0.6f) * power, rot, new Vector2(0f, 0.5f), new Vector2(len, LanceWidth * 0.18f * power), SpriteEffects.None);
                Main.EntitySpriteDraw(star, to - Main.screenPosition, null, (Glow with { A = 0 }) * power, rot, star.Size() / 2f, new Vector2(0.5f, 0.3f) * power, SpriteEffects.None);
            }
            bool left = dir.X < 0;
            Vector2 origin = left ? new Vector2(tex.Width - SunAloftSwing.HoldPoint.X, SunAloftSwing.HoldPoint.Y) : SunAloftSwing.HoldPoint;
            float spriteRot = Projectile.rotation + (left ? MathHelper.Pi * 0.75f : MathHelper.PiOver4);
            Main.EntitySpriteDraw(tex, hand - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.35f), spriteRot, origin, Projectile.scale, left ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            return false;
        }
    }
}
