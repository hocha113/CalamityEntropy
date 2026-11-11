using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 竞技场边界的禁戒旋风:高 100 格的沙柱,位置每帧从本体的竞技场事实重算,不自己移动,三阶段随场地收窄而合拢
    /// ai[0] 本体下标,ai[1] 侧向 ±1;localAI[0] 成形帧,localAI[1] 消散帧
    /// 成形要 90 帧,成形前没有判定;本体死亡或消失后 40 帧散去
    /// </summary>
    public class SandseaArenaWall : SandseaHostileProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SandnadoHostile;
        public override bool Persistent => true;

        private const int FadeFrames = 40;

        private ref float Form => ref Projectile.localAI[0];
        private ref float Fade => ref Projectile.localAI[1];
        private float Shown => MathHelper.Clamp(Form / SandseaDirector.ArenaWallFormFrames, 0f, 1f) * (1f - Fade / FadeFrames);
        private float ColumnTop => Projectile.Center.Y - (SandseaDirector.ArenaWallHeight + SandseaDirector.ArenaWallBelow) / 2f;
        private float ColumnBottom => Projectile.Center.Y + (SandseaDirector.ArenaWallHeight + SandseaDirector.ArenaWallBelow) / 2f;

        public override void SetStaticDefaults() {
            //柱子高 2000 像素,判定盒只在中点,绘制裁剪要放宽到半高以上
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 104;
            Projectile.height = 104;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Projectile.velocity = Vector2.Zero;
            SandseaDragonHead head = OwnerHead(Projectile.ai[0]);
            if (head == null || head.Context.Dying) {
                Fade++;
                if (Fade >= FadeFrames && Projectile.owner == Main.myPlayer) {
                    Projectile.Kill();
                }
                return;
            }
            Projectile.timeLeft = 600;
            if (Form < SandseaDirector.ArenaWallFormFrames) {
                Form++;
            }

            SandseaStateContext ctx = head.Context;
            float mid = ctx.ArenaFloorY + SandseaDirector.ArenaWallBelow - (SandseaDirector.ArenaWallHeight + SandseaDirector.ArenaWallBelow) / 2f;
            Projectile.Center = new Vector2(ctx.ArenaCenterX + Projectile.ai[1] * ctx.ArenaHalfWidth, mid);

            if (!Main.dedServ) {
                if (Form == 1f) {
                    SandseaVfx.Sound(SoundID.Item60 with { Pitch = -0.4f, Volume = 0.8f, MaxInstances = 2 }, Projectile.Center);
                }
                //玩家附近的那一段柱身往外甩沙
                Player local = Main.LocalPlayer;
                if (Math.Abs(local.Center.X - Projectile.Center.X) < 1400f && Main.rand.NextBool(2)) {
                    float y = MathHelper.Clamp(local.Center.Y + Main.rand.NextFloat(-500f, 500f), ColumnTop, ColumnBottom);
                    Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-60f, 60f), y);
                    Dust d = Dust.NewDustPerfect(pos, DustID.Sandnado, new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 5f)), 0, default, Main.rand.NextFloat(0.9f, 1.5f) * Shown);
                    d.noGravity = true;
                }
            }
        }

        public override bool? CanDamage() => Form >= SandseaDirector.ArenaWallFormFrames && Fade <= 0f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float half = SandseaDirector.ArenaWallHalfWidth;
            return targetHitbox.Right > Projectile.Center.X - half && targetHitbox.Left < Projectile.Center.X + half
                && targetHitbox.Bottom > ColumnTop && targetHitbox.Top < ColumnBottom;
        }

        public override bool PreDraw(ref Color lightColor) {
            float shown = Shown;
            if (shown <= 0.01f) {
                return false;
            }
            float seed = Projectile.ai[1] * 1.7f;
            float time = Main.GlobalTimeWrappedHourly;
            SandseaTwisterDraw.DrawColumn(new Vector2(Projectile.Center.X, ColumnBottom), ColumnBottom - ColumnTop,
                t => (122f + 26f * MathF.Sin(t * 46f + time * 2.2f + seed)) * (0.5f + 0.5f * shown), shown, 1f, 0.04f, 0.06f, 1.5f);
            return false;
        }
    }
}
