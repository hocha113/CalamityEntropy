using CalamityEntropy.Content.Buffs.VoidSlime;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using CalamityEntropy.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.VoidSlime
{
    /// <summary>幼星凝胶,召出虚空星辰史莱姆宠物</summary>
    public class BabyStarGel : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.width = 24;
            Item.height = 24;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<VoidStarSlimePet>();
            Item.buffType = ModContent.BuffType<VoidStarSlimeBuff>();
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.value = Item.sellPrice(gold: 5);
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
    }

    /// <summary>虚空史莱姆鞍,骑上一只会滞空的虚空史莱姆</summary>
    public class VoidSlimeSaddle : ModItem
    {
        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 28;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item79;
            Item.noMelee = true;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.mountType = ModContent.MountType<VoidSlimeMount>();
        }
    }

    /// <summary>史莱姆坐骑:连跳、免摔落伤害,带两秒滞空;贴图两帧,0 正常 1 压扁</summary>
    public class VoidSlimeMount : ModMount
    {
        public override void SetStaticDefaults() {
            MountData.buff = ModContent.BuffType<VoidSlimeMountBuff>();
            MountData.spawnDust = DustID.PurpleTorch;
            MountData.heightBoost = 20;
            MountData.fallDamage = 0f;
            MountData.runSpeed = 9f;
            MountData.dashSpeed = 11f;
            MountData.acceleration = 0.2f;
            MountData.jumpHeight = 22;
            MountData.jumpSpeed = 9.5f;
            MountData.constantJump = true;
            MountData.flightTimeMax = 120;
            MountData.fatigueMax = 0;

            MountData.totalFrames = 2;
            MountData.playerYOffsets = new int[] { 16, 14 };
            MountData.xOffset = 0;
            MountData.yOffset = 10;
            MountData.playerHeadOffset = 22;
            MountData.bodyFrame = 3;

            MountData.standingFrameCount = 1;
            MountData.standingFrameDelay = 12;
            MountData.standingFrameStart = 0;
            MountData.runningFrameCount = 2;
            MountData.runningFrameDelay = 12;
            MountData.runningFrameStart = 0;
            MountData.flyingFrameCount = 1;
            MountData.flyingFrameDelay = 12;
            MountData.flyingFrameStart = 0;
            MountData.inAirFrameCount = 1;
            MountData.inAirFrameDelay = 12;
            MountData.inAirFrameStart = 0;
            MountData.idleFrameCount = 2;
            MountData.idleFrameDelay = 30;
            MountData.idleFrameStart = 0;
            MountData.idleFrameLoop = true;
            MountData.swimFrameCount = MountData.inAirFrameCount;
            MountData.swimFrameDelay = MountData.inAirFrameDelay;
            MountData.swimFrameStart = MountData.inAirFrameStart;

            if (!Main.dedServ) {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }

        public override void UpdateEffects(Player player) {
            Lighting.AddLight(player.Center, 0.3f, 0.16f, 0.48f);
        }
    }
}
