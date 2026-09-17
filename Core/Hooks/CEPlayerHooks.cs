using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.Oath;
using CalamityEntropy.Content.Items.Atbm;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Core.Dash;
using CalamityEntropy.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Core.Hooks
{
    /// <summary>
    /// 挂在原版 <see cref="Player"/> 上的全部 On_* 钩子:近战尺寸、手套、水下飞行、暴击上限、
    /// 受伤结算、碰撞矩形、治疗封锁、增益时长、弹药替换、染料读取。
    /// </summary>
    internal sealed class CEPlayerHooks : ICELoader
    {
        //按冷却类处理的增益:这些走 CooldownTimeMult,不吃 DebuffTime
        private static List<int> cooldownBuffs;

        //染料钩子要认的四件装备。在 SetupData 里一次解析,不再每次调用走哨兵判断
        private static int theocracyMarkType;
        private static int rustyDetectionType;
        private static int azafureDetectionType;
        private static int oathBannerType;

        void ICELoader.LoadData() {
            CEDetourRegistry.Add(() => On_Player.ApplyMeleeScale += ApplyMeleeScaleHook, () => On_Player.ApplyMeleeScale -= ApplyMeleeScaleHook);
            CEDetourRegistry.Add(() => On_Player.ApplyEquipFunctional += ApplyEquipFunctionalHook, () => On_Player.ApplyEquipFunctional -= ApplyEquipFunctionalHook);
            CEDetourRegistry.Add(() => On_Player.WaterCollision += WaterCollisionHook, () => On_Player.WaterCollision -= WaterCollisionHook);
            CEDetourRegistry.Add(() => On_Player.GetTotalCritChance += GetTotalCritChanceHook, () => On_Player.GetTotalCritChance -= GetTotalCritChanceHook);
            CEDetourRegistry.Add(() => On_Player.Update_NPCCollision += UpdateNPCCollisionHook, () => On_Player.Update_NPCCollision -= UpdateNPCCollisionHook);
            CEDetourRegistry.Add(() => On_Player.Hurt_HurtInfo_bool += HurtHook, () => On_Player.Hurt_HurtInfo_bool -= HurtHook);
            CEDetourRegistry.Add(() => On_Player.getRect += GetRectHook, () => On_Player.getRect -= GetRectHook);
            CEDetourRegistry.Add(() => On_Player.Heal += HealHook, () => On_Player.Heal -= HealHook);
            CEDetourRegistry.Add(() => On_Player.AddBuff += AddBuffHook, () => On_Player.AddBuff -= AddBuffHook);
            CEDetourRegistry.Add(() => On_Player.PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool += PickAmmoHook,
                () => On_Player.PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool -= PickAmmoHook);
            CEDetourRegistry.Add(() => On_Player.UpdateItemDye += UpdateItemDyeHook, () => On_Player.UpdateItemDye -= UpdateItemDyeHook);
        }

        void ICELoader.SetupData() {
            cooldownBuffs = new List<int>() { BuffID.PotionSickness, BuffID.ChaosState, ModContent.BuffType<DivineShieldCooldown>(), ModContent.BuffType<ShatteredOrb>() };
            theocracyMarkType = ModContent.ItemType<TheocracyMark>();
            rustyDetectionType = ModContent.ItemType<RustyDetectionEquipment>();
            azafureDetectionType = ModContent.ItemType<AzafureDetectionEquipment>();
            oathBannerType = ModContent.ItemType<OathBanner>();
        }

        void ICELoader.UnLoadData() {
            cooldownBuffs = null;
        }

        private static void ApplyMeleeScaleHook(On_Player.orig_ApplyMeleeScale orig, Player self, ref float scale) {
            orig(self, ref scale);
            scale += (self.Entropy().MeleeScale - 1);
        }

        //四种手套原版直接改 scale,这里把它们的加成挪进自有的 MeleeScale,避免与本模组的缩放叠乘
        private static void ApplyEquipFunctionalHook(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual) {
            bool gloveScale = self.meleeScaleGlove;
            orig(self, currentItem, hideVisual);
            if (currentItem.type == ItemID.BerserkerGlove) {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
            if (currentItem.type == ItemID.PowerGlove) {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
            if (currentItem.type == ItemID.MechanicalGlove) {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
            if (currentItem.type == ItemID.FireGauntlet) {
                self.meleeScaleGlove = gloveScale;
                self.Entropy().MeleeScale += 0.1f;
            }
        }

        private static void WaterCollisionHook(On_Player.orig_WaterCollision orig, Player self, bool fallThrough, bool ignorePlats) {
            EModPlayer entropy = self.Entropy();
            if (entropy.MariviniumSet) {
                int num = ((!self.onTrack) ? self.height : (self.height - 20));
                self.velocity = Collision.TileCollision(self.position, self.velocity + new Vector2(0, self.controlDown ? (self.controlJump ? -self.velocity.Y : 5) : 0), self.width, num, fallThrough, ignorePlats, (int)self.gravDir);
                Vector2 vector2 = self.velocity;
                self.position += vector2;
                if (self.wingTime < self.wingTimeMax)
                    self.wingTime = self.wingTimeMax;
            }
            else if (entropy.accAzureAbyss) {
                int num = ((!self.onTrack) ? self.height : (self.height - 20));
                Vector2 vector = self.velocity;
                self.velocity = Collision.TileCollision(self.position, self.velocity, self.width, num, fallThrough, ignorePlats, (int)self.gravDir);
                Vector2 vector2 = self.velocity * 0.7f;
                if (self.velocity.X != vector.X)
                    vector2.X = self.velocity.X;

                if (self.velocity.Y != vector.Y)
                    vector2.Y = self.velocity.Y;

                self.position += vector2;
            }
            else {
                orig(self, fallThrough, ignorePlats);
            }
        }

        private static float GetTotalCritChanceHook(On_Player.orig_GetTotalCritChance orig, Player self, DamageClass damageClass) {
            float rt = orig(self, damageClass);
            if (CalamityEntropy.EntropyMode)
                rt = float.Min(50, rt);
            return rt;
        }

        private static void UpdateNPCCollisionHook(On_Player.orig_Update_NPCCollision orig, Player self) {
            self.Entropy().ApplyScale();
            // 原版冲刺起手帧只在 DashMovement 之后、接触伤害之前可见:暗影披风对原版冲刺的强化在这里接入
            self.GetModPlayer<CEDashPlayer>().PostMovementVanillaCheck();
            orig(self);
            self.Entropy().ResetScale();
        }

        private static void HurtHook(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet) {
            EModPlayer entropy = self.Entropy();
            bool entropyMode = CalamityEntropy.EntropyMode;
            if (entropyMode)
                info.Damage = (int)(info.Damage * 1.25f);
            float num = ModContent.GetInstance<ServerConfig>().LeastDamageSufferedBasedOnMaxHealth;
            if (entropyMode && num < 22)
                num = 22;
            int leastDmg = (int)((num * 0.01f) * self.statLifeMax2);
            // 2026-08-31 平衡案:神谕卡组单次受伤上限改为最大生命66.7%,带5秒内置冷却
            if (entropy.oracleDeck) {
                int cap = (int)(self.statLifeMax2 * 0.667f);
                if (info.Damage > cap && CECooldowns.CheckBMProc("OracleDeckDamageCap", 300)) {
                    info.Damage = cap;
                }
            }
            if (info.Damage < leastDmg)
                info.Damage = leastDmg;

            if (entropy.deusCore && info.Damage > 20) {
                entropy.deusCoreBloodOut += info.Damage - 20;
                info.Damage = 20;
            }
            if (entropy.NihTwinArmorConnetPlayer != -1) {
                if (self.statLife - info.Damage <= 0 && entropy.NihTwinArmorConnetPlayer.ToPlayer().statLife > info.Damage) {
                    if (CECooldowns.CheckCD("NihDamageDeathTrans", 12 * 60)) {
                        CombatText.NewText(self.getRect(), Color.LightBlue, $"{info.Damage}->");
                        info.Cancelled = true;
                        info.Damage = 1;
                        entropy.immune = 120;
                        entropy.NihTwinArmorConnetPlayer.ToPlayer().statLife -= info.Damage;
                        entropy.SyncLife(entropy.NihTwinArmorConnetPlayer.ToPlayer());
                        CEUtils.PlaySound("charm");
                    }
                }
            }
            orig(self, info, quiet);
        }

        private static Rectangle GetRectHook(On_Player.orig_getRect orig, Player self) {
            if (self.GetModPlayer<AtbmPlayer>().Active && Main.netMode != NetmodeID.SinglePlayer) {
                return self.GetModPlayer<AtbmPlayer>().opos.getRectCentered(self.width, self.height);
            }
            Rectangle rect = orig(self);
            float scale = self.Entropy().Scale;
            if (scale != 1)
                rect = rect.Center.ToVector2().getRectCentered(scale * rect.Width, scale * rect.Height);
            return rect;
        }

        private static void HealHook(On_Player.orig_Heal orig, Player self, int amount) {
            if (!(CalamityEntropy.EntropyMode && self.Entropy().HitTCounter > 0)) {
                orig(self, amount);
            }
        }

        private static void AddBuffHook(On_Player.orig_AddBuff orig, Player self, int type, int timeToAdd, bool quiet, bool foodHack) {
            EModPlayer entropy = self.Entropy();
            if (entropy.hasAcc("VastLV4")) {
                if (type == BuffID.ManaSickness) {
                    timeToAdd /= 2;
                }
            }
            bool isDebuff = Main.debuff[type];
            if (isDebuff) {
                if (Main.rand.NextDouble() < entropy.DebuffImmuneChance) {
                    return;
                }
            }
            bool isCooldownBuff = cooldownBuffs != null && cooldownBuffs.Contains(type);
            if (isCooldownBuff) {
                timeToAdd = (int)(timeToAdd * entropy.CooldownTimeMult);
            }
            if (isDebuff && !isCooldownBuff) {
                timeToAdd = (int)(timeToAdd * entropy.DebuffTime);
            }
            orig(self, type, timeToAdd, quiet, foodHack);
        }

        private static void PickAmmoHook(On_Player.orig_PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool orig, Player player, Item item, ref int projToShoot, ref float speed, ref bool canShoot, ref int totalDamage, ref float KnockBack, out int usedAmmoItemId, bool dontConsume) {
            orig(player, item, ref projToShoot, ref speed, ref canShoot, ref totalDamage, ref KnockBack, out usedAmmoItemId, dontConsume);
            if (projToShoot >= 0 && projToShoot != ProjectileID.None) {
                if (item.useAmmo != AmmoID.None && player.Entropy().fruitCake) {
                    if (Fruitcake.ammoList.ContainsKey(item.useAmmo)) {
                        projToShoot = ContentSamples.ItemsByType[Fruitcake.ammoList[item.useAmmo].random<int>()].shoot;
                    }
                }
            }
        }

        private static void UpdateItemDyeHook(On_Player.orig_UpdateItemDye orig, Player self, bool isNotInVanitySlot, bool isSetToHidden, Item armorItem, Item dyeItem) {
            if (!armorItem.IsAir) {
                armorItem.Entropy().DyeType = dyeItem.type;
            }
            if (!dyeItem.IsAir && dyeItem.ModItem != null && dyeItem.ModItem is RoaringDye) {
                self.Entropy().roaringDye = true;
            }
            if (!armorItem.IsAir && armorItem.type == theocracyMarkType) {
                self.GetModPlayer<VanityModPlayer>().TheocrazyDye = dyeItem.IsAir ? 0 : dyeItem.dye;
                self.GetModPlayer<VanityModPlayer>().TheocrazyDyeItemID = dyeItem.type;
            }
            if (!armorItem.IsAir && (armorItem.type == rustyDetectionType || armorItem.type == azafureDetectionType)) {
                self.Entropy().JetpackDye = dyeItem.dye;
            }
            if (!armorItem.IsAir && armorItem.type == oathBannerType) {
                self.Entropy().oathBannerDye = dyeItem.IsAir ? 0 : dyeItem.dye;
            }
            orig(self, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem);
        }
    }
}
