using CalamityEntropy.Content.NPCs.VoidDestroyer;
using CalamityEntropy.Content.NPCs.VoidDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;

namespace CalamityEntropy.Content.Projectiles.VoidDestroyer
{
    /// <summary>
    /// 深度弹幕的生成来源:把初始 Z / Z 速度 / Z 加速度随 NewProjectile 一起交给 <see cref="VDDepthProjectile.OnSpawn"/>,
    /// 这样生成包里的 ExtraAI 就已经是正确的深度,客户端不会有一帧把它当成平面弹(Z = 0 即带内)
    /// </summary>
    public sealed class VDDepthSource : IEntitySource
    {
        public string Context => "VDDepth";
        public NPC Npc;
        public float Z;
        public float ZVel;
        public float ZAccel;

        public VDDepthSource(NPC npc, float z, float zVel, float zAccel) {
            Npc = npc;
            Z = z;
            ZVel = zVel;
            ZAccel = zAccel;
        }
    }

    /// <summary>
    /// 驱逐舰深度弹幕基类:在 <see cref="VDHostileProjectile"/> 之上加一根 Z 轴。
    /// 平面坐标(Projectile.Center / velocity)仍是 gameplay,Z 只决定:绘制的透视投影与缩放、所在绘制层、
    /// 以及是否有判定(<c>|Z| ≤ 判定带</c> 才碰撞)。Z 状态随生成包过线,之后各端按帧确定性积分;
    /// 子类只写 <see cref="DepthAI"/>(平面运动 / 模式逻辑)与 <see cref="DrawDepth"/>(用投影后的量绘制)。
    /// 逼近平面时基类自动画落点标记(所有 Z 轴弹幕的公平阀),越过镜头那一瞬自动放呼啸
    /// </summary>
    public abstract class VDDepthProjectile : VDHostileProjectile, IVDDepthDrawable
    {
        /// <summary>当前深度(0 = 玩家平面)</summary>
        public float Z;
        public float ZVel;
        public float ZAccel;

        /// <summary>轨迹环形缓冲(平面位置 + 当时的 Z),新在 0</summary>
        public const int TrailLength = 8;
        protected readonly Vector2[] trailPos = new Vector2[TrailLength];
        protected readonly float[] trailZ = new float[TrailLength];
        protected int trailCount;

        private bool whooshed;
        private bool incomingCued;
        /// <summary>本帧分到的层(DrawBehind 里判定)</summary>
        protected VDDepthLayer Layer { get; private set; }

        float IVDDepthDrawable.DepthZ => Z;

        /// <summary>是否有 Z 运动(Z 为 0 且不动的弹就是普通平面弹,基类的深度逻辑全部旁路)</summary>
        public bool HasDepth => Z != 0f || ZVel != 0f || ZAccel != 0f;

        /// <summary>正在向平面逼近(远端向近、或近端向远)</summary>
        public bool Approaching => (Z > 0f && ZVel < 0f) || (Z < 0f && ZVel > 0f);

        /// <summary>投影后的世界坐标中心(减 screenPosition 即屏幕坐标)</summary>
        public Vector2 ProjectedCenter => VDDepth.Project(Projectile.Center, Z);

        /// <summary>绘制缩放 = 弹幕自身 scale × 透视缩放</summary>
        public float DrawScale => Projectile.scale * VDDepth.Scale(Z);

        /// <summary>落点标记半径 / 颜色,子类按弹体大小覆盖</summary>
        public virtual float MarkerRadius => VDDirector.DepthMarkerRadius;
        public virtual Color MarkerColor => VDVfx.DepthMarker;
        /// <summary>是否画落点标记(带外且逼近;子类可关)</summary>
        public virtual bool WantsMarker => HasDepth && Approaching && !VDDepth.InHitBand(Z, ZVel);

