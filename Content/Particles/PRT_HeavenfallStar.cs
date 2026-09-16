using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //三变体都是常规PRT Additive桶(HowlingCannon/AbyssalStar等),不走EffectLoader RT
    public class PRT_HeavenfallStar : BasePRT
    {
        public bool Glow = true;
        public Color InitialColor;
        public float xScale = 1;   //PreDraw竖向拉伸系数,和Scale乘在一起

        public override bool CanPool => true;

        //CanPool复用,InitialColor/xScale/drawScale/orgScale/Inverse新加字段都得来Reset登记
        public override void Reset() {
            base.Reset();
            Glow = true;
            InitialColor = default;
            xScale = 1f;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/StarTexture_White";

        public PRT_HeavenfallStar Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            InitialColor = Color;   //spawn色快照,AI里Lerp淡出去,2/3同理
            if (Lifetime <= 0)
                Lifetime = 200;
        }

        public override void AI() {
            Scale *= 0.92f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));   //三次方=前段亮得久,旧fade曲线
            Velocity *= 0.92f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTExtraTextures.StarTexture_White.Value;
            //双层Draw叠辉光,第二层scale*0.45,旧Draw一行没动
            Vector2 scaled = new Vector2(0.2f, 1.6f * xScale) * Scale;
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() * 0.5f, scaled, 0, 0f);
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() * 0.5f, scaled * new Vector2(0.45f, 1f), 0, 0f);
            return false;
        }
    }

    //变体2:Gungnir/EGlobalProjectile,Inverse=true时Scale从0胀到orgScale,颜色走1-Completion
    public class PRT_HeavenfallStar2 : BasePRT
    {
        public bool Glow = true;
        public Color InitialColor;
        public Vector2 drawScale = Vector2.One;
        public bool Inverse = false;
        public float InitialScale = 1;
        private float orgScale = -1;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            InitialColor = default;
            drawScale = Vector2.One;
            Inverse = false;
            InitialScale = 1f;
            orgScale = -1f;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/StarTexture_White";

        public PRT_HeavenfallStar2 Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            InitialColor = Color;
            InitialScale = Scale;
            orgScale = -1f;
            if (Lifetime <= 0)
                Lifetime = 26;
        }

        public override void AI() {
            //orgScale第一帧AI才抓,Inverse分支Scale算法另算,Reset里清回-1
            if (orgScale == -1) {
                orgScale = Scale;
                Scale = 0;
            }
            if (Inverse) {
                Scale += (Lifetime - Time) * orgScale * 0.002f;
                Color = InitialColor * (1f - LifetimeCompletion);
            }
            else {
                Scale = InitialScale * (1f - LifetimeCompletion);
                Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            }
            Velocity *= 0.92f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTExtraTextures.StarTexture_White.Value;
            Vector2 scaled = drawScale * Scale;
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() * 0.5f, scaled, 0, 0f);
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() * 0.5f, scaled * 1.2f, 0, 0f);
            return false;
        }
    }

    //变体3:drawScale非均匀+和1同款的Pow(LifetimeCompletion,3)淡出,没Inverse/orgScale那套
    public class PRT_HeavenfallStar3 : BasePRT
    {
        public bool Glow = true;
        public Color InitialColor;
        public Vector2 drawScale = Vector2.One;
        public bool Inverse = false;   //字段留着对齐2的spawn签名,这版AI不读
        public float InitialScale = 1;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            InitialColor = default;
            drawScale = Vector2.One;
            Inverse = false;
            InitialScale = 1f;
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/StarTexture_White";

        public PRT_HeavenfallStar3 Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            InitialColor = Color;
            InitialScale = Scale;
            if (Lifetime <= 0)
                Lifetime = 200;
        }

        public override void AI() {
            Scale *= 0.92f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.92f;
        }

        public override bool PreDraw(SpriteBatch sb) {
            Texture2D tex = PRTExtraTextures.StarTexture_White.Value;
            Vector2 scaled = drawScale * Scale;
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() * 0.5f, scaled, 0, 0f);
            sb.Draw(tex, Position - Main.screenPosition, null, Color, Rotation + MathHelper.PiOver2, tex.Size() * 0.5f, scaled * 1.2f, 0, 0f);
            return false;
        }
    }




}
