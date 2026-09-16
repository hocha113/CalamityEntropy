using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class OblivionHoldout : ModProjectile
    {
        //拖尾贴图,加载期就位,绘制时不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Extra/wohslash3")]
        internal static Asset<Texture2D> WohSlash3Tex;
        public class Vpoint
        {
            public Vector2 pos = Vector2.Zero;
            public float rot = 0;
        }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Default;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.ArmorPenetration = 86;
        }

        public override bool? CanCutTiles() {
            return false;
        }

        List<Vpoint> v1 = new List<Vpoint>();
        List<Vpoint> v2 = new List<Vpoint>();

        public void updatePoints() {
            for (int i = 0; i < v1.Count; i++) {
                float urotu = Projectile.rotation;
                float urotd = Projectile.rotation;
                Vector2 uposu = Projectile.Center + new Vector2(-36, -80).RotatedBy(Projectile.rotation);
                Vector2 uposd = Projectile.Center + new Vector2(-36, 80).RotatedBy(Projectile.rotation);
                if (i > 0) {
                    urotu = v1[i - 1].rot;
                    urotd = v2[i - 1].rot;
                    uposu = v1[i - 1].pos;
                    uposd = v2[i - 1].pos;
                }
                v1[i].pos -= Projectile.rotation.ToRotationVector2() * 8;
                v2[i].pos -= Projectile.rotation.ToRotationVector2() * 8;
                v1[i].pos = uposu - (uposu - v1[i].pos).SafeNormalize(-Vector2.UnitX) * 12;
                v1[i].rot = (uposu - v1[i].pos).ToRotation();
                v2[i].pos = uposd - (uposd - v2[i].pos).SafeNormalize(-Vector2.UnitX) * 12;
                v2[i].rot = (uposd - v2[i].pos).ToRotation();

                v1[i].rot = CEUtils.RotateTowardsAngle(v1[i].rot, urotu, 0.5f, false);
                v1[i].pos = uposu - v1[i].rot.ToRotationVector2() * 12;
                v2[i].rot = CEUtils.RotateTowardsAngle(v2[i].rot, urotd, 0.5f, false);
                v2[i].pos = uposd - v2[i].rot.ToRotationVector2() * 12;

            }
        }
        bool sp = true;
        public override void AI() {
            if (sp) {
                sp = false;
                for (int i = 0; i < 16; i++) {
                    v1.Add(new Vpoint() { pos = Projectile.Center });
                    v2.Add(new Vpoint() { pos = Projectile.Center });
                }
            }
            updatePoints();
            Player player = Projectile.owner.ToPlayer();
            if (player.dead) {
                Projectile.Kill();
            }
            if (Projectile.Entropy().IndexOfTwistedTwinShootedThisProj != -1 && !(Projectile.Entropy().IndexOfTwistedTwinShootedThisProj.ToProj().active)) {
                Projectile.Kill();
            }
            Vector2 playerRotatedPoint = player.RotatedRelativePoint(player.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner) {
                HandleChannelMovement(player, playerRotatedPoint);
            }
            Projectile.Center = player.MountedCenter;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center += new Vector2(12, 6 * Projectile.direction).RotatedBy(Projectile.rotation) + player.gfxOffY * Vector2.UnitY;
            if (Projectile.velocity.X >= 0) {
                player.direction = 1;
                Projectile.direction = 1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            }
            else {
                player.direction = -1;
                Projectile.direction = -1;
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            }
            if (player.HeldItem.type == ModContent.ItemType<Oblivion>()) {
                Projectile.timeLeft = 3;
            }
            player.heldProj = Projectile.whoAmI;
        }
        public void HandleChannelMovement(Player player, Vector2 playerRotatedPoint) {
            float speed = 16f;
            Vector2 newVelocity = (Main.MouseWorld - playerRotatedPoint).SafeNormalize(Vector2.UnitX * player.direction) * speed;

            if (Projectile.velocity.X != newVelocity.X || Projectile.velocity.Y != newVelocity.Y) {
                Projectile.netUpdate = true;
            }
            Projectile.velocity = newVelocity;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(230, 200, 255);
            return result * completionRatio;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            if (completionRatio > 0.95f) {
                return 42 * Projectile.scale * MathHelper.SmoothStep(0, 1, (1 - (completionRatio - 0.95f) / 0.05f));
            }
            return MathHelper.Lerp(16, 42 * Projectile.scale, completionRatio);
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            List<Vector2> l1 = new List<Vector2>();
            List<Vector2> l2 = new List<Vector2>();
            Vector2 uposu = Projectile.Center + new Vector2(-36, -80).RotatedBy(Projectile.rotation);
            Vector2 uposd = Projectile.Center + new Vector2(-36, 80).RotatedBy(Projectile.rotation);

            l1.Add(uposu);
            l2.Add(uposd);
            for (int i = 0; i < v1.Count; i++) {
                l1.Add(v1[i].pos);
                l2.Add(v2[i].pos);
            }
            drawT(l1);
            drawT(l2);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, 0).RotatedBy(Projectile.rotation), null, Color.White, Projectile.rotation, texture.Size() / 2, Projectile.scale, (Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically));

            return false;
        }
        float tofs = 0;
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public void drawT(List<Vector2> points) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var mp = points;
            if (mp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(30, 30, 150);

                float a = 0;
                float lr = 0;
                for (int i = 1; i < mp.Count; i++) {
                    a += 1f / (float)mp.Count;

                    ve.Add(new ColoredVertex(mp[i] - Main.screenPosition + (mp[i] - mp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 22,
                          new Vector3((float)(i + 1) / mp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp[i] - Main.screenPosition + (mp[i] - mp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 22,
                          new Vector3((float)(i + 1) / mp.Count, 0, 1),
                          b * a));
                    lr = (mp[i] - mp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = WohSlash3Tex.Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }


            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            tofs++;
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            Vector2 position = base.Projectile.Center - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
            Vector2 origin = value.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(mp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();

        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
    }

}