global using Microsoft.Xna.Framework;
using CalamityEntropy.Common;
using CalamityEntropy.Content.ArmorPrefixes;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.ILEditing;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Items.Accessories;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Books.BookMarks;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.MusicBoxes;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.NPCs.Apsychos;
using CalamityEntropy.Content.NPCs.Cruiser;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Prophet;
using CalamityEntropy.Content.Skies;
using CalamityEntropy.Content.UI;
using CalamityEntropy.Content.UI.EntropyBookUI;
using CalamityEntropy.Content.UI.Poops;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Core.Hooks;
using CalamityEntropy.Core.Integrations;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.UI;
namespace CalamityEntropy
{
    public partial class CalamityEntropy : Mod
    {
        internal static List<ICELoader> ILoaders { get; private set; }
        public static ref bool EntropyMode => ref EDownedBosses.EntropyMode;
        public static bool AprilFool = false;
        public static CalamityEntropy Instance;
        public static int noMusTime = 0;
        public float screenShakeAmp = 0;
        public float cvcount = 0;
        public ArmorForgingStationUI armorForgingStationUI;
        public UserInterface userInterface;
        public static DynamicSpriteFont efont1;
        public static DynamicSpriteFont efont2;
        public static DynamicSpriteFont efont3;
        public static float cutScreenVel = 0;
        public static float cutScreen = 0;
        public static float cutScreenRot = 0;
        public static Vector2 cutScreenCenter = Vector2.Zero;
        public static float FlashEffectStrength = 0;
        public static float blackMaskAlpha = 0;
        public static int blackMaskTime = 0;
        public static Dictionary<int, Projectile> Proj_ID_To_Instance { get; set; } = null;
        public static SoundEffect ealaserSound = null;
        public static SoundEffect ealaserSound2 = null;
        public static SoundEffect ofCharge = null;
        public override void Load() {
            VanityDisplaySys.VanityItems = new();
            CEUtils.TexCache = new Dictionary<string, Texture2D>();
            BookMarkLoader.CustomBMEffectsByName = new Dictionary<string, BookMarkLoader.BookmarkEffectFunctionGroups>();
            BookMarkLoader.CustomBMByID = new Dictionary<int, BookMarkLoader.BookMarkTag>();
            Instance = this;
            Proj_ID_To_Instance = new Dictionary<int, Projectile>();
            DateTime today = DateTime.Now;
            AprilFool = today.Month == 4 && today.Day == 1;
            CEUtils.SoundStyles = new Dictionary<string, Terraria.Audio.SoundStyle>();

            ShadowCrystalDeltarune.Load();

            ILoaders = new List<ICELoader>();
            string name = typeof(ICELoader).Name;
            Type[] anyModCodeType = VaultUtils.GetAnyModCodeType();
            foreach (Type type in anyModCodeType) {
                if (type.IsClass && !type.IsAbstract && type.GetInterface(name) != null && RuntimeHelpers.GetUninitializedObject(type) is ICELoader item) {
                    ILoaders.Add(item);
                }
            }
            foreach (ICELoader setup in ILoaders) {
                setup.LoadData();
                setup.DompLoadText();
            }
            LoopSoundManager.init();

            CEWikithisIntegration.Register();

            efont1 = ModContent.Request<DynamicSpriteFont>("CalamityEntropy/Assets/Fonts/EFont", AssetRequestMode.ImmediateLoad).Value;
            efont2 = ModContent.Request<DynamicSpriteFont>("CalamityEntropy/Assets/Fonts/VCRFont", AssetRequestMode.ImmediateLoad).Value;
            efont3 = ModContent.Request<DynamicSpriteFont>("CalamityEntropy/Assets/Fonts/MaruMonica", AssetRequestMode.ImmediateLoad).Value;
            if (!Main.dedServ) {
                EBookUI.shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Outline", AssetRequestMode.ImmediateLoad).Value;
            }
            armorForgingStationUI = new ArmorForgingStationUI();
            armorForgingStationUI.Activate();
            userInterface = new UserInterface();
            userInterface.SetState(armorForgingStationUI);

            CruiserHead.loadHead();

            EntropySkies.setUpSkies();

            EModSys.timer = 0;
            EModILEdit.load();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) => CENetWork.Handle(reader, whoAmI);

        public static Projectile GetAProjectileInstance(int type) {
            if (Proj_ID_To_Instance.TryGetValue(type, out Projectile cached)) {
                return cached;
            }
            Projectile p = new Projectile();
            p.SetDefaults(type);
            Proj_ID_To_Instance[type] = p;
            return p;
        }

        public override void Unload() {
            //先撤掉全部原版钩子,再拆它们读取的状态,免得卸载途中还有钩子体能被触发
            CEDetourRegistry.UndoAll();
            CommonEffects.Unload();
            CELists.Unload();
            Typer.activeTypers = null;
            ScreenShaker.Unload();
            VanityDisplaySys.VanityItems = null;
            CEUtils.SoundStyles = null;
            StartBagGItem.items = null;
            ShadowCrystalDeltarune.Reset();
            EBookUI.shader = null;
            if (ILoaders != null) {
                foreach (ICELoader setup in ILoaders) {
                    setup.UnLoadData();
                    setup.DompUnLoadText();
                }
            }
            ILoaders = null;
            CERecipeGroups.unload();
            CEUtils.TexCache = null;
            BookMarkLoader.CustomBMEffectsByName = null;
            BookMarkLoader.CustomBMByID = null;

            Proj_ID_To_Instance = null;
            EModHooks.UnLoadData();
            LoopSoundManager.unload();
            ealaserSound = null;
            ealaserSound2 = null;
            CWRQuestSupport.Unload();
            ArmorPrefix.instances = null;
            Poop.instances = null;
            WallpaperHelper.wallpaper = null;
            efont1 = null;
            efont2 = null;
            efont3 = null;
            Instance = null;
        }

        //两套 Call 面:ModCall 的字典分发先跑,没登记(或返回 null)才落到早期的字符串接口
        public override object Call(params object[] args) => ModCall.Call(args) ?? CELegacyCallApi.Handle(args);

        public static List<MusicBox> mbRegs = null;
        public void RegistryMusicBoxes() {
            foreach (var mb in mbRegs) {
                MusicBox.AddMusicBox(mb.MusicFile, mb.Type, mb.MusicBoxTile);
            }
            mbRegs = null;
        }
        public override void PostSetupContent() {
            CommonEffects.Load();
            CELists.Load();
            Apsychos.WhiteTransShader();
            if (!Main.dedServ) {
                EntropySkies.setUpShaderFilters();
            }
            ScreenShaker.Init();
            Typer.activeTypers = new();
            StartBagGItem.items = new List<int>();
            VanityDisplaySys.SetupVanities();

            void bookUpdateDirt(Projectile projectile, bool ownerClient) {
                if (ownerClient && CECooldowns.CheckCD("Dirt", 60)) {
                    if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                        eb.ShootSingleProjectile(ModContent.ProjectileType<BMDirtProj>(), projectile.Center, projectile.rotation.ToRotationVector2(), 0.3f, 1, 0.8f, (proj) => { proj.ai[1] = -1; proj.ai[0] = ItemID.DirtBlock; });
                }
            }
            BookMarkLoader.RegisterBookmarkEffect("DirtEffect", bookUpdate: bookUpdateDirt);
            BookMarkLoader.RegisterBookmark(ItemID.DirtBlock, null, effectName: "DirtEffect");

            void bookUpdateStone(Projectile projectile, bool ownerClient) {
                if (ownerClient && CECooldowns.CheckCD("Stone", 60)) {
                    if (projectile.ModProjectile is EntropyBookHeldProjectile eb)
                        eb.ShootSingleProjectile(ModContent.ProjectileType<BMDirtProj>(), projectile.Center, projectile.rotation.ToRotationVector2(), 0.25f, 1, 0.8f, (proj) => { proj.ai[1] = 1; proj.ai[0] = ItemID.StoneBlock; });
                }
            }
            BookMarkLoader.RegisterBookmarkEffect("StoneEffect", bookUpdate: bookUpdateStone);
            BookMarkLoader.RegisterBookmark(ItemID.StoneBlock, null, effectName: "StoneEffect");
            if (!Main.dedServ) {
                Main.instance.LoadItem(ItemID.StoneBlock);
                Main.instance.LoadItem(ItemID.DirtBlock);
            }
            for (int i = 0; i < ItemLoader.ItemCount; i++) {
                Item item = ContentSamples.ItemsByType[i];
                if (item.ModItem != null && item.ModItem is IGetFromStarterBag) {
                    StartBagGItem.items.Add(i);
                }
            }
            CEObtainTooltipIntegration.Register();
            foreach (ICELoader setup in ILoaders) {
                setup.SetupData();
                if (!Main.dedServ) {
                    setup.LoadAsset();
                }
            }
            Type baseTypeLR = typeof(LoreEffect);
            Type[] lrTypes = AssemblyManager.GetLoadableTypes(this.Code);
            foreach (Type type in lrTypes) {
                if (!type.IsSubclassOf(baseTypeLR) || type.IsAbstract)
                    continue;
                LoreEffect loreEffect = (LoreEffect)Activator.CreateInstance(type);
                //CEID 在灾厄缺席时返回 0。不跳过会把全部效果塞进 ItemID.None,读 Decription 加载期 NRE
                if (loreEffect.ItemType <= 0) {
                    continue;
                }
                LoreReworkSystem.loreEffects[loreEffect.ItemType] = loreEffect;
                _ = loreEffect.Decription.Value;
            }
            RegistryMusicBoxes();
            for (int i = 0; i < NPCLoader.NPCCount; i++) {
                NPCID.Sets.SpecificDebuffImmunity[i][ModContent.BuffType<Content.Buffs.HeatDeath>()] = false;
                NPCID.Sets.SpecificDebuffImmunity[i][ModContent.BuffType<LifeOppress>()] = false;
            }
            CECalContentRegistry.RegisterDebuffImmunityOverrides();
            CECalContentRegistry.RegisterBossBarExclusions();
            EntropyModeGNPC.FillCalTypes();

            string MyGameFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games");
            string Isaac1 = Path.Combine(MyGameFolder, "Binding of Isaac Repentance").Replace("/", "\\");
            string Isaac2 = Path.Combine(MyGameFolder, "Binding of Isaac Repentance+").Replace("/", "\\");
            BrokenAnkh.isaac = Directory.Exists(Isaac1) || Directory.Exists(Isaac2);

            //Load special sounds
            if (!Main.dedServ) {
                ealaserSound = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/VoidLaserLoop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                ealaserSound2 = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/portal_loop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                ofCharge = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/ElectricLoop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                FableEye.sound = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/prophetlaserloop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                UrnOfSoulsHoldout.loopSnd = ModContent.Request<SoundEffect>("CalamityEntropy/Assets/Sounds/flamethrower loop", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            CEMusicDisplayIntegration.Register();
            CEBossChecklistIntegration.Register();
            //血条色表三段有先后:原版与自有手调表 → 灾厄表 → 外部模组表。三者键互不相交,顺序沿用 3.33 原样
            EntropyBossbar.SetupVanillaColors();
            CECalContentRegistry.RegisterBossbarColors();
            CEForeignBossbarColors.Register();

            //向 CWR 任务书注入本模组节点，没装 CWR 时整段静默跳过
            CWRQuestSupport.Register();

            //Custom titles
            if (!Main.dedServ && Main.rand.NextBool(9)) {
                SetARandomEntropyTitle();
            }
        }
        public static void SetARandomEntropyTitle() {
            if (Main.dedServ)
                return;
            int titleType = Main.rand.Next(7);
            string text = Instance.GetLocalization("TitleTexts.Terraria").Value + Instance.GetLocalization("TitleTexts.Title" + titleType.ToString()).Value;
            if (titleType == 4) {
                List<string> names = new List<string>();
                //Pick a random weapon
                for (int i = 0; i < ItemLoader.ItemCount; i++) {
                    Item item = ContentSamples.ItemsByType[i];
                    if (item.damage > 0 && item.ammo == AmmoID.None) {
                        names.Add(item.Name);
                    }
                }
                text = text.Replace("[NAME]", names[Main.rand.Next(names.Count)]);
            }
            if (titleType == 5) {
                List<string> names = new List<string>();
                //Pick a random entropy item
                for (int i = ItemID.Count; i < ItemLoader.ItemCount; i++) {
                    Item item = ContentSamples.ItemsByType[i];
                    if (item.ModItem != null && item.ModItem.Mod is CalamityEntropy) {
                        names.Add(item.Name);
                    }
                }
                text = text.Replace("[NAME]", names[Main.rand.Next(names.Count)]);
            }
            Main.instance.Window.Title = text;
        }
        public static void SpawnHeavenSpark(Vector2 pos, float rot, float length, float scale, Color color = default, int LifeTime = 24) {
            Vector2 norl = rot.ToRotationVector2();
            float sengs = length;
            if (color == default) {
                color = Color.BlueViolet;
            }
            for (int j = 0; j < 53; j++) {
                PRTLoader.NewParticle<PRT_HeavenfallStar>(pos, norl * (0.1f + j * 0.34f) * sengs, color, Main.rand.NextFloat(0.6f, 1.3f) * scale)
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, norl.ToRotation(), LifeTime);
            }
            for (int j = 0; j < 53; j++) {
                PRTLoader.NewParticle<PRT_HeavenfallStar>(pos, norl * -(0.1f + j * 0.34f) * sengs, color, Main.rand.NextFloat(0.6f, 1.3f) * scale)
                    .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, (-norl).ToRotation(), LifeTime);
            }
        }
    }
}
