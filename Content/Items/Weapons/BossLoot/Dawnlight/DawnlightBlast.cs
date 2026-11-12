using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Dawnlight
{
    //晨曦之刃命中时的日耀爆炸,只由主人端生成;开头两帧结算圆形范围伤害,之后只播动画
    //ai[0]=命中时剑刃的指向(弧度),火星顺着它喷 localAI[0]=已存活帧数
    public class DawnlightBlast : ModProjectile
    {
        //判定圆直径6格、半径3格(像素)
        private const int Diameter = 96;
        private const float Radius = Diameter / 2f;
        //伤害只在开头几帧结算(帧)
        private const int DamageFrames = 2;
        //原版612贴图5帧,每帧停3游戏帧
        private const int FrameCount = 5;
        private const int FrameTicks = 3;
        private const int Lifetime = FrameCount * FrameTicks;
        //贴图主体约48像素,2倍即判定圆直径;每帧再涨0.01倍,同原版612
        private const float BaseScale = 2f;
        private const float ScaleGrowth = 0.01f;
        //爆闪:橙色大光晕与白热核心(Glow2原图256像素)、存活帧数
        private const float FlashScale = 0.45f;
        private const float CoreScale = 0.22f;
        private const int FlashLife = 6;
        //火星:数量、相邻两粒的夹角(弧度)、速度区间(像素/帧)、大小(GlowSpark原图2048像素)、存活帧数
        private const int SparkCount = 4;
        private const float SparkSpread = 0.4f;
        private const float SparkSpeedMin = 5f;
        private const float SparkSpeedMax = 9f;
        private const float SparkScale = 0.035f;
        private const int SparkLife = 10;
        //爆炸声用原版日耀爆炸同款,压低音量、调高音高;同时至多3个,多出的不播
        private const float SoundVolume = 0.35f;
        private const float SoundPitch = 0.3f;
        private const float SoundPitchRange = 0.2f;
        private const int SoundInstances = 3;

        //原版612的绘制色,半加色不受光照
        private static readonly Color SheetTint = new(255, 255, 255, 127);
        private static readonly Color FlashOrange = new(255, 120, 30);
        private static readonly Color FlashWhite = new(255, 245, 210);
        private static readonly Color SparkGold = new(255, 190, 70);
        private static readonly Vector2 SparkSquash = new(0.4f, 1f);
        private static readonly Vector3 BlastLight = new(0.9f, 0.55f, 0.2f);

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SolarWhipSwordExplosion;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Diameter;
            Projectile.height = Diameter;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() {
            if (Projectile.localAI[0] > DamageFrames)
                return false;
            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => EGlobalProjectile.CircleIntersectsRectangle(Projectile.Center, Radius, targetHitbox);

        public override bool CanHitPvp(Player target) => false;

        public override bool? CanCutTiles() => false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.rotation = Projectile.ai[0];
                if (!Main.dedServ)
                    Burst();
            }
            Projectile.frame = Math.Min((int)Projectile.localAI[0] / FrameTicks, FrameCount - 1);
            Projectile.scale = BaseScale + ScaleGrowth * Projectile.localAI[0];
            Projectile.localAI[0]++;
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, BlastLight * (Projectile.timeLeft / (float)Lifetime));
        }

        private void Burst() {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = SoundVolume, Pitch = SoundPitch, PitchVariance = SoundPitchRange, MaxInstances = SoundInstances, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, Projectile.Center);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, FlashOrange, FlashScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, FlashWhite, CoreScale).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, FlashLife);
            for (int i = 0; i < SparkCount; i++) {
                float angle = Projectile.rotation + (i - (SparkCount - 1) / 2f) * SparkSpread;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, velocity, SparkGold, SparkScale).Configure(false, SparkLife, SparkSquash, true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / FrameCount;
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, SheetTint, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
