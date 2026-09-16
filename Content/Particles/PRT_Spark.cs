using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityEntropy.Content.Particles
{
    //EParticle迁来的Spark,跟SparkCal同AI但贴图是本模组自有、Configure签名也不一样
    internal class PRT_Spark : BasePRT
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public Entity entity;
        public override int InGame_World_MaxCount => 4000;

        public override bool CanPool => true;

        public override void Reset() {
            base.Reset();
            InitialColor = default;
            AffectedByGravity = false;
            entity = null;   //CanPool开着,跟着owner的引用得清
        }

        public PRT_Spark Configure(bool affectedByGravity, int lifetime, Entity entity = null) {
            AffectedByGravity = affectedByGravity;
            InitialColor = Color;
            this.entity = entity;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            //这粒Configure不收mode,blend锁Additive,跟CalamityPorts的SparkCal一套
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 200;
        }

        public override void AI() {
            Scale *= 0.95f;
            Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));   //0→1渐隐,立方让尾段更快透明
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity) {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;

            if (entity != null) {
                if (entity.active) {
                    Position += entity.velocity;   //跟owner走,框架自动位移不管entity
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Vector2 scale = new Vector2(0.5f, 1.6f) * Scale;
            Texture2D texture = PRTLoader.PRT_IDToTexture[ID];

            //拉伸火花叠两层,0.45内层是Calamity原版Draw写法
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation
                , texture.Size() * 0.5f, scale, 0, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation
                , texture.Size() * 0.5f, scale * new Vector2(0.45f, 1f), 0, 0f);

            return false;
        }
    }
}
