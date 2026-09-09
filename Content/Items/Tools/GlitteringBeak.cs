using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Tools
{
    public class GlitteringBeak : ModItem
    {
        private static int PickPower = 1000;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 70;
            Item.damage = 1000;
            Item.knockBack = 9f;
            Item.useTime = 1;
            Item.useAnimation = 6;
            Item.pick = PickPower;
            Item.axe = PickPower / 5;
            Item.tileBoost = 120;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(platinum: 2, gold: 80);
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.crit = 18;
            Item.useTurn = true;
            Item.autoReuse = true;
        }

        public override bool AltFunctionUse(Player player) => true;
        public Vector2 mouseLast = Vector2.Zero;
        public Vector2 mousePos = Vector2.Zero;
        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                if (player.altFunctionUse == 2)
                {
                    void KillCircleTile(Vector2 pos)
                    {
                        for (float i = 0; i <= 4; i += 0.95f)
                        {
                            for (float r = 0; r < 360; r += 20)
                            {
                                Point point = ((pos + (r + 5).ToRadians().ToRotationVector2() * i * 16) / 16f).ToPoint();
                                CEUtils.TryKillTileAndChest(point.X, point.Y, player);
                            }
                        }
                    }
                    for (float i = 0; i <= 1; i += 0.25f)
                    {
                        KillCircleTile(Vector2.Lerp(Main.MouseWorld, mouseLast, i));
                        //PRT_HadCircle2 AdditiveBlend+rotation走Configure
                        PRTLoader.NewParticle<PRT_HadCircle2>(Vector2.Lerp(Main.MouseWorld, mouseLast, i), Vector2.Zero, Color.SkyBlue, 0.4f)
                            .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0).CScale = 0.46f;
                    }
                }
                else
                {
                    if (!Main.SmartCursorIsUsed)
                    {
                        void KillTile(Vector2 pos)
                        {
                            Point point = (pos / 16f).ToPoint();
                            CEUtils.TryKillTileAndChest(point.X, point.Y, player);
                        }
                        int c = 0;
                        for (float i = 0; i <= 1; i += 0.01f)
                        {
                            KillTile(Vector2.Lerp(Main.MouseWorld, mouseLast, i));
                            if (c++ % 10 == 0)
                            {
                                PRTLoader.NewParticle<PRT_HadCircle2>(Vector2.Lerp(Main.MouseWorld, mouseLast, i), Vector2.Zero, Color.SkyBlue, 0.4f)
                                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0).CScale = 0.14f;
                            }
                        }
                    }
                    else
                    {
                        if (Main.SmartCursorShowing)
                            PRTLoader.NewParticle<PRT_HadCircle2>(new Vector2(Main.SmartCursorX * 16 + 8, Main.SmartCursorY * 16 + 8), Vector2.Zero, Color.SkyBlue, 0.4f)
                                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0).CScale = 0.14f;
                    }
                }
            }
            return base.UseItem(player);
        }
        public override void HoldItem(Player player)
        {
            mouseLast = mousePos;
            mousePos = Main.MouseWorld;
        }

        public override void ModifyTooltips(List<TooltipLine> list)
        {
            if (Item.useStyle == ItemUseStyleID.Shoot)
            {
                TooltipLine line = list.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "TileBoost");

                if (line != null)
                    line.Text = string.Empty;
            }
        }


        public override void AddRecipes()
        {
            // 灾厄繁花矿镐换为原版夜明镐（虚空主题取旋涡）；门槛由龙牙与暗淡符石把关
            CreateRecipe().
                AddCalOrOwn(CEID.Item_BlossomPickaxe, ItemID.VortexPickaxe).
                AddIngredient<FadingRunestone>().
                AddIngredient<WyrmTooth>(5).
                AddTile<AbyssalAltarTile>().
                Register();
        }
    }
}
