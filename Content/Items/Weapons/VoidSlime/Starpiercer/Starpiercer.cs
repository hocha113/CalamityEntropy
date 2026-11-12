using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Items.Weapons.Aftermath.GoldbowSilvershot;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Starpiercer
{
    /// <summary>
    /// 穿天星点。弹弓,按住蓄力、松开发射,蓄力不足松开不发;右键在脉冲星与引力星之间切换
    /// 脉冲星蓄满发一发高能弹丸,命中迸出弧形冲击波与电弧;引力星蓄满发一发无伤害的引力弹丸,撞掉一发小型敌对射弹
    /// 不吃弹药;蓄力与发射都在持握弹幕里
    /// </summary>
    public class Starpiercer : ModItem
    {
        public const int ModePulsar = 0;                //模式取值: 脉冲星
        public const int ModeGraviton = 1;              //引力星
        public const int UseFrames = 45;                //使用时间,帧,即脉冲星蓄满所需帧数,再乘攻速
        private const int SpriteWidth = 40;             //物品图尺寸,像素
        private const int SpriteHeight = 54;
        private const int BaseDamage = 1200;
        private const float BaseKnockback = 4f;
        private const int SellGold = 32;                //售价,金币
        private const int SupernovaGelCount = 10;       //配方里超新星凝胶的数量
        private const float ToggleVolume = 0.5f;        //切换提示音音量
        private const int ToggleInstances = 2;          //切换提示音同时最多几个
        private static readonly float[] TogglePitch = { 1.25f, 0.8f };          //各模式的切换提示音音高,CEUtils.PlaySound 以 1 为原调
        private static readonly Color[] ModeTextColor = { new Color(170, 204, 255), new Color(255, 214, 92) };   //头顶飘字: 脉冲星蓝白,引力星金

        public static LocalizedText[] ModeTexts { get; private set; }

        public override void SetStaticDefaults() {
            ModeTexts = new[] { this.GetLocalization("ModePulsar"), this.GetLocalization("ModeGraviton") };
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
            Item.shoot = ModContent.ProjectileType<StarpiercerHeld>();
            Item.shootSpeed = StarpiercerPulsar.Speed;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2) {
                //右键只切模式不发射;模式只在拥有者端有意义,生成持握弹幕时写进它的 ai[2]
                if (player.whoAmI == Main.myPlayer) {
                    StarpiercerPlayer modPlayer = player.GetModPlayer<StarpiercerPlayer>();
                    modPlayer.Mode = modPlayer.Mode == ModePulsar ? ModeGraviton : ModePulsar;
                    CombatText.NewText(player.getRect(), ModeTextColor[modPlayer.Mode], ModeTexts[modPlayer.Mode].Value);
                    CEUtils.PlaySound("bell", TogglePitch[modPlayer.Mode], player.Center, ToggleInstances, ToggleVolume);
                }
                return false;
            }
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float mode = player.GetModPlayer<StarpiercerPlayer>().Mode;
            Projectile.NewProjectile(source, player.Center, velocity.SafeNormalize(Vector2.UnitX * player.direction), Item.shoot, damage, knockback, player.whoAmI, 0f, 0f, mode);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<GoldbowSilvershot>()
                .AddIngredient<SupernovaGel>(SupernovaGelCount)
                .AddTile(ModContent.TileType<GalaxySolidifierTile>())
                .Register();
        }
    }

    /// <summary>穿天星点的模式。只在拥有者端有意义,随角色存档</summary>
    public class StarpiercerPlayer : ModPlayer
    {
        public int Mode;    //Starpiercer.ModePulsar / ModeGraviton

        public override void SaveData(TagCompound tag) {
            if (Mode != Starpiercer.ModePulsar) {
                tag["Mode"] = Mode;
            }
        }

        public override void LoadData(TagCompound tag) {
            Mode = Utils.Clamp(tag.GetInt("Mode"), Starpiercer.ModePulsar, Starpiercer.ModeGraviton);
        }
    }
}
