using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
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
    public class CinderLantern : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.IntegrateHotkey(CEKeybinds.CommandMinions);
        }
        public override void SetDefaults() {
            Item.damage = 12;
            Item.DamageType = DamageClass.Summon;
            Item.width = 36;
            Item.height = 64;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 5;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.shoot = ModContent.ProjectileType<CinderMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(0, 10);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<CinderCore>();
            Item.rare = ItemRarityID.LightRed;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;
            return false;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.HellstoneBar, 8)
                .AddIngredient<TectonicShard>(6)
                .AddIngredient(ItemID.DemonTorch, 4)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
    public class CinderCore : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<CinderMinion>();
    }
    public class CinderMinion : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.8f;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
        }
        public override bool? CanDamage() {
            return false;
        }
        public override void AI() {
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<CinderCore>();
            if (Projectile.localAI[0] == 0) {
                for (int i = 0; i < 16; i++)
                    //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                    PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center, CEUtils.randomPointInCircle(9), Color.OrangeRed, Main.rand.NextFloat(0.6f, 1f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 22);
                float scale = 1f;
                PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/FlameExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 8);
            }
            if (Projectile.localAI[0]++ < 3) {
                Projectile.timeLeft++;
                return;
            }
            if (Projectile.Distance(player.Center) > 4000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            NPC target = Projectile.FindMinionTarget();
            if (target != null) {
                Projectile.pushByOther(1);
                if (Projectile.ai[0] > 0) {
                    Projectile.localAI[1]++;
                    if (Projectile.localAI[1] >= 5) {
                        Projectile.localAI[1] = 0;
                        Projectile.ai[0]--;
                        Vector2 v = Shoot(target);
                        Projectile.velocity -= v * 0.2f;
                    }
                    Projectile.velocity *= 0.96f;
                }
                else {
                    Projectile.localAI[1]++;
                    if (Projectile.localAI[1] >= 46) {
                        Projectile.localAI[1] = 0;
                        Projectile.ai[0] = 3;
                    }
                }
                Vector2 tpos = target.Center + new Vector2(Math.Sign(Projectile.Center.X - target.Center.X) * 180, -90);
                if (CEUtils.getDistance(tpos, Projectile.Center) > 36) {
                    Projectile.velocity += (tpos - Projectile.Center) * 0.0048f;
                }
                Projectile.velocity *= 0.96f;
                Projectile.spriteDirection = Math.Sign(target.Center.X - Projectile.Center.X);
            }
            else {
                Projectile.pushByOther(0.4f);
                Projectile.spriteDirection = (Math.Sign(player.Center.X - Projectile.Center.X));
                Projectile.velocity *= 0.96f;
                Vector2 tpos = player.Center + new Vector2(player.direction * -120, -80);
                if (CEUtils.getDistance(tpos, Projectile.Center) > 14) {
                    Projectile.velocity += (tpos - Projectile.Center) * 0.0048f;
                }
            }

        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Rectangle frame = CEUtils.GetCutTexRect(tex, 4, ((int)Main.GameUpdateCount / 4) % 4, false);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, new Vector2(tex.Width / 2, 42), Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            return false;
        }

        public Vector2 Shoot(NPC target) {
            if (target == null)
                return Vector2.Zero;
            Vector2 vel = (target.Center + target.velocity * 3 - Projectile.Center).normalize() * 17;
            if (Main.myPlayer == Projectile.owner) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, vel, ModContent.ProjectileType<MinionCinderFireball>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            return vel;
        }
    }
    public class MinionCinderFireball : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
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
            float scale = 0.5f;
            //CustomPulse贴图路径Configure现传,Texture属性填白图应付框架
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.OrangeRed * 0.95f, scale * 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White * 0.95f, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
            CEUtils.PlaySound("cinderExplosion", Main.rand.NextFloat(1.2f, 1.5f), Projectile.Center, 8, 0.36f);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.04f, 10);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 14);
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(255, 230, 60), 0.005f).Configure("CalamityEntropy/Assets/Particles/SoftRoundExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.06f, 18);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += 25;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
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
            //光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Projectile.Center - Projectile.velocity + CEUtils.randomPointInCircle(3), CEUtils.randomPointInCircle(3), Color.Firebrick, 0.4f).Configure(1f, 8, Main.rand.NextFloat(-0.1f, 0.1f), true);
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
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public int tofs;
    }
}
