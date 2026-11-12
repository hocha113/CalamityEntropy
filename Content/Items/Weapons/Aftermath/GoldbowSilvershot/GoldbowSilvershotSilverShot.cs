using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.GoldbowSilvershot
{
    /// <summary>
    /// 梨花暴雨的银弹。ai[0] 本轮里的序号,序号 0 负责播放这一轮的声音
    /// 不穿透,每帧受一点重力,只追前方 60° 内的敌人且每帧最多转 1.5°
    /// </summary>
    public class GoldbowSilvershotSilverShot : ModProjectile
    {
        public const float Speed = 14f;                 //初速,像素每帧
        private const int LifeFrames = 90;              //存活帧数
        private const int Size = 8;                     //碰撞箱边长,像素,同贴图
        private const float Gravity = 0.08f;            //每帧竖直方向增加的速度,像素每帧
        private const float HomingRange = 480f;         //追踪距离,像素
        private const float HomingHalfCone = MathHelper.Pi / 6f;    //只追速度方向两侧这么大角度内的敌人,弧度,合计前方 60°
        private const float HomingTurn = MathHelper.Pi / 120f;      //每帧最多转向,弧度,即 1.5°
        private const int TrailLength = 6;              //拖尾记录的位置数
        private const float TrailWidth = 4f;            //拖尾头部宽度,像素,不小于弹丸的一半
        private const float TrailOpacity = 0.6f;        //拖尾头部不透明度
        private const float HeadGlowSize = 16f;         //弹头光晕直径,像素
        private const float HeadGlowOpacity = 0.45f;
        private const float SelfLight = 0.5f;           //贴图颜色在环境光与纯白之间取的比例,暗处也看得见
        private const int DeathDust = 4;                //消失时的粉尘数
        private const float DeathDustSpeed = 2f;        //消失粉尘初速上限,像素每帧
        private const float BowPitch = 0.35f;           //弓声音高
        private const float BowVolume = 0.8f;           //弓声音量
        private const float ScatterPitch = 0.45f;       //散射声音高
        private const float ScatterVolume = 0.4f;       //散射声音量
        private const float PitchBand = 0.1f;           //每轮声音的音高随机幅度,连射不单调
        private const int SoundInstances = 2;           //同一声音同时最多几个
        private static readonly Vector3 LightRGB = new Vector3(0.3f, 0.4f, 0.55f);    //照明,RGB 0-1
        private static readonly Color TrailColor = new Color(170, 204, 255);

        private bool started;   //各端各记,首次更新时播放一轮的声音

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
            Projectile.timeLeft = LifeFrames;
        }

        public override void AI() {
            if (!started) {
                started = true;
                if (!Main.dedServ && Projectile.ai[0] == 0f) {
                    SoundEngine.PlaySound(SoundID.Item5 with { Pitch = BowPitch, PitchVariance = PitchBand, Volume = BowVolume, MaxInstances = SoundInstances }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item36 with { Pitch = ScatterPitch, PitchVariance = PitchBand, Volume = ScatterVolume, MaxInstances = SoundInstances }, Projectile.Center);
                }
            }
            Projectile.velocity.Y += Gravity;
            NPC target = FindTarget();
            if (target != null) {
                float rotation = Projectile.velocity.ToRotation().AngleTowards((target.Center - Projectile.Center).ToRotation(), HomingTurn);
                Projectile.velocity = rotation.ToRotationVector2() * Projectile.velocity.Length();
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, LightRGB);
        }

        private NPC FindTarget() {
            NPC best = null;
            float bestDistance = HomingRange;
            float heading = Projectile.velocity.ToRotation();
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                Vector2 offset = npc.Center - Projectile.Center;
                float distance = offset.Length();
                if (distance >= bestDistance || Math.Abs(MathHelper.WrapAngle(offset.ToRotation() - heading)) > HomingHalfCone) {
                    continue;
                }
                best = npc;
                bestDistance = distance;
            }
            return best;
        }

        public override void OnKill(int timeLeft) {
            for (int i = 0; i < DeathDust; i++) {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverCoin);
                dust.noGravity = true;
                dust.velocity = Main.rand.NextVector2Circular(DeathDustSpeed, DeathDustSpeed);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Color trail = TrailColor with { A = 0 };
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
                CEUtils.drawLine(previous, point, trail * (TrailOpacity * fade), TrailWidth * fade);
                previous = point;
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, trail * HeadGlowOpacity, 0f, glow.Size() * 0.5f, HeadGlowSize / glow.Width, SpriteEffects.None);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, SelfLight), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
