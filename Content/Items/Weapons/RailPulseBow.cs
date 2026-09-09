using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class RailPulseBow : ModItem, IPriceFromRecipe
    {
        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 50;
            Item.damage = 21;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 5;
            Item.useAnimation = 5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shootSpeed = 22f;
            Item.useAmmo = AmmoID.Arrow;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public bool cs = false;
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return cs;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-28, 0);
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }
        public override bool RangedPrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DubiousPlating, CEID.Item_MysteriousCircuitry))
            {
                CreateRecipe().
                AddIngredient(CEID.Item_DubiousPlating, 6).
                AddIngredient(CEID.Item_MysteriousCircuitry, 8).
                AddIngredient(ItemID.SoulofLight, 5).
                AddTile(TileID.Anvils).
                Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.PlatinumBow)
                .AddIngredient(ItemID.Cog, 50)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
