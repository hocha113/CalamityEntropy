using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class CruiserEnergyBall : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 300;
        }
        public float Scale = 0;
        public LoopSound snd = null;
        public NPC owner { get { return ((int)Projectile.ai[0]).ToNPC(); } set { Projectile.ai[0] = value.whoAmI; } }
        public override void AI() {
            if (!Main.dedServ) {
                if (snd == null) {
                    snd = new LoopSound(CalamityEntropy.ofCharge);
                    snd.instance.Pitch = 0;
                    snd.instance.Volume = 0;
                    snd.play();
                }
                if (Projectile.timeLeft > 120) {
                    snd.setVolume_Dist(Projectile.Center, 100, 2600, ((300 - Projectile.timeLeft) / 180f) * 0.5f);
                    snd.instance.Pitch = (300 - Projectile.timeLeft) / 180f;
                    snd.timeleft = 3;
                }
            }
            if (Projectile.timeLeft > 120) {
                Scale += 1 / 180f;
                Projectile.Center = owner.Center + owner.velocity.normalize() * 80;
            }
            if (Projectile.timeLeft == 120) {
                Scale = 1;
                Projectile.velocity = owner.velocity.normalize() * 30;
                CEUtils.PlaySound("CrystalBallActive", 1, Projectile.Center);
            }
            if (Projectile.timeLeft < 120) {
                if (Projectile.timeLeft > 16) {
                    Projectile.timeLeft -= 1;
                }
                Projectile.velocity *= 0.975f;
            }
            if (Projectile.timeLeft < 10) {
                Scale *= 1.1f;
                Projectile.Opacity -= 0.1f;
            }
            if (Projectile.timeLeft == 10) {
                CEUtils.PlaySound("energyImpact", 1, Projectile.Center);
                if (!(Main.netMode == NetmodeID.MultiplayerClient)) {
                    float rj = CEUtils.randomRot();
                    for (float i = 0; i < 360; i += 12f) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (rj + MathHelper.ToRadians(i)).ToRotationVector2() * 6, ModContent.ProjectileType<VoidSpike>(), Projectile.damage, Projectile.knockBack);
                    }
                    for (float i = 0; i < 360; i += 16f) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (rj + MathHelper.ToRadians(i)).ToRotationVector2() * 8, ModContent.ProjectileType<VoidSpike>(), Projectile.damage, Projectile.knockBack);
                    }
                    for (float i = 0; i < 360; i += 18f) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (rj + MathHelper.ToRadians(i)).ToRotationVector2() * 10, ModContent.ProjectileType<VoidSpike>(), Projectile.damage, Projectile.knockBack);
                    }
                }
            }
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            hitbox = Projectile.Center.getRectCentered(Scale * 60, Scale * 60);
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public List<Vector2> GP(float distAdd = 0, float c = 1) {
            float dist = distAdd;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++) {
                points.Add(new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 80 * c * Main.GlobalTimeWrappedHourly)));
            }
            return points;
        }
        public void Draw() {

            float a = Scale;
            if (a > 1) {
                a = 1;
            }
            a *= Projectile.Opacity;
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<Vector2> points = GP(0);
                List<Vector2> pointsOutside = GP(70 * Scale);
                int i;
                for (i = 0; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + points[i],
                    new Vector3((float)i / points.Count, 1, 1f),
                          Color.LightBlue * a));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + pointsOutside[i],
                          new Vector3((float)i / points.Count, 0, 1f),
                          Color.LightBlue * a));

                }
                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.AbyssalCircle3;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<Vector2> points = GP(0, -1);
                List<Vector2> pointsOutside = GP(60 * Scale, -1);
                int i;
                for (i = 0; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + points[i],
                          new Vector3((float)i / points.Count, 1, 1f),
                          Color.White * a));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + pointsOutside[i],
                          new Vector3((float)i / points.Count, 0, 1f),
                          Color.White * a));

                }
                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.AbyssalCircle3;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<Vector2> points = GP(0, 0.6f);
                List<Vector2> pointsOutside = GP(80 * Scale, -1);
                int i;
                for (i = 0; i < points.Count; i++) {
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + points[i],
                          new Vector3((float)i / points.Count, 1, 1f),
                          Color.LightBlue * a));
                    ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + pointsOutside[i],
                          new Vector3((float)i / points.Count, 0, 1f),
                          Color.LightBlue * a));

                }
                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.AbyssalCircle4;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }
    }

}