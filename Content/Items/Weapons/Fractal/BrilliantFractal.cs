using CalamityEntropy.Assets.Register;
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
    public class BrilliantFractal : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 65;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 60;
            Item.useTime = Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BrilliantFractalHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public int atkType = 1;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType *= -1;
            return false;
        }

        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_StormSaber, CEID.Item_CelestialClaymore, CEID.Item_BrimstoneSword))
            {
                CreateRecipe().AddIngredient<WelkinFractal>()
                .AddIngredient(CEID.Item_StormSaber)
                .AddIngredient(CEID.Item_CelestialClaymore)
                .AddIngredient(CEID.Item_BrimstoneSword)
                .AddTile(TileID.MythrilAnvil).Register();
                return;
            }
            CreateRecipe().AddIngredient<WelkinFractal>()
                .AddIngredient(ItemID.ChlorophyteClaymore)
                .AddIngredient(ItemID.BreakerBlade)
                .AddTile(TileID.MythrilAnvil).Register();
        }
    }
    public class BrilliantFractalHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/BrilliantFractal";
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
            if (Main.myPlayer == Projectile.owner)
            {
                if (spawnProj && progress > 0.4f)
                {
                    int dir = (int)(Projectile.ai[0]) * (Projectile.velocity.X > 0 ? -1 : 1);
                    spawnProj = false;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 49, (Vector2)(CEUtils.normalize(Projectile.velocity.RotatedBy(dir * MathHelper.PiOver2)) * 4.2f + CEUtils.randomPointInCircle(2)), ModContent.ProjectileType<FractalShadow>(), Projectile.damage, Projectile.knockBack * 4, Projectile.owner, Projectile.rotation, dir);
                }
            }
            if (init)
            {
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                if (Projectile.ai[0] == 2)
                {
                    CEUtils.PlaySound("powerwhip", 1, Projectile.Center, volume: 0.6f * CEUtils.WeapSound);
                }
                if (Projectile.ai[0] < 2)
                {
                    CEUtils.PlaySound("sf_use", 1 + Projectile.ai[0] * 0.12f, Projectile.Center, volume: 0.6f * CEUtils.WeapSound);
                }
                init = false;
            }
            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;
            float RotF = 4f;
            if (progress > 0.36f && progress < 0.64f)
                spawnProjCounter += owner.GetTotalAttackSpeed(Projectile.DamageType);
            if (spawnProjCounter >= 6f)
            {
                spawnProjCounter -= 6f;
                Vector2 spawnPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, CEUtils.randomPointInCircle(0.1f), ModContent.ProjectileType<FractalBlight>(), Projectile.damage / 6, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat() * 6.28f);
                }
            }
            alpha = 1;
            scale = 1.6f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
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

        public bool playHitSound = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (playHitSound)
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
            int dir = (int)(Projectile.ai[0]) * (Projectile.velocity.X > 0 ? -1 : 1);
            if (Projectile.ai[0] == 2)
            {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * 1.1f, effect);

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D bs = CEExtraAssets.SemiCircularSmear;
            Main.spriteBatch.Draw(bs, (Vector2)(Projectile.Center + CEUtils.GetOwner(Projectile).gfxOffY * Vector2.UnitY - Main.screenPosition), null, Color.Lerp(Color.White, Color.LightGoldenrodYellow, counter / MaxUpdateTime) * (float)(Math.Cos(CEUtils.GetRepeatedCosFromZeroToOne(counter / MaxUpdateTime, 3) * MathHelper.Pi - MathHelper.PiOver2)), Projectile.rotation + MathHelper.ToRadians(32) * -dir, bs.Size() / 2f, Projectile.scale * 1.4f * scale, SpriteEffects.None, 0);

            if (shineTex == null)
                shineTex = CEExtraAssets.StarTexture;
            Main.spriteBatch.Draw(shineTex, Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale - Main.screenPosition, null, Color.LightGoldenrodYellow * 0.7f * ((float)Math.Cos((counter / MaxUpdateTime) * MathHelper.TwoPi - MathHelper.Pi) * 0.5f + 0.5f), 0, shineTex.Size() / 2f, 0.36f * Projectile.scale * new Vector2(2.8f, 0.5f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(shineTex, Projectile.Center + Projectile.rotation.ToRotationVector2() * 98 * scale * Projectile.scale - Main.screenPosition, null, Color.LightGoldenrodYellow * 0.7f * ((float)Math.Cos((counter / MaxUpdateTime) * MathHelper.TwoPi - MathHelper.Pi) * 0.5f + 0.5f), 0, shineTex.Size() / 2f, 0.36f * Projectile.scale * new Vector2(0.5f, 2.8f), SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 110 * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 110 * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
    }

}
