using System;
using Terraria;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.StarDance
{
    /// <summary>
    /// 蓝球:飞出一小段后追踪,命中后像利刃台风那样绕着目标转,转够时间就消失,期间一直造成伤害;
    /// ai[0] 为追踪目标序号+1,ai[1] 为环绕目标序号+1,都由主人端写入并同步
    /// </summary>
    public class StarDanceOrbBlue : StarDanceOrb
    {
        #region 调参旋钮
        //出手速度、追踪速度(像素/更新)与每次更新朝目标转向的插值
        public const float LaunchSpeed = 11f;
        public const float HomingSpeed = 18f;
        public const float HomingLerp = 0.12f;
        //飞出15帧后追踪45格内的目标(策划)
        public const int HomingDelay = 15;
        public const float HomingRange = 720f;
        //一直没打中时的寿命(更新次数)
        public const int LifeUpdates = 150;
        //绕目标0.5秒,从命中那一刻算(策划)
        public const int OrbitFrames = 30;
        //10静态无敌帧(策划),所有蓝球共用
        public const int StaticCooldown = 10;
        //环绕半径 = 目标半个对角线 + 这个距离,再夹在上下限之间(像素)
        public const float OrbitPad = 20f;
        public const float OrbitMin = 36f;
        public const float OrbitMax = 150f;
        //环绕角速度(弧度/帧),0.5秒约转一圈半;从命中点收拢到环绕半径的帧数
        public const float OrbitSpin = 0.32f;
        public const int OrbitEaseFrames = 8;
        #endregion

        public static readonly Color BodyColor = new(60, 104, 240);
        public static readonly Color RimColor = new(170, 204, 255);

        protected override Color Body => BodyColor;
        protected override Color Rim => RimColor;
        protected override int Lifetime => LifeUpdates;

        private bool orbiting;
        private int orbitAge;
        private float orbitAngle;
        private float orbitStartRadius;
        private int orbitDir;

        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = StaticCooldown;
        }

        private static NPC ValidNPC(float index) {
            int i = (int)index - 1;
            if (i < 0 || i >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[i];
            return npc.active && npc.life > 0 ? npc : null;
        }

        private NPC OrbitTarget => orbiting ? ValidNPC(Projectile.ai[1]) : null;

        protected override void Move() {
            if (Projectile.ai[1] > 0f) {
                Orbit();
                return;
            }
            if (Dying) {
                Projectile.velocity *= DyingDrag;
                return;
            }
            if (age < HomingDelay)
                return;
            if (Projectile.owner == Main.myPlayer) {
                NPC current = ValidNPC(Projectile.ai[0]);
                if (current == null || !current.CanBeChasedBy(Projectile)) {
                    NPC found = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, HomingRange);
                    float next = found == null ? 0f : found.whoAmI + 1;
                    if (next != Projectile.ai[0]) {
                        Projectile.ai[0] = next;
                        Projectile.netUpdate = true;
                    }
                }
            }
            NPC target = ValidNPC(Projectile.ai[0]);
            if (target != null) {
                Vector2 want = Projectile.DirectionTo(target.Center) * HomingSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, HomingLerp);
            }
        }

        //各端从看到 ai[1] 变化的那一刻起自己计时;目标没了就安静缩小
        private void Orbit() {
            NPC target = ValidNPC(Projectile.ai[1]);
            if (target == null) {
                if (Projectile.timeLeft > ShrinkFrames)
                    Projectile.timeLeft = ShrinkFrames;
                Projectile.velocity *= DyingDrag;
                return;
            }
            if (!orbiting) {
                orbiting = true;
                Vector2 offset = Projectile.Center - target.Center;
                orbitAngle = offset.ToRotation();
                orbitStartRadius = offset.Length();
                //沿着飞来时的绕向继续转
                orbitDir = offset.X * Projectile.velocity.Y - offset.Y * Projectile.velocity.X >= 0f ? 1 : -1;
                if (!Main.dedServ)
                    Burst();
            }
            if (Projectile.FinalExtraUpdate()) {
                orbitAge++;
                orbitAngle += OrbitSpin * orbitDir;
            }
            if (orbitAge < OrbitFrames)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, ShrinkFrames + 1);
            else if (Projectile.timeLeft > ShrinkFrames)
                Projectile.timeLeft = ShrinkFrames;
            float radius = Math.Clamp(target.Size.Length() / 2f + OrbitPad, OrbitMin, OrbitMax);
            radius = MathHelper.Lerp(orbitStartRadius, radius, Utils.GetLerpValue(0f, OrbitEaseFrames, orbitAge, true));
            Vector2 goal = target.Center + orbitAngle.ToRotationVector2() * radius;
            Projectile.velocity = goal - Projectile.Center;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            NPC target = OrbitTarget;
            if (target != null && targetHitbox == target.Hitbox)
                return true;
            return null;
        }

        //命中不扣次数,只记下环绕目标;消失交给环绕计时
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.ai[1] > 0f)
                return;
            Projectile.ai[1] = target.whoAmI + 1;
            Projectile.netUpdate = true;
        }
    }
}
