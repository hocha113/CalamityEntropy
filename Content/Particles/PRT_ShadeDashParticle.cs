using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    public class PRT_ShadeDashParticle : BasePRT
    {
        public bool Glow = true;
        //odpl/odpr是左右轨,不开CanPool:池化复用忘Clear上一条残影会闪出来
        //还夹着TL=160上限和每帧Insert(0),Reset漏一项下一条就带脏轨迹出生,查起来像闹鬼
        public List<Vector2> odpl = new List<Vector2>();
        public List<Vector2> odpr = new List<Vector2>();
        public float c = Main.rand.NextFloat() * MathHelper.TwoPi;
        public Color c1 = new Color(40, 40, 40, 255);
        public Color c2 = new Color(0, 0, 0, 255);
        public int TL = 160;
        public int dir = Main.rand.NextBool() ? 1 : -1;

        public override string Texture => "CalamityEntropy/Content/Particles/ShadeDashParticle";

        public PRT_ShadeDashParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1)
        {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty()
        {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 14;
        }

        //AI里自己Position+=Velocity,框架再补一次就双倍位移
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            //PRTLoader先Time++再进AI,首帧Time已是1,Time==0永远不成立;
            //用odpl为空判首帧。漏了这步Rotation停在Configure的0,速度被拍成(len,0),不论冲刺朝向全往右飞
            bool firstTick = odpl.Count == 0;
            if (firstTick)
            {
                Rotation = Velocity.ToRotation();
                Lifetime += 8;
            }
            int ac = firstTick ? 8 : 1;
            for (int i = 0; i < ac; i++)
            {
                Position += Velocity;
                Opacity = 1f - LifetimeCompletion;
                Velocity = Rotation.ToRotationVector2() * Velocity.Length();
                Rotation += (float)Math.Sin(c) * 0.065f;
                Velocity *= 0.98f;
                c += 0.46f;
                odpl.Insert(0, Position + Velocity.RotatedBy(MathHelper.PiOver2).normalize() * Scale * 13);
                odpr.Insert(0, Position - Velocity.RotatedBy(MathHelper.PiOver2).normalize() * Scale * 13);
                if (odpl.Count > TL)
                {
                    odpl.RemoveAt(odpl.Count - 1);
                    odpr.RemoveAt(odpr.Count - 1);
                }
            }
        }

        public override bool PreDraw(SpriteBatch sb)
        {
            Texture2D trail = PRTSharedAssets.ShadeDashParticle.Value;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            //TriangleStrip:左右轨成对塞顶点,UV的y=1/0区分内外边,x沿弧长0→1给shader采样
            //primitive数=ve.Count-2,不是ve.Count,抄XNA文档抄错了一度画不出带子
            for (int i = 0; i < odpr.Count; i++)
            {
                Color b = new Color(220, 200, 255);
                ve.Add(new ColoredVertex(odpl[i] - Main.screenPosition, new Vector3(i / ((float)odpl.Count - 1), 1, 1), b));
                ve.Add(new ColoredVertex(odpr[i] - Main.screenPosition, new Vector3(i / ((float)odpr.Count - 1), 0, 1), b));
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                Effect shader = PRTSharedAssets.ShadeDashParticleShader.Value;
                //TriangleStrip+ShadeDashParticle.fx走Immediate,进来时PRT批次是开着的得先End
                sb.End();
                //ShadeDashParticle.fx:color1→color2按贴图R通道lerp,alpha再乘顶点A和贴图A
                //uniform alpha跟AI里Opacity是同一套1-LifetimeCompletion,别一个改了一个没改
                //参数在Begin前设好,shader直接进Begin(Immediate下flush即应用),再Apply一次确保图元生效
                shader.Parameters["color1"].SetValue(c1.ToVector4());
                shader.Parameters["color2"].SetValue(c2.ToVector4());
                shader.Parameters["alpha"].SetValue(1f - LifetimeCompletion);
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                //就这一遍shader+图元,没有PostDraw第二刀;旧EParticle也是单Draw,别照着SlashDarkRed硬拆两趟
                sb.End();
                //还PRT桶,删BeginDrawingWithMode同桶后面粒子全花
                PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
            }
            return false;
        }
    }


}
