using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的Shine,Solar Storm/Silence冲击闪一下靠这粒,flag=true走另一套淡出
    public class PRT_ShineParticle : BasePRT
    {
        public bool Glow = true;
        public Entity FollowOwner;
        public Vector2 ownerLastPos = Vector2.Zero;
        public bool flag = false;
        public float orgScale = -1;
        public Vector2 drawScale = Vector2.One;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            FollowOwner = null;   //CanPool,FollowOwner得Reset里清
            ownerLastPos = Vector2.Zero;
            flag = false;
            orgScale = -1;
            drawScale = Vector2.One;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/Glow2";

        public PRT_ShineParticle Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 200;
            ownerLastPos = Vector2.Zero;
            orgScale = -1;   //随机/orgScale放SetProperty,池化复用不会重跑字段初始化器
        }

        public override void AI() {
            if (FollowOwner != null) {
                if (ownerLastPos != Vector2.Zero)
                    Position += FollowOwner.Center - ownerLastPos;
                ownerLastPos = FollowOwner.Center;
            }

            float remaining = 1f - LifetimeCompletion;
            Opacity = (float)(Math.Cos(remaining * MathHelper.Pi - MathHelper.PiOver2));   //余弦脉冲,旧alpha曲线原样搬
            if (flag) {
                //flag分支:前段缩放回弹+尾段喷dust,跟默认Cos淡出是两套手感
                float remTicks = Lifetime - Time;
                if (remTicks > 20)
                    Opacity = 1 - (remTicks - 20f) / (Lifetime - 20f);
                else
                    Opacity = remTicks / 20f;
                if (orgScale < 0)
                    orgScale = Scale;
                Scale = orgScale * Opacity;
                Opacity = 1;
                if (Time > 32) {
                    for (int i = 0; i < (Time - 32) / 16; i++)
                        Main.dust[Dust.NewDust(Position, 0, 0, DustID.MagicMirror)].velocity = CEUtils.randomPointInCircle(Scale * 3);
                }
            }
        }

        public override bool PreDraw(SpriteBatch sb) {
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTExtraTextures.Glow2.Value;   //真图走VaultLoaden,Texture属性只是给框架验路径
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale * drawScale, SpriteEffects.None, 0);
            return false;
        }
    }


}
