using CalamityEntropy.Content.Buffs;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.SharkSquad
{
    /// <summary>
    /// 一次召出整队四只小鲨,每只 2 栏共 8 栏;空余栏位不足 8 或小队已在场时不能使用
    /// 四只要么全在要么全不在,残队由 <see cref="SharkSquadPlayer"/> 清掉
    /// </summary>
    public class SharkSquadStaff : ModItem
    {
        public const int BaseDamage = 140;                     //面板;整队每秒约 7.5 倍面板,约 1050/秒
        public const int UseFrames = 30;                       //使用间隔(帧)
        public const int ManaCost = 10;
        public const float BaseKnockback = 3f;
        public const float AimSpeed = 1f;                      //原版据此算传给 Shoot 的初速,这里不用,照本仓召唤杖给非零值
        public const int SellGold = 10;                        //售价(金币),策划原文
        public const int SquadSize = 4;
        public const float SlotsEach = 2f;                     //每只占的仆从栏
        public const float SquadSlots = SquadSize * SlotsEach; //策划: 整队 8 栏
        public const float SlotEpsilon = 0.001f;               //栏位是浮点数,比较时留的余量
        public const int BuffKickFrames = 3;                   //先给几帧增益,下一帧起由增益自己续
        public const int AltUse = 2;                           //altFunctionUse 的右键值
        private const int DropWidth = 48;                      //掉落碰撞箱(像素),物品图是 70×94
        private const int DropHeight = 64;

        /// <summary>四只出场时相对光标的位置(像素),顺序同 Shoot 里的类型表</summary>
        public static readonly Vector2[] SpawnOffsets = { new(-34f, -22f), new(-34f, 22f), new(34f, 22f), new(34f, -22f) };

        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Type] = SquadSlots;
        }

        public override void SetDefaults() {
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Summon;
            Item.width = DropWidth;
            Item.height = DropHeight;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.knockBack = BaseKnockback;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<SharkSquadHappy>();
            Item.shootSpeed = AimSpeed;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.UseSound = SoundID.Item44;
            Item.autoReuse = false;
            Item.noMelee = true;
            Item.mana = ManaCost;
            Item.buffType = ModContent.BuffType<SharkSquadBuff>();
            Item.rare = ItemRarityID.Blue;
        }

        /// <summary>右键是原版的仆从指定目标(不会走到 Shoot),栏位不够也要能用</summary>
        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == AltUse) {
                return true;
            }
            //slotsMinions 在物品检查前已被清零,已用栏位得自己扫
            float used = 0f;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner != player.whoAmI || !p.minion) {
                    continue;
                }
                if (p.ModProjectile is SharkSquadMinionBase) {
                    return false;
                }
                used += p.minionSlots;
            }
            return player.maxMinions - used + SlotEpsilon >= SquadSlots;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, BuffKickFrames);
            int[] types = {
                ModContent.ProjectileType<SharkSquadHappy>(),
                ModContent.ProjectileType<SharkSquadGrumpy>(),
                ModContent.ProjectileType<SharkSquadLazy>(),
                ModContent.ProjectileType<SharkSquadBrave>()
            };
            Vector2 at = Main.MouseWorld;
            player.LimitPointToPlayerReachableArea(ref at);
            for (int i = 0; i < SquadSize; i++) {
                Projectile.NewProjectile(source, at + SpawnOffsets[i], Vector2.Zero, types[i], Item.damage, knockback, player.whoAmI);
            }
            return false;
        }

        /// <summary>
        /// 拥有者端每帧在弹幕更新之前跑,小队缺了人或召唤上限装不下 8 栏就整队解散
        /// 上限掉了但还装得下整队时先让别的仆从走;原版按弹幕序号杀超栏的那个,轮到小鲨就会拆掉整队
        /// </summary>
        public static void KeepSquadWhole(Player player) {
            int members = 0;
            float others = 0f;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner != player.whoAmI || !p.minion) {
                    continue;
                }
                if (p.ModProjectile is SharkSquadMinionBase) {
                    members++;
                }
                else {
                    others += p.minionSlots;
                }
            }
            if (members == 0) {
                return;
            }
            if (members < SquadSize || player.maxMinions + SlotEpsilon < SquadSlots) {
                Dismiss(player);
                return;
            }
            if (others + SquadSlots <= player.maxMinions + SlotEpsilon) {
                return;
            }
            List<Projectile> spare = new List<Projectile>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.minion && p.minionSlots > 0f && p.ModProjectile is not SharkSquadMinionBase && !IsSegmentedMinion(p.type)) {
                    spare.Add(p);
                }
            }
            spare.Sort((a, b) => a.minionSlots.CompareTo(b.minionSlots));
            foreach (Projectile p in spare) {
                if (others + SquadSlots <= player.maxMinions + SlotEpsilon) {
                    break;
                }
                others -= p.minionSlots;
                p.Kill();
            }
        }

        private static void Dismiss(Player player) {
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == player.whoAmI && p.ModProjectile is SharkSquadMinionBase) {
                    p.Kill();
                }
            }
        }

        /// <summary>星尘龙拆一节会散架,整条留给原版自己的超栏判定</summary>
        private static bool IsSegmentedMinion(int type) {
            return type >= ProjectileID.StardustDragon1 && type <= ProjectileID.StardustDragon4;
        }
    }

    public class SharkSquadBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<SharkSquadHappy>();
    }

    public class SharkSquadPlayer : ModPlayer
    {
        public override void PostUpdate() {
            if (Player.whoAmI == Main.myPlayer) {
                SharkSquadStaff.KeepSquadWhole(Player);
            }
        }
    }
}
