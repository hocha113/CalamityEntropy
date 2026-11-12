using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BloodforgedEmberScythe
{
    public class BloodforgedEmber : ModItem
    {
        private static readonly Color Blood = new(230, 24, 32);
        private static readonly Vector2[] StrokeOffsets = [new(-1, -1), new(-1, 0), new(-1, 1), new(0, -1), new(0, 1), new(1, -1), new(1, 0), new(1, 1)];

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.width = 132;
            Item.height = 120;
            Item.damage = 3200;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 20;
            Item.useTime = Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 6f;
            Item.crit = 10;
            Item.UseSound = null;
            Item.shoot = ModContent.ProjectileType<BloodforgedEmberHeld>();
            Item.shootSpeed = 1f;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            Item.QuickDrawItemWithBloomToWorld(spriteBatch, Color.Red, ref scale, rotation);
            return false;
        }

        //名字的黑字红边在 EGlobalItem 里和刻尘者共用,这里只画说明行的黑红闪烁
        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset) {
            if (line.Mod != "Terraria" || !line.Name.StartsWith("Tooltip"))
                return true;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float time = Main.GlobalTimeWrappedHourly;
            float x = line.X;
            for (int i = 0; i < line.Text.Length; i++) {
                string ch = line.Text[i].ToString();
                //一道波沿着行推过去,每个字只停在黑字红边或红字黑边两态之一,换态很快
                float red = Utils.GetLerpValue(-0.2f, 0.2f, MathF.Sin(time * 3.2f - i * 0.3f), true);
                Color fill = Color.Lerp(Color.Black, Blood, red);
                Color rim = Color.Lerp(Blood, Color.Black, red);
                Vector2 pos = new(x, line.Y);
                foreach (Vector2 o in StrokeOffsets)
                    Main.spriteBatch.DrawString(font, ch, pos + o, rim);
                Main.spriteBatch.DrawString(font, ch, pos, fill);
                x += font.MeasureString(ch).X;
            }
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.DeathSickle)
                .AddIngredient(ModContent.ItemType<FadingRunestone>(), 5)
                .AddIngredient(ItemID.SoulofNight, 50)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }
}
