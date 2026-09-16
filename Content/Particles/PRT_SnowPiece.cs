using CalamityEntropy.Content.Items.Books.BookMarks;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Particles
{
    //BookmarkSnowgrave召唤的雪花片,Snowgrave射弹没了owner.ownedProjectileCounts<1时Opacity衰减
    public class PRT_SnowPiece : BasePRT
    {
        public bool Glow = true;
        public Player owner = null;   //挂Player引用,别开CanPool
        public static int ProjType = -1;   //懒缓存Snowgrave.Type,AI里别每帧GetModProjectileType

        public override string Texture => "CalamityEntropy/Content/Particles/SnowPiece";

        public PRT_SnowPiece Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
            if (ProjType == -1)
                ProjType = ModContent.ProjectileType<Snowgrave>();
            if (owner != null && owner.ownedProjectileCounts[ProjType] < 1)
                Opacity *= 0.7f;
            if (Lifetime - Time < 8)
                Color = Color.White;
            else
                Color = Color.Lerp(new Color(110, 110, 255), Color.White, 1 - ((Lifetime - Time - 8) / 18f));
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
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }

    public class PRT_SnowStorm : BasePRT
    {
        public bool Glow = true;
        public Player owner = null;   //同SnowPiece,跟Snowgrave射弹寿命绑着
        public static int ProjType = -1;

        //大尺度白雾块,Texture随便指白图,PreDraw走PRT_IDToTexture[ID]就行
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_SnowStorm Configure(float opacity, bool glow, PRTDrawModeEnum mode,
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
                Lifetime = 26 * 4;
        }

        public override void AI() {
            if (ProjType == -1)
                ProjType = ModContent.ProjectileType<Snowgrave>();
            if (owner != null && owner.ownedProjectileCounts[ProjType] < 1)
                Opacity *= 0.9f;
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
            sb.Draw(tex, Position - Main.screenPosition, null, clr, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }



}