        /// <summary>
        /// 距到达平面的帧数(按当前 Z 速度与加速度解一元二次;不逼近返回 -1)。
        /// 标记进度与落点预测都从它来
        /// </summary>
        public float FramesToPlane() {
            if (!Approaching) {
                return -1f;
            }
            if (Math.Abs(ZAccel) < 1e-5f) {
                return Math.Abs(Z / ZVel);
            }
            //Z + ZVel t + 0.5 a t² = 0,取最小正根
            float a = 0.5f * ZAccel;
            float disc = ZVel * ZVel - 4f * a * Z;
            if (disc < 0f) {
                return Math.Abs(Z / ZVel);
            }
            float sq = MathF.Sqrt(disc);
            float t1 = (-ZVel + sq) / (2f * a);
            float t2 = (-ZVel - sq) / (2f * a);
            float best = float.MaxValue;
            if (t1 > 0f) {
                best = t1;
            }
            if (t2 > 0f && t2 < best) {
                best = t2;
            }
            return best == float.MaxValue ? Math.Abs(Z / ZVel) : best;
        }

        /// <summary>预测落点(平面坐标):按当前平面速度直线外推到到达平面那一帧</summary>
        public virtual Vector2 PredictedLanding() {
            float frames = FramesToPlane();
            if (frames < 0f) {
                return Projectile.Center;
            }
            return Projectile.Center + Projectile.velocity * frames;
        }

        /// <summary>标记进度 0..1:距平面还有 DepthMarkerLeadFrames 帧时开始,到达时 1</summary>
        public float MarkerProgress() {
            float frames = FramesToPlane();
            if (frames < 0f) {
                return 0f;
            }
            return 1f - MathHelper.Clamp(frames / VDDirector.DepthMarkerLeadFrames, 0f, 1f);
        }

        #region 生成与同步
        public override void OnSpawn(IEntitySource source) {
            if (source is VDDepthSource depth) {
                Z = depth.Z;
                ZVel = depth.ZVel;
                ZAccel = depth.ZAccel;
            }
            for (int i = 0; i < TrailLength; i++) {
                trailPos[i] = Projectile.Center;
                trailZ[i] = Z;
            }
            OnDepthSpawn(source);
        }

        /// <summary>子类的生成钩(深度已就位)</summary>
        protected virtual void OnDepthSpawn(IEntitySource source) {
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Z);
            writer.Write(ZVel);
            writer.Write(ZAccel);
            SendDepthExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            Z = reader.ReadSingle();
            ZVel = reader.ReadSingle();
            ZAccel = reader.ReadSingle();
            ReceiveDepthExtraAI(reader);
        }

        protected virtual void SendDepthExtraAI(BinaryWriter writer) {
        }

        protected virtual void ReceiveDepthExtraAI(BinaryReader reader) {
        }
        #endregion

        #region AI
        public sealed override void AI() {
            DepthAI();
            StepDepth();
            PushTrail();
            DepthCues();
        }

        /// <summary>子类 AI:平面运动与模式逻辑。Z 的积分在它之后由基类做,想改 Z 速度就在这里写 ZVel / ZAccel</summary>
        protected abstract void DepthAI();

        /// <summary>Z 积分</summary>
        protected virtual void StepDepth() {
            if (!HasDepth) {
                return;
            }
            ZVel += ZAccel;
            Z += ZVel;
        }

        protected void PushTrail() {
            for (int i = TrailLength - 1; i > 0; i--) {
                trailPos[i] = trailPos[i - 1];
                trailZ[i] = trailZ[i - 1];
            }
            trailPos[0] = Projectile.Center;
            trailZ[0] = Z;
            if (trailCount < TrailLength) {
                trailCount++;
            }
        }

        /// <summary>纯本地声画:越过镜头的呼啸(一次)、逼近开始的来袭音(一次,限流)</summary>
        private void DepthCues() {
            if (Main.dedServ || !HasDepth) {
                return;
            }
            if (!whooshed && ZVel < 0f && Z <= VDDirector.DepthWhooshZ) {
                whooshed = true;
                float strength = WhooshStrength;
                //一群弹同帧掠过镜头时子类把大多数成员的强度置 0,只留一两声呼啸
                if (strength > 0f) {
                    VDVfx.PassBy(ProjectedCenter, strength);
                }
            }
            if (!incomingCued && WantsMarker && MarkerProgress() > 0f) {
                incomingCued = true;
                if (Projectile.identity % 3 == 0) {
                    CEUtils.PlaySound("vbapear", VDDepth.DopplerPitch(Z), PredictedLanding(), 3, 0.35f);
                }
            }
        }

