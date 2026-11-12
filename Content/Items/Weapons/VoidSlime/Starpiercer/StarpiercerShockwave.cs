using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Starpiercer
{
    /// <summary>
    /// 脉冲星命中时迸出的弧形冲击波。以命中点为圆心、顺着弹丸来向张开 45°,半径迅速扩大,扫过的敌人各受一次伤害,最多 6 个
    /// 不打被脉冲星直接命中的那个敌人;命中数自己计,打满也让波纹画完;贴图是一段半径固定的弧,按当前半径整体缩放
    /// ai[0] 被直接命中的敌人序号;localAI[0] 已过帧数;localAI[1] 仅拥有者端,已命中的敌人数
    /// </summary>
    public class StarpiercerShockwave : ModProjectile
    {
        private const int MaxTargets = 6;               //最多命中的敌人数
        private const float HalfAngle = MathHelper.Pi / 8f;     //张角的一半,弧度,即 22.5°
        private const float StartRadius = 16f;          //刚迸出时的半径,像素
        private const float EndRadius = 240f;           //扩到头的半径,像素,15 格
        private const float ExpandFrames = 16f;         //扩到头所需帧数,期间才有伤害
        private const float FadeFrames = 8f;            //扩到头后淡出的帧数
        private const int Size = 16;                    //自身判定边长,像素,只影响绘制裁剪
        private const int DrawFluff = 320;              //绘制裁剪放宽,像素,大于最大半径
        private const float TargetRadiusFactor = 0.25f; //目标半径取判定框宽高之和的这个比例,即平均半边长
        //以下是在 StarpiercerShockwave.png(8×64,弧朝 +X 鼓出)上量出的像素坐标,圆心在贴图左外侧
        private const float ArcCenterX = -74.62f;
        private const float ArcCenterY = 32f;
        private const float ArcRadius = 83.62f;         //弧外缘到圆心的距离,像素
        private const float LeadScale = 1.08f;          //前导光弧相对本体的半径比例
        private const float LeadOpacity = 0.5f;
        private const float LightStep = 48f;            //沿波前每隔多远点一盏光,像素
        private const float BlastPitch = 1.1f;          //爆发声音高,CEUtils.PlaySound 以 1 为原调
        private const float BlastVolume = 0.6f;
        private const int BlastInstances = 3;           //爆发声同时最多几个
        private static readonly Color BodyColor = new Color(223, 248, 255, 220);   //弧本体,半透明叠色
        private static readonly Color LeadColor = new Color(115, 150, 253);       //前导光弧,A=0 叠加
        private static readonly Vector3 LightRGB = new Vector3(0.4f, 0.55f, 1f);   //照明,RGB 0-1

        private ref float Age => ref Projectile.localAI[0];
        private ref float Hits => ref Projectile.localAI[1];

        private float Radius => MathHelper.Lerp(StartRadius, EndRadius, EaseOut(Math.Min(Age / ExpandFrames, 1f)));

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = DrawFluff;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = (int)(ExpandFrames + FadeFrames);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        private static float EaseOut(float x) {
            float left = 1f - x;
            return 1f - left * left;
        }

        public override void AI() {
            Age++;
            if (Age == 1f) {
                CEUtils.PlaySound("shockBlast", BlastPitch, Projectile.Center, BlastInstances, BlastVolume);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            float radius = Radius;
            for (float d = -radius * HalfAngle; d <= radius * HalfAngle; d += LightStep) {
                Lighting.AddLight(Projectile.Center + (Projectile.rotation + d / radius).ToRotationVector2() * radius, LightRGB);
            }
        }

        public override bool? CanHitNPC(NPC target) {
            if (Age > ExpandFrames || Hits >= MaxTargets || target.whoAmI == (int)Projectile.ai[0]) {
                return false;
            }
            return null;
        }

        //目标按外接圆算: 离圆心最近处已被波前扫过,且角度上和张角有重叠
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 toTarget = targetHitbox.Center.ToVector2() - Projectile.Center;
            float distance = toTarget.Length();
            float targetRadius = (targetHitbox.Width + targetHitbox.Height) * TargetRadiusFactor;
            if (distance - targetRadius > Radius) {
                return false;
            }
            if (distance <= targetRadius) {
                return true;
            }
            float widen = MathF.Asin(targetRadius / distance);
            return Math.Abs(MathHelper.WrapAngle(toTarget.ToRotation() - Projectile.rotation)) <= HalfAngle + widen;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Hits++;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float opacity = Age <= ExpandFrames ? 1f : Utils.GetLerpValue(ExpandFrames + FadeFrames, ExpandFrames, Age, true);
            float scale = Radius / ArcRadius;
            Vector2 origin = new Vector2(ArcCenterX, ArcCenterY);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, center, null, LeadColor with { A = 0 } * (LeadOpacity * opacity), Projectile.rotation, origin, scale * LeadScale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, null, BodyColor * opacity, Projectile.rotation, origin, scale, SpriteEffects.None);
            return false;
        }
    }
}
