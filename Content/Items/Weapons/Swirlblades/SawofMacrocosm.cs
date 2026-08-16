using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Particles;
using CalamityMod.Rarities;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class SawofMacrocosm : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.DamageType = CEUtils.RogueDC;
            Item.useAnimation = Item.useTime = 40;
            Item.width = 86;
            Item.height = 86;
            Item.damage = 225;
            Item.crit = 8;
            Item.ArmorPenetration = 30;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f };
            Item.value = CalamityGlobalItem.RarityDarkBlueBuyPrice;
            Item.rare = ModContent.RarityType<CosmicPurple>();
            Item.shoot = ModContent.ProjectileType<SawofMacrocosmProj>();
            Item.shootSpeed = 52f;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override float StealthDamageMultiplier => 0.5f;
        public override float StealthVelocityMultiplier => 1.25f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CEUtils.PlaySound("SwingMid", Main.rand.NextFloat(1.8f, 2.1f), position, 16, 0.44f);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (player.Calamity().StealthStrikeAvailable() && p.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[p].Calamity().stealthStrike = true;
                CEUtils.SyncProj(p);
            }
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<BlazingSwirlblade>())
                .AddIngredient(ModContent.ItemType<DimensionTearingDisk>())
                .AddIngredient(ModContent.ItemType<CosmiliteBar>(), 6)
                .AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 2)
                .AddTile<CosmicAnvil>()
                .Register();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class SawofMacrocosmProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<SawofMacrocosm>();
        public override int OldPosLength => 12;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
            Projectile.tileCollide = false;
        }
        public override float Radius => 180;
        public override int SpreadTime => Projectile.Calamity().stealthStrike ? 63 : 36;
        public override void AI()
        {
            base.AI();
            if (BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), false, Main.rand.Next(12, 16), Main.rand.NextFloat(0.6f, 1f) * 0.06f * BladeScale * Projectile.scale, (Main.rand.NextBool() ? Color.MediumPurple : new Color(80, 80, 255)) * BladeScale, new Vector2(0.22f, 1f), false, false));
            }
            CEUtils.AddLight(Projectile.Center, new Color(255, 100, 255));

            if (Main.GameUpdateCount % 4 == 0 && BladeScale > 0.4f)
            {
                EParticle.spawnNew(new ShineParticle(), Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 0.9f) * Radius * Projectile.scale * BladeScale, Projectile.velocity, new Color(230, 230, 255), Projectile.scale * BladeScale * Main.rand.NextFloat(0.6f, 1f), 1, true, BlendState.Additive, 0, 10);
            }
        }
        public override void FlyBack()
        {
            if (Projectile.Calamity().stealthStrike)
            {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 6000);
                if(target == null)
                {
                    Projectile.Calamity().stealthStrike = false;
                    return;
                }
                CEUtils.PlaySound("DoGLaserWallBigAttack", 1.2f, Projectile.Center);
                CalamityMod.Particles.Particle pulse = new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Purple, new Vector2(2f, 2f), 0, 0.1f, 0.85f, 46);
                GeneralParticleHandler.SpawnParticle(pulse);
                CalamityMod.Particles.Particle explosion2 = new DetailedExplosion(Projectile.Center, Vector2.Zero, Color.Purple, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, (Projectile.Calamity().stealthStrike ? 2.2f : 1) * 0.65f, 30);
                GeneralParticleHandler.SpawnParticle(explosion2);

                float scale = 7f;
                for (float i = 1; i >= 0.2f; i -= 0.2f)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 0, 160), new Color(170, 160, 255), i) * i, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f * i * (Radius / 180f), scale * 0.07f * i * (Radius / 180f), 12 + (int)(i * 8)));
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 0, 180), new Color(170, 160, 255), i) * i, "CalamityMod/Particles/FlameExplosion", Vector2.One, CEUtils.randomRot(), 0.0046f * i * (Radius / 180f), scale * 0.0625f * i * (Radius / 180f), 12 + (int)(i * 8)));
                }
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 46, ModContent.ProjectileType<MacrocosmSawDash>(), Projectile.damage * 8, Projectile.knockBack * 8, Projectile.owner, Radius);
                }
                ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 9), Projectile.Distance(Main.LocalPlayer.Center), 3600);
                Projectile.Kill();
            }
            if(!Projectile.Calamity().stealthStrike)
            {
                base.FlyBack();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for(int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++)
                {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = new Color(200, 140, 255) * 0.58f * p;
                    Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(0, 150, 160));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                float scale = Radius / 112f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);

                void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f)
                {
                    Main.spriteBatch.End();
                    Effect effect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Vortex", AssetRequestMode.ImmediateLoad).Value;
                    effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
                    effect.Parameters["Strength"].SetValue(19);
                    effect.Parameters["AspectRatio"].SetValue(1);
                    effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
                    float fadeOutDistance = 0.05f;
                    float fadeOutWidth = 0.3f;
                    effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
                    effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
                    effect.Parameters["enhanceLightAlpha"].SetValue(1f);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(CEUtils.getExtraTex("VoronoiShapes"), pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEUtils.getExtraTex("VoronoiShapes").Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
                    CEUtils.DrawGlow(pos, Color.White * glow * 0.6f, 1.4f * Size * glow);
                }
                Vector2 shakeOffset = CEUtils.randomPointInCircle(BladeScale * 3);
                Vector2 jpos = Projectile.Center + shakeOffset;
                DrawVortex(jpos, new Color(100, 100, 255), scale * 2.6f);
                DrawVortex(jpos, new Color(200, 100, 255), scale * 1.4f, 0.4f);

                Texture2D j1 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE1");
                Texture2D j2 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE2");
                Texture2D s = CEUtils.getExtraTex("SemiCircularSmear");
                Color c = Color.Lerp(Color.White, Color.Blue, (float)Math.Sin(time * 26f) * 0.5f + 0.5f);

                Main.EntitySpriteDraw(j1, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * 0.6f, j1.Size() / 2f, scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * 0.6f + MathHelper.Pi * 0.6f, s.Size() / 2f, scale * 1.4f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();

                c = Color.Lerp(Color.Blue, Color.White, (float)Math.Cos(time * 26f) * 0.5f + 0.5f);
                Main.EntitySpriteDraw(j2, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * -0.6f, j2.Size() / 2f, scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * -0.6f - MathHelper.Pi * 0.6f, s.Size() / 2f, scale * 0.84f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                float rt = CEUtils.randomRot();
                int sType = ModContent.ProjectileType<SawofMacrocosmSplit>();
                for (int i = 0; i < 4; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, (i * MathHelper.PiOver2 + rt).ToRotationVector2() * Projectile.scale * (Projectile.Calamity().stealthStrike ? 90 : 42), sType, Projectile.damage, 0, Projectile.owner, i % 2 == 0 ? 1 : 0, Projectile.Calamity().stealthStrike ? 1 : 0);
                }
            }
            CEUtils.PlaySound("DoGLaserWallSpawn", Main.rand.NextFloat(1f, 1.25f), Projectile.Center, volume: 1f);
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.75f, 1f), Projectile.Center);
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue, new Vector2(2.4f, 0.6f), true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<GodSlayerInferno>(300);
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.4f, 1.7f), target.Center, volume: 0.7f);
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1f, 1.3f), target.Center, volume: 1f);

            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, 0.04f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue, new Vector2(2.4f, 0.6f), true));
        }
    }
    public class SawofMacrocosmSplit : BaseSwirlblade
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 15;
            Projectile.tileCollide = false;
        }
        public override bool CollideWithNPC => false;
        public override float Radius => 140;
        public override int SpreadTime => 26;
        public override int FlyTime => 20;
        public Vector2 orgPos = Vector2.Zero;
        public override void FlyBack()
        {
            Projectile.velocity *= 0;
            if (TimeUtilSpread == 2)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MacrocosmVoidBolt>(), (int)(Projectile.damage * 0.66f), 4f, Projectile.owner, Projectile.ai[1], orgPos.X, orgPos.Y);
                }
            }
            if(TimeUtilSpread > 10)
                Projectile.Kill();
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
                orgPos = Projectile.Center;
            if (Projectile.ai[1] > 0)
                Projectile.Calamity().stealthStrike = true;
            base.AI();
            if(Counter < FlyTime)
            {
                Projectile.velocity *= 0.86f;
            }    
            if (BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + particleRot.ToRotationVector2() * Radius, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), false, Main.rand.Next(12, 16), Main.rand.NextFloat(0.6f, 1f) * 0.06f, (Main.rand.NextBool() ? Color.MediumPurple : new Color(80, 80, 255)) * BladeScale, new Vector2(0.22f, 1f), false, false));
            }
            CEUtils.AddLight(Projectile.Center, new Color(255, 100, 255));

            if (Main.GameUpdateCount % 4 == 0 && BladeScale > 0.4f)
            {
                EParticle.spawnNew(new ShineParticle(), Projectile.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 0.9f) * Radius * Projectile.scale * BladeScale, Projectile.velocity, new Color(230, 230, 255), Projectile.scale * BladeScale * Main.rand.NextFloat(0.6f, 1f), 1, true, BlendState.Additive, 0, 3);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.ai[0] == 0 ? Projectile.GetTexture() : this.getTextureAlt();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for (int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                if (Counter <= FlyTime)
                {
                    Main.spriteBatch.UseBlendState(BlendState.Additive);
                    for (int i = 0; i < posC.Count; i++)
                    {
                        float p = ((float)(1 + i) / posC.Count);
                        Color clr = new Color(200, 140, 255) * 0.58f * p;
                        Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                    }
                    Main.spriteBatch.ExitShaderRegion();
                }

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(0, 150, 160));
            }
            if(Counter <= FlyTime)
                Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, tex, Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                float scale = Radius / 112f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);

                void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f)
                {
                    Main.spriteBatch.End();
                    Effect effect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Vortex", AssetRequestMode.ImmediateLoad).Value;
                    effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
                    effect.Parameters["Strength"].SetValue(19);
                    effect.Parameters["AspectRatio"].SetValue(1);
                    effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
                    float fadeOutDistance = 0.05f;
                    float fadeOutWidth = 0.3f;
                    effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
                    effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
                    effect.Parameters["enhanceLightAlpha"].SetValue(1f);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(CEUtils.getExtraTex("VoronoiShapes"), pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEUtils.getExtraTex("VoronoiShapes").Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
                    CEUtils.DrawGlow(pos, Color.White * glow * 0.6f, 1.4f * Size * glow);
                }
                Vector2 shakeOffset = CEUtils.randomPointInCircle(BladeScale * 3);
                Vector2 jpos = Projectile.Center + shakeOffset;
                DrawVortex(jpos, new Color(100, 100, 255), scale * 2.6f);
                DrawVortex(jpos, new Color(200, 100, 255), scale * 1.4f, 0.4f);

                Texture2D j1 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE1");
                Texture2D j2 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE2");
                Texture2D s = CEUtils.getExtraTex("SemiCircularSmear");
                Color c = Color.Lerp(Color.White, Color.Blue, (float)Math.Sin(time * 26f) * 0.5f + 0.5f);

                Main.EntitySpriteDraw(j1, jpos - Main.screenPosition, null, Color.White * 0.8f, Main.GameUpdateCount * 0.6f, j1.Size() / 2f, scale, SpriteEffects.None);
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f, Main.GameUpdateCount * 0.6f + MathHelper.Pi * 0.6f, s.Size() / 2f, scale * 1.4f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
                Main.spriteBatch.ExitShaderRegion();
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(1.2f, 1.5f), Projectile.Center);
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue, new Vector2(2.4f, 0.6f), true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<GodSlayerInferno>(300);
            if (!target.boss)
            {
                target.velocity *= 0.94f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.4f, 1.7f), target.Center, volume: 0.7f);
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1f, 1.3f), target.Center, volume: 1f);

            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 4, false, 11, 0.028f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue, new Vector2(2f, 0.4f), true));
        }
    }
    public class MacrocosmVoidBolt : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.timeLeft = 180;
        }
        public List<Vector2> odp = new List<Vector2>();
        public Vector2 orgPos;
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
                orgPos = Projectile.Center;
            for (float i = 0.05f; i <= 1f; i += 0.05f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 180 + (Projectile.ai[0] > 0 ? 60 : 0))
                {
                    odp.RemoveAt(0);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.AddLight(Projectile.Center, Color.MediumSeaGreen, Projectile.scale);
            Projectile.Calamity().stealthStrike = Projectile.ai[0] > 0;
            Projectile.localAI[0]++;
            if (Projectile.ai[0] > 0)
            {
                Vector2 targetPos = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                int pt = 15;
                float pc = Projectile.localAI[0] / pt;
                if (pc > 1)
                    pc = 1;
                Vector2 nv = Vector2.Lerp(targetPos, orgPos, CEUtils.Parabola(0.5f + pc * 0.5f, 1));
                Projectile.velocity = (nv - Projectile.Center);
                if (Projectile.localAI[0] > 28)
                {
                    Projectile.Kill();
                }
            }
            else
            {
                Projectile.HomingToNPCNearby(12f, 0.8f, 2000);
            }
        }
        public override bool? CanDamage()
        {
            return Projectile.ai[0] == 0 ? null : false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }
        public override void OnKill(int timeLeft)
        {
            if(!Projectile.Calamity().stealthStrike)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/NPCKilled/DevourerDeathImpact") with { PitchRange = (0.7f, 1.1f), Volume = 0.4f }, Projectile.Center);
                float scale = 4f;
                float Radius = 180;
                for (float i = 1; i >= 0.2f; i -= 0.2f)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 0, 160), new Color(170, 160, 255), i) * i, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f * i * (Radius / 180f), scale * 0.07f * i * (Radius / 180f), 9 + (int)(i * 8)));
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 0, 180), new Color(170, 100, 255), i) * i, "CalamityMod/Particles/FlameExplosion", Vector2.One, CEUtils.randomRot(), 0.0046f * i * (Radius / 180f), scale * 0.05f * i * (Radius / 180f), 9 + (int)(i * 8)));
                }
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, scale * 54 * (Radius / 180f), Projectile.DamageType);
            }
        }
        public static void DrawBlackTrail(List<CEUtils.VertexPointSets> sets, Color a, Texture2D trail1, float innerWidth = 1)
        {
            if (sets.Count > 1)
            {
                List<CEUtils.VertexPointSets> sets1 = new List<CEUtils.VertexPointSets>();
                Vector2 lastPoint = Vector2.Zero;
                float cxOffset = 0;
                for (int i = 0; i < sets.Count; i++)
                {
                    var s = sets[i];

                    if (i > 0)
                        cxOffset += CEUtils.getDistance(lastPoint, s.Position) * 0.007f;
                    float opc = (s.Color.A / 255f);
                    sets1.Add(new CEUtils.VertexPointSets(s.Position, a * opc, s.Width * innerWidth, cxOffset + Main.GlobalTimeWrappedHourly * 6));
                }
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearWrap);

                List<ColoredVertex> lt;
                lt = sets1.GetVertexesList(false);
                gd.Textures[0] = trail1;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, lt.ToArray(), 0, lt.Count - 2);

                Main.spriteBatch.ExitShaderRegion();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for (int i = 0; i < odp.Count; i++)
            {
                float p = (i / (odp.Count - 1f));
                float alpha = p < 0.7f ? p / 0.7f : 1;
                float width = 1;
                if (p < 0.8f)
                    width = p / 0.8f;
                else
                    width = CEUtils.Parabola(0.5f + (p - 0.8f) / 0.4f, 1);
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 30 * Projectile.scale * width * (Projectile.Calamity().stealthStrike ? 1.8f : 1), 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(220, 180, 255), new Color(80, 60, 255));
            if (vp.Count > 6)
            {
                vp = new();
                for (int i = 0; i < odp.Count - 6; i++)
                {
                    float p = (i / (odp.Count - 6f - 1f));
                    float alpha = p < 0.7f ? p / 0.7f : 1;
                    float width = 1;
                    if (p < 0.8f)
                        width = p / 0.8f;
                    else
                        width = CEUtils.Parabola(0.5f + (p - 0.8f) / 0.4f, 1);
                    vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 30 * Projectile.scale * width * (Projectile.Calamity().stealthStrike ? 1.8f : 1), 0));
                }
                DrawBlackTrail(vp, Color.Black, CEUtils.getExtraTex("MegaStreakBacking2b"), 0.3f);
            }
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
    public class MacrocosmSawDash : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.localNPCHitCooldown = 16;
            Projectile.MaxUpdates = 3;
        }
        public override bool ShouldUpdatePosition()
        {
            return NoPosUpdate <= 0;
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                SoundStyle ShootSound = new("CalamityMod/Sounds/Item/SawShot", 2) { PitchVariance = 0f, Volume = 1 };
                SoundEngine.PlaySound(ShootSound, Projectile.Center);

                for (int i = 0; i < 32; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.3f, 1f) * 4;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(1f) * Main.rand.NextFloat(0.5f, 1) * 40;
                    dust.noGravity = false;
                    dust.color = Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue;
                    dust.fadeIn = 2f;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                int dir = Main.rand.NextBool() ? 1 : -1;
                Vector2 offset = new Vector2(Radius * -0.9f, Radius * dir * 0.42f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset.RotatedBy(Projectile.velocity.ToRotation()), ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.8f, 1f) * 3.2f;
                dust.velocity = Projectile.velocity.RotatedBy(dir * 0.16f) * -0.8f;
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue;
                dust.fadeIn = 2f;
            }
            if (NoPosUpdate > 0)
            {
                NoPosUpdate--;
            }
            else if (CD > 0)
            {
                CD--;
            }
            if (Projectile.timeLeft < 20)
                Projectile.Opacity -= 1 / 20f;
            for (float i = 0.25f; i <= 1f; i += 0.25f)
            {
                oldPos.Add(Projectile.Center + Projectile.velocity * i);
                if(oldPos.Count > 60)
                {
                    oldPos.RemoveAt(0);
                }
            }

        }
        public int NoPosUpdate = 0;
        public int CD = 0;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CD <= 0)
            {
                NoPosUpdate = 24;
                CD = 30;

                ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.NoDirQuickShake(24), Projectile.Distance(Main.LocalPlayer.Center), 3600);

                for (int i = 0; i < 6; i++)
                {
                    float rot = 2;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16), false, 16, Projectile.scale * 0.04f, Color.LightBlue, new Vector2(0.3f, 1), false, false));
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16), false, 16, Projectile.scale * 0.04f, Color.LightBlue, new Vector2(0.3f, 1), false, false));
                }
            }
            target.AddBuff<GodSlayerInferno>(300);
            if (!target.boss)
            {
                target.velocity *= 0.6f;
            }
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, 0.04f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.MediumPurple : Color.LightBlue, new Vector2(2.4f, 0.6f), true));
            SoundEngine.PlaySound(new("CalamityMod/Sounds/NPCKilled/DevourerSegmentBreak1") { Volume = 0.6f, PitchRange = (0.4f, 0.7f) });
            CalamityMod.Particles.Particle pulse = new DirectionalPulseRing(target.Center, Vector2.Zero, Color.Purple, new Vector2(2f, 2f), 0, 0.1f, 1.2f, 46);
            GeneralParticleHandler.SpawnParticle(pulse);
            CalamityMod.Particles.Particle explosion2 = new DetailedExplosion(target.Center, Vector2.Zero, Color.Purple, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, 1.6f * 0.65f, 40);
            GeneralParticleHandler.SpawnParticle(explosion2);
            float scale = 4f;
            for (float i = 1; i >= 0.2f; i -= 0.2f)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(target.Center, Vector2.Zero, Color.Lerp(new Color(100, 0, 160), new Color(170, 160, 255), i) * i, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f * i * (Radius / 180f), scale * 0.07f * i * (Radius / 180f), 16 + (int)(i * 20)));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(target.Center, Vector2.Zero, Color.Lerp(new Color(100, 0, 180), new Color(170, 160, 255), i) * i, "CalamityMod/Particles/FlameExplosion", Vector2.One, CEUtils.randomRot(), 0.0046f * i * (Radius / 180f), scale * 0.0625f * i * (Radius / 180f), 16 + (int)(i * 20)));
            }
            for (int i = 0; i < 32; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(3f, 4.6f);
                dust.velocity = (new Vector2(46, 46).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                dust.noGravity = true;
                dust.color = Color.Lerp(Color.LightBlue, Color.MediumPurple, Main.rand.NextFloat());
                dust.fadeIn = 2f;
            }
        }

        public float BladeScale => 1;
        public float Radius => Projectile.ai[0];
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(projHitbox.Center.ToVector2(), Radius * Projectile.scale * BladeScale).Intersects(targetHitbox);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float scale = Radius / 112f * Projectile.scale * BladeScale;
            float time = Main.GlobalTimeWrappedHourly;
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);

            void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f)
            {
                Main.spriteBatch.End();
                Effect effect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Vortex", AssetRequestMode.ImmediateLoad).Value;
                effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
                effect.Parameters["Strength"].SetValue(22);
                effect.Parameters["AspectRatio"].SetValue(1);
                effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
                float fadeOutDistance = 0.06f;
                float fadeOutWidth = 0.3f;
                effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
                effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
                effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(CEUtils.getExtraTex("VoronoiShapes"), pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEUtils.getExtraTex("VoronoiShapes").Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
                CEUtils.DrawGlow(pos, Color.White * glow * 0.6f, 1.4f * Size * glow);
            }
            Vector2 shakeOffset = CEUtils.randomPointInCircle(BladeScale * 3);
            Vector2 jpos = Projectile.Center + shakeOffset;
            DrawVortex(jpos, new Color(100, 100, 255) * Projectile.Opacity, scale * 2.6f);
            DrawVortex(jpos, new Color(200, 100, 255) * Projectile.Opacity, scale * 1.4f, 0.4f * Projectile.Opacity);

            Texture2D j1 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE1");
            Texture2D j2 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/OblivionThresher/OblivionThresherShootE2");
            Texture2D s = CEUtils.getExtraTex("SemiCircularSmear");
            Color c = Color.Lerp(Color.White, Color.Blue, (float)Math.Sin(time * 26f) * 0.5f + 0.5f);

            Main.EntitySpriteDraw(j1, jpos - Main.screenPosition, null, Color.White * 0.8f * Projectile.Opacity, Main.GameUpdateCount * 0.6f, j1.Size() / 2f, scale, SpriteEffects.None);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f * Projectile.Opacity, Main.GameUpdateCount * 0.6f + MathHelper.Pi * 0.6f, s.Size() / 2f, scale * 1.4f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            c = Color.Lerp(Color.Blue, Color.White, (float)Math.Cos(time * 26f) * 0.5f + 0.5f);
            Main.EntitySpriteDraw(j2, jpos - Main.screenPosition, null, Color.White * 0.8f * Projectile.Opacity, Main.GameUpdateCount * -0.6f, j2.Size() / 2f, scale, SpriteEffects.None);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(s, jpos - Main.screenPosition, null, c * 0.8f * Projectile.Opacity, Main.GameUpdateCount * -0.6f - MathHelper.Pi * 0.6f, s.Size() / 2f, scale * 0.84f, SpriteEffects.None);
            float jscale = scale * 1.4f;
            Texture2D jaws = CEUtils.RequestTex("CalamityMod/Particles/Jaws");
            
            for(int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;

                Main.spriteBatch.Draw(jaws, oldPos[i] - Main.screenPosition, null, new Color(120, 120, 255) * Projectile.Opacity * p * 0.3f, Projectile.velocity.ToRotation() + MathHelper.PiOver2, jaws.Size() * 0.5f, jscale * p, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(jaws, Projectile.Center - Main.screenPosition, null, new Color(120, 120, 255) * Projectile.Opacity, Projectile.velocity.ToRotation() + MathHelper.PiOver2, jaws.Size() * 0.5f, jscale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(jaws, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity, Projectile.velocity.ToRotation() + MathHelper.PiOver2, jaws.Size() * 0.5f, jscale * 0.85f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return Projectile.Opacity > 0.6f ? null : false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
