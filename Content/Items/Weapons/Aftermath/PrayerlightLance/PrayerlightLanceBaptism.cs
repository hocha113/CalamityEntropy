using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Goddess;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.PrayerlightLance
{
    /// <summary>
    /// 圣光洗礼:跟随拥有者的同步鼠标,拥有者端每 12 帧在半径内挑本次被打得最少的目标降一道神圣激光,
    /// 没有目标就空打在圈内;3 秒共 15 道。自身不造成伤害,只把伤害传给激光
    /// </summary>
    public class PrayerlightLanceBaptism : ModProjectile
    {
        //每帧向鼠标靠拢的比例
        public const float FollowLerp = 0.25f;
        public const int FadeFrames = 12;
        //空打落点占半径的比例
        public const float IdleScatter = 0.6f;
        //每帧冒出光尘的概率
        public const float MoteChance = 0.6f;

        private int age;
        //仅拥有者端:NPC 下标 → 本次洗礼已降下的激光数
        private readonly Dictionary<int, int> strikes = new();

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        private float Alpha => MathHelper.Clamp(Math.Min(age / (float)FadeFrames, (PrayerlightLance.BaptismFrames - age) / (float)FadeFrames), 0f, 1f);

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = PrayerlightLance.BaptismFrames + 2;
            Projectile.aiStyle = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            age++;
            if (age > PrayerlightLance.BaptismFrames) {
                Projectile.Kill();
                return;
            }
            owner.Entropy().MouseWorldListener = true;
            Projectile.Center = Vector2.Lerp(Projectile.Center, owner.Entropy().MouseWorld, FollowLerp);
            if (Projectile.owner == Main.myPlayer && (age - 1) % PrayerlightLance.BaptismInterval == 0) {
                Pulse();
            }
            Lighting.AddLight(Projectile.Center, SunVfx.Gold.ToVector3() * (0.8f * Alpha));
            Motes();
        }

        /// <summary>训练假人不算可追踪目标,单独放行方便试伤害</summary>
        private void Pulse() {
            NPC best = null;
            int bestCount = int.MaxValue;
            float bestDist = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile) && npc.type != NPCID.TargetDummy) {
                    continue;
                }
                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist > PrayerlightLance.BaptismRadius) {
                    continue;
                }
                strikes.TryGetValue(npc.whoAmI, out int count);
                if (count < bestCount || (count == bestCount && dist < bestDist)) {
                    best = npc;
                    bestCount = count;
                    bestDist = dist;
                }
            }
            int type = ModContent.ProjectileType<PrayerlightLanceLaser>();
            if (best == null) {
                float scatter = PrayerlightLance.BaptismRadius * IdleScatter;
                Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(scatter, scatter);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero, type, 0, 0f, Projectile.owner, -1f);
                return;
            }
            strikes[best.whoAmI] = bestCount + 1;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), best.Center, Vector2.Zero, type, Projectile.damage, 0f, Projectile.owner, best.whoAmI, best.type);
        }

        private void Motes() {
            if (Main.dedServ || Main.rand.NextFloat() >= MoteChance * Alpha) {
                return;
            }
            float r = PrayerlightLance.BaptismRadius;
            Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(r, r);
            Vector2 v = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(1.5f, 3.5f));
            Color c = Color.Lerp(SunVfx.Gold, SunVfx.Core, Main.rand.NextFloat());
            PRTLoader.NewParticle<PRT_SparkCal>(at, v, c, Main.rand.NextFloat(0.4f, 0.7f)).Configure(false, 30);
        }

        public override bool PreDraw(ref Color lightColor) {
            float alpha = Alpha;
            if (alpha <= 0.01f) {
                return false;
            }
            float r = PrayerlightLance.BaptismRadius;
            //光圈轻微呼吸
            float breathe = 1f + 0.03f * MathF.Sin(age * 0.2f);
            VDWeaponFx.BeginAdditive();
            VDWeaponFx.Glow(Projectile.Center, SunVfx.Gold * (0.18f * alpha), r * 0.9f);
            VDWeaponFx.Ring(Projectile.Center, SunVfx.Gold * (0.45f * alpha), r * breathe);
            VDWeaponFx.Ring(Projectile.Center, SunVfx.Ember * (0.25f * alpha), r * 0.62f * breathe);
            VDWeaponFx.Glow(Projectile.Center, SunVfx.Core * (0.3f * alpha), 36f);
            VDWeaponFx.End();
            return false;
        }
    }
}
