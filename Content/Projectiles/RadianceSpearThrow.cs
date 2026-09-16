using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class RadianceSpearThrow : ModProjectile, IJavelin
    {
        //拖尾贴图,加载期就位,绘制时不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Extra/slash2")]
        internal static Asset<Texture2D> Slash2Tex;
        List<Vector2> odp = new List<Vector2>();
        List<float> odr = new List<float>();
        public bool SetHandRot { get; set; }
        public bool beam = true;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ArmorPenetration = 6;
            SetHandRot = true;
        }
        public float handrot = 0;
        public float handrotspeed = 0;
        public Vector2 ownerMouse = Vector2.Zero;

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Projectile.rotation);
            writer.Write(handrot);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Projectile.rotation = reader.ReadSingle();
            handrot = reader.ReadSingle();
        }
        public override void OnSpawn(IEntitySource source) {
            foreach (Projectile p in Main.projectile) {
                if (p.whoAmI != Projectile.whoAmI) {
                    if (p.ModProjectile is IJavelin jv) {
                        jv.SetHandRot = false;
                    }
                }
            }
        }
        public override void PostAI() {
            if (Projectile.ai[0] > 10) {
                odp.Add(Projectile.Center + Projectile.rotation.ToRotationVector2() * 76);
                odr.Add(Projectile.rotation);
                if (odp.Count > 10) {
                    odp.RemoveAt(0);
                    odr.RemoveAt(0);
                }
            }
        }
        public override void AI() {

            if (Projectile.ai[0] == 0) {
                handrotspeed = -0.3f;
            }
            else if (Projectile.ai[0] < 12) {
                handrotspeed += 0.056f;
            }
            if (Projectile.ai[0] < 12) {

                var owner = Projectile.owner.ToPlayer();

                if (Main.myPlayer == Projectile.owner) {
                    Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
                    Projectile.netUpdate = true;
                }
                if (this.SetHandRot) {
                    Projectile.owner.ToPlayer().heldProj = Projectile.whoAmI;
                    if (owner.direction == 1) {
                        Projectile.Center = owner.MountedCenter + new Vector2(26, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver2 - handrot);
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - handrot - MathHelper.Pi);
                    }
                    else {
                        Projectile.Center = owner.MountedCenter + new Vector2(26, 0).RotatedBy(Projectile.rotation + MathHelper.PiOver2 + handrot);
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + handrot);
                    }
                }
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
            }
            else if (Projectile.ai[0] < 36) {
                handrotspeed *= 0.84f;
                var owner = Projectile.owner.ToPlayer();
                if (this.SetHandRot) {
                    if (owner.direction == 1) {
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - handrot - MathHelper.Pi);
                    }
                    else {
                        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + handrot);

                    }
                    Projectile.owner.ToPlayer().heldProj = -1;
                }

            }
            if (Projectile.ai[0] > 12) {
                Projectile.tileCollide = true;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            handrot -= handrotspeed;
            if (Projectile.ai[0] == 10) {

                SoundStyle SwingSound = SoundID.Item1;
                SwingSound.Pitch = 0f;
                if (Projectile.IsEmpowered()) {
                    SwingSound.Pitch = 1f;
                }

                SoundEngine.PlaySound(SwingSound, Projectile.Center);
            }

            Projectile.ai[0]++;

        }
        public override void OnKill(int timeLeft) {
            float sparkCount = Projectile.IsEmpowered() ? 26 : 16;
            Projectile target = Projectile;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = new Vector2(16, 0).RotatedBy((Projectile.velocity * -1).ToRotation()).RotatedByRandom(MathHelper.ToRadians(40)) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(20, 24);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = Color.Yellow;

                float velc = Projectile.IsEmpowered() ? 1.5f : 0.9f;
                if (Main.rand.NextBool()) {
                    //PRT_AltSpark跟LineCal随机混用,旧Calamity spark/Lines二选一
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * velc, sparkColor2, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                }
                else {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * velc, Main.rand.NextBool() ? Color.GreenYellow : Color.LightYellow, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/Smash", 2) { Volume = 0.3f }, Projectile.Center);
            float sparkCount = Projectile.IsEmpowered() ? 26 : 16;
            for (int i = 0; i < sparkCount; i++) {
                Vector2 sparkVelocity2 = new Vector2(16, 0).RotatedBy(Projectile.rotation).RotatedByRandom(MathHelper.ToRadians(40)) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime2 = Main.rand.Next(20, 24);
                float sparkScale2 = Main.rand.NextFloat(0.95f, 1.8f);
                Color sparkColor2 = Color.Yellow;

                float velc = Projectile.IsEmpowered() ? 1.5f : 0.9f;
                if (Main.rand.NextBool()) {
                    //AltSpark Configure(bool,int)是Ports签名,不是opacity/glow/mode那套
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * velc, sparkColor2, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                }
                else {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * velc, Main.rand.NextBool() ? Color.GreenYellow : Color.LightYellow, sparkScale2 * 1).Configure(false, (int)(sparkLifetime2 * 1));
                }
            }
            if (Projectile.owner == Main.myPlayer && beam && Projectile.IsEmpowered()) {
                beam = false;
                for (int i = 0; i < 4; i++) {
                    Vector2 velocity = MathHelper.ToRadians(i * 90).ToRotationVector2() * 16;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, velocity, ModContent.ProjectileType<HolyBeam>(), (int)(Projectile.damage * 0.26f), 0f, Projectile.owner).ToProj().DamageType = DamageClass.Melee;
                }
            }
        }
        public override bool ShouldUpdatePosition() {
            return Projectile.ai[0] >= 12;
        }

        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] <= 10) {
                return false;
            }
            return null;
        }
        public bool eff = true;
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = Color.Lerp(new Color(255, 255, 255), Color.Gold, (1 - completionRatio) * 0.6f);
            return result * completionRatio;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 70 * Projectile.scale, completionRatio);
        }

        public void drawT() {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var mp = this;
            if (mp.odp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.Gold;

                float a = 0;
                float lr = 0;
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = Slash2Tex.Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }


            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            Vector2 position = base.Projectile.Center - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
            Vector2 origin = value.Size() * 0.5f;
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.SylvestaffStreakAsset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();

        }
        public override bool PreDraw(ref Color lightColor) {
            drawT();

            Texture2D tx = TextureAssets.Projectile[Projectile.type].Value;
            float rj = 0;
            if (Projectile.ai[0] < 12) {
                rj = -handrot * Projectile.owner.ToPlayer().direction;
            }
            Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4 + rj, tx.Size() / 2, Projectile.scale, SpriteEffects.None);

            return false;
        }


    }

}