using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.AncientPaladinWhip
{
    //不设配方,掉落挂在VDAftermathGlobalNPC的圣骑士分支(25%)
    public class AncientPaladinWhip : BaseWhipItem
    {
        //鞭伤与挥鞭用时(帧):每秒两鞭约300,8%标记暴击带给10栏召唤约104,圣骑士锤每秒一把约750
        private const int BaseDamage = 150;
        private const int UseFrames = 30;
        private const float Knockback = 3f;
        //出鞭速度(像素/帧),和弹幕的长度倍率一起决定鞭长
        private const float ShootSpeed = 4f;
        //标记暴击率,对应提示「增加8%召唤物暴击率」
        private const float TagCrit = 0.08f;
        private const int SellGold = 25;

        public override int TagDamage => 0;
        public override float TagCritChance => TagCrit;

        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<AncientPaladinWhipProj>(), BaseDamage, Knockback, ShootSpeed, UseFrames);
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
            Item.autoReuse = true;
        }
    }
}
