using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class Voidshade : ModItem
    {
        public int attackType = 0; public int comboExpireTimer = 0;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 32;
            Item.useAnimation = 32;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 100;
            Item.knockBack = 6;
            Item.UseSound = CEUtils.GetSound("powerwhip");
            Item.crit = 6;
            Item.shoot = ModContent.ProjectileType<VoidshadeHeld>();
            Item.shootSpeed = 16;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                attackType = 3;
                damage = (int)(damage * 1.5f);
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AntivoidDashSlash") { Pitch = -0.2f, MaxInstances = 4, Volume = 0.65f * CEUtils.WeapSound }, player.Center);
            }
            else
            {
                if (player.Entropy().voidshadeBoostTime > 0)
                {
                    var st = new SoundStyle("CalamityEntropy/Assets/Sounds/rswave");
                    st.Volume = 0.6f * CEUtils.WeapSound ;
                    SoundEngine.PlaySound(st, player.Center);
                }
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer, attackType);
            attackType = (attackType + 1) % 2; if (player.altFunctionUse == 2)
            {
                attackType = (attackType + 1) % 2;

            }
            comboExpireTimer = 0; return false;
        }

        public override void UpdateInventory(Player player)
        {
            if (comboExpireTimer++ >= 120) attackType = 0;
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            //脱离灾厄:灾厄Voidstone按material-map换黑曜石
            CreateRecipe().AddIngredient(ItemID.BreakerBlade).AddCalOrOwn(CEID.Item_Voidstone, ItemID.Obsidian, 12).AddTile(TileID.Anvils).Register();
        }
    }
}
