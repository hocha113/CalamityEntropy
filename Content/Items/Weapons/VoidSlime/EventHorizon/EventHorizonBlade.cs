using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.EventHorizon
{
    //挥剑发出的白边黑剑,直线飞行带残影,不穿透;命中时生成引力冲击和三柄剑形幻影
    public class EventHorizonBlade : ModProjectile
    {
        //时间单位次更新,extraUpdates 1 时每帧两次,80 次约 40 帧、飞 1200 像素
        private const int ExtraUpdates = 1;
        private const int LifetimeUpdates = 80;
        private const int TrailLength = 10;
        //判定线: 剑尖在中心前 28 像素,剑柄在后 24 像素,线宽 16 像素
        private const float TipReach = 28f;
        private const float TailReach = 24f;
        private const int HitLineWidth = 16;
        //尾迹火花每帧出现的概率分母与寿命帧数,消失时迸出的火花数
        private const int SparkChance = 2;
        private const int SparkLife = 12;
        private const int BurstCount = 8;
        private const int BurstLife = 16;
        //飞行音效的同时实例上限与音量,每秒 3 发
        private const int SoundInstances = 4;
        private const float SoundVolume = 0.45f;

        private static readonly Color GlowColor = new(223, 248, 255);

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifetimeUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                CEUtils.PlaySound("darkbladespawn", Main.rand.NextFloat(1.05f, 1.2f), Projectile.Center, SoundInstances, CEUtils.WeapSound * SoundVolume);
            }
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.35f);
            if (!Main.dedServ && Projectile.FinalExtraUpdate() && Main.rand.NextBool(SparkChance)) {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + back * TailReach + Main.rand.NextVector2Circular(6f, 6f), back * 2f, GlowColor * 0.8f, 0.03f)
                    .Configure(false, SparkLife, new Vector2(0.6f, 1.2f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            return CEUtils.LineThroughRect(Projectile.Center - dir * TailReach, Projectile.Center + dir * TipReach, targetHitbox, HitLineWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EventHorizon.SpawnTerraBladeHit(target, Projectile.velocity, Projectile.owner);
            IEntitySource source = Projectile.GetSource_FromThis();
            //黑剑伤害已是面板的 75%,按比例换算回各自的面板倍率
            int pulseDamage = (int)(Projectile.damage * EventHorizon.PulseDamageMult / EventHorizon.BladeDamageMult);
            int phantomDamage = (int)(Projectile.damage * EventHorizon.PhantomDamageMult / EventHorizon.BladeDamageMult);
            Projectile.NewProjectile(source, target.Center, Vector2.Zero, ModContent.ProjectileType<EventHorizonPulse>(), pulseDamage, Projectile.knockBack, Projectile.owner);
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < EventHorizonPhantom.Count; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / EventHorizonPhantom.Count;
                Vector2 spawn = target.Center + angle.ToRotationVector2() * EventHorizonPhantom.SpawnDistance;
                Projectile.NewProjectile(source, spawn, Vector2.Zero, ModContent.ProjectileType<EventHorizonPhantom>(), phantomDamage, Projectile.knockBack, Projectile.owner, target.whoAmI, i * EventHorizonPhantom.StaggerFrames, target.type);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            for (int i = 0; i < BurstCount; i++) {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + dir * TipReach, dir.RotatedByRandom(0.9f) * Main.rand.NextFloat(2f, 7f), GlowColor, 0.04f)
                    .Configure(false, BurstLife, new Vector2(0.5f, 1.4f));
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            Vector2 half = Projectile.Size / 2f;
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, Color.White * (0.45f * t), Projectile.oldRot[i], origin, Projectile.scale * (0.85f + 0.15f * t), SpriteEffects.None);
            }
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(GlowColor, 0.35f), Projectile.rotation, new Vector2(tex.Width * 1.6f, tex.Height * 2.4f) / glow.Width);
            GodDraw.End();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
