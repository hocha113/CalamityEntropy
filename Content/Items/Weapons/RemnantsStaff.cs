using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class RemnantsStaff : ModItem
    {
        public override void SetStaticDefaults() {
            Item.staff[Item.type] = true;
        }
        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = rt + player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float d = (float)(Math.Cos(Utils.Remap(animProgress, 0, 1, -MathHelper.Pi, MathHelper.Pi))) * 14;
            if (player.reuseDelay <= 0)
                d = 0;
            Vector2 itemPosition = player.MountedCenter + new Vector2(0, 46) + itemRotation.ToRotationVector2() * (26 + d);
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = Vector2.Zero;

            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player) {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = rt + (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
        #endregion
        public override void SetDefaults() {
            Item.damage = 12;
            Item.DamageType = DamageClass.Magic;
            Item.width = 60;
            Item.height = 78;
            Item.useTime = 6;
            Item.useAnimation = 18;
            Item.reuseDelay = 16;
            Item.knockBack = 4.4f;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<ScorchingFireballMagic>();
            Item.shootSpeed = 2f;
            Item.mana = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.LightRed;
        }
        public int attackCount = 0;
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.FlowerofFire)
                .AddIngredient<TectonicShard>(6)
                .AddTile(TileID.Hellforge)
                .Register();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            velocity = velocity.RotatedBy(player.direction * -0.16f).RotatedBy(rt);
            position = position + velocity.normalize() * 64;
            Projectile.NewProjectile(source, position, velocity * Main.rand.NextFloat(0.75f, 1f), type, damage, knockback, player.whoAmI);

            return false;
        }
        public float rt = 0;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
            rt = Main.rand.NextFloat(-0.2f, 0.2f);
            velocity = velocity.RotatedBy(player.direction * 0.16f);
        }
        public override bool MagicPrefix() {
            return true;
        }
    }
    public class ScorchingFireballMagic : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float scale = 0f;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void PostAI() {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 16) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255) * completionRatio;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 14 * Projectile.scale * scale, completionRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 180);
            if (Projectile.timeLeft > 2)
                Projectile.timeLeft = 2;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 25;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 0.8f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 300;
            Projectile.MaxUpdates = 2;
        }
        public override void AI() {
            if (scale < 1)
                scale += 0.05f;
            if (Projectile.localAI[2] == 0)
                CEUtils.PlaySound("feathershot", Main.rand.NextFloat(1.2f, 1.4f), Projectile.Center, 8, 0.6f);
            if (Projectile.localAI[2] < 36 && Projectile.localAI[2] > 9)
                Projectile.velocity *= 1.07f;
            if (Projectile.localAI[2]++ > 32)
                Projectile.HomingToNPCNearby(1.2f, 0.96f, 640);
            Projectile.rotation += Projectile.velocity.X * 0.014f;
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("cinderExplosion", 0.7f, Projectile.Center, 12, 0.6f);
            float scale = 2.8f;
            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 26);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 22);
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, 150, Projectile.DamageType);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color color = Color.OrangeRed;
            var mp = this;
            if (mp.odp.Count > 1) {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = color * 0.66f;
                b.A = 255;
                float a = 0;
                float lr = 0;
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale * scale,
                          new Vector3(0, 1, 1),
                        b * (1f / (float)mp.odp.Count)));
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale * scale,
                      new Vector3(0, 0, 1),
                      b * (1f / (float)mp.odp.Count)));
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale * scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale * scale,
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

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * a * Projectile.scale * scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * a * Projectile.scale * scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                              b * a));
                        lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                    }
                    tx = CEExtraAssets.MegaStreakBacking2;
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
                Vector2 position = odp[odp.Count - 1] - Main.screenPosition + Vector2.UnitY * base.Projectile.gfxOffY;
                Main.spriteBatch.UseAdditive();
                Texture2D r = CEExtraAssets.Ray;
                Main.spriteBatch.Draw(r, position, null, Color.OrangeRed, Projectile.rotation, r.Size() * 0.5f, Projectile.scale * 0.64f * scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(r, position, null, Color.White, Projectile.rotation, r.Size() * 0.5f, Projectile.scale * 0.3f * scale, SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
                CEUtils.DrawGlow(position + Main.screenPosition, color, Projectile.scale * 0.75f * scale);
                CEUtils.DrawGlow(position + Main.screenPosition, Color.White, Projectile.scale * 0.5f * scale);
            }
            return false;
        }
        public int tofs;
    }
    public class RemnantsLaser : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 5 * 60);
            float scale = 100 / 40f;
            //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.Red * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 26);
            PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(target.Center, Vector2.Zero, Color.OrangeRed * 1.4f, 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 22);
        }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }
        public int counter = 0;
        public int length = 2000;
        NPC ownern = null;
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
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Magic;
        }
        public bool st = true;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 16;
        }
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (st) {
                //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 20), 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 160), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 160), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 255, 160), 0.29f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                CEUtils.PlaySound("CrystalBallActive", 0.6f + Main.rand.NextFloat(-0.2f, 0.2f), Projectile.Center, 10, 0.4f);
                st = false;
            }
            if (Projectile.timeLeft < 6) {
                width -= 1f / 16f;
            }
            else {
                width += 1f / 16f;

            }
            aicounter++;
            float maxlength = 2000;
            for (float i = 0; i < maxlength; i += 8) {
                Vector2 v = Projectile.Center + Projectile.rotation.ToRotationVector2() * i;
                length = (int)i;
                if (!CEUtils.inWorld(v) || (Main.tile[(int)(v.X / 16), (int)(v.Y / 16)].IsTileSolid())) {
                    break;
                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * length, targetHitbox, 30);
        }
        public override bool PreDraw(ref Color lightColor) {
            counter++;
            Texture2D tex = CEExtraAssets.DeathRay2;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearWrap);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 18, 0, length, tex.Height), Color.Yellow, Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.8f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 26, 0, length, tex.Height), new Color(255, 255, 90), Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.6f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-counter * 40, 0, length, tex.Height), Color.White, Projectile.rotation, new Vector2(0, tex.Height / 2), new Vector2(1, width * 0.5f), SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);

            Texture2D star = CEExtraAssets.StarTexture;
            float num = 0.5f * (float)Math.Sin(Main.GameUpdateCount / 10f);
            float num2 = 0.5f * (float)Math.Sin(Main.GameUpdateCount / 10f + MathHelper.PiOver4);
            Vector2 pos = Projectile.Center;
            Color color = new Color(225, 190, 40);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.2f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, pos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.2f, SpriteEffects.None, 0);
            Vector2 epos = Projectile.Center + Projectile.rotation.ToRotationVector2() * length;
            Main.spriteBatch.Draw(star, epos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, epos - Main.screenPosition, null, color * Projectile.Opacity, 0, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, epos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.2f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, epos - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, star.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.2f, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
    }
}
