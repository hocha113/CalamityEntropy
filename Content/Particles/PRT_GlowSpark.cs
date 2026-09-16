using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的GlowSpark,LightSoul一类trail用,grav=false时只淡出不下坠
    public class PRT_GlowSpark : BasePRT
    {
        public bool Glow = true;
        public bool grav = true;

        public override bool CanPool => true;

        public override void Reset() {
            //CanPool,grav默认true,复用忘清会继承上一条的无重力状态
            base.Reset();
            Glow = true;
            grav = true;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/GlowSpark";

        public PRT_GlowSpark Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 26;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //旧alpha走remaining比例,等价写法
            if (grav) {
                Velocity += Vector2.UnitY * 0.2f;
                Rotation = Velocity.ToRotation();
            }
        }

        public override bool PreDraw(SpriteBatch sb) {
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied只乘A,blend走Configure尾参mode
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }

    //GlowSpark的简化版,永远重力+旋转,没grav开关
    public class PRT_GlowSpark2 : BasePRT
    {
        public bool Glow = true;

        public override bool CanPool => true;

        //GlowSpark兄弟,CanPool开着,Reset只登记Glow
        public override void Reset() {
            base.Reset();
            Glow = true;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/GlowSpark2";

        public PRT_GlowSpark2 Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 26;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;   //1→0淡出,跟Directing那粒反着
            Velocity += Vector2.UnitY * 0.2f;
            Rotation = Velocity.ToRotation();
        }

        public override bool PreDraw(SpriteBatch sb) {
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied只乘A,blend走Configure尾参mode
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }

    //蓄力条导向火花,Solar StormHeld每帧Lerp跟枪,跟上面GlowSpark fade方向反着来
    public class PRT_GlowSparkDirecting : BasePRT
    {
        public bool Glow = true;
        public Vector2 TargetPos;
        public float scaleX = 1f;
        public Vector2 SpawnPos;
        public Entity followOwner;
        public Vector2 ownerLastPos = Vector2.Zero;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            Glow = true;
            TargetPos = default;
            scaleX = 1f;
            SpawnPos = default;
            followOwner = null;   //池化复用,entity引用必须清
            ownerLastPos = Vector2.Zero;
        }

        public override string Texture => "CalamityEntropy/Content/Particles/GlowSpark";

        public PRT_GlowSparkDirecting Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            SpawnPos = Position;
            ownerLastPos = Vector2.Zero;
        }

        public override bool ShouldUpdatePosition() => false;   //Position由AI里Lerp算,框架别自动+=Velocity

        public override void AI() {
            if (followOwner != null) {
                if (ownerLastPos == Vector2.Zero)
                    ownerLastPos = followOwner.Center;
                else {
                    TargetPos += followOwner.Center - ownerLastPos;
                    SpawnPos += followOwner.Center - ownerLastPos;
                }
                ownerLastPos = followOwner.Center;
            }
            Opacity = LifetimeCompletion;   //fade方向跟GlowSpark兄弟反着,0→1,Solar Storm蓄力条就这手感
            Position = Vector2.Lerp(TargetPos, SpawnPos, 1f - LifetimeCompletion);
            Rotation = (TargetPos - SpawnPos).ToRotation();
        }

        public override bool PreDraw(SpriteBatch sb) {
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation,
                tex.Size() / 2f, Scale * new Vector2(scaleX, 1), SpriteEffects.None, 0);
            CEUtils.DrawGlow(Position, Color * 0.8f, Scale * 0.4f);
            //DrawGlow(setState:true)内部End完批次停在Deferred+AlphaBlend,跟当前PRT桶对不上
            //Solar Storm蓄力大量spawn这粒,缺下面两行同桶后面全花屏
            sb.End();
            PRTLoader.BeginDrawingWithMode(PRTDrawMode, sb);
            return false;
        }
    }




}
