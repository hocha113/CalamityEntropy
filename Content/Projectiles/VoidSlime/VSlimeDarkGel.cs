using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 暗物质凝胶球,命中附带 3 秒虚空之火
    /// ai[0] 模式,ai[1] 模式参数,ai[2] 本体下标,localAI[0] 存活帧,localAI[1] 横排模式的松开闩
    /// 瞄准:参数是悬停帧数,悬停完朝目标发射并逐帧加速
    /// 抛落:参数是寿命,下落加速度随存活帧增长
    /// 直飞:参数是寿命,匀速
    /// 横排:参数是相对本体的横向偏移,本体声明 RowHeld 时挂在本体上,松开后缓慢下落
    /// </summary>
    public class VSlimeDarkGel : VSlimeHostileProjectile
    {
        public const float ModeAimed = 0f;
        public const float ModeFall = 1f;
        public const float ModeStraight = 2f;
        public const float ModeRow = 3f;

        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DebuffTime => VSlimeDirector.VoidFireFrames;

        private float Age => Projectile.localAI[0];
        private int Mode => (int)Projectile.ai[0];
        private bool Released => Projectile.localAI[1] > 0f;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetExtraDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
        }

        public override bool ShouldUpdatePosition() => Mode != 3 || Released;

        public override void AI() {
            bool first = Age == 0f;
            Projectile.localAI[0]++;
            switch (Mode) {
                case 0:
                    UpdateAimed(first);
                    break;
                case 1:
                    if (first) {
                        Projectile.timeLeft = Math.Max(2, (int)Projectile.ai[1]);
                    }
                    Projectile.velocity.X *= VSlimeDirector.SprayDragX;
                    float gravity = VSlimeDirector.SprayGravity + VSlimeDirector.SprayGravityGrowth * Age;
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + gravity, VSlimeDirector.SprayMaxFall);
                    break;
                case 2:
                    if (first) {
                        Projectile.timeLeft = Math.Max(2, (int)Projectile.ai[1]);
                    }
                    break;
                default:
                    UpdateRow(first);
                    break;
            }
            Projectile.rotation += 0.05f;
            Projectile.Opacity = MathHelper.Clamp(Age / 10f, 0f, 1f) * MathHelper.Clamp(Projectile.timeLeft / 14f, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 0.3f, 0.12f, 0.5f);
            if (!Main.dedServ && Main.rand.NextBool(9)) {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f), DustID.PurpleTorch, -Projectile.velocity * 0.15f, 100, default, Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }

        private void UpdateAimed(bool first) {
            int hover = (int)Projectile.ai[1];
            if (first) {
                Projectile.timeLeft = hover + VSlimeDirector.GelAimLife;
                Projectile.velocity = Vector2.Zero;
            }
            if (Age < hover) {
                Projectile.velocity = Vector2.Zero;
                return;
            }
            if (Age == hover) {
                //发射那一帧锁向,权威端把速度过线,客户端先按本地目标算,收包后以包为准
                VoidSlimeNPC boss = OwnerBoss(Projectile.ai[2]);
                Player target = boss != null && boss.NPC.HasValidTarget ? Main.player[boss.NPC.target] : null;
                Vector2 dir = target != null ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
                Projectile.velocity = dir * VSlimeDirector.GelAimStartSpeed;
                if (!VaultUtils.isClient) {
                    Projectile.netUpdate = true;
                }
                VSlimeVfx.Sound(SoundID.Item155 with { Volume = 0.5f, MaxInstances = 4 }, Projectile.Center);
                return;
            }
            float speed = Projectile.velocity.Length();
            if (speed > 0.01f && speed < VSlimeDirector.GelAimMaxSpeed) {
                Projectile.velocity *= Math.Min(speed + VSlimeDirector.GelAimAccel, VSlimeDirector.GelAimMaxSpeed) / speed;
            }
        }

        private void UpdateRow(bool first) {
            if (first) {
                Projectile.timeLeft = VSlimeDirector.RowGelLife;
            }
            if (!Released) {
                VoidSlimeNPC boss = OwnerBoss(Projectile.ai[2]);
                bool held = boss != null && (boss.RowHeld || Age <= StateGraceFrames);
                if (held) {
                    Projectile.Center = boss.NPC.Center + new Vector2(Projectile.ai[1], 0f);
                    Projectile.velocity = Vector2.Zero;
                    Projectile.timeLeft = VSlimeDirector.RowGelLife;
                    return;
                }
                Projectile.localAI[1] = 1f;
            }
            Projectile.velocity.X = 0f;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + VSlimeDirector.RowGelAccel, VSlimeDirector.RowGelMaxFall);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircleHits(Projectile.Center, 12f, targetHitbox);

        public override void OnKill(int timeLeft) {
            VSlimeVfx.Stardust(Projectile.Center, 5, 1.5f, 4f, 0.9f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            DrawBolt(tex, tex.Frame(), VSlimeVfx.VoidBright, Projectile.Opacity, Projectile.scale, 0.34f);
            return false;
        }
    }
}
