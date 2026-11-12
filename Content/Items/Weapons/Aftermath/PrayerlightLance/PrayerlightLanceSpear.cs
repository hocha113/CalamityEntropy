using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PrayerlightLance
{
    /// <summary>
    /// 光枪,每刺一支,每个敌人只吃一次。ai[0] 出手时的长矛伤害(激光按它折算);
    /// ai[2] 命中标记(序号 × 256 + NPC 下标),其他端据此补放命中火花
    /// </summary>
    public class PrayerlightLanceSpear : ModProjectile
    {
        public const int ExtraUpdates = 1;
        //帧,射程约 940 像素
        public const int LifeFrames = 36;
        public const int FadeFrames = 6;
        //贴图长度,像素;判定线从尾到尖
        public const float Length = 80f;
        public const int HitLineWidth = 18;
        //拖尾点数,每次更新一点
        public const int TrailPoints = 10;
        public const float TrailWidth = 16f;
        public const int HitMarkerStride = 256;

        private int laserHits;
        private int hitSerial;
        private int seenMarker;
        private readonly List<Vector2> trail = new();
        private static readonly List<Vector2> scratch = new();

        private Vector2 Dir => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private float Fade => MathHelper.Clamp(Projectile.timeLeft / (float)(FadeFrames * (ExtraUpdates + 1)), 0f, 1f);

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = PrayerlightLance.SpearPenetrate;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeFrames * (ExtraUpdates + 1);
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            trail.Add(Projectile.Center);
            if (trail.Count > TrailPoints) {
                trail.RemoveAt(0);
            }
            ReadHitMarker();
            Lighting.AddLight(Projectile.Center, SunVfx.Gold.ToVector3() * 0.55f * Fade);
            if (!Main.dedServ && Main.rand.NextBool(4)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.SolarFlare, -Projectile.velocity * 0.1f, 0, default, 0.8f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 half = Dir * (Length * 0.5f);
            return CEUtils.LineThroughRect(Projectile.Center - half, Projectile.Center + half, targetHitbox, HitLineWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (laserHits < PrayerlightLance.SpearLaserHits) {
                laserHits++;
                int damage = Math.Max(1, (int)(Projectile.ai[0] * PrayerlightLance.LaserDamageMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<PrayerlightLanceLaser>(), damage, 0f, Projectile.owner, target.whoAmI, target.type);
            }
            hitSerial++;
            seenMarker = hitSerial * HitMarkerStride + target.whoAmI;
            Projectile.ai[2] = seenMarker;
            Projectile.netUpdate = true;
            HitFx(target);
        }

        private void ReadHitMarker() {
            int marker = (int)Projectile.ai[2];
            if (marker == seenMarker) {
                return;
            }
            seenMarker = marker;
            int index = marker % HitMarkerStride;
            if (marker > 0 && index < Main.maxNPCs && Main.npc[index].active) {
                HitFx(Main.npc[index]);
            }
        }

        private void HitFx(NPC target) {
            if (Main.dedServ) {
                return;
            }
            Vector2 at = Vector2.Clamp(Projectile.Center + Dir * (Length * 0.5f), target.TopLeft, target.BottomRight);
            CEUtils.PlaySound("LightHit", Main.rand.NextFloat(1.1f, 1.3f), at, 4, 0.35f);
            for (int i = 0; i < 5; i++) {
                Color c = i % 2 == 0 ? SunVfx.Core : SunVfx.Gold;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(at, Dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(6f, 14f), c, Main.rand.NextFloat(0.03f, 0.045f)).Configure(false, 9, new Vector2(2.4f, 0.8f), true);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Color c = Color.Lerp(SunVfx.Gold, SunVfx.Core, Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_SparkCal>(Projectile.Center, Main.rand.NextVector2Circular(4f, 4f) + Projectile.velocity * 0.3f, c, Main.rand.NextFloat(0.5f, 0.8f)).Configure(false, 16);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            float fade = Fade;
            VDWeaponFx.BeginAdditive();
            if (trail.Count > 1) {
                scratch.Clear();
                for (int i = trail.Count - 1; i >= 0; i--) {
                    scratch.Add(trail[i]);
                }
                VDWeaponFx.Ribbon(scratch, t => TrailWidth * (1f - t * 0.7f), t => Color.Lerp(SunVfx.Gold, SunVfx.Ember, t) * (0.6f * (1f - t) * fade));
            }
            VDWeaponFx.GlowStretched(Projectile.Center, SunVfx.Gold * (0.5f * fade), Projectile.rotation, Length * 1.6f, 34f);
            VDWeaponFx.End();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * fade, Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
