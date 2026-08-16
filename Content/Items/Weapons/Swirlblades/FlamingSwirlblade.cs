using AlchemistNPCLite.Items;
using CalamityEntropy.Content.Items.Books;
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
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class FlamingSwirlblade : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.DamageType = CEUtils.RogueDC;
            Item.useAnimation = Item.useTime = 26;
            Item.width = 48;
            Item.height = 52;
            Item.damage = 8;
            Item.crit = 2;
            Item.ArmorPenetration = 8;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f };
            Item.value = CalamityGlobalItem.RarityLightRedBuyPrice;
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<FlamingSwirlbladeProj>();
            Item.shootSpeed = 45f;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override float StealthDamageMultiplier => 1.0f;
        public override float StealthVelocityMultiplier => 1.4f;

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
                .AddIngredient(ModContent.ItemType<BrillianceSwirlblade>())
                .AddIngredient(ItemID.HellstoneBar, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class FlamingSwirlbladeProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<FlamingSwirlblade>();
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
        }
        public override float Radius => 170 * (Projectile.Calamity().stealthStrike ? 1.6f : 1);
        public override int SpreadTime => Projectile.Calamity().stealthStrike ? 38 : 21;
        public override void AI()
        {
            base.AI();
            if (BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), false, Main.rand.Next(12, 16), Main.rand.NextFloat(0.6f, 1f) * 0.04f * BladeScale * Projectile.scale, (Main.rand.NextBool() ? Color.Firebrick : Color.Orange) * BladeScale, new Vector2(0.18f, 1f), false, false));
            }
            CEUtils.AddLight(Projectile.Center, new Color(255, 130, 130));
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
                    Color clr = new Color(255, 170, 170) * 0.58f * p;
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
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 230, 230), new Color(180, 16, 16));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                Texture2D smear = CEUtils.getExtraTex("CircularSmear");
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                ApplyShader(new Color(255, 240, 180));
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 20, 0) * Projectile.Opacity * BladeScale, time * 52f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 20, 0) * Projectile.Opacity * BladeScale, time * -48f, o, scale * 1, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 20, 0) * Projectile.Opacity * BladeScale, time * 44f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 20, 0) * Projectile.Opacity * BladeScale, time * -40f, o, scale * 0.98f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.6f, 0.84f), Projectile.Center);
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.OrangeRed : Color.Orange, new Vector2(2.4f, 0.6f), true));
            if(Main.myPlayer == Projectile.owner)
            {
                int flame = ModContent.ProjectileType<FlamingSwirlbladeFlame>();
                if(Projectile.Calamity().stealthStrike)
                {
                    for(int i = 0; i < 8; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ((i / 8f) * MathHelper.TwoPi).ToRotationVector2() * 20, flame, (int)(Projectile.damage * 0.4f), 6, Projectile.owner);
                    }
                }
                else
                {
                    NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 1000);
                    float dir = target == null ? CEUtils.randomRot() : (target.Center - Projectile.Center).ToRotation();
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir.ToRotationVector2() * 20, flame, (int)(Projectile.damage * 0.4f), 6, Projectile.owner);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 90);
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1.4f, 1.7f), target.Center, volume: 0.9f);

            for (int i = 0; i < 10; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, 0.04f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.Orange : (Color.Firebrick * 1.25f), new Vector2(2.4f, 0.6f), true));
        }
    }
    public class FlamingSwirlbladeFlame : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 400;
            Projectile.MaxUpdates = 2;
            Projectile.localNPCHitCooldown = -1;
        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            if (Projectile.numHits > 0)
            {
                Projectile.Opacity -= 0.05f;
                if (Projectile.Opacity <= 0)
                {
                    Projectile.Opacity = 0;
                    Projectile.Kill();
                }
            }
            else
            {
                if (Projectile.localAI[1] < 12 && Projectile.localAI[1] > 4)
                    Projectile.velocity = Projectile.velocity.RotatedBy(0.09f * (Projectile.whoAmI % 2 == 0 ? 1 : -1));
                if (Projectile.localAI[1]++ > 16)
                    Projectile.HomingToNPCNearby(2.2f, 0.95f, 1000);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.AddLight(Projectile.Center, Color.Orange, Projectile.scale);
            for (float i = 0.1f; i <= 1f; i += 0.1f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 140)
                {
                    odp.RemoveAt(0);
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 90);

            float scale = 1.5f;
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f;
                dust.velocity = CEUtils.randomPointInCircle(30);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.Orange : Color.OrangeRed;
                dust.fadeIn = 2f;
            }
            scale = 1.6f;
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.OrangeRed * 0.8f, scale * 1f, 1, true, BlendState.Additive, 0, 7);
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.6f, 1, true, BlendState.Additive, 0, 7);
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
                width *= Projectile.Opacity;
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha * Projectile.Opacity, 32 * Projectile.scale * width, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(255, 235, 220), new Color(255, 120, 50), CEUtils.getExtraTex("Streak5"), CEUtils.getExtraTex("StreakSolid"), innerWidth: 1.6f);
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return (Projectile.Opacity > 0.3f && Projectile.localAI[1] > 10) ? null : false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
