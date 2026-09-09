using CalamityEntropy.Common.LoreReworks;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Dash;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.ILEditing;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Accessories.Cards;
using CalamityEntropy.Content.Items.Accessories.EvilCards;
using CalamityEntropy.Content.Items.Accessories.Hungry;
using CalamityEntropy.Content.Items.Accessories.Oath;
using CalamityEntropy.Content.Items.Accessories.SoulCards;
using CalamityEntropy.Content.Items.Armor.Marivinium;
using CalamityEntropy.Content.Items.Armor.NihTwins;
using CalamityEntropy.Content.Items.Armor.Smoldering;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Donator.Ratziel;
using CalamityEntropy.Content.Items.Lores;
using CalamityEntropy.Content.Items.Pets.Glue;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Items.Weapons.AzafureLightMachineGun;
using CalamityEntropy.Content.Items.Weapons.Bait;
using CalamityEntropy.Content.Items.Weapons.DustCarverBow;
using CalamityEntropy.Content.Items.Weapons.Fractal;
using CalamityEntropy.Content.Items.Weapons.Whips;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.NPCs.FriendFinderNPC;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Prefixes;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using CalamityEntropy.Content.Projectiles.HBProj;
using CalamityEntropy.Content.Projectiles.SamsaraCasket;
using CalamityEntropy.Content.Projectiles.VoidEchoProj;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Content.UI;
using CalamityEntropy.Content.UI.Poops;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Audio;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Common
{
    public class EModPlayer : ModPlayer
    {
        //虚寂之绳贴图在加载期就位,只在客户端绘制路径读取,专用服务器上恒为 null
        [VaultLoaden("CalamityEntropy/Content/NPCs/NihilityTwin/NihRope")]
        internal static Asset<Texture2D> NihRopeTex;
        public float GetPressure()
        {
            float p = 1;
            return p;
        }
        public float healPoints = 0;
        public void HealFloat(float amount)
        {
            int healInt = (int)amount;
            if (healInt > 0)
            {
                Player.Heal(healInt);
            }
            if (amount - healInt > 0)
                healPoints += amount - healInt;
            if (healPoints >= 1)
            {
                healPoints--;
                Player.Heal(1);
            }
        }
        public Vector2 floweryPosition = Vector2.Zero;
        public int voidOreNearby = 0;
        public bool rottenFangs = false;
        public float alpha = 1f;
        public float CooldownTimeMult = 1;
        public List<int> enabledLoreItems = new List<int>();
        public bool NihilityTwinLoreBonus = false;
        public bool ProphetLoreBonus = false;
        public float voidResistance = 0f;
        public int itemTime = 0;
        public bool CruiserLoreBonus = false;
        public bool Godhead = false;
        public bool auraCard = false;
        public int brillianceCard = 0;
        public bool enduranceCard = false;
        public bool entityCard = false;
        public bool inspirationCard = false;
        public bool metropolisCard = false;
        public bool radianceCard = false;
        public bool temperanceCard = false;
        public bool wisdomCard = false;
        public bool oracleDeck = false;
        public bool holyMoonlight = false;
        public int MagiShield;
        public static int localPlayerMaxShield = 0;
        public int HMRegenCd = 0;
        public int pot_time = 0;
        public int pot_amp = 0;
        public bool oracleDeckInInv = false;
        public bool taintedDeckInInv = false;
        public bool hasSM = false;
        public bool yuzuCheck = false;
        public bool summonerVF;
        public bool magiVF;
        public bool rogueVF;
        public bool meleeVF;
        public bool rangerVF;
        public bool VFSet;
        public float FallSpeed = 1;
        public bool VFLeg;
        public bool VFHelmRanged;
        public bool VFHelmMagic;
        public bool VFHelmSummoner;
        public bool VFHelmRogue;
        public bool VFHelmMelee;
        /// <summary>魔力病持续时间减半(虚灵宙法盔/圣洁月光共用;2026-08-31 平衡案)。</summary>
        public bool halfManaSick;
        /// <summary>上神之佑团队免伤光环持有者标记(2026-08-31 平衡案)。</summary>
        public bool odinAura;
        /// <summary>莱拉蜂蜜光环持有者标记(2026-08-31 平衡案)。</summary>
        public bool leylaAura;
        /// <summary>蚀之窃贼怀表持有标记(死亡复活,2026-08-31 平衡案)。</summary>
        public bool thiefWatch;
        // 书签玩家侧被动票据:由各书签 BookUpdate 每帧续期,消费处自行倒数(2026-08-31 平衡案)
        public int bmLightJumpTime;
        public int bmTerraDefTime;
        public int bmSilvaRegenTime;
        public int bmSagSpawnTime;
        private int manaSickPrev = 0;
        /// <summary>虚灵宙法盔:攻击敌人后的强再生剩余帧(5hp/s)。</summary>
        public int cosmosRegenTime = 0;
        public bool mariviniumBody = false;
        public int vfcd = 0;
        public float voidcharge = 0;
        public float damageReduce = 1;
        public float moveSpeed = 0;
        public float ManaCost = 1;
        public float Thorn = 0;
        public float WingSpeed = 1;
        public bool GodHeadVisual = false;
        public bool samsaraCasketOpened = false;
        public int sCasketLevel = 0;
        public int SacredJudgeShields = 2;
        public float screenShift = 0;
        public bool holyMantle = false;
        public int mantleCd = 0;
        public bool HolyShield = false;
        public Vector2 screenPos = Vector2.Zero;
        public int WeaponBoost = 0;
        public bool CrPlush = false;
        public bool brokenAnkh = false;
        public bool reincarnationBadge = false;
        public bool nihShell = false;
        public List<Poop> poops = new List<Poop>();
        public int MaxPoops = 19;
        public Poop PoopHold = null;
        public bool _holdingPoop;
        public int holyGroundTime = 0;
        public float dodgeChance = 0;
        public bool mawOfVoid = false;
        public float serviceWhipDamageBonus = 0;
        public bool deusCore = false;
        public bool heartOfStorm = false;
        public int ffinderCd = 0;
        public float temporaryArmor = 0;
        public bool vetrasylsEye = false;
        public int XSpeedSlowdownTime = 0;
        public IEnumerable<KeyValuePair<string, Vector2>> homes = new Dictionary<string, Vector2>();
        public int AzChargeShieldSteamTime = 0;
        public Item foreseeOrbItem = null;
        public int CruiserAntiGravTime = 0;
        public int gravAddTime = 0;
        public bool plagueEngine = false;
        public int UsingItemCounter = 0;
        public float VanityTailRot = 0;
        public float FlagRot = 0;
        public int JetpackDye = -1;
        public int StealthRegenDelay = 0;
        public bool CanSlainTownNPC = false;
        internal int koishiStabTimer = 0;
        public int MewmewSmokeEffect = 0;

        public List<bool> drCrystals = null;

        public class SpecialWingDrawingData
        {
            public int MaxFrame = 3;
            public int SlowFallFrame = 2;
            public int FrameCount = 0;
        }
        public SpecialWingDrawingData wingData = new SpecialWingDrawingData();

        public class EquipInfo
        {
            public string id;
            public bool visual;
            public bool hasEffect;
            public EquipInfo(string Id, bool hasVisual = true, bool effect = true)
            {
                id = Id;
                hasEffect = effect;
                visual = hasVisual;
            }
        }
        public int HealingCd = 0;
        public bool TryHealMeWithCd(int amount, int cd = 12)
        {
            if (HealingCd <= 0)
            {
                HealingCd = cd;
                Player.Heal(amount);
                return true;
            }
            return false;
        }
        public List<EquipInfo> equipAccs = new List<EquipInfo>();
        public bool holdingPoop { get { return _holdingPoop; } set { if (Player.whoAmI == Main.myPlayer && value != _holdingPoop) { syncHoldingPoop = true; } _holdingPoop = value; } }
        public float CasketSwordRot { get { return (float)effectCount * 0.12f; } }
        public float VoidCharge
        {
            get { return voidcharge; }
            set
            {
                if (value < voidcharge)
                {
                    voidcharge = value;
                }
                else if (vfcd <= 0)
                {
                    voidcharge = value; vfcd = 8;
                }
            }
        }
        public bool CRing = false;
        public bool LastStand = false;
        public int lastStandCd = 0;
        public bool ilmeranAsylum = false;

        public bool accWispLantern = false;
        public bool visualWispLantern = false;

        public int HammerStrikeTimes = 0;
        
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            // 2026-08-31 平衡案:蚀之窃贼怀表——死亡后复活并回复100生命,120秒冷却
            if (thiefWatch && CECooldowns.CheckBMProc("ThiefWatchRevive", 120 * 60))
            {
                Player.statLife = 100;
                Player.HealEffect(100);
                immune = 120;
                CEUtils.PlaySound("ARCD", 1.2f, Player.Center, 6, 0.8f);
                return false;
            }
            deusCoreBloodOut = 0;
            if (Player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
            {
                var rs = PlayerDeathReason.ByCustomReason(Mod.GetLocalization("LilyDeath" + Main.rand.Next(2).ToString()).ToNetworkText(Player.name));
                damageSource = rs;
            }
            bool CruiserDeathText = false;
            if(damageSource.SourceProjectileType > 0)
            {
                if(CELists.CruiserSpecificDeathProjs.Contains(damageSource.SourceProjectileType))
                {
                    CruiserDeathText = true;
                }
            }
            if(damageSource.SourceNPCIndex >= 0)
            {
                if (Main.npc[damageSource.SourceNPCIndex].active && CELists.CruiserSegs.Contains(Main.npc[damageSource.SourceNPCIndex].type))
                    CruiserDeathText = true;
            }
            if(CruiserDeathText)
            {
                damageSource = PlayerDeathReason.ByCustomReason(Mod.GetLocalization("VoidDeath" + Main.rand.Next(6).ToString()).ToNetworkText(Player.name));
            }

            if (immune > 0)
            {
                if (Player.statLife < 6)
                {
                    Player.statLife = 6;
                }
                return false;
            }
            if (SacredJudgeShields > 0 && Player.ownedProjectileCounts[ModContent.ProjectileType<SacredJudge>()] > 0)
            {
                SacredJudgeShields -= 1;
                if (Player.statLife < 6)
                {
                    Player.statLife = 6;
                }
                immune = 120;
                return false;
            }
            if (LastStand && lastStandCd <= 0)
            {
                playSound = false;
                genDust = false;
                lastStandCd = (int)(4000 * CooldownTimeMult);
                Player.Heal(100);
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/holyshield_shatter") { Volume = 0.6f }, Player.Center);
                return false;
            }
            if (!Player.HasCooldown(ScytheReviveCooldown.ID) && Player.HeldItem.ModItem is TlipocasScythe && TlipocasScythe.AllowRevive())
            {
                Player.AddCooldown(ScytheReviveCooldown.ID, 5 * 60 * 60);
                Player.statLife = Player.statLifeMax2 / 2;
                CEUtils.PlaySound("beast_lavaball_rise1", 1, Player.Center);
                return false;
            }
            return true;
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            NihTwinArmorConnetPlayer = -1;
            if (Player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
            {
                var st = SoundID.PlayerKilled;
                st.MaxInstances = 1;
                st.Volume = 0;
                SoundEngine.PlaySound(st);
                CEUtils.PlaySound("llDeath", 1);
                for (int i = 0; i < Main.gore.Length; i++)
                {
                    Main.gore[i].active = false;
                }
                for (int i = 0; i < Main.dust.Length; i++)
                {
                    if (Main.dust[i].type == DustID.Blood)
                    {
                        Main.dust[i].active = false;
                    }
                }
            }
            voidcharge = 0; VoidInspire = 0; lastStandCd = 0; mantleCd = 0; magiShieldCd = 0; sJudgeCd = 2;

            respawnsnd = true;
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<Flowery>()] > 0)
                CEUtils.PlaySound("VoiceClips/Death", 1, floweryPosition);
        }
        public bool respawnsnd = false;
        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            //脱离灾厄:SilvasCrown 原有的灾厄防御损伤(defenseDamageRatio)联动已退役
            if (Thorn > 0)
            {
                int thornDamage = (int)Math.Min(hurtInfo.Damage * Thorn, 1000);
                Player.ApplyDamageToNPC(npc, thornDamage, 0, 0, false);
            }
        }
        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
        {
            //脱离灾厄:SilvasCrown 原有的灾厄防御损伤(defenseDamageRatio)联动已退役
            if (CELists.CruiserSpecificDeathProjs.Contains(proj.whoAmI))
                Player.AddBuff(ModContent.BuffType<VoidTouch>(), 200);
        }
        public bool SCrown;

        public int VoidInspire = 0;

        public bool GreedCard = false;
        public bool FrailCard = false;
        public bool BarrenCard = false;
        public bool TarnishCard = false;
        public bool ConfuseCard = false;
        public bool PerplexedCard = false;
        public bool SacrificeCard = false;
        public bool NothingCard = false;
        public bool FoolCard = false;
        public bool EvilDeck = false;

        public bool ashesCore = false;
        public int lifeRegenPerSec = 0;
        public int lifeRegenCD = 60;
        public float light = 0;
        public float AttackVoidTouch = 0;
        public float DebuffImmuneChance = 0;
        public float shootSpeed = 1;
        /// <summary>强化魔力的百分比部分(按常规魔力上限乘算)。</summary>
        public float enhancedMana = 0;
        /// <summary>强化魔力的固定点数部分(大魔导师手镜等直接加点)。</summary>
        public int enhancedManaFlat = 0;
        /// <summary>任一来源提供了强化魔力,资源条金色段与悬停文本据此显示。</summary>
        public bool HasEnhancedMana => enhancedMana > 0 || enhancedManaFlat > 0;
        public bool sacrMask = false;
        public int voidshadeBoostTime = 0;
        public float mawOfVoidCharge = 0;
        public bool mawOfVoidUsing = false;
        public bool revelation = false;
        public float revelationCharge = 0;
        public bool revelationUsing = false;
        public int deusCoreBloodOut = 0;
        public int summonCrit = 0;
        public float meleeDamageReduce = 0;
        public int hitTimeCount = 10000000;

        public bool isUsingItem()
        {
            return Main.mouseLeft && !Player.mouseInterface && Player.HeldItem.damage > 0 && Player.HeldItem.active;
        }
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            modifiers.SourceDamage *= (1 - meleeDamageReduce);
        }
        public override void PostUpdateBuffs()
        {
            // 魔力病持续时间减半(帧间跳变侦测新施加;虚灵宙法盔/圣洁月光)
            int msIdx = Player.FindBuffIndex(BuffID.ManaSickness);
            int msCur = msIdx >= 0 ? Player.buffTime[msIdx] : 0;
            if (halfManaSick && msIdx >= 0 && msCur > manaSickPrev + 1)
            {
                int halved = System.Math.Max(manaSickPrev - 1, msCur / 2);
                Player.buffTime[msIdx] = halved;
                msCur = halved;
            }
            manaSickPrev = msCur;
        }
        public bool MariviniumSet = false;
        public void addEquip(string id, bool hasVisual = true)
        {
            equipAccs.Add(new EquipInfo(id, hasVisual, true));
        }
        public void addEquipVisual(string id)
        {
            equipAccs.Add(new EquipInfo(id, true, false));
        }
        public bool hasAcc(string id)
        {
            foreach (var i in equipAccs)
            {
                if (i.id == id && i.hasEffect)
                {
                    return true;
                }
            }
            return false;
        }
        public bool hasAccVisual(string id)
        {
            foreach (var i in equipAccs)
            {
                if (i.id == id && i.visual)
                {
                    return true;
                }
            }
            return false;
        }
        public bool foreseeOrbLast = false;

        //悲恸的属性记录
        public float McDefense = 0;
        public float McRegen = 0;
        public float McEndurance = 0;

        public class McAttributeRecord
        {
            public static float dec = 0.015f;
            public McAttributeRecord(DamageClass dmgc)
            {
                dmgClass = dmgc;
            }
            public DamageClass dmgClass;
            public float McDamage = 1;
            public float McCrit = 0;
            public void Record(Player player)
            {
                if (McDamage < player.GetDamage(dmgClass).Additive)
                {
                    McDamage = player.GetDamage(dmgClass).Additive;
                }
                if (McCrit < player.GetCritChance(dmgClass))
                {
                    McCrit = player.GetCritChance(dmgClass);
                }
            }

            public void UpdateAtrLossing(Player player)
            {
                McCrit += (player.GetCritChance(dmgClass) - McCrit) * dec;
                McDamage += (player.GetDamage(dmgClass).Additive - McDamage) * dec;
            }

            public void SetStats(Player player)
            {
                if (player.GetCritChance(dmgClass) < McCrit)
                    player.GetCritChance(dmgClass) = McCrit;
                if (player.GetDamage(dmgClass).Additive < McDamage)
                    player.GetDamage(dmgClass) += McDamage - player.GetDamage(dmgClass).Additive;
            }
        }
        #region Misc
        public List<McAttributeRecord> McAttributes = null;
        public bool BaitCharging = false;
        public bool devouringCard = false;
        //脱离灾厄:潜行退役惰性字段(NoNaturalStealthRegen/ExtraStealthBar/ExtraStealth/worshipRelic/worshipStealthRegenTime/shadowPact)已裁删
        public bool shadowRune = false;
        public float ManaExtraHeal = 0f;
        public int ManaRegenPer30Tick = 0;
        public int ManaRegenTime = 0;
        public Dictionary<DamageClass, float> CritDamage;
        public bool smolderingSet = false;
        public bool fruitCake;
        public bool roaringDye = false;
        public float LifeStealP = 0;
        public float SnowgraveCharge = 0;
        public int SnowgraveChargeTime = 0;
        public bool SulphurousBubble = false;
        public int SulphurousBubbleRecharge = 3600;
        public int DontDrawTime = 0;
        public int AzureRapierBlock = 0;
        public int HeatEffectTime = 0;
        public int AdditionalBookmarkSlot = 0;
        public List<Texture2D> BookmarkHolderSpecialTextures = new();
        public int DriverShield = 0;
        public int DriverRecharge = 0;
        public int DriverRegenDelay = 0;

        public int NihilityShield = 0;
        public int NihilityRecharge = 0;
        public int NihilityRegenDelay = 0;

        public int VoidShield = 0;
        public int VoidRecharge = 0;
        public int VoidRegenDelay = 0;

        public int RatzielShield = 0;
        public int RatzielRecharge = 0;
        public int RatzielRegenDelay = 0;

        public bool NihilityShieldEnabled = false;
        public bool NihilitySet = false;
        public float NihShieldScale = 0;
        public float VoidCoreShieldScale = 0;
        public bool ChaoticSet = false;
        public bool SpawnChaoticCellOnHurt = false;
        public NPC lastHitTarget = null;
        public int ShootLaserTime = 0;
        public float DriverScale = 0;
        public bool DriverShieldVisual = false;
        public bool VoidShieldVisual = false;
        public float Scale = 1;
        public int oWidth = 20;
        public int oHeight = 42;
        public float ScaleTarget = 1;
        public float snowgrave = 0;
        public bool NoAdrenaline = false;
        public int NoAdrenalineTime = 0;
        public float EDamageReduce = 0;
        public int NoPlatformCollide = 0;
        public bool exquisiteCrown = false;
        public bool oathBanner = false;
        public bool oathBannerVisual = false;
        public int oathBannerDye = 0;
        public int OathBannerFrameCount = 0;
        #endregion
        public void UpdateDriverShield()
        {
            bool Equiped = AzafureDriverShieldItem != null;
            DriverScale = float.Lerp(DriverScale, Equiped ? (DriverShield > 0 ? 2.5f : 1.9f) : 0, 0.05f);
            if (Equiped)
            {
                if (Player.EntropyCooldowns().cooldowns.TryGetValue(DriverCoreCooldown.ID, out var value4))
                {
                    value4.timeLeft = DriverShield > 0 ? (AzafureDriverCore.MaxShield - DriverShield) : AzafureDriverCore.RechargeTime - DriverRecharge;
                    if (DriverShield > 0)
                    {
                        value4.duration = AzafureDriverCore.MaxShield;
                    }
                    else
                    {
                        value4.duration = AzafureDriverCore.RechargeTime;
                    }
                }
                else
                {
                    Player.AddCooldown(DriverCoreCooldown.ID, AzafureDriverCore.RechargeTime);
                }
                if (DriverShield > 0)
                {
                    if (DriverRegenDelay-- < 0)
                    {
                        if (DriverShield < AzafureDriverCore.MaxShield)
                        {
                            int dl = Player.AzafureEnhance() ? 4 : 5;
                            if (DriverRegenDelay < dl)
                                DriverRegenDelay = dl;
                            DriverShield += 1;
                            if (DriverShield > AzafureDriverCore.MaxShield)
                            {
                                DriverShield = AzafureDriverCore.MaxShield;
                            }
                        }
                    }
                    DriverRecharge = 0;
                }
                else
                {
                    DriverRecharge += Player.AzafureEnhance() ? Main.rand.Next(1, 3) : 1;
                    if (DriverRecharge > AzafureDriverCore.RechargeTime)
                    {
                        CEUtils.PlaySound("RoverDriveActivate", 1.4f, Player.Center);
                        DriverShield = AzafureDriverCore.MaxShield;
                    }
                }
            }
            else
            {
                DriverRecharge = 0;
                DriverShield = 0;
                DriverRegenDelay = 5 * 60;
                RemoveCooldown(DriverCoreCooldown.ID);
            }
        }

        public void UpdateNihShield()
        {
            bool Equiped = NihilityShieldEnabled;

            NihShieldScale = float.Lerp(NihShieldScale, Equiped ? (NihilityShield > 0 ? (0.6f + 0.4f * ((float)NihilityShield / VoidEaterHelmet.MaxShield)) : 0.5f) : 0, 0.05f);
            if (Equiped)
            {
                if (Player.EntropyCooldowns().cooldowns.TryGetValue(NihilityShieldCD.ID, out var value4))
                {
                    value4.timeLeft = NihilityShield > 0 ? (VoidEaterHelmet.MaxShield - NihilityShield) : VoidEaterHelmet.ShieldRecharge - NihilityRecharge;
                    if (NihilityShield > 0)
                    {
                        value4.duration = VoidEaterHelmet.MaxShield;
                    }
                    else
                    {
                        value4.duration = VoidEaterHelmet.ShieldRecharge;
                    }
                }
                else
                {
                    Player.AddCooldown(NihilityShieldCD.ID, VoidEaterHelmet.ShieldRecharge);
                }
                if (NihilityShield > 0)
                {
                    if (NihilityRegenDelay-- < 0)
                    {
                        if (NihilityShield < VoidEaterHelmet.MaxShield)
                        {
                            if (NihilityRegenDelay < 4)
                                NihilityRegenDelay = 4;
                            NihilityShield += 1;
                        }
                    }
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        int lifeToShield = 80;
                        if (ArmorSetBonusHotKey.JustPressed && NihilityShield < VoidEaterHelmet.MaxShield && Player.statLife > lifeToShield)
                        {
                            if (CECooldowns.CheckCD("NihilitySet", 60))
                            {
                                Player.statLife -= lifeToShield;
                                SyncLife();
                                NihilityShield += lifeToShield;
                                CEUtils.PlaySound("ARCD", 1.6f, Player.Center, 6, 0.8f);
                                for (int i = 0; i < 16; i++)
                                {
                                    Vector2 pos = CEUtils.randomPointInCircle(160) + Player.velocity * 12;
                                    PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center + pos, pos * -0.15f, new Color(120, 120, 255), Main.rand.NextFloat(4f, 5f)).Configure(7);
                                }
                            }
                        }
                    }
                    if (NihilityShield > VoidEaterHelmet.MaxShield)
                    {
                        NihilityShield = VoidEaterHelmet.MaxShield;
                    }
                    NihilityRecharge = 0;
                }
                else
                {
                    NihilityRecharge += 1;
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        int lifeToShield = 80;
                        if (ArmorSetBonusHotKey.JustPressed && Player.statLife > lifeToShield)
                        {
                            if (CECooldowns.CheckCD("NihilitySet", 60))
                            {
                                Player.statLife -= lifeToShield;
                                SyncLife();
                                NihilityRecharge += 300;
                                CEUtils.PlaySound("ARCD", 1.6f, Player.Center, 6, 0.8f);
                                for (int i = 0; i < 16; i++)
                                {
                                    Vector2 pos = CEUtils.randomPointInCircle(160) + Player.velocity * 12;
                                    PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center + pos, pos * -0.15f, new Color(120, 120, 255), Main.rand.NextFloat(4f, 5f)).Configure(7);
                                }
                            }
                        }
                    }
                    if (NihilityRecharge > VoidEaterHelmet.ShieldRecharge)
                    {
                        CEUtils.PlaySound("vp_use", 0.85f, Player.Center, 6, 0.8f);
                        NihilityShield = VoidEaterHelmet.MaxShield / 10;
                    }
                }
            }
            else
            {
                NihilityRecharge = 0;
                NihilityShield = 0;
                NihilityRegenDelay = 5 * 60;
                RemoveCooldown(NihilityShieldCD.ID);
            }
        }
        public void UpdateVoidShield(bool Equiped, string CooldownID, ref float Scale, ref int shieldCount, ref int regenDelay, ref int rechargeCounter, int MaxShield, int RechargeTime)
        {
            // 修复:此处曾误用 Scale/MaxShield(恒近0),护盾视觉不随护盾量变化,观感上"护盾没生成"
            Scale = float.Lerp(Scale, Equiped ? (shieldCount > 0 ? (0.6f + 0.4f * ((float)shieldCount / MaxShield)) : 0.5f) : 0, 0.05f);
            if (Equiped)
            {
                if (Player.EntropyCooldowns().cooldowns.TryGetValue(CooldownID, out var value4))
                {
                    value4.timeLeft = shieldCount > 0 ? (MaxShield - shieldCount) : RechargeTime - rechargeCounter;
                    if (shieldCount > 0)
                    {
                        value4.duration = MaxShield;
                    }
                    else
                    {
                        value4.duration = RechargeTime;
                    }
                }
                else
                {
                    Player.AddCooldown(CooldownID, RechargeTime);
                }
                if (shieldCount > 0)
                {
                    if (regenDelay-- < 0)
                    {
                        if (shieldCount < MaxShield)
                        {
                            if (regenDelay < 4)
                                regenDelay = 4;
                            shieldCount += 1;
                        }
                    }
                    if (shieldCount > MaxShield)
                    {
                        shieldCount = MaxShield;
                    }
                    rechargeCounter = 0;
                }
                else
                {
                    rechargeCounter += 1;
                    if (rechargeCounter > RechargeTime)
                    {
                        CEUtils.PlaySound("vp_use", 0.85f, Player.Center, 6, 0.8f);
                        shieldCount = MaxShield / 2;
                    }
                }
            }
            else
            {
                rechargeCounter = 0;
                shieldCount = 0;
                if (regenDelay < 2 * 60)
                    regenDelay = 2 * 60;
                RemoveCooldown(CooldownID);
            }
        }
        public void UpdateRatzielShield(bool Equiped, string CooldownID, ref float Scale, ref int shieldCount, ref int regenDelay, ref int rechargeCounter, int MaxShield, int RechargeTime)
        {
            Scale = float.Lerp(Scale, Equiped ? (shieldCount > 0 ? (0.6f + 0.4f * ((float)shieldCount / MaxShield)) : 0.5f) : 0, 0.05f);
            if (Equiped)
            {
                if (Player.EntropyCooldowns().cooldowns.TryGetValue(CooldownID, out var value4))
                {
                    value4.timeLeft = shieldCount > 0 ? (MaxShield - shieldCount) : RechargeTime - rechargeCounter;
                    if (shieldCount > 0)
                    {
                        value4.duration = MaxShield;
                    }
                    else
                    {
                        value4.duration = RechargeTime;
                    }
                }
                else
                {
                    Player.AddCooldown(CooldownID, RechargeTime);
                }
                if (shieldCount > 0)
                {
                    if (regenDelay-- < 0)
                    {
                        if (shieldCount < MaxShield)
                        {
                            if (regenDelay < 4)
                                regenDelay = 4;
                            shieldCount += 1;
                        }
                    }
                    if (shieldCount > MaxShield)
                    {
                        shieldCount = MaxShield;
                    }
                    rechargeCounter = 0;
                }
                else
                {
                    rechargeCounter += 1;
                    if (rechargeCounter > RechargeTime)
                    {
                        CEUtils.PlaySound("vp_use", 0.85f, Player.Center, 6, 0.8f);
                        shieldCount = MaxShield / 2;
                    }
                }
            }
            else
            {
                rechargeCounter = 0;
                shieldCount = 0;
                if (regenDelay < 2 * 60)
                    regenDelay = 2 * 60;
                RemoveCooldown(CooldownID);
            }
        }
        public void RemoveCooldown(string id)
        {
            Player.RemoveCooldown(id);
        }

        #region 自研玩家设施(脱离灾厄): 键位/鼠标世界坐标/冲刺ID
        /// <summary>盔甲套装技能键(默认Y),替代灾厄 CalamityKeybinds.ArmorSetBonusHotKey。</summary>
        public static ModKeybind ArmorSetBonusHotKey;
        /// <summary>冲刺键(默认F),替代灾厄 CalamityKeybinds.DashHotkey。双击方向键仍然有效。</summary>
        public static ModKeybind DashHotkey;
        /// <summary>饰品主动技能键(默认V),替代灾厄 FindAccessory().GetDynamicModHotkey()。</summary>
        public static ModKeybind AccessoryAbilityHotKey;

        //冷却:统一走自研冷却框架(Player.AddCooldown/HasCooldown/TryGetCooldown/EntropyCooldowns())

        /// <summary>最近一次冲刺的效果 ID(CEDashEffect.ID),空串=无。由 Core/Dash 的 CEDashPlayer 在起手时写入。</summary>
        public string LastUsedDashID = "";

        /// <summary>鼠标世界坐标相对 MountedCenter 的偏移(网络同步的最小载荷)。</summary>
        public Vector2 mouseWorldDelta;
        /// <summary>各端可见的玩家鼠标世界坐标(自研,替代灾厄 mouseWorld)。本地玩家每帧自动写入。</summary>
        public Vector2 MouseWorld
        {
            get => Player.MountedCenter + mouseWorldDelta;
            set => mouseWorldDelta = value - Player.MountedCenter;
        }
        /// <summary>本帧是否有武器/弹幕需要各端读取鼠标坐标。每帧自动复位,调用方(如 CEUtils.mouseWorld)每帧置位。</summary>
        public bool MouseWorldListener;
        private Vector2 lastSyncedMouseDelta = new(float.MinValue, float.MinValue);
        private int mouseSyncTimer;

        private void UpdateSelfOwnedFacilities()
        {
            //自研mouseWorld:本地玩家每帧写入;仅在有监听者且位移超阈值时节流发包(参考灾厄:5px阈值+2tick节流)
            if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
            {
                MouseWorld = Main.MouseWorld;
                mouseSyncTimer++;
                if (Main.netMode == NetmodeID.MultiplayerClient && MouseWorldListener
                    && mouseSyncTimer >= 2 && (mouseWorldDelta - lastSyncedMouseDelta).Length() > 5f)
                {
                    mouseSyncTimer = 0;
                    lastSyncedMouseDelta = mouseWorldDelta;
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.SyncMouseWorld);
                    packet.Write(Player.whoAmI);
                    packet.Write((short)mouseWorldDelta.X);
                    packet.Write((short)mouseWorldDelta.Y);
                    packet.Send();
                }
            }
            MouseWorldListener = false;
        }
        #endregion

        public void DriverShieldHit(ref Player.HurtInfo info)
        {
            if (DriverShield > 0)
            {
                int reduceDmg = 0;
                int DamageToShield = int.Max(1, (int)(info.Damage * (Player.AzafureEnhance() ? 0.6f : 1)));
                if (DriverShield >= DamageToShield)
                {
                    DriverShield -= DamageToShield;
                    info.Cancelled = true;
                    immune = 40;
                    reduceDmg = DamageToShield;
                    CEUtils.PlaySound("RoverDriveHit", 1.4f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 3));
                }
                else
                {
                    reduceDmg = DriverShield;
                    info.Damage = info.Damage - DriverShield;
                    DriverShield = 0;
                    CEUtils.PlaySound("RoverDriveBreak", 1.4f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 8));
                }
                DriverRegenDelay = 5 * 60;
                for (int i = 0; i < 16; i++)
                {
                    PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center + new Vector2(Main.rand.NextFloat(-28, 28), Main.rand.NextFloat(-28, 28)), CEUtils.randomPointInCircle(12), Color.OrangeRed * 0.6f, Main.rand.NextFloat(1.6f, 2f)).Configure(Main.rand.Next(6, 12));
                }
                ShieldAlphaAdd = 1;
                CombatText.NewText(Player.getRect(), Color.OrangeRed, "-" + reduceDmg);
            }
        }
        public void NihShieldHit(ref Player.HurtInfo info)
        {
            if (NihilityShield > 0)
            {
                int reduceDmg = 0;
                int DamageToShield = int.Max(1, (int)(info.Damage * 1));
                if (NihilityShield >= DamageToShield)
                {
                    NihilityShield -= DamageToShield;
                    info.Cancelled = true;
                    immune = 40;
                    reduceDmg = DamageToShield;
                    CEUtils.PlaySound("shockBlast", 1.4f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 2));
                }
                else
                {
                    reduceDmg = NihilityShield;
                    info.Damage = info.Damage - NihilityShield;
                    NihilityShield = 0;
                    CEUtils.PlaySound("shielddown", 1.4f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 6));
                }
                NihilityRegenDelay = (int)(1.5f * 60);
                for (int i = 0; i < int.Min(20, reduceDmg / 5 + 1); i++)
                {
                    PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center + CEUtils.randomPointInCircle(64 * NihShieldScale), CEUtils.randomPointInCircle(16), new Color(100, 100, 255) * 0.9f, Main.rand.NextFloat(1.6f, 2f)).Configure(Main.rand.Next(12, 16));
                }
                ShieldAlphaAdd = 1;
                CombatText.NewText(Player.getRect(), Color.SkyBlue, "-" + reduceDmg);
            }
        }
        public void VoidShieldHit(ref Player.HurtInfo info, ref int shield, ref int regenDelay)
        {
            if (shield > 0)
            {
                int reduceDmg = 0;
                int DamageToShield = int.Max(1, (int)(info.Damage * 1));
                if (shield >= DamageToShield)
                {
                    shield -= DamageToShield;
                    info.Cancelled = true;
                    immune = 40;
                    reduceDmg = DamageToShield;
                    CEUtils.PlaySound("shockBlast", 1.5f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 2));
                }
                else
                {
                    reduceDmg = shield;
                    info.Damage = info.Damage - shield;
                    shield = 0;
                    CEUtils.PlaySound("shielddown", 1.5f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 4));
                }
                regenDelay = (int)(5f * 60);
                for (int i = 0; i < int.Min(20, reduceDmg / 5 + 1); i++)
                {
                    PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center + CEUtils.randomPointInCircle(64 * VoidCoreShieldScale), CEUtils.randomPointInCircle(16), new Color(160, 160, 255) * 0.8f, Main.rand.NextFloat(1.2f, 1.4f)).Configure(Main.rand.Next(15, 18));
                }
                ShieldAlphaAdd = 1;
                CombatText.NewText(Player.getRect(), Color.LightSkyBlue, "-" + reduceDmg);
            }
        }
        public void RatzielShieldHit(ref Player.HurtInfo info, ref int shield, ref int regenDelay)
        {
            if (shield > 0)
            {
                int reduceDmg = 0;
                int DamageToShield = int.Max(1, (int)(info.Damage * 1));
                if (shield >= DamageToShield)
                {
                    shield -= DamageToShield;
                    info.Cancelled = true;
                    immune = 40;
                    reduceDmg = DamageToShield;
                    CEUtils.PlaySound("shockBlast", 1.5f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 2));
                }
                else
                {
                    reduceDmg = shield;
                    info.Damage = info.Damage - shield;
                    shield = 0;
                    CEUtils.PlaySound("shielddown", 1.5f, Player.Center);
                    ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 4));
                }
                regenDelay = (int)(5f * 60);
                for (int i = 0; i < int.Min(20, reduceDmg / 5 + 1); i++)
                {
                    PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center + CEUtils.randomPointInCircle(64 * NihShieldScale), CEUtils.randomPointInCircle(16), Color.Aqua, Main.rand.NextFloat(1.2f, 1.4f)).Configure(Main.rand.Next(15, 18));
                }
                ShieldAlphaAdd = 1;
                CombatText.NewText(Player.getRect(), Color.Aqua, "-" + reduceDmg);
            }
        }

        public void ApplyScale()
        {
            oWidth = Player.width;
            oHeight = Player.height;
            if (Scale != 1)
            {
                Vector2 c = new Vector2(Player.Center.X, Player.position.Y + Player.height);
                Player.width = (int)(Player.defaultWidth * Scale);
                Player.height = (int)(Player.defaultHeight * Scale);
                Player.position = c - new Vector2(Player.width / 2, Player.height);
            }
        }
        public void ResetScale()
        {
            Vector2 c = new Vector2(Player.Center.X, Player.position.Y + Player.height);
            Player.width = oWidth;
            Player.height = oHeight;
            Player.position = c - new Vector2(Player.width / 2, Player.height);
        }
        public float AbyssalLight = 0;
        public Item goldenRock = null;
        public int RatzielShieldTime = 0;
        /// <summary>圣佑窗口(上神之佑盾撞命中后的短暂减伤),仅本地玩家写入,伤害结算也在本端,无需同步。</summary>
        public int OdinWardTime = 0;
        public int BigShotWingVisual = 0;
        public float MeleeScale = 1;
        public int SunriseScene = 0;
        public override void ResetEffects()
        {
            BaitCharging = false;
            MaxBaitCharge = 1;
            oathBannerDye = 0;
            oathBanner = false;
            oathBannerVisual = false;
            MeleeScale = 1;
            if (RatzielShieldTime > 0)
                RatzielShieldTime--;
            if (OdinWardTime > 0)
                OdinWardTime--;
            AbyssalLight = 0;
            smdVisual = false;
            smolderingSet = false;
            ashesCore = false;
            accAzureAbyss = false;
            goldenRock = null;
            rottenFangs = false;
            exquisiteCrown = false;
            EDamageReduce = 0;
            VoidShieldVisual = false;
            ScaleTarget = 1;
            Scale = float.Lerp(Scale, ScaleTarget, 0.1f);
            DriverShieldVisual = false;
            NihilitySet = false;
            VoidCoreItem = null;
            NihilityShieldEnabled = false;
            ChaoticSet = false;
            SpawnChaoticCellOnHurt = false;
            BookmarkHolderSpecialTextures.Clear();
            AdditionalBookmarkSlot = 0;
            AzureRapierBlock--;
            LifeStealP = 0;
            roaringDye = false;
            fruitCake = false;
            CritDamage = new Dictionary<DamageClass, float>();
            ManaExtraHeal = 0;
            shadowRune = false;
            vanityWing = null;
            wing = null;
            ilmeranAsylum = false;
            FallSpeed = 1;
            WingTimeMult = 1;
            devouringCard = false;
            bitternessCard = false;
            mourningCard = false;
            grudgeCard = false;
            obscureCard = false;
            DebuffTime = 1;
            CooldownTimeMult = 1;
            HitCooldown = 0;
            voidResistance = 0;
            plagueEngine = false;
            if (Player.whoAmI == Main.myPlayer)
            {
                if (foreseeOrbLast && foreseeOrbItem == null && Player.HasBuff<ShatteredOrb>())
                {
                    CEUtils.PlaySound("amethyst_break", 1, Player.Center);
                    Player.Hurt(PlayerDeathReason.ByCustomReason(Mod.GetLocalization("OrbPunishDeath" + Main.rand.Next(0, 2).ToString()).ToNetworkText(Player.name)), (int)(Player.statLifeMax2 * 0.9f), 0);
                }
                foreseeOrbLast = foreseeOrbItem != null;
            }
            foreseeOrbItem = null;
            soulDisorder = false;
            equipAccs = new List<EquipInfo>();
            vetrasylsEye = false;
            maliciousCode = false;
            AzafureChargeShieldItem = null;
            AzafureDriverShieldItem = null;
            visualMagiShield = false;
            MariviniumSet = false;
            meleeDamageReduce = 0;
            deusCore = false;
            heartOfStorm = false;
            summonCrit = 0;
            mawOfVoid = false;
            revelation = false;
            wyrmPhantom = false;
            cHat = false;
            dodgeChance = 0;
            sacrMask = false;
            GodHeadVisual = true;
            shootSpeed = 1;
            WeaponBoost = 0;
            Thorn = 0;
            WingSpeed = 1;
            AttackVoidTouch = 0;
            light = 0;
            CRing = false;
            lifeRegenPerSec = 0;
            Godhead = false;
            ManaCost = 1;
            auraCard = false;
            LastStand = false;
            brokenAnkh = false;
            holyMantle = false;
            nihShell = false;
            mariviniumBody = false;
            CrPlush = false;
            if (brillianceCard > 0)
            {
                brillianceCard -= 1;
            }
            enduranceCard = false;
            entityCard = false;
            inspirationCard = false;
            metropolisCard = false;
            radianceCard = false;
            temperanceCard = false;
            wisdomCard = false;
            oracleDeck = false;
            GreedCard = false;
            FrailCard = false;
            BarrenCard = false;
            TarnishCard = false;
            ConfuseCard = false;
            PerplexedCard = false;
            SacrificeCard = false;
            NothingCard = false;
            FoolCard = false;
            EvilDeck = false;
            holyMoonlight = false;
            oracleDeckInInv = false;
            taintedDeckInInv = false;
            summonerVF = false;
            magiVF = false;
            rogueVF = false;
            meleeVF = false;
            rangerVF = false;
            VFSet = false;
            VFLeg = false;
            VFHelmRanged = false;
            VFHelmMagic = false;
            VFHelmSummoner = false;
            VFHelmRogue = false;
            VFHelmMelee = false;
            halfManaSick = false;
            odinAura = false;
            leylaAura = false;
            thiefWatch = false;
            SCrown = false;
            GreedCard = false;
            enhancedMana = 0;
            enhancedManaFlat = 0;
            damageReduce = 1;
            moveSpeed = 0;
            DebuffImmuneChance = 0;
            reincarnationBadge = false;
            visualWispLantern = false;
            accWispLantern = false;
            CanSlainTownNPC = false;
        }


        public int crSky = 0;
        public int NihSky = 0;
        public int VortexSky = 0;
        public int llSky = 0;
        public int magiShieldCd = 0;
        public int sJudgeCd = 30 * 60;
        public float rBadgeCharge = 12;
        public int nihShellCount = 0;
        public int nihShellCd = 0;
        public override void FrameEffects()
        {
            if (CrPlush)
            {
                Player.head = EquipLoader.GetEquipSlot(base.Mod, "CruiserPlush", EquipType.Head);
            }
        }
        public int BlackFlameCd = 0;
        public bool rBadgeActive = false;
        public bool[] tileSolid = null;
        public bool[] solidTop = null;
        public bool[] tilePlatform = null;
        public bool cUp = false;
        public bool cDown = false;
        public bool cLeft = false;
        public bool cRight = false;
        public float rbDotDist = 0;
        public int vShieldCD = 0;
        public Item wing = null;
        public Item vanityWing = null;
        public int WingFrameAnmCount = 0;
        public float plWingTrailAlpha = 0;
        public PRT_StarTrailParticle plWingTrail = null;   //PhantomLightWing,每帧刷Lifetime+Position,不是spawn完就撒手
        public override void Load()
        {
            wingData = new SpecialWingDrawingData();
            drCrystals = null;
            //自研键位注册(替代灾厄 CalamityKeybinds)。三条键位名文案见 hjson 的 Mods.CalamityEntropy.Keybinds.*
            if (!Main.dedServ)
            {
                ArmorSetBonusHotKey = KeybindLoader.RegisterKeybind(Mod, "ArmorSetBonus", "Y");
                DashHotkey = KeybindLoader.RegisterKeybind(Mod, "Dash", "F");
                AccessoryAbilityHotKey = KeybindLoader.RegisterKeybind(Mod, "AccessoryAbility", "V");
            }
        }
        public override void Unload()
        {
            wingData = null;
            drCrystals = null;
            ArmorSetBonusHotKey = null;
            DashHotkey = null;
            AccessoryAbilityHotKey = null;
        }

        public float BaitCharge = 0;
        public override void PreUpdate()
        {
            UpdateSelfOwnedFacilities();
            if (SunriseScene > 0)
                SunriseScene--;
            if (drCrystals == null && Main.myPlayer == Player.whoAmI && !Main.dedServ)
            {
                drCrystals = new List<bool>() {
                    ShadowCrystalDeltarune.Ch1Crystal,
                    ShadowCrystalDeltarune.Ch2Crystal,
                    ShadowCrystalDeltarune.Ch3Crystal,
                    ShadowCrystalDeltarune.Ch4Crystal,
                    ShadowCrystalDeltarune.Ch5Crystal
                };
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var mp = Mod.GetPacket();
                    mp.Write((byte)CEMessageType.SyncDRShadowCrystal);
                    mp.Write(Player.whoAmI);
                    for (int i = 0; i < 5; i++)
                        mp.Write(drCrystals[i]);
                }
            }
            NoPlatformCollide--;
            NoAdrenaline = false;
            if (NoAdrenalineTime > 0)
                NoAdrenaline = true;
            NoAdrenalineTime--;
            if (hasAccVisual("PLWing") && (vanityWing == null || vanityWing.ModItem is PhantomLightWing))
            {
                if (plWingTrail == null || plWingTrail.Lifetime <= 1)
                {
                    plWingTrail = PRTLoader.NewParticle<PRT_StarTrailParticle>(Player.Center, Vector2.Zero, Color.White, 1.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
                    plWingTrail.maxLength = 32;
                }
                plWingTrail.Lifetime = 30;
                plWingTrail.Position = Player.MountedCenter + Player.gfxOffY * Vector2.UnitY + Player.velocity;
                plWingTrail.Color = Color.White * plWingTrailAlpha;
                if (Player.velocity.Y != 0 && !Player.dead && Player.head != EquipLoader.GetEquipSlot(Mod, "LuminarRing", EquipType.Head))
                {
                    plWingTrailAlpha += (1 - plWingTrailAlpha) * 0.1f;
                }
                else
                {
                    plWingTrailAlpha *= 0.9f;
                }
            }
            ISpecialDrawingWing sw;
            if (vanityWing != null && vanityWing.ModItem != null && vanityWing.ModItem is ISpecialDrawingWing)
            {
                sw = (ISpecialDrawingWing)vanityWing.ModItem;
                wingData.MaxFrame = sw.MaxFrame;
                wingData.SlowFallFrame = sw.SlowFallingFrame;
                if (Player.velocity.Y == 0)
                {
                    wingData.FrameCount = -1;
                    WingFrameAnmCount = 0;
                }
                else if (Player.wingTime <= 0)
                {
                    if (Player.controlJump)
                    {
                        wingData.FrameCount = sw.SlowFallingFrame;
                        WingFrameAnmCount = 0;
                    }
                    else
                    {
                        wingData.FrameCount = -1;
                        WingFrameAnmCount = 0;
                    }
                }
                else if (!Player.controlJump)
                {
                    wingData.FrameCount = sw.FallingFrame;
                    WingFrameAnmCount = 0;
                }
                else
                {
                    WingFrameAnmCount++;
                    if (WingFrameAnmCount >= sw.AnimationTick)
                    {
                        WingFrameAnmCount = 0;
                        wingData.FrameCount++;
                    }
                    if (wingData.FrameCount >= wingData.MaxFrame)
                    {
                        wingData.FrameCount = 0;
                    }
                }
            }
            else if (wing != null && wing.ModItem != null && wing.ModItem is ISpecialDrawingWing)
            {
                sw = (ISpecialDrawingWing)wing.ModItem;
                wingData.MaxFrame = sw.MaxFrame;
                wingData.SlowFallFrame = sw.SlowFallingFrame;
                if (Player.velocity.Y == 0)
                {
                    wingData.FrameCount = -1;
                    WingFrameAnmCount = 0;
                }
                else if (Player.wingTime <= 0)
                {
                    if (Player.controlJump)
                    {
                        wingData.FrameCount = sw.SlowFallingFrame;
                        WingFrameAnmCount = 0;
                    }
                    else
                    {
                        wingData.FrameCount = -1;
                        WingFrameAnmCount = 0;
                    }
                }
                else if (!Player.controlJump)
                {
                    wingData.FrameCount = sw.FallingFrame;
                    WingFrameAnmCount = 0;
                }
                else
                {
                    WingFrameAnmCount++;
                    if (WingFrameAnmCount >= sw.AnimationTick)
                    {
                        WingFrameAnmCount = 0;
                        wingData.FrameCount++;
                    }
                    if (wingData.FrameCount >= wingData.MaxFrame)
                    {
                        wingData.FrameCount = 0;
                    }
                }
            }
            else
            {
                WingFrameAnmCount = 0;
                wingData.FrameCount = 0;
            }
            if (McAttributes == null)
            {
                McAttributes = new();
                for (int i = 0; i < DamageClassLoader.DamageClassCount; i++)
                {
                    McAttributes.Add(new McAttributeRecord(DamageClassLoader.GetDamageClass(i)));
                }
            }
            hitTimeCount++;
            if (Player.HeldItem != null && Player.HeldItem.ModItem is EntropyBook eb)
            {
                eb.CheckSpawn(Player);
            }
            if (!Main.dedServ && vetrasylsEye && vShieldCD <= 0 && CEKeybinds.VetrasylsEyeBlockHotKey.JustPressed)
            {
                vShieldCD = VetrasylsEye.GetShieldCooldown().ApplyCdDec(Player);
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, (Main.MouseWorld - Player.Center).normalize() * 6, ModContent.ProjectileType<WelkingShield>(), 0, 0, Player.whoAmI);
            }
            vShieldCD -= 1;
            if (vShieldCD == 0 && vetrasylsEye)
            {
                CEUtils.PlaySound("beep", 1, Player.Center);
            }
            temporaryArmor *= 0.992f;
            if (temporaryArmor > 0)
            {
                temporaryArmor -= 0.003f;
            }
            if (temporaryArmor < 0)
            {
                temporaryArmor = 0;
            }
            ffinderCd--;
            if (syncHoldingPoop)
            {
                syncHoldingPoop = false;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.PoopSync);
                    packet.Write(Player.whoAmI);
                    packet.Write(holdingPoop);
                    packet.Send();
                }
            }
            if (hasAcc("FlowingLightWing") && !Player.controlJump && Player.controlDown)
                Player.maxFallSpeed *= 2f;
            if (gravAddTime > 0)
            {
                Player.gravity *= 1.6f;
                Player.maxFallSpeed *= 3;
                Player.controlDown = true;
            }
            if (Player.wet && accAzureAbyss)
            {
                if (!Player.controlJump)
                {
                    Player.gravity *= Player.controlDown ? 2.4f : 1.8f;
                }
                else
                    if (Math.Abs(Player.velocity.Y) < 6 && Player.velocity.Y < 0)
                    Player.velocity.Y *= 1.3f;
                Player.maxFallSpeed *= Player.controlDown ? 3f : 1.8f;
            }
            if (FallSpeedUP > 0)
            {
                Player.maxFallSpeed *= 5;
            }
            Player.maxFallSpeed *= FallSpeed;
            gravAddTime--;
            if (CruiserAntiGravTime > 0)
            {
                CruiserAntiGravTime--;
                Player.gravity = 0;
                Player.maxFallSpeed = 100;
                Player.velocity *= 0.97f;
                if (Player.controlDown)
                {
                    Player.velocity.Y += 0.6f;
                }
            }
            if (Player.dead) { return; }
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<BatteringRamProj>()] > 0)
            {
                int ps = -1;
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.ModProjectile is BatteringRamProj pj && pj.CanHit)
                    {
                        ps = p.whoAmI;
                    }
                }
                if (ps != -1)
                {
                    Player.gravity = 0;
                    Player.maxFallSpeed = 1000;
                }
            }
            if (rBadgeActive)
            {
                Player.maxFallSpeed = 99;
                rbDotDist += (1 - rbDotDist) * 0.06f;
                Player.velocity *= 0f;
                float speed = 20f;
                if (cUp)
                {
                    Player.velocity.Y -= speed;
                }
                if (cDown)
                {
                    Player.velocity.Y += speed;
                }
                if (cLeft)
                {
                    Player.direction = -1;
                    Player.velocity.X -= speed;
                }
                if (cRight)
                {
                    Player.direction = 1;
                    Player.velocity.X += speed;
                }
                if (!Player.velocity.Equals(Vector2.Zero) && Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket pack = Mod.GetPacket();
                    pack.Write((byte)CEMessageType.PlayerSetPos);
                    pack.Write(Player.whoAmI);
                    pack.WriteVector2(Player.Center);
                    pack.Send();
                }
                Player.velocity.Y += 0.0001f;

            }
            else
            {
                rbDotDist += (-rbDotDist) * 0.06f;
            }
            if (NoPlatformCollide > 0 || rBadgeActive || Player.GetModPlayer<CEDashPlayer>().IgnoresPlatforms || CruiserAntiGravTime > 0 || Player.mount.Type == ModContent.MountType<ReplicaPenMount>())
            {
                resetTileSets = true;
                tileSolid = (bool[])Main.tileSolid.Clone();
                solidTop = (bool[])Main.tileSolidTop.Clone();
                tilePlatform = (bool[])TileID.Sets.Platforms.Clone();
                for (int type = 0; type < TileID.Sets.Platforms.Length; type++)
                {
                    if (TileID.Sets.Platforms[type])
                    {
                        Main.tileSolid[type] = false;
                        Main.tileSolidTop[type] = false;
                        TileID.Sets.Platforms[type] = false;
                    }
                }
            }
            if (BlackFlameCd > 0)
            {
                BlackFlameCd--;
            }
            if (TarnishCard && Main.myPlayer == Player.whoAmI)
            {
                if (Player.channel && Player.HeldItem.damage > 0)
                {
                    if (BlackFlameCd < 1)
                    {
                        int useTime = Player.HeldItem.useTime > 0 ? Player.HeldItem.useTime : Player.HeldItem.useAnimation;
                        BlackFlameCd = Math.Max(useTime, Tarnish.BlackFireCooldownMin);
                        Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.One) * 14, ModContent.ProjectileType<BlackFire>(), Tarnish.BlackFireDamage, 2, Player.whoAmI);
                    }
                }
            }
            if (voidshadeBoostTime > 0)
            {
                voidshadeBoostTime--;
            }
            if (HolyShield)
            {
                mantleCd = (int)(HolyMantle.Cooldown * CooldownTimeMult);
            }
            else
            {
                mantleCd--;
            }
            if (!HolyShield && holyMantle)
            {
                if (mantleCd <= 0)
                {
                    mantleCd = HolyMantle.Cooldown.ApplyCdDec(Player);
                    HolyShield = true;
                }
            }
            screenShift *= 0.9f;
            if (immune > 0)
            {
                Player.immune = true;
                if (Player.immuneTime < immune)
                    Player.immuneTime = immune;
                for (int i = 0; i < Player.hurtCooldowns.Length; i++)
                {
                    if (Player.hurtCooldowns[i] < immune)
                        Player.hurtCooldowns[i] = immune;
                }
                _immune--;
            }
            if (SacredJudgeShields < 2)
            {
                sJudgeCd -= 1;
                if (sJudgeCd <= 0)
                {
                    sJudgeCd = (90 * 60).ApplyCdDec(Player);
                    SacredJudgeShields += 1;
                }
            }
            lastStandCd--;
            Lighting.AddLight(Player.Center, light, light, light);
            bool sm = Player.HasBuff(ModContent.BuffType<SoyMilkBuff>());
            if (hasSM && !sm)
            {
                foreach (Projectile p in Main.projectile)
                {
                    if (p.friendly && p.active && p.owner == Player.whoAmI)
                    {
                        p.Kill();
                    }
                }
            }
            hasSM = sm;
            if (crSky > 0)
            {
                crSky--;
            }
            if (NihSky > 0)
            {
                NihSky--;
            }
            if (snowgrave > 0)
            {
                snowgrave--;
            }
            if (VortexSky > 0)
            { VortexSky--; }
            if (llSky > 0)
            {
                llSky--;
            }
            if (VFLeg && !Player.controlLeft && !Player.controlRight)
            {
                Player.velocity.X *= 0.96f;
            }

        }
        public List<Vector2> daPoints = new List<Vector2>();
        public Vector2 daLastP = Vector2.Zero;

        public override void PostUpdateRunSpeeds()
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<WyrmDash>()] > 0)
            {
                Player.maxFallSpeed = 1000;
            }
            if (Player.Entropy().inspirationCard)
            {
                Player.runAcceleration *= 1.2f;
                Player.maxRunSpeed *= 1.2f;
            }
            // 2026-08-31 平衡案:神谕卡组移速改为+10%(走 moveSpeed 字段,在 OracleDeck 里),原跑速乘区退役
            if (MagiShield > 0)
            {
                Player.runAcceleration *= 1.1f;
                Player.maxRunSpeed *= 1.1f;
            }
            if (VFSet)
            {
                Player.maxRunSpeed *= 1.12f;
            }
            if (VFHelmRogue)
            {
                Player.runAcceleration *= 1.05f;
                Player.maxRunSpeed *= 1.15f;
            }
            if (CRing)
            {
                Player.runAcceleration *= 1.05f;
                Player.maxRunSpeed *= 1.05f;
            }
            if (maliciousCode)
            {
                Player.maxRunSpeed *= 0.85f;
            }
            if (maliciousCode)
            {
                //恶意代码debuff(脱离灾厄:原CWR大修联动的加强档已随恒假属性坍缩删除)
                Player.GetDamage(DamageClass.Generic) *= 0.8f;
                Player.GetAttackSpeed(DamageClass.Generic) *= 0.8f;
                Player.statLifeMax2 = (int)(Player.statLifeMax2 * 0.8f);
                Player.lifeRegen /= 2;
                lifeRegenPerSec /= 2;
                Player.statDefense *= 0.8f;
                Player.maxRunSpeed *= 0.88f;
            }

            if (VFSet)
            {
                Player.runAcceleration *= 1f + 0.02f * VoidCharge;
                Player.maxRunSpeed *= 1f + 0.15f * VoidCharge;
            }
            if (moveSpeed != 0)
            {
                Player.runAcceleration *= 1f + moveSpeed;
                Player.maxRunSpeed *= 1f + moveSpeed;
            }
            if (Scale != 1)
            {
                Player.maxRunSpeed *= Scale;
                Player.runAcceleration *= Scale;
            }
            if (CalamityEntropy.EntropyMode)
            {
                Player.maxRunSpeed *= 0.96f;
                if (HitTCounter > 0)
                {
                    Player.maxRunSpeed *= 0.86f;
                }
            }
        }
        public int scHealCD = 60;
        public int HitTCounter = 0;
        public int voidslashType = -1;
        public float WingTimeMult = 1;
        public int ffDecSlot = 0;
        public override void UpdateLifeRegen()
        {
            // 虚灵宙法盔:攻击敌人后5秒内大幅提升自然生命再生(5hp/s)
            if (cosmosRegenTime > 0)
            {
                cosmosRegenTime--;
                Player.lifeRegen += 10;
            }
            // 苍绿书签:每秒恢复2点生命(2026-08-31 平衡案)
            if (bmSilvaRegenTime > 0)
            {
                bmSilvaRegenTime--;
                Player.lifeRegen += 4;
            }
            // 堕化卡组:自然生命再生降低100%(2026-08-31 平衡案)
            if (EvilDeck)
            {
                Player.lifeRegenTime = 0;
                Player.lifeRegen = int.Min(0, Player.lifeRegen);
            }
            if (deusCore && Player.lifeRegen > 0)
            {
                Player.lifeRegen /= 2;
            }
            //脱离灾厄:血沸附魔已删除,bloodBoiling无写入点,条件只留VoidTouch半支
            if (Player.HasBuff<VoidTouch>())
            {
                Player.lifeRegenTime = 0;
                Player.lifeRegen = int.Min(0, Player.lifeRegen);
            }
        }
        public override void PostUpdateMiscEffects()
        {
            ffDecSlot = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.ModNPC is FriendFindNPC)
                {
                    ffDecSlot++;
                    Player.maxMinions--;
                }
            }
            //脱离灾厄:腐化巢心Lore增伤随灾厄Lore下线删除
            if (shadowRune)
            {
                Player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += ShadowRune.WhipAtkSpeedAddition;

            }
            if (SacrificeCard)
            {
                Player.lifeRegen -= (int)(Player.lifeRegen * 0.7f);
                lifeRegenPerSec = (int)(lifeRegenPerSec * 0.4f);
            }
            if (obscureCard)
            {
                bool f = false;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && npc.getRect().Intersects(Player.getRect()))
                    {
                        f = true;
                        if (Player.wingTime < Player.wingTimeMax * 2)
                        {
                            Player.wingTime += 2f;
                        }

                        if ((!npc.HasBuff<SoulDisorder>()) || npc.buffTime[npc.FindBuffIndex(ModContent.BuffType<SoulDisorder>())] < 120)
                        {
                            npc.AddBuff(ModContent.BuffType<SoulDisorder>(), 600);
                        }
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror);
                    }
                }
                if (f)
                {
                    Player.lifeRegen += 16;
                    lifeRegenPerSec += 4;
                }
            }
            if (LoreEffect.Enabled)
            {
                foreach (var lore in enabledLoreItems)
                {
                    if (LoreReworkSystem.loreEffects.TryGetValue(lore, out var lef))
                    {
                        lef.UpdateEffects(Player);
                    }
                }
            }
            if (NihilityTwinLoreBonus)
            {
                lifeRegenPerSec += NihilityTwinLore.HealPreSec;
                voidResistance += NihilityTwinLore.VoidRes;
                Player.wingTimeMax = (int)(Player.wingTimeMax * (1 + NihilityTwinLore.MaxFlyTimeAddition));
            }
            if (soulDisorder)
            {
                HitCooldown -= 0.5f;
            }
            if (ProphetLoreBonus)
            {
                HitCooldown += ProphetLore.ImmuneAdd;
                Player.lifeRegen += ProphetLore.LifeRegen;
            }
            Player.wingTimeMax = (int)(Player.wingTimeMax * WingTimeMult);
            if (voidslashType == -1)
            {
                voidslashType = ModContent.ProjectileType<VoidSlash>();
            }
            if (mariviniumBody)
            {
                Player.breath = Player.breathMax + 20000;
            }
            if (accAzureAbyss)
            {
                Player.breath = Player.breathMax + 20000;
            }
            if (Player.ownedProjectileCounts[voidslashType] > 0)
            {
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == voidslashType && p.ModProjectile is VoidSlash vs && vs.d < 16)
                    {
                        Player.gravity = 0;
                        Player.invis = true;
                        if (immune < 8)
                        {
                            immune = 8;
                        }
                    }
                }

            }


            //脱离灾厄:血沸附魔(bloodBoiling)触发链断死,自伤/攻速/死亡播报块一并裁删
            /*if (SubworldSystem.IsActive<VOIDSubworld>())
            {
                Player.gravity = 0;
            }*/
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is JewelSapphire)
                {
                    if (p.Distance(Player.Center) <= 300)
                    {
                        Player.lifeRegen += 2;
                        Player.endurance += 0.04f;
                        Player.GetDamage(DamageClass.Generic) += 0.08f;
                    }
                }
            }
            // 2026-08-31 平衡案:幽影护符效果重做,原减伤项退役(新效果在 LurkersCharm.UpdateAccessory)
            float d = damageReduce - 1;
            if (Player.Entropy().enduranceCard)
            {
                d += 0.05f;
            }
            if (Player.Entropy().oracleDeck)
            {
                d += 0.05f;
            }
            // 2026-08-31 平衡案:虚湮吞天盔职业奖励重做,原+10%全减伤退役(改为接触伤害-15%,在头盔文件里)
            // 上神之佑:自己或任一队友持有时+15%免伤(不叠加)
            bool odinWard = odinAura;
            if (!odinWard)
            {
                foreach (Player teammate in Main.ActivePlayers)
                {
                    if (!teammate.dead && teammate.whoAmI != Player.whoAmI && teammate.team == Player.team && teammate.Entropy().odinAura)
                    {
                        odinWard = true;
                        break;
                    }
                }
            }
            if (odinWard)
            {
                d += OdinsRefuge.TeamWardDR;
            }
            Player.endurance += d;
            if (rBadgeActive)
            {
                Player.gravity = 0;
            }

            manaNorm = Player.statManaMax2;
            Player.statManaMax2 += (int)(Player.statManaMax2 * enhancedMana) + enhancedManaFlat;
            if (Player.statMana > manaNorm)
            {
                Player.GetDamage(DamageClass.Magic) += (Player.statMana - manaNorm) * 0.0015f;
            }
            Player player = Player;
            if (bitternessCard)
            {
                player.GetDamage(DamageClass.Generic) += BitternessCard.DmgMax * (player.statLife / (float)player.statLifeMax2);
                player.endurance += BitternessCard.enduMax * (1f - (player.statLife / (float)player.statLifeMax2));
            }
            if (CalamityEntropy.EntropyMode)
            {
                Player.statLifeMax2 = (int)(Player.statLifeMax2 * 0.8f);
                Player.statDefense *= 0.5f;
                Player.endurance *= 0.5f;
                Player.lifeRegen /= 3;
                lifeRegenPerSec /= 3;
                if (HitTCounter > 0)
                {
                    Player.lifeRegen = 0;
                    lifeRegenPerSec = 0;
                }
                //脱离灾厄:伤害类遍历表收敛为原版+自有类(原灾厄17类)
                List<DamageClass> dmgClasses = new List<DamageClass>() { DamageClass.Generic, DamageClass.Default, DamageClass.Melee, DamageClass.MeleeNoSpeed, DamageClass.Ranged, DamageClass.Magic, DamageClass.Summon, DamageClass.SummonMeleeSpeed, DamageClass.Throwing, ModContent.GetInstance<Content.DamageClasses.NoDRMelee>(), ModContent.GetInstance<NoneTypeDamageClass>() };
                for (int i = 0; i < dmgClasses.Count; i++)
                {
                    DamageClass dc = dmgClasses[i];
                    if (Player.GetDamage(dc).Additive > 1)
                    {
                        float tv = (float)Math.Pow(Player.GetDamage(dc).Additive, 0.85f);
                        Player.GetDamage(dc) -= (Player.GetDamage(dc).Additive - tv);
                    }
                }
            }
            Player.statDefense += (int)temporaryArmor;
            HitTCounter--;

            if (McAttributes != null)
            {
                if (Player.statDefense > McDefense)
                {
                    McDefense = Player.statDefense;
                }
                if (Player.lifeRegen > McRegen)
                {
                    McRegen = Player.lifeRegen;
                }
                if (Player.endurance > McEndurance)
                {
                    McEndurance = Player.endurance;
                }
                foreach (var mca in McAttributes)
                {
                    mca.Record(Player);
                }

                float dec = McAttributeRecord.dec;
                McDefense += (Player.statDefense - McDefense) * dec;
                McRegen += (Player.lifeRegen - McRegen) * dec;
                McEndurance += (Player.endurance - McEndurance) * dec;
                foreach (var mca in McAttributes)
                {
                    mca.UpdateAtrLossing(Player);
                }

                if (mourningCard)
                {
                    if (Player.statDefense < McDefense)
                    {
                        Player.statDefense += (int)Math.Round(McDefense) - Player.statDefense;
                    }
                    if (Player.lifeRegen < McRegen)
                    {
                        Player.lifeRegen = (int)Math.Round(McRegen);
                    }
                    foreach (var mca in McAttributes)
                    {
                        mca.SetStats(Player);
                    }
                    if (Player.endurance < McEndurance)
                    {
                        Player.endurance = McEndurance;
                    }
                }
            }
            if (RatzielShieldTime > 0)
            {
                Player.statDefense += Player.maxMinions * 1;
                Player.endurance += Player.maxMinions * 0.004f;
            }
            // 2026-08-31 平衡案:上神之佑重做,原盾撞圣佑窗口退役(改为常驻团队15%免伤,见减伤汇总处)
            // 书签玩家侧被动:光明书签+20%跳跃速度,泰拉书签+10防御
            if (bmLightJumpTime > 0)
            {
                bmLightJumpTime--;
                Player.jumpSpeedBoost += Player.jumpSpeed * 0.2f;
            }
            if (bmTerraDefTime > 0)
            {
                bmTerraDefTime--;
                Player.statDefense += 10;
            }
            if (bmSagSpawnTime > 0)
            {
                bmSagSpawnTime--;
            }
            //基于尺寸更改机动性
            Player.jumpSpeed *= float.Lerp(Scale, 1, 0.55f);
            Player.extraFall += (int)((Scale - 1f) * 22);
            Player.maxFallSpeed *= float.Lerp(Scale, 1, 0.4f);
        }

        //脱离灾厄:潜行退役惰性字段EquipedAnyRogueAcc已裁删
        public int manaNorm = 0;
        public int deusCoreAdd = 0;
        public float ShieldAlphaAdd = 0;
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            deusCoreAdd = 0;

            modifiers.ModifyHurtInfo += EPHurtModifier;
            if (AzureShield > 0)
            {
                modifiers.SourceDamage *= 0.75f;
            }
            if (soulDisorder)
            {
                modifiers.SourceDamage *= 1.25f;
            }
            if (Player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
            {
                modifiers.DisableSound();
            }
        }
        private int _immune = 0;
        public int immune { get { return _immune; } set { if (value > _immune) _immune = value; } }
        public bool cHat = false;
        public float HitCooldown = 0;
        public float DebuffTime = 1;
        public float BloodthirstyEffect = 0;
        public override void OnHurt(Player.HurtInfo info)
        {
            if (AzureShield > 0)
            {
                CEUtils.PlaySound("YharonFireball1", 2.2f, Player.Center);
            }
            // 2026-08-31 平衡案:苍溟护符重做——受击时召唤3个追踪深渊漩涡,5秒效果冷却(原大伤护盾环退役)
            if (accAzureAbyss && CECooldowns.CheckBMProc("AzureVortexOnHurt", 300))
            {
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, CEUtils.randomRot().ToRotationVector2() * 16, ModContent.ProjectileType<AzureVortex>(), (int)(Player.GetBestClassDamage().ApplyTo(TalismanOfTheAzureAbyss.VortexBaseDamage.ApplyAccArmorDamageBonus())), 0, Player.whoAmI);
                }
            }
            BloodthirstyEffect += (info.Damage / (float)Player.statLifeMax2) * 27;
            if (BloodthirstyEffect > 16)
                BloodthirstyEffect = 16;
            if (BookMarkLoader.GetPlayerHeldEntropyBook(Player, out var eb) && info.Damage > 19 && CECooldowns.CheckCD("BloodthirstResetShootDelay", 30))
            {
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BloodthirstBMEffect>(Player))
                    eb.shotCooldown = 0;
            }
            if (SpawnChaoticCellOnHurt)
            {
                if (info.Damage > 12)
                {
                    if (CECooldowns.CheckCD("ChaoticSetCellSpawnCD", 160))
                    {
                        CEUtils.PlaySound("ksLand", 0.7f, Player.Center);
                        int type = ModContent.ProjectileType<ChaoticCellMinion>();
                        for (int i = 0; i < 3; i++)
                        {
                            if (Player.ownedProjectileCounts[type] < ChaoticHelmet.MaxCells)
                            {
                                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, CEUtils.randomRot().ToRotationVector2() * 15, type, ((int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(ChaoticCellMinion.BaseDamage)).ApplyAccArmorDamageBonus(Player), 2, Player.whoAmI);
                            }
                        }
                    }
                }
            }
            // 修复:驱动核心护盾曾在每次受击时清零充能进度,战斗中永远攒不满(20s),
            // 即"护盾无法生成"。与虚空核心护盾对齐:受击不再打断充能。
            // 2026-08-31 平衡案:瘟疫内燃机——受击时使附近敌人遭受困惑与酸性毒液
            if (plagueEngine)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.friendly && !npc.dontTakeDamage && CEUtils.getDistance(npc.Center, Player.Center) < PlagueInternalCombustionEngine.HurtDebuffRadius)
                    {
                        npc.AddBuff(BuffID.Confused, 180);
                        npc.AddBuff(BuffID.Venom, 300);
                    }
                }
            }
            if (Player.statLife - info.Damage > 0)
            {
                if ((hasAcc("VastLV2") && ManaRegenTime > 0) || Player.HasBuff<ManaAwaken>())
                {
                    immune = 240;
                    ManaRegenTime = 0;
                    Player.AddBuff(hasAcc("VastLV4") ? ModContent.BuffType<ManaCaress>() : ModContent.BuffType<ManaPray>(), 60 * 10);
                }
            }
            if (Player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
            {
                CEUtils.PlaySound("llHurt", 1, Player.Center);
            }
            HitTCounter = 600;
            hitTimeCount = 0;
            JustHit = true;
            //脱离灾厄:血肉巢穴Lore受击回血随灾厄Lore下线删除
        }
        public bool JustHit = false;
        // 瘟疫内燃机命中派生挂在此处;苍溟漩涡改走 OnHitNPCWithItem/WithProj,避免自触发
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            // 瘟疫内燃机:手持武器攻击命中时召唤穿墙瘟疫能量(0.5秒内置CD)
            if (plagueEngine && CECooldowns.CheckBMProc("PlagueEnergy", 30))
            {
                int dmg = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(PlagueInternalCombustionEngine.EnergyBaseDamage);
                int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center,
                    (target.Center - Player.Center).SafeNormalize(Vector2.UnitX).RotatedByRandom(0.6f) * 9f,
                    ModContent.ProjectileType<PlagueEnergy>(), dmg, 2f, Player.whoAmI, target.whoAmI);
                CEUtils.SyncProj(p);
            }
            // 无垠:魔法暴击给予目标3秒灵魂紊乱
            if (hasAcc("Vast") && hit.Crit && hit.DamageType.CountsAsClass(DamageClass.Magic))
            {
                target.AddBuff(ModContent.BuffType<SoulDisorder>(), 180);
            }
        }
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            TrySpawnAzureVortexOnHit(target);
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (proj.type == ModContent.ProjectileType<AzureVortex>())
                return;
            TrySpawnAzureVortexOnHit(target);
        }
        private void TrySpawnAzureVortexOnHit(NPC target)
        {
            if (!accAzureAbyss || !CECooldowns.CheckBMProc("AzureVortexOnHit", 30))
                return;
            int vp = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center,
                (target.Center - Player.Center).SafeNormalize(Vector2.UnitX) * 16,
                ModContent.ProjectileType<AzureVortex>(), (int)(Player.GetBestClassDamage().ApplyTo(TalismanOfTheAzureAbyss.VortexBaseDamage.ApplyAccArmorDamageBonus())), 0, Player.whoAmI);
            CEUtils.SyncProj(vp);
        }
        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (immune > 0)
            {
                deusCoreBloodOut -= deusCoreAdd;
                return true;
            }
            if (dodgeChance > Main.rand.NextDouble())
            {
                deusCoreBloodOut -= deusCoreAdd;
                return true;
            }
            return false;
        }
        public bool PetsHat { get { return cHat || DateTime.Now.Month == 12; } }
        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            if (noCsDodge)
            {
                return false;
            }
            if (Main.rand.NextDouble() < dodgeChance)
            {
                deusCoreBloodOut -= deusCoreAdd;
                immune = 30;
                return true;
            }
            // 2026-08-31 平衡案:渊海护盾改为抵挡一次超过50点的伤害并消失,破盾给深渊狂怒
            if (info.Damage > 50 && MariviniumShieldCount > 0)
            {
                immune = 60;
                MariviniumShieldCount--;
                CEUtils.PlaySound("crystalShieldBreak", 1, Player.Center, 1, 0.7f);
                Player.AddBuff(ModContent.BuffType<AbyssalWrath>(), 600);
                for (int i = 0; i < 42; i++)
                {
                    Dust.NewDust(Player.Center, 1, 1, DustID.BlueCrystalShard, Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6), Scale: 2);
                }
                return true;
            }
            if (HolyShield && info.Damage > 100)
            {
                immune = 120;
                HolyShield = false;

                Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<MantleBreak>(), 0, 0, Player.whoAmI);

                Player.AddCooldown("HolyMantleCooldown", HolyMantle.Cooldown, true);

                deusCoreBloodOut -= deusCoreAdd;
                return true;
            }
            if (info.Damage * (2 - damageReduce) - Player.statDefense > 16)
            {
                if (SacredJudgeShields > 0 && info.Damage < 80 && Player.ownedProjectileCounts[ModContent.ProjectileType<SacredJudge>()] > 0)
                {
                    immune = 120;
                    SacredJudgeShields -= 1;
                    Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<MantleBreak>(), 0, 0, Player.whoAmI);
                    deusCoreBloodOut -= deusCoreAdd;
                    return true;
                }
                if (SacredJudgeShields > 1 && Player.ownedProjectileCounts[ModContent.ProjectileType<SacredJudge>()] > 0)
                {
                    immune = 120;
                    SacredJudgeShields -= 2;
                    Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<MantleBreak>(), 0, 0, Player.whoAmI);
                    deusCoreBloodOut -= deusCoreAdd;
                    return true;
                }
            }
            return false;
        }

        public bool noCsDodge = false;
        //脱离灾厄:EPHurtModifier2(血肉墙Lore减伤)随灾厄Lore下线,连同注册点一并裁删
        private void EPHurtModifier(ref Player.HurtInfo info)
        {
            if (AzureRapierBlock > 0)
            {
                bool AllBlock = false;
                if (!Player.HasCooldown(BlockingCooldown.ID))
                {
                    info.Cancelled = true;
                    AllBlock = true;
                    info.Damage = 0;
                    immune = 16;
                }
                else
                {
                    info.Damage = (int)(info.Damage * (Player.EntropyCooldowns().cooldowns[BlockingCooldown.ID].timeLeft / 600f));
                }
                Entity source = null;
                if (info.DamageSource.TryGetCausingEntity(out Entity ent))
                {
                    if (ent is NPC)
                        source = ent;

                    if (ent is Projectile proj)
                    {
                        if (proj.Entropy().Shooter >= 0)
                        {
                            source = proj.Entropy().Shooter.ToNPC();
                        }
                        if (source != null && !source.active)
                            source = null;
                    }
                }
                if (source == null)
                    source = CEUtils.FindTarget_HomingProj(Player, Player.Center, 1000);
                if (source == null)
                    source = Player;
                AzureRapierHeld.OnBlock(Player, source.Center, source.velocity);
                if (AllBlock)
                {
                    info.Cancelled = true;
                    immune = 40;
                    return;
                }
            }
            if (AzafureDriverShieldItem != null)
            {
                DriverShieldHit(ref info);
                if (info.Cancelled)
                    return;
            }
            if (NihilityShieldEnabled)
            {
                NihShieldHit(ref info);
                if (info.Cancelled)
                    return;
            }
            if (VoidCoreItem != null)
            {
                VoidShieldHit(ref info, ref VoidShield, ref VoidRegenDelay);
                if (info.Cancelled)
                    return;
            }
            if (RatzielShieldTime > 0)
            {
                RatzielShieldHit(ref info, ref RatzielShield, ref RatzielRegenDelay);
                if (info.Cancelled)
                    return;
            }
            info.Damage = (int)(info.Damage * (1 - EDamageReduce));
            noCsDodge = false;
            if (HolyShield && info.Damage > 120)
            {
                return;
            }
            if (MariviniumShieldCount > 0 && info.Damage > 50)
            {
                return;
            }
            bool setToOne = false;
            if (!setToOne)
            {
                if (foreseeOrbItem != null && !Player.HasBuff<ShatteredOrb>())
                {
                    CEUtils.PlaySound("amethyst_break", 1, Player.Center);
                    info.Damage = info.Damage > 100 ? 100 : info.Damage;
                    Player.AddBuff(ModContent.BuffType<ShatteredOrb>(), 30 * 60);
                }
            }
            if (!setToOne)
            {
                if (SulphurousBubble && info.Damage > 9)
                {
                    setToOne = true;
                    info.Cancelled = true;
                    immune = 60;
                    SulphurousBubbleRecharge = 0;
                    SulphurousBubble = false;
                    // 2026-08-31 平衡案:咒噬书签泡泡爆炸不再造成硫磺海剧毒
                    if (BookMarkLoader.GetPlayerHeldEntropyBook(Player, out var ebk))
                        CEUtils.SpawnExplotionFriendly(Player.GetSource_FromThis(), Player, Player.Center, ebk.CauculateProjectileDamage(12), 580, DamageClass.Magic);
                    PRTLoader.NewParticle<PRT_PulseRing>(Player.Center, Vector2.Zero, new Color(10, 190, 10), 0.2f).Configure(5.45f, 16);
                    PRTLoader.NewParticle<PRT_PulseRing>(Player.Center, Vector2.Zero, new Color(10, 190, 10), 0.2f).Configure(5.8f, 16);
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 80; i++)
                        {
                            PRTLoader.NewParticle<PRT_Light>(Player.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(1f, 10f)
                                , new Color(0, 60, 0), Main.rand.NextFloat(0.3f, 0.6f))
                                .Configure(0.2f, lifetime: 60);
                        }
                    }
                    SoundEngine.PlaySound(in SoundID.Item54, Player.position);
                    CEUtils.PlaySound("beast_lavaball_rise1", 1, Player.Center);
                }
            }
            if (!setToOne)
            {
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.ModProjectile is PoopWulfrumProjectile w && CEUtils.getDistance(Player.Center, p.Center) < PoopWulfrumProjectile.shieldDistance)
                    {
                        if (w.shield > 0)
                        {
                            if (w.shield > info.Damage)
                            {
                                w.shield -= info.Damage;
                                info.Damage = 0;
                                setToOne = true;
                                p.netUpdate = true;
                                noCsDodge = true;
                            }
                            else
                            {
                                info.Damage -= w.shield;
                                w.shield = 0;
                                p.netUpdate = true;
                            }
                        }
                    }
                }
            }
            if (!setToOne && SacredJudgeShields > 0 && Player.ownedProjectileCounts[ModContent.ProjectileType<SacredJudge>()] > 0 && SacredJudgeShields < 2)
            {
                SacredJudgeShields--;
                info.Damage -= 80;
                Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<MantleBreak>(), 0, 0, Player.whoAmI);
                immune = 120;
            }

            if (!setToOne && MagiShield > 0)
            {
                if (MagiShield >= info.Damage)
                {
                    MagiShield -= info.Damage;
                    info.Damage = 0;
                    noCsDodge = true;
                    setToOne = true;
                }
                else
                {
                    info.Damage -= MagiShield;
                    MagiShield = 0;
                }
                if (MagiShield == 0)
                {
                    for (int i = 0; i < Player.buffType.Length; i++)
                    {
                        if (Player.buffTime[i] > 0 && Main.debuff[Player.buffType[i]])
                        {
                            Player.buffTime[i] = 0;
                        }
                    }
                    Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<MoonlightShieldBreak>(), 8000.ApplyAccArmorDamageBonus(Player), 0, Player.whoAmI);
                    Player.statMana = Player.statManaMax2;
                }
            }
            if (!setToOne)
            {
                if (info.Damage > 30)
                {
                    if (nihShellCount > 0)
                    {
                        nihShellCount--;
                        info.Damage = (int)(info.Damage * (1 - NihilityShell.HurtDamageReduce));
                        CEUtils.PlaySound("shielddown", 1, Player.Center);
                        for (int i = 0; i < 24; i++)
                        {
                            PRTLoader.NewParticle<PRT_TechyHolosquare>(Player.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(5, 9), new Color(165, 58, 222), Main.rand.NextFloat(0.8f, 2.4f)).Configure(40);
                        }
                    }
                }
            }

            if (setToOne && !info.Cancelled)
            {
                if (immune < 40)
                {
                    immune = 40;
                    Player.Heal(1);
                }
            }
        }
        public int OracleDeckHealCd = 0;
        public int effectCount = 0;
        public int shielddamagecd = 0;
        public int noItemTime = 0;
        public bool syncHoldingPoop = false;
        public int damageRecord = 0;

        public Item AzafureChargeShieldItem = null;
        public Item AzafureDriverShieldItem = null;
        public Item VoidCoreItem = null;

        public bool VSoundsPlayed = false;
        public bool maliciousCode = false;
        public int UICJ = 0;
        public int ilVortexType = -1;
        //脱离灾厄:潜行退役惰性字段(WeaponsNoCostRogueStealth/RogueStealthRegen/LastStealth/LastStealthStrikeAble/ResetStealth/shadowStealth/GaleWristbladeCharge)已裁删
        public int BrambleBarAdd = 0;
        public float BrambleBarCharge = 0;
        public int BBarNoDecrease = 0;
        public bool NDFlag = false;
        public bool ResetRot = false;
        public int TDeckTime = 0;
        public int SDeckTime = 0;
        //脱离灾厄:潜行退役惰性字段(StealthMaxLast/RstStealth)已裁删
        public int DmgAdd20 = 0;
        public bool accAzureAbyss = false;
        public int NihTwinArmorConnetPlayer = -1;
        public void SyncLife(Player plr = null)
        {
            if (plr == null)
                plr = Player;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;
            ModPacket p = Mod.GetPacket();
            p.Write((byte)CEMessageType.SyncPlayerLife);
            p.Write(plr.whoAmI);
            p.Write(plr.statLife);
            p.Send();
        }
        public Rope NihArmorRope = null;
        public void DrawNihRope()
        {
            var rope = NihArmorRope;
            if (rope == null)
            {
                return;
            }
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<Vector2> points = new List<Vector2>();
            points = rope.GetPoints();

            points.Insert(0, Player.Center);
            if (NihTwinArmorConnetPlayer >= 0)
            {
                points.Add(NihTwinArmorConnetPlayer.ToPlayer().Center);
                points.Add(NihTwinArmorConnetPlayer.ToPlayer().Center);
            }
            float lc = 1;
            float jn = 0;

            for (int i = 1; i < points.Count - 1; i++)
            {
                jn += CEUtils.getDistance(points[i - 1], points[i]) / (float)28 * lc;

                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 7 * lc,
                      new Vector3(jn, 1, 1),
                      Color.White));
                ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 7 * lc,
                      new Vector3(jn, 0, 1),
                      Color.White));

            }

            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (ve.Count >= 3)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                gd.Textures[0] = NihRopeTex.Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            }
        }
        public int FallSpeedUP = 0;
        public float MariviumLight = 0;
        public int AzureShield = 0;
        public List<int> smolderingSets;
        public bool smdVisual = false;
        public float veloCounter = 0;
        public float MaxBaitCharge = 1;
        public bool BaitUsable = false;
        public int lbaitType = -1;
        public override void PostUpdate()
        {
            if (BaitCharge < 0)
                BaitCharge = 0;
            if (Player.HeldItem.IsAir)
            {
                BaitCharge = 0;
            }
            else
            {
                if (BaitCharging)
                {
                    if (Player.HeldItem.type != lbaitType)
                        BaitCharge = 0;
                    lbaitType = Player.HeldItem.type;
                    if (BaitCharge < MaxBaitCharge)
                    {
                        float mul = 12;
                        if (Player.HeldItem.ModItem != null && Player.HeldItem is IBaitItem ibi)
                            mul = ibi.ChargeTimeMult;
                        float chargeSpeed = Player.GetTotalAttackSpeed(Player.HeldItem.DamageType) / (Player.HeldItem.useTime * mul);
                        BaitCharge += chargeSpeed;
                        if (BaitCharge >= MaxBaitCharge)
                        {
                            BaitCharge = MaxBaitCharge;
                        }
                        if(!BaitUsable && BaitCharge >= 1)
                        {
                            BaitUsable = true;
                            if (Main.myPlayer == Player.whoAmI)
                            {
                                CEUtils.PlaySound("BaitReady", 1.1f, Player.MountedCenter);
                            }
                        }
                    }
                }
                else
                {
                    BaitCharge = 0;
                }
            }
            
            BaitUsable = BaitCharge >= 1;
            if(respawnsnd && !Player.dead)
            {
                respawnsnd = false;

                if (Player.ownedProjectileCounts[ModContent.ProjectileType<Flowery>()] > 0)
                    CEUtils.PlaySound("VoiceClips/Respawn", 1, floweryPosition);
            }
            FlagRot *= 0.9f;
            if (Player.velocity.Length() > 1f)
            {
                int dr = (Player.velocity.X != 0) ? Math.Sign(Player.velocity.X) : Player.direction;
                Vector2 vl = Player.velocity * new Vector2(1, 1) * (dr < 0 ? -1 : 1);
                FlagRot = float.Lerp(FlagRot, vl.ToRotation(), 0.02f);
            }
            veloCounter += 0.1f;
            veloCounter += Player.velocity.Length() * 0.028f;
            while (veloCounter > 1)
            {
                veloCounter--;
                OathBannerFrameCount++;
                if (OathBannerFrameCount > 7)
                    OathBannerFrameCount = 0;
            }
            foreach(Player plr in Main.ActivePlayers)
            {
                if(plr.whoAmI != Player.whoAmI && plr.Entropy().oathBanner && plr.team == Player.team)
                {
                    if(plr.Distance(Player.Center) < OathBanner.TeamBuffRange)
                        Player.AddBuff(ModContent.BuffType<OathofCommand>(), 8);
                }
            }
            if(MewmewSmokeEffect > 0)
            {
                Player.immuneNoBlink = true;
                MewmewSmokeEffect--;
                if(Main.rand.NextBool(2))
                {
                    PRTLoader.NewParticle<PRT_EMediumSmoke>(CEUtils.randomPoint(Player.getRect()), CEUtils.randomPointInCircle(5) + new Vector2(0, -6), Color.Black, Main.rand.NextFloat(0.52f, 0.7f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), 50).wl = 0;
                }
            }
            if (smolderingSets == null)
            {
                smolderingSets = new List<int>() { ModContent.ItemType<SmolderingHelmet>(), ModContent.ItemType<SmolderingBreastplate>(), ModContent.ItemType<SmolderingGreaves>() };
            }
            bool fullSet = smolderingSet;
            int smdCount = 0;
            for (int i = 10; i < 13; i++)
            {
                if (smolderingSets.Contains(Player.armor[i].type))
                    smdCount++;
            }
            for (int i = 0; i < 3; i++)
            {
                if (smolderingSets.Contains(Player.armor[i].type))
                    smdCount++;
            }
            smdVisual = smdCount >= 3;
            int sType = ModContent.ProjectileType<SmolderingTail>();
            if ((smolderingSet || smdVisual) && Player.ownedProjectileCounts[sType] < 1)
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, sType, smolderingSet ? 30.ApplyAccArmorDamageBonus() : 0, 4, Player.whoAmI);
            if (Main.myPlayer == Player.whoAmI && Player.HeldItem.ModItem != null && Player.HeldItem.ModItem is IHoldoutItem ih)
            {
                int holdoutType = ih.ProjectileType;
                if (Player.ownedProjectileCounts[holdoutType] < 1)
                    Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.MountedCenter, (Main.MouseWorld - Player.Center).normalize() * Player.HeldItem.shootSpeed, holdoutType, Player.HeldItem.damage, Player.GetWeaponKnockback(Player.HeldItem), Player.whoAmI);
            }
            voidOreNearby--;
            AzureShield--;
            MariviumLight = float.Lerp(MariviumLight, MariviniumSet ? 1 : (accAzureAbyss ? (AzureShield > 0 ? 1f : 0.8f) : 0), 0.05f);
            if (accAzureAbyss)
            {
                if (Player.wet)
                {
                    if (Player.wingTime < Player.wingTimeMax)
                    {
                        if (!Player.controlJump)
                        {
                            Player.wingTime += 1.2f;
                            if (Player.wingTime > Player.wingTimeMax)
                                Player.wingTime = Player.wingTimeMax;
                        }
                    }
                }
            }
            if (AbyssalLight + MariviumLight > 0.02f)
            {
                //脱离灾厄:灾厄深渊黑暗系统(EnhancedDarknessSystem)光源登记随灾厄移除,保留自有发光
                if (MariviniumSet || accAzureAbyss)
                {
                    CEUtils.AddLight(Player.Center, Color.LightBlue * MariviumLight, 12f);
                }
            }
            BloodthirstyEffect *= 0.974f;
            if (CalamityEntropy.EntropyMode)
            {
                //脱离灾厄:伤害类遍历表收敛为原版+自有类(原灾厄17类)
                List<DamageClass> dmgClasses = new List<DamageClass>() { DamageClass.Generic, DamageClass.Default, DamageClass.Melee, DamageClass.MeleeNoSpeed, DamageClass.Ranged, DamageClass.Magic, DamageClass.Summon, DamageClass.SummonMeleeSpeed, DamageClass.Throwing, ModContent.GetInstance<Content.DamageClasses.NoDRMelee>(), ModContent.GetInstance<NoneTypeDamageClass>() };
                for (int i = 0; i < dmgClasses.Count; i++)
                {
                    DamageClass dc = dmgClasses[i];
                    if (Player.GetDamage(dc).Additive > 1)
                    {
                        float tv = (float)Math.Pow(Player.GetDamage(dc).Additive, 0.85f);
                        Player.GetDamage(dc) += tv - Player.GetDamage(dc).Additive;
                    }
                    if (Player.GetCritChance(dc) > 50)
                    {
                        Player.GetCritChance(dc) = 50;
                    }
                }
            }
            if (exquisiteCrown)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    int crptype = ModContent.ProjectileType<RubyCrown>();
                    if (Player.ownedProjectileCounts[crptype] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, crptype, 32, 0, Player.whoAmI);
                    }
                }
            }
            FallSpeedUP--;
            DmgAdd20--;
            ShieldAlphaAdd *= 0.95f;
            if (NihilitySet)
                NihilityShieldEnabled = true;
            if (ChaoticSet)
                SpawnChaoticCellOnHurt = true;

            if (NihTwinArmorConnetPlayer != -1)
            {
                var mp = NihTwinArmorConnetPlayer.ToPlayer().Entropy();
                if (Main.player[NihTwinArmorConnetPlayer].active && !Main.player[NihTwinArmorConnetPlayer].dead && (NihilitySet || ChaoticSet) && (mp.NihilitySet || mp.ChaoticSet))
                {
                    if (NihArmorRope == null)
                        NihArmorRope = new Rope(Player.Center, NihTwinArmorConnetPlayer.ToPlayer().Center, 30, 0, new Vector2(0, 0f), 0.006f, 15, false);

                    NihilityShieldEnabled = true;
                    SpawnChaoticCellOnHurt = true;
                    Player.lifeRegen += 4;
                    if (Player.whoAmI == Main.myPlayer)
                    {
                        if (Main.GameUpdateCount % 8 == 0 || Main.GameUpdateCount % 40 == 32)
                            SyncLife(Player);
                        if (Main.GameUpdateCount % 40 == 0)
                        {
                            if (NihTwinArmorConnetPlayer.ToPlayer().statLife < NihTwinArmorConnetPlayer.ToPlayer().statLifeMax2 - 20 && Math.Abs(Player.statLife - NihTwinArmorConnetPlayer.ToPlayer().statLife) > 60 && Player.statLife > NihTwinArmorConnetPlayer.ToPlayer().statLife)
                            {
                                int LifeTrans = int.Min(16, (Player.statLife - NihTwinArmorConnetPlayer.ToPlayer().statLife));
                                if (Player.statLife > LifeTrans)
                                {
                                    Player.statLife -= LifeTrans;
                                    NihTwinArmorConnetPlayer.ToPlayer().statLife += LifeTrans;

                                    SyncLife();
                                    SyncLife(NihTwinArmorConnetPlayer.ToPlayer());
                                    CEUtils.PlaySound("ksLand", 0.5f, Player.Center, 6, 0.6f);
                                }
                            }
                        }
                    }
                }
                else
                {
                    NihArmorRope = null;
                    NihTwinArmorConnetPlayer.ToPlayer().Entropy().NihTwinArmorConnetPlayer = -1;
                    NihTwinArmorConnetPlayer = -1;
                }
            }
            else
            {
                NihArmorRope = null;
            }
            if (NihArmorRope != null && NihTwinArmorConnetPlayer >= 0)
            {
                var vec1 = Player.Center;
                var vec2 = NihTwinArmorConnetPlayer.ToPlayer().Center;
                if (CEUtils.getDistance(vec1, vec2) < 10000)
                {
                    NihArmorRope.segmentLength = CEUtils.getDistance(vec1, vec2) / 35f;
                    NihArmorRope.Start = vec1;
                    NihArmorRope.End = vec2;
                    NihArmorRope.Update();
                }
                else
                {
                    NihArmorRope = null;
                }
            }
            if (ShootLaserTime > 0 && ShootLaserTime % 3 == 0)
            {
                if (lastHitTarget != null && lastHitTarget.active)
                {
                    Vector2 tpos = lastHitTarget.Center + lastHitTarget.velocity * 4;
                    Vector2 spos = Player.Center + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(70, 120);
                    Projectile.NewProjectile(Player.GetSource_FromThis(), spos, (tpos - spos).normalize() * 16, ModContent.ProjectileType<VENihilityLaser>(), ((int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(VoidEaterHelmet.LaserDamage)).ApplyAccArmorDamageBonus(Player), 8, Player.whoAmI);
                    if (CECooldowns.CheckCD("NihLaserSound", 1))
                        CEUtils.PlaySound("void_laser", 2.4f, spos, 6, 0.2f);
                }
            }
            if (NihilitySet || ChaoticSet)
            {
                if (!Main.dedServ && CEKeybinds.NihilityAndChaoticArmorConnectKey.JustPressed)
                {
                    foreach (var player in Main.ActivePlayers)
                    {
                        if (player.whoAmI != Player.whoAmI)
                        {
                            if (player.Distance(Main.MouseWorld) < 300)
                            {
                                if (player.Entropy().NihilitySet != NihilitySet && player.Entropy().ChaoticSet != ChaoticSet)
                                {
                                    if (NihTwinArmorConnetPlayer != -1 || player.Entropy().NihTwinArmorConnetPlayer != -1)
                                    {
                                        ModPacket pack = Mod.GetPacket();
                                        pack.Write((byte)CEMessageType.NihilityConnet);
                                        pack.Write(true);
                                        pack.Write(player.whoAmI);
                                        pack.Write(NihTwinArmorConnetPlayer);
                                        pack.Send();
                                    }
                                    else
                                    {
                                        ModPacket pack = Mod.GetPacket();
                                        pack.Write((byte)CEMessageType.NihilityConnet);
                                        pack.Write(false);
                                        pack.Write(Player.whoAmI);
                                        pack.Write(player.whoAmI);
                                        pack.Send();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (SpawnChaoticCellOnHurt && Main.myPlayer == Player.whoAmI)
            {
                if (ArmorSetBonusHotKey.JustPressed && Player.statLife > 90)
                {
                    if (CECooldowns.CheckCD("SummonCells", 120))
                    {
                        Player.statLife -= 80;
                        SyncLife();
                        CEUtils.PlaySound("ksLand", 0.7f, Player.Center);
                        int type = ModContent.ProjectileType<ChaoticCellMinion>();
                        for (int i = 0; i < 3; i++)
                        {
                            if (Player.ownedProjectileCounts[type] < ChaoticHelmet.MaxCells)
                            {
                                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, CEUtils.randomRot().ToRotationVector2() * 15, type, ((int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(ChaoticCellMinion.BaseDamage)).ApplyAccArmorDamageBonus(Player), 2, Player.whoAmI);
                            }
                        }
                    }
                }
            }
            ShootLaserTime--;
            UpdateDriverShield();
            UpdateNihShield();
            UpdateVoidShield(VoidCoreItem != null, VoidCoreShield.ID, ref VoidCoreShieldScale, ref VoidShield, ref VoidRegenDelay, ref VoidRecharge, VoidCore.MaxShield, VoidCore.ShieldRecharge);
            float s_ = 1;
            UpdateRatzielShield(RatzielShieldTime > 0, RatizelShieldCD.ID, ref s_, ref RatzielShield, ref RatzielRegenDelay, ref RatzielRecharge, Ratziel.MaxShield(Ratziel.Level()), 20 * 60);
            if (hasAcc(HungryLantern.ID))
            {
                if (Player.ownedProjectileCounts[HungryLantern.ProjType] < 1)
                {
                    int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, CEUtils.randomPointInCircle(8), HungryLantern.ProjType, HungryLantern.Damage, 0, Player.whoAmI);
                    p.ToProj().originalDamage = HungryLantern.Damage;
                }
            }
            if (Main.myPlayer == Player.whoAmI && BookMarkLoader.GetPlayerHeldEntropyBook(Player, out var eb))
            {
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkMariviumEffect>(Player) && Player.ownedProjectileCounts[BookmarkMarivium.ProjType] < 1)
                {
                    eb.ShootSingleProjectile(BookmarkMarivium.ProjType, Player.Center, Vector2.UnitY * -0.01f, 1, 1, 1);
                }
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkHammerBMEffect>(Player) && Player.ownedProjectileCounts[BookmarkHammer.ProjType] < 1)
                {
                    eb.ShootSingleProjectile(BookmarkHammer.ProjType, Player.Center, Vector2.UnitY * -0.02f, 1, 1, 1);
                }
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkSwordBMEffect>(Player) && Player.ownedProjectileCounts[BookmarkSword.ProjType] < 1)
                {
                    eb.ShootSingleProjectile(BookmarkSword.ProjType, Player.Center, Vector2.UnitY * -0.01f, 1, 1, 1);
                }
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkFairyEffect>(Player) && Player.ownedProjectileCounts[BookmarkFairy.ProjType] < 1)
                {
                    eb.ShootSingleProjectile(BookmarkFairy.ProjType, Player.Center, Vector2.UnitY * -0.01f, 1, 1, 1, (proj) => { proj.ai[2] = 0; });
                    eb.ShootSingleProjectile(BookmarkFairy.ProjType, Player.Center, Vector2.UnitY * -0.01f, 1, 1, 1, (proj) => { proj.ai[2] = 1; });
                    eb.ShootSingleProjectile(BookmarkFairy.ProjType, Player.Center, Vector2.UnitY * -0.01f, 1, 1, 1, (proj) => { proj.ai[2] = 2; });
                }
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<WarPactBMEffect>(Player))
                {
                    BookMarkLoader.GetPlayerHeldEntropyBook(Player, out var book);
                    WarPactBMEffect.CheckProj(book);
                }
                if (BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkLacewingsEffect>(Player) && Player.ownedProjectileCounts[BookmarkLacewings.ProjType] < 3)
                {
                    eb.ShootSingleProjectile(BookmarkLacewings.ProjType, Player.Center, Vector2.UnitY * -0.001f, 1, 1, 1);
                }
            }
            if (hasAcc("SoulDeck"))
                SDeckTime = 5;
            if (hasAcc("TaintedDeck"))
                TDeckTime = 5;
            SDeckTime--;
            TDeckTime--;
            foreach (var plr in Main.ActivePlayers)
            {
                if (plr.Distance(Player.Center) > 6000)
                    continue;
                if (plr.Entropy().SDeckTime > 0)
                {
                    HitCooldown += WisperCard.ImmuneAdd;
                }
                if (plr.Entropy().TDeckTime > 0)
                {
                    LifeStealP += 0.003f;
                }
            }
            StealthRegenDelay--;
            DontDrawTime--;

            /*for(float i = 0; i < 1; i += 0.05f)
            {
                var __prt = PRTLoader.NewParticle<PRT_SlashDarkRed>(Player.Center - Player.velocity, Player.velocity * i, Color.Red, Main.rand.NextFloat(0.1f, 0.12f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, Player.velocity.ToRotation(), 10);
                __prt.scw = 1f;
            }*/
            bool addSRec = true;
            if (Main.myPlayer == Player.whoAmI && BookMarkLoader.HeldingBookAndHasBookmarkEffect<BookmarkSulphurousBMEffect>(Player))
            {
                if (SulphurousBubbleRecharge >= 3600)
                {
                    addSRec = false;
                    SulphurousBubble = true;
                }
            }
            else
            {
                SulphurousBubble = false;
            }
            if (addSRec)
            {
                SulphurousBubbleRecharge++;
            }
            if (SnowgraveChargeTime-- > 0)
            {
                SnowgraveCharge += 0.001f;
                if (SnowgraveCharge >= 1)
                {
                    SnowgraveChargeTime = 0;
                    SnowgraveCharge = 1;
                }
            }
            if (roaringDye && Main.GameUpdateCount % 4 == 0)
            {
                var shadow = PRTLoader.NewParticle<PRT_PlayerShadow>(Player.position, new Vector2(Player.direction * -4, 0), Color.White, 1)
                    .Configure(1, true, PRTDrawModeEnum.AlphaBlend);
                shadow.plr = Player;
            }
            if (ResetRot)
            {
                ResetRot = false;
                Player.fullRotation = 0;
            }

            float mhrot = (Player.legs == EquipLoader.GetEquipSlot(Mod, "MariviniumLeggings", EquipType.Legs) ? 0f : 0.64f) + (float)Math.Cos(Main.GameUpdateCount * 0.04f) * 0.16f;
            float v = Player.velocity.Length();

            mhrot = Vector2.Lerp(mhrot.ToRotationVector2(), Player.velocity.normalize() * new Vector2(Player.direction, 1), (float)(1 - Math.Exp(-0.1 * v))).ToRotation();
            VanityTailRot = CEUtils.RotateTowardsAngle(VanityTailRot, mhrot, 0.2f, false);
            if (hasAcc(AzafureDetectionEquipment.ID))
            {
                if (Player.controlJump && Player.wingTime > 0f && Player.jump == 0 && Player.controlUp)
                {
                    Player.wingTime -= Player.wingTimeMax / 300f;
                    Player.velocity.Y -= 0.4f;
                }
            }
            if (ManaRegenTime-- > 0 && ManaRegenTime % 30 == 2)
            {
                Player.statMana += ManaRegenPer30Tick;
                if (Player.statMana > Player.statManaMax2)
                {
                    Player.statMana = Player.statManaMax2;
                }
            }
            if (hasAcc(SmartScope.ID) && Main.myPlayer == Player.whoAmI)
            {
                if (SmartScope.target != null && !SmartScope.target.active)
                {
                    SmartScope.target = null;
                }
                if (Mouse.GetState().MiddleButton == ButtonState.Pressed)
                {
                    SmartScope.target = CEUtils.FindTarget_HomingProj(Player, Main.MouseWorld, 800, (i) => i.ToNPC().realLife < 0);
                }
            }
            if (BrambleBarAdd-- > 0)
            {
                if (BrambleBarCharge < 1)
                {
                    BrambleBarCharge += 0.002f;
                }
                BBarNoDecrease = 120;
            }
            else
            {
                if (BBarNoDecrease-- < 0)
                {
                    if (BrambleBarCharge > 0)
                        BrambleBarCharge -= 0.002f;

                }
            }
            if (BrambleBarCharge < 0)
                BrambleBarCharge = 0;
            if (BrambleBarCharge > 1)
                BrambleBarCharge = 1;
            if (Player.GetModPlayer<VanityModPlayer>().vanityEquipped == nameof(LostHeirloom))
            {
                CEUtils.AddLight(Player.Center, Color.White * 0.8f);
            }
            if (shadowRune)
            {
                if (Player.GetDamage(DamageClass.Summon).Additive > 1)
                {
                    Player.GetDamage(DamageClass.Summon) -= Player.GetDamage(DamageClass.Summon).Additive - 1;
                }
            }
            if (Main.myPlayer == Player.whoAmI)
            {
                if (!Player.mount.Active && Player.miscEquips[3].type == ModContent.ItemType<TheReplicaofThePen>() && Player.ownedProjectileCounts[ModContent.ProjectileType<PenMinion>()] < 1)
                {
                    Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<PenMinion>(), 400, 1, Player.whoAmI);
                }
            }
            if (Player.mount.Type == ModContent.MountType<ReplicaPenMount>())
            {
                Player.velocity.Y *= 0.996f;
            }

            //脱离灾厄:shadowPact蓄影与潜行维护逻辑已随盗贼系统退役,字段与复位一并裁删
            if (ilVortexType == -1)
                ilVortexType = ModContent.ProjectileType<IlmeranVortex>();
            if (ilmeranAsylum && Main.myPlayer == Player.whoAmI)
            {
                if (Player.ownedProjectileCounts[ilVortexType] < 3)
                {
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ilVortexType, 1000.ApplyAccArmorDamageBonus(Player), 1, Player.whoAmI);
                }
            }
            if (Player.itemTime > 0 || Player.channel)
            {
                UICJ = 3;
            }
            UICJ--;
            if (UICJ > 0)
            {
                UsingItemCounter++;
            }
            else
            {
                UsingItemCounter = 0;
            }
            if (HealingCd > 0) HealingCd--;

            if (AzChargeShieldSteamTime > 0)
            {
                AzChargeShieldSteamTime--;
                if (AzChargeShieldSteamTime == 12)
                {
                    CEUtils.PlaySound("steam", 1, Player.Center);
                }
                if (AzChargeShieldSteamTime < 12)
                {

                    float c = AzChargeShieldSteamTime / 12f;
                    Vector2 steamCenter = Player.MountedCenter;
                    float rot = new Vector2(-1 * Player.direction, -2).ToRotation();
                    for (int i = 0; i < 8; i++)
                    {
                        var p = PRTLoader.NewParticle<PRT_Smoke>(steamCenter, rot.ToRotationVector2().RotatedByRandom(0.16f) * 8, Color.White * 0.42f * c, 0f);
                        p.timeleftmax = 36;
                        p.Lifetime = 36;
                        p.scaleEnd = Main.rand.NextFloat(0.06f, 0.16f);
                        p.vc = 0.94f;
                        p.Configure(0.03f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 36);
                        var p2 = PRTLoader.NewParticle<PRT_Smoke>(steamCenter + rot.ToRotationVector2().RotatedByRandom(0.16f) * 4, rot.ToRotationVector2().RotatedByRandom(0.16f) * 8, Color.White * 0.42f * c, 0.016f);
                        p2.timeleftmax = 36;
                        p2.Lifetime = 36;
                        p2.scaleEnd = Main.rand.NextFloat(0.06f, 0.16f);
                        p2.vc = 0.94f;
                        p2.Configure(0.01f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 36);
                    }
                }
            }
            if (XSpeedSlowdownTime-- >= 0)
            {
                Player.velocity.X *= 0.94f;
            }
            if (CalamityEntropy.EntropyMode && HitTCounter > 0)
            {
                Player.AddBuff(ModContent.BuffType<NoHeal>(), HitTCounter);
            }
            if (MariviniumSet)
            {
                if (MariviniumShieldCount < MariviniumHelmet.MaxShield)
                {
                    MariviniumShieldCd--;
                    if (MariviniumShieldCount > 0)
                    {
                        MariviniumShieldCd--;
                    }
                    if (MariviniumShieldCd <= 0)
                    {
                        MariviniumShieldCount++;
                        MariviniumShieldCd = MariviniumHelmet.ShieldCd.ApplyCdDec(Player);
                    }
                }
                else
                {
                    MariviniumShieldCd = MariviniumHelmet.ShieldCd.ApplyCdDec(Player);
                }
            }
            else
            {
                MariviniumShieldCount = 0;
                MariviniumShieldCd = MariviniumHelmet.ShieldCd.ApplyCdDec(Player);

            }
            if (bloodTrCD > 0)
                bloodTrCD--;
            if (deusCoreBloodOut < 0)
                deusCoreBloodOut = 0;
            if (deusCoreBloodOut > 0 && Main.GameUpdateCount % 2 == 0)
            {
                int dmgApply = (int)(deusCoreBloodOut * 0.01f + 1);
                if (dmgApply > deusCoreBloodOut)
                {
                    dmgApply = deusCoreBloodOut;
                }
                Player.statLife -= dmgApply;
                if (Player.statLife < 0)
                    Player.statLife = 0;
                if (Player.statLife < 1)
                {
                    Player.Hurt(PlayerDeathReason.ByCustomReason(Mod.GetLocalization("KilledByAstral" + Main.rand.Next(0, 10)).ToNetworkText(Player.name)), dmgApply, 0, false, true, 0, false, 0);
                }
                deusCoreBloodOut -= dmgApply;
            }
            itemTime--;
            if (Player.HasBuff<VoidVirus>())
            {
                Player.statDefense -= 15;
                lifeRegenPerSec = 0;
            }
            if (VFHelmSummoner)
            {
                // 2026-08-31 平衡案:套装仆从换为迷你虚空吞噬者(原 VoidMonster 退役)
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<MiniVoidDevourer>()] < 1)
                {
                    int mvd = Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<MiniVoidDevourer>(), (int)(Player.GetTotalDamage(DamageClass.Summon).ApplyTo(680)), 4, Player.whoAmI);
                    if (mvd >= 0 && mvd < Main.maxProjectiles)
                    {
                        Main.projectile[mvd].originalDamage = 680;
                    }
                }
            }
            serviceWhipDamageBonus *= 0.995f;
            if (serviceWhipDamageBonus > 0.009f)
            {
                Player.ClearBuff(ModContent.BuffType<ServiceBuff>());
                Player.AddBuff(ModContent.BuffType<ServiceBuff>(), (int)(serviceWhipDamageBonus * 10000));
            }
            if (nihShellCd > 0)
            {
                nihShellCd--;
            }
            if (Main.myPlayer == Player.whoAmI)
            {
                if (mawOfVoid)
                {
                    if (mawOfVoidUsing)
                    {
                        mawOfVoidCharge -= 1f / 60f;
                        if (mawOfVoidCharge <= 0)
                        {
                            mawOfVoidCharge = 0;
                            mawOfVoidUsing = false;
                        }
                    }
                    else
                    {
                        if (isUsingItem())
                        {
                            mawOfVoidCharge += 0.01f;
                            if (mawOfVoidCharge >= 1)
                            {
                                mawOfVoidCharge = 1;
                                if (Main.mouseRight)
                                {
                                    mawOfVoidUsing = true;
                                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<BloodRing>(), ((int)Player.GetDamage(Player.GetBestClass()).ApplyTo(MawOfTheVoid.Damage)).ApplyAccArmorDamageBonus(Player), 0, Player.whoAmI);
                                }
                            }
                        }
                        else
                        {
                            if (mawOfVoidCharge == 1)
                            {
                                mawOfVoidUsing = true;
                                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<BloodRing>(), ((int)Player.GetDamage(Player.GetBestClass()).ApplyTo(MawOfTheVoid.Damage)).ApplyAccArmorDamageBonus(Player), 0, Player.whoAmI);
                            }
                            else
                            {
                                mawOfVoidCharge -= 0.01f;
                                if (mawOfVoidCharge <= 0)
                                {
                                    mawOfVoidCharge = 0;
                                }
                            }
                        }
                    }
                }
                else
                {
                    mawOfVoidCharge = 0;
                }
                if (revelation)
                {
                    if (revelationUsing)
                    {
                        revelationCharge -= 1f / 60f;
                        if (revelationCharge <= 0)
                        {
                            revelationCharge = 0;
                            revelationUsing = false;
                        }
                    }
                    else
                    {
                        if (isUsingItem())
                        {
                            revelationCharge += 0.01f;
                            if (revelationCharge >= 1)
                            {
                                revelationCharge = 1;
                                if (Main.mouseRight)
                                {
                                    revelationUsing = true;
                                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<HolyBeamRevelation>(), ((int)Player.GetDamage(Player.GetBestClass()).ApplyTo(TheRevelation.Damage)).ApplyAccArmorDamageBonus(Player), 0, Player.whoAmI);
                                }
                            }
                        }
                        else
                        {
                            if (revelationCharge == 1)
                            {
                                revelationUsing = true;
                                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<HolyBeamRevelation>(), ((int)Player.GetDamage(Player.GetBestClass()).ApplyTo(TheRevelation.Damage)).ApplyAccArmorDamageBonus(Player), 0, Player.whoAmI);
                            }
                            else
                            {
                                revelationCharge -= 0.01f;
                                if (revelationCharge <= 0)
                                {
                                    revelationCharge = 0;
                                }
                            }
                        }
                    }
                }
                else
                {
                    revelationCharge = 0;
                }
            }
            if (holyGroundTime > 0)
            {
                holyGroundTime--;
            }
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<PhantomWyrm>()] <= 0 && wyrmPhantom)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, CEUtils.randomVec(14), ModContent.ProjectileType<PhantomWyrm>(), (int)(Player.GetTotalDamage(DamageClass.Summon).ApplyTo(Ystralyn.PhantomDamage)), 1, Player.whoAmI);
                }
            }
            int shadoidType = ModContent.ItemType<ShadoidPotion>();
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                if (Player.inventory[i].type == shadoidType)
                {
                    Player.inventory[i].healLife = 1 + (int)(Player.statLifeMax2 * 0.3f);
                }
            }
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is WhipOfServiceProjectile wos)
                {
                    if (p.Colliding(p.getRect(), Player.getRect()) && p.owner != Player.whoAmI)
                    {
                        if (wos.hitCd[Player.whoAmI] <= 0)
                        {
                            if (Player.whoAmI == Main.myPlayer)
                            {
                                Player.Hurt(PlayerDeathReason.ByPlayerItem(p.owner, p.owner.ToPlayer().HeldItem), p.owner.ToPlayer().HeldItem.damage, 0, true, false);
                            }
                            serviceWhipDamageBonus += 0.07f;
                            wos.hitCd[Player.whoAmI] = 1000;
                            //脱离灾厄:原被鞭击时返还灾厄怒气(rage)的联动已退役
                        }

                    }
                }
            }

            if (accWispLantern || visualWispLantern)
            {
                if (Player.whoAmI == Main.myPlayer && Player.ownedProjectileCounts[ModContent.ProjectileType<WispLanternProj>()] < 1)
                {
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<WispLanternProj>(), 0, 0, Player.whoAmI);
                }
            }

            if (!nihShell)
            {
                nihShellCount = 0;
            }
            if (holdingPoop)
            {
                Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi);
                Player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi);
            }
            if (noItemTime > 0)
            {
                noItemTime--;
            }
            if (brokenAnkh)
            {
                if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                {
                    if (!holdingPoop && CEKeybinds.PoopHoldHotKey is not null && CEKeybinds.PoopHoldHotKey.JustPressed)
                    {
                        if (PoopHold is not null)
                        {
                            PoopsUI.holdAnmj = 0.2f;
                            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, new Vector2(0, 0), PoopHold.ProjectileType(), 80.ApplyAccArmorDamageBonus(Player), 3, Player.whoAmI);
                            holdingPoop = true;
                            PoopHold = null;
                            CEUtils.PlaySound("poop_itemthrow");
                        }
                        else
                        {
                            if (poops.Count > 0)
                            {
                                PoopsUI.holdAnmj = 0.2f;
                                PoopHold = poops[0];
                                poops.RemoveAt(0);
                            }
                        }
                    }
                    if (!Main.dedServ && !holdingPoop && CEKeybinds.ThrowPoopHotKey is not null && CEKeybinds.ThrowPoopHotKey.JustPressed)
                    {
                        if (poops.Count > 0)
                        {
                            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, new Vector2(0, 0), poops[0].ProjectileType(), 80, 3, Player.whoAmI);
                            holdingPoop = true;
                            poops.RemoveAt(0);
                            CEUtils.PlaySound("poop_itemthrow");
                        }
                    }
                }
            }
            if (holdingPoop)
            {
                noItemTime = 12;
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (Main.mouseLeft)
                    {
                        holdingPoop = false;
                    }
                }
            }
            if (resetTileSets)
            {
                resetTileSets = false;
                Main.tileSolid = tileSolid;
                Main.tileSolidTop = solidTop;
                TileID.Sets.Platforms = tilePlatform;
                tileSolid = null;
                solidTop = null;
                tilePlatform = null;
            }
            
            if (HeatEffectTime > 0) HeatEffectTime--;
            //原本还有一路由幽邃魔灵在场逐帧点亮,该 Boss 移除后只剩 HeatEffectTime 这一路驱动
            if (HeatEffectTime > 0)
            {
                Player.ManageSpecialBiomeVisuals("HeatDistortion", true);
            }
            // 2026-08-31 平衡案:始源冠冕重做(2防/+25生命/蜂蜜),原每秒回血退役
            if (!Player.dead)
            {
                if (Player.statLife < Player.statLifeMax2 && lifeRegenPerSec > 0)
                {
                    lifeRegenCD--;
                    if (lifeRegenCD <= 0)
                    {
                        lifeRegenCD = 60;
                        Player.Heal(lifeRegenPerSec);
                    }
                }
            }
            vfcd--;

            if (VoidInspire > 0)
            {
                Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;
                VoidInspire--;
                if (VoidInspire == 0)
                {
                    VoidCharge = 0;
                }
            }
            else
            {
                if (!VSoundsPlayed && voidcharge >= 1)
                {
                    VSoundsPlayed = true;
                    //音效:原灾厄 PhantomHeartUse,按 sound-map 换自有 soulshine
                    SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/soulshine"));
                }
            }
            if (VoidInspire <= 0 && voidcharge < 1)
            {
                VSoundsPlayed = false;
            }
            if (!Main.dedServ && VoidCharge >= 1 && ArmorSetBonusHotKey.JustPressed)
            {
                if (VoidInspire <= 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/OmegaBlueAbility"), Player.Center);
                    VoidInspire = 600;
                    Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<VoidWraith>(), 2000.ApplyAccArmorDamageBonus(Player), 0, Player.whoAmI);
                }
            }

            if (!VFSet)
            {
                if (VoidCharge > 0)
                {
                    VoidCharge = 0;
                }
            }
            if (VoidCharge > 1)
            {
                VoidCharge = 1;
            }
            if (pot_time > 0)
            {
                pot_time--;
            }
            else
            {
                pot_amp = 0;
            }
            effectCount += 1;
            if (HMRegenCd > 0)
            {
                HMRegenCd -= 1;
            }
            if (MagiShield > 0 && shielddamagecd <= 0)
            {
                foreach (NPC n in Main.npc)
                {
                    if (n.active && !n.friendly && n.getRect().Intersects(Player.getRect()) && !n.dontTakeDamage)
                    {
                        NPC.HitInfo h = n.CalculateHitInfo(MagiShield, 0, false, 5);
                        Player.StrikeNPCDirect(n, h);
                        shielddamagecd = 30;
                    }
                }
            }
            shielddamagecd--;
            if (holyMoonlight)
            {
                // 2026-08-31 平衡案:护盾存在时魔力病持续时间减半
                if (MagiShield > 0)
                {
                    halfManaSick = true;
                    magiShieldCd = (30 * 60).ApplyCdDec(Player);
                }
                if (magiShieldCd > 0)
                {
                    if (MagiShield <= 0)
                    {
                        magiShieldCd -= 1;
                    }
                }
                else
                {
                    // 2026-08-31 平衡案:护盾取最大魔力50%,上限250
                    int magiShieldAddCount = int.Min(250, (int)(Player.statManaMax2 * 0.5f));
                    magiShieldCd = 30 * 60;
                    if (MagiShield < magiShieldAddCount)
                    {
                        MagiShield = magiShieldAddCount;
                        MagiShieldMax = magiShieldAddCount;
                        localPlayerMaxShield = magiShieldAddCount;
                    }
                }
                if (Player.EntropyCooldowns().cooldowns.TryGetValue(MoonlightShield.ID, out var value4))
                {
                    value4.timeLeft = MagiShieldMax - MagiShield;

                }
                else
                {
                    Player.AddCooldown(MoonlightShield.ID, MagiShieldMax);
                }
            }
            else
            {
                MagiShield = 0;

            }
            if (MagiShield <= 0)
            {
                Player.RemoveCooldown(MoonlightShield.ID);
            }
            List<Point> EdgeTiles = new List<Point>();
            Collision.GetEntityEdgeTiles(EdgeTiles, Player);
            foreach (Point touchedTile in EdgeTiles)
            {
                Tile tile = Framing.GetTileSafely(touchedTile);
                if (!tile.HasTile || !tile.HasUnactuatedTile)
                    continue;
                float auricRejectionKB = Player.noKnockback ? 20f : 40f;
                if (tile.TileType == ModContent.TileType<AuricBoulderTile>())
                {
                    Player.RemoveAllGrapplingHooks();


                    var yeetVec = Vector2.Normalize(Player.Center - touchedTile.ToWorldCoordinates());
                    Player.velocity += yeetVec * auricRejectionKB;
                    //死亡播报文案键 Mods.CalamityEntropy.Status.Death.AuricRejection 已在 hjson 登记
                    Player.Hurt(PlayerDeathReason.ByCustomReason(CEUtils.GetText("Status.Death.AuricRejection").ToNetworkText(Player.name)), 460, 0);
                    Player.AddBuff(BuffID.Electrified, 300);

                    //音效:原灾厄 TeslaShoot1,按 sound-map 换自有 shockBlast
                    SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/shockBlast"));
                }
            }
            if (VaMoving > 0)
            {
                foreach (NPC n in Main.npc)
                {
                    if (n.Hitbox.Intersects(Player.Hitbox) && n.active)
                    {
                        //脱离灾厄:原盗贼类连带伤害无新职业裁定,按通用类结算(与DarkArts一致)
                        Player.ApplyDamageToNPC(n, (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(1200), 0, 0, false, DamageClass.Generic);

                    }
                }
                daPoints.Add(Player.Center - Player.velocity * 0.5f);
                daPoints.Add(Player.Center);
            }
            VaMoving--;
            if (Player.Entropy().oracleDeck)
            {
                if (OracleDeckHealCd <= 0)
                {
                    // 2026-08-31 平衡案:每10秒固定一跳(不吃冷却缩减)
                    OracleDeckHealCd = 600;
                    if (CEUtils.getDistance(Main.LocalPlayer.Center, Player.Center) < 6000)
                    {
                        if (Main.LocalPlayer.statLife < Main.LocalPlayer.statLifeMax2)
                        {
                            Main.LocalPlayer.Heal(20);
                        }
                    }
                }
            }
            if (OracleDeckHealCd > 0)
            {
                OracleDeckHealCd--;
            }
            // 莱拉:为自己与附近队友提供蜂蜜增益(各端只给本地玩家上buff,天然同步)
            if (Player.Entropy().leylaAura && !Main.dedServ && !Main.LocalPlayer.dead
                && (Player.whoAmI == Main.myPlayer || Main.LocalPlayer.team == Player.team)
                && CEUtils.getDistance(Main.LocalPlayer.Center, Player.Center) < 2000)
            {
                Main.LocalPlayer.AddBuff(BuffID.Honey, 2);
            }
            //脱离灾厄:灾厄NOU减益判定随灾厄移除(该减益已无施加源)
            if (Player.whoAmI == Main.myPlayer)
            {
                if (!Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<VoidEcho>())
                {
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<VoidEchoProj>()] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<VoidEchoProj>(), Player.HeldItem.damage, 0, Player.whoAmI);
                    }
                }
                if (!Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<Mercy>())
                {
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<HB>()] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<HB>(), Player.HeldItem.damage, 0, Player.whoAmI);
                    }
                }
                if (!Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<GhostdomWhisper>())
                {
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<GhostdomWhisperHoldout>()] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<GhostdomWhisperHoldout>(), Player.HeldItem.damage, 0, Player.whoAmI);
                    }
                }
                if (!Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<HadopelagicEchoII>())
                {
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<HadopelagicEchoIIProj>()] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<HadopelagicEchoIIProj>(), Player.HeldItem.damage, 0, Player.whoAmI);
                    }
                }
                if (!Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<RailPulseBow>())
                {
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<RailPulseBowProjectile>()] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<RailPulseBowProjectile>(), Player.HeldItem.damage, 0, Player.whoAmI);
                    }
                }
                if (!Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<Oblivion>())
                {
                    if (Player.ownedProjectileCounts[ModContent.ProjectileType<OblivionHoldout>()] <= 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<OblivionHoldout>(), Player.HeldItem.damage, 0, Player.whoAmI);
                    }
                }
            }
            if (Player.HasBuff(ModContent.BuffType<StealthState>()))
            {
            }
            else
            {

                if (DarkArtsTarget.Count > 0)
                {
                    Player.velocity *= 0;
                    if (daCd <= 0)
                    {
                        daCd = 4;

                        if (DarkArtsTarget[0] is NPC npc)
                        {
                            npc.Entropy().daTarget = false;
                            int od = npc.defense;
                            npc.defense = 0;
                            //脱离灾厄:灾厄DR清零联动退役,盗贼类改通用类
                            Player.ApplyDamageToNPC(npc, (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(460 + 50 * daCount), 0, 0, false, DamageClass.Generic);
                            npc.defense = od;
                            daLastP = npc.Center;
                        }
                        else
                        {
                            if (DarkArtsTarget[0] is Projectile proj)
                            {
                                proj.Entropy().daTarget = false;
                                proj.Kill();
                                daLastP = proj.Center;
                            }
                        }
                        SoundEffect se = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/da3").Value;
                        if (se != null) { se.Play(Main.soundVolume, 0, 0); }
                        daCount++;
                        if (DarkArtsTarget.Count > 0)
                            DarkArtsTarget.RemoveAt(0);
                        if (DarkArtsTarget.Count > 0)
                        {
                            daPoints.Add(Vector2.Lerp(daLastP, DarkArtsTarget[0].Center, (float)(4 - daCd) / 2f));

                        }
                        if (DarkArtsTarget.Count == 0)
                        {
                            foreach (NPC n in Main.npc)
                            {
                                if (CEUtils.getDistance(n.Center, Player.Center) < 140)
                                {
                                    int od = n.defense;
                                    n.defense = 0;
                                    int dmg = 0;
                                    for (int i = daCount; i > 0; i--)
                                    {
                                        dmg += 460 + 50 * i;
                                    }
                                    Player.ApplyDamageToNPC(n, (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(dmg), 0, 0, false, DamageClass.Generic);
                                    n.defense = od;
                                }
                            }
                            daCount = 0;
                            daPoints.Add(Vector2.Lerp(daLastP,
                                Player.Center, 0.2f));
                            daPoints.Add(Vector2.Lerp(daLastP,
                                                            Player.Center, 0.4f));
                            daPoints.Add(Vector2.Lerp(daLastP,
                                                            Player.Center, 0.6f));
                            daPoints.Add(Vector2.Lerp(daLastP,
                                                            Player.Center, 0.8f));
                            daPoints.Add(Vector2.Lerp(daLastP,
                                Player.Center, 1f));

                        }
                    }
                }

            }
            if (daCd > 0)
            {
                daCd--;
            }
            if (DarkArtsTarget.Count == 0 || daPoints.Count > 60)
            {
                if (daPoints.Count > 0)
                    daPoints.RemoveAt(0);
            }
            if (JustHit)
            {
                Player.immuneTime = (int)(Player.immuneTime * (1 + HitCooldown));
            }
            JustHit = false;
        }
        public int daCd = 0;
        public int daCount = 0;
        public List<Entity> DarkArtsTarget = new List<Entity>();
        public int VaMoving = 0;
        public int twinSpawnIndex = -1;
        private int MagiShieldMax = 1;
        public float attackSpeed = 0;
        public int bloodTrCD = 0;
        public int MariviniumShieldCount = 0;
        public int MariviniumShieldCd = 12 * 60;
        public bool visualMagiShield = false;
        //脱离灾厄:潜行退役惰性字段RogueStealthRegenMult已裁删(Vigorous词缀已改走充能速度)
        public int baitHeldType = -1;
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Player.dead)
                return;
            if (reincarnationBadge)
            {
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<RbCircle>()] < 1)
                {
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile.NewProjectile(Player.GetSource_FromAI(), Player.Center, Vector2.Zero, ModContent.ProjectileType<RbCircle>(), 0, 0, Player.whoAmI);
                    }
                }
                if (!Main.dedServ && AccessoryAbilityHotKey.JustPressed || (rBadgeActive && (Player.controlJump || rBadgeCharge <= 0)))
                {
                    rBadgeActive = !rBadgeActive;
                    if (rBadgeActive)
                    {
                        Player.mount.Dismount(Player);
                        SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AscendantActivate"), Player.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AscendantOff"), Player.Center);
                        Player.velocity *= 0.2f;
                    }
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket pack = Mod.GetPacket();
                        pack.Write((byte)CEMessageType.PlayerSetRB);
                        pack.Write(Player.whoAmI);
                        pack.Write(rBadgeActive);
                        pack.Send();
                    }
                }
                if (Player.controlMount)
                {
                    if (rBadgeActive)
                    {
                        rBadgeActive = false;
                        SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/AscendantOff"), Player.Center);
                        Player.velocity *= 0.2f;
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            ModPacket pack = Mod.GetPacket();
                            pack.Write((byte)CEMessageType.PlayerSetRB);
                            pack.Write(Player.whoAmI);
                            pack.Write(rBadgeActive);
                            pack.Send();
                        }
                    }
                }
                if (rBadgeActive)
                {
                    rBadgeCharge -= 0.025f;
                }
                else
                {
                    rBadgeCharge += 0.01f;
                    if (rBadgeCharge > 12)
                    {
                        rBadgeCharge = 12;
                    }
                }
            }
            else
            {
                rBadgeActive = false;
            }

            //脱离灾厄:暗影斗篷原按潜行值加成并清空潜行,潜行系统退役后改为固定基础伤害+冷却门槛
            if (!Main.dedServ && hasAcc(ShadowMantle.ID) && Player.whoAmI == Main.myPlayer && AccessoryAbilityHotKey.JustPressed)
            {
                if (!Player.HasCooldown(ShadowDashCD.ID))
                {
                    Player.AddCooldown(ShadowDashCD.ID, ShadowMantle.CooldownTicks);
                    immune = 16;
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, (Main.MouseWorld - Player.Center).normalize() * 800, ModContent.ProjectileType<ShadowMantleSlash>(), (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(((int)(1 + ShadowMantle.BaseDamage)).ApplyAccArmorDamageBonus(Player)), 0, Player.whoAmI);
                }
            }
        }
        public override void PostUpdateEquips()
        {
            // 护盾吸收/受击生成细胞的开关必须在受伤结算前就绪。
            // 只在 PostUpdate 里推导会晚一步:ResetEffects 每帧清 false,受击发生在 PostUpdate 之前,
            // 结算时开关恒为 false(历史 bug:噬虚者护盾能生成但无法生效)。联结伙伴同理。
            if (NihilitySet)
                NihilityShieldEnabled = true;
            if (ChaoticSet)
                SpawnChaoticCellOnHurt = true;
            if (NihTwinArmorConnetPlayer != -1 && Main.player[NihTwinArmorConnetPlayer].active && !Main.player[NihTwinArmorConnetPlayer].dead)
            {
                EModPlayer mp = NihTwinArmorConnetPlayer.ToPlayer().Entropy();
                if ((NihilitySet || ChaoticSet) && (mp.NihilitySet || mp.ChaoticSet))
                {
                    NihilityShieldEnabled = true;
                    SpawnChaoticCellOnHurt = true;
                }
            }
            if (!Player.HeldItem.IsAir && Player.HeldItem.ModItem != null)
            {
                if(Player.HeldItem.ModItem is Fooveria)
                {
                    if(Player.ItemTimeIsZero && NPC.downedBoss1)
                    {
                        Player.statDefense += 6;
                    }
                }
                if (Player.HeldItem.ModItem is IBaitItem)
                {
                    BaitCharging = true;
                    if (baitHeldType <= 0)
                        baitHeldType = ModContent.ProjectileType<BaitHeldEffect>();
                    if (Player.whoAmI == Main.myPlayer && Player.ownedProjectileCounts[baitHeldType] == 0)
                    {
                        Projectile.NewProjectile(Player.GetSource_FromThis(), Player.MountedCenter, Vector2.Zero, baitHeldType, 0, 0, Player.whoAmI);
                    }
                }
            }
            if (exquisiteCrown && rottenFangs)
                Player.maxMinions++;
            //脱离灾厄:血神圣杯(chaliceOfTheBloodGod)与月光护盾互斥的灾厄联动已退役
            if (DmgAdd20 > 0)
                Player.GetDamage(DamageClass.Generic) += 0.25f;


            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == Player.whoAmI && p.ModProjectile != null && p.ModProjectile is StarlightMothMinion smm)
                {
                    if (p.ai[1] == 1)
                    {
                        Player.lifeRegen += 5;
                    }
                }
                if (p.owner == Player.whoAmI && p.ModProjectile != null && p.ModProjectile is CarverSpirit cs && cs.mode == CarverSpirit.Mode.Defending)
                {
                    // 2026-08-31 平衡案:防御魔灵加成降至每个4防御+2%伤害减免
                    Player.statDefense += 4;
                    Player.endurance += 0.02f;
                }
            }
            //脱离灾厄:潜伏攻击消耗减免(stealthStrike75Cost/HalfCost)已随盗贼系统退役
            //脱离灾厄:ShadowMantle 的惰性 RogueStealthRegen 写入已删(该饰品已自研突进增伤窗口)
            if (soulDisorder)
            {
                Player.statDefense -= 16;
            }
            if (Player.HeldItem.prefix == ModContent.PrefixType<Echo>())
            {
                //脱离灾厄:回响词缀原+12%潜行上限,潜行退役后改为+6%通用伤害(后续按武器大招体系重做)
                Player.GetDamage(DamageClass.Generic) += 0.06f;
            }
            if (Player.HeldItem.prefix == ModContent.PrefixType<Vigorous>())
            {
                //脱离灾厄:原+15%潜行回复,按rogue-weapons通用换算(x1.5)改为大招充能速度+22.5%
                Player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.225f;
            }
            //脱离灾厄:疾风腕刃旧充能链(GaleWristbladeCharge/WindPressureTime/WindPressure buff)全仓无写入点,已裁删;新效果在GaleWristbladesPlayer
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is WispLanternProj wl)
                {
                    wl.applyEffects();
                }
            }
            if (holyGroundTime > 0)
            {
                Player.GetAttackSpeed(DamageClass.Generic) += 1;
                Player.GetDamage(DamageClass.Generic) += 1;
            }
            // 2026-08-31 平衡案:渊洋神迹套装奖励重做,原溢出暴击转伤害的旧奖励退役
            Player.GetDamage(DamageClass.Generic) += serviceWhipDamageBonus;
            Player.GetCritChance(DamageClass.Generic) += (int)(serviceWhipDamageBonus * 180);
            Player.pickSpeed -= serviceWhipDamageBonus * 4.3f;
            Player.GetAttackSpeed(DamageClass.Generic) += serviceWhipDamageBonus * 2.2f;
            if (Player.pickSpeed < 0)
            {
                Player.pickSpeed = 0;
            }
            if (holyGroundTime > 0)
            {
                dodgeChance += 0.5f;
            }
            Player.manaCost *= ManaCost;
            //脱离灾厄:SilvasCrown 原有的灾厄防御损伤(defenseDamageRatio)联动已退役
            if (Player.Entropy().wisdomCard)
            {
                Player.manaCost *= WisdomCard.ManaCostMul;
            }
            if (Player.Entropy().oracleDeck)
            {
                // 2026-08-31 平衡案:魔力减耗 30%→10%
                Player.manaCost *= 0.9f;
            }
            if (MagiShield > 0)
            {
                Player.manaCost *= 0.9f;
            }
            // 2026-08-31 平衡案:虚灵宙法盔职业奖励重做,原-25%魔力消耗退役(改为魔力病减半+命中强再生)
            if (GreedCard)
            {
                float greedDamage = Player.maxMinions * Content.Items.Accessories.EvilCards.GreedCard.DamagePerMinion;
                if (greedDamage > Content.Items.Accessories.EvilCards.GreedCard.DamageCap)
                {
                    greedDamage = Content.Items.Accessories.EvilCards.GreedCard.DamageCap;
                }
                Player.GetDamage(DamageClass.Generic) += greedDamage;
            }
            if (VoidInspire > 0)
            {
                Player.GetDamage(DamageClass.Generic) += 0.4f;
                //脱离灾厄:原灾厄infiniteFlight,改为每帧回满飞行时间的自研等价
                Player.wingTime = Player.wingTimeMax;
            }
            // 2026-08-31 平衡案:无垠重做,强化魔力贡献退役(魔流改为+3%魔法暴击伤害/层)
            // 2026-08-31 平衡案:堕化卡组自然再生惩罚改为-100%(见 UpdateLifeRegen),原30%惩罚退役
            if (shadowRune)
            {
                Player.maxMinions += (int)((Player.GetDamage(DamageClass.Summon).Additive - 1.0f) * ShadowRune.SummonDmgToMinionSlot);
            }
            //脱离灾厄:worshipRelic 原-50%潜行上限的代价已随盗贼系统退役
            if (hasAcc(AmuletOfSanctuary.ID))
            {
                int defence = AmuletOfSanctuary.GetDefence(Player.maxMinions);
                Player.statDefense += defence;
            }
            if (hasAcc(AmuletOfHealing.ID))
            {
                int regen = AmuletOfHealing.GetRegen(Player.maxMinions);
                Player.lifeRegen += regen;
            }
            //脱离灾厄:RogueAccessoriesProvide40Stealth 配置及潜行上限赠予已随盗贼系统退役
        }
        public override void ModifyScreenPosition()
        {
            Main.screenPosition.Y -= Player.height * (Scale * 0.5f - 0.5f);
            Main.screenPosition = Vector2.Lerp(Main.screenPosition, screenPos - Main.ScreenSize.ToVector2() / 2, screenShift <= 1 ? screenShift : 1);

            var shaker = Main.rand;
            Main.screenPosition += new Vector2(shaker.Next((int)-CalamityEntropy.Instance.screenShakeAmp * 8, (int)CalamityEntropy.Instance.screenShakeAmp * 8 + 1), shaker.Next((int)-CalamityEntropy.Instance.screenShakeAmp, (int)CalamityEntropy.Instance.screenShakeAmp + 1));

            Main.screenPosition += ScreenShaker.GetShiftVec();
        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Player.HasBuff(ModContent.BuffType<StealthState>()) || DarkArtsTarget.Count > 0)
            {
                a = 0.4f;
                r = 0.2f;
                g = 0.2f;
                b = 0.2f;
            }
            if(MewmewSmokeEffect > 0)
            {
                float c = 1 - float.Min(1, MewmewSmokeEffect / 50f);
                a = 1f;
                r = g = b = c;
            }
        }
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<BloodRing>()] > 0)
            {
                Player.headRotation = MathHelper.ToRadians(Player.direction * -(45 + 16f * (float)(Math.Cos(Main.GameUpdateCount * 0.1f))));
            }
        }
        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            health = StatModifier.Default;
            health.Base = CruiserLoreBonus.ToInt() * CruiserLore.LifeBoost;

            mana = StatModifier.Default;
            mana.Base = CruiserLoreBonus.ToInt() * CruiserLore.ManaBoost;
        }
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (Player.HasBuff(ModContent.BuffType<StealthState>()) || DarkArtsTarget.Count > 0 || VaMoving > 0)
            {
                return false;
            }
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (Player.HasBuff(ModContent.BuffType<StealthState>()) || DarkArtsTarget.Count > 0 || VaMoving > 0 || proj.Entropy().vdtype >= 0 || proj.ModProjectile is GodSlayerRocketProjectile)
            {
                return false;
            }
            return base.CanBeHitByProjectile(proj);
        }
        public bool resetTileSets = false;
        public bool wyrmPhantom = false;

        public override void SetControls()
        {
            if (Player.mount.Type == ModContent.MountType<ReplicaPenMount>())
            {
                if (Player.controlUp)
                {
                    Player.controlJump = true;
                }
            }
            if (rBadgeActive)
            {
                cDown = Player.controlDown;
                cLeft = Player.controlLeft;
                cRight = Player.controlRight;
                cUp = Player.controlUp;
                Player.controlDown = false;
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
            }
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == voidslashType && p.ModProjectile is VoidSlash vs && vs.d < 16)
                {
                    cDown = Player.controlDown;
                    cLeft = Player.controlLeft;
                    cRight = Player.controlRight;
                    cUp = Player.controlUp;
                    Player.controlDown = false;
                    Player.controlLeft = false;
                    Player.controlRight = false;
                    Player.controlUp = false;
                    break;
                }
            }
        }

        public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
        {
            // 群系判定按 biome-map.md（GreedCard→腐化/猩红钓鱼；灾厄 Voidstone 词条整体删除；FetalDream→血月海洋）。
            // 词条为先命中先得的优先级结构：命中即返回，替代旧的顺序赋值互相覆盖（2026-08-27 修正）。
            if ((Player.ZoneCorrupt || Player.ZoneCrimson) && Main.rand.NextBool(3))
            {
                itemDrop = ModContent.ItemType<GreedCard>();
                return;
            }
            if (!Player.ZoneBeach)
                return;
            // 血月稀有档最高优先（沿用原裁定）。最终期望：血月海洋 rare 渔获 1/8
            if (Main.bloodMoon && attempt.rare && Main.rand.NextBool(8))
            {
                itemDrop = ModContent.ItemType<FetalDream>();
                return;
            }
            // 沉沦海书签：最终期望 = 标称 1/20，仅血月 rare 被 FetalDream 命中时抢占
            if (Main.rand.NextBool(20))
            {
                itemDrop = ModContent.ItemType<BookMarkSunkenSea>();
                return;
            }
            // 增补段（bookmark-rehang §五）：困难海洋，标称 1/20。最终期望 ≈ 19/20 × 1/20 = 4.75%
            if (Main.hardMode && Main.rand.NextBool(20))
            {
                itemDrop = ModContent.ItemType<AbyssalPiercer>();
                return;
            }
        }

        // 熵之馈赠一次性发放旗标（misc-map §五增补段），随 EntropyBoosts 列表存档
        public bool starterBagReceived = false;
        public override void OnEnterWorld()
        {
            if (starterBagReceived || !ModContent.GetInstance<ServerConfig>().ExtraItemsInStarterBag)
                return;
            // 礼包本体（EntropyStarterBag）按名字松耦合查找，类型存在即生效；
            // 发放成功才置旗标，老角色可在礼包实装后进世界补领
            if (Mod.TryFind<ModItem>("EntropyStarterBag", out ModItem starterBag))
            {
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), starterBag.Type);
                starterBagReceived = true;
            }
        }

        public override void Initialize()
        {
            CruiserLoreBonus = false;
        }
        public override void SaveData(TagCompound tag)
        {
            var boost = new List<string>();
            boost.AddWithCondition("CruiserLore", CruiserLoreBonus);
            boost.AddWithCondition("NihTwinLore", NihilityTwinLoreBonus);
            boost.AddWithCondition("ProphetLore", ProphetLoreBonus);
            boost.AddWithCondition("StarterBagGot", starterBagReceived);

            tag["EntropyBoosts"] = boost;
            tag["LoreEnabled"] = enabledLoreItems;
            tag["yuzuCheck"] = yuzuCheck;
            if (EBookStackItems != null)
            {
                int BookMarks = EBookStackItems.Count;
                foreach (var item in EBookStackItems)
                {
                    if (item.type != ItemID.None)
                    {
                        tag["EntropyBookMarks"] = BookMarks;
                        //Mod.Logger.Info("Saved book marks:" + BookMarks.ToString());
                        for (int i = 0; i < BookMarks; i++)
                        {
                            tag["EntropyBookMark" + i.ToString()] = ItemIO.Save(EBookStackItems[i]);
                        }
                        break;
                    }
                }
            }
        }



        public List<Item> EBookStackItems = null;
        public bool soulDisorder = false;
        public bool obscureCard = false;
        public bool grudgeCard = false;
        public bool mourningCard = false;
        public bool bitternessCard = false;
        public bool soulDeckInInv = false;

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            if (enabledLoreItems == null)
                enabledLoreItems = new();
            if (EBookStackItems == null)
                EBookStackItems = new();
            var mp = Mod.GetPacket();
            mp.Write((byte)CEMessageType.SyncPlayer);
            mp.Write(Player.whoAmI);
            mp.Write(enabledLoreItems.Count);
            foreach (var item in enabledLoreItems)
            {
                mp.Write(item);
            }
            mp.Send();

            //冷却框架加入同步接线(见 cooldown-api.md §7):序列化由 CECooldownPlayer 提供
            var cdPack = Mod.GetPacket();
            cdPack.Write((byte)CEMessageType.SyncCooldowns);
            Player.GetModPlayer<CECooldownPlayer>().WriteAllCooldowns(cdPack);
            cdPack.Send(toWho, fromWho);
        }
        public void SyncBookmarks()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)CEMessageType.SyncBookmarks);
                    packet.Write(Player.whoAmI);
                    packet.Write(EBookStackItems.Count);
                    foreach (var item in EBookStackItems)
                    {
                        packet.Write(item.type);
                        ItemIO.Send(item, packet);
                    }
                    packet.Send();
                }
            }
        }
        public override void LoadData(TagCompound tag)
        {
            var boost = tag.GetList<string>("EntropyBoosts");
            CruiserLoreBonus = boost.Contains("CruiserLore");
            NihilityTwinLoreBonus = boost.Contains("NihTwinLore");
            ProphetLoreBonus = boost.Contains("ProphetLore");
            starterBagReceived = boost.Contains("StarterBagGot");
            enabledLoreItems = [.. tag.GetList<int>("LoreEnabled")];
            EBookStackItems = new List<Item>();
            if (tag.TryGet<bool>("yuzuCheck", out bool yz))
                yuzuCheck = yz;
            if (tag.ContainsKey("EntropyBookMarks"))
            {
                int BookMarks = (int)tag["EntropyBookMarks"];
                for (int i = 0; i < BookMarks; i++)
                {
                    var item = ItemIO.Load((TagCompound)tag["EntropyBookMark" + i.ToString()]);
                    if (item.type != ItemID.None)
                    {
                        //Mod.Logger.Info("Loaded mark:" + item.Name);
                        EBookStackItems.Add(item);
                    }
                }
            }

        }
    }
}
