using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Particles;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Items;
using CalamityMod.Items.LoreItems;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class Fooveria : ModItem, IDonatorItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
            Item.width = 48;
            Item.height = 60;
            Item.useTime = 24;
            Item.crit = 12;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2;
            Item.value = CalamityGlobalItem.RarityGreenBuyPrice;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FooveriaHeld>();
            Item.shootSpeed = 12f;
            Item.Entropy().Legend = true;
            Item.Calamity().CannotBeEnchanted = true;
            LastLevel = -1;
            UpdateInventory(Main.LocalPlayer);
        }
        public int useCounter = 0;
        public int atkType = 1;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            damage *= useCounter % 4 == 3 ? 2 : 1;
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0, useCounter % 4 == 3 ? 1 : Main.rand.NextBool() ? 1 : -1, useCounter % 4 == 3 ? 1 : 0);
            atkType *= -1;
            useCounter++;
            return false;
        }
        public override bool MeleePrefix()
        {
            return true;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item.QuickDrawItemWithBloomToWorld(spriteBatch, Color.SkyBlue, ref scale, rotation);
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Shiverthorn, 3)
                .AddIngredient(ItemID.IceBlock, 20)
                .AddRecipeGroup(RecipeGroupID.Fruit)
                .AddTile(TileID.Anvils)
                .Register();
        }
        public static int GetLevel()
        {
            int Level = 0;
            bool flag = true;
            void Check(bool f)
            {
                if (f && flag)
                {
                    Level++;
                }
                else
                {
                    flag = false;
                }
            }

            Check(NPC.downedSlimeKing);
            Check(NPC.downedBoss1);
            Check(DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator);
            Check(DownedBossSystem.downedSlimeGod);
            Check(Main.hardMode);
            Check(NPC.downedMechBossAny);
            Check(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3);
            Check(NPC.downedPlantBoss);
            Check(NPC.downedGolemBoss);
            Check(NPC.downedMoonlord);
            Check(DownedBossSystem.downedProvidence);
            Check(DownedBossSystem.downedDoG);
            Check(DownedBossSystem.downedYharon);
            Check(DownedBossSystem.downedExoMechs || DownedBossSystem.downedCalamitas);
            Check(DownedBossSystem.downedCalamitas && DownedBossSystem.downedExoMechs);

            return Level;

        }
        public int LastLevel = -1;

        public string DonatorName => "普维莉雅";

        public override void UpdateInventory(Player player)
        {
            int level = GetLevel();
            if (LastLevel != level)
            {
                int dmg = Item.damage;
                switch (level)
                {
                    case 0: dmg = 5; break;
                    case 1: dmg = 10; break;
                    case 2: dmg = 13; break;
                    case 3: dmg = 18; break;
                    case 4: dmg = 25; break;
                    case 5: dmg = 42; break;
                    case 6: dmg = 56; break;
                    case 7: dmg = 75; break;
                    case 8: dmg = 95; break;
                    case 9: dmg = 115; break;
                    case 10: dmg = 300; break;
                    case 11: dmg = 460; break;
                    case 12: dmg = 950; break;
                    case 13: dmg = 1200; break;
                    case 14: dmg = 1350; break;
                    case 15: dmg = 2300; break;
                }
                Item.damage = dmg;
                Item.useTime = Item.useAnimation = int.Max(10, 16 - GetLevel() / 4);
                LastLevel = level;
                Item.crit = level * 1;
                Item.knockBack = level / 3;
                Item.scale = 1;
                Item.Prefix(Item.prefix);
            }
            if (player.HeldItem == Item)
            {
                player.Entropy().BBarNoDecrease = 120;
                player.Calamity().mouseWorldListener = true;
            }
        }
        public override bool AllowPrefix(int pre)
        {
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[L]", GetLevel());
        }
    }
    public class FooveriaHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/Fooveria";
        List<float> odr = new List<float>();
        List<float> ods = new List<float>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = ModContent.GetInstance<TrueMeleeDamageClass>();
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = 16;
        }
        public float rotRP = 0;
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public bool RightHold = true;
        public bool LeftClicked = false;
        public bool Spin = false;
        public bool snd = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (snd)
            {
                CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), Main.rand.NextFloat(1.4f, 1.7f), Projectile.Center);
                if (target.Organic())
                {
                }
                else
                {
                    CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.8f, 1.2f), target.Center, 6, CEUtils.WeapSound * 0.5f);
                }
                CEUtils.PlaySound("GrassSwordHit" + Main.rand.Next(4).ToString(), 1, target.Center, 16, CEUtils.WeapSound * 0.5f);
                snd = false;
            }
            Color impactColor = Color.LightBlue;
            float impactParticleScale = Main.rand.NextFloat(1.4f, 1.6f);

            SparkleParticle impactParticle = new SparkleParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, Color.SkyBlue, impactParticleScale, 8, 0, 2.5f);
            GeneralParticleHandler.SpawnParticle(impactParticle);

            float sparkCount = 16 + Fooveria.GetLevel() / 2;
            for (int i = 0; i < sparkCount; i++)
            {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 20 * (2 - p)) * (1 + Fooveria.GetLevel() * 0.1f) * 0.7f;
                int sparkLifetime2 = (int)((2 - p) * 16);
                float sparkScale2 = 0.6f + (1 - p);
                sparkScale2 *= (1 + Fooveria.GetLevel() * 0.06f);
                Color sparkColor2 = Color.Lerp(Color.LightSkyBlue, Color.AliceBlue, p);
                if (Main.rand.NextBool())
                {
                    AltSparkParticle spark = new AltSparkParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), false, (int)(sparkLifetime2 * (1.2f)), sparkScale2 * (1.4f), sparkColor2);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
                else
                {
                    LineParticle spark = new LineParticle(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2, false, (int)(sparkLifetime2), sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f), Main.rand.NextBool() ? Color.LightBlue : Color.SkyBlue);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }
            if (Projectile.ai[2] > 0)
            {
                CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 16);
                CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), Main.rand.NextFloat(1f, 1.2f), Projectile.Center, 16);
                Projectile.GetOwner().Entropy().noItemTime = (int)(30f / Projectile.GetOwner().GetTotalAttackSpeed<TrueMeleeDamageClass>());
                int pt = ModContent.ProjectileType<FooveriaIceShard>();
                for (int i = 0; i < 6; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(0, 100) * Projectile.scale, Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(16, 26) + CEUtils.randomPointInCircle(4), pt, Projectile.damage / 2, 4, Projectile.owner);
                }
                Projectile.Kill();
            }
        }
        public float rScale = 1;
        public float slashP = Main.rand.NextFloat(0.2f, 0.3f);
        public override void AI()
        {
            CEUtils.AddLight(Projectile.Center + Projectile.velocity.normalize() * 20 * Projectile.scale, Color.LightBlue, Projectile.scale);
            Player owner = Projectile.GetOwner();
            if (owner.dead)
            {
                Projectile.Kill();
                return;
            }
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            if (Projectile.ai[2] == 1)
            {
                MaxUpdateTimes *= 2;
                slashP = 1;
            }
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init)
            {
                Projectile.ai[1] *= Projectile.velocity.X > 0 ? 1 : -1;

                CEUtils.PlaySound("HellkiteSwing" + Main.rand.Next(1, 3), Main.rand.NextFloat(0f, 0.5f) + (Projectile.ai[2] == 1 ? 2f : 2.4f), Projectile.Center, 12, 0.66f * CEUtils.WeapSound);
                Projectile.scale = 1f + 0.05f * Fooveria.GetLevel();
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;
                if (Main.myPlayer == Projectile.owner)
                {

                }
            }
            float p = Main.rand.NextFloat();
            rScale = 1;
            Projectile.timeLeft = 3;
            alpha = 1;
            float cr = MathHelper.ToRadians(10);
            float RotF = 4.5f; 
            float rot = progress <= 0.5f ? ((RotF * -0.5f + CEUtils.Parabola(progress, RotF + cr)) * Projectile.ai[1]) : ((RotF * 0.5f + cr - CEUtils.GetRepeatedCosFromZeroToOne(2 * (progress - 0.5f), 1) * cr) * Projectile.ai[1]);
            Vector2 v = rot.ToRotationVector2() * new Vector2(1, slashP);
            Projectile.rotation = v.ToRotation() + Projectile.velocity.ToRotation();
            Projectile.Center = Projectile.GetOwner().GetDrawCenter();
            rScale = v.Length();
            scale = 1.4f;

            if (Projectile.velocity.X > 0)
            {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            if (counter > MaxUpdateTimes)
            {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
            if (progress < 0.45f)
            {
                SpawnParticle();
                odr.Add(Projectile.rotation);
                ods.Add(rScale);
                if (odr.Count > 120)
                {
                    odr.RemoveAt(0);
                    ods.RemoveAt(0);
                }
            }
            else
            {
                if (odr.Count > 0)
                {
                    ods.RemoveAt(0);
                    odr.RemoveAt(0);
                }
            }
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public bool NoDraw = false;
        public Vector2 lPos = Vector2.Zero;
        public void SpawnParticle()
        {
            Vector2 vpos = Projectile.rotation.ToRotationVector2() * rScale * scale * Projectile.scale * 116;
            if (lPos == Vector2.Zero)
            {
                lPos = vpos;
                return;
            }

            Vector2 sparkVelocity2 = (vpos - lPos).normalize() * 4 * Projectile.ai[1];
            int sparkLifetime2 = (int)(Main.rand.NextFloat() * 16);
            float sparkScale2 = 0.6f * Main.rand.NextFloat();
            sparkScale2 *= (1 + Fooveria.GetLevel() * 0.06f);
            Color sparkColor2 = Color.Lerp(Color.AliceBlue, Color.LightBlue, Main.rand.NextFloat());
            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 116 * scale * Projectile.scale * rScale;
            if (Main.rand.NextBool())
            {
                AltLineParticle spark = new AltLineParticle(pos, sparkVelocity2 * (1f), false, (int)(sparkLifetime2 * (1.2f)), sparkScale2 * (1.4f), sparkColor2);
                GeneralParticleHandler.SpawnParticle(spark);
            }
            else
            {
                LineParticle spark = new LineParticle(pos, sparkVelocity2, false, (int)(sparkLifetime2), sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f), Main.rand.NextBool() ? Color.LightBlue : Color.SkyBlue);
                GeneralParticleHandler.SpawnParticle(spark);
            }
            EParticle.spawnNew(new Snowflake(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 100 * scale * Projectile.scale * rScale * Main.rand.NextFloat(Main.rand.NextFloat(0.25f, 1f), 1f), sparkVelocity2 * 0.2f, Color.White, Main.rand.NextFloat(0.16f, 0.36f) * scale * Projectile.scale, 1, true, BlendState.Additive, CEUtils.randomRot(), 8);
            lPos = vpos;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (NoDraw)
            {
                return false;
            }
            Texture2D tex = Projectile.GetTexture();
            Texture2D trail = CEUtils.getExtraTex("MotionTrail2");
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(220, 255, 200);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(116 * Projectile.scale * scale * ods[i], 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(32 * Projectile.scale * scale * ods[i], 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/SwordTrail", AssetRequestMode.ImmediateLoad).Value;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(90, 120, 255)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(60, 200, 255)).ToVector4());
                shader.Parameters["alpha"].SetValue(1 - progress);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                trail = CEUtils.getExtraTex("SplitTrail");
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                Main.spriteBatch.ExitShaderRegion();
            }


            int dir = (int)(Projectile.ai[1]);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            Main.spriteBatch.UseAdditiveClamp();
            if (Projectile.ai[2] > 0)
            {
                for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
                {
                    Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition + i.ToRotationVector2() * 4, null, lightColor * alpha, rot, origin, Projectile.scale * scale * rScale * 0.6f, effect);
                }
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * rScale * 0.6f, effect);
            Main.spriteBatch.UseAdditiveClamp();
            if (Projectile.ai[2] > 0)
            {
                Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.White * alpha, rot, origin, Projectile.scale * scale * rScale * 0.6f, effect);
                Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.White * alpha, rot, origin, Projectile.scale * scale * rScale * 0.6f, effect);
            }
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return null;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale * scale * rScale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale * scale * rScale, 54, DelegateMethods.CutTiles);
        }
    }
    public class FooveriaIceShard : ModProjectile
    {
        public List<Vector2> odp = new List<Vector2>();
        public List<float> odr = new List<float>();
        public float alpha = 1;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void PostAI()
        {
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 7)
            {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public Color TrailColor(float completionRatio, Vector2 vertex)
        {
            Color result = new Color(220, 235, 255) * completionRatio * alpha;
            return result;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex)
        {
            return MathHelper.Lerp(0, 12 * Projectile.scale, completionRatio);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), 1, Projectile.Center);

            float scale = 60 / 40f;
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.LightBlue, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.05f, 16));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.LightBlue, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f, scale * 0.035f, 13));

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.timeLeft = 120;
        }
        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (Projectile.localAI[0]++ < 50)
            {
                Projectile.velocity *= 0.96f;
            }
            else
            {
                if (Projectile.HomingToNPCNearby(4, 0.94f, 2000))
                    if (Projectile.timeLeft < 60)
                        Projectile.timeLeft = 60;
            }
            alpha = float.Min(1, Projectile.timeLeft / 20f);
        }
        public override bool? CanDamage()
        {
            return Projectile.localAI[0] >= 50;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Color color = new Color(160, 180, 255) * alpha;
            var mp = this;
            if (mp.odp.Count > 1)
            {
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
                for (int i = 1; i < mp.odp.Count; i++)
                {
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
                if (ve.Count >= 3)
                {
                    Texture2D tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/wohslash").Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                    ve = new List<ColoredVertex>();
                    b = color;

                    a = 0;
                    lr = 0;
                    for (int i = 1; i < mp.odp.Count; i++)
                    {
                        a += 1f / (float)mp.odp.Count;

                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 1, 1),
                            b * a));
                        ve.Add(new ColoredVertex(mp.odp[i] - Main.screenPosition + (mp.odp[i] - mp.odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 8 * Projectile.scale,
                              new Vector3((float)(i + 1) / mp.odp.Count, 0, 1),
                              b * a));
                        lr = (mp.odp[i] - mp.odp[i - 1]).ToRotation();
                    }
                    tx = ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/wohslash").Value;
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            tofs++;

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:ArtAttack"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityEntropy/Assets/Extra/Streak1"));
            GameShaders.Misc["CalamityMod:ArtAttack"].Apply();
            PrimitiveRenderer.RenderTrail(odp, new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityMod:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            if (odp.Count > 1)
            {
                Texture2D texture = Projectile.GetTexture();
                Rectangle frame = CEUtils.GetCutTexRect(texture, 4, Projectile.whoAmI % 4, false);
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
