using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer.Core
{
    /// <summary>深度层位:远景层(墙后物块前)/ 平面(原层)/ 近景层(玩家之上)</summary>
    public enum VDDepthLayer : byte
    {
        Far,
        Plane,
        Near,
    }

    /// <summary>
    /// 驱逐舰伪 3D 的数学核心。坐标约定:Z = 0 是玩家平面(唯一有判定的层),Z &gt; 0 越远越深,Z &lt; 0 朝镜头。
    /// 缩放 <c>Scale = Focal / (Focal + Z)</c>;透视投影把世界点向相机中心收敛,远物随镜头移动更少(视差)。
    /// 服务端没有相机,摆位时用目标玩家中心做相机代理(<see cref="WorldFromApparent"/>),目标玩家本机看到的就是精确值。
    /// 全部数字在 <see cref="VDDirector"/> 的「纵深」分区
    /// </summary>
    public static class VDDepth
    {
        /// <summary>Z 对应的绘制缩放;近端钳在 <see cref="VDDirector.DepthNearClamp"/>,再近只会是一团糊</summary>
        public static float Scale(float z) {
            z = Math.Max(z, VDDirector.DepthNearClamp);
            return VDDirector.DepthFocal / (VDDirector.DepthFocal + z);
        }

        /// <summary>本地相机中心(世界坐标):原版缩放围绕它进行,所以投影后再经 GameViewMatrix 也自洽</summary>
        public static Vector2 CameraCenter => Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

        /// <summary>透视投影:世界点按 Z 向相机中心收敛,返回仍是世界坐标(减 screenPosition 即屏幕坐标)</summary>
        public static Vector2 Project(Vector2 world, float z) => Project(world, z, CameraCenter);

        public static Vector2 Project(Vector2 world, float z, Vector2 camera) => camera + (world - camera) * Scale(z);

        /// <summary>表观偏移 → 世界偏移:想让一个 Z 处的东西看起来离相机中心 apparent 远,世界上要放到 apparent / Scale 处</summary>
        public static Vector2 WorldOffset(Vector2 apparentOffset, float z) => apparentOffset / Scale(z);

        /// <summary>服务端摆位:以代理相机(目标玩家中心)为准,把「表观偏移 + Z」换成世界坐标</summary>
        public static Vector2 WorldFromApparent(Vector2 proxyCamera, Vector2 apparentOffset, float z) => proxyCamera + WorldOffset(apparentOffset, z);

        /// <summary>判定带半宽:按 Z 速度放宽,快弹穿越平面也留得住 3 帧碰撞</summary>
        public static float HitBand(float zVel) => Math.Max(VDDirector.DepthHitBandMin, VDDirector.DepthHitBandVelMult * Math.Abs(zVel));

        /// <summary>是否在判定带内(只有这里有碰撞)</summary>
        public static bool InHitBand(float z, float zVel = 0f) => Math.Abs(z) <= HitBand(zVel);

        /// <summary>按 Z 分层</summary>
        public static VDDepthLayer LayerOf(float z) {
            if (z >= VDDirector.DepthFarLayerZ) {
                return VDDepthLayer.Far;
            }
            if (z <= VDDirector.DepthNearLayerZ) {
                return VDDepthLayer.Near;
            }
            return VDDepthLayer.Plane;
        }

        /// <summary>远端雾化量 0..1:Z 从 0 到 DepthFogFullZ 线性;近端为 0</summary>
        public static float FogAmount(float z) => MathHelper.Clamp(z / VDDirector.DepthFogFullZ, 0f, 1f);

        /// <summary>远端雾色:向深空冷色插值并顺带去一点饱和</summary>
        public static Color Fog(Color color, float z) {
            float fog = FogAmount(z);
            if (fog <= 0f) {
                return color;
            }
            return Color.Lerp(color, VDVfx.FarFog, fog * 0.75f);
        }

        /// <summary>
        /// 深度透明度倍率:远端随缩放暗下去但留 55% 地板(深空里的东西是暗不是透);
        /// 近端从近景层门槛起线性淡到 <see cref="VDDirector.DepthNearFadeZ"/> 归零,且不超过剪影上限
        /// </summary>
        public static float Alpha(float z) {
            if (z >= 0f) {
                return MathHelper.Lerp(VDDirector.DepthFarAlphaFloor, 1f, Scale(z));
            }
            if (z >= VDDirector.DepthNearLayerZ) {
                //平面到近景门槛之间:从 1 平滑压到剪影上限,进近景层那一刻不跳变
                float t = z / VDDirector.DepthNearLayerZ;
                return MathHelper.Lerp(1f, VDDirector.DepthNearAlphaMax, t);
            }
            float fade = MathHelper.Clamp((z - VDDirector.DepthNearFadeZ) / (VDDirector.DepthNearLayerZ - VDDirector.DepthNearFadeZ), 0f, 1f);
            return VDDirector.DepthNearAlphaMax * fade;
        }

        /// <summary>多普勒音高:Z 从 DepthFogFullZ 到 0 由低到高</summary>
        public static float DopplerPitch(float z) {
            float t = 1f - MathHelper.Clamp(z / VDDirector.DepthFogFullZ, 0f, 1f);
            return MathHelper.Lerp(VDDirector.DepthDopplerLow, VDDirector.DepthDopplerHigh, t);
        }

        /// <summary>
        /// 远景层本帧对某个投影点是否可用:本地相机中心在地表线以下(地下战斗,远景层整片被物块盖住),
        /// 或投影点正落在实心物块里,都退回原层按同样的投影绘制,保证来袭物永远看得见
        /// </summary>
        public static bool FarLayerUsable(Vector2 projectedWorld) {
            if (Main.dedServ) {
                return false;
            }
            if (CameraCenter.Y > Main.worldSurface * 16f) {
                return false;
            }
            int tx = (int)(projectedWorld.X / 16f);
            int ty = (int)(projectedWorld.Y / 16f);
            if (!WorldGen.InWorld(tx, ty, 2)) {
                return true;
            }
            Tile tile = Framing.GetTileSafely(tx, ty);
            return !(tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]);
        }

        /// <summary>
        /// 抛物线的初速与加速度:<paramref name="frames"/> 帧正中到顶 <paramref name="apex"/>、走完回到 0
        /// (半隐式积分下落地早约一帧,以帧计时的东西别拿 Z == 0 当钟)。回旋弹与核弹都用它
        /// </summary>
        public static (float zVel, float zAccel) Parabola(float apex, int frames) {
            float half = frames * 0.5f;
            float accel = -2f * apex / (half * half);
            return (-accel * half, accel);
        }

        /// <summary>立方缓入:俯冲/入场的「朝镜头飞来」曲线(慢起、猛到)</summary>
        public static float DiveCurve(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * t;
        }

        /// <summary>立方缓出:退入深处的曲线(猛起、慢停)</summary>
        public static float RetreatCurve(float t) {
            t = MathHelper.Clamp(t, 0f, 1f);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
