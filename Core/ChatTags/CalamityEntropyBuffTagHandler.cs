using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.Wyrm;
using CalamityEntropy.Content.Items.Donator;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityEntropy.Core.ChatTags
{
    public sealed class CalamityEntropyBuffTagHandler : CETagHandler<CalamityEntropyBuffTagHandler>
    {
        public static Color FireDebuffColor => new Color(253, 107, 2);
        public static Color SicknessDebuffColor => new Color(136, 198, 10);
        public static Color WaterDebuffColor => new Color(105, 147, 255);
        public static Color ColdDebuffColor => new Color(159, 230, 252);
        public static Color ElectricDebuffColor => new Color(255, 245, 0);
        public static Color BuffColor => new Color(255, 105, 237);
        public static Color TypelessDebuffColor => new Color(230, 202, 250);
        public static Color VulnHexDebuffColor => new Color(196, 35, 43);
        public static Color MiracleBlightDebuffColor => Main.DiscoColor;

        public sealed class Snippet(int buffId) : TextSnippet
        {
            private const float IconSize = 26f;

            public int BuffId => buffId;

            public bool DrawIcon => true;


            private static Dictionary<int, Color> _buffColorOverrides;

            private static Dictionary<int, Color> BuffColorOverrides => _buffColorOverrides ??= new() {
                [ModContent.BuffType<VoidTouch>()] = Color.Purple,
                [ModContent.BuffType<VoidVirus>()] = TypelessDebuffColor,
                [ModContent.BuffType<EclipsedImprint>()] = Color.Orange,
                [ModContent.BuffType<StareOfTheAbyss>()] = Color.DarkBlue,
                [ModContent.BuffType<ServiceBuff>()] = BuffColor,
                [ModContent.BuffType<LifeOppress>()] = TypelessDebuffColor,
                [ModContent.BuffType<SoulDisorder>()] = TypelessDebuffColor,
                [ModContent.BuffType<MechanicalTrauma>()] = TypelessDebuffColor,
                [ModContent.BuffType<FlamingBlood>()] = FireDebuffColor,
                [ModContent.BuffType<Deceive>()] = TypelessDebuffColor,
                [ModContent.BuffType<BonePiercingToxin>()] = TypelessDebuffColor,
                [ModContent.BuffType<HeatDeath>()] = TypelessDebuffColor,
                [ModContent.BuffType<Koishi>()] = Color.Cyan,
            };

            public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = new Vector2(), Color color = new Color(), float scale = 1) {
                size = new Vector2(GetStringLength(FontAssets.MouseText.Value), IconSize);

                if (!justCheckingString && (color.R != 0 || color.G != 0 || color.B != 0)) {
                    if (DrawIcon) {
                        if (Main.netMode != NetmodeID.Server && !Main.dedServ) {
                            var texture = TextureAssets.Buff[BuffId];
                            spriteBatch.Draw(texture.Value, new Rectangle((int)position.X, (int)position.Y - 2, (int)IconSize, (int)IconSize), null, Color.White);
                        }

                        position.X += IconSize;
                    }
                    Color buffColor;

                    if (BuffColorOverrides.TryGetValue(buffId, out Color overrideColor)) {
                        buffColor = overrideColor;
                    }
                    else {
                        // 颜色表未收录时按增/减益给默认色（原先由灾厄的元素减益调色函数兜底）
                        buffColor = Main.debuff[buffId] ? TypelessDebuffColor : BuffColor;
                    }

                    var name = $"{(DrawIcon ? " " : "")}{Lang.GetBuffName(buffId)}";
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, name, position, buffColor, 0f, Vector2.Zero, new Vector2(scale));
                }
                return true;
            }

            public override float GetStringLength(DynamicSpriteFont font) {
                float iconSize = !DrawIcon ? 0f : IconSize + font.MeasureString(" ").X;
                float size = iconSize + font.MeasureString(Lang.GetBuffName(buffId)).X;
                return size * Scale;
            }
        }

        protected override string[] TagNames { get; } = ["cebuff"];

        public override TextSnippet Parse(string text, Color baseColor = new(), string options = null) {
            if (int.TryParse(text, out int buffId) && buffId >= 0 && buffId < BuffLoader.BuffCount)
                return new Snippet(buffId);

            if (BuffID.Search.TryGetId(text, out buffId))
                return new Snippet(buffId);

            return new TextSnippet(text);
        }
    }

    public class TweakToolTips : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            int lastTooltipIndex = -1;
            for (int i = 0; i < tooltips.Count; i++)
                if (tooltips[i].Name.StartsWith("Tooltip"))
                    lastTooltipIndex = i;

            var buffIdsInTooltip = new HashSet<int>();
            foreach (var tooltip in tooltips) {
                var snippets = ChatManager.ParseMessage(tooltip.Text, Color.White);
                foreach (var snippet in snippets) {
                    if (snippet is CalamityEntropyBuffTagHandler.Snippet buffSnippet)
                        buffIdsInTooltip.Add(buffSnippet.BuffId);
                }
            }

            if (buffIdsInTooltip.Count == 0 || lastTooltipIndex == -1)
                return;

            bool foundDebuff = false;
            bool showHint = false;

            foreach (int buffId in buffIdsInTooltip) {
                string tooltipKey = buffId < BuffID.Count
                    ? $"Mods.Terraria.Buffs.{BuffID.Search.GetName(buffId)}.ItemTooltip"
                    : $"Mods.{BuffLoader.GetBuff(buffId).Mod.Name}.Buffs.{BuffLoader.GetBuff(buffId).Name}.ItemTooltip";

                if (!Language.Exists(tooltipKey) || string.IsNullOrWhiteSpace(Language.GetTextValue(tooltipKey)))
                    continue;

                foundDebuff = true;

                if (!PlayerInput.Triggers.Current.SmartCursor) {
                    showHint = true;
                    break;
                }

                tooltips.Add(
                    new TooltipLine(Mod, "CE:AltExpand" + buffId, $"[cebuff:{buffId}]\n{Language.GetTextValue(tooltipKey)}"));
            }

            if (showHint) {
                bool hasAltHintAlready = false;
                for (int i = 0; i < tooltips.Count; i++) {
                    if (tooltips[i].Name.StartsWith("RagnarokMod:AltHint") || tooltips[i].Name.StartsWith("IEoR:AltHint")) {
                        hasAltHintAlready = true;
                        break;
                    }
                }


                if (!hasAltHintAlready) {
                    var key = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard]
                        .KeyStatus["SmartCursor"].First().ToString();
                    var hint = new TooltipLine(Mod, "CE:AltHint", $"Hold {key} to see buff information");
                    hint.OverrideColor = new Color(170, 170, 170);
                    tooltips.Add(hint);
                }
            }
            else if (foundDebuff) {
                foreach (var t in tooltips)
                    if (t.Name.Contains("Tooltip") && !t.Name.Contains("AltExpand"))
                        t.Hide();
            }
        }
    }
}
