using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Starpiercer
{
    /// <summary>
    /// 脉冲星命中时放出的电弧。先电被直接命中的敌人,再跳到离那里最近、看得见的另一个敌人,电到第二个后消失
    /// 弹体极快地扑向当前目标,只打当前目标,身后拖一道抖动的折线电光;目标只由拥有者选定并同步
    /// ai[0] 当前目标的 NPC 序号,NoTarget 为没有;ai[1] 已电过的敌人序号,NoTarget 为还没电过
    /// </summary>
    public class StarpiercerArc : ModProjectile
    {
        public const float Speed = 20f;                 //扑向目标的速度,像素每次更新
        public const float NoTarget = -1f;              //ai[0] 与 ai[1] 取值: 没有
        private const int Strikes = 2;                  //电到这么多个敌人后消失
        private const int ExtraUpdates = 3;             //每帧额外更新次数,合计 80 像素每帧
        private const int Size = 8;                     //判定边长,像素
        private const int LifeUpdates = 240;            //寿命,更新次数,合计 60 帧
        private const float HopRange = 400f;            //跳向下一个敌人的索敌半径,像素,25 格
        private const int TrailRecordMode = 0;          //ProjectileID.Sets.TrailingMode 取值,逐次记下旧位置
        private const int TrailLength = 14;             //折线拐点数,也是记下的旧位置数
        private const float JitterAmplitude = 5f;       //拐点离轨迹的随机偏移上限,像素
        private const float CoreWidth = 2f;             //电光芯粗细,像素
        private const float GlowWidth = 6f;             //电光外晕粗细,像素,不窄于芯的两倍
        private const float GlowOpacity = 0.6f;
        private const float HeadGlowSize = 28f;         //电花头光晕直径,像素
        private const float HeadGlowOpacity = 0.7f;
        private const float SparkChance = 0.2f;         //每次更新出一颗电花粉尘的概率
        private const float SparkSpeed = 2f;            //电花粉尘初速上限,像素每帧
        private const float StrikePitch = 1.2f;         //放电声音高,CEUtils.PlaySound 以 1 为原调
        private const float StrikeVolume = 0.5f;
        private const int StrikeInstances = 4;          //放电声同时最多几个
        private static readonly Color CoreColor = new Color(223, 248, 255);
        private static readonly Color GlowColor = new Color(115, 150, 253);
        private static readonly Vector3 LightRGB = new Vector3(0.4f, 0.55f, 1f);   //照明,RGB 0-1

        private float heardTarget = NoTarget;           //各端各记,上次播放电声时的目标

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailRecordMode;
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = Strikes;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.timeLeft = LifeUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            if (Projectile.ai[0] != heardTarget) {
                heardTarget = Projectile.ai[0];
                if (heardTarget != NoTarget) {
                    CEUtils.PlaySound("spark", StrikePitch, Projectile.Center, StrikeInstances, StrikeVolume);
                }
            }
            if (HasTarget(out NPC target)) {
                Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * Speed;
            }
            else if (Projectile.owner == Main.myPlayer && !Retarget()) {
                Projectile.Kill();
                return;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, LightRGB);
            if (!Main.dedServ && Main.rand.NextFloat() < SparkChance) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2Circular(SparkSpeed, SparkSpeed));
                dust.noGravity = true;
            }
        }

        //目标只在拥有者端选,不再回头电已电过的;找不到返回 false
        private bool Retarget() {
            int struck = (int)Projectile.ai[1];
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HopRange, (proj, npc) => npc != struck && CEUtils.HomingWithTileBlockingFilter(proj, npc));
            if (target == null) {
                return false;
            }
            Projectile.ai[0] = target.whoAmI;
            Projectile.netUpdate = true;
            return true;
        }

        private bool HasTarget(out NPC target) {
            int index = (int)Projectile.ai[0];
            target = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
            return target != null && target.CanBeChasedBy(Projectile);
        }

        public override bool? CanHitNPC(NPC target) {
            return target.whoAmI == (int)Projectile.ai[0] ? null : false;
        }

        //电完当前目标就放掉它,下一次更新由拥有者另找;电满两个时穿透数已耗尽
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Projectile.ai[1] = target.whoAmI;
            Projectile.ai[0] = NoTarget;
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor) {
            //拐点每帧重新抖一次,电光看起来在跳;外晕 A=0 叠加,芯用实色
            Vector2 half = Projectile.Size * 0.5f;
            Vector2 previous = Projectile.Center;
            for (int i = 0; i < TrailLength; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    break;
                }
                Vector2 point = Projectile.oldPos[i] + half + Main.rand.NextVector2Circular(JitterAmplitude, JitterAmplitude);
                float fade = 1f - i / (float)TrailLength;
                CEUtils.drawLine(previous, point, GlowColor with { A = 0 } * (GlowOpacity * fade), GlowWidth * Projectile.scale);
                CEUtils.drawLine(previous, point, CoreColor * fade, CoreWidth * Projectile.scale);
                previous = point;
            }
            Texture2D glow = CEExtraAssets.Glow2;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, GlowColor with { A = 0 } * HeadGlowOpacity, 0f, glow.Size() * 0.5f, HeadGlowSize * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, null, Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
