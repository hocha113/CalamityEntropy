using CalamityEntropy.Content.Items.Vanity;
using CalamityMod.Items.Accessories.Vanity;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CalamityEntropy.Common
{
    public class VanityDisplaySys : GlobalItem
    {
        public static List<int> VanityItems = new List<int>();
        public override void ModifyTooltips(Item entity, List<TooltipLine> tooltips)
        {
            if (IsASkinVanity(entity))
            {
                tooltips.Add(new TooltipLine(Mod, "CESkinDisplay", "-"));
                tooltips.Add(new TooltipLine(Mod, "Placeholder", "-"));
            }
        }
        public override bool InstancePerEntity => true;
        public Player dummy = null;
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Name == "CESkinDisplay")
            {
                int oFlag = Main.LocalPlayer.GetModPlayer<VanityModPlayer>().SpecialFlag;
                Main.LocalPlayer.GetModPlayer<VanityModPlayer>().SpecialFlag = 1;
                dummy = new Player();
                dummy.CopyVisuals(Main.LocalPlayer);
                dummy.cHandOn = 0;
                dummy.cHandOff = 0;
                dummy.cBack = 0;
                dummy.cFront = 0;
                dummy.cShoe = 0;
                dummy.cWaist = 0;
                dummy.cShield = 0;
                dummy.cNeck = 0;
                dummy.cFace = 0;
                dummy.cFaceHead = 0;
                dummy.cFaceFlower = 0;
                dummy.cBalloon = 0;
                dummy.cBalloonFront = 0;
                dummy.cWings = 0;
                dummy.cCarpet = 0;
                dummy.cFloatingTube = 0;
                dummy.cBackpack = 0;
                dummy.cTail = 0;
                dummy.cShieldFallback = 0;
                dummy.legs = EquipLoader.GetEquipSlot(item.ModItem.Mod, item.ModItem.Name, EquipType.Legs);
                dummy.body = EquipLoader.GetEquipSlot(item.ModItem.Mod, item.ModItem.Name, EquipType.Body);
                dummy.head = EquipLoader.GetEquipSlot(item.ModItem.Mod, item.ModItem.Name, EquipType.Head);
                dummy.direction = 1;
                dummy.velocity = Vector2.Zero;
                var drawInfo = default(PlayerDrawSet);
                drawInfo.isSitting = drawInfo.isSleeping = false;
                drawInfo.BoringSetup(dummy, new List<DrawData>(), new List<int>(), new List<int>(), new Vector2(line.X + 8, line.Y) + Main.screenPosition * 1, 0, 0, Vector2.Zero);
                drawInfo.colorArmorBody = drawInfo.colorArmorHead = drawInfo.colorArmorLegs = Color.White;
                drawInfo.colorHead = drawInfo.colorBodySkin = drawInfo.colorLegs = Main.LocalPlayer.skinColor;

                foreach (var layer in PlayerDrawLayerLoader.GetDrawLayers(drawInfo))
                {
                    layer.DrawWithTransformationAndChildren(ref drawInfo);
                }
                SpriteBatch sb = Main.spriteBatch;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                foreach (var data in drawInfo.DrawDataCache)
                {
                    if (data.useDestinationRectangle)
                        sb.Draw(data.texture, data.destinationRectangle, data.sourceRect, data.color, data.rotation, data.origin, data.effect, 0f);
                    else
                        sb.Draw(data.texture, data.position, data.sourceRect, data.color, data.rotation, data.origin, data.scale, data.effect, 0f);
                }
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                Main.LocalPlayer.GetModPlayer<VanityModPlayer>().SpecialFlag = oFlag;
            }
            if (line.Name == "CESkinDisplay" || line.Name == "Placeholder")
            {
                return false;
            }

            return true;
        }
        public static bool IsASkinVanity(Item item)
        {
            if (item.ModItem != null && CELists.CalVanityItems.Contains(item.type))
                return true;
            return VanityItems.Contains(item.type);
        }
        public static void SetupVanities()
        {
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                Item item = ContentSamples.ItemsByType[i];
                if (item.ModItem != null && item.ModItem is IVanitySkin)
                {
                    VanityItems.Add(i);
                }
            }
        }
    }
    public interface IVanitySkin
    { }
}