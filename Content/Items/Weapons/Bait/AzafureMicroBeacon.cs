using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class AzafureMicroBeacon : ModItem, IBaitItem, IAzafureEnhancable
    {
        public static int TagDamage = 4;
        public static float DamageMult = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.knockBack = 0;
            Item.shootSpeed = 33;
            Item.useAnimation = Item.useTime = 24;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.width = 42;
            Item.height = 42; 
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<AzafureMicroBeaconProjectile>();
            Item.autoReuse = true;
        }
        public override bool CanUseItem(Player player)
        {
            return player.Entropy().BaitUsable;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, (int)(damage * DamageMult), knockback, player.whoAmI, 0, 0, TagDamage);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient<AerialiteBar>(8)
                .AddIngredient<MysteriousCircuitry>(2)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class AzafureMicroBeaconProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<AzafureMicroBeacon>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, -1);
            Projectile.width = Projectile.height = 24;
        }

        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.GetOwner().Entropy().BaitCharge--;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (StickNPC < 0)
            {
                if (Counter > 12)
                {
                    Projectile.velocity *= 0.99f;
                    Projectile.velocity.Y += 0.8f;
                }
            }
            else
            {
                NPC npc = StickNPC.ToNPC();
                if (!npc.active)
                {
                    Projectile.Kill();
                    return;
                }
                Main.player[Projectile.owner].MinionAttackTargetNPC = npc.whoAmI;
                npc.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 2;
                if (IsActive)
                {
                    Projectile.GetOwner().Calamity().mouseWorldListener = true;
                    npc.GetGlobalNPC<WhipDebuffNPC>().ClearBaitTags();
                    npc.GetGlobalNPC<WhipDebuffNPC>().Tags.Add(new WhipTag(this.GetType().Name, 5, this.TagDamage, 1, 0, this.GetType().Name) { IsABaitTag = true });
                    if (ActiveCounter % 20 == 0)
                        CEUtils.PlaySound("WulfrumPingReady",  0.9f + ActiveCounter / 280f, Projectile.Center);
                }
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if(ActiveCounter > 120)
                {
                    if(IsActive)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                        SetActive();
                    }
                }
            }
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
        }
        public override void ActiveEffect(float damageMul)
        {
            if(Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < (Projectile.GetOwner().AzafureEnhance() ? 9 : 6); i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.GetOwner().Center + new Vector2(Main.rand.NextFloat(-100, 100), -800), Vector2.Zero, ModContent.ProjectileType<AzafureAssaultDrone>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner, 0, Main.rand.Next(0, 30));
                }
            }
        }
        public float activeEffectAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            if (activeEffectAlpha >= 0.01f)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Texture2D pulse = CEUtils.getExtraTex("HollowCircleSoftEdge");
                for(float i = 0; i < 1f; i += 0.5f)
                {
                    float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly * 2f);
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, new Color(255, 80, 80) * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.16f, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage()
        {
            return StickNPC == -1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.tileCollide = false;
            OnHitEffect(Projectile.Center); 
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/WulfrumPing"), target.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/WulfrumProsthesisSucc") with { Volume = 0.34f}, target.Center);
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            target.AddBuff<MechanicalTrauma>(260);
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("ExoHit1", Main.rand.NextFloat(1.2f, 1.5f), pos, 8, 0.65f);
            for(int i = 0; i < 2; i++)
            {
                float scale = 0.05f + 0.02f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.Red, new Color(255, 108, 108), (i / 2f)), "CalamityEntropy/Assets/Extra/HollowCircleSoftEdge", Vector2.One, CEUtils.randomRot(), scale * 0.2f, scale, 12 + i * 4));
            }

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 1, false, 12, 0.04f * Main.rand.NextFloat(0.65f, 1f), new Color(255, 180, 180), new Vector2(2f, 0.6f), true));
        }
        public override void OnKill(int timeLeft)
        {
            if(timeLeft > 0 && !Main.dedServ)
            {
                OnHitEffect(Projectile.Center);
            }
        }
    }
    public class AzafureAssaultDrone : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            Main.projFrames[Type] = 5;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 32;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 220;
        }
        public float RandOffset = Main.rand.NextFloat(-200, 200);
        public int ShootCount = 16;
        public int ShootCD = 0;
        public int NoShootTime = 50;
        public override bool? CanDamage()
        {
            return false;
        }
        public override void AI()
        {
            if (Projectile.ai[1]-- <= 0)
            {
                if (Projectile.timeLeft < 18)
                    Projectile.Opacity -= 1 / 18f;
                Player player = Projectile.GetOwner();
                Vector2 targetPos = Vector2.Zero;
                NPC target = Projectile.FindMinionTarget(2000, true);
                if (target != null && ShootCount > 0)
                {
                    targetPos = target.Center + new Vector2(RandOffset, -380);
                }
                else
                {
                    target = null;
                    targetPos = Projectile.Center + new Vector2(0, -400);
                }

                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 140)
                {
                    Projectile.velocity *= 0.935f;
                    Projectile.velocity += (targetPos - Projectile.Center).normalize() * 2.5f;
                }
                else
                {
                    Projectile.velocity *= 0.88f;
                }
                if (target != null)
                    gunRot = (target.Center - Projectile.Center).ToRotation();
                else
                    gunRot = CEUtils.RotateTowardsAngle(gunRot, MathHelper.PiOver2, 0.1f, false);

                NoShootTime--;
                if (target != null && NoShootTime <= 0)
                {
                    if (ShootCD-- <= 0)
                    {
                        ShootCount--;
                        ShootCD = 5;
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + new Vector2(0, 12) * Projectile.scale, (target.Center - Projectile.Center).normalize() * 18, ModContent.ProjectileType<BeaconDroneShoot>(), Projectile.damage / 8, 2, Projectile.owner);
                        }
                    }
                }
            }
        }
        public float gunRot = MathHelper.PiOver2;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D t = this.getTextureAlt("Gun");
            Main.EntitySpriteDraw(t, Projectile.Center + new Vector2(0, 12).RotatedBy(Projectile.rotation) * Projectile.scale - Main.screenPosition, null, lightColor, gunRot, new Vector2(2, t.Height / 2), Projectile.scale, SpriteEffects.None);
            Projectile.spriteDirection = gunRot.ToRotationVector2().X < 0 ? 1 : -1;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
    public class BeaconDroneShoot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = true;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center - Projectile.velocity * 3, targetHitbox, 16);
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("GunShotSmall" + (Main.rand.NextBool() ? "" : "Alt"), Main.rand.NextFloat(1.6f, 1.8f), Projectile.Center, 64, 0.42f);
            }
            else
            {
                Lighting.AddLight(Projectile.Center, Color.IndianRed.ToVector3() * 0.35f);

                if (Projectile.Entropy().counter > 2)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, Projectile.velocity * 0.01f, false, 3, 0.02f, new Color(255, 60, 60), new Vector2(0.14f, 1f)));
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(260);
            if (target.GetGlobalNPC<WhipDebuffNPC>().BaitStick > 0)
            {
                target.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 0;
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.ModProjectile != null && p.ModProjectile is BaitProj ibp && ibp.StickNPC == target.whoAmI)
                    {
                        if (!ibp.IsActive)
                            p.Kill();
                    }
                }
            }
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 dustVel = (Projectile.rotation).ToRotationVector2().RotatedByRandom(0.2f) * Main.rand.NextFloat(2.5f, 8.5f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(11, 11), Main.rand.NextBool() ? 278 : ModContent.DustType<LightDust>(), dustVel * Main.rand.NextFloat(0.3f, 1.7f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1.1f);
                dust.color = new Color(255, 120, 120);
                dust.noLightEmittence = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.Entropy().counter < 3)
                return false;
            Texture2D tex = CEUtils.getExtraTex("Circle");
            Color drawColor = new Color(255, 160, 160);
            Main.spriteBatch.UseAdditiveClamp();
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.velocity.ToRotation(), tex.Size() * 0.5f, new Vector2(2.25f, 0.05f) * Projectile.scale * 0.16f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
