using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles.CalamityPorts
{
        //灾厄AltSpark的移植。原灾厄贴图走 AlphaBlend,自制 StarProj 是白底光晕,必须走 Additive
    public class PRT_AltSpark : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;

        public override bool CanPool => true;

        public override void Reset()
        {
            base.Reset();
            InitialColor = default;
            AffectedByGravity = false;   //CanPool复用,重力开关忘了清下一朵行为就变了
        }

        //StarProj自制贴图在Assets/Particles,映射PRTSharedAssets.StarProj
        public override string Texture => CEUtils.WhiteTexPath;

        public PRT_AltSpark Configure(bool affectedByGravity, int lifetime)
        {
            AffectedByGravity = affectedByGravity;
            InitialColor = Color;
            // 自制 StarProj 是白底+alpha 光晕,AlphaBlend 会画成发灰的大方块;Additive 才是溅射火花
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty()
        {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 30;
        }

        public override void AI()
        {
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }

            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch)
        {
            Vector2 drawScale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = PRTSharedAssets.StarProj.Value;   //自制StarProj,VaultLoaden映射

            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, drawScale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
