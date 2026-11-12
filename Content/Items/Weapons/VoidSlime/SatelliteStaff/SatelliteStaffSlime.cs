using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.SatelliteStaff
{
    /// <summary>
    /// 火球史莱姆:每只占1个栏位,按召唤先后(identity)在玩家周围均匀排成一圈。
    /// ai[0] 攻击计时(帧),ai[1] 已攻击次数;目标与开火只在主人端决定
    /// </summary>
    public class SatelliteStaffSlime : ModProjectile
    {
        #region 调参旋钮
        //环绕半径(像素,约6格,任务文件)、转一圈的帧数、每次更新补近环绕点的比例
        public const float OrbitRadius = 96f;
        public const int OrbitPeriod = 360;
        public const float FollowLerp = 0.3f;
        //超过这个距离直接瞬移(像素)
        public const float TeleportDistance = 2000f;
        //攻击间隔(帧)、索敌距离(像素,120格),均为策划
        public const int AttackInterval = 40;
        public const float SeekRange = 1920f;
        //每攻击8次后第9次换成耀斑射线(策划)
        public const int FlareEvery = 9;
        //火球:速度(像素/更新,每帧2次更新)、伤害倍率(策划)
        public const float FireballSpeed = 14f;
        public const int FireballUpdates = 2;
        public const float FireballMult = 1.5f;
        //耀斑射线:速度(像素/更新)、伤害倍率(策划)
        public const float FlareSpeed = 12f;
        public const float FlareMult = 1f;
        //判定尺寸(像素);续命帧数(有增益时每帧续上,没了就在这么多帧后消失)
        public const int HitboxWidth = 28;
        public const int HitboxHeight = 22;
        public const int KeepAliveFrames = 2;
        //贴图帧数(竖排)、每帧持续(帧)、照明强度
        public const int Frames = 4;
        public const int FrameTicks = 6;
        public const float LightStrength = 0.8f;
        //身后光晕:直径(像素)、强度、明暗起伏的幅度与角速度(弧度/秒)
        public const float GlowDiameter = 64f;
        public const float GlowOpacity = 0.5f;
        public const float PulseDepth = 0.15f;
        public const float PulseSpeed = 6f;
        #endregion

        public static readonly Color GlowColor = new(255, 120, 40);

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxWidth;
            Projectile.height = HitboxHeight;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = KeepAliveFrames;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;
        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                owner.ClearBuff(ModContent.BuffType<SatelliteStaffBuff>());
                return;
            }
            if (owner.HasBuff(ModContent.BuffType<SatelliteStaffBuff>())) {
                Projectile.timeLeft = KeepAliveFrames;
            }
            Orbit(owner);
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * LightStrength);
            if (Projectile.owner == Main.myPlayer) {
                Attack();
            }
        }

        //序号 = 同主人里 identity 比自己小的个数,各端算出来一致;角度按游戏更新数转
        private void Orbit(Player owner) {
            int index = 0;
            int total = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == Type) {
                    total++;
                    if (p.identity < Projectile.identity) {
                        index++;
                    }
                }
            }
            float turn = Main.GameUpdateCount % OrbitPeriod / (float)OrbitPeriod + index / (float)Math.Max(total, 1);
            Vector2 home = owner.MountedCenter + (MathHelper.TwoPi * turn).ToRotationVector2() * OrbitRadius;
            if (Projectile.Distance(home) > TeleportDistance) {
                Projectile.Center = home;
                Projectile.netUpdate = true;
            }
            Projectile.velocity = (home - Projectile.Center) * FollowLerp;
        }

        private void Attack() {
            if (Projectile.ai[0] < AttackInterval) {
                Projectile.ai[0]++;
                return;
            }
            NPC target = FindTarget();
            if (target == null) {
                return;
            }
            Projectile.ai[0] = 0f;
            Projectile.ai[1]++;
            Projectile.netUpdate = true;
            IEntitySource source = Projectile.GetSource_FromThis();
            if (Projectile.ai[1] % FlareEvery == 0f) {
                Projectile.NewProjectile(source, Projectile.Center, Projectile.DirectionTo(target.Center) * FlareSpeed, ModContent.ProjectileType<SatelliteStaffFlare>(),
                    (int)(Projectile.damage * FlareMult), Projectile.knockBack, Projectile.owner);
                return;
            }
            //按目标当前速度带提前量瞄准
            float frames = Projectile.Distance(target.Center) / (FireballSpeed * FireballUpdates);
            Vector2 aim = (target.Center + target.velocity * frames - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(source, Projectile.Center, aim * FireballSpeed, ModContent.ProjectileType<SatelliteStaffFireball>(),
                (int)(Projectile.damage * FireballMult), Projectile.knockBack, Projectile.owner, target.whoAmI);
        }

        //玩家手动锁定的目标优先,否则取索敌距离内最近的
        private NPC FindTarget() {
            NPC manual = Projectile.OwnerMinionAttackTargetNPC;
            if (manual != null && manual.CanBeChasedBy(Projectile) && Projectile.Distance(manual.Center) <= SeekRange) {
                return manual;
            }
            NPC best = null;
            float bestDistance = SeekRange;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                float distance = Projectile.Distance(npc.Center);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);
            float pulse = 1f - PulseDepth + PulseDepth * MathF.Sin(Main.GlobalTimeWrappedHourly * PulseSpeed + Projectile.identity);
            GodDraw.BeginAdditive();
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(GlowColor, GlowOpacity * pulse), 0f, GlowDiameter / GodDraw.Glow.Width);
            GodDraw.End();
            Main.spriteBatch.Draw(tex, (Projectile.Center - Main.screenPosition).Floor(), frame, Color.White, 0f, frame.Size() / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
