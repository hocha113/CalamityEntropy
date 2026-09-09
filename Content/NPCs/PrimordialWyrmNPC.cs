using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Projectiles.Cruiser;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityEntropy.Content.NPCs
{
    [AutoloadHead]
    public class PrimordialWyrmNPC : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Clothier];
            NPCID.Sets.ExtraFramesCount[Type] = NPCID.Sets.ExtraFramesCount[NPCID.Clothier];
            NPCID.Sets.AttackFrameCount[Type] = NPCID.Sets.AttackFrameCount[NPCID.Clothier];
            NPCID.Sets.DangerDetectRange[Type] = 1000;
            NPCID.Sets.AttackType[Type] = NPCID.Sets.AttackType[NPCID.Clothier];
            NPCID.Sets.AttackTime[Type] = 50;
            NPCID.Sets.AttackAverageChance[Type] = 1;
            NPCID.Sets.MagicAuraColor[base.NPC.type] = Color.Purple;
        }
        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 22;
            NPC.height = 32;
            NPC.aiStyle = 7;
            NPC.damage = 10;
            NPC.defense = 105;
            NPC.lifeMax = 7200000;
            NPC.HitSound = SoundID.NPCHit1;
            // 死亡音效就近取巡游者死亡爆发音
            NPC.DeathSound = CEUtils.GetSound("VoidAttack");
            NPC.knockBackResist = 0f;
            AnimationType = NPCID.Clothier;
        }
        public override bool PreAI()
        {
            dcd--;
            return base.PreAI();
        }
        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            // 灾厄在场读渊海灾虫,缺席回落巡游者
            if (CECal.DownedPrimordialWyrm)
            {
                return true;
            }
            return false;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            {
                if (Main.rand.NextBool(6))
                {
                    string dns = "";
                    var lc = new List<string>();
                    foreach (string s in Donators.Donors)
                    {
                        lc.Add(s);
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        int d = Main.rand.Next(lc.Count);
                        dns += lc[d];
                        if (i < 15)
                        {
                            dns += ", ";
                        }
                        lc.RemoveAt(d);
                    }
                    chat.Add(Mod.GetLocalization("WyrmChatDonors").Value.Replace("[0]", dns));
                    return chat;
                }
                if (!Main.bloodMoon && !Main.eclipse)
                {
                    if (NPC.homeless)
                    {
                        chat.Add(Mod.GetLocalization("WyrmChatNoHome").Value);
                    }
                    else
                    {
                        chat.Add(Mod.GetLocalization("WyrmChat" + Main.rand.Next(1, 12).ToString()).Value);
                        if (Main.raining)
                            chat.Add(Mod.GetLocalization("WyrmChatRain" + Main.rand.Next(1, 4).ToString()).Value);
                    }
                }
                else
                {
                    if (Main.eclipse)
                    {
                        chat.Add(Mod.GetLocalization("WyrmChatEclipse1").Value);
                        chat.Add(Mod.GetLocalization("WyrmChatEclipse2").Value);
                    }
                    if (Main.bloodMoon)
                    {
                        chat.Add(Mod.GetLocalization("WyrmChatBloodMoon1").Value);
                        chat.Add(Mod.GetLocalization("WyrmChatBloodMoon2").Value);
                    }
                }
                return chat;
            }
        }
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            button2 = Mod.GetLocalization("SpecialThanks").Value;
        }
        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {

            if (firstButton)
            {
                shopName = ShopName;
            }
            else
            {
                string chat = "";
                string dns = "";
                var lc = new List<string>();
                foreach (string s in Donators.Donors)
                {
                    lc.Add(s);
                }
                for (int i = 0; i < 25; i++)
                {
                    int d = Main.rand.Next(lc.Count);
                    dns += lc[d];
                    if (i < 24)
                    {
                        dns += ", ";
                    }
                    lc.RemoveAt(d);
                }
                chat = Mod.GetLocalization("WyrmChatDonors").Value.Replace("[0]", dns);
                Main.npcChatText = chat;
            }
        }
        public static string ShopName = "Shop";
        public override void AddShops()
        {
            NPCShop npcShop = new NPCShop(Type, ShopName)
                .Add<WyrmTooth>()
                .Add<VoidBar>()
                .Add<NihilityFragments>()
                .Add<WraithSoulEssence>();
            AddCalOrOwn(npcShop, CEID.Item_Lumenyl, ItemID.LunarOre);
            npcShop.Add(ItemID.SuperHealingPotion);
            AddCalOrOwn(npcShop, CEID.Item_GrandDad, ItemID.Celeb2);
            AddCalOrOwn(npcShop, CEID.Item_EidolicWail, ItemID.LastPrism);
            AddCalOrOwn(npcShop, CEID.Item_EidolonStaff, ItemID.LunarFlareBook);
            AddCalOrOwn(npcShop, CEID.Item_Valediction, ItemID.PaladinsHammer);
            AddCalOrOwn(npcShop, CEID.Item_VoidTorch, ItemID.BoneTorch);
            AddCalOrOwn(npcShop, CEID.Item_AbyssShellFossil, ItemID.FossilOre, 50);
            AddCalOrOwn(npcShop, CEID.Item_ReaperTooth, ItemID.SharkToothNecklace);
            AddCalOrOwn(npcShop, CEID.Item_BobbitHook, ItemID.StaticHook);
            npcShop.Add(ItemID.MusicBoxBoss5);
            npcShop.Add<BookmarkMarivium>();
            AddCal(npcShop, CEID.Item_CalamarisLament);
            AddCal(npcShop, CEID.Item_DeepSeaDumbbell);
            AddCal(npcShop, CEID.Item_HalibutCannon);
            AddCal(npcShop, CEID.Item_DepthCells);
            AddCal(npcShop, CEID.Item_PlantyMush);
            AddCal(npcShop, CEID.Item_AbyssalTreasure);
            if (CERef.Has && CEID.Item_Rock > 0)
            {
                npcShop.Add(CEID.Item_Rock, new Condition(Mod.GetLocalization("PassedBossRush"), () => CECal.DownedBossRush));
            }
            npcShop.Register();
        }

        //货架条目:灾厄在场且该内容存在时上架灾厄商品,否则上架 4.0 的替身
        private static NPCShop AddCalOrOwn(NPCShop shop, int calType, int ownType, int ownStack = 1)
        {
            if (CERef.Has && calType > 0)
            {
                return shop.Add(calType);
            }
            if (ownStack > 1)
            {
                return shop.Add(new Item(ownType, ownStack));
            }
            return shop.Add(ownType);
        }

        private static void AddCal(NPCShop shop, int calType)
        {
            if (CERef.Has && calType > 0)
            {
                shop.Add(calType);
            }
        }

        public override void ModifyActiveShop(string shopName, Item[] items)
        {
            foreach (Item item in items)
            {
                if (item == null || item.type == ItemID.None)
                {
                    continue;
                }

                if (CEID.Item_Rock > 0 && item.type == CEID.Item_Rock)
                {
                    item.shopCustomPrice = 100000000;
                    continue;
                }

                if (item.type == ModContent.ItemType<BookmarkMarivium>())
                {
                    item.shopCustomPrice = Item.buyPrice(platinum: 45);
                    continue;
                }

                int value = item.shopCustomPrice ?? item.value;
                item.shopCustomPrice = value / 8;
            }
        }
        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = Main.zenithWorld ? 2000 : 700;

            knockback = 3f;

        }
        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 15;
        }

        public int dcd = 0;
        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            if (CERef.Has && CEID.Proj_EidolicWailSoundwave > 0)
            {
                projType = CEID.Proj_EidolicWailSoundwave;
            }
            else
            {
                projType = ModContent.ProjectileType<CruiserLaser2>();
            }
            attackDelay = 4;
            if (dcd <= 0)
            {
                var sd = CEUtils.GetSound("he" + (Main.rand.NextBool() ? 1 : 3).ToString());
                sd.MaxInstances = 6;
                SoundEngine.PlaySound(in sd, NPC.Center);
                dcd = 59;
            }
        }
        public override void PostAI()
        {
            // 巡游者激光默认敌对且以 ai[0] 绑定所有者 NPC；城镇攻击 AI 生成后在此改挂
            // 本模组统一友好化通道 ToFriendly（每帧强制转友方并随 ExtraAI 同步），并解除所有者绑定
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.type == ModContent.ProjectileType<CruiserLaser2>() && proj.npcProj && !proj.Entropy().ToFriendly)
                {
                    proj.Entropy().ToFriendly = true;
                    // 驯服弹幕默认吃 16 倍增伤（FriendFinder 宠物专用），城镇攻击保持面板伤害，须关掉
                    proj.Entropy().dmgUpFrd = false;
                    proj.ai[0] = -1;
                    proj.netUpdate = true;
                }
            }
        }
        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 14.5f;
            if (Main.zenithWorld)
            {
                multiplier = 32;
            }
            gravityCorrection = 0f;
            randomOffset = 0f;
        }
        public override void TownNPCAttackMagic(ref float auraLightMultiplier)
        {
            auraLightMultiplier = 2f;
        }
    }
}
