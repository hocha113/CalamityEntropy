using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Vanity
{
    public class FadingRoseateReverie : ModItem, IDonatorItem, IVanitySkin, IGetFromStarterBag
    {
        public string DonatorName => "Rathyep";

        public override void Load() {
            if (Main.netMode != NetmodeID.Server) {
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Head", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, $"{Mod.Name.ToString()}/Content/Items/Vanity/{Name}_Legs", EquipType.Legs, this);
            }
        }

        public override void SetStaticDefaults() {
            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlotHead = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlotHead] = false;

            int equipSlotBody = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlotBody] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlotBody] = true;

            int equipSlotLegs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlotLegs] = true;
        }

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;

            Item.accessory = true;
            Item.vanity = true;

            Item.value = Item.buyPrice(0, 1, 0, 0);

            Item.rare = ItemRarityID.Green;
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset) {
            if (line.Mod == "Terraria") {
                if (line.Name == "ItemName") {
                    CERarityNameEffects.DrawCrystal(Item, line, Color.DeepPink, Color.LightPink, Color.LightPink);
                    return false;
                }
                if (line.Text.StartsWith("^")) {
                    TooltipLine parent = new TooltipLine(Mod, line.Name, line.Text.Substring(1));
                    var newLine = new DrawableTooltipLine(parent, line.Index, line.X, line.Y, line.Color);
                    CERarityNameEffects.DrawCrystal(Item, newLine, new Color(255, 42, 54), new Color(90, 84, 255), Color.LightPink, false);
                    return false;
                }
            }
            return true;
        }
        public void SpawnParticles(Vector2 playerPos) {
            if (Main.rand.NextBool(8)) {
                Vector2 pos = playerPos + new Vector2(Main.rand.NextFloat(-1600, 1600), -650);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-2.4f, 2.4f), Main.rand.NextFloat(1.8f, 2.45f));
                int t = Main.rand.Next(1, Main.rand.NextBool(12) ? 6 : 2);
                for (int i = 0; i < t; i++) {
                    //PRT_SakuraPetalsParticle AlphaBlend+rotation走Configure
                    PRTLoader.NewParticle<PRT_SakuraPetalsParticle>(pos, vel, Color.Pink, Main.rand.NextFloat(0.35f, 0.68f))
                        .Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
                    pos += CEUtils.randomPointInCircle(100);
                    vel += CEUtils.randomPointInCircle(0.4f);
                }
            }
        }
        public override void UpdateVanity(Player player) {
            player.GetModPlayer<VanityModPlayer>().vanityEquipped = Name;
            player.Entropy().light += 0.5f;
            SpawnParticles(player.Center);
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) {
                player.GetModPlayer<VanityModPlayer>().vanityEquipped = Name;
                player.Entropy().light += 0.5f;
                SpawnParticles(player.Center);
            }
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddRecipeGroup(CERecipeGroups.butterflies)
                .AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.LifeCrystal)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public bool OwnAble(Player player, ref int count) {
            return StartBagGItem.NameContains(player, "rathyep") || StartBagGItem.NameContains(player, "hikari");
        }
    }
}
