using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.GoldbowSilvershot
{
    /// <summary>
    /// 穿山碎峦的金弹。ai[0] 可命中的目标数,ai[1] 蓄力档位 0-1,只影响拖尾、声音与震屏
    /// 极快,穿敌不穿墙,每个敌人只吃一次
    /// </summary>
    public class GoldbowSilvershotGoldShot : ModProjectile
    {
        public const float Speed = 20f;                 //每次更新移动的距离,像素
        private const int ExtraUpdates = 7;             //每帧额外更新次数,合计每帧 160 像素
        private const int LifeUpdates = 120;            //存活的更新次数
        public const float Range = Speed * LifeUpdates; //射程,像素
        private const int Size = 12;                    //碰撞箱边长,像素,同贴图
        private const int TrailLength = 14;             //拖尾记录的位置数,每次更新记一个
        private const float TrailMinWidth = 6f;         //拖尾头部宽度,像素,不小于弹丸的一半,随蓄力加粗
        private const float TrailMaxWidth = 10f;
        private const float TrailOpacity = 0.85f;       //拖尾头部不透明度
        private const float CoreWidthRatio = 0.4f;      //拖尾白芯占拖尾宽度的比例
        private const float HeadGlowMinSize = 24f;      //弹头光晕直径,像素,随蓄力变大
        private const float HeadGlowMaxSize = 48f;
        private const float HeadGlowOpacity = 0.7f;
        private const int HitSparks = 5;                //每穿过一个敌人迸出的火花数
        private const int DeathSparks = 10;             //消失时迸出的火花数
        private const float SparkSpread = 0.6f;         //火花方向偏离弹道的上限,弧度
        private const float SparkSpeedMin = 2.5f;       //火花初速,像素每帧
        private const float SparkSpeedMax = 8f;
        private const float SparkScale = 0.8f;          //火花缩放
        private const int SparkLifetime = 16;           //火花寿命,帧
        private const float BowPitch = -0.25f;          //弓声音高
        private const float ShotVolumeMin = 0.35f;      //狙击声音量,随蓄力加大
        private const float ShotVolumeMax = 0.9f;
        private const float ShotPitchMin = 0.25f;       //狙击声音高,随蓄力变沉
        private const float ShotPitchMax = -0.2f;
        private const float HitVolumeMin = 0.4f;        //命中声音量,随蓄力加大
        private const float HitVolumeMax = 0.8f;
        private const float HitPitchMin = 0.9f;         //命中声音高,1 为原调,每次在这个范围里随机
        private const float HitPitchMax = 1.1f;
        private const int SoundInstances = 2;           //同一声音同时最多几个
        private const float MaxShake = 6f;              //满蓄时的屏幕震动强度,随蓄力线性减小
        private const float ShakeRange = 1600f;         //震屏的最远距离,像素
        private static readonly Vector3 LightRGB = new Vector3(0.9f, 0.7f, 0.25f);    //照明,RGB 0-1
        private static readonly Color TrailColor = new Color(255, 190, 64);
        private static readonly Color CoreColor = new Color(255, 248, 200);

        private bool started;   //各端各记,首次更新时播放放箭声与震屏
        private readonly bool[] touched = new bool[Main.maxNPCs];  //各端各记,已经迸过命中火花的敌人;命中回调只在拥有者端,火花要各端都看得见

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        //只在生成它的拥有者端调用,穿透只在这一端生效
        public override void OnSpawn(IEntitySource source) {
            Projectile.penetrate = Math.Max(1, (int)Projectile.ai[0]);
        }

        public override void AI() {
            float tier = Projectile.ai[1];
            if (!started) {
                started = true;
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item5 with { Pitch = BowPitch, MaxInstances = SoundInstances }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item40 with { Volume = MathHelper.Lerp(ShotVolumeMin, ShotVolumeMax, tier), Pitch = MathHelper.Lerp(ShotPitchMin, ShotPitchMax, tier), MaxInstances = SoundInstances }, Projectile.Center);
                    if (tier > 0f) {
                        CEUtils.SetShake(Projectile.Center, MaxShake * tier, ShakeRange);
                    }
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, LightRGB);
            if (!Main.dedServ) {
                SparkOnTouch(tier);
            }
        }

        //按碰撞箱判断穿过了谁,与真正的命中可能差一两个,只用于火花与命中声
        private void SparkOnTouch(float tier) {
            Rectangle hitbox = Projectile.Hitbox;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (touched[npc.whoAmI] || npc.friendly || npc.dontTakeDamage || !hitbox.Intersects(npc.Hitbox)) {
                    continue;
                }
                touched[npc.whoAmI] = true;
                Burst(npc.Center, HitSparks);
                CEUtils.PlaySound("ystn_hit", Main.rand.NextFloat(HitPitchMin, HitPitchMax), npc.Center, SoundInstances, MathHelper.Lerp(HitVolumeMin, HitVolumeMax, tier));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft) {
            Burst(Projectile.Center, DeathSparks);
        }

        private void Burst(Vector2 position, int count) {
            if (Main.dedServ) {
                return;
            }
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < count; i++) {
                Vector2 velocity = direction.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_SparkCal>(position, velocity, TrailColor, SparkScale).Configure(false, SparkLifetime);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            float tier = Projectile.ai[1];
            float width = MathHelper.Lerp(TrailMinWidth, TrailMaxWidth, tier);
            Color trail = TrailColor with { A = 0 };
            Color core = CoreColor with { A = 0 };
            Vector2 half = Projectile.Size * 0.5f;
            Vector2 previous = Projectile.Center;
            int length = Projectile.oldPos.Length;
            //拖尾与光晕都用 A=0 的颜色在默认批次里叠加,不切批次
            for (int i = 0; i < length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    break;
                }
                Vector2 point = Projectile.oldPos[i] + half;
                float fade = 1f - (float)i / length;
                CEUtils.drawLine(previous, point, trail * (TrailOpacity * fade), width * fade);
                CEUtils.drawLine(previous, point, core * (TrailOpacity * fade), width * fade * CoreWidthRatio);
                previous = point;
            }
            Texture2D glow = CEExtraAssets.Glow2;
            float glowSize = MathHelper.Lerp(HeadGlowMinSize, HeadGlowMaxSize, tier);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, trail * HeadGlowOpacity, 0f, glow.Size() * 0.5f, glowSize / glow.Width, SpriteEffects.None);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