        /// <summary>呼啸强度(按弹体大小,子类覆盖;本体级的东西给 1)</summary>
        protected virtual float WhooshStrength => 0.35f;

        /// <summary>Z 越过近端淡出线或远端雾满线之后弹幕已不可见,子类在 DepthAI 里用它决定自杀</summary>
        protected bool OutOfSight => Z < VDDirector.DepthNearFadeZ - 0.05f || Z > VDDirector.DepthFogFullZ + 1.5f;
        #endregion

        #region 判定
        /// <summary>判定门:只在判定带内交给平面判定</summary>
        public sealed override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (HasDepth && !VDDepth.InHitBand(Z, ZVel)) {
                return false;
            }
            return CollidingOnPlane(projHitbox, targetHitbox);
        }

        /// <summary>平面判定(默认原版盒碰)</summary>
        protected virtual bool? CollidingOnPlane(Rectangle projHitbox, Rectangle targetHitbox) => null;
        #endregion

        #region 分层与绘制
        public sealed override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            Layer = HasDepth ? VDDepth.LayerOf(Z) : VDDepthLayer.Plane;
            if (Layer == VDDepthLayer.Far && VDDepth.FarLayerUsable(ProjectedCenter)) {
                Projectile.hide = true;
                VDDepthStage.RegisterFar(this);
            }
            else if (Layer == VDDepthLayer.Near) {
                Projectile.hide = true;
                overPlayers.Add(index);
            }
            else {
                Layer = VDDepthLayer.Plane;
                Projectile.hide = false;
            }
            if (WantsMarker) {
                VDDepthStage.RegisterMarker(this);
            }
        }

        public sealed override bool PreDraw(ref Color lightColor) {
            DrawDepth(Main.spriteBatch);
            return false;
        }

        void IVDDepthDrawable.DrawDepthFar(SpriteBatch spriteBatch) => DrawDepth(spriteBatch);

        void IVDDepthDrawable.DrawDepthMarker(SpriteBatch spriteBatch) => DrawMarker(spriteBatch);

        /// <summary>子类绘制:用 <see cref="ProjectedCenter"/> / <see cref="DrawScale"/> / <see cref="Z"/>,批次进出保持 Deferred/AlphaBlend</summary>
        protected abstract void DrawDepth(SpriteBatch spriteBatch);

        /// <summary>落点标记(默认收缩环);子类可换成别的形状</summary>
        protected virtual void DrawMarker(SpriteBatch spriteBatch) {
            float p = MarkerProgress();
            if (p <= 0f) {
                return;
            }
            VDVfx.DrawDepthMarker(PredictedLanding(), p, MarkerRadius * Projectile.scale, MarkerColor, 0.9f);
        }

        /// <summary>按各自历史 Z 投影的残影(加法批次内调用)</summary>
        protected void DrawDepthTrail(Texture2D tex, Vector2 origin, Color color, float baseAlpha, float scaleFalloff = 0.04f) {
            for (int i = 1; i < trailCount; i++) {
                float a = (1f - i / (float)TrailLength) * baseAlpha * VDDepth.Alpha(trailZ[i]);
                if (a <= 0.01f) {
                    continue;
                }
                Vector2 pos = VDDepth.Project(trailPos[i], trailZ[i]) - Main.screenPosition;
                float s = Projectile.scale * VDDepth.Scale(trailZ[i]) * (1f - i * scaleFalloff);
                Main.spriteBatch.Draw(tex, pos, null, VDDepth.Fog(color, trailZ[i]) * a, Projectile.rotation, origin, s, SpriteEffects.None, 0f);
            }
        }
        #endregion
    }
}
