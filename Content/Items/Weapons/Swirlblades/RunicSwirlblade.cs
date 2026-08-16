using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class RunicSwirlblade : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.DamageType = CEUtils.RogueDC;
            Item.useAnimation = Item.useTime = 30;
            Item.width = 50;
            Item.height = 58;
            Item.damage = 50;
            Item.crit = 5;
            Item.ArmorPenetration = 15;
            Item.UseSound = SoundID.Item1 with { Volume = 1f };
            Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<RunicSwirlbladeProj>();
            Item.shootSpeed = 58f;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override float StealthDamageMultiplier => 0.6f;
        public override float StealthVelocityMultiplier => 1.2f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
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
                .AddIngredient(ModContent.ItemType<GlacierSwirlblade>())
                .AddIngredient(ModContent.ItemType<SamsaraSlicer>())
                .AddIngredient(ItemID.Ectoplasm, 6)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class RunicSwirlbladeProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<RunicSwirlblade>(); public override int OldPosLength => 10;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 4;
            Projectile.tileCollide = false;
            Projectile.light = 1;
        }
        public override float Radius => 170 * (Projectile.Calamity().stealthStrike ? 1.2f : 1);
        public override int SpreadTime => Projectile.Calamity().stealthStrike ? 50 : 13;
        public float ExtraRadius = 0;
        public override void AI()
        {
            base.AI();
            if (BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), false, Main.rand.Next(12, 16), Main.rand.NextFloat(0.6f, 1f) * 0.04f * BladeScale * Projectile.scale, (Main.rand.NextBool() ? new Color(200, 220, 255) : Color.LightBlue) * BladeScale, new Vector2(0.18f, 1f), false, false));
            }
            if (Projectile.Calamity().stealthStrike)
            {
                if (Spreaded)
                {
                    ExtraRadius = float.Lerp(ExtraRadius, 1, 0.08f);
                    Projectile.localAI[1] += 1f / (24 * (Counter - FlyTime)) * 80f;
                }
                else
                {
                    ExtraRadius *= 0.86f;
                }
            }
        }
        public static float ExtraRadMul = 1.3f;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(projHitbox.Center.ToVector2(), float.Max(Radius * BladeScale, Radius * ExtraRadMul * ExtraRadius) * Projectile.scale).Intersects(targetHitbox);
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
                    Color clr = Color.Aqua * 0.58f * p;
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
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(190, 200, 255));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            Texture2D smear = CEUtils.getExtraTex("CircularSmear");
            if (BladeScale > 0)
            {
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                BaseSwirlblade.ApplyShader(new Color(230, 240, 255));
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 125, 255) * Projectile.Opacity * BladeScale, time * -34f, o, scale * 1.6f * ExtraRadius, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 125, 255) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 125, 255) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 0.98f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 125, 155) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 0.96f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 120, 155) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.94f, SpriteEffects.None, 0);
            }
            if(ExtraRadius > 0.01f)
            {
                float eRad = Radius * ExtraRadMul;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, CommonEffects.rotation, Main.GameViewMatrix.TransformationMatrix);
                CommonEffects.rotation.CurrentTechnique.Passes[0].Apply();
                CommonEffects.rotation.Parameters["center"].SetValue(Vector2.One * 0.5f);
                CommonEffects.rotation.Parameters["rad"].SetValue(Main.GlobalTimeWrappedHourly * 7);
                Texture2D cTex = CEUtils.getExtraTex("Ray");
                for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
                {
                    float rt = i + Projectile.localAI[1];
                    Main.spriteBatch.Draw(cTex, Projectile.Center + rt.ToRotationVector2() * ExtraRadius * eRad * Projectile.scale * 0.7f - Main.screenPosition, null, new Color(200, 220, 255) * Projectile.Opacity * ExtraRadius, rt + MathHelper.Pi, cTex.Size() * 0.5f, new Vector2(0.38f, 1) * ExtraRadius * Radius * 0.9f / 78f * Projectile.scale * 0.7f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(cTex, Projectile.Center + rt.ToRotationVector2() * ExtraRadius * eRad * Projectile.scale - Main.screenPosition, null, new Color(200, 220, 255) * Projectile.Opacity * ExtraRadius, rt, cTex.Size() * 0.5f, new Vector2(0.38f, 1) * ExtraRadius * Radius * 0.9f / 78f * Projectile.scale, SpriteEffects.None, 0);
                }

                Texture2D pulse = CEUtils.getExtraTex("HighResFoggyCircleHardEdge");
                Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, new Color(136, 160, 200) * Projectile.Opacity * ExtraRadius, Projectile.rotation, pulse.Size().Half(), ExtraRadius * Radius * 0.0016f * Projectile.scale * 0.7f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, new Color(136, 160, 200) * Projectile.Opacity * ExtraRadius, Projectile.rotation, pulse.Size().Half(), ExtraRadius * Radius * 0.0016f * Projectile.scale, SpriteEffects.None, 0);

                Main.spriteBatch.ExitShaderRegion();
            }
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("soulshine", Main.rand.NextFloat(0.6f, 0.8f), Projectile.Center);
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.75f, 1f), Projectile.Center);
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? new Color(200, 220, 255) : Color.LightBlue, new Vector2(2.4f, 0.6f), true));
            int type = ModContent.ProjectileType<RunicSwirlbladeBullet>();
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 8; i++)
                {
                    float rt = Main.rand.Next(0, 4) * MathHelper.PiOver2;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + rt.ToRotationVector2() * Projectile.scale, rt.ToRotationVector2() * 9, type, Projectile.damage / 4, 6, Projectile.owner);
                }
            }
        }

        public override void OnRetract()
        {
            if (Projectile.Calamity().stealthStrike)
            {
                CEUtils.PlaySound("soulshine", Main.rand.NextFloat(0.3f, 0.36f), Projectile.Center);
                CEUtils.PlaySound("soulshine", Main.rand.NextFloat(1.85f, 2.2f), Projectile.Center);
                CEUtils.PlaySound("CruiserDash", 1.2f, Projectile.Center);
                int type = ModContent.ProjectileType<RunicSwirlbladeBullet>();
                if (Main.myPlayer == Projectile.owner)
                {
                    for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
                    {
                        float rt = i + Projectile.localAI[1];
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + rt.ToRotationVector2() * Radius * ExtraRadius * ExtraRadMul * 0.52f, rt.ToRotationVector2() * 10, type, Projectile.damage / 3, 6, Projectile.owner, 0, 0, 1);
                    }
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<SoulDisorder>(180);
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.6f, 1.9f), target.Center, volume: 1f);

            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 13, 0.06f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? new Color(200, 220, 255) : Color.LightBlue, new Vector2(2.4f, 0.6f), true));
        }
    }
    public class RunicSwirlbladeBullet : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = CEUtils.RogueDC;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 16 * 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.MaxUpdates = 4;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[2] > 0 && PreCounter < MovingTime * 4)
                return false;
            if (PreCounter < 20)
                return false;
            return Projectile.position.getRectCentered(20 * Projectile.scale, 20 * Projectile.scale).Intersects(targetHitbox);
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public int PreCounter = 0;
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.ai[0] = Main.rand.NextFloat(100);
                if (Projectile.ai[2] > 0)
                {
                    Projectile.Calamity().stealthStrike = true;
                    Projectile.scale *= 2f;
                }
                if (Projectile.Calamity().stealthStrike)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity, ModContent.DustType<SquashDust>(), Vector2.Zero);
                        dust.scale = Main.rand.NextFloat(0.2f, 1f) * 3f;
                        dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.22f) * Main.rand.NextFloat(0.5f, 1) * 50;
                        dust.noGravity = false;
                        dust.color = Main.rand.NextBool() ? Color.AliceBlue : Color.LightSkyBlue;
                        dust.fadeIn = 2f;
                    }
                }
            }
            PreCounter++;
            if(PreCounter == 1 + MovingTime * 4)
            {
                if (!Projectile.Calamity().stealthStrike) 
                {
                    NPC target = Projectile.FindMinionTarget(3600);
                    if (target != null)
                    {
                        Projectile.velocity = (target.Center - Projectile.Center).normalize() * 10;
                    } 
                }
            }
            if (Projectile.Calamity().stealthStrike || PreCounter > MovingTime * 4)
            {
                NPC target = Projectile.FindMinionTarget(1600);
                if (Projectile.localAI[0]++ > 28 && target != null)
                {
                    Projectile.velocity *= 0.97f;
                    Vector2 v = target.Center - Projectile.position;
                    v.Normalize();

                    Projectile.velocity += v * 0.5f;
                }
                Vector2 adv = Projectile.velocity;
                Projectile.rotation = adv.ToRotation();
                Projectile.position += adv;
            }
            else
            {
                if (Projectile.localAI[1]++ % 6 == 0 && Main.rand.NextBool(3))
                {
                    float r = MathHelper.PiOver4 * Main.rand.Next(-1, 2);
                    Projectile.velocity = Projectile.velocity.RotatedBy(r);
                }
                Vector2 adv = Projectile.velocity * 0.6f;
                Projectile.rotation = adv.ToRotation();
                Projectile.position += adv;
                if(Main.rand.NextBool(3))
                    EParticle.spawnNew(new RuneParticle(), Projectile.Center, Vector2.Zero, new Color(200, 230, 255), Main.rand.NextFloat(0.4f, 0.6f) * Projectile.scale, 1, true, BlendState.Additive, 0, 46);
            }

            oldPos.Add(Projectile.Center);
            oldRots.Add(Projectile.rotation);
            if (oldPos.Count > 46)
            {
                oldPos.RemoveAt(0);
                oldRots.RemoveAt(0);
            }
        }
        public int MovingTime = Main.rand.Next(12, 20);
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<SoulDisorder>(180);
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(2f, 2.4f) * Projectile.scale;
                dust.velocity = new Vector2(20, 0).RotatedBy(CEUtils.randomRot()) * Main.rand.NextFloat(0.3f, 1f) * Projectile.scale;
                dust.noGravity = false;
                dust.color = new Color(150, 190, 255);
                dust.fadeIn = 2f;
            }
            float r = CEUtils.randomRot();
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.6f, 1.9f), target.Center, 12, 0.4f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
            float alpha = 1;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<ColoredVertex> ve2 = new List<ColoredVertex>();
            float trailOffset = Main.GlobalTimeWrappedHourly * 6;
            for (int i = 0; i < oldPos.Count; i++)
            {
                alpha = i / (oldPos.Count - 1f);
                Vector2 m = oldPos[i];
                Vector2 l = oldRots[i].ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                ve.Add(new ColoredVertex(m + l * alpha * 15 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset, 1, 1),
                      new Color(150, 190, 255) * alpha));
                ve.Add(new ColoredVertex(m - l * alpha * 15 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset, 0, 1),
                      new Color(150, 190, 255) * alpha));

                ve2.Add(new ColoredVertex(m + l * alpha * 10 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset * 1.6f, 1, 1),
                      new Color(235, 240, 255) * alpha));
                ve2.Add(new ColoredVertex(m - l * alpha * 10 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset * 1.6f, 0, 1),
                      new Color(235, 240, 255) * alpha));
            }
            if (ve.Count >= 3)
            {
                Texture2D tx = CEUtils.getExtraTex("DeathRay");
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.Textures[0] = CEUtils.getExtraTex("Streak2");
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
            }

            Texture2D glow = CEUtils.getExtraTex("Glow2");
            Texture2D tex = CEUtils.getExtraTex("Ray");

            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, new Color(60, 60, 255), Projectile.rotation, glow.Size().Half(), 0.34f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, new Color(230, 230, 255), Projectile.rotation, glow.Size().Half(), 0.18f * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(200, 255, 255), Projectile.rotation, tex.Size().Half(), new Vector2(2, 1.5f) * 0.16f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 255), Projectile.rotation, tex.Size().Half(), new Vector2(2, 1.5f) * 0.1f * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
