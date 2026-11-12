using CalamityEntropy.Content.Items.Weapons.VoidDestroyer;
using CalamityEntropy.Content.NPCs.SunMoonGods.Sage;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.StarmoonShark
{
    /// <summary>
    /// 淡蓝星光拖尾,只是视觉,不碰子弹本身。ai[0] 子弹的 identity,ai[1] 子弹类型;
    /// 各端按 拥有者 + identity + 类型 找到子弹跟随,子弹没了或槽位被新弹幕顶替就停止取点、从尾部收拢
    /// </summary>
    public class StarmoonSharkStarTrail : ModProjectile
    {
        public const int MaxPoints = 8;             //每帧一点
        public const int SearchGrace = 4;           //帧,远端等子弹同步包
        public const int MaxLife = 120;             //帧,子弹活得再久也只跟这么久
        public const float Width = 8f;              //像素
        public const int SparkleInterval = 3;       //帧
        public const float JumpTolerance = 2.5f;    //两帧位移超过子弹每帧行程的这么多倍就当槽位被顶替
        public const float JumpSlack = 16f;         //像素

        private readonly List<Vector2> points = new();
        private static readonly List<Vector2> scratch = new();
        private int bulletIndex = -1;
        private int lastTimeLeft;
        private bool lost;
        private int age;

        public override string Texture => "CalamityEntropy/Assets/Extra/Empty";

        public override void SetDefaults() {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = MaxLife;
            Projectile.aiStyle = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI() {
            age++;
            if (Projectile.timeLeft <= MaxPoints) {
                lost = true;
            }
            if (!lost) {
                Projectile bullet = FindBullet();
                if (bullet != null) {
                    Projectile.Center = bullet.Center;
                    points.Add(bullet.Center);
                    if (points.Count > MaxPoints) {
                        points.RemoveAt(0);
                    }
                    if (!Main.dedServ && age % SparkleInterval == 0) {
                        Sparkle(bullet.Center);
                    }
                    return;
                }
                if (bulletIndex < 0 && age <= SearchGrace) {
                    return;
                }
                lost = true;
            }
            if (points.Count > 0) {
                points.RemoveAt(0);
            }
            if (points.Count == 0) {
                Projectile.Kill();
            }
        }

        private Projectile FindBullet() {
            int identity = (int)Projectile.ai[0];
            int type = (int)Projectile.ai[1];
            if (bulletIndex >= 0) {
                Projectile p = Main.projectile[bulletIndex];
                if (!p.active || p.owner != Projectile.owner || p.identity != identity || p.type != type || p.timeLeft > lastTimeLeft) {
                    return null;
                }
                if (points.Count > 0) {
                    float step = p.velocity.Length() * (p.extraUpdates + 1);
                    if (Vector2.Distance(p.Center, points[points.Count - 1]) > step * JumpTolerance + JumpSlack) {
                        return null;
                    }
                }
                lastTimeLeft = p.timeLeft;
                return p;
            }
            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.identity == identity && p.type == type) {
                    bulletIndex = i;
                    lastTimeLeft = p.timeLeft;
                    return p;
                }
            }
            return null;
        }

        private static void Sparkle(Vector2 at) {
            Vector2 pos = at + Main.rand.NextVector2Circular(4f, 4f);
            PRTLoader.NewParticle<PRT_SparkleCal>(pos, Vector2.Zero, SageVfx.StarWhite, Main.rand.NextFloat(0.25f, 0.4f)).Configure(SageVfx.StarBlue, 16, 0.08f, 1.4f);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (points.Count < 2) {
                return false;
            }
            scratch.Clear();
            for (int i = points.Count - 1; i >= 0; i--) {
                scratch.Add(points[i]);
            }
            VDWeaponFx.BeginAdditive();
            VDWeaponFx.Ribbon(scratch, t => Width * (1f - t * 0.8f), t => Color.Lerp(SageVfx.StarWhite, SageVfx.StarBlue, t) * (0.8f * (1f - t)));
            VDWeaponFx.Glow(scratch[0], SageVfx.StarBlue * 0.5f, 9f);
            VDWeaponFx.End();
            return false;
        }
    }
}
