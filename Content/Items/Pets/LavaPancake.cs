using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class LavaPancake : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<ProfPetG1>();
            Item.buffType = ModContent.BuffType<GuardiansBuff>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

    }
    public class GuardiansBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<ProfPetG1>());
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<ProfPetG2>());
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<ProfPetG3>());
        }
    }
    public abstract class ProfanedGuardianPet : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        internal abstract Texture2D[] Frames { get; }
        public int counter = 0;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 42;
            Projectile.height = 42;
        }
        public override bool PreDraw(ref Color lightColor) {
            lightColor = Color.White;
            Texture2D[] frames = Frames;
            if (Main.gameMenu) {
                Texture2D txd = frames[0];
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            Texture2D tx = frames[(counter / 4) % frames.Length];
            Projectile.direction = Math.Sign(Projectile.GetOwner().Center.X - Projectile.Center.X);
            if (Projectile.direction == -1) {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

            }
            else {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);

            }

            return false;

        }
        public virtual float MS => 0.1f;
        public virtual Vector2 posOffset => new Vector2(-40, -40);
        void MoveToTarget(Vector2 targetPos) {
            Projectile.velocity = (targetPos + posOffset * new Vector2(Projectile.GetOwner().direction, 1) - Projectile.Center) * MS;
        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            return true;
        }

        public override void AI() {
            counter++;
            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center);
            if (!player.dead && (player.HasBuff(this.Buff) || player.HasBuff(ModContent.BuffType<ProfNGuardBuff>()))) {
                Projectile.timeLeft = 2;
            }
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3());
            Projectile.rotation = Projectile.velocity.X * 0.01f;
        }

        public virtual int Buff => ModContent.BuffType<GuardiansBuff>();
    }

    public class ProfPetG1 : ProfanedGuardianPet
    {
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/A", 1, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FramesA;
        internal override Texture2D[] Frames => FramesA;
        public override float MS => 0.1f;
        public override Vector2 posOffset => new Vector2(-85, -20);
    }
    public class ProfPetG2 : ProfanedGuardianPet
    {
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/B", 1, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FramesB;
        internal override Texture2D[] Frames => FramesB;
        public override float MS => 0.08f;
        public override Vector2 posOffset => new Vector2(-115, -20);
    }
    public class ProfPetG3 : ProfanedGuardianPet
    {
        [VaultLoaden("CalamityEntropy/Content/Items/Pets/Prof/C", 1, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FramesC;
        internal override Texture2D[] Frames => FramesC;
        public override float MS => 0.06f;
        public override Vector2 posOffset => new Vector2(-145, -20);
    }
}