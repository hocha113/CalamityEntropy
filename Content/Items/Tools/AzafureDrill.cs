using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Rarities;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Tools
{
    public class AzafureDrill : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 18;
            Item.damage = 7;
            Item.ArmorPenetration = 5;
            Item.knockBack = 0f;
            Item.useTime = 6;
            Item.useAnimation = 25;
            Item.pick = 70;
            // 灾厄真近战伤害类按全局裁定统一归 DamageClass.Melee
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = SoundID.Item23;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.tileBoost = -1;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<HellIndustrialComponents>(4).
                AddCalOrOwn(CEID.Item_DubiousPlating, ModContent.ItemType<AzafurePlating>(), 6).
                AddRecipeGroup(CERecipeGroups.IronBar, 6).
                AddTile(TileID.Anvils).
                Register();
        }

        public override void HoldItem(Player player)
        {
            Item.pick = player.AzafureEnhance() ? 100 : 70;
            Item.tileBoost = player.AzafureEnhance() ? 3 : -1;
            player.Entropy().MouseWorldListener = true;
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;
            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 6f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2));

            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }
        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
    }
}
