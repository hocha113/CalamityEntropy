using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.EventHorizon
{
    //黑剑命中后在目标周围现身的剑形幻影,停顿后穿刺,只伤那个目标
    //ai0 目标 whoAmI,ai1 现身延迟帧数,ai2 目标 type(防槽位被别的 NPC 顶替)
    public class EventHorizonPhantom : ModProjectile
    {
        //策划: 3 柄;现身处离目标中心的距离,单位像素
        public const int Count = 3;
        public const float SpawnDistance = 120f;
        //时间单位帧: 相邻两柄错开 4 帧现身,淡入 8 帧,现身后第 14 帧起刺,淡出 8 帧
        public const int StaggerFrames = 4;
        private const int FadeInFrames = 8;
        private const int HoldFrames = 14;
        private const int FadeOutFrames = 8;
        //停顿时每帧向目标收拢的比例
        private const float HoldApproach = 0.985f;
        //穿刺速度,单位像素每次更新,extraUpdates 1 时每帧 36 像素;刺穿后再冲 90 像素开始淡出,淡出时每次更新的减速
        private const int ExtraUpdates = 1;
        private const float ThrustSpeed = 18f;
        private const float Overshoot = 90f;
        private const float FadeDrag = 0.9f;
        //判定线半长与线宽,单位像素
        private const float BladeHalfLength = 28f;
        private const int HitLineWidth = 18;
        //保险寿命,单位次更新
        private const int MaxLifetime = 600;
        //刺穿时迸出的火花数与寿命帧数;穿刺音效的同时实例上限与音量
        private const int PierceSparks = 6;
        private const int SparkLife = 14;
        private const int SoundInstances = 3;
        private const float SoundVolume = 0.35f;

        private static readonly Color BodyTint = new(120, 126, 150);
        private static readonly Color GlowColor = new(196, 228, 255);

        private bool initialized;
        private int age;
        private Vector2 offset;
        private bool thrusting;
        private bool fading;
        //刺穿后已冲出的距离,负数表示还没刺穿
        private float overshootTravel = -1f;
        private float opacity;

        public override string Texture => "CalamityEntropy/Content/Items/Weapons/VoidSlime/EventHorizon/EventHorizonBlade";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = MaxLifetime;
        }

        private NPC Target {
            get {
                int index = (int)Projectile.ai[0];
                if (index < 0 || index >= Main.maxNPCs)
                    return null;
                NPC npc = Main.npc[index];
                return npc.active && npc.type == (int)Projectile.ai[2] ? npc : null;
            }
        }

        public override void AI() {
            NPC target = Target;
            if (!initialized) {
                initialized = true;
                offset = target != null ? Projectile.Center - target.Center : Vector2.UnitX * SpawnDistance;
                Projectile.rotation = (-offset).ToRotation();
            }
            bool frameTick = Projectile.FinalExtraUpdate();
            if (frameTick)
                age++;

            if (fading) {
                Projectile.velocity *= FadeDrag;
                if (frameTick)
                    opacity -= 1f / FadeOutFrames;
                if (opacity <= 0f)
                    Projectile.Kill();
                return;
            }

            if (!thrusting) {
                if (target == null) {
                    fading = true;
                    return;
                }
                if (frameTick)
                    offset *= HoldApproach;
                Projectile.Center = target.Center + offset;
                Projectile.rotation = (-offset).ToRotation();
                int appear = (int)Projectile.ai[1];
                opacity = MathHelper.Clamp((age - appear) / (float)FadeInFrames, 0f, 1f);
                if (age >= appear + HoldFrames)
                    BeginThrust(target);
                return;
            }

            if (overshootTravel < 0f) {
                Vector2 toTarget = target != null ? target.Center - Projectile.Center : Vector2.Zero;
                if (target == null || toTarget.Length() <= ThrustSpeed || Vector2.Dot(toTarget, Projectile.velocity) <= 0f) {
                    overshootTravel = 0f;
                    if (target != null)
                        SpawnPierceSparks();
                }
                else {
                    Projectile.velocity = toTarget.SafeNormalize(Vector2.UnitX) * ThrustSpeed;
                }
            }
            else {
                overshootTravel += ThrustSpeed;
                if (overshootTravel >= Overshoot)
                    fading = true;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        private void BeginThrust(NPC target) {
            thrusting = true;
            opacity = 1f;
            Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * ThrustSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.PlaySound("sn_swing", Main.rand.NextFloat(1.2f, 1.45f), Projectile.Center, SoundInstances, CEUtils.WeapSound * SoundVolume);
        }

        //刺穿的一瞬在所有客户端的 AI 里出火花,OnHitNPC 只在拥有者端跑
        private void SpawnPierceSparks() {
            if (Main.dedServ)
                return;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < PierceSparks; i++) {
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 9f), GlowColor, 0.04f)
                    .Configure(false, SparkLife, new Vector2(0.5f, 1.5f));
            }
        }

        public override bool? CanHitNPC(NPC target) => thrusting && target.whoAmI == (int)Projectile.ai[0] ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            return CEUtils.LineThroughRect(Projectile.Center - dir * BladeHalfLength, Projectile.Center + dir * BladeHalfLength, targetHitbox, HitLineWidth);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (opacity <= 0f)
                return false;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() / 2f;
            if (thrusting) {
                Vector2 half = Projectile.Size / 2f;
                for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float t = 1f - i / (float)Projectile.oldPos.Length;
                    Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, BodyTint * (0.35f * t * opacity), Projectile.oldRot[i], origin, Projectile.scale, SpriteEffects.None);
                }
            }
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(GlowColor, 0.3f * opacity), Projectile.rotation, new Vector2(tex.Width * 1.5f, tex.Height * 2.2f) / glow.Width);
            GodDraw.End();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, BodyTint * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
