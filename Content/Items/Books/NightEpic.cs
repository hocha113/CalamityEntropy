using CalamityEntropy.Content.Projectiles;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books
{
    public class NightEpic : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 56;
            Item.useAnimation = Item.useTime = 18;
            Item.crit = 5;
            Item.mana = 16;
            Item.rare = Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(platinum: 1);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark4")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<NightEpicHeld>();
        public override int SlotCount => 3;

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AstralBar, CEID.Item_StarblightSoot))
            {
                CreateRecipe().AddIngredient<RedemptionBible>()
                .AddIngredient(CEID.Item_AstralBar, 8)
                .AddIngredient(CEID.Item_StarblightSoot, 6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient<BurntLostClassics>()
                .AddIngredient(ItemID.FragmentNebula, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class NightEpicHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/NightEpic/NightEpicOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/NightEpic/NightEpicPage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/NightEpic/NightEpicUI";

        public override bool Shoot()
        {
            base.Shoot();
            return base.Shoot();
        }

        public override EBookStatModifer getBaseModifer()
        {
            var mdf = base.getBaseModifer();
            mdf.Homing += 1f;
            mdf.HomingRange += 0.8f;
            return mdf;
        }
        public override float randomShootRotMax => 0.5f;
        public override int baseProjectileType => ModContent.ProjectileType<NightStar>();

        public override int frameChange => 3;

    }
}
