using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_HomingSpiritParticle : BasePRT
    {
        public bool Glow = true;
        public Vector2 TargetPos = Vector2.Zero;
        public float Opc = 0;
        public float h = 0;
        //OldPos拖尾+子步进,池化Reset忘Clear轨迹会闪,干脆不开CanPool
        //TargetPos是调用点后赋的,池化不带重置的话下一条会朝上一个目标飞,离谱
        public List<Vector2> OldPos = new List<Vector2>();

        public override string Texture => "CalamityEntropy/Assets/Extra/Glow2";

        public PRT_HomingSpiritParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 5;
        }

        //子步进在AI里自己Position+=Velocity,关框架自动位移
        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            //旧UpdateTimes=6,全库就这类每tick跑6遍子步
            for (int step = 0; step < 6; step++) {
                if (Opc < 1)
                    Opc += 0.05f;
                h = 0.6f + Utils.Remap(CEUtils.getDistance(Position, TargetPos), 0, 900, 1, 0);
                Velocity *= 1f - h * 0.012f;
                Velocity += (TargetPos - Position).normalize() * 0.056f * h;
                Position += Velocity;
                if (CEUtils.getDistance(Position, TargetPos) < Velocity.Length() * 1.1f + 64) {
                    Velocity *= 0;
                    Kill();
                    return;
                }
                OldPos.Add(Position);
                if (OldPos.Count > 80)
                    OldPos.RemoveAt(0);
            }
            //每tick末尾Lifetime=Time+5是旧逻辑:没撞到就续命,不是笔误
            //6步子步里自己Position+=Velocity,框架ShouldUpdatePosition关了,开回去双倍位移
            Lifetime = Time + 5;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //就一趟Additive拖尾,没有shader也没有TriangleStrip,别跟ShadeDash那套混
            //绘制顺序OldPos[0]→末尾,i越大s越大=越靠近弹头越亮,反了像尾巴点火
            //拖尾固定Additive+TransformationMatrix,End掉PRT批次画完BeginDrawingWithMode还
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = PRTExtraTextures.Glow2.Value;
            for (int i = 0; i < OldPos.Count; i++) {
                float s = (1f + i) / OldPos.Count;
                sb.Draw(tex, OldPos[i] - Main.screenPosition, null, Color * Opc * s, 0, tex.Size() * 0.5f, s * 0.16f, SpriteEffects.None, 0);
            }
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
            return false;
        }
    }


}
