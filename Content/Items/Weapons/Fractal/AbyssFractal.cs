using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
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
    public class AbyssFractal : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;
            Item.height = 40;
            Item.useTime = Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AbyssFractalHeld>();
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
            if (CECal.CalChainReady(CEID.Item_AbyssBlade, CEID.Item_Floodtide, CEID.Item_Lumenyl))
            {
                CreateRecipe().AddIngredient<BrilliantFractal>()
                .AddIngredient(CEID.Item_AbyssBlade)
                .AddIngredient(CEID.Item_Floodtide)
                .AddIngredient(CEID.Item_Lumenyl, 8)
                .AddTile(TileID.MythrilAnvil).Register();
                return;
            }
            CreateRecipe().AddIngredient<BrilliantFractal>()
                .AddIngredient(ItemID.InfluxWaver)
                .AddIngredient(ItemID.BrokenHeroSword)
                .AddTile(TileID.MythrilAnvil).Register();
        }
    }
    public class AbyssFractalHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/AbyssFractal";
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
                    for (int i = 0; i < 3; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 80, (Vector2)(CEUtils.normalize(Projectile.velocity.RotatedBy(dir * MathHelper.PiOver2 * 0.7f)) * 12 + CEUtils.randomPointInCircle(5)), ModContent.ProjectileType<AbyssalBullet>(), Projectile.damage / 6, Projectile.knockBack, Projectile.owner);
                    }
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
            if (progress > 0.46 && Projectile.owner == Main.myPlayer)
            {
                if (shoot)
                {
                    shoot = false;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * 0.16f, ModContent.ProjectileType<FractalAbyssalBlade>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
            }
            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;
            float RotF = 5.2f;
            alpha = 1;
            scale = 1.6f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.CustomLerp2(progress)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
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
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 400);
            if (playHitSound)
            {
                playHitSound = false;
                CEUtils.PlaySound("sf_hit", 1, Projectile.Center, volume: CEUtils.WeapSound);
                CEUtils.PlaySound("FractalHit", 1, Projectile.Center, volume: CEUtils.WeapSound);

            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TownSlimeTransform, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
            //PRT_Abyssal不进常规桶,EffectLoader DrawParticleEffectsAlt RT画,vd/ad直赋对齐旧AbyssalParticles
            //slash那套AbyssalLine是常规PRT桶,Configure设AdditiveBlend就行
            for (int i = 0; i < 64; i++)
            {
                var p = PRTLoader.NewParticle<PRT_Abyssal>(
                    CEUtils.randomPoint(target.Hitbox),
                    Projectile.velocity.RotatedByRandom(0.16f).normalize() * Main.rand.NextFloat(8, 48),
                    Color.White, 1f);
                p.vd = 0.92f;
                p.ad = 0.03f;
                p.Opacity = 0.38f * Main.rand.NextFloat(1.2f, 1.6f);
            }
        }
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

            Main.spriteBatch.Draw(bs, (Vector2)(Projectile.Center + CEUtils.GetOwner(Projectile).gfxOffY * Vector2.UnitY - Main.screenPosition), null, new Color(100, 50, 200) * (1 - ((counter / MaxUpdateTime) * (counter / MaxUpdateTime))) * 0.8f, CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.4f, false) + MathHelper.ToRadians(32) * -dir, bs.Size() / 2f, Projectile.scale * 1.74f * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(bs, (Vector2)(Projectile.Center + CEUtils.GetOwner(Projectile).gfxOffY * Vector2.UnitY - Main.screenPosition), null, new Color(100, 50, 200) * 0.6f * (1 - ((counter / MaxUpdateTime) * (counter / MaxUpdateTime))) * 0.8f, CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.1f, false) + MathHelper.ToRadians(32) * -dir, bs.Size() / 2f, Projectile.scale * 2f * scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (156) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
    }

}
