using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.NurglePot
{
    /// <summary>
    /// 凯文的纳垢锅(捐赠者:Reficul)。困难模式敲碎恶魔/猩红祭坛时极低概率掉落。
    /// 左键双手甩锅撒出一片侵蚀性粘液,沾敌怪每秒结算伤害并中毒,沾物块成为伤害经过者的粘液坑;
    /// 右键持锅对准方向、以火魔法加热,1.5 秒沸腾后每 0.4 秒喷出一坨滚烫粘液,
    /// 滚烫粘液附着 5 秒每秒结算伤害并附加中毒与酸性中毒,到期汽化为施加瘟疫的毒雾。
    /// 手持时锅口不断飘出随风散去的臭雾,不伤生物,只让沾到的家伙发臭。
    /// </summary>
    public class KevinsNurglePot : ModItem, IDonatorItem
    {
        public string DonatorName => "Reficul";

        #region 调参旋钮
        //敲碎祭坛时的掉落概率
        public const float AltarDropChance = 0.0066f;
        //左键一次撒出的粘液坨数与散射半角(弧度)
        public const int SplashCount = 7;
        public const float SplashSpread = 0.42f;
        public const float SplashSpeedMin = 7f;
        public const float SplashSpeedMax = 11.5f;
        //右键加热到沸腾的帧数、沸腾后每坨滚烫粘液的间隔帧与每坨魔力
        public const int HeatFrames = 90;
        public const int BoilShotInterval = 24;
        public const int ManaPerBoilShot = 6;
        public const float BoilShotSpeed = 13f;
        //手持臭雾的生成间隔(平均帧)
        public const int StinkMistInterval = 16;
        #endregion

        public override void SetDefaults() {
            Item.width = 58;
            Item.height = 42;
            Item.damage = 34;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 7;
            Item.useTime = Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.knockBack = 2.5f;
            Item.UseSound = null;
            Item.value = Item.buyPrice(gold: 8);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<NurglePotHeld>();
            Item.shootSpeed = 10f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[Item.shoot] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int mode = player.altFunctionUse == 2 ? 1 : 0;
            Projectile.NewProjectile(source, player.MountedCenter, velocity, type, damage, knockback, player.whoAmI, mode);
            return false;
        }

        public override void HoldItem(Player player) {
            if (Main.myPlayer != player.whoAmI || Main.dedServ || player.dead) {
                return;
            }
            if (Main.rand.NextBool(StinkMistInterval)) {
                Vector2 pos = player.MountedCenter + new Vector2(player.direction * 14f, -10f);
                Vector2 vel = new Vector2(Main.windSpeedCurrent * 2.5f + Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.9f, -0.4f));
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), pos, vel, ModContent.ProjectileType<NurgleStinkMist>(), 0, 0f, player.whoAmI);
            }
        }

        /// <summary>祭坛被真正敲碎时由 <see cref="Core.Hooks.CEWorldGenHooks"/> 调用,只在服务端/单人端执行。</summary>
        public static void OnAltarSmashed(int i, int j) {
            if (Main.rand.NextFloat() < AltarDropChance) {
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 32, 32, ModContent.ItemType<KevinsNurglePot>());
            }
        }
    }
}
