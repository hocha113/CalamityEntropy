using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.GoldbowSilvershot
{
    /// <summary>
    /// 金弓银弹。右键在穿山碎峦(按住蓄力,松手射一发穿透金弹)与梨花暴雨(按住每秒散射一轮银弹)之间切换
    /// 箭矢只作弹药,发射物一律是本弓的弹丸,只吃弹药的伤害加成;拉弓、放箭都在持弓弹幕里
    /// </summary>
    public class GoldbowSilvershot : ModItem
    {
        private const int SpriteWidth = 34;         //物品图尺寸,像素
        private const int SpriteHeight = 86;
        private const int BaseDamage = 120;
        private const float BaseKnockback = 3f;
        private const int UseFrames = 20;           //使用时间,帧,只用于面板攻速与生成持弓弹幕
        private const float PanelShootSpeed = 14f;  //面板射速,像素每帧,实际弹速见两种弹丸
        private const int SellGold = 25;            //售价,金币
        private const int SunlightCount = 10;       //配方里裂日之光的数量
        private const int GoldBarCount = 10;        //配方里金锭或铂金锭的数量
        private const float GoldTogglePitch = -0.1f;    //切到穿山碎峦的提示音音高
        private const float SilverTogglePitch = 0.3f;   //切到梨花暴雨的提示音音高
        private static readonly Color GoldModeColor = new Color(255, 214, 92);     //切到穿山碎峦时头顶飘字的颜色
        private static readonly Color SilverModeColor = new Color(200, 222, 255);  //切到梨花暴雨时头顶飘字的颜色

        public static LocalizedText ModeGoldText { get; private set; }
        public static LocalizedText ModeSilverText { get; private set; }

        public override void SetStaticDefaults() {
            ModeGoldText = this.GetLocalization("ModeGold");
            ModeSilverText = this.GetLocalization("ModeSilver");
        }

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = BaseKnockback;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<GoldbowSilvershotHeld>();
            Item.shootSpeed = PanelShootSpeed;
            Item.useAmmo = AmmoID.Arrow;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            GoldbowSilvershotPlayer modPlayer = player.GetModPlayer<GoldbowSilvershotPlayer>();
            if (player.altFunctionUse == 2) {
                //右键只切模式不射击;模式只在拥有者端有意义,生成持弓弹幕时写进它的 ai[2]
                if (player.whoAmI == Main.myPlayer) {
                    modPlayer.SilverMode = !modPlayer.SilverMode;
                    bool silver = modPlayer.SilverMode;
                    CombatText.NewText(player.getRect(), silver ? SilverModeColor : GoldModeColor, (silver ? ModeSilverText : ModeGoldText).Value);
                    SoundEngine.PlaySound(SoundID.Unlock with { Pitch = silver ? SilverTogglePitch : GoldTogglePitch }, player.Center);
                }
                return false;
            }
            return modPlayer.Cooldown <= 0 && player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float mode = player.GetModPlayer<GoldbowSilvershotPlayer>().SilverMode ? GoldbowSilvershotHeld.ModeSilver : GoldbowSilvershotHeld.ModeGold;
            Projectile.NewProjectile(source, player.Center, velocity.SafeNormalize(Vector2.UnitX * player.direction), Item.shoot, damage, knockback, player.whoAmI, 0f, 0f, mode);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.VenomDartTrap)
                .AddIngredient<BlazingSunlight>(SunlightCount)
                .AddRecipeGroup(CERecipeGroups.AnyGoldBar, GoldBarCount)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    /// <summary>金弓银弹的模式与拉弓冷却。只在拥有者端有意义,模式随角色存档</summary>
    public class GoldbowSilvershotPlayer : ModPlayer
    {
        public bool SilverMode;     //true 为梨花暴雨,false 为穿山碎峦
        public int Cooldown;        //还要多少帧才能再拿出弓,帧;梨花暴雨收弓后靠它守住每秒一轮

        public override void PostUpdate() {
            if (Cooldown > 0) {
                Cooldown--;
            }
        }

        public override void SaveData(TagCompound tag) {
            if (SilverMode) {
                tag["SilverMode"] = true;
            }
        }

        public override void LoadData(TagCompound tag) {
            SilverMode = tag.GetBool("SilverMode");
        }
    }
}
