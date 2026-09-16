using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class HellBohea : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<ProfPet>();
            Item.buffType = ModContent.BuffType<ProfBuff>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

    }
    public class ProfBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<ProfPet>());
        }
    }
    public class ProfPet : ProfanedGuardianPet
    {
        //改为逐张单字段加载,首次绘制时缓存成数组
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/1")]
        internal static Texture2D F1;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/2")]
        internal static Texture2D F2;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/3")]
        internal static Texture2D F3;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/4")]
        internal static Texture2D F4;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/5")]
        internal static Texture2D F5;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/6")]
        internal static Texture2D F6;
        private static Texture2D[] framesCache;
        internal override Texture2D[] Frames => framesCache ??= new[] { F1, F2, F3, F4, F5, F6 };
        public override float MS => 0.14f;
        public override Vector2 posOffset => new Vector2(-45, -30);

        public override int Buff => ModContent.BuffType<ProfBuff>();
    }
}