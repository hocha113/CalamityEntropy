using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Fractal
{
    public class StarlitFractal : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 60;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<StarlitFractalHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public int atkType = 1;
        public int useCount = 0;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (useCount++ % 2 == 1)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<FractalStar>(), damage, knockback, player.whoAmI);
            }
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
            if (CECal.CalChainReady(CEID.Item_TitanArm, CEID.Item_AegisBlade, CEID.Item_AureusCell, CEID.Item_StarblightSoot))
            {
                CreateRecipe().AddIngredient<AbyssFractal>()
                .AddIngredient(CEID.Item_TitanArm)
                .AddIngredient(CEID.Item_AegisBlade)
                .AddIngredient(ItemID.PiercingStarlight)
                .AddIngredient(CEID.Item_AureusCell, 4)
                .AddIngredient(CEID.Item_StarblightSoot, 16)
                .AddTile(TileID.MythrilAnvil).Register();
                return;
            }
            CreateRecipe().AddIngredient<AbyssFractal>()
                .AddIngredient(ItemID.TheHorsemansBlade)
                .AddIngredient(ItemID.FragmentSolar, 5)
                .AddIngredient(ItemID.FragmentStardust, 20)
                .AddTile(TileID.LunarCraftingStation).Register();
        }
    }
    public class StarlitFractalHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/StarlitFractal";
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
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 49, (Vector2)(CEUtils.normalize(Projectile.velocity.RotatedBy(dir * MathHelper.PiOver2)) * 3 + CEUtils.randomPointInCircle(2)), ModContent.ProjectileType<FractalStarBlade>(), (int)(Projectile.damage * 0.925f), Projectile.knockBack * 4, Projectile.owner, Projectile.rotation, dir);
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
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 460);
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = target.Center + new Vector2(0, -900) + CEUtils.randomPointInCircle(400);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, (target.Center - pos).normalize() * 42, ModContent.ProjectileType<AstralStarMelee>(), Projectile.damage / 4, Projectile.owner);
            }
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
            //GlowSparkCal命中扇形,CalamityPorts Configure不是统一五参
            for (int i = 0; i < 24; i++)
                //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, Projectile.velocity.normalize().RotatedByRandom(1.4f) * Main.rand.NextFloat(8f, 42f), Color.BlueViolet, Main.rand.NextFloat(0.02f, 0.08f)).Configure(false, 14, new Vector2(0.4f, 1));
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

            Main.spriteBatch.Draw(bs, (Vector2)(Projectile.Center + CEUtils.GetOwner(Projectile).gfxOffY * Vector2.UnitY - Main.screenPosition), null, new Color(100, 50, 200) * (1 - (counter / MaxUpdateTime) * (counter / MaxUpdateTime)) * 0.7f, CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.64f, false) + MathHelper.ToRadians(36) * -dir, bs.Size() / 2f, Projectile.scale * 1.74f * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(bs, (Vector2)(Projectile.Center + CEUtils.GetOwner(Projectile).gfxOffY * Vector2.UnitY - Main.screenPosition), null, Color.Lerp(Color.White, Color.Blue, counter / MaxUpdateTime) * (1 - (counter / MaxUpdateTime) * (counter / MaxUpdateTime)) * 0.7f, CEUtils.RotateTowardsAngle(Projectile.rotation, Projectile.velocity.ToRotation(), 0.24f, false) + MathHelper.ToRadians(36) * -dir, bs.Size() / 2f, Projectile.scale * 1.56f * scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (126) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (132) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }
    }
    public class AstralStarMelee : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/Ports/AstralStar";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            base.Projectile.width = 24;
            base.Projectile.height = 24;
            base.Projectile.friendly = true;
            base.Projectile.DamageType = DamageClass.Melee;
            base.Projectile.penetrate = 1;
            base.Projectile.tileCollide = false;
            base.Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            base.Projectile.ai[1] += 1f;

            base.Projectile.rotation += (Math.Abs(base.Projectile.velocity.X) + Math.Abs(base.Projectile.velocity.Y)) * 0.01f * (float)base.Projectile.direction;
            if (Main.rand.NextBool(8))
            {
                int num = 2;
                for (int i = 0; i < num; i++)
                {
                    Color newColor = Main.hslToRgb(0.5f, 1f, 0.5f);
                    int num2 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.RainbowMk2, 0f, 0f, 0, newColor);
                    Main.dust[num2].position = base.Projectile.Center + Main.rand.NextVector2Circular(base.Projectile.width, base.Projectile.height) * 0.5f;
                    Main.dust[num2].velocity *= Main.rand.NextFloat() * 0.8f;
                    Main.dust[num2].noGravity = true;
                    Main.dust[num2].fadeIn = 0.6f + Main.rand.NextFloat();
                    Main.dust[num2].velocity += base.Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3f;
                    Main.dust[num2].scale = 0.7f;
                    if (num2 != 6000)
                    {
                        Dust dust = Dust.CloneDust(num2);
                        dust.scale /= 2f;
                        dust.fadeIn *= 0.85f;
                        dust.color = new Color(255, 255, 255, 255);
                    }
                }

                Vector2 vector = Vector2.UnitX.RotatedByRandom(1.5707963705062866).RotatedBy(base.Projectile.velocity.ToRotation());
                int num3 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Torch, base.Projectile.velocity.X * 0.25f, base.Projectile.velocity.Y * 0.25f, 150);
                Main.dust[num3].velocity = vector * 0.33f;
                Main.dust[num3].position = base.Projectile.Center + vector * 6f;
            }

            if (Main.rand.NextBool(24) && Main.netMode != 2)
            {
                int num4 = Gore.NewGore(base.Projectile.GetSource_FromAI(), base.Projectile.Center, base.Projectile.velocity * 0.1f, 16);
                Main.gore[num4].velocity *= 0.66f;
                Main.gore[num4].velocity += base.Projectile.velocity * 0.15f;
            }

            base.Projectile.light = 0.9f;
            if (Main.rand.NextBool(5))
            {
                Color newColor2 = Main.hslToRgb(1f, 1f, 0.5f);
                int num5 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, 267, 0f, 0f, 0, newColor2);
                Main.dust[num5].position = base.Projectile.Center + Main.rand.NextVector2Circular(base.Projectile.width, base.Projectile.height) * 0.5f;
                Main.dust[num5].velocity *= Main.rand.NextFloat() * 0.8f;
                Main.dust[num5].noGravity = true;
                Main.dust[num5].fadeIn = 0.6f + Main.rand.NextFloat();
                Main.dust[num5].velocity += base.Projectile.velocity * 0.25f;
                Main.dust[num5].scale = 0.7f;
                if (num5 != 6000)
                {
                    Dust dust2 = Dust.CloneDust(num5);
                    dust2.scale /= 2f;
                    dust2.fadeIn *= 0.85f;
                    dust2.color = new Color(255, 255, 255, 255);
                }

                Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Torch, base.Projectile.velocity.X * 0.25f, base.Projectile.velocity.Y * 0.25f, 150);
            }

            if (Main.rand.NextBool(10) && Main.netMode != 2)
            {
                Gore.NewGore(base.Projectile.GetSource_FromAI(), base.Projectile.position, base.Projectile.velocity * 0.1f, Main.rand.Next(16, 18));
            }

            CEUtils.HomeInOnNPC(Projectile, base.Projectile.tileCollide, 500f, 15f, 20f);

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 100, 250, base.Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CEUtils.DrawAfterimagesCentered(base.Projectile, ProjectileID.Sets.TrailingMode[base.Projectile.type], lightColor);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(base.Projectile.position, base.Projectile.velocity, base.Projectile.width, base.Projectile.height);
            SoundEngine.PlaySound(in SoundID.Dig, base.Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            base.Projectile.ExpandHitboxBy(50);
            int num = 3;
            for (int i = 0; i < num; i++)
            {
                Color newColor = Main.hslToRgb(1f, 1f, 0.5f);
                int num2 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, 267, 0f, 0f, 0, newColor);
                Main.dust[num2].position = base.Projectile.Center + Main.rand.NextVector2Circular(base.Projectile.width, base.Projectile.height);
                Main.dust[num2].velocity *= Main.rand.NextFloat() * 2.4f;
                Main.dust[num2].noGravity = true;
                Main.dust[num2].fadeIn = 0.6f + Main.rand.NextFloat();
                Main.dust[num2].scale = 1.4f;
                if (num2 != 6000)
                {
                    Dust dust = Dust.CloneDust(num2);
                    dust.scale /= 2f;
                    dust.fadeIn *= 0.85f;
                    dust.color = new Color(255, 255, 255, 255);
                }
            }

            for (int j = 0; j < 3; j++)
            {
                int num3 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Torch, 0f, 0f, 100);
                Main.dust[num3].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[num3].scale = 0.5f;
                    Main.dust[num3].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                }
            }

            for (int k = 0; k < 3; k++)
            {
                int num4 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Torch, 0f, 0f, 100, default(Color), 1.5f);
                Main.dust[num4].noGravity = true;
                Main.dust[num4].velocity *= 5f;
                num4 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Torch, 0f, 0f, 100);
                Main.dust[num4].velocity *= 2f;
            }

            if (Main.netMode != 2)
            {
                for (int l = 0; l < 3; l++)
                {
                    Gore.NewGore(base.Projectile.GetSource_Death(), base.Projectile.position, base.Projectile.velocity * 0.05f, Main.rand.Next(16, 18));
                }
            }
        }

    }
}
