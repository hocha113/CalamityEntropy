using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NetherRiftBlade : ModProjectile
    {
        //拖尾与锁链贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/NetherRiftHandle")]
        internal static Asset<Texture2D> HandleTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/NetherChain")]
        internal static Asset<Texture2D> ChainTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/NetherChainWhite")]
        internal static Asset<Texture2D> ChainWhiteTex;
        public List<Vector2> odp = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 136;
            Projectile.height = 136;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3;
            Projectile.MaxUpdates = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.ArmorPenetration = 1000;
        }
        public int counter1 = 0;
        public int counter2 = 0;
        public Vector2 mousePos = Vector2.Zero;
        public float length { get { return Projectile.ai[1]; } set { Projectile.ai[1] = value; } }
        float rotspeed = 0;
        float l = 0;
        bool soundplay = false;
        int counter = 0;
        public override bool? CanHitNPC(NPC target) {
            if (counter < 30) {
                return false;
            }
            return null;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            writer.Write(channel);
            writer.WriteVector2(mousePos);
            writer.Write(chainToMouse);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
            channel = reader.ReadBoolean();
            mousePos = reader.ReadVector2();
            chainToMouse = reader.ReadBoolean();
        }
        public bool channel = false;
        public bool chainToMouse = false;
        public override void AI() {
            if (counter1 % 6 == 0) {
                bool flag = false;
                if ((odp.Count < 2 || CEUtils.getDistance(odp[odp.Count - 1], Projectile.Center) > 46)) {
                    flag = true;
                    odp.Add(Projectile.Center);
                }
                if (odp.Count > 14 || (!flag && odp.Count > 0)) {
                    odp.RemoveAt(0);
                }
            }
            counter++;
            counter1++;
            if (rope == null) {
                rope = new Rope(Projectile.owner.ToPlayer().Center, Projectile.Center, 92, 0, new Vector2(0, 0), 0.02f, 32, false);
            }
            rope.segmentLength = CEUtils.getDistance(Projectile.Center, Projectile.owner.ToPlayer().Center) / 92f;
            rope.Start = Projectile.owner.ToPlayer().Center;
            rope.End = Projectile.Center;
            rope.Update();
            List<Vector2> p = rope.GetPoints();
            Projectile.rotation = (p[p.Count - 1].GetSymmetryPoint(Projectile.owner.ToPlayer().Center, Projectile.Center) - p[p.Count - 2].GetSymmetryPoint(Projectile.owner.ToPlayer().Center, Projectile.Center)).ToRotation();
            if (chainToMouse) {
                Projectile.rotation = (Projectile.Center - mousePos).ToRotation();
            }
            Player player = Projectile.owner.ToPlayer();
            Projectile.timeLeft = 3;
            if (Main.myPlayer == Projectile.owner) {
                if (mousePos != Main.MouseWorld) {
                    Projectile.netUpdate = true;
                    mousePos = Main.MouseWorld;
                }
            }
            if (player.channel) {
                channel = true;
            }
            player.channel = channel;
            if (Main.myPlayer == Projectile.owner) {
                if (Main.mouseLeft && !channel) {
                    channel = true;
                    Projectile.netUpdate = true;
                }
                if (!Main.mouseLeft && channel) {
                    channel = false;
                    player.channel = false;
                    Projectile.netUpdate = true;
                }
            }
            if (!player.channel) {
                if (Projectile.ai[0] == 0) {
                    Projectile.Center = player.Center;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.velocity = (Main.MouseWorld - Projectile.Center).normalize() * 16;

                        Projectile.netUpdate = true;
                    }

                    Projectile.ai[0] = 1;
                }
            }
            Projectile.localAI[1] *= 0.98f;
            if (Projectile.ai[0] == 1) {
                if (player.channel && Projectile.ai[2] > 68 && Projectile.localAI[2] == 0) {
                    l = l + (length - l) * 0.1f;
                    rotspeed += (0.1f - rotspeed) * 0.01f;
                    Vector2 targetpos = mousePos + new Vector2(l, 0).RotatedBy((Projectile.Center - mousePos).ToRotation() + rotspeed * player.direction * 0.76f);
                    Projectile.velocity = targetpos - Projectile.Center;
                    float a = (Projectile.Center - player.Center).ToRotation();
                    if (a < 0) {
                        soundplay = true;
                    }
                    else {
                        if (soundplay) {
                            soundplay = false;
                            if (Main.rand.NextBool(2)) {
                                CEUtils.PlaySound("spin1", 1f, Projectile.Center, volume: CEUtils.WeapSound);
                            }
                            else {
                                CEUtils.PlaySound("spin2", 1f, Projectile.Center, volume: CEUtils.WeapSound);
                            }
                        }
                    }
                    if (!chainToMouse) {
                        chainToMouse = true;
                        Projectile.localAI[1] = 1;
                        Projectile.netUpdate = true;
                        foreach (var pt in p) {
                            //TrailSparkParticle跟TrailParticle成对spawn,旧trail+spark一套
                            PRTLoader.NewParticle<PRT_TrailSparkParticle>(pt, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-8, 8), Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);

                        }
                    }
                }
                else {
                    if (chainToMouse) {
                        Projectile.localAI[2] = 1;
                        chainToMouse = false;
                        Projectile.localAI[0] = 10 * Projectile.MaxUpdates;
                        Projectile.localAI[1] = 1;
                        Projectile.netUpdate = true;
                        foreach (var pt in p) {
                            //TrailSparkParticle跟TrailParticle成对spawn,旧trail+spark一套
                            PRTLoader.NewParticle<PRT_TrailSparkParticle>(pt, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-8, 8), Color.White, 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);

                        }
                    }

                    Projectile.ai[2]++;
                    if (Projectile.ai[2] > 36) {
                        Projectile.velocity *= 0.98f;
                    }
                    if (Projectile.ai[2] == 50) {
                        Projectile.localAI[0] = 16 * Projectile.MaxUpdates;
                    }
                    if (Projectile.localAI[0] > 0) {
                        Projectile.localAI[0]--;
                        Projectile.velocity *= 0.994f;
                    }
                    else {
                        if (Projectile.ai[2] > 50) {
                            Projectile.velocity = (player.Center - Projectile.Center).normalize() * (Projectile.velocity.Length() + 0.3f);
                            if (Projectile.Distance(player.Center) < Projectile.velocity.Length() * 1.1f) {
                                Projectile.Kill();
                            }
                        }
                    }
                }
            }
            else {
                if (counter1 > 2) {
                    l = l + (length - l) * 0.1f;
                    rotspeed += (0.1f - rotspeed) * 0.01f;
                    Vector2 targetpos = player.Center + new Vector2(l, 0).RotatedBy((Projectile.Center - player.Center).ToRotation() + rotspeed * player.direction * 0.76f);
                    Projectile.velocity = targetpos - Projectile.Center;
                }
                float a = (Projectile.Center - player.Center).ToRotation();
                if (a < 0) {
                    soundplay = true;
                }
                else {
                    if (soundplay) {
                        soundplay = false;
                        if (Main.rand.NextBool(2)) {
                            CEUtils.PlaySound("spin1", 1f, Projectile.Center, volume: CEUtils.WeapSound);
                        }
                        else {
                            CEUtils.PlaySound("spin2", 1f, Projectile.Center, volume: CEUtils.WeapSound);
                        }
                    }
                }
            }
            if (Main.myPlayer == Projectile.owner) {
                length = chainToMouse ? 240 : 180;
            }
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.heldProj = Projectile.whoAmI;
            if (Main.myPlayer == Projectile.owner)
                player.direction = (Main.MouseWorld.X > player.Center.X ? 1 : -1);
        }
        Rope rope;
        public List<Vector2> GP(float distAdd = 0, float c = 1) {
            float dist = distAdd;
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 60; i++) {
                points.Add(new Vector2(dist, 0).RotatedBy(MathHelper.ToRadians(i * 6 - 80 * c * Main.GlobalTimeWrappedHourly)));
            }
            return points;
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak2Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            odp.Add(Projectile.Center);

            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor2, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);

            odp.RemoveAt(odp.Count - 1);
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Player player = Projectile.owner.ToPlayer();
            if (chainToMouse) {
                {
                    float alpha = 0.8f;
                    Main.spriteBatch.End();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    {
                        List<ColoredVertex> ve = new List<ColoredVertex>();
                        List<Vector2> points = GP(0);
                        List<Vector2> pointsOutside = GP(84);
                        int i;
                        for (i = 0; i < points.Count; i++) {
                            ve.Add(new ColoredVertex(mousePos - Main.screenPosition + points[i],
                                  new Vector3((float)i / points.Count, 1, 0.99f),
                                  Color.SkyBlue * 0.66f * alpha));
                            ve.Add(new ColoredVertex(mousePos - Main.screenPosition + pointsOutside[i],
                                  new Vector3((float)i / points.Count, 0, 0.99f),
                                  Color.SkyBlue * 0.66f * alpha));

                        }
                        SpriteBatch sb = Main.spriteBatch;
                        GraphicsDevice gd = Main.graphics.GraphicsDevice;
                        if (ve.Count >= 3) {
                            Texture2D tx = CEExtraAssets.AbyssalCircle2;
                            gd.Textures[0] = tx;
                            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        }
                    }
                    {
                        List<ColoredVertex> ve = new List<ColoredVertex>();
                        List<Vector2> points = GP(0, -1);
                        List<Vector2> pointsOutside = GP(84, -1);
                        int i;
                        for (i = 0; i < points.Count; i++) {
                            ve.Add(new ColoredVertex(mousePos - Main.screenPosition + points[i],
                                  new Vector3((float)i / points.Count, 1, 0.99f),
                                  Color.SkyBlue * 0.66f * alpha));
                            ve.Add(new ColoredVertex(mousePos - Main.screenPosition + pointsOutside[i],
                                  new Vector3((float)i / points.Count, 0, 0.99f),
                                  Color.SkyBlue * 0.66f * alpha));

                        }
                        SpriteBatch sb = Main.spriteBatch;
                        GraphicsDevice gd = Main.graphics.GraphicsDevice;
                        if (ve.Count >= 3) {
                            Texture2D tx = CEExtraAssets.AbyssalCircle2;
                            gd.Textures[0] = tx;
                            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                        }
                    }
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                }
            }
            {
                List<Vector2> points = new List<Vector2>();
                points = rope.GetPoints();

                Texture2D handle = HandleTex.Value;
                Main.spriteBatch.Draw(handle, (chainToMouse ? mousePos : Projectile.owner.ToPlayer().Center) + player.gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.White, chainToMouse ? (Projectile.Center - mousePos).ToRotation() : (points[1] - points[0]).ToRotation(), new Vector2(28, handle.Height / 2), Projectile.scale, SpriteEffects.None, 0);
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                Color b = lightColor;
                if (!chainToMouse) {
                    points.Insert(0, Projectile.owner.ToPlayer().Center);
                    points.Add(Projectile.Center);
                }
                float lc = 1;
                float jn = 0;
                if (!chainToMouse) {
                    for (int i = 1; i < points.Count - 1; i++) {
                        jn += CEUtils.getDistance(points[i - 1], points[i]) / (float)90 * lc;

                        ve.Add(new ColoredVertex(points[i].GetSymmetryPoint(Projectile.owner.ToPlayer().Center, Projectile.Center) - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 25 * lc,
                              new Vector3(jn, 1, 1),
                              Color.White));
                        ve.Add(new ColoredVertex(points[i].GetSymmetryPoint(Projectile.owner.ToPlayer().Center, Projectile.Center) - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 25 * lc,
                              new Vector3(jn, 0, 1),
                              Color.White));
                        ve2.Add(new ColoredVertex(points[i].GetSymmetryPoint(Projectile.owner.ToPlayer().Center, Projectile.Center) - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 25 * lc,
                              new Vector3(jn, 1, 1),
                              Color.White * Projectile.localAI[1]));
                        ve2.Add(new ColoredVertex(points[i].GetSymmetryPoint(Projectile.owner.ToPlayer().Center, Projectile.Center) - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 25 * lc,
                              new Vector3(jn, 0, 1),
                              Color.White * Projectile.localAI[1]));
                    }

                    SpriteBatch sb = Main.spriteBatch;
                    GraphicsDevice gd = Main.graphics.GraphicsDevice;
                    if (ve.Count >= 3) {
                        gd.Textures[0] = ChainTex.Value;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                        gd.Textures[0] = ChainWhiteTex.Value;
                        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
                    }
                }
                else {
                    Texture2D c = ChainTex.Value;
                    Texture2D c2 = ChainWhiteTex.Value;
                    Main.EntitySpriteDraw(c, mousePos - Main.screenPosition, new Rectangle(0, 0, (int)CEUtils.getDistance(Projectile.Center, mousePos), c.Height), Color.White, (Projectile.Center - mousePos).ToRotation(), new Vector2(0, c.Height / 2), 1, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(c2, mousePos - Main.screenPosition, new Rectangle(0, 0, (int)CEUtils.getDistance(Projectile.Center, mousePos), c.Height), Color.White * Projectile.localAI[1], (Projectile.Center - mousePos).ToRotation(), new Vector2(0, c.Height / 2), 1, SpriteEffects.None, 0);

                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


                /*for (int i = 1; i < points.Count; i++)
                {
                    Texture2D t = ModContent.Request<Texture2D>("CalamityEntropy/Extra/white").Value;
                    Util.drawLine(Main.spriteBatch, t, points[i - 1], points[i], Color.White, 8);
                }*/

                Texture2D pt = TextureAssets.Projectile[Projectile.type].Value;
                Main.spriteBatch.Draw(pt, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pt.Size() / 2, Projectile.scale * 2, (Projectile.GetOwner().direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically), 0);
                Main.spriteBatch.End();
                Main.spriteBatch.begin_();
                return false;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            return false;
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(160, 160, 255);
            return result * completionRatio;
        }
        public Color TrailColor2(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255);
            return result * completionRatio;
        }
        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 122 * Projectile.scale, completionRatio);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 60, 2, 600, 16);
            if (Projectile.ai[2] < 74 && !channel) {
                Projectile.velocity *= -0.5f;
                Projectile.localAI[0] = 16 * Projectile.MaxUpdates;
                float s = Utils.Remap(Main.LocalPlayer.Distance(target.Center), 1600, 300, 0f, 8);
                ScreenShaker.AddShake((Main.LocalPlayer.Center - target.Center).normalize() * 10, s);
                Projectile.netUpdate = true;
                CEUtils.PlaySound("scatter", 1, Projectile.Center, volume: CEUtils.WeapSound);
                Projectile.ai[2] = 74;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), 0, 0, Projectile.owner, 0).ToProj().hostile = false;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<NetherRiftCrack>(), Projectile.damage, 1, Projectile.owner);
            }

        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (!chainToMouse && !channel && Projectile.ai[2] < 74) {
                modifiers.SourceDamage *= 4f;
            }
            if (chainToMouse) {
                modifiers.SourceDamage *= 0.7f;
            }
        }
    }
}