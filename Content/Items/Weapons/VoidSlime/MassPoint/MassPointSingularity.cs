using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.MassPoint
{
    //跟随悠悠球的金边黑洞,悠悠球久未命中或已收回时快速塌缩消散
    //ai0 宿主悠悠球的 identity,ai1 为 1 表示在塌缩(拥有者端决定并同步)
    public class MassPointSingularity : ModProjectile
    {
        //策划: 半径 5 格,单位像素;局部无敌帧 6;悠悠球 3 秒没命中就塌缩,单位帧
        public const float Radius = 80f;
        public const int HitCooldown = 6;
        public const int IdleFrames = 180;
        //时间单位帧: 40 帧长成,20 帧塌缩完
        private const int GrowFrames = 40;
        private const int CollapseFrames = 20;
        //每帧把寿命续到这么多,何时消失由 AI 自己决定
        private const int KeepAliveFrames = 2;
        //长到这个比例才开判定,塌缩时不判定
        private const float MinDamageSize = 0.25f;
        //黑盘占旋流盘的比例与旋流转速,黑盘边缘落在判定半径上
        private const float CoreFraction = 0.6f;
        private const float Spin = 1.6f;
        //轻微扭曲走仓库奇点的 VDScreenFx 透镜,Boss 用 0.09 / 520
        private const float LensStrength = 0.06f;
        private const float LensRadius = 160f;
        //每帧吸入的火花数、出生半径倍数与寿命帧数;消散时迸出的火花数
        private const int InwardSparks = 2;
        private const float SparkSpawnReach = 1.6f;
        private const int SparkLife = 14;
        private const int BurstCount = 10;
        //成形与塌缩音效的同时实例上限与音量
        private const int SoundInstances = 2;
        private const float FormVolume = 0.5f;
        private const float CollapseVolume = 0.45f;

        private static readonly Color DiskColor = new(230, 150, 40);
        private static readonly Color HotColor = new(255, 214, 110);
        private static readonly Color HaloColor = new(91, 35, 159);

        private int growTimer;
        private int collapseTimer;
        private bool formSoundPlayed;
        private bool collapseSoundPlayed;

        private bool Collapsing => Projectile.ai[1] == 1f;

        private float Size {
            get {
                float grow = GodDraw.EaseOut(growTimer / (float)GrowFrames);
                return grow * (1f - GodDraw.EaseIn(collapseTimer / (float)CollapseFrames));
            }
        }

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.timeLeft = KeepAliveFrames;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        private Projectile FindYoyo() {
            int type = ModContent.ProjectileType<MassPointYoyo>();
            int identity = (int)Projectile.ai[0];
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == type && p.identity == identity)
                    return p;
            }
            return null;
        }

        public override void AI() {
            Projectile.timeLeft = KeepAliveFrames;
            Projectile yoyo = FindYoyo();
            if (yoyo != null)
                Projectile.Center = yoyo.Center;
            if (Projectile.owner == Main.myPlayer && !Collapsing) {
                if (yoyo?.ModProjectile is not MassPointYoyo host || host.FramesSinceHit >= IdleFrames) {
                    Projectile.ai[1] = 1f;
                    Projectile.netUpdate = true;
                }
            }
            if (Collapsing) {
                if (++collapseTimer >= CollapseFrames) {
                    Projectile.Kill();
                    return;
                }
            }
            else if (growTimer < GrowFrames) {
                growTimer++;
            }
            if (Main.dedServ)
                return;
            PlayCues();
            float size = Size;
            VDScreenFx.ReportLens(Projectile.Center, LensStrength * size, LensRadius * Math.Max(size, MinDamageSize));
            if (Collapsing)
                return;
            for (int i = 0; i < InwardSparks; i++) {
                Vector2 dir = Main.rand.NextVector2Unit();
                float reach = Radius * size * SparkSpawnReach * Main.rand.NextFloat(0.8f, 1.2f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + dir * reach, (-dir).RotatedBy(0.5f) * Main.rand.NextFloat(3f, 5f), HotColor * 0.8f, 0.035f)
                    .Configure(false, SparkLife, new Vector2(0.5f, 1.3f));
            }
        }

        private void PlayCues() {
            if (!formSoundPlayed) {
                formSoundPlayed = true;
                CEUtils.PlaySound("VoidAnticipation", 1.2f, Projectile.Center, SoundInstances, CEUtils.WeapSound * FormVolume);
            }
            if (Collapsing && !collapseSoundPlayed) {
                collapseSoundPlayed = true;
                CEUtils.PlaySound("voidSound", 1.3f, Projectile.Center, SoundInstances, CEUtils.WeapSound * CollapseVolume);
            }
        }

        public override bool? CanDamage() => !Collapsing && Size >= MinDamageSize ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => targetHitbox.Distance(Projectile.Center) <= Radius * Size;

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            for (int i = 0; i < BurstCount; i++) {
                Vector2 dir = Main.rand.NextVector2Unit();
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + dir * Radius * 0.3f, dir * Main.rand.NextFloat(3f, 7f), HotColor, 0.04f)
                    .Configure(false, SparkLife, new Vector2(0.5f, 1.4f));
            }
        }

        //画在 NPC 后面,黑盘不挡住被磨的敌人
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) {
            float size = Size;
            if (size <= 0.01f)
                return false;
            float radius = Radius * size;
            Texture2D glow = GodDraw.Glow;
            GodDraw.BeginAdditive();
            GodDraw.Centered(glow, Projectile.Center, GodDraw.Add(HaloColor, 0.7f * size), 0f, radius * 4.4f / glow.Width);
            GodDraw.End();
            DrawSwirl(Projectile.Center, radius / CoreFraction, CoreFraction, DiskColor, HotColor, Spin, size);
            //金边落在判定半径上
            GodDraw.BeginAdditive();
            GodDraw.RingStrip(GodDraw.SoftLine, Projectile.Center, radius, Math.Max(3f, radius * 0.05f), GodDraw.Add(HotColor, 0.9f * size), 0f, MathHelper.TwoPi, 64);
            GodDraw.End();
            return false;
        }

        //旋流盘走 VDSingularity,预乘输出配 AlphaBlend,黑盘是真遮挡;着色器缺失时退回光环加黑盘
        private static void DrawSwirl(Vector2 center, float half, float coreFraction, Color disk, Color hot, float spin, float opacity) {
            Effect shader = CEEffectAssets.VDSingularity?.Value;
            if (shader == null) {
                Texture2D disc = GodDraw.Disc;
                GodDraw.BeginAdditive();
                GodDraw.Centered(GodDraw.BloomRing, center, GodDraw.Add(disk, opacity), 0f, GodDraw.RingScale(half * 0.75f));
                GodDraw.End();
                Main.spriteBatch.Draw(disc, center - Main.screenPosition, null, Color.Black * opacity, 0f, disc.Size() / 2f, half * coreFraction * 2f / disc.Width, SpriteEffects.None, 0f);
                return;
            }
            Texture2D quad = CEUtils.getExtraTex("white");
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            Main.spriteBatch.EnterShaderRegion(BlendState.AlphaBlend, shader);
            gd.Textures[1] = CEUtils.getExtraTex("TurbulentNoise");
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uColor"]?.SetValue(disk.ToVector3());
            shader.Parameters["uColor2"]?.SetValue(hot.ToVector3());
            shader.Parameters["uCoreRadius"]?.SetValue(coreFraction);
            shader.Parameters["uOpacity"]?.SetValue(opacity);
            shader.Parameters["uSpin"]?.SetValue(spin);
            shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(quad, center - Main.screenPosition, null, Color.White, 0f, quad.Size() / 2f, half * 2f / quad.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            gd.Textures[1] = null;
        }
    }
}
