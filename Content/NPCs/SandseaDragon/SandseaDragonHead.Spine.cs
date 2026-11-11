using CalamityEntropy.Content.NPCs.SandseaDragon.Core;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.SandseaDragon
{
    /// <summary>
    /// 脊柱:身体沿头部走过的轨迹排开,节长固定,再叠一层向尾部传的摆尾波和弓身
    /// 输入只有头部位置与速度,各端本地重建,不过线;摆尾相位各端可以不同
    /// 悬停时轨迹随 FrameVelocity 整体平移,身体保持到位时的形状
    /// </summary>
    public partial class SandseaDragonHead
    {
        /// <summary>头部轨迹,[0] 最新</summary>
        private readonly Vector2[] trail = new Vector2[SandseaDirector.TrailCapacity];
        private int trailCount;
        private bool spineReady;
        private float wavePhase;
        private readonly Vector2[] jointOffsets = new Vector2[SandseaDirector.SegmentCount + 1];
        private static float[] boneLengths;
        private static float totalLength;

        /// <summary>关节世界坐标:[0] 头部中心,[k] 第 k-1 节的后关节,最后一项是尾尖</summary>
        public readonly Vector2[] Joints = new Vector2[SandseaDirector.SegmentCount + 1];
        /// <summary>各节的前进方向角,[0] 是头</summary>
        public readonly float[] SegmentAngles = new float[SandseaDirector.SegmentCount];
        /// <summary>+1 头朝右,-1 头朝左,贴图默认朝左</summary>
        public int FacingDir { get; private set; } = -1;

        private static void EnsureBones() {
            if (boneLengths != null) {
                return;
            }
            float[] lengths = new float[SandseaDirector.SegmentCount];
            float total = 0f;
            for (int k = 0; k < lengths.Length; k++) {
                lengths[k] = Vector2.Distance(SandseaDirector.JointFront[k], SandseaDirector.JointBack[k]);
                total += lengths[k];
            }
            totalLength = total;
            boneLengths = lengths;
        }

        /// <summary>把轨迹铺成从 head 沿 behind 伸出的直线,出生或位置跳变后调</summary>
        public void ResetSpine(Vector2 head, Vector2 behind) {
            behind = behind.SafeNormalize(Vector2.UnitY);
            trailCount = SandseaDirector.TrailCapacity;
            for (int i = 0; i < trailCount; i++) {
                trail[i] = head + behind * (SandseaDirector.TrailSpacing * (i + 1));
            }
            spineReady = true;
        }

        /// <summary>各端每帧都跑,用加过本帧速度的头部位置,与随后画出来的位置一致</summary>
        private void UpdateSpine() {
            EnsureBones();
            Vector2 head = NPC.Center + NPC.velocity;
            if (!spineReady) {
                ResetSpine(head, -NPC.rotation.ToRotationVector2());
            }

            Vector2 shift = Context.FrameVelocity;
            if (shift != Vector2.Zero) {
                for (int i = 0; i < trailCount; i++) {
                    trail[i] += shift;
                }
            }
            //客户端硬对齐等位置跳变
            if (Vector2.DistanceSquared(head, trail[0]) > 420f * 420f) {
                ResetSpine(head, -NPC.rotation.ToRotationVector2());
            }

            for (int guard = 0; guard < 64; guard++) {
                Vector2 diff = head - trail[0];
                float dist = diff.Length();
                if (dist < SandseaDirector.TrailSpacing) {
                    break;
                }
                Vector2 point = trail[0] + diff / dist * SandseaDirector.TrailSpacing;
                Array.Copy(trail, 0, trail, 1, Math.Min(trailCount, SandseaDirector.TrailCapacity - 1));
                trail[0] = point;
                if (trailCount < SandseaDirector.TrailCapacity) {
                    trailCount++;
                }
            }

            SolveJoints(head);
            ApplyWaveAndCurl();
            for (int k = 0; k < SandseaDirector.SegmentCount; k++) {
                Vector2 forward = Joints[k] - Joints[k + 1];
                if (forward.LengthSquared() > 0.01f) {
                    SegmentAngles[k] = forward.ToRotation();
                }
                else if (k > 0) {
                    SegmentAngles[k] = SegmentAngles[k - 1];
                }
            }

            NPC.rotation = SegmentAngles[0];
            float facing = MathF.Cos(NPC.rotation);
            if (facing > SandseaDirector.FlipDeadZone) {
                FacingDir = 1;
            }
            else if (facing < -SandseaDirector.FlipDeadZone) {
                FacingDir = -1;
            }
        }

        private Vector2 TrailPoint(int index) {
            if (index < trailCount) {
                return trail[index];
            }
            Vector2 last = trail[trailCount - 1];
            Vector2 dir = trailCount >= 2 ? (last - trail[trailCount - 2]).SafeNormalize(Vector2.UnitY) : Vector2.UnitY;
            return last + dir * (SandseaDirector.TrailSpacing * (index - trailCount + 1));
        }

        /// <summary>沿折线往后走,每节取离前一关节直线距离正好等于节长的点</summary>
        private void SolveJoints(Vector2 head) {
            Joints[0] = head;
            Vector2 previous = head;
            Vector2 a = head;
            int index = 0;
            for (int k = 0; k < SandseaDirector.SegmentCount; k++) {
                float length = boneLengths[k];
                Vector2 joint = previous;
                for (int guard = 0; guard < 256; guard++) {
                    Vector2 b = TrailPoint(index);
                    if (Vector2.DistanceSquared(b, previous) >= length * length) {
                        joint = CircleCross(a, b, previous, length);
                        a = joint;
                        break;
                    }
                    a = b;
                    index++;
                }
                Joints[k + 1] = joint;
                previous = joint;
            }
        }

        /// <summary>线段 a→b 上离 center 距离为 radius 的点,a 在圆内 b 在圆外</summary>
        private static Vector2 CircleCross(Vector2 a, Vector2 b, Vector2 center, float radius) {
            Vector2 d = b - a;
            float dd = d.LengthSquared();
            if (dd < 0.0001f) {
                return b;
            }
            Vector2 f = a - center;
            float fd = Vector2.Dot(f, d);
            float disc = fd * fd - dd * (f.LengthSquared() - radius * radius);
            if (disc < 0f) {
                return b;
            }
            float t = MathHelper.Clamp((-fd + MathF.Sqrt(disc)) / dd, 0f, 1f);
            return a + d * t;
        }

        /// <summary>摆尾波从头向尾传,幅度沿身体增长,头部不动;弓身朝背侧弯</summary>
        private void ApplyWaveAndCurl() {
            wavePhase += SandseaDirector.WaveSpeedBase + NPC.velocity.Length() * SandseaDirector.WaveSpeedPerVelocity;
            if (wavePhase > MathHelper.TwoPi) {
                wavePhase -= MathHelper.TwoPi;
            }
            float s = 0f;
            for (int k = 1; k <= SandseaDirector.SegmentCount; k++) {
                s += boneLengths[k - 1];
                float u = s / totalLength;
                Vector2 tangent = (Joints[k - 1] - Joints[k]).SafeNormalize(Vector2.UnitX);
                Vector2 normal = new Vector2(-tangent.Y, tangent.X);
                Vector2 dorsal = FacingDir < 0 ? normal : -normal;
                float wave = SandseaDirector.WaveAmpTail * MathF.Pow(u, 1.3f) * MathF.Sin(wavePhase - MathHelper.TwoPi * s / SandseaDirector.WaveLength) * Context.Effort;
                jointOffsets[k] = normal * wave + dorsal * (SandseaDirector.CurlTailOffset * u * u * Context.Curl);
            }
            for (int k = 1; k <= SandseaDirector.SegmentCount; k++) {
                Joints[k] += jointOffsets[k];
            }
        }

        /// <summary>体节实体读自己那节的判定中心与朝向,index 1..4</summary>
        public bool TryGetSegment(int index, out Vector2 center, out float rotation) {
            if (!spineReady || index < 1 || index >= SandseaDirector.SegmentCount) {
                center = NPC.Center;
                rotation = NPC.rotation;
                return false;
            }
            //尾节后半是薄鳍,判定中心靠前
            float along = index == SandseaDirector.SegmentCount - 1 ? 0.3f : 0.5f;
            center = Vector2.Lerp(Joints[index], Joints[index + 1], along);
            rotation = SegmentAngles[index];
            return true;
        }

        /// <summary>第 seg 节贴图的绘制旋转,贴图里后关节指向前关节的轴与水平有小夹角,一并扣掉</summary>
        public float SegmentDrawRotation(int seg, out bool flip) {
            flip = FacingDir > 0;
            Vector2 axis = SandseaDirector.JointFront[seg] - SandseaDirector.JointBack[seg];
            if (flip) {
                axis.X = -axis.X;
            }
            return SegmentAngles[seg] - axis.ToRotation();
        }

        /// <summary>第 seg 节贴图上的像素点换成世界坐标</summary>
        public Vector2 SegmentPoint(int seg, Vector2 spritePoint) {
            Vector2 local = spritePoint - SandseaDirector.JointFront[seg];
            float rotation = SegmentDrawRotation(seg, out bool flip);
            if (flip) {
                local.X = -local.X;
            }
            return Joints[seg] + local.RotatedBy(rotation);
        }

        public Vector2 EyeWorld => SegmentPoint(0, SandseaDirector.EyeInSprite);

        /// <summary>头部正前方 distance 处</summary>
        public Vector2 SnoutWorld(float distance = 90f) => Joints[0] + SegmentAngles[0].ToRotationVector2() * distance;
    }
}
