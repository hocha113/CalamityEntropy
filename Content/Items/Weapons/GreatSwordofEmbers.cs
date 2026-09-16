using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Apsychos;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class GreatSwordofEmbers : ModItem, IHoldoutItem
    {
        public int ProjectileType => Item.shoot;
        public override void SetDefaults() {
            Item.damage = 146;
            Item.DamageType = DamageClass.Melee;
            Item.width = 70;
            Item.height = 72;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 8;
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EmberSwordHoldout>();
            Item.shootSpeed = 12f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            return false;
        }

        public override bool MeleePrefix() {
            return true;
        }


    }
    public class EmberSwordHoldout : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/GreatSwordofEmbers";
        //挥砍拖影贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Particles/VerticalSmearRagged")]
        internal static Asset<Texture2D> SmearRaggedTex;

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = 8;
            Projectile.scale = 1.5f;
        }
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(Dir);
            writer.Write(swing);
            writer.Write(rotVel);
            writer.Write(Breaked);
            writer.Write(NumHits);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            Dir = reader.ReadInt32();
            swing = reader.ReadInt32();
            rotVel = reader.ReadSingle();
            Breaked = reader.ReadInt32();
            NumHits = reader.ReadInt32();
        }
        public int Dir = -1;
        public int swing = 0;
        public float vsAlpha = 0;
        public float rotVel = 0;
        public bool rcl = true;
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public bool shake = true;
        public bool flag2 = true;
        public int Breaked = -1;
        public int NumHits = 0;
        public override void AI() {
            Player player = Projectile.GetOwner();
            if (init) {
                init = false;
                float scale_ = Projectile.GetOwner().HeldItem.scale;
                Projectile.GetOwner().ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
            }
            if (player.dead) {
                Projectile.Kill();
                return;
            }
            if (player.HeldItem.ModItem is GreatSwordofEmbers) {
                Projectile.timeLeft = 5;
                float speed = player.GetWeaponAttackSpeed(player.HeldItem);
                Projectile.damage = player.GetWeaponDamage(player.HeldItem);
                Projectile.knockBack = player.GetWeaponKnockback(player.HeldItem);
                Projectile.CritChance = player.GetWeaponCrit(player.HeldItem);
                player.heldProj = Projectile.whoAmI;
                if (flag2) {
                    flag2 = false;
                    Dir = Projectile.velocity.X > 0 ? -1 : 1;
                }
                player.Entropy().MouseWorldListener = true;
                Projectile.Center = Projectile.GetOwner().MountedCenter;
                float rot = (player.Entropy().MouseWorld - Projectile.Center).ToRotation();
                float targetRot = rot + Dir * 2.4f;
                if (Projectile.localAI[1]++ == 0)
                    Projectile.rotation = rot + Dir * 1.2f;
                if (swing < 0 && Breaked < 0)
                    Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.01f, false);
                else {
                    player.itemTime = 2;
                    player.itemAnimation = 2;
                }
                int swingTime = 16;
                swingTime *= Projectile.MaxUpdates;
                Projectile.rotation += rotVel * speed;
                rotVel *= (float)(Math.Pow(0.964f, speed));
                if (swing < 0)
                    Projectile.velocity = rot.ToRotationVector2() * 16;
                swing--;
                if (swing < -40 * Projectile.MaxUpdates / speed && Main.mouseLeft && Main.myPlayer == Projectile.owner && !player.mouseInterface) {
                    vsAlpha = 0;
                    Dir *= -1;
                    shake = true;
                    Projectile.ResetLocalNPCHitImmunity();
                    swing = (int)(swingTime / speed);
                    rotVel = Dir * 0.16f;
                    CEUtils.PlaySound("DemonSwordSwing1", Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 8, 0.6f);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        CEUtils.SyncProj(Projectile.whoAmI);
                }

                if (swing > 0) {
                    if (swing / (swingTime / speed) < 0.6f)
                        vsAlpha *= (float)(Math.Pow(0.986f, speed));
                    else {
                        vsAlpha = float.Lerp(vsAlpha, 1, 0.1f);
                    }
                }
                else
                    vsAlpha = 0;
                if (!(Breaked >= 0)) {
                    player.SetHandRotWithDir(Projectile.rotation, Math.Sign(player.Entropy().MouseWorld.X - player.Center.X));
                }
                else {
                    swing = -1;
                    Breaked--;
                    if (Breaked == 0) {
                        rotVel = 0;
                        HighLight = 1;
                        Dir = -player.direction;
                        Projectile.rotation = rot + Dir * 2.4f;
                    }
                }
            }
            else {
                Projectile.Kill();
            }
            HighLight *= 0.989f;
        }
        public float HighLight = 1;
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? CanDamage() {
            return swing > 0;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (shake) {
                target.AddBuff(BuffID.OnFire3, 150);
                CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2.9f, 3.4f), Projectile.Center, 8, 0.9f);

                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(-(target.Center - Projectile.Center).normalize(), 8));
                shake = false;
                float scale = 90 / 40f;
                //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
                PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
                PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
                PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
                NumHits++;
                if (NumHits > 3) {
                    NumHits = 0;
                    Breaked = 80 * Projectile.MaxUpdates;
                    swing = -1;
                    int pt = ModContent.ProjectileType<TectonicShardHoming>();
                    for (int i = 0; i < 6; i++) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(0, 100) * Projectile.scale, Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(16, 26) + CEUtils.randomPointInCircle(4), pt, Projectile.damage / 2, 4, Projectile.owner);
                    }
                    CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(1.6f, 1.8f), Projectile.Center);
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
                CEUtils.SyncProj(Projectile.whoAmI);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (Breaked > 0)
                return false;
            lightColor = Color.White;
            Texture2D tex = Projectile.GetTexture();
            Texture2D c = SmearRaggedTex.Value;
            int dir = Dir;
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            Effect shader = Apsychos.WhiteTransShader();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            shader.Parameters["strength"].SetValue(HighLight);

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor, rot, origin, Projectile.scale * scale, effect);

            float alphac = vsAlpha * 0.82f;
            float crot = Projectile.rotation + Dir * 0.1f;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(c, Projectile.Center - Main.screenPosition, null, Color.OrangeRed * alphac * 0.85f, crot, c.Size() / 2f, 0.34f * Projectile.scale, Dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 106 * Projectile.scale, targetHitbox, 30);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (76 * Projectile.scale) * Projectile.scale, 40, DelegateMethods.CutTiles);
        }
    }
    public class TectonicShardHoming : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float alpha = 1;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void PostAI() {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 7) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255) * completionRatio * alpha;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 12 * Projectile.scale, completionRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(1.8f, 2.4f), Projectile.Center, 8, 0.24f);

            float scale = 50 / 40f;
            //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 13);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 11);

        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.timeLeft = 120;
        }
        public override void AI() {
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (Projectile.localAI[0]++ < 50) {
                Projectile.velocity *= 0.96f;
            }
            else {
                if (Projectile.HomingToNPCNearby(4, 0.94f, 2000))
                    if (Projectile.timeLeft < 60)
                        Projectile.timeLeft = 60;
            }
            alpha = float.Min(1, Projectile.timeLeft / 20f);
        }
        public override bool? CanDamage() {
            return Projectile.localAI[0] >= 50;
        }

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color color = Color.OrangeRed * alpha;
            var mp = this;
            if (mp.odp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = color * 0.66f;
                b.A = 255;
                float a = 0;
                float lr = 0;
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14 * Projectile.scale,
                          new Vector3(0, 1, 1),
                        b * (1f / (float)mp.odp.Count)));
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14 * Projectile.scale,
                      new Vector3(0, 0, 1),
                      b * (1f / (float)mp.odp.Count)));
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 14 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                          b * a));
                    lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                }
                a = 1;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3) {
                    Texture2D tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                    ve = new List<ColoredVertex>();
                    b = color;

                    a = 0;
                    lr = 0;
                    for (int i = 1; i < mp.odp.Count; i++) {
                        a += 1f / (float)mp.odp.Count;

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                              b * a));
                        lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                    }
                    tx = CEExtraAssets.wohslash;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            tofs++;

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak1Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(odp, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            if (odp.Count > 1) {
                Texture2D texture = Projectile.GetTexture();
                Rectangle frame = CEUtils.GetCutTexRect(texture, 3, Projectile.whoAmI % 3, false);
                Vector2 position = odp[odp.Count - 1] - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
                Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
                CEUtils.DrawGlow(position + Main.screenPosition, color, Projectile.scale * 0.6f);
                Main.EntitySpriteDraw(texture, position, frame, Projectile.GetAlpha(Color.White) * alpha, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            }
            return false;
        }
        public int tofs;
    }

}
