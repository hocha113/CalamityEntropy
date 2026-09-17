using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityEntropy.Content.Particles
{
    //IPixelPassPRT,门控同ElecParticle:CEScreenPipeline.PixelPassActive+PixelPass缺任一就不画(PreDraw/DrawPixelPass都跳过)
    public class PRT_PrismShard : BasePRT, IPixelPassPRT
    {
        public bool Glow = true;
        public bool PixelPass { get; set; }

        public override string Texture => "CalamityEntropy/Content/Particles/PrismShard";

        public PRT_PrismShard Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 38;
        }

        public override void AI() {
            //碎裂/音效钉在倒数第二tick(Time==Lifetime-1),对齐旧系统到期前一刻爆发,别挪到Time==0(SetProperty太早)
            if (Time == Lifetime - 1) {
                for (int i = 0; i < 4; i++) {
                    var p = PRTLoader.NewParticle<PRT_PrismShardSmall>(Position, CEUtils.randomPointInCircle(12), Color.White, 1f);
                    p.PixelPass = PixelPass;   //父走像素RT子也必须同通道,不然Screen2合成和常规桶分层
                    p.Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), 120);
                }
                for (int i = 0; i < 20; i++)
                    Dust.NewDustDirect(Position, 0, 0, DustID.MagicMirror).velocity = CEUtils.randomPointInCircle(10);
                SoundEngine.PlaySound(in SoundID.Item27, Position);
            }
        }

        public override bool PreDraw(SpriteBatch sb) {
            if (PixelPass)
                return false;
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);   //第三分支只乘A,DrawPixelPass也走这套路(但跳过Lighting)
            else
                clr *= Opacity;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }

        //DrawPixelPass故意不吃Glow/Lighting,进Screen2 RT前就要全亮,和旧DrawPixelShaderParticles一致
        public void DrawPixelPass(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, Color * Opacity, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }

    //碎片子粒子,PixelPass从父shard拷贝,CEPixelScreen三桶分流看PRTDrawMode不是看类名
    public class PRT_PrismShardSmall : BasePRT, IPixelPassPRT
    {
        public bool Glow = true;
        public bool PixelPass { get; set; }

        public override string Texture => "CalamityEntropy/Content/Particles/PrismShardSmall";

        public PRT_PrismShardSmall Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 120;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;
            Rotation += Velocity.X * 0.025f;
            Velocity += new Vector2(0, 0.36f);
        }

        public override bool PreDraw(SpriteBatch sb) {
            if (PixelPass)
                return false;
            Color clr = Color;
            if (!Glow)
                clr = Lighting.GetColor((int)(Position.X / 16), (int)(Position.Y / 16), clr);
            //NonPremultiplied只乘A,和父shard PreDraw同套路(DrawPixelPass走Color*Opacity不经这分支)
            if (PRTDrawMode == PRTDrawModeEnum.NonPremultiplied)
                clr.A = (byte)(clr.A * Opacity);
            else
                clr *= Opacity;
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }

        public void DrawPixelPass(SpriteBatch sb) {
            Texture2D tex = PRTLoader.PRT_IDToTexture[ID];
            sb.Draw(tex, Position - Main.screenPosition, null, Color * Opacity, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }



}
