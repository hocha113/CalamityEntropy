using CalamityEntropy.Content.Items.Armor.Sunblaze;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SatelliteStaff
{
    /// <summary>耀斑射线第一次命中时的爆炸:观感照原版破晓爆炸(953),自建弹幕,伤害类型与归属是召唤;每个敌人只吃一次</summary>
    public class SatelliteStaffExplosion : ModProjectile
    {
        #region 调参旋钮
        //原版破晓爆炸贴图 5 帧,每帧 52 像素见方,每 3 帧换一帧
        public const int Frames = 5;
        public const int FrameSize = 52;
        public const int FrameTicks = 3;
        //放大倍数(判定同比例)、有判定的前几帧
        public const float Scale = 1.4f;
        public const int DamageTicks = 6;
        //烈日灼烧时长(帧,3秒,策划)
        public const int BurnFrames = 180;
        //爆开时的火尘:数量、缩放、速度倍率;烟尘:数量、缩放、速度倍率;两者的透明度(0..255)
        public const int FireDust = 14;
        public const float FireScale = 2.2f;
        public const float FireSpeed = 4f;
        public const int SmokeDust = 6;
        public const float SmokeScale = 1.5f;
        public const float SmokeSpeed = 1.4f;
        public const int DustAlpha = 100;
        //爆炸音效的音量、音高随机幅度与同时发声上限
        public const float SoundVolume = 0.6f;
        public const float SoundPitchVariance = 0.2f;
        public const int SoundMaxInstances = 3;
        #endregion

        //照明色;绘制色带一半 alpha,照原版爆炸半加色的画法
        public static readonly Vector3 LightColor = new(0.9f, 0.8f, 0.6f);
        public static readonly Color DrawColor = new(255, 255, 255, 127);

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DaybreakExplosion;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = (int)(FrameSize * Scale);
            Projectile.scale = Scale;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Frames * FrameTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => Projectile.timeLeft > Frames * FrameTicks - DamageTicks ? null : false;

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Burst();
            }
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = Math.Min(Projectile.frame + 1, Frames - 1);
            }
            Lighting.AddLight(Projectile.Center, LightColor);
        }

        private void Burst() {
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = SoundVolume, PitchVariance = SoundPitchVariance, MaxInstances = SoundMaxInstances }, Projectile.Center);
            for (int i = 0; i < SmokeDust; i++) {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, DustAlpha, default, SmokeScale);
                dust.velocity *= SmokeSpeed;
            }
            for (int i = 0; i < FireDust; i++) {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, DustAlpha, default, FireScale);
                dust.noGravity = true;
                dust.velocity *= FireSpeed;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<SunblazeBurn>(), BurnFrames);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / Frames;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, DrawColor, 0f, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
