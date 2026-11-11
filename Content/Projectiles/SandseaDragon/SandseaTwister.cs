using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 小型禁戒龙卷风,成对生成:20 帧成形,原地停 30 帧,用 120 帧朝合拢点匀速平移,合拢后消散并放出一圈 10 发直飞的沙弹
    /// ai[0] 侧向 ±1,ai[1] 合拢点的世界 X,ai[2] 出生时底部的世界 Y
    /// 横坐标只由出生数据和存活帧决定;底部每帧重新贴地,物块各端一致,所以各端自算也一致
    /// 成形期间没有判定;高 12 格,能从上方越过;一对里只有右侧那道放沙弹
    /// </summary>
    public class SandseaTwister : SandseaHostileProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SandnadoHostile;
        public override int DefaultTimeLeft => SandseaDirector.TwisterLife;

        /// <summary>贴地时底部每帧最多升降的像素,过坡不跳变</summary>
        private const float MaxClimb = 6f;
        /// <summary>贴地探测从底部上方这么高往下找,三格</summary>
        private const float ProbeLift = 48f;

        private ref float Age => ref Projectile.localAI[0];
        private int CloseAt => SandseaDirector.TwisterGrow + SandseaDirector.TwisterHold;
        private int FadeAt => CloseAt + SandseaDirector.TwisterClose;

        /// <summary>当前底部的世界 Y,各端自己记</summary>
        private float baseY;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 52;
            Projectile.height = 52;
        }

        public override bool ShouldUpdatePosition() => false;

        /// <summary>成形与消散的展开程度 0..1</summary>
        private float Shown {
            get {
                float grow = MathHelper.Clamp(Age / SandseaDirector.TwisterGrow, 0f, 1f);
                float fade = 1f - MathHelper.Clamp((Age - FadeAt) / SandseaDirector.TwisterFade, 0f, 1f);
                return grow * fade;
            }
        }

        public override void AI() {
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = SandseaDirector.TwisterLife + 2;
                baseY = Projectile.ai[2];
                SandseaVfx.Sound(SoundID.Item60 with { Pitch = 0.1f, Volume = 0.7f, MaxInstances = 4 }, Projectile.Center);
            }
            Age++;
            Projectile.velocity = Vector2.Zero;

            float start = Projectile.ai[1] + Projectile.ai[0] * SandseaDirector.TwisterStartOffset;
            float close = MathHelper.Clamp((Age - CloseAt) / SandseaDirector.TwisterClose, 0f, 1f);
            float x = MathHelper.Lerp(start, Projectile.ai[1], close);
            float ground = SandseaTerrain.Surface(x, baseY - ProbeLift, true);
            baseY += MathHelper.Clamp(ground - baseY, -MaxClimb, MaxClimb);
            Projectile.Center = new Vector2(x, baseY - SandseaDirector.TwisterHeight / 2f);

            if (Age == FadeAt && Projectile.ai[0] > 0f) {
                Vector2 foot = new Vector2(Projectile.ai[1], baseY);
                Vector2 merge = foot - new Vector2(0f, SandseaDirector.TwisterRingHeight);
                SandseaVfx.SandBurst(foot, 1.6f);
                if (Projectile.owner == Main.myPlayer) {
                    for (int i = 0; i < SandseaDirector.TwisterRingBlasts; i++) {
                        Vector2 velocity = (MathHelper.TwoPi * i / SandseaDirector.TwisterRingBlasts).ToRotationVector2() * SandseaDirector.TwisterRingSpeed;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), merge, velocity, ModContent.ProjectileType<SandseaSandBlast>(),
                            SandseaDirector.ProjDamage(SandseaDirector.DmgSandBlast), 0f, Main.myPlayer, 1f);
                    }
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(2)) {
                Vector2 pos = new Vector2(x + Main.rand.NextFloat(-24f, 24f), baseY - Main.rand.NextFloat(0f, SandseaDirector.TwisterHeight) * Shown);
                Dust d = Dust.NewDustPerfect(pos, DustID.Sandnado, new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(1f, 4f)), 0, default, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }

        public override bool? CanDamage() => Age >= SandseaDirector.TwisterGrow && Age < FadeAt ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float half = SandseaDirector.TwisterHalfWidth;
            return targetHitbox.Right > Projectile.Center.X - half && targetHitbox.Left < Projectile.Center.X + half
                && targetHitbox.Bottom > baseY - SandseaDirector.TwisterHeight && targetHitbox.Top < baseY;
        }

        public override bool PreDraw(ref Color lightColor) {
            float shown = Shown;
            if (shown <= 0.01f) {
                return false;
            }
            //原版沙尘龙卷的轮廓:底窄顶宽;原版底端完全透明,这里贴地的一段就有判定,底端淡入段收短
            SandseaTwisterDraw.DrawColumn(new Vector2(Projectile.Center.X, baseY), SandseaDirector.TwisterHeight * (0.4f + 0.6f * shown),
                t => 50f + 60f * t, shown, 1f, 0.18f, 0.35f, 1.8f);
            return false;
        }
    }
}
