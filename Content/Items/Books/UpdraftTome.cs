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
    public class UpdraftTome : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 35;
            Item.useAnimation = Item.useTime = 23;
            Item.crit = 5;
            Item.mana = 12;
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark2")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<UpdraftTomeHeld>();
        public override int SlotCount => 2;

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_AerialiteBar, 6)
                .AddIngredient<AncientScriptures>()
                .AddTile(TileID.SkyMill)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<AncientScriptures>()
                .AddIngredient<SpectralWhispers>()
                .AddIngredient<AzafureCylinder>()
                .AddIngredient(ItemID.Bone, 50)
                .AddTile(TileID.DemonAltar)
                .Register();

            CreateRecipe()
                .AddIngredient<AncientScriptures>()
                .AddIngredient<BloodCodex>()
                .AddIngredient<AzafureCylinder>()
                .AddIngredient(ItemID.Bone, 50)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }

    public class UpdraftTomeHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/UpdraftTome/UpdraftTomeOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/UpdraftTome/UpdraftTomePage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/UpdraftTome/UpdraftTomeUI";

        public override void playPageSound()
        {
            CEUtils.PlaySound("windpage", 1, Projectile.Center, 6, 0.52f);
        }

        public override float randomShootRotMax => 0.14f;
        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.Knockback *= 2;
            return m;
        }
        public override int baseProjectileType => ModContent.ProjectileType<UpdraftBullet>();
    }

}
