using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Items.Vanity;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class MomosLightPet : ModItem, IDonatorItem
    {
        public string DonatorName => "Momodzmz";
        public override void SetStaticDefaults() {
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<MosHat>();
        }
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.UseSound = SoundID.Item58;
            Item.shoot = ModContent.ProjectileType<Molightpet>();
            Item.buffType = ModContent.BuffType<MomosLightPetBuff>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
    }
    public class MomosLightPetBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.lightPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<Molightpet>());
        }
    }
    public class Molightpet : ModProjectile
    {
        //帧动画与菜单贴图统一在加载期就位,不再在 PreDraw 里逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Deus/AstrumDeus")]
        internal static Asset<Texture2D> MenuTex;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Molightpet")]
        internal static Asset<Texture2D> Frame1;
        //数组下标 0 对应文件 mo2
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/mo/mo", 2, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FramesRest;
        public int counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.LightPet[Projectile.type] = true;
            base.SetStaticDefaults();

        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 24;
            Projectile.height = 58;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Main.gameMenu) {
                Texture2D txd = MenuTex.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            int frameIdx = (counter / 4) % 5;
            Texture2D tx = frameIdx == 0 ? Frame1.Value : FramesRest[frameIdx - 1];
            if (Main.player[Projectile.owner].Entropy().MouseWorld.X > Projectile.Center.X) {
                Projectile.direction = 1;
            }
            else {
                Projectile.direction = -1;
            }
            if (Projectile.direction == -1) {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

            }
            else {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            }


            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(CEExtraAssets.GlowCone, Projectile.Center - Main.screenPosition, null, Color.White * 0.2f, (Projectile.GetOwner().Entropy().MouseWorld - Projectile.Center).ToRotation(), new Vector2(0, 250), new Vector2(1.4f, 0.8f), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;

        }
        void MoveToTarget(Vector2 targetPos) {
            Projectile.GetOwner().Entropy().MouseWorldListener = true;
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1600) {
                Projectile.Center = Main.player[Projectile.owner].Center - new Vector2(0, 200);
            }
            Projectile.velocity = (targetPos - Projectile.Center) * 0.06f;
        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            CEUtils.AddLight(Projectile.Center, Color.White, 4);
            Vector2 lp = Projectile.Center;
            bool addlight = false;
            float r = (Projectile.GetOwner().Entropy().MouseWorld - Projectile.Center).ToRotation();
            for (int i = 0; i < 1000; i += 8) {
                Point tpos = ((lp + r.ToRotationVector2() * i) / 16f).ToPoint();
                if (CEUtils.inWorld(tpos.X, tpos.Y)) {
                    if (Main.tile[tpos].IsTileSolid()) {
                        addlight = true;
                        lp = tpos.ToVector2() * 16;
                        break;
                    }
                }
            }
            if (addlight) {
                CEUtils.AddLight(lp, Color.White, 3);
            }
            SpawnLighting();
            return true;
        }
        public void SpawnLighting() {
            for (float r = -0.12f; r <= 0.12f; r += 0.01f) {
                float rot = (Projectile.GetOwner().Entropy().MouseWorld - Projectile.Center).ToRotation() + r;

                for (float i = 0; i < 900; i += 8) {
                    CEUtils.AddLight(Projectile.Center + Projectile.velocity + rot.ToRotationVector2() * i, Color.White, ((910 - i) / 900f) * (5f * (0.2f - Math.Abs(r))));
                }
            }
        }
        public int shotCd = 0;

        public override void AI() {
            counter++;
            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, -100));
            if (!player.dead && player.HasBuff(ModContent.BuffType<MomosLightPetBuff>())) {
                Projectile.timeLeft = 2;
            }
        }


    }
}