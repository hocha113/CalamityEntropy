using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.Bow
{
    /// <summary>
    /// 沙暴巨箭,木箭经沙尘暴转化而来。飞得慢,轻微追踪,命中敌怪或物块时在命中点左右各生成一道 <see cref="SandstormBowTwister"/>
    /// ai[0] 面板伤害,龙卷按它取倍率。localAI[0] 已飞行的更新次数
    /// </summary>
    public class SandstormBowGiantArrow : ModProjectile
    {
        public const float HomingRadius = 144f;         //追踪半径,像素,策划 9 格,量到目标碰撞箱的最近点
        private const float HomingTurnRate = 0.05f;     //每次更新最多转过的弧度,8.4 像素/帧时转弯半径约 168 像素
        public const float TwisterDamageRatio = 0.8f;   //沙龙卷伤害占面板的比例,策划 80%
        public const float TwisterKnockback = 12f;      //沙龙卷击退,策划原文
        private const float TwisterSpawnOffset = 6f;    //两道龙卷离命中点的横向距离,像素
        private const int Lifetime = 240;               //存活的更新次数,约飞 2000 像素
        private const int BodySize = 16;                //碰撞箱边长,像素
        private const int TileBodySize = 10;            //撞物块用的边长,同原版箭
        private const int TrailLength = 12;             //尾迹缓存的位置数,约拖出 92 像素
        private const int TrailMode = 2;                //原版尾迹模式 2,每次更新记位置与转角
        private const float HeadOriginY = 12f;          //贴图上箭头中心离顶端的距离,像素,碰撞箱中心落在这里
        //尾迹是沿飞行轨迹密排的原版沙尘龙卷环片(60 像素),越往后越大越淡,叠成一道横着的小沙卷
        private const int WakeSubsteps = 3;             //缓存的相邻两格之间画几片,太疏叠不出实感
        private const float WakeScaleNear = 0.3f;       //紧贴箭头的环片缩放,约 18 像素
        private const float WakeScaleFar = 0.7f;        //尾迹末端的环片缩放,约 42 像素
        private const float WakeOpacity = 1f;           //尾迹整体不透明度
        private const float WakeAlphaScale = 0.5f;      //环片 A 通道折半,同原版沙尘龙卷的画法
        private const float WakeSpin = 0.25f;           //环片自转,弧度每次更新
        private const float WakeTwist = 0.7f;           //相邻环片的转角差,弧度

        private Vector2 travel = Vector2.UnitX;     //最近一次更新的飞行方向,撞物块后速度被裁掉,爆散方向取这个
        private bool struck;                        //命中过敌怪或物块,满足才生成龙卷
        private bool struckTile;                    //撞的是物块,沙往回溅
        private bool locked;                        //已锁定过目标,拥有者据此只对一次位置

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.width = BodySize;
            Projectile.height = BodySize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            Projectile.localAI[0]++;
            float speed = Projectile.velocity.Length();
            NPC target = speed > 0f ? FindTarget() : null;
            if (target != null) {
                float wanted = (target.Center - Projectile.Center).ToRotation();
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(wanted, HomingTurnRate).ToRotationVector2() * speed;
                //转向各端自己算,锁定的那一刻由拥有者对一次位置
                if (!locked) {
                    locked = true;
                    if (Projectile.owner == Main.myPlayer) {
                        Projectile.netUpdate = true;
                    }
                }
            }
            if (speed > 0f) {
                travel = Projectile.velocity / speed;
            }
            Projectile.rotation = travel.ToRotation() + MathHelper.PiOver2;
            SandstormBowVfx.ArrowFlight(Projectile);
        }

        /// <summary>半径内、在前方、中间没有物块的最近敌人</summary>
        private NPC FindTarget() {
            NPC best = null;
            float bestDistance = HomingRadius;
            Vector2 center = Projectile.Center;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                float distance = Vector2.Distance(npc.Hitbox.ClosestPointInRect(center), center);
                //擦身而过的不回头追
                if (distance > bestDistance || Vector2.Dot(npc.Center - center, Projectile.velocity) <= 0f) {
                    continue;
                }
                if (!Collision.CanHitLine(center, 0, 0, npc.Center, 0, 0)) {
                    continue;
                }
                best = npc;
                bestDistance = distance;
            }
            return best;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            width = TileBodySize;
            height = TileBodySize;
            return true;
        }

        //命中钩子只在拥有者端跑,龙卷也只由拥有者生成,正好对得上
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            struck = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            struck = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            struck = true;
            struckTile = true;
            //被撞的物块自己的受击声与碎屑
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            return true;
        }

        public override void OnKill(int timeLeft) {
            SandstormBowVfx.ArrowBurst(Projectile.Center, travel, struckTile);
            SandstormBowVfx.TrailDissolve(Projectile.oldPos, Projectile.Size * 0.5f);
            if (!struck || Projectile.owner != Main.myPlayer) {
                return;
            }
            //不是由弓射出、没带面板时,按巨箭倍率折回去
            float panel = Projectile.ai[0] > 0f ? Projectile.ai[0] : Projectile.damage / SandstormBow.GiantDamageRatio;
            int damage = Math.Max(1, (int)MathF.Round(panel * TwisterDamageRatio));
            int type = ModContent.ProjectileType<SandstormBowTwister>();
            for (int side = -1; side <= 1; side += 2) {
                Vector2 spawn = Projectile.Center + new Vector2(TwisterSpawnOffset * side, 0f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, Vector2.Zero, type, damage, TwisterKnockback, Projectile.owner, side);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.instance.LoadProjectile(ProjectileID.SandnadoFriendly);
            Texture2D ring = TextureAssets.Projectile[ProjectileID.SandnadoFriendly].Value;
            Vector2 ringOrigin = ring.Size() * 0.5f;
            Vector2 half = Projectile.Size * 0.5f;
            int total = (Projectile.oldPos.Length - 1) * WakeSubsteps;
            for (int step = total; step >= 1; step--) {
                //每 WakeSubsteps 步对应缓存里的一格,落在两格之间的按比例插值;oldPos[0] 就是当前位置
                int index = step / WakeSubsteps;
                int part = step % WakeSubsteps;
                Vector2 spot = Projectile.oldPos[index];
                if (spot == Vector2.Zero) {
                    continue;
                }
                if (part > 0) {
                    Vector2 next = Projectile.oldPos[index + 1];
                    if (next == Vector2.Zero) {
                        continue;
                    }
                    spot = Vector2.Lerp(spot, next, part / (float)WakeSubsteps);
                }
                float far = step / (float)total;    //0 贴着箭头,1 尾迹末端
                Color color = SandstormBowVfx.Sand * ((1f - far) * WakeOpacity * Projectile.Opacity);
                color.A = (byte)(color.A * WakeAlphaScale);
                float spin = step * WakeTwist - Projectile.localAI[0] * WakeSpin;
                float scale = MathHelper.Lerp(WakeScaleNear, WakeScaleFar, far) * Projectile.scale;
                Main.EntitySpriteDraw(ring, spot + half - Main.screenPosition, null, color, spin, ringOrigin, scale, SpriteEffects.None);
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2(texture.Width * 0.5f, HeadOriginY);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
