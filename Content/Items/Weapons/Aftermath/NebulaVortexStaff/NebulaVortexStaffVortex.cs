using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NebulaVortexStaff
{
    /// <summary>
    /// 星云涡旋:缓慢追向 30 格内最近的敌人,接触造成两倍伤害,每 20 帧从一根旋臂尖朝 40 格内的敌人射出一团星云烈焰;
    /// 自然到期时收拢,放出四团三倍伤害的蓝色星云烈焰。
    /// ai[0] = 目标 NPC 序号 + 1(0 为没有目标),所有者写入并同步,各端据此转向
    /// </summary>
    public class NebulaVortexStaffVortex : ModProjectile
    {
        #region 调参旋钮
        //存在时间(帧,30 秒)
        public const int Life = 30 * 60;
        //接触伤害倍率;接触命中冷却(帧,每秒 4 次)
        public const float ContactDamageMult = 2f;
        public const int ContactHitCooldown = 15;
        //接触判定半径(像素,3.5 格);绘制半径(像素,4.5 格)
        public const float ContactRadius = 56f;
        public const float VisualRadius = 72f;
        //追踪半径(像素,30 格)与开火半径(像素,40 格);重新索敌间隔(帧);所有者定期同步间隔(帧),压住各端位置漂移
        public const float ChaseRange = 30f * 16f;
        public const float FireRange = 40f * 16f;
        public const int RetargetInterval = 10;
        public const int SyncInterval = 120;
        //追踪速度与没有目标时的漂移速度(像素/帧);距目标小于此距离(像素)开始减速,停在目标身上而不是绕圈;速度插值比例(每帧)
        public const float ChaseSpeed = 2.6f;
        public const float WanderSpeed = 1.6f;
        public const float ArriveRadius = 48f;
        public const float SteerLerp = 0.06f;
        //开火间隔(帧,每秒 3 团);紫焰初速(像素/更新,每帧 3 次更新)
        public const int FireInterval = 20;
        public const float BlazeSpeed = 6f;
        //旋臂数、旋臂尖半径(像素)、转速(弧度/帧)
        public const int ArmCount = 3;
        public const float ArmTipRadius = 50f;
        public const float ArmSpin = 0.05f;
        //终结:蓝焰数量、伤害倍率、初速(像素/更新,每帧 4 次更新);有目标时朝目标扇形展开,相邻两团的夹角(弧度)
        public const int FinaleCount = 4;
        public const float FinaleDamageMult = 3f;
        public const float FinaleSpeed = 4f;
        public const float FinaleFanStep = 0.3f;
        //生成时膨胀与到期前收拢的帧数;收拢到的最小缩放;收拢期间不再开火
        public const int GrowFrames = 20;
        public const int CollapseFrames = 40;
        public const float CollapseMinScale = 0.35f;
        //呼吸幅度与速度(弧度/帧);核心光晕与旋臂尖光点半径(像素)
        public const float PulseAmount = 0.04f;
        public const float PulseSpeed = 0.08f;
        public const float CoreRadius = 22f;
        public const float TipGlowRadius = 12f;
        //Glow2 可见半径占边长的比例;HollowCircleHardEdge 边长(像素)
        public const float GlowVisibleFrac = 0.4f;
        public const float RingTexSize = 156f;
        //星尘:切向与向内速度(像素/帧),收拢时向内更快;寿命(帧)
        public const float MoteSwirl = 2.5f;
        public const float MoteInward = 1.5f;
        public const float MoteInwardCollapse = 4f;
        public const int MoteLife = 24;
        //接触火花数;提前移除时散开的星尘数
        public const int ContactSparkCount = 4;
        public const int PoofMoteCount = 14;
        //开涡音效音量;光照强度
        public const float SpawnVolume = 0.6f;
        public const float LightStrength = 1f;
        //开涡音效同时实例上限;开涡冲击环起始缩放与寿命(帧)
        public const int SpawnMaxInstances = 2;
        public const float RingStartScale = 0.1f;
        public const int SpawnRingLife = 20;
        //星尘:出生半径相对绘制半径的浮动范围、缩放范围、不透明度
        public const float MoteMinRadius = 0.85f;
        public const float MoteMaxRadius = 1.15f;
        public const float MoteMinScale = 0.25f;
        public const float MoteMaxScale = 0.4f;
        public const float MoteOpacity = 0.85f;
        //接触火花:散开角(弧度)、速度范围(像素/帧)、缩放、寿命(帧)
        public const float ContactSparkSpread = 0.4f;
        public const float ContactSparkMinSpeed = 3f;
        public const float ContactSparkMaxSpeed = 8f;
        public const float ContactSparkScale = 0.04f;
        public const int ContactSparkLife = 12;
        //散成星尘:散布半径占绘制半径的比例、速度上限(像素/帧)、缩放范围
        public const float PoofSpread = 0.7f;
        public const float PoofSpeed = 4f;
        public const float PoofMinScale = 0.3f;
        public const float PoofMaxScale = 0.5f;
        //核心常态亮度(收拢时升到 1);旋臂尖光点亮度
        public const float CoreBaseOpacity = 0.7f;
        public const float TipGlowOpacity = 0.8f;
        #endregion

        //涡体四层:自转倍率(相对 Projectile.rotation,负为反转)、相对缩放、颜色、是否加法;前两层带真实 alpha 压暗背景
        private static readonly (float Spin, float Scale, Color Tint, bool Additive)[] Layers = {
            (0.6f, 1f, new Color(60, 24, 110, 210), false),
            (-1.1f, 0.72f, new Color(110, 50, 170, 150), false),
            (1.4f, 1f, new Color(255, 90, 210) * 0.55f, true),
            (-2f, 0.6f, new Color(170, 120, 255) * 0.5f, true),
        };

        public static readonly Color NebulaPink = new Color(255, 90, 210);
        public static readonly Color NebulaLavender = new Color(170, 120, 255);
        public static readonly Color NebulaCore = new Color(255, 210, 245);
        //火花贴图的宽、长缩放比
        public static readonly Vector2 SparkSquash = new Vector2(0.4f, 1f);

        private int age;

        private NPC Target {
            get {
                int t = (int)Projectile.ai[0] - 1;
                if (t < 0 || t >= Main.maxNPCs) {
                    return null;
                }
                NPC npc = Main.npc[t];
                return npc.CanBeChasedBy(Projectile) ? npc : null;
            }
        }

        //涡体全在 PreDraw 里画,类自己不带贴图
        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = (int)(ContactRadius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = ContactHitCooldown;
            Projectile.timeLeft = Life;
            Projectile.netImportant = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI() {
            age++;
            if (!Main.player[Projectile.owner].active) {
                Projectile.Kill();
                return;
            }
            bool mine = Projectile.owner == Main.myPlayer;
            if (mine && age % RetargetInterval == 0) {
                float want = FindTarget(ChaseRange, false) + 1;
                if (want != Projectile.ai[0]) {
                    Projectile.ai[0] = want;
                    Projectile.netUpdate = true;
                }
            }
            if (mine && age % SyncInterval == 0) {
                Projectile.netUpdate = true;
            }
            Steer();
            Projectile.rotation += ArmSpin;
            Projectile.scale = CurrentScale();
            bool collapsing = Projectile.timeLeft <= CollapseFrames;
            if (mine && !collapsing && age % FireInterval == 0) {
                Fire();
            }
            Lighting.AddLight(Projectile.Center, NebulaLavender.ToVector3() * LightStrength * Projectile.scale);
            if (!Main.dedServ) {
                Effects(collapsing);
            }
        }

        private int FindTarget(float range, bool needSight) {
            int best = -1;
            float bestDist = range;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                if (needSight && !Collision.CanHitLine(Projectile.Center, 1, 1, npc.position, npc.width, npc.height)) {
                    continue;
                }
                float d = Vector2.Distance(npc.Center, Projectile.Center);
                if (d < bestDist) {
                    bestDist = d;
                    best = npc.whoAmI;
                }
            }
            return best;
        }

        private void Steer() {
            NPC target = Target;
            Vector2 desired;
            if (target != null) {
                Vector2 to = target.Center - Projectile.Center;
                desired = to.SafeNormalize(Vector2.Zero) * ChaseSpeed * MathHelper.Clamp(to.Length() / ArriveRadius, 0f, 1f);
            }
            else {
                desired = Projectile.velocity.SafeNormalize(Vector2.UnitX) * WanderSpeed;
            }
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, SteerLerp);
        }

        private float CurrentScale() {
            float grow = MathHelper.Clamp(age / (float)GrowFrames, 0f, 1f);
            grow = 1f - (1f - grow) * (1f - grow);
            float collapse = MathHelper.Clamp(Projectile.timeLeft / (float)CollapseFrames, 0f, 1f);
            return grow * MathHelper.Lerp(CollapseMinScale, 1f, collapse);
        }

        private Vector2 ArmTip(int arm) {
            float angle = Projectile.rotation + MathHelper.TwoPi * arm / ArmCount;
            return Projectile.Center + angle.ToRotationVector2() * (ArmTipRadius * Projectile.scale);
        }

        private void Fire() {
            int t = FindTarget(FireRange, true);
            if (t < 0) {
                return;
            }
            NPC target = Main.npc[t];
            Vector2 tip = ArmTip(age / FireInterval % ArmCount);
            //旋臂尖卡在墙里时改从涡心射出
            if (!Collision.CanHitLine(tip, 1, 1, target.position, target.width, target.height)) {
                tip = Projectile.Center;
            }
            Vector2 vel = (target.Center - tip).SafeNormalize(Vector2.UnitX) * BlazeSpeed;
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), tip, vel, ModContent.ProjectileType<NebulaVortexStaffBlaze>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        private void Effects(bool collapsing) {
            if (age == 1) {
                CEUtils.PlaySound("VortexSpawn", 1f, Projectile.Center, SpawnMaxInstances, SpawnVolume);
                PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, NebulaPink, RingStartScale).Configure(VisualRadius * 2f / RingTexSize, SpawnRingLife);
            }
            //从外缘卷向中心的星尘,顺着旋臂的转向
            Vector2 radial = CEUtils.randomRot().ToRotationVector2();
            Vector2 spawn = Projectile.Center + radial * (VisualRadius * Projectile.scale * Main.rand.NextFloat(MoteMinRadius, MoteMaxRadius));
            float inward = collapsing ? MoteInwardCollapse : MoteInward;
            Vector2 vel = radial.RotatedBy(MathHelper.PiOver2) * MoteSwirl - radial * inward + Projectile.velocity;
            Color mote = Main.rand.NextBool() ? NebulaPink : NebulaLavender;
            PRTLoader.NewParticle<PRT_Light>(spawn, vel, mote, Main.rand.NextFloat(MoteMinScale, MoteMaxScale)).Configure(MoteOpacity, lifetime: MoteLife);
            //接触火花与接触伤害同节奏,顺旋臂切向甩出;纯表现,各端自己判定
            if (age % ContactHitCooldown != 0) {
                return;
            }
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile) || !InContact(npc.Hitbox)) {
                    continue;
                }
                Vector2 swirl = (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                for (int i = 0; i < ContactSparkCount; i++) {
                    Vector2 v = swirl.RotatedByRandom(ContactSparkSpread) * Main.rand.NextFloat(ContactSparkMinSpeed, ContactSparkMaxSpeed);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(npc.Center, v, NebulaPink, ContactSparkScale).Configure(false, ContactSparkLife, SparkSquash, true);
                }
            }
        }

        private bool InContact(Rectangle box) {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, box.TopLeft(), box.BottomRight());
            float r = ContactRadius * Projectile.scale;
            return Vector2.DistanceSquared(nearest, Projectile.Center) < r * r;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => InContact(targetHitbox);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= ContactDamageMult;
            modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
        }

        public override void OnKill(int timeLeft) {
            //只有自然到期(timeLeft 走到 0)才放终结,有目标朝目标扇形射出,没有则十字散开;终结的声光由带标记的那团蓝焰在各端放
            if (timeLeft <= 0 && Projectile.owner == Main.myPlayer) {
                int type = ModContent.ProjectileType<NebulaVortexStaffBlazeBlue>();
                int damage = (int)(Projectile.damage * FinaleDamageMult);
                int t = FindTarget(FireRange, false);
                for (int i = 0; i < FinaleCount; i++) {
                    float angle = t >= 0
                        ? (Main.npc[t].Center - Projectile.Center).ToRotation() + (i - (FinaleCount - 1) / 2f) * FinaleFanStep
                        : Projectile.rotation + MathHelper.TwoPi * i / FinaleCount;
                    Vector2 vel = angle.ToRotationVector2() * FinaleSpeed;
                    float tag = i == 0 ? NebulaVortexStaffBlazeBlue.FinaleTag : 0f;
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, vel, type, damage, Projectile.knockBack, Projectile.owner, 0f, 0f, tag);
                }
            }
            if (Main.dedServ || timeLeft <= CollapseFrames) {
                return;
            }
            //被新涡旋替掉或主人离开:没有终结,散成星尘
            float r = VisualRadius * Projectile.scale;
            for (int i = 0; i < PoofMoteCount; i++) {
                Color mote = Main.rand.NextBool() ? NebulaPink : NebulaLavender;
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(r * PoofSpread), CEUtils.randomPointInCircle(PoofSpeed), mote, Main.rand.NextFloat(PoofMinScale, PoofMaxScale)).Configure(MoteOpacity, lifetime: MoteLife);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D vortex = CEUtils.getExtraTex("SoulVortex");
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float pulse = 1f + PulseAmount * MathF.Sin(age * PulseSpeed);
            float bodyScale = VisualRadius / (vortex.Width / 2f) * Projectile.scale * pulse;
            Vector2 vo = vortex.Size() / 2f;
            foreach (var layer in Layers) {
                if (!layer.Additive) {
                    Main.EntitySpriteDraw(vortex, center, null, layer.Tint, Projectile.rotation * layer.Spin, vo, bodyScale * layer.Scale, SpriteEffects.None);
                }
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            foreach (var layer in Layers) {
                if (layer.Additive) {
                    Main.EntitySpriteDraw(vortex, center, null, layer.Tint, Projectile.rotation * layer.Spin, vo, bodyScale * layer.Scale, SpriteEffects.None);
                }
            }
            //收拢时核心变大变亮
            float collapse = 1f - MathHelper.Clamp(Projectile.timeLeft / (float)CollapseFrames, 0f, 1f);
            float glowUnit = glow.Width * GlowVisibleFrac;
            Vector2 go = glow.Size() / 2f;
            Main.EntitySpriteDraw(glow, center, null, NebulaCore * MathHelper.Lerp(CoreBaseOpacity, 1f, collapse), 0f, go, CoreRadius * (1f + collapse) / glowUnit * pulse, SpriteEffects.None);
            for (int k = 0; k < ArmCount; k++) {
                Main.EntitySpriteDraw(glow, ArmTip(k) - Main.screenPosition, null, NebulaLavender * TipGlowOpacity, 0f, go, TipGlowRadius * Projectile.scale / glowUnit, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
