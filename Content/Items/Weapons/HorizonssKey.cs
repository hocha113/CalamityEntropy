using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles.SamsaraCasket;
using CalamityEntropy.Core.CalamityRef;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class NoneTypeDamageClass : DamageClass
    {
        internal static NoneTypeDamageClass Instance;


        public override void Load()
        {
            Instance = this;
        }

        public override void Unload()
        {
            Instance = null;
        }

        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            // 盗贼职业已并入原版投掷,继承比例照旧
            if (damageClass == Throwing)
            {
                return new StatInheritanceData(0.2f, 0.2f, 0.2f, 0.2f, 0.2f);
            }
	    if (damageClass == Summon)
            {
                return new StatInheritanceData(0.3f, 0.3f, 0.3f, 0.3f, 0.3f);
            }
            return StatInheritanceData.Full;
        }
    }
    public class HorizonssKey : ModItem
    {
        // 装灾厄补回成长曲线(棺体/穿甲/暴击/虚空之触/进度提示),职业定位保持 4.0 召唤。
        // 栏位占用 2026-09-11 由 8 降到 4,两个时代同值(SamsaraCasketProj 的 minionSlots 读同一个常量)
        public const int BaseDamage = 50;
        public const float MinionSlotCost = 4f;
        public override bool AltFunctionUse(Player player) => true;
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.noMelee = true;
            Item.value = Item.buyPrice(silver: 1);
            Item.rare = ItemRarityID.Red;
            Item.Entropy().Legend = true;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item.QuickDrawItemWithBloomToWorld(spriteBatch, Color.HotPink, ref scale, rotation);
            return false;
        }
        public override bool? UseItem(Player player)
        {

            if (player.altFunctionUse == 2)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    float dist = 400;
                    int npc = -1;
                    foreach (NPC n in Main.npc)
                    {
                        if (n.active && !n.friendly && CEUtils.getDistance(n.Center, Main.MouseWorld) < dist)
                        {
                            npc = n.whoAmI;
                            dist = CEUtils.getDistance(n.Center, Main.MouseWorld);
                        }
                    }
                    if (npc >= 0)
                    {
                        player.MinionAttackTargetNPC = npc;
                    }
                }
            }
            else
            {
                player.Entropy().samsaraCasketOpened = !player.Entropy().samsaraCasketOpened;
                if (Main.myPlayer == player.whoAmI && player.Entropy().samsaraCasketOpened)
                {
                    int p = Projectile.NewProjectile(player.GetSource_FromAI(), player.Center - new Vector2(0, 60), Vector2.Zero, ModContent.ProjectileType<e0>(), 0, 0, -1);
                    SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AscendantActivate"), player.Center);
                }
            }

            return true;
        }


        public override void HoldItem(Player player)
        {
            player.Entropy().sCasketLevel = GetCasketLevel();
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SamsaraCasketProj>()] < 1
                && player.maxMinions - player.slotsMinions >= MinionSlotCost)
            {
                int p = Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<SamsaraCasketProj>(), Item.damage, player.GetWeaponKnockback(Item), player.whoAmI);
                if (p >= 0 && p < Main.maxProjectiles)
                {
                    Main.projectile[p].originalDamage = Item.damage;
                }
            }
        }

        private static int GetCasketLevel()
        {
            if (!CERef.Has)
            {
                return 6;
            }
            int lv = 0;
            if (CECal.DownedHiveMind || CECal.DownedPerforator)
            {
                lv = 1;
            }
            if (Main.hardMode)
            {
                lv = 2;
            }
            if (NPC.downedPlantBoss)
            {
                lv = 3;
            }
            if (NPC.downedMoonlord)
            {
                lv = 4;
            }
            if (CECal.DownedDoG(EDownedBosses.downedCruiser))
            {
                lv = 5;
            }
            if (CECal.DownedYharon(EDownedBosses.downedCruiser))
            {
                lv = 6;
            }
            return lv;
        }

        public static float getVoidTouchLevel()
        {
            return CERef.Has && EDownedBosses.downedCruiser ? 4 : 0;
        }

        public static int getArmorPen()
        {
            if (!CERef.Has)
            {
                return 50 + 10 * Main.LocalPlayer.Entropy().WeaponBoost;
            }
            int ap = 0;
            if (NPC.downedAncientCultist)
            {
                ap += 20;
            }
            if (CECal.DownedSignus)
            {
                ap += 30;
            }
            ap += 10 * Main.LocalPlayer.Entropy().WeaponBoost;
            return ap;
        }

        public static int getLevel()
        {
            if (!CERef.Has)
            {
                return 20;
            }
            int j = 0;
            if (NPC.downedSlimeKing)
            {
                j++;
            }
            if (NPC.downedBoss1)
            {
                j++;
            }
            if (NPC.downedBoss2)
            {
                j++;
            }
            if (CECal.DownedPerforator || CECal.DownedHiveMind)
            {
                j++;
            }
            if (NPC.downedBoss3)
            {
                j++;
            }
            if (Main.hardMode)
            {
                j++;
            }
            if (CECal.DownedCryogen)
            {
                j++;
            }
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
            {
                j++;
            }
            if (NPC.downedPlantBoss)
            {
                j++;
            }
            if (NPC.downedGolemBoss)
            {
                j++;
            }
            if (NPC.downedAncientCultist)
            {
                j++;
            }
            if (NPC.downedMoonlord)
            {
                j++;
            }
            if (CECal.DownedDragonfolly)
            {
                j++;
            }
            if (CECal.DownedProvidence(EDownedBosses.downedNihilityTwin))
            {
                j++;
            }
            if (CECal.DownedSignus)
            {
                j++;
            }
            if (CECal.DownedPolterghast)
            {
                j++;
            }
            if (CECal.DownedDoG(EDownedBosses.downedCruiser))
            {
                j++;
            }
            if (CECal.DownedYharon(EDownedBosses.downedCruiser))
            {
                j++;
            }
            if (CECal.DownedExoMechs(EDownedBosses.downedCruiser))
            {
                j++;
            }
            if (CECal.DownedCalamitas(EDownedBosses.downedCruiser))
            {
                j++;
            }
            return j;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!CERef.Has)
            {
                return;
            }
            tooltips.Add(new TooltipLine(Mod, "Caskept Level", Mod.GetLocalization("hkLevel") + " " + getLevel().ToString() + "/20"));
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            if (!CERef.Has)
            {
                return;
            }
            float c = 0.0f;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
            {
                c += 15f;
            }
            if (NPC.downedGolemBoss)
            {
                c += 10f;
            }
            if (NPC.downedAncientCultist)
            {
                c += 15f;
            }
            crit += c;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_LoreAwakening))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.FallenStar, 5)
                    .AddIngredient(ItemID.WoodenSword)
                    .AddIngredient(CEID.Item_LoreAwakening)
                    .AddTile(TileID.WorkBenches)
                    .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.BreakerBlade)
                .AddIngredient(ItemID.FragmentStardust, 5)
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation).Register();
        }
    }
}
