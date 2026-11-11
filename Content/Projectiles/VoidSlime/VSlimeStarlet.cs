using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 小辉星,蓝、紫、橙三色共用一张三帧贴图
    /// ai[0] 模式,ai[1] 模式参数,ai[2] 颜色(0 蓝 1 紫 2 橙),localAI[0] 存活帧
    /// 直飞:参数是寿命,匀速
    /// 追踪:参数是寿命,追踪窗内每帧朝最近的玩家转一小角度,过窗只直飞
    /// 旋入:参数是本体下标,绕本体逆时针旋转着向心收紧,进到吞没半径就消失,轨迹按出生点解析算出
    /// </summary>
    public class VSlimeStarlet : VSlimeHostileProjectile
    {
        public const float ModeStraight = 0f;
        public const float ModeHoming = 1f;
        public const float ModeSpiral = 2f;
        public const float ColorBlue = 0f;
        public const float ColorPurple = 1f;
        public const float ColorOrange = 2f;

        private Vector2 spiralCenter;
        private float spiralRadius;
        private float spiralAngle;

        private float Age => Projectile.localAI[0];
        private int Mode => (int)Projectile.ai[0];
        private int ColorKind => Math.Clamp((int)Projectile.ai[2], 0, 2);

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool ShouldUpdatePosition() => Mode != 2;

        public override void AI() {
            bool first = Age == 0f;
            Projectile.localAI[0]++;
            Projectile.frame = ColorKind;
            switch (Mode) {
                case 1:
                    if (first) {
                        Projectile.timeLeft = Math.Max(2, (int)Projectile.ai[1]);
                    }
                    if (Age <= VSlimeDirector.HomingFrames) {
                        SteerToNearest();
                    }
                    break;
                case 2:
                    if (!UpdateSpiral(first)) {
                        return;
                    }
                    break;
                default:
                    if (first) {
                        Projectile.timeLeft = Math.Max(2, (int)Projectile.ai[1]);
                    }
                    break;
            }
            Projectile.rotation += 0.07f;
            Projectile.Opacity = MathHelper.Clamp(Age / 8f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 14f, 0f, 1f);
            Color tint = VSlimeVfx.StarletColor(ColorKind);
            Lighting.AddLight(Projectile.Center, tint.ToVector3() * 0.5f);
        }

        private void SteerToNearest() {
            Player target = null;
            float best = float.MaxValue;
            foreach (Player player in Main.ActivePlayers) {
                if (player.dead) {
                    continue;
                }
                float dist = Vector2.DistanceSquared(player.Center, Projectile.Center);
                if (dist < best) {
                    best = dist;
                    target = player;
                }
            }
            float speed = Projectile.velocity.Length();
            if (target == null || speed < 0.01f) {
                return;
            }
            float current = Projectile.velocity.ToRotation();
            float wanted = (target.Center - Projectile.Center).ToRotation();
            float delta = MathHelper.WrapAngle(wanted - current);
            delta = MathHelper.Clamp(delta, -VSlimeDirector.HomingTurnRate, VSlimeDirector.HomingTurnRate);
            Projectile.velocity = (current + delta).ToRotationVector2() * speed;
        }

        /// <summary>返回 false 表示本帧已被吞掉或本体失效</summary>
        private bool UpdateSpiral(bool first) {
            VoidSlimeNPC boss = OwnerBoss(Projectile.ai[1]);
            if (boss == null || (Age > StateGraceFrames && boss.CurrentStateIndex != VSlimeStateIndex.EndlessDevour)) {
                Projectile.Kill();
                return false;
            }
            if (first) {
                spiralCenter = boss.NPC.Center;
                Vector2 offset = Projectile.Center - spiralCenter;
                spiralRadius = offset.Length();
                spiralAngle = offset.ToRotation();
            }
            Projectile.timeLeft = 60;
            float radius = spiralRadius - VSlimeDirector.DevourInwardSpeed * Age;
            if (radius <= VSlimeDirector.DevourAbsorbRadius) {
                Projectile.Kill();
                return false;
            }
            //屏幕坐标 Y 向下,角度递减才是逆时针
            float angle = spiralAngle - VSlimeDirector.DevourAngularSpeed * Age;
            Vector2 next = spiralCenter + angle.ToRotationVector2() * radius;
            Projectile.velocity = next - Projectile.Center;
            Projectile.Center = next;
            return true;
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircleHits(Projectile.Center, 9f, targetHitbox);

        public override void OnKill(int timeLeft) {
            if (timeLeft > 0) {
                VSlimeVfx.Scatter(Projectile.Center, ColorKind == 2 ? DustID.OrangeTorch : ColorKind == 1 ? DustID.PurpleTorch : DustID.BlueTorch, 5, 1.5f, 4f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            DrawBolt(tex, frame, VSlimeVfx.StarletColor(ColorKind), Projectile.Opacity, Projectile.scale, 0.3f);
            return false;
        }
    }
}
