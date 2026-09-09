using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Donator
{
    public class TheReplicaofThePen : ModItem, IDonatorItem
    {
        public string DonatorName => Mod.GetLocalization("RPDonorName").Value;
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.sellPrice(platinum: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.UseSound = SoundID.Item79;
            Item.noMelee = true;
            Item.mountType = ModContent.MountType<ReplicaPenMount>();
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_DarkPlasma, CEID.Item_RuinousSoul))
            {
                CreateRecipe()
                .AddIngredient<VoidBar>(6)
                .AddIngredient(CEID.Item_DarkPlasma, 4)
                .AddIngredient(CEID.Item_RuinousSoul, 2)
                .AddIngredient(ItemID.Goggles)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            // 灾厄原料按 material-map.md 替换：DarkPlasma×4+RuinousSoul×2→虚无碎片（合并为×6）
            CreateRecipe()
                .AddIngredient<VoidBar>(6)
                .AddIngredient<NihilityFragments>(6)
                .AddIngredient(ItemID.Goggles)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class PenMountBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<ReplicaPenMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }
    public class ReplicaPenMount : ModMount
    {
        protected class PenMountData
        {

        }

        public override void SetStaticDefaults()
        {
            MountData.spawnDustNoGravity = true;
            MountData.flightTimeMax = int.MaxValue - 1;
            MountData.fatigueMax = int.MaxValue - 1;
            MountData.fallDamage = 0f;
            MountData.usesHover = true;
            MountData.runSpeed = 12f;
            MountData.dashSpeed = 12f;
            MountData.acceleration = 0.2f;
            MountData.jumpHeight = 10;
            MountData.jumpSpeed = 4f;
            MountData.blockExtraJumps = true;

            // Misc
            MountData.buff = ModContent.BuffType<PenMountBuff>();

            // Effects
            MountData.spawnDust = DustID.Shadowflame; // The ID of the dust spawned when mounted or dismounted.

            // Frame data and player offsets
            MountData.totalFrames = 1; // Amount of animation frames for the mount
            MountData.playerYOffsets = Enumerable.Repeat(16, MountData.totalFrames).ToArray(); // Fills an array with values for less repeating code
            MountData.xOffset = 0;
            MountData.yOffset = 10;
            MountData.playerHeadOffset = 22;
            MountData.bodyFrame = 3;
            // Standing
            MountData.standingFrameCount = 1;
            MountData.standingFrameDelay = 12;
            MountData.standingFrameStart = 0;
            // Running
            MountData.runningFrameCount = 1;
            MountData.runningFrameDelay = 12;
            MountData.runningFrameStart = 0;
            // Flying
            MountData.flyingFrameCount = 1;
            MountData.flyingFrameDelay = 0;
            MountData.flyingFrameStart = 0;
            // In-air
            MountData.inAirFrameCount = 1;
            MountData.inAirFrameDelay = 12;
            MountData.inAirFrameStart = 0;
            // Idle
            MountData.idleFrameCount = 1;
            MountData.idleFrameDelay = 12;
            MountData.idleFrameStart = 0;
            MountData.idleFrameLoop = true;
            // Swim
            MountData.swimFrameCount = MountData.inAirFrameCount;
            MountData.swimFrameDelay = MountData.inAirFrameDelay;
            MountData.swimFrameStart = MountData.inAirFrameStart;



            if (!Main.dedServ)
            {
                MountData.textureWidth = MountData.backTexture.Width() + 20;
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }

        public override void UpdateEffects(Player player)
        {
            player.fullRotation = Math.Sign(player.velocity.X) * player.velocity.Length() * 0.02f;
            int type = ModContent.ProjectileType<InkTrail>();
            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[type] < 1)
            {
                Projectile.NewProjectile(player.GetSource_FromAI(), player.Center, Vector2.Zero, type, 200, 0, player.whoAmI);
            }
        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            player.mount._mountSpecificData = new PenMountData();

            if (!Main.dedServ)
            {
                for (int i = 0; i < 16; i++)
                {
                    Dust.NewDustPerfect(player.Center + new Vector2(80, 0).RotatedBy(i * Math.PI * 2 / 16f), MountData.spawnDust);
                }

                skipDust = true;
            }
        }

        //坐骑贴图,加载期就位,不再逐帧请求
        [VaultLoaden("CalamityEntropy/Assets/Extra/PenMount")]
        internal static Texture2D PenMountTex;
        public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer, ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition, ref Rectangle frame, ref Color drawColor, ref Color glowColor, ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin, ref float drawScale, float shadow)
        {
            var tex = PenMountTex;
            playerDrawData.Add(new DrawData(tex, drawPosition, null, drawColor, drawPlayer.bodyRotation, tex.Size() / 2f, 1, drawPlayer.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            return false;
        }
    }
}
