
using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using InnoVault;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class EndlessAbyssLaser : ModProjectile
    {
        //激光着色器与贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Effects/abyssallaser", AssetMode.EffectValue, "fableeyelaser")]
        internal static Effect AbyssalLaserShader;
        [VaultLoaden("CalamityEntropy/Assets/Extra/clback2")]
        internal static Asset<Texture2D> LaserBack2Tex;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 20, 1, 600, 20);
        }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 6000;
        }
        public int counter = 0;
        List<Vector2> p = new List<Vector2>();
        List<Vector2> l = new List<Vector2>();
        public int length = 2500;
        public float width = 0;
        public int aicounter = 0;
        public override void SetDefaults() {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;

        }
        public bool st = true;
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindNPCs.Add(index);
        }
        public LoopSound sound = null;
        public LoopSound sound2 = null;
        public override void AI() {
            if (st) {
                SoundEffect sf = CalamityEntropy.ealaserSound;
                SoundEffect sf2 = CalamityEntropy.ealaserSound2;
                sound = new LoopSound(sf);
                sound.play();
                sound2 = new LoopSound(sf2);
                sound2.play();
                st = false;
                for (int ii = 0; ii < 100; ii++) {
                    counter++;
                    var rand = Main.rand;
                    int tspeed = 46;
                    if (counter % 1 == 0) {
                        p.Add(new Vector2(0, rand.Next(0, 41) - 20));
                    }
                    if (counter % 6 == 0) {
                        l.Add(new Vector2(0, rand.Next(0, 17) - 8));
                    }
                    for (int i = 0; i < p.Count; i++) {
                        p[i] = p[i] + new Vector2(tspeed, 0);
                    }
                    for (int i = 0; i < l.Count; i++) {
                        l[i] = l[i] + new Vector2(tspeed, 0);
                    }
                    for (int i = 0; i < p.Count; i++) {
                        if (p[i].X > length) {
                            p.RemoveAt(i);
                            break;
                        }
                    }
                    for (int i = 0; i < l.Count; i++) {
                        if (l[i].X > length) {
                            l.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            sound.timeleft = 2;
            sound2.timeleft = 2;
            if (CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) > 600) {
                if (CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) > 2000) {
                    sound.setVolume(0);
                    sound2.setVolume(0);
                }
                else {
                    sound.setVolume(1 - (float)(CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) - 600) / 1400f);
                    sound2.setVolume(1 - (float)(CEUtils.getDistance(Projectile.Center, Main.LocalPlayer.Center) - 600) / 1400f);
                }
            }
            else {
                sound.setVolume(1);
                sound2.setVolume(1);

            }

            CEUtils.SetShake(Projectile.Center, 4.5f, 1800);
            if (Main.myPlayer == Projectile.owner) {
                Player owner = Main.LocalPlayer;
                Vector2 nv = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.One) * 16;
                if (nv != Projectile.velocity) {
                    Projectile.netUpdate = true;
                }
                Projectile.velocity = nv;

            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center = Projectile.owner.ToPlayer().MountedCenter + Projectile.owner.ToPlayer().gfxOffY * Vector2.UnitY + Projectile.rotation.ToRotationVector2() * 14;
            if (Projectile.timeLeft < 6) {
                width -= 1f / 7f;
            }
            else if (Projectile.timeLeft > 6) {
                width += 1f / 7f;

            }
            if (Projectile.timeLeft == 6 && Projectile.owner.ToPlayer().channel) {
                Projectile.timeLeft++;
            }
            aicounter++;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return width >= 0.3f && CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * length, targetHitbox, 50);
        }
        float yx = 0;
        public List<Vector2> getSamplePoints() {
            List<Vector2> p = new List<Vector2>();
            for (int i = 0; i <= length; i++) {
                p.Add(Projectile.Center + Projectile.velocity.normalize() * i);
            }
            return p;
        }
        public override bool PreDraw(ref Color lightColor) {
            float w = width * 0.7f;
            yx += 0.036f;
            List<Vector2> points = this.getSamplePoints();
            points.Insert(0, Projectile.Center - Projectile.velocity);
            if (points.Count < 2) {
                return false;
            }
            Main.spriteBatch.End();
            var effect = AbyssalLaserShader;
            effect.Parameters["yofs"].SetValue(-yx);
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                //effect.CurrentTechnique.Passes["fableeyelaser"].Apply();

                Texture2D tx = CEExtraAssets.BasicTrail;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 255, 255);
                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++) {
                    float wd = 1;
                    if (i < 360) {
                        wd = new Vector2(1, 0).RotatedBy((i / 360f) * MathHelper.PiOver2).Y;
                    }
                    wd += i * 0.001f;
                    if (wd < 1)
                        wd = 1;
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 66 * Projectile.scale * w,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 66 * Projectile.scale * w,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.32f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                Main.spriteBatch.End();
            }
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                //effect.CurrentTechnique.Passes["fableeyelaser"].Apply();

                Texture2D tx = LaserBack2Tex.Value;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 235, 235);
                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++) {
                    float wd = 1;
                    if (i < 360) {
                        wd = new Vector2(1, 0).RotatedBy((i / 360f) * MathHelper.PiOver2).Y;
                    }
                    wd += i * 0.001f;
                    if (wd < 1)
                        wd = 1;
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 22 * Projectile.scale * w,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 22 * Projectile.scale * w,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.32f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
                Main.spriteBatch.End();
            }
            effect.Parameters["yofs"].SetValue(yx);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            {
                Texture2D tx = CEExtraAssets.SylvestaffStreak;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 255, 255) * 0.7f;
                float p = -Main.GlobalTimeWrappedHourly * 4;
                for (int i = 1; i < points.Count; i++) {
                    float wd = 1;
                    if (i < 360) {
                        wd = new Vector2(1, 0).RotatedBy((i / 360f) * MathHelper.PiOver2).Y;
                    }
                    wd += i * 0.001f;
                    if (wd < 1f)
                        wd = 1f;
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 64 * Projectile.scale * w,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 64 * Projectile.scale * w,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.36f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                }
            }
            {
                Texture2D tx = CEExtraAssets.PatchyTallNoise;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 255, 255) * 0.75f;
                float p = -Main.GlobalTimeWrappedHourly * 4;
                for (int i = 1; i < points.Count; i++) {
                    float wd = 1;
                    if (i < 360) {
                        wd = new Vector2(1, 0).RotatedBy((i / 360f) * MathHelper.PiOver2).Y;
                    }
                    wd += i * 0.001f;
                    if (wd < 1f)
                        wd = 1f;
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 98 * Projectile.scale * w,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 98 * Projectile.scale * w,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.36f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    gd.Textures[0] = CEExtraAssets.Streak1;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

}