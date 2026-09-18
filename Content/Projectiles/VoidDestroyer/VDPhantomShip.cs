using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using VoidDestroyerNPC = CalamityEntropy.Content.NPCs.VoidDestroyer.VoidDestroyer;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 幻影舰:驱逐舰的全息复制体,在门口待机 ai[1] 帧(与真身同拍瞄准)→ 服务端一帧定速朝穿越点齐冲 → 冲刺结束碎成全息碎片。
    /// 带深度生成时(立体舰队的远门 / 近门)沿三维直线冲刺:平面速度与 Z 速度都按「第 FleetCrossFrame 帧穿过预测点」反推,
    /// 远舰放大着来、近舰缩小着来,只在穿过平面那几帧有判定,穿过后继续飞出视野。
    /// ai[0] 本体,ai[2] 目标玩家。P3 在平面附近沿路留加速虚空弹。伤害是真身接触的 60%
    /// </summary>
    public class VDPhantomShip : VDDepthProjectile
    {
        [InnoVault.VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2")]
        private static Asset<Texture2D> p2Tex = null;

        public override string Texture => "CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyer";
        public override int DebuffType => ModContent.BuffType<VoidFire>();
        public override int DefaultTimeLeft => 200;
        public override float MarkerRadius => 60f;
        protected override float WhooshStrength => 0.5f;

        public int AimFrames => (int)Math.Max(Projectile.ai[1], 6f);
        public int Age => (int)Projectile.localAI[1];
        public bool Dashing => Age >= AimFrames && Age < AimFrames + VDDirector.FleetDashFrames;

        private VoidDestroyerNPC Owner {
            get {
                int idx = (int)Projectile.ai[0];
                if (idx < 0 || idx >= Main.maxNPCs || !Main.npc[idx].active || Main.npc[idx].ModNPC is not VoidDestroyerNPC boss) {
                    return null;
                }
                return boss;
            }
        }

        public override void SetExtraDefaults() {
            Projectile.width = 120;
            Projectile.height = 70;
        }

        protected override void DepthAI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = AimFrames + VDDirector.FleetDashFrames + VDDirector.FleetEndFade;
                VDVfx.HoloBurst(ProjectedCenter, VDDepth.Fog(VDVfx.VoidPurple, Z));
            }
            Projectile.localAI[1]++;
            int age = Age;
            Player target = TargetPlayer(2);

            if (age < AimFrames) {
                //待机:钉在门口,Z 不动
                Projectile.velocity = Vector2.Zero;
                ZVel = 0f;
            }
            if (age == AimFrames) {
                //齐冲:平面速度由服务端按预测穿越点定,Z 速度各端按自己的 Z 反推(同一帧到达平面)
                if (IsServer) {
                    Vector2 aim = target != null ? target.Center + target.velocity * VDDirector.FleetCrossLead : Projectile.Center + Vector2.UnitY * 200f;
                    Projectile.velocity = (aim - Projectile.Center) / VDDirector.FleetCrossFrame;
                    Projectile.netUpdate = true;
                }
                if (Z != 0f) {
                    ZVel = -Z / VDDirector.FleetCrossFrame;
                }
                if (!Main.dedServ) {
                    CEUtils.PlaySound("CruiserDash", 1.1f * (Z != 0f ? VDDepth.DopplerPitch(Math.Max(Z, 0f)) : 1f), ProjectedCenter, 4, 0.8f);
                }
            }
            if (age >= AimFrames + VDDirector.FleetDashFrames) {
                Projectile.velocity *= 0.8f;
                ZVel *= 0.8f;
            }

            //倾斜跟随表观横向速度(与真身同一套)
            float apparentVx = Projectile.velocity.X * VDDepth.Scale(Z);
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.Clamp(apparentVx * 0.012f, -0.25f, 0.25f), 0.2f);
            if (Math.Abs(Z) < 0.5f) {
                Lighting.AddLight(Projectile.Center, VDVfx.VoidPurple.ToVector3() * 0.7f);
            }

            VoidDestroyerNPC boss = Owner;
            bool nearPlane = Math.Abs(Z) < 0.3f;
            if (Dashing && nearPlane && boss != null && boss.Phase >= 3 && IsServer && age % VDDirector.FleetTrailInterval == 3) {
                int dmg = boss.ProjDamage(VDDirector.DmgVoidBolt);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY) * VDDirector.PhantomTrailSpeed, ModContent.ProjectileType<VDVoidBolt>(), dmg, 0f, Main.myPlayer, VDVoidBolt.ModeDashTrail);
            }
            if (!Main.dedServ && Dashing && nearPlane && Main.rand.NextBool(2)) {
                Vector2 v = -Projectile.velocity * 0.1f + CEUtils.randomPointInCircle(2f);
                VDVfx.SparkBurst(Projectile.Center + CEUtils.randomPointInCircle(40f), VDVfx.VoidPurple, 1, v.Length(), v.Length(), 18, 0.5f, 1f);
            }
        }

        protected override bool? CollidingOnPlane(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!Dashing) {
                return false;
            }
            return projHitbox.Intersects(targetHitbox);
        }

        public override void OnKill(int timeLeft) {
            if (Math.Abs(Z) > 0.6f) {
                return;
            }
            VDVfx.HoloBurst(Projectile.Center, VDVfx.VoidPurple);
            VDVfx.HoloBurst(Projectile.Center, Color.White);
        }

        protected override void DrawDepth(SpriteBatch spriteBatch) {
            VoidDestroyerNPC boss = Owner;
            Texture2D tex = boss != null && boss.Phase >= 2 && p2Tex != null ? p2Tex.Value : TextureAssets.Projectile[Type].Value;
            Vector2 pos = ProjectedCenter - Main.screenPosition;
            float fadeIn = MathHelper.Clamp(Age / 8f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)VDDirector.FleetEndFade, 0f, 1f);
            float opacity = 0.85f * Math.Min(fadeIn, fadeOut) * VDDepth.Alpha(Z);
            //瞄准期越接近出手越亮(全息闪烁加剧 = 预告)
            if (Age < AimFrames) {
                float p = Age / (float)AimFrames;
                opacity *= 0.6f + 0.4f * p;
            }
            if (opacity <= 0.01f) {
                return;
            }
            float scale = DrawScale;
            Color tint = VDDepth.Fog(VDVfx.VoidPurple, Z);

            VDHologramDraw.Begin();
            if (Dashing) {
                //残影按各自历史 Z 投影:三维冲刺的残影会沿透视线拉开
                for (int i = 1; i < trailCount && i <= 4; i++) {
                    Vector2 ghost = VDDepth.Project(trailPos[i], trailZ[i]) - Main.screenPosition;
                    float gs = Projectile.scale * VDDepth.Scale(trailZ[i]);
                    VDHologramDraw.DrawPart(tex, ghost, null, tint, opacity * (0.3f - i * 0.06f), Projectile.rotation, tex.Size() / 2f, gs, SpriteEffects.None);
                }
            }
            VDHologramDraw.DrawPart(tex, pos, null, tint, opacity, Projectile.rotation, tex.Size() / 2f, scale, SpriteEffects.None);
            VDHologramDraw.End();
        }
    }
}
