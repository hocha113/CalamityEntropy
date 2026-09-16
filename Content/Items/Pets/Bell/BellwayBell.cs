using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets.Bell
{
    public class BellwayBell : ModItem, IDonatorItem
    {
        public string DonatorName => "鹰身吹雪";

        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<Bellbeast>();
            Item.buffType = ModContent.BuffType<BellbeastBuff>();
            Item.UseSound = null;
            Item.noUseGraphic = true;
            Item.useStyle = -1;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(0, 0, 20, 0);
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Bell)
                .AddIngredient(ItemID.CopperBar, 5)
                .AddTile(TileID.Anvils)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.Bell)
                .AddIngredient(ItemID.TinBar, 5)
                .AddTile(TileID.Anvils)
                .Register();

        }
        public override bool? UseItem(Player player) {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/bell"));
            }
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
    }
    public class BellbeastBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<Bellbeast>());
        }
    }
    public class Bellbeast : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.height = 24;
        }
        public int TexType = 0;
        public bool Flying = false;
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            fallThrough = Projectile.GetOwner().Center.Y > 64 + Projectile.Center.Y;
            return true;
        }
        public int fr = 0;
        public float frc = 0;
        public int dir = 1;
        public override void AI() {
            if (Projectile.Entropy().FirstFrames)
                Projectile.velocity.Y = -18;
            Player player = Projectile.GetOwner();
            if (!player.dead && (player.HasBuff(ModContent.BuffType<BellbeastBuff>()))) {
                Projectile.timeLeft = 3;
            }
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 4000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            Projectile.tileCollide = !Flying;
            TexType = Flying ? 1 : 0;
            if (Flying) {
                if (CEUtils.getDistance(Projectile.Center, player.Center) > 140) {
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += (player.Center - Projectile.Center).normalize() * 0.86f;
                }
                if (CEUtils.getDistance(Projectile.Center, player.Center) < 200) {
                    if (!CEUtils.CheckSolidTile(Projectile.getRect())) {
                        if (CEUtils.CheckSolidTileOrPlatform((Projectile.Center + new Vector2(0, 120)).getRectCentered(48, 120))) {
                            Flying = false;
                            Projectile.velocity *= 0.2f;
                            Projectile.velocity.Y = -12;
                            SoundEngine.PlaySound(SoundID.Item128, Projectile.Center);
                            PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Gold, 0.0046f).Configure("CalamityEntropy/Assets/Particles/FlameExplosion", Vector2.One, CEUtils.randomRot(), 0.0046f, 0.05f, 18);
                        }
                    }
                }
                Projectile.rotation += Projectile.velocity.X * 0.036f;
                dir = Projectile.velocity.X > 0 ? 1 : -1;
            }
            else {
                Projectile.rotation = 0;
                if (CEUtils.getDistance(Projectile.Center, player.Center) > 650 || (CEUtils.getDistance(Projectile.Center, player.Center) > 300 && Projectile.velocity.Y >= 10)) {
                    Projectile.velocity *= 0;
                    Flying = true;
                    SoundEngine.PlaySound(SoundID.Item128, Projectile.Center);
                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, Color.Gold, 0.0046f).Configure("CalamityEntropy/Assets/Particles/FlameExplosion", Vector2.One, CEUtils.randomRot(), 0.0046f, 0.05f, 18);
                    return;
                }
                Projectile.velocity *= 0.98f;
                float ms = 0.28f;
                if (Projectile.velocity.Y < -0.5f && Projectile.velocity.Length() < 10) {
                    ms = 2;
                }
                if (Projectile.Center.X < player.Center.X - 200) {
                    Projectile.velocity.X += ms;
                }
                if (Projectile.Center.X > player.Center.X + 200) {
                    Projectile.velocity.X -= ms;
                }
                if (Projectile.Distance(player.Center) < 200)
                    Projectile.velocity.X *= 0.9f;
                else
                    Projectile.velocity.X *= 0.99f;
                if (CEUtils.CheckSolidTile((Projectile.Center + new Vector2(Projectile.velocity.X * 5, 0)).getRectCentered(100, Projectile.height / 2 - 4))) {
                    if (Projectile.Distance(player.Center) > 240) {
                        if (Projectile.velocity.Y == 0)
                            Projectile.velocity.Y = -22;
                    }
                }
                if (Projectile.velocity.Y < 40)
                    Projectile.velocity.Y += 1.2f;
                dir = Projectile.velocity.X > 1 ? 1 : (Projectile.velocity.X < -1 ? -1 : dir);
                if (Projectile.velocity.Length() < 2) {
                    TexType = 2;
                    frc += 0.05f;
                    if (frc >= 1) {
                        frc = 0;
                        fr = 1 - fr;
                    }
                }
                else {
                    frc += Projectile.velocity.Length() * 0.03f;
                    if (frc >= 1) {
                        frc = 0;
                        fr = 1 - fr;

                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 dp = Projectile.Center;
            if (Projectile.isAPreviewDummy) {
                TexType = 0;
                dir = 1;
                dp += new Vector2(0, 20);
            }
            if (TexType == 0) {
                tex = this.getTextureAlt(fr == 0 ? "Move0" : "Move1");
            }
            if (TexType == 2) {
                tex = this.getTextureAlt(fr == 0 ? "Idle0" : "Idle1");
            }
            Main.EntitySpriteDraw(tex, dp - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * 0.5f, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            return false;
        }
    }
}