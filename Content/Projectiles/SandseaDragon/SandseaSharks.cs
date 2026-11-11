using CalamityEntropy.Content.NPCs.SandseaDragon;
using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SandseaDragon
{
    /// <summary>
    /// 沙鲨射弹的共用部分:弹体直接画原版四种沙鲨的贴图,种类 0 沙鲨 1 噬骨 2 戮血 3 水晶,后三种命中各带一种减益
    /// 原版沙鲨贴图头朝左,rotation 存的是朝向角,朝右时水平翻转,背鳍始终朝上
    /// 能穿墙;localAI[0] 存活帧,localAI[1] 首帧标记
    /// </summary>
    public abstract class SandseaSharkBase : SandseaHostileProjectile
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.SandShark;

        protected virtual int VariantIndex => (int)Projectile.ai[0] & 3;

        protected float Age => Projectile.localAI[0];

        public static int NpcTypeOf(int variant) => variant switch {
            1 => NPCID.SandsharkCorrupt,
            2 => NPCID.SandsharkCrimson,
            3 => NPCID.SandsharkHallow,
            _ => NPCID.SandShark,
        };

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 28;
            Projectile.height = 28;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            switch (VariantIndex) {
                case 1:
                    target.AddDebuffFixed(BuffID.CursedInferno, SandseaDirector.SharkCursedFrames);
                    break;
                case 2:
                    target.AddDebuffFixed(BuffID.Ichor, SandseaDirector.SharkIchorFrames);
                    break;
                case 3:
                    target.AddDebuffFixed(BuffID.Slow, SandseaDirector.SharkSlowFrames);
                    break;
            }
        }

        /// <summary>判定是沿朝向的一条 80x26 的带,比贴图窄一圈</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - dir * 40f, Projectile.Center + dir * 40f, 26f, ref point);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ || Projectile.Opacity < 0.3f) {
                return;
            }
            for (int i = 0; i < 8; i++) {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 10f), DustID.Sand, Main.rand.NextVector2Circular(3f, 3f), 0, default, 1.2f);
            }
        }

        /// <summary>残影与弹体同一张图;空中的弹幕要在任何光照下都看得清,亮度只取一半环境光</summary>
        protected void DrawShark(Color lightColor, bool trail) {
            float opacity = Projectile.Opacity;
            if (opacity <= 0.01f) {
                return;
            }
            int npcType = NpcTypeOf(VariantIndex);
            Main.instance.LoadNPC(npcType);
            Texture2D tex = TextureAssets.Npc[npcType].Value;
            int frames = Math.Max(1, Main.npcFrameCount[npcType]);
            int frameHeight = tex.Height / frames;
            int frame = (int)(Main.GameUpdateCount / 6 + (uint)Projectile.whoAmI) % frames;
            Rectangle source = new Rectangle(0, frame * frameHeight, tex.Width, frameHeight);
            Vector2 origin = source.Size() / 2f;
            Color color = Color.Lerp(lightColor, Color.White, 0.5f) * opacity;

            if (trail) {
                for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero) {
                        continue;
                    }
                    float t = 1f - i / (float)Projectile.oldPos.Length;
                    Facing(Projectile.oldRot[i], out float trailRot, out SpriteEffects trailFx);
                    Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, source, color * (0.35f * t),
                        trailRot, origin, Projectile.scale * (0.8f + 0.2f * t), trailFx, 0);
                }
            }
            Facing(Projectile.rotation, out float rotation, out SpriteEffects effects);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, source, color, rotation, origin, Projectile.scale, effects, 0);
        }

        private static void Facing(float facing, out float rotation, out SpriteEffects effects) {
            bool right = MathF.Cos(facing) > 0f;
            effects = right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            rotation = right ? facing : facing + MathHelper.Pi;
        }
    }

    /// <summary>
    /// 沙鲨冲击的环上沙鲨:先绕本体头部慢转,到点由权威端锁向发射,之后匀速直飞
    /// ai[0] 低两位是种类、其余是圈上序号,ai[1] 本体下标,ai[2] 出生时距发射的帧数,发射后写成 -1
    /// 每只都瞄玩家在它发射那一帧的位置;绕圈期间没有判定
    /// </summary>
    public class SandseaRingShark : SandseaSharkBase
    {
        private int RingIndex => (int)Projectile.ai[0] >> 2;
        private bool Launched => Projectile.ai[2] < 0f;
        /// <summary>发射的音效与寿命重置各端只做一次</summary>
        private bool launchSeen;

        public override bool ShouldUpdatePosition() => Launched;

        public override bool? CanDamage() => Launched && Projectile.Opacity > 0.7f ? null : false;

        public override void AI() {
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = (int)Math.Max(0f, Projectile.ai[2]) + SandseaDirector.SharkLife + 60;
            }
            Projectile.localAI[0]++;

            if (!Launched) {
                SandseaDragonHead head = OwnerHead(Projectile.ai[1]);
                if (head == null || head.Context.Dying) {
                    if (Projectile.owner == Main.myPlayer) {
                        Projectile.Kill();
                    }
                    return;
                }
                //圈的转角用本体状态计时,先后生成的沙鲨也等距
                float angle = RingIndex * MathHelper.TwoPi / SandseaDirector.BarrageSharks + SandseaDirector.SharkRingSpin * head.StateTimer;
                float grow = MathHelper.Clamp(Age / 24f, 0f, 1f);
                float radius = SandseaDirector.BarrageRingRadius * (0.35f + 0.65f * (1f - (1f - grow) * (1f - grow)));
                Projectile.Center = head.NPC.Center + angle.ToRotationVector2() * radius;
                Projectile.velocity = Vector2.Zero;

                //绕圈时头朝切线,临发射转向玩家
                Player target = Main.player[head.NPC.target];
                float left = Projectile.ai[2] - Age;
                float aim = (target.Center - Projectile.Center).ToRotation();
                Projectile.rotation = (angle + MathHelper.PiOver2).AngleLerp(aim, Utils.GetLerpValue(16f, 2f, left, true));
                Projectile.Opacity = MathHelper.Clamp(Age / 20f, 0f, 1f) * 0.8f;

                if (left <= 0f && Projectile.owner == Main.myPlayer) {
                    Launch(target);
                }
                return;
            }

            if (!launchSeen) {
                launchSeen = true;
                Projectile.timeLeft = SandseaDirector.SharkLife;
                SandseaVfx.Sound(SoundID.Zombie7 with { Volume = 0.6f, PitchVariance = 0.25f, MaxInstances = 5 }, Projectile.Center);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f));
        }

        /// <summary>策划写明瞄玩家当前位置:每只在自己发射的那一帧锁向,不取提前量</summary>
        private void Launch(Player target) {
            Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * SandseaDirector.SharkLaunchSpeed;
            Projectile.ai[2] = -1f;
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor) {
            DrawShark(lightColor, Launched);
            return false;
        }
    }

    /// <summary>
    /// 二阶段随冲沙鲨:锁向拍起贴在本体上下的队形位上,与本体排成 > 形一同冲刺,本体收速后沿原航向自己飞出场外
    /// ai[0] 种类,ai[1] 本体下标,ai[2] 带符号的队形位;贴位期间位置由各端按本体的声明重算,不过线
    /// 本体起冲之前没有判定
    /// </summary>
    public class SandseaEscortShark : SandseaSharkBase
    {
        /// <summary>已经松开队形位,各端自己判定</summary>
        private bool released;
        private bool dashing;

        public override bool ShouldUpdatePosition() => released;

        public override bool? CanDamage() => (released || dashing) && Projectile.Opacity > 0.7f ? null : false;

        public override void AI() {
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = SandseaDirector.DashLockFrames + SandseaDirector.DashFlyFrames + SandseaDirector.EscortExitFrames + 30;
            }
            Projectile.localAI[0]++;
            SandseaDragonHead head = OwnerHead(Projectile.ai[1]);
            float fade = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);

            //客户端的本体计时可能落后一两帧,出生后的前 12 帧不认松开
            if (!released && head != null && (head.Context.EscortHold || Age < 12f)) {
                Vector2 dir = head.Context.LockedAngle.ToRotationVector2();
                int slot = (int)Projectile.ai[2];
                int rank = Math.Abs(slot);
                Vector2 offset = -dir * (rank * SandseaDirector.EscortSpacingBack) + new Vector2(0f, Math.Sign(slot) * rank * SandseaDirector.EscortSpacingY);
                Projectile.Center = head.NPC.Center + offset;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = dir.ToRotation();
                Projectile.Opacity = MathHelper.Clamp(Age / 12f, 0f, 1f) * fade;
                dashing = head.NPC.velocity.Length() > 12f;
                return;
            }

            if (!released) {
                released = true;
                dashing = false;
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                int phase = head != null ? head.Phase : 2;
                Projectile.velocity = dir * SandseaDirector.DashSpeed(phase);
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, SandseaDirector.EscortExitFrames);
            }
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, fade);
        }

        public override bool PreDraw(ref Color lightColor) {
            DrawShark(lightColor, released || dashing);
            return false;
        }
    }

    /// <summary>三阶段从屏幕上方斜落的沙鲨:匀速直线,不追踪,ai[0] 种类</summary>
    public class SandseaRainShark : SandseaSharkBase
    {
        public override int DefaultTimeLeft => SandseaDirector.RainLife;

        public override bool? CanDamage() => Projectile.Opacity > 0.7f ? null : false;

        public override void AI() {
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = SandseaDirector.RainLife;
            }
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = MathHelper.Clamp(Age / 20f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor) {
            DrawShark(lightColor, true);
            return false;
        }
    }
}
