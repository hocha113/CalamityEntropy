using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.SharkSquad
{
    /// <summary>
    /// 四只小鲨的公共部分
    /// ai[0] 状态(0 恒为跟随),ai[1] 目标敌怪索引加 1(0 无),ai[2] 当前状态已过帧数(进入后第一帧为 1)
    /// 选目标和切状态只在拥有者端做并打 netUpdate,其余端照 ai[] 跑同一套移动;音效粒子各端看状态变化自己放
    /// </summary>
    public abstract class SharkSquadMinionBase : ModProjectile
    {
        //贴图 56×480,8 帧竖排: 0-3 闭嘴游动,4-7 张嘴游动,头朝左
        public const int SwimFrames = 4;
        public const int FrameCount = 8;
        /// <summary>身体中心在帧内的像素坐标(头朝左)</summary>
        public static readonly Vector2 Pivot = new Vector2(22f, 36f);
        public const float MouthForward = 17f;     //嘴在身体中心前方(像素)
        public const float MouthDrop = 4f;         //嘴在身体中心下方(像素)
        public const int HitboxWidth = 30;         //碰撞箱(像素),只括住身体不含头饰
        public const int HitboxHeight = 26;

        public const int StateFollow = 0;
        public const int SpawnGraceFrames = 120;   //出生后等增益同步到位的宽限(帧),之后靠增益续命
        public const float TargetRange = 1100f;    //索敌半径,自玩家量起(像素)
        public const float ForcedRangeMult = 1.4f; //玩家指定的目标放宽倍数
        public const float KeepRangeMult = 1.15f;  //已锁目标放宽倍数,免得在边界上来回换
        public const float TeleportRange = 2000f;  //离玩家超过就瞬移回站位(像素)
        public const float FollowMaxSpeed = 14f;   //跟随时在玩家速度之外最多再加(像素/帧)
        public const float FollowGain = 0.12f;     //离站位每像素换来的速度
        public const float FollowLerp = 0.12f;     //速度每帧向期望值靠拢的比例
        public const float TurnSpeed = 1.5f;       //相对玩家的横向速度超过它才按移动方向转身(像素/帧)
        public const float ArriveGain = 0.2f;      //接敌机动时的距离换速度系数
        public const float ArriveEpsilon = 0.5f;   //离目标点不到这个距离就不再朝它加速(像素)
        public const float BobRate = 0.07f;        //站位上下浮动的相位速度(弧度/帧)
        public const float BobHeight = 5f;         //浮动幅度(像素)
        public const int SwimTicksSlow = 8;        //慢游时每张帧停留的帧数
        public const int SwimTicksFast = 4;
        public const float FastSwimSpeed = 8f;     //超过这个速度换快游节奏(像素/帧)
        public const float TiltPerSpeed = 0.035f;  //竖直速度换俯仰(弧度每像素/帧)
        public const float MaxTilt = 0.45f;        //弧度
        public const float TiltLerp = 0.2f;
        public const float MinGlow = 0.25f;        //暗处保底亮度
        public const int ShotMouthFrames = 8;      //放出弹幕后张嘴的帧数
        public const int TrailLength = 8;          //残影缓存的帧数
        public const int TrailMode = 2;            //原版拖尾缓存模式: 位置、转角、朝向都记
        public const float TrailAlpha = 0.45f;     //最近一道残影的不透明度
        public const int PuffCount = 12;           //出场、瞬移、解散时的尘数
        public const float PuffSpeed = 3.5f;       //像素/帧
        public const float PuffSpreadX = 14f;      //尘的散布半径(像素)
        public const float PuffSpreadY = 12f;
        public const float PuffScaleMin = 0.9f;
        public const float PuffScaleMax = 1.3f;
        public const int DefaultHitCooldown = 20;  //不靠接触伤害的小鲨用不到,占位

        /// <summary>1 朝右,-1 朝左(贴图原向)</summary>
        protected int facing = -1;
        protected int mouthTimer;
        /// <summary>本帧由状态指定的贴图转角,没指定就按竖直速度俯仰</summary>
        protected float? poseRotation;
        private float swimClock;
        private int swimFrame;
        private int lastState = -1;
        private bool spawned;

        public int State => (int)Projectile.ai[0];
        public int Timer => (int)Projectile.ai[2];
        protected bool IsOwner => Projectile.owner == Main.myPlayer;
        public Vector2 MouthWorld => Projectile.Center + new Vector2(MouthForward * facing, MouthDrop).RotatedBy(Projectile.rotation);

        protected NPC Target {
            get {
                int index = (int)Projectile.ai[1] - 1;
                if (index < 0 || index >= Main.maxNPCs) {
                    return null;
                }
                NPC npc = Main.npc[index];
                return npc.CanBeChasedBy(Projectile) ? npc : null;
            }
        }

        /// <summary>没目标时相对玩家的站位(像素)</summary>
        protected abstract Vector2 IdleOffset { get; }
        protected abstract int PuffDust { get; }
        protected abstract void UpdateState(Player player, NPC target);
        protected virtual int HitCooldown => DefaultHitCooldown;
        /// <summary>各端在看到状态变了的那一帧调,放音效和粒子</summary>
        protected virtual void OnStateChanged(int oldState, int newState) { }
        /// <summary>拥有者端换了目标时调</summary>
        protected virtual void OnTargetChanged() { }
        protected virtual bool MouthOpen => mouthTimer > 0;
        /// <summary>残影浓度,0 不画</summary>
        protected virtual float TrailStrength => 0f;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailMode;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = HitboxWidth;
            Projectile.height = HitboxHeight;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = SpawnGraceFrames;
            Projectile.minion = true;
            Projectile.minionSlots = SharkSquadStaff.SlotsEach;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
        }

        public override bool? CanHitNPC(NPC target) => false;
        public override bool? CanCutTiles() => false;

        /// <summary>该玩家的某只小鲨,每人每种至多一只</summary>
        public static T Find<T>(int owner) where T : SharkSquadMinionBase {
            int type = ModContent.ProjectileType<T>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == owner && p.type == type) {
                    return p.ModProjectile as T;
                }
            }
            return null;
        }

        /// <summary>自己放出的弹幕首帧在各端回调: 张嘴,身体挨一下后坐</summary>
        public void OnShot(Vector2 dir, float recoil) {
            mouthTimer = Math.Max(mouthTimer, ShotMouthFrames);
            Projectile.velocity -= dir * recoil;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            Projectile.MinionCheck<SharkSquadBuff>();
            if (!spawned) {
                spawned = true;
                facing = player.direction;
                Puff();
            }
            if (Projectile.Distance(player.MountedCenter) > TeleportRange) {
                Puff();
                Projectile.Center = player.MountedCenter + IdleOffset;
                Projectile.velocity = Vector2.Zero;
                Puff();
                if (IsOwner) {
                    Projectile.ai[1] = 0f;
                    SetState(StateFollow);
                }
            }
            if (IsOwner) {
                NPC picked = PickTarget(player);
                int slot = picked == null ? 0 : picked.whoAmI + 1;
                if (slot != (int)Projectile.ai[1]) {
                    Projectile.ai[1] = slot;
                    Projectile.netUpdate = true;
                    OnTargetChanged();
                }
            }
            if (State != lastState) {
                if (lastState >= 0) {
                    OnStateChanged(lastState, State);
                }
                lastState = State;
            }
            Projectile.ai[2]++;
            poseRotation = null;
            UpdateState(player, Target);
            Animate();
        }

        /// <summary>玩家指定的目标优先,其次沿用当前目标,丢了才找离玩家最近的;四只因此会集火同一个</summary>
        private NPC PickTarget(Player player) {
            if (player.HasMinionAttackTargetNPC) {
                NPC forced = Main.npc[player.MinionAttackTargetNPC];
                if (forced.CanBeChasedBy(Projectile) && forced.Distance(player.Center) < TargetRange * ForcedRangeMult) {
                    return forced;
                }
            }
            NPC current = Target;
            if (current != null && current.Distance(player.Center) < TargetRange * KeepRangeMult) {
                return current;
            }
            NPC best = null;
            float bestDist = TargetRange;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                float dist = npc.Distance(player.Center);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        /// <summary>只在拥有者端调;timer 是新状态计时的起点</summary>
        protected void SetState(int state, int timer = 0) {
            Projectile.ai[0] = state;
            Projectile.ai[2] = timer;
            Projectile.netUpdate = true;
        }

        /// <summary>速度向期望值靠拢;期望值 = carry + 指向 dest、大小 min(maxSpeed, 距离 × gain)</summary>
        protected void FlyTo(Vector2 dest, float maxSpeed, float lerp, float gain, Vector2 carry) {
            Vector2 to = dest - Projectile.Center;
            float dist = to.Length();
            Vector2 want = carry;
            if (dist > ArriveEpsilon) {
                want += to / dist * Math.Min(maxSpeed, dist * gain);
            }
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, lerp);
        }

        protected void Follow(Player player) {
            Vector2 home = player.MountedCenter + IdleOffset;
            //拿站位的横坐标当相位差,四只不同步起伏
            home.Y += MathF.Sin(swimClock * BobRate + IdleOffset.X) * BobHeight;
            FlyTo(home, FollowMaxSpeed, FollowLerp, FollowGain, player.velocity);
            float drift = Projectile.velocity.X - player.velocity.X;
            facing = Math.Abs(drift) > TurnSpeed ? Math.Sign(drift) : player.direction;
        }

        /// <summary>让头指向世界角度 heading: 定朝向并给出本帧贴图转角</summary>
        protected void PointHead(float heading) {
            if (MathF.Cos(heading) >= 0f) {
                facing = 1;
                poseRotation = MathHelper.WrapAngle(heading);
            }
            else {
                facing = -1;
                poseRotation = MathHelper.WrapAngle(heading - MathHelper.Pi);
            }
        }

        private void Animate() {
            swimClock++;
            if (mouthTimer > 0) {
                mouthTimer--;
            }
            int ticks = Projectile.velocity.Length() > FastSwimSpeed ? SwimTicksFast : SwimTicksSlow;
            if (++Projectile.frameCounter >= ticks) {
                Projectile.frameCounter = 0;
                swimFrame = (swimFrame + 1) % SwimFrames;
            }
            Projectile.frame = swimFrame + (MouthOpen ? SwimFrames : 0);
            Projectile.spriteDirection = facing;
            if (poseRotation.HasValue) {
                Projectile.rotation = poseRotation.Value;
            }
            else {
                //头朝右时正转角是低头,朝左时相反
                float tilt = facing * MathHelper.Clamp(Projectile.velocity.Y * TiltPerSpeed, -MaxTilt, MaxTilt);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, tilt, TiltLerp);
            }
        }

        protected void Puff() {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < PuffCount; i++) {
                Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(PuffSpreadX, PuffSpreadY);
                Dust dust = Dust.NewDustPerfect(at, PuffDust, Main.rand.NextVector2Circular(PuffSpeed, PuffSpeed), 0, default, Main.rand.NextFloat(PuffScaleMin, PuffScaleMax));
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) {
            Puff();
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, FrameCount, 0, Projectile.frame);
            Color color = Color.Lerp(lightColor, Color.White, MinGlow);
            float trail = TrailStrength;
            if (trail > 0f) {
                int length = Projectile.oldPos.Length;
                for (int i = length - 1; i >= 1; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero) {
                        continue;
                    }
                    float fade = trail * TrailAlpha * (1f - i / (float)length);
                    DrawBody(tex, frame, Projectile.oldPos[i] + Projectile.Size / 2f, color * fade, Projectile.oldRot[i], Projectile.oldSpriteDirection[i]);
                }
            }
            DrawBody(tex, frame, Projectile.Center, color, Projectile.rotation, facing);
            return false;
        }

        private void DrawBody(Texture2D tex, Rectangle frame, Vector2 world, Color color, float rotation, int dir) {
            bool right = dir > 0;
            Vector2 origin = right ? new Vector2(frame.Width - Pivot.X, Pivot.Y) : Pivot;
            Main.EntitySpriteDraw(tex, world - Main.screenPosition, frame, color, rotation, origin, Projectile.scale, right ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }
    }

    /// <summary>快乐小鲨: 与目标水平对齐 → 连喷三发碎片 → 蓄势 → 水平冲刺(接触伤害 100%) → 缓一口气,一轮约 100 帧</summary>
    public class SharkSquadHappy : SharkSquadMinionBase
    {
        public const int StateAlign = 1;
        public const int StateSpit = 2;
        public const int StateWindup = 3;
        public const int StateDash = 4;
        public const int StateRest = 5;

        public const float AlignGap = 170f;        //对齐点离目标碰撞箱侧边的水平距离(像素)
        public const float AlignSpeed = 18f;       //像素/帧
        public const float AlignLerp = 0.16f;
        public const float AlignTolX = 56f;        //对齐容差(像素)
        public const float AlignTolY = 22f;
        public const int AlignMinFrames = 20;      //连同下面几段凑成一轮约 100 帧: 20 + 22 + 10 + 16 + 32
        public const int AlignMaxFrames = 50;      //追不上也照打,免得卡在对齐里
        public const int SpitCount = 3;            //策划: 喷 3 发
        public const int SpitInterval = 7;         //帧
        public const float ShardMult = 0.5f;       //策划: 每发 50%
        public const float ShardSpeed = 17f;       //像素/帧
        public const float SpitRecoil = 1.6f;      //每发后坐(像素/帧)
        public const int WindupFrames = 10;
        public const float WindupBackSpeed = 3.5f; //蓄势时后撤(像素/帧)
        public const float WindupLerp = 0.3f;
        public const float DashSpeed = 24f;        //相对目标的冲刺速度(像素/帧)
        public const int DashFrames = 16;          //共 384 像素,从目标一侧穿到另一侧
        public const float DashSteerGain = 0.3f;   //竖直偏差每像素换来的修正速度
        public const float DashSteerMax = 6f;      //冲刺中竖直修正上限(像素/帧)
        public const int RestFrames = 32;
        public const float RestDrag = 0.88f;       //缓气时每帧保留的速度比例
        public const int DashHitCooldown = 30;     //比一次冲刺长、比一轮循环短,每次冲刺每个敌怪只吃一下

        //以下只管表现
        public const int DashDustPerFrame = 2;
        public const float DashDustSpread = 10f;   //像素
        public const float DashDustDragMin = 0.1f; //沙尘带走冲刺速度的比例
        public const float DashDustDragMax = 0.3f;
        public const float DashDustScaleMin = 1f;
        public const float DashDustScaleMax = 1.4f;
        public const int ImpactDust = 10;
        public const float ImpactSpread = 0.9f;    //撞击尘沿冲刺方向的散角(弧度)
        public const float ImpactSpeedMin = 3f;    //像素/帧
        public const float ImpactSpeedMax = 9f;
        public const float ImpactDustScaleMin = 0.9f;
        public const float ImpactDustScaleMax = 1.4f;
        public const float ImpactRingStart = 0.05f; //命中环的起始缩放
        public const float ImpactRingScale = 0.45f; //命中环的终值缩放,环贴图 156 像素,即约 70 像素
        public const int ImpactRingLife = 10;      //帧
        public const float DashPitch = 1.35f;      //CEUtils.PlaySound 的音高,1 为原调
        public const float DashVolume = 0.28f;
        public const float ImpactPitch = 1.1f;
        public const float ImpactVolume = 0.3f;
        public const int SoundCap = 3;             //同一音效的并发上限
        public static readonly Color ImpactColor = new Color(255, 214, 96);

        /// <summary>这次冲刺的命中特效放过没有,各端自己记</summary>
        private bool impactShown;

        protected override Vector2 IdleOffset => new Vector2(-46f, -44f);
        protected override int PuffDust => DustID.Sand;
        protected override int HitCooldown => DashHitCooldown;
        protected override float TrailStrength => State == StateDash ? 1f : 0f;
        protected override bool MouthOpen => base.MouthOpen || State is StateSpit or StateWindup or StateDash;

        public override bool? CanHitNPC(NPC target) => State == StateDash ? null : false;

        protected override void UpdateState(Player player, NPC target) {
            int t = Timer;
            switch (State) {
                case StateFollow:
                    Follow(player);
                    if (IsOwner && target != null) {
                        SetState(StateAlign);
                    }
                    break;
                case StateAlign:
                case StateSpit:
                    if (target == null) {
                        GiveUp(player);
                        break;
                    }
                    HoldLine(target, t);
                    break;
                case StateWindup:
                    if (target == null) {
                        GiveUp(player);
                        break;
                    }
                    Windup(target, t);
                    break;
                case StateDash:
                    Dash(target, t);
                    break;
                case StateRest:
                    Projectile.velocity *= RestDrag;
                    if (IsOwner && t >= RestFrames) {
                        SetState(target != null ? StateAlign : StateFollow);
                    }
                    break;
            }
        }

        private void GiveUp(Player player) {
            Follow(player);
            if (IsOwner) {
                SetState(StateFollow);
            }
        }

        /// <summary>对齐与喷射共用: 守在目标同一水平线、自己这一侧的对齐点上</summary>
        private void HoldLine(NPC target, int t) {
            int side = Projectile.Center.X >= target.Center.X ? 1 : -1;
            Vector2 dest = target.Center + new Vector2(side * (target.width / 2f + AlignGap), 0f);
            FlyTo(dest, AlignSpeed, AlignLerp, ArriveGain, target.velocity);
            facing = -side;
            if (!IsOwner) {
                return;
            }
            if (State == StateAlign) {
                bool aligned = Math.Abs(Projectile.Center.X - dest.X) < AlignTolX && Math.Abs(Projectile.Center.Y - dest.Y) < AlignTolY;
                if ((aligned && t >= AlignMinFrames) || t >= AlignMaxFrames) {
                    SetState(StateSpit);
                }
                return;
            }
            //进入状态后第一帧 t 为 1,所以拿 t - 1 排发射时刻
            if ((t - 1) % SpitInterval == 0 && (t - 1) / SpitInterval < SpitCount) {
                Spit(target);
            }
            if (t > SpitCount * SpitInterval) {
                SetState(StateWindup);
            }
        }

        private void Spit(NPC target) {
            Vector2 mouth = MouthWorld;
            Vector2 aim = (target.Center - mouth).SafeNormalize(new Vector2(facing, 0f));
            int damage = Math.Max(1, (int)(Projectile.damage * ShardMult));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), mouth, aim * ShardSpeed, ModContent.ProjectileType<SharkSquadShard>(), damage, Projectile.knockBack, Projectile.owner);
        }

        private void Windup(NPC target, int t) {
            int dir = target.Center.X >= Projectile.Center.X ? 1 : -1;
            facing = dir;
            Vector2 back = target.velocity + new Vector2(-dir * WindupBackSpeed, 0f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, back, WindupLerp);
            if (IsOwner && t >= WindupFrames) {
                //叠上目标的横向速度,目标在跑也穿得过去
                Projectile.velocity = new Vector2(dir * DashSpeed + target.velocity.X, 0f);
                SetState(StateDash);
            }
        }

        private void Dash(NPC target, int t) {
            facing = Projectile.velocity.X >= 0f ? 1 : -1;
            if (t > DashFrames) {
                //非拥有者端等同步包的这几帧先减速
                Projectile.velocity *= RestDrag;
            }
            else {
                Projectile.velocity.Y = target == null ? 0f : MathHelper.Clamp((target.Center.Y - Projectile.Center.Y) * DashSteerGain, -DashSteerMax, DashSteerMax);
                if (!Main.dedServ) {
                    for (int i = 0; i < DashDustPerFrame; i++) {
                        Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(DashDustSpread, DashDustSpread);
                        Dust dust = Dust.NewDustPerfect(at, DustID.Sand, -Projectile.velocity * Main.rand.NextFloat(DashDustDragMin, DashDustDragMax), 0, default, Main.rand.NextFloat(DashDustScaleMin, DashDustScaleMax));
                        dust.noGravity = true;
                    }
                }
                if (!impactShown && target != null && Projectile.Hitbox.Intersects(target.Hitbox)) {
                    impactShown = true;
                    Impact();
                }
            }
            if (IsOwner && t >= DashFrames) {
                SetState(StateRest);
            }
        }

        /// <summary>冲刺撞上目标的那一帧,各端按碰撞箱相交自己判,不走只在拥有者端触发的命中钩子</summary>
        private void Impact() {
            if (Main.dedServ) {
                return;
            }
            Vector2 at = MouthWorld;
            Vector2 dir = new Vector2(facing, 0f);
            for (int i = 0; i < ImpactDust; i++) {
                Vector2 velocity = dir.RotatedByRandom(ImpactSpread) * Main.rand.NextFloat(ImpactSpeedMin, ImpactSpeedMax);
                Dust dust = Dust.NewDustPerfect(at, DustID.Sandnado, velocity, 0, default, Main.rand.NextFloat(ImpactDustScaleMin, ImpactDustScaleMax));
                dust.noGravity = true;
            }
            PRTLoader.NewParticle<PRT_PulseRing>(at, Vector2.Zero, ImpactColor, ImpactRingStart).Configure(ImpactRingScale, ImpactRingLife);
            CEUtils.PlaySound("DnBite", ImpactPitch, at, SoundCap, ImpactVolume);
        }

        protected override void OnStateChanged(int oldState, int newState) {
            if (newState != StateDash) {
                return;
            }
            impactShown = false;
            Puff();
            CEUtils.PlaySound("Dash2", DashPitch, Projectile.Center, SoundCap, DashVolume);
        }
    }

    /// <summary>暴躁小鲨: 冲上去咬住目标,挂在它身上每 18 帧咬一口(60%),只伤被咬住的那一个</summary>
    public class SharkSquadGrumpy : SharkSquadMinionBase
    {
        public const int StateChase = 1;
        public const int StateLatched = 2;

        public const float BiteMult = 0.6f;        //策划: 60%
        public const int BiteCooldown = 18;        //策划: 18 帧局部无敌帧,也是一口的节拍
        public const float BiteKnockback = 0.25f;  //击退倍率,咬住不该把目标顶飞
        public const float ChaseSpeed = 20f;       //像素/帧
        public const float ChaseLerp = 0.14f;
        public const float ChaseGain = 0.25f;      //追击时的距离换速度系数
        public const float LatchInset = 0.55f;     //咬点落在目标碰撞箱半宽高的这个比例处,两个碰撞箱始终相交
        public const float ShakeAmplitude = 3f;    //咬住时甩头幅度(像素)
        public const float ShakeRate = 0.9f;       //弧度/帧
        public const int ChompShutFrames = 6;      //每口咬下后闭嘴的帧数
        public const float ChaseTrail = 0.6f;      //追击时的残影浓度

        //以下只管表现
        public const int ChompDust = 2;
        public const float ChompDustSpeed = 3f;    //像素/帧
        public const float ChompDustScaleMin = 0.7f;
        public const float ChompDustScaleMax = 1f;
        public const float ChompPitchMin = 1.3f;   //CEUtils.PlaySound 的音高,1 为原调
        public const float ChompPitchMax = 1.5f;
        public const float ChompVolume = 0.2f;
        public const int SoundCap = 3;             //同一音效的并发上限

        /// <summary>咬点相对目标中心的方位角,随同步包走</summary>
        private float biteAngle;

        protected override Vector2 IdleOffset => new Vector2(-92f, -14f);
        protected override int PuffDust => DustID.Blood;
        protected override int HitCooldown => BiteCooldown;
        protected override float TrailStrength => State == StateChase ? ChaseTrail : 0f;
        protected override bool MouthOpen => State == StateChase || (State == StateLatched && (Timer - 1) % BiteCooldown >= ChompShutFrames);

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(biteAngle);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            biteAngle = reader.ReadSingle();
        }

        public override bool? CanHitNPC(NPC target) => State == StateLatched && target.whoAmI == (int)Projectile.ai[1] - 1 ? null : false;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= BiteMult;
            modifiers.Knockback *= BiteKnockback;
            modifiers.HitDirectionOverride = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
        }

        protected override void OnTargetChanged() {
            if (State == StateLatched) {
                SetState(Projectile.ai[1] > 0f ? StateChase : StateFollow);
            }
        }

        protected override void UpdateState(Player player, NPC target) {
            if (target == null) {
                Follow(player);
                if (IsOwner && State != StateFollow) {
                    SetState(StateFollow);
                }
                return;
            }
            switch (State) {
                case StateFollow:
                    Follow(player);
                    if (IsOwner) {
                        SetState(StateChase);
                    }
                    break;
                case StateChase:
                    FlyTo(target.Center, ChaseSpeed, ChaseLerp, ChaseGain, target.velocity);
                    facing = target.Center.X >= Projectile.Center.X ? 1 : -1;
                    if (IsOwner && Projectile.Hitbox.Intersects(target.Hitbox)) {
                        biteAngle = (Projectile.Center - target.Center).ToRotation();
                        SetState(StateLatched);
                    }
                    break;
                case StateLatched:
                    Latch(target);
                    break;
            }
        }

        private void Latch(NPC target) {
            Vector2 dir = biteAngle.ToRotationVector2();
            Vector2 inset = new Vector2(target.width, target.height) / 2f * LatchInset;
            //小目标上退到嘴刚好够得着中心的距离,不整只盖在目标上
            float reach = Math.Max((dir * inset).Length(), MouthForward);
            Vector2 anchor = target.Center + dir * reach;
            Vector2 sway = (biteAngle + MathHelper.PiOver2).ToRotationVector2() * (MathF.Sin(Timer * ShakeRate) * ShakeAmplitude);
            Projectile.Center = anchor + sway;
            Projectile.velocity = Vector2.Zero;
            PointHead(biteAngle + MathHelper.Pi);
            if ((Timer - 1) % BiteCooldown == 0) {
                Chomp();
            }
        }

        /// <summary>每口的声与尘,各端按 ai[2] 的节拍自己放;拥有者端的实际伤害走局部无敌帧,与它同周期</summary>
        private void Chomp() {
            if (Main.dedServ) {
                return;
            }
            Vector2 at = MouthWorld;
            for (int i = 0; i < ChompDust; i++) {
                Dust.NewDustPerfect(at, DustID.Bone, Main.rand.NextVector2Circular(ChompDustSpeed, ChompDustSpeed), 0, default, Main.rand.NextFloat(ChompDustScaleMin, ChompDustScaleMax));
            }
            CEUtils.PlaySound("DnBite", Main.rand.NextFloat(ChompPitchMin, ChompPitchMax), at, SoundCap, ChompVolume);
        }
    }

    /// <summary>懒散小鲨: 慢吞吞地守在目标侧上方,每 30 帧吐一发追踪的诅咒火球(100%)</summary>
    public class SharkSquadLazy : SharkSquadMinionBase
    {
        public const int StateCast = 1;

        public const float KeepDistance = 260f;    //与目标碰撞箱侧边保持的水平距离(像素)
        public const float KeepHeight = 90f;       //比目标中心高出(像素)
        public const float DriftSpeed = 9f;        //像素/帧,比另三只慢一半
        public const float DriftLerp = 0.06f;
        public const float DriftGain = 0.1f;       //距离换速度系数
        public const float DriftCarry = 0.5f;      //只跟目标一半的速度
        public const int CastInterval = 30;        //策划: 每 0.5 秒一发
        public const int FirstCastDelay = 12;      //接敌后第一发的延迟(帧)
        public const float CastRange = 900f;       //离目标超过这个距离不放(像素)
        public const float FireballSpeed = 9f;     //出膛速度(像素/帧),火球自己再提速
        public const float LobAngle = 0.5f;        //出膛上扬的角度(弧度)
        public const float CastRecoil = 2f;        //像素/帧

        /// <summary>距下一发的帧数,只在拥有者端走</summary>
        private int castCooldown;

        protected override Vector2 IdleOffset => new Vector2(92f, -14f);
        protected override int PuffDust => DustID.CursedTorch;

        protected override void UpdateState(Player player, NPC target) {
            if (target == null) {
                Follow(player);
                if (IsOwner && State != StateFollow) {
                    SetState(StateFollow);
                }
                return;
            }
            if (State == StateFollow) {
                Follow(player);
                if (IsOwner) {
                    castCooldown = FirstCastDelay;
                    SetState(StateCast);
                }
                return;
            }
            int side = player.Center.X >= target.Center.X ? 1 : -1;
            Vector2 dest = target.Center + new Vector2(side * (target.width / 2f + KeepDistance), -KeepHeight);
            FlyTo(dest, DriftSpeed, DriftLerp, DriftGain, target.velocity * DriftCarry);
            facing = target.Center.X >= Projectile.Center.X ? 1 : -1;
            if (IsOwner && --castCooldown <= 0 && Projectile.Distance(target.Center) < CastRange) {
                castCooldown = CastInterval;
                Cast(target);
            }
        }

        private void Cast(NPC target) {
            Vector2 mouth = MouthWorld;
            Vector2 aim = (target.Center - mouth).SafeNormalize(new Vector2(facing, 0f));
            //朝上偏一个角度出膛,火球先上扬再拐向目标
            Vector2 velocity = aim.RotatedBy(-facing * LobAngle) * FireballSpeed;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), mouth, velocity, ModContent.ProjectileType<SharkSquadCursedFireball>(), Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI + 1);
        }
    }

    /// <summary>勇敢小鲨: 抢到目标头顶,低头每 20 帧向下射一把光剑(66%,无视 20 护甲)</summary>
    public class SharkSquadBrave : SharkSquadMinionBase
    {
        public const int StateStrike = 1;

        public const float HoverHeight = 170f;     //悬停点高出目标碰撞箱顶边(像素)
        public const float RushSpeed = 22f;        //像素/帧
        public const float RushLerp = 0.18f;
        public const float RushGain = 0.22f;       //距离换速度系数
        public const float LeadFrames = 6f;        //悬停点按目标横向速度的提前量(帧)
        public const float FaceDeadzone = 8f;      //水平偏差小于它不转身(像素)
        public const float FireTolX = 70f;         //水平偏差在这之内(另加半个目标宽)才算到了头顶(像素)
        public const int FireInterval = 20;        //策划: 发射 cd 20 帧
        public const float SwordMult = 0.66f;      //策划: 66%
        public const float SwordSpeed = 14f;       //每次更新的速度,光剑一帧两次更新
        public const float SwordDrop = 12f;        //出剑点在身体中心下方(像素)
        public const float AimLead = 4f;           //瞄准时按目标速度的提前量(帧)
        public const float FireRecoil = 2.2f;      //像素/帧
        public const float NoseDownTilt = 0.6f;    //到位后低头的角度(弧度)
        public const float NoseDownLerp = 0.15f;

        /// <summary>距下一剑的帧数,只在拥有者端走</summary>
        private int fireCooldown;
        /// <summary>0-1,低头程度,只管表现</summary>
        private float noseDown;

        protected override Vector2 IdleOffset => new Vector2(46f, -44f);
        protected override int PuffDust => DustID.PinkTorch;

        protected override void UpdateState(Player player, NPC target) {
            if (target == null) {
                noseDown = 0f;
                Follow(player);
                if (IsOwner && State != StateFollow) {
                    SetState(StateFollow);
                }
                return;
            }
            if (State == StateFollow) {
                Follow(player);
                if (IsOwner) {
                    SetState(StateStrike);
                }
                return;
            }
            Vector2 dest = new Vector2(target.Center.X + target.velocity.X * LeadFrames, target.position.Y - HoverHeight);
            FlyTo(dest, RushSpeed, RushLerp, RushGain, target.velocity);
            float dx = target.Center.X - Projectile.Center.X;
            if (Math.Abs(dx) > FaceDeadzone) {
                facing = Math.Sign(dx);
            }
            bool above = Math.Abs(dx) < FireTolX + target.width / 2f && Projectile.Center.Y < target.Center.Y;
            noseDown = MathHelper.Lerp(noseDown, above ? 1f : 0f, NoseDownLerp);
            float tilt = MathHelper.Clamp(Projectile.velocity.Y * TiltPerSpeed, -MaxTilt, MaxTilt);
            poseRotation = facing * MathHelper.Lerp(tilt, NoseDownTilt, noseDown);
            if (!IsOwner) {
                return;
            }
            if (fireCooldown > 0) {
                fireCooldown--;
            }
            if (above && fireCooldown <= 0) {
                fireCooldown = FireInterval;
                Fire(target);
            }
        }

        private void Fire(NPC target) {
            Vector2 from = Projectile.Center + new Vector2(0f, SwordDrop);
            Vector2 aim = (target.Center + target.velocity * AimLead - from).SafeNormalize(Vector2.UnitY);
            int damage = Math.Max(1, (int)(Projectile.damage * SwordMult));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), from, aim * SwordSpeed, ModContent.ProjectileType<SharkSquadLightSword>(), damage, Projectile.knockBack, Projectile.owner, 0f, Main.rand.Next(SharkSquadLightSword.Variants));
        }
    }
}
