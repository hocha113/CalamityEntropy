using CalamityEntropy.Content.Items.Donator;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class Zafkiel : ModItem, IDonatorItem
    {
        public string DonatorName => "Dannon";

        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<ZafkielProj>();
            Item.buffType = ModContent.BuffType<ZafkielBuff>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.GoldWatch)
                .AddIngredient(ItemID.SoulofNight, 8)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.PlatinumWatch)
                .AddIngredient(ItemID.SoulofNight, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }

    }
    public class ZafkielBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<ZafkielProj>());
        }
    }
    public class ZafkielProj : ModProjectile
    {
        //帧动画贴图统一在加载期就位(路径 + 序号),不再在 PreDraw 里逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/zfk/f", 1, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FlyFrames;
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/zfk/w", 1, 8, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] WalkFrames;
        public float counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();

        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 24;
            Projectile.height = 44;
        }
        public override bool PreDraw(ref Color lightColor) {
            bool hat = Projectile.owner.ToPlayer().Entropy().PetsHat;
            if (Main.gameMenu) {
                Texture2D txd = FlyFrames[0];
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            Player player = Main.player[Projectile.owner];
            if (counter > 36) {
                counter -= 36;
            }
            Texture2D[] frames = Projectile.ai[1] == 1 ? FlyFrames : WalkFrames;
            Texture2D tx = frames[(((int)counter / 6) % frames.Length)];
            if (Projectile.velocity.X > -2 && Projectile.velocity.X < 2f) {
                if (player.Center.X > Projectile.Center.X) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }
            }
            if (Projectile.direction == -1) {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

            }
            else {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            }


            return false;

        }
        void MoveToTarget(Vector2 targetPos) {
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1400) {
                Projectile.Center = Main.player[Projectile.owner].Center - new Vector2(0, 50);
            }
            if (Projectile.ai[1] == 1) {
                counter++;
                Projectile.tileCollide = false;
                Projectile.rotation = MathHelper.ToRadians((Projectile.velocity.X * 1.4f));
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 100) {
                    Vector2 px = targetPos - Projectile.Center;
                    px.Normalize();
                    Projectile.velocity += px * 0.7f;

                    Projectile.velocity *= 0.98f;

                }
                if (Projectile.Center.Y < targetPos.Y - 16 && CEUtils.getDistance(Projectile.Center, targetPos) < 100 && !(CEUtils.isAir(Projectile.owner.ToPlayer().Center + new Vector2(0, Projectile.owner.ToPlayer().height / 2 + 2), true))) {
                    Projectile.ai[1] = 0;
                }
                if (Projectile.velocity.X > 0) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }
            }
            else {
                if (Projectile.velocity.Y == 0) {
                    counter += Math.Abs(Projectile.velocity.X / 4);
                }
                Projectile.tileCollide = true;
                Projectile.rotation = 0;
                Projectile.velocity.Y += 0.5f;
                Projectile.velocity *= 0.98f;
                if (Projectile.velocity.Y > 14)
                    Projectile.velocity.Y = 14;
                if (CEUtils.getDistance(targetPos, Projectile.Center) > 250 || (Math.Abs(targetPos.Y - Projectile.Center.Y) > 60 && Projectile.owner.ToPlayer().velocity.Y == 0)) {
                    Projectile.ai[1] = 1;
                    if (Projectile.Center.Y < targetPos.Y - 16 && CEUtils.getDistance(Projectile.Center, targetPos) < 100 && !(CEUtils.isAir(Projectile.owner.ToPlayer().Center + new Vector2(0, Projectile.owner.ToPlayer().height / 2 + 2), true))) {
                        Projectile.ai[1] = 0;
                    }
                }
                else if (CEUtils.getDistance(targetPos * new Vector2(1, 0), Projectile.Center * new Vector2(1, 0)) > 150) {
                    if (targetPos.X > Projectile.Center.X) {
                        Projectile.velocity.X += 1f;
                    }
                    else {
                        Projectile.velocity.X -= 1f;
                    }
                    Projectile.velocity.X *= 0.95f;
                }
                else {
                    Projectile.velocity.X *= 0.9f;
                }
                if (targetPos.X > Projectile.Center.X) {
                    Projectile.direction = 1;
                }
                else {
                    Projectile.direction = -1;
                }

                if (Math.Abs(Projectile.velocity.X) > 0.3f && !CEUtils.isAir(Projectile.Center + (Projectile.velocity * new Vector2(1, 0)).SafeNormalize(Vector2.Zero) * 14 + new Vector2(0, 14))) {
                    Projectile.velocity.Y -= 3f;
                }
            }

        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            return true;
        }

        public override void AI() {

            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, 0));
            if (!player.dead && player.HasBuff(ModContent.BuffType<ZafkielBuff>())) {
                Projectile.timeLeft = 2;
            }

        }


    }
}