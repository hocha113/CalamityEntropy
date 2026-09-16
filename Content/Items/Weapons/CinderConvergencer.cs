using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.NPCs.Apsychos;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class CinderConvergencer : ModItem, IHoldoutItem
    {
        public int ProjectileType => Item.shoot;

        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 72;
            Item.damage = 24;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.crit = 5;
            Item.value = Item.buyPrice(0, 20);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<CinderConvergencerHoldout>();
            Item.UseSound = null;
            Item.shootSpeed = 12f;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public override bool RangedPrefix() {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            return false;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.PhoenixBlaster)
                .AddIngredient(ItemID.IllegalGunParts, 1)
                .AddIngredient<TectonicShard>(8)
                .AddTile(TileID.Hellforge)
                .Register();

        }
    }
    public class CinderConvergencerHoldout : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/CinderConvergencer";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
        }
        public float Charge = 0;
        public int EnhancedBulletCount = 0;
        public override bool? CanDamage() => false;
        public float ShootDelay = 0;
        public float rotAdd = 0;
        public float rotV = 0;
        public float HighLight = 0.5f;
        public override void AI() {
            HighLight *= 0.92f;
            Player player = Projectile.GetOwner();
            if (player.dead) {
                Projectile.Kill();
                return;
            }
            Projectile.StickToPlayer();
            Projectile.rotation += player.direction * rotAdd;
            player.SetHandRot(Projectile.rotation);
            if (player.HeldItem.ModItem is CinderConvergencer) {
                Projectile.damage = player.GetWeaponDamage(player.HeldItem);
                Projectile.knockBack = player.GetWeaponKnockback(player.HeldItem);
                Projectile.CritChance = player.GetWeaponCrit(player.HeldItem);
                player.heldProj = Projectile.whoAmI;
                Projectile.timeLeft = 3;

                if (player.whoAmI == Projectile.owner) {
                    if (Main.mouseLeft && !player.mouseInterface) {
                        if (ShootDelay <= 0) {
                            Vector2 shootPos = Projectile.Center + new Vector2(10, -14 * player.direction).RotatedBy(Projectile.rotation);
                            if (EnhancedBulletCount <= 0) {
                                Charge += 0.1f;
                                CEUtils.PlaySound("malignShoot", 2, Projectile.Center, volume: 0.7f);
                                ShootDelay = Projectile.MaxUpdates * player.HeldItem.useTime / player.GetWeaponAttackSpeed(player.HeldItem);
                                rotV = -0.2f;
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), shootPos, Projectile.velocity.normalize() * 16, ModContent.ProjectileType<CinderFireball>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                                if (Charge >= 1) {
                                    CEUtils.PlaySound("DeadSunShot", 1, Projectile.Center);
                                    Charge = 0;
                                    EnhancedBulletCount = 3;
                                    HighLight = 1;
                                    ShootDelay *= 2;
                                }
                            }
                            else {
                                EnhancedBulletCount--;
                                CEUtils.PlaySound("CinderEnhanced", 1, Projectile.Center, volume: 0.8f);
                                ShootDelay = 2 * Projectile.MaxUpdates * player.HeldItem.useTime / player.GetWeaponAttackSpeed(player.HeldItem);
                                rotV = -0.3f;
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), shootPos, Projectile.velocity.normalize() * 7, ModContent.ProjectileType<CinderEnhancedShoot>(), Projectile.damage * 4, Projectile.knockBack, Projectile.owner);
                                if (EnhancedBulletCount <= 0) {
                                    Charge = 0;
                                    HighLight = 1;
                                }
                            }
                        }
                    }
                }
            }
            rotAdd += rotV;
            rotV *= 0.5f;
            rotAdd *= 0.5f;
            if (ShootDelay > 0) {
                ShootDelay--;
                player.itemTime = player.itemAnimation = 3;
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            if (EnhancedBulletCount > 0)
                tex = this.getTextureAlt();
            Effect shader = Apsychos.WhiteTransShader();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            shader.Parameters["strength"].SetValue(HighLight);
            Vector2 origin = new Vector2(12, Projectile.GetOwner().direction > 0 ? 22 : (tex.Height - 22));
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    #region projectile
    public class CinderFireball : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void PostAI() {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 12) {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex) {
            Color result = new Color(255, 255, 255) * completionRatio;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex) {
            return MathHelper.Lerp(0, 14 * Projectile.scale, completionRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public override void OnKill(int timeLeft) {
            float scale = 30 / 40f;
            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 0.95f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.95f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            CEUtils.PlaySound("cinderExplosion", Main.rand.NextFloat(1.8f, 2f), Projectile.Center);
            //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 12);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 20);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 25;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.light = 0.4f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 500;
            Projectile.MaxUpdates = 3;
        }
        public override void AI() {
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center + CEUtils.randomPointInCircle(2) - Projectile.velocity * Main.rand.NextFloat(), CEUtils.randomPointInCircle(2), Color.Firebrick, 0.4f).Configure(1f, 8, Main.rand.NextFloat(-0.1f, 0.1f), true);
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
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale,
                          new Vector3(0, 1, 1),
                        b * (1f / (float)mp.odp.Count)));
                ve.Add(new ColoredVertex(mp.odp[0] - Main.screenPosition + (mp.odp[1] - mp.odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale,
                      new Vector3(0, 0, 1),
                      b * (1f / (float)mp.odp.Count)));
                for (int i = 1; i < mp.odp.Count; i++) {
                    a += 1f / (float)mp.odp.Count;

                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 18 * Projectile.scale,
                          new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                        b * a));
                    ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 18 * Projectile.scale,
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

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 16 * a * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 16 * a * Projectile.scale,
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
                CEUtils.DrawGlow(position + Main.screenPosition, color, Projectile.scale * 0.6f);
                CEUtils.DrawGlow(position + Main.screenPosition, Color.White, Projectile.scale * 0.3f);
            }
            return false;
        }
        public int tofs;
    }

    public class CinderEnhancedShoot : ModProjectile
    {
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults() {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.hostile = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 200 * 8;
            Projectile.penetrate = 1;
            Projectile.MaxUpdates = 8;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public override void AI() {
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 60) {
                oldPos.RemoveAt(0);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = Projectile.ai[0];
        }
        public override void OnKill(int timeLeft) {
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(2.6f, 3f), Projectile.Center, 8, 0.4f);
            float scale = 120 / 40f;
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, (int)(Projectile.damage * 0.85f), 130, Projectile.DamageType);
            //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Blue * 0.8f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(80, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 24);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(80, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 18);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(80, 80, 255), 0.005f).Configure("CalamityEntropy/Assets/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.02f, 15);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseAdditive();
            float scale = 1;

            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            for (int i = 0; i < oldPos.Count; i++) {
                float c = (i + 1f) / oldPos.Count;
                DrawEnergyBall(oldPos[i], scale * c, Projectile.Opacity * c);
            }
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, Projectile.GetTexture().Size() * 0.5f, 1, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public void DrawEnergyBall(Vector2 pos, float size, float alpha) {
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(180, 180, 255) * alpha, 0, tex.Size() * 0.5f, size * 0.12f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(120, 120, 160) * alpha, 0, tex.Size() * 0.5f, size * 0.2f, SpriteEffects.None, 0);
        }
    }
    #endregion
}
