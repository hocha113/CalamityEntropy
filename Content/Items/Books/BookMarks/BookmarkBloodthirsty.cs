using CalamityEntropy.Common;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkBloodthirsty : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(gold: 1);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Bloodthirsty");
        public override Color tooltipColor => new Color(255, 6, 6);
        public override void ModifyStat(EBookStatModifer modifer) {
            modifer.attackSpeed += 0.15f;
            modifer.lifeSteal += 0.1f;
        }
        public override EBookProjectileEffect getEffect() {
            return new BloodthirstBMEffect();
        }
        public override void AddRecipes() {
        }
        public override void OnCreated(ItemCreationContext context) {
            if (context is RecipeItemCreationContext) {
                Main.LocalPlayer.Hurt(PlayerDeathReason.ByCustomReason(Mod.GetLocalization("BloodthirstyKilled").ToNetworkText(Main.LocalPlayer.name)), 199, 0, false, false, -1, false, 99999);
                if (Main.LocalPlayer.statLife <= 0 || Main.LocalPlayer.dead)
                    Item.TurnToAir();
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.Add(new TooltipLine(Mod, "TooltipBt", Mod.GetLocalization("BloodthirstyRequirement").Value) { OverrideColor = Color.Yellow });
            base.ModifyTooltips(tooltips);
        }
    }
    public class BloodthirstBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            // 重设计暂行案: 灾厄怒气泵送随 RageMode 退役, 改为命中喂养自有嗜血狂热值
            // (BloodthirstyEffect, 上限 16, 受击也会增长, 由本书签的攻速加成消费)
            var plr = projectile.GetOwner();
            if (CECooldowns.CheckCD("BloodthirstyRage", 30)) {
                plr.Entropy().BloodthirstyEffect = float.Min(16f, plr.Entropy().BloodthirstyEffect + 0.8f);
            }
        }
    }
}
