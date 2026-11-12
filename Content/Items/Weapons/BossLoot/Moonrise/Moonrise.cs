using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Moonrise
{
    /// <summary>
    /// 月上梢头。右键按新月、弦月、满月轮换模式: 新月只发追踪火球,弦月喷白焰并夹着火球,满月喷射程最远的蓝焰
    /// 以凝胶为弹药,每次该扣凝胶时有 66% 不扣;出弹节奏与扣弹在持握弹幕里,冷却记在 MoonrisePlayer
    /// </summary>
    public class Moonrise : ModItem
    {
        public const int ModeNew = 0;                   //模式取值: 新月
        public const int ModeHalf = 1;                  //弦月
        public const int ModeFull = 2;                  //满月
        private const int ModeCount = 3;
        private const int SpriteWidth = 88;             //物品图尺寸,像素
        private const int SpriteHeight = 36;
        private const int BaseDamage = 110;
        private const float BaseKnockback = 1f;
        private const int UseFrames = 6;                //使用时间,帧,等于喷火间隔;只影响面板攻速与持握弹幕的生成,出弹节奏在持握弹幕里
        private const int SellGold = 30;                //售价,金币
        private const float AmmoSaveChance = 0.66f;     //每次该扣凝胶时不扣的概率
        private const float ToggleVolume = 0.5f;        //切换提示音音量
        private const int ToggleInstances = 2;          //切换提示音同时最多几个
        private static readonly float[] TogglePitch = { 1.25f, 1f, 0.75f };     //各模式的切换提示音音高,月越满越低,CEUtils.PlaySound 以 1 为原调
        private static readonly Color ModeTextColor = new Color(115, 150, 253);  //头顶飘字的蓝,取自星月贤者

        public static LocalizedText[] ModeTexts { get; private set; }

        public override void SetStaticDefaults() {
            ModeTexts = new[] { this.GetLocalization("ModeNew"), this.GetLocalization("ModeHalf"), this.GetLocalization("ModeFull") };
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
            Item.shoot = ModContent.ProjectileType<MoonriseHeld>();
            Item.shootSpeed = MoonriseFireball.LaunchSpeed;
            Item.useAmmo = AmmoID.Gel;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2) {
                //右键只换模式不开火;模式只在拥有者端有意义,生成持握弹幕时写进它的 ai[0]
                if (player.whoAmI == Main.myPlayer) {
                    MoonrisePlayer modPlayer = player.GetModPlayer<MoonrisePlayer>();
                    modPlayer.Mode = (modPlayer.Mode + 1) % ModeCount;
                    CombatText.NewText(player.getRect(), ModeTextColor, ModeTexts[modPlayer.Mode].Value);
                    CEUtils.PlaySound("bell", TogglePitch[modPlayer.Mode], player.Center, ToggleInstances, ToggleVolume);
                }
                return false;
            }
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) {
            return Main.rand.NextFloat() >= AmmoSaveChance;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            MoonrisePlayer modPlayer = player.GetModPlayer<MoonrisePlayer>();
            //使用物品时已按上面的概率扣过一次凝胶,扣弹计时从这里起
            modPlayer.AmmoCooldown = MoonriseHeld.AmmoCheckFrames;
            Projectile.NewProjectile(source, player.Center, velocity.SafeNormalize(Vector2.UnitX * player.direction), Item.shoot, damage, knockback, player.whoAmI, modPlayer.Mode);
            return false;
        }
    }

    /// <summary>月上梢头的模式与出弹冷却。只在拥有者端有意义,模式随角色存档;冷却跨持握弹幕保留,连点快不过按住</summary>
    public class MoonrisePlayer : ModPlayer
    {
        public int Mode;            //Moonrise.ModeNew / ModeHalf / ModeFull
        public int FlameCooldown;   //还要多少帧才能喷下一团火焰,帧
        public int BallCooldown;    //还要多少帧才能发下一个火球,帧
        public int AmmoCooldown;    //还要多少帧才再尝试扣一次凝胶,帧

        public override void PostUpdate() {
            if (FlameCooldown > 0) {
                FlameCooldown--;
            }
            if (BallCooldown > 0) {
                BallCooldown--;
            }
            if (AmmoCooldown > 0) {
                AmmoCooldown--;
            }
        }

        public override void SaveData(TagCompound tag) {
            if (Mode != Moonrise.ModeNew) {
                tag["Mode"] = Mode;
            }
        }

        public override void LoadData(TagCompound tag) {
            Mode = Utils.Clamp(tag.GetInt("Mode"), Moonrise.ModeNew, Moonrise.ModeFull);
        }
    }
}
