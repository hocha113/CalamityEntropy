using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Kinanition : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 36;
            Item.height = 110;
            Item.useTime = 5;
            Item.useAnimation = 15;
            Item.reuseDelay = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 20f;
            Item.useAmmo = AmmoID.Arrow;
            Item.autoReuse = true;
            Item.ArmorPenetration = 30;
            Item.value = Item.buyPrice(gold: 12);
            Item.rare = ItemRarityID.Purple;
        }

        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 16;

        public override Vector2? HoldoutOffset() => new Vector2(-42, 0);

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useTime = 20;
                Item.useAnimation = 20;
            }
            else
            {
                Item.useTime = 5;
                Item.useAnimation = 15;
            }
            return true;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            if (player.itemAnimation < 38)
            {
                return false;
            }
            return base.CanConsumeAmmo(ammo, player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                CEUtils.PlaySound("flashback", 1.2f, position, 8, 0.4f);
                type = ModContent.ProjectileType<LightningSpear>();
                Projectile.NewProjectile(source, position, velocity * 3, type, damage * 6, knockback, player.whoAmI);
            }
            else
            {
                SoundEngine.PlaySound(Item.UseSound);
                if (CEUtils.CheckWoodenAmmo(type, player))
                    type = ProjectileID.WoodenArrowFriendly;

                int j = 2;
                var var = Main.rand;
                for (int i = 0; i < 2 + player.Entropy().WeaponBoost; i++)
                {
                    if (i == 0)
                    {
                    }
                    else
                    {
                        int arrow;
                        arrow = Projectile.NewProjectile(source, position + player.itemRotation.ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * j + player.itemRotation.ToRotationVector2() * Main.rand.Next(4, 16), velocity.RotatedBy(MathHelper.ToRadians((float)Main.rand.Next(0, 7) - 3f)) * 2, type, damage / 2, knockback, player.whoAmI);
                        Main.projectile[arrow].Entropy().Lightning = true;
                        arrow.ToProj().localNPCHitCooldown = 6;
                        arrow.ToProj().usesLocalNPCImmunity = true;
                        CEUtils.SyncProj(arrow);
                        arrow = Projectile.NewProjectile(source, position + player.itemRotation.ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * j + player.itemRotation.ToRotationVector2() * Main.rand.Next(4, 16), velocity.RotatedBy(MathHelper.ToRadians((float)Main.rand.Next(0, 7) - 3f)) * 2, type, damage / 2, knockback, player.whoAmI);
                        Main.projectile[arrow].Entropy().Lightning = true;
                        arrow.ToProj().localNPCHitCooldown = 6;
                        arrow.ToProj().usesLocalNPCImmunity = true;
                        CEUtils.SyncProj(arrow);
                    }
                }
            }

            return false;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Barinade, CEID.Item_Barinautical, CEID.Item_Lumenyl, CEID.Item_LifeAlloy))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_Barinade, 1).
                AddIngredient(CEID.Item_Barinautical, 1).
                AddIngredient(CEID.Item_Lumenyl, 20).
                AddIngredient(CEID.Item_LifeAlloy, 5).
                AddTile(TileID.MythrilAnvil).
                Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.HallowedRepeater)
                .AddIngredient(ItemID.ChlorophyteShotbow)
                .AddIngredient(ItemID.ShroomiteBar, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class KinanitionSpawn : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, 3);
            Projectile.width = Projectile.height = 46;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/Glow";
        public int NoChaseTime = 14;
        public override void AI()
        {
            if (NoChaseTime > 0)
            {
                Projectile.velocity *= 0.9f;
                NoChaseTime--;
            }
            else
            {
                Projectile.HomingToNPCNearby(12, 0.9f, 1200);
            }
            for(float i = 0.1f; i <= 1f; i+=0.1f)
            {
                oldPos.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity, i));
                if(oldPos.Count > 40)
                    oldPos.RemoveAt(0);
            }
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            NoChaseTime = 8;
            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(60, 255, 255), 0.01f).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, 0.6f, 14);
        }
        public override bool? CanHitNPC(NPC target)
        {
            return NoChaseTime <= 0 ? null : false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            float ap = 1f / (float)oldPos.Count;
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < oldPos.Count; i++)
            {
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, new Color(60, 255, 255) * ap, Projectile.velocity.ToRotation(), tex.Size() / 2, new Vector2(1 + (Projectile.velocity.Length() * 0.01f), (1f / (1 + (Projectile.velocity.Length() * 0.01f)))) * 0.26f * ap, SpriteEffects.None, 0);
                ap += 1f / (float)oldPos.Count;
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
