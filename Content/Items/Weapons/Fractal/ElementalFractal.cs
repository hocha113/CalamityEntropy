using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Fractal
{
    public class ElementalFractal : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 480;
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;
            Item.height = 60;
            Item.useTime = Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ElementalFractalHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public int atkType = 0;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType);
            atkType = 1 - atkType;
            return false;
        }

        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_FlarefrostBlade, CEID.Item_LifeAlloy))
            {
                CreateRecipe()
                .AddIngredient<StarlitFractal>()
                .AddIngredient(CEID.Item_FlarefrostBlade)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddIngredient(CEID.Item_LifeAlloy, 5)
                .AddIngredient(ItemID.FragmentSolar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<StarlitFractal>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class ElementalFractalHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/ElementalFractal";
        List<float> odr = new List<float>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 3;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public float spawnProjCounter = 0;
        public override void AI()
        {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init)
            {
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                if (Projectile.ai[0] == 0)
                {
                    CEUtils.PlaySound("sf_use", 0.6f, Projectile.Center, volume: 0.6f * CEUtils.WeapSound);
                }
                init = false;
            }
            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;

            if (Projectile.ai[0] == 0)
            {
                if (progress > 0.18f && progress < 0.82f)
                {
                    for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                    {
                        if (Projectile.localNPCImmunity[i] == -1)
                            Projectile.localNPCImmunity[i] = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));
                    }
                    Projectile.localNPCHitCooldown = (int)(Projectile.MaxUpdates * 4 / owner.GetTotalAttackSpeed(Projectile.DamageType));
                }
                else
                {
                    Projectile.localNPCHitCooldown = -1;
                }

                float RotF = MathHelper.ToRadians(140) + MathHelper.TwoPi * 4;
                if (progress > 0.3f && progress < 0.7f)
                    spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
                if (spawnProjCounter >= 6f)
                {
                    spawnProjCounter -= 6f;
                    Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, CEUtils.randomPointInCircle(0.1f) + Projectile.rotation.ToRotationVector2() * 6, ModContent.ProjectileType<FractalBlight>(), Projectile.damage / 5, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat() * 6.28f, 1);
                    }
                }
                alpha = 1;
                scale = 1.6f;
                Projectile.rotation = Projectile.velocity.ToRotation() + (MathHelper.ToRadians(-140) + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 1)) * -1 * (Projectile.velocity.X > 0 ? -1 : 1);
            }
            else
            {
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((owner.mouseWorld() - Projectile.Center).ToRotation());
                if (progress < 0.34f)
                {
                    float p = progress / 0.34f;
                    Projectile.rotation = (owner.mouseWorld() - Projectile.Center).ToRotation() + CEUtils.GetRepeatedCosFromZeroToOne(p, 2) * MathHelper.ToRadians(140) * (Projectile.velocity.X > 0 ? -1 : 1);
                }
                else
                {
                    if (progress < 0.67)
                    {
                        float p = (progress - 0.34f) / 0.33f;

                        Projectile.rotation = (owner.mouseWorld() - Projectile.Center).ToRotation() + (CEUtils.GetRepeatedCosFromZeroToOne(1 - p, 2) * MathHelper.ToRadians(280) - MathHelper.ToRadians(140)) * (Projectile.velocity.X > 0 ? -1 : 1);

                    }
                    else
                    {
                        float p = (progress - 0.67f) / 0.33f;

                        Projectile.rotation = (owner.mouseWorld() - Projectile.Center).ToRotation() + (CEUtils.GetRepeatedCosFromZeroToOne(p, 2) * MathHelper.ToRadians(280) - MathHelper.ToRadians(140)) * (Projectile.velocity.X > 0 ? -1 : 1);

                    }
                    if (progress > 0.46 && shoot)
                    {
                        shoot = false;
                        CEUtils.PlaySound("zypshot2", 1, Projectile.Center, volume: CEUtils.WeapSound);
                        if (Main.myPlayer == Projectile.owner)
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 2, ModContent.ProjectileType<ElementalFractalThrown>(), (int)(Projectile.damage * 1.6f), Projectile.knockBack * 2, Projectile.owner);
                    }
                }
                scale = 1.6f;
                alpha = 1;
            }


            Projectile.Center = Projectile.GetOwner().MountedCenter;


            if (odr.Count > 60)
            {
                odr.RemoveAt(0);
            }
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
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            if (Projectile.ai[0] == 1 && progress < 0.32f)
            {
                return false;
            }
            return null;
        }
        public bool playHitSound = true;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 0)
            {
                modifiers.SourceDamage *= 0.4f;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<ElementalMix>(), 400);
            if (playHitSound || Projectile.ai[0] == 0)
            {
                playHitSound = false;
                CEUtils.PlaySound("sf_hit", 1, Projectile.Center, volume: CEUtils.WeapSound);
                CEUtils.PlaySound("FractalHit", 1, Projectile.Center, volume: CEUtils.WeapSound);

            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        public static Texture2D shineTex = null;
        public bool spawnProj = true;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            int dir = (Projectile.velocity.X > 0 ? 1 : -1);
            if (Projectile.ai[0] == 2)
            {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * 1.1f, effect);

            if (Projectile.ai[0] == 0)
            {

                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Texture2D bs = CEExtraAssets.SemiCircularSmear;
                Main.spriteBatch.Draw(bs, (Vector2)(Projectile.Center + CEUtils.GetOwner(Projectile).gfxOffY * Vector2.UnitY - Main.screenPosition), null, Color.Lerp(new Color(255, 200, 215), new Color(255, 140, 150), counter / MaxUpdateTime) * (float)(Math.Cos(CEUtils.GetRepeatedCosFromZeroToOne(counter / MaxUpdateTime, 1) * MathHelper.Pi - MathHelper.PiOver2)), Projectile.rotation + MathHelper.ToRadians(32) * -dir, bs.Size() / 2f, Projectile.scale * 1.6f * scale, SpriteEffects.None, 0);

                if (shineTex == null)
                    shineTex = CEExtraAssets.StarTexture;
                Main.spriteBatch.Draw(shineTex, Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale - Main.screenPosition, null, Color.LightGoldenrodYellow * 0.7f * ((float)Math.Cos((counter / MaxUpdateTime) * MathHelper.TwoPi - MathHelper.Pi) * 0.5f + 0.5f), 0, shineTex.Size() / 2f, 0.36f * Projectile.scale * new Vector2(2.8f, 0.5f), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(shineTex, Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale - Main.screenPosition, null, Color.LightGoldenrodYellow * 0.7f * ((float)Math.Cos((counter / MaxUpdateTime) * MathHelper.TwoPi - MathHelper.Pi) * 0.5f + 0.5f), 0, shineTex.Size() / 2f, 0.36f * Projectile.scale * new Vector2(0.5f, 2.8f), SpriteEffects.None, 0);

                Main.spriteBatch.ExitShaderRegion();
            }

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (124 * (Projectile.ai[0] == 2 ? 0.9f : 1)) * Projectile.scale * scale, targetHitbox, 46);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (130 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
    }

}
