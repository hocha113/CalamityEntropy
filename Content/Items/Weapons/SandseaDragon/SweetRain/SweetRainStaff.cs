using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.SandseaDragon.SweetRain
{
    /// <summary>
    /// 甘霖法杖,按住在鼠标上方维持一片 <see cref="SweetRainCloud"/>
    /// <br/>物品只管起手那一次,持续扣魔、下雨、持杖朝向都在云里
    /// </summary>
    public class SweetRainStaff : ModItem
    {
        public const int BaseDamage = 52;       //面板伤害,每滴雨一次
        public const int ManaPerTick = 5;       //每个扣魔周期的魔力,起手那次由原版扣
        public const int ManaTickFrames = 12;   //扣魔周期,帧;也是起手的 useTime
        public const float AimSpeed = 1f;       //shootSpeed,只让原版在起手帧算出持杖朝向
        public const int SpriteWidth = 52;      //贴图宽,像素
        public const int SpriteHeight = 70;     //贴图高,像素
        public const int SellGold = 8;          //售价,金币

        public override void SetStaticDefaults() {
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaPerTick;
            Item.useTime = ManaTickFrames;
            Item.useAnimation = ManaTickFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.knockBack = 0f;
            Item.UseSound = SoundID.Item66;
            Item.shoot = ModContent.ProjectileType<SweetRainCloud>();
            Item.shootSpeed = AimSpeed;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //扭曲孪生会带着 twinSpawnIndex 再调一次本方法,本体与每个孪生各留一片在下雨的云
            int origin = player.Entropy().twinSpawnIndex;
            foreach (Projectile proj in Main.ActiveProjectiles) {
                if (proj.owner == player.whoAmI && proj.type == type && proj.ai[0] == SweetRainCloud.StateRaining && proj.Entropy().IndexOfTwistedTwinShootedThisProj == origin)
                    return false;
            }
            Projectile.NewProjectile(source, SweetRainCloud.CenterForMouse(Main.MouseWorld), Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
