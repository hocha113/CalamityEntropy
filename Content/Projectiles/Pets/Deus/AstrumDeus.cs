using CalamityEntropy.Content.Buffs.Pets;
using CalamityEntropy.Core.CalamityRef;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Pets.Deus
{
    public class AstrumDeus : ModProjectile
    {
        //首帧文件名不带序号(AstrumDeus.png 同时是弹幕主贴图),数组标签只认「路径+数字」,首帧单独加载
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Deus/AstrumDeus")]
        internal static Asset<Texture2D> Frame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Deus/AstrumDeus", 2, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FramesRest;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Deus/s/AstrumDeus")]
        internal static Asset<Texture2D> HatFrame1;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Pets/Deus/s/AstrumDeus", 2, 4, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] HatFramesRest;
        public int counter = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.LightPet[Projectile.type] = true;
            base.SetStaticDefaults();

        }
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 24;
            Projectile.height = 58;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            bool hat = Projectile.owner.ToPlayer().Entropy().PetsHat;
            if (Main.gameMenu)
            {
                Texture2D txd = Frame1.Value;
                Main.EntitySpriteDraw(txd, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(txd.Width, txd.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

                return false;
            }
            //共 5 帧:首帧 + 后 4 帧,按下标拼回原来的取帧顺序
            Texture2D[] rest = hat ? HatFramesRest : FramesRest;
            int frame = (counter / 6) % (rest.Length + 1);
            Texture2D tx = frame == 0 ? (hat ? HatFrame1 : Frame1).Value : rest[frame - 1];
            if (Projectile.velocity.X > -2 && Projectile.velocity.X < 2f)
            {
                if (Main.player[Projectile.owner].Center.X > Projectile.Center.X)
                {
                    Projectile.direction = 1;
                }
                else
                {
                    Projectile.direction = -1;
                }
            }
            if (Projectile.direction == -1)
            {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.FlipHorizontally, 0);

            }
            else
            {
                Main.EntitySpriteDraw(tx, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, Projectile.scale, SpriteEffects.None, 0);
            }


            return false;

        }
        void MoveToTarget(Vector2 targetPos)
        {
            if (CEUtils.getDistance(Projectile.Center, targetPos) > 1400)
            {
                Projectile.Center = Main.player[Projectile.owner].Center - new Vector2(0, 50);
            }
            if (Projectile.ai[1] == 1 || true)
            {
                Projectile.tileCollide = false;
                Projectile.rotation = MathHelper.ToRadians((Projectile.velocity.X * 1.4f));
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 100)
                {
                    Vector2 px = targetPos - Projectile.Center;
                    px.Normalize();
                    Projectile.velocity += px * 0.8f;

                    Projectile.velocity *= 0.94f;

                }
                if (CEUtils.getDistance(Projectile.Center, targetPos) < 100)
                {
                    Projectile.ai[1] = 0;
                }
                if (Projectile.velocity.X > 0)
                {
                    Projectile.direction = 1;
                }
                else
                {
                    Projectile.direction = -1;
                }
            }
            else
            {
                Projectile.tileCollide = true;
                Projectile.rotation = 0;
                Projectile.velocity.Y += 0.5f;
                if (CEUtils.getDistance(targetPos, Projectile.Center) > 600)
                {
                    Projectile.ai[1] = 1;
                }
                else if (CEUtils.getDistance(targetPos * new Vector2(1, 0), Projectile.Center * new Vector2(1, 0)) > 200)
                {
                    if (targetPos.X > Projectile.Center.X)
                    {
                        Projectile.velocity.X += 0.6f;
                    }
                    else
                    {
                        Projectile.velocity.X -= 0.6f;
                    }
                }
                if (targetPos.X > Projectile.Center.X)
                {
                    Projectile.direction = 1;
                }
                else
                {
                    Projectile.direction = -1;
                }
                Projectile.velocity.X *= 0.96f;
            }

        }
        public override bool PreAI()
        {
            Player player = Main.player[Projectile.owner];

            player.zephyrfish = false;
            if (CECal.DownedAstrumDeus)
            {
                Lighting.AddLight(Projectile.Center, 1.2f, 1f, 1.2f);
            }
            else
            {
                if (Main.hardMode)
                {
                    Lighting.AddLight(Projectile.Center, 0.6f, 0.5f, 0.6f);
                }
                else
                {
                    Lighting.AddLight(Projectile.Center, 0.4f, 0.4f, 0.4f);
                }
            }
            return true;
        }
        public int shotCd = 0;

        public override void AI()
        {
            counter++;
            Player player = Main.player[Projectile.owner];
            MoveToTarget(player.Center + new Vector2(0, -60));
            if (!player.dead && player.HasBuff(ModContent.BuffType<AstrumDeusBuff>()))
            {
                Projectile.timeLeft = 2;
            }
            NPC n = Projectile.FindTargetWithinRange(1000, false);
            shotCd--;
            if (n != null && shotCd < 0)
            {
                if (Projectile.owner == Main.myPlayer && CECal.DownedAstrumDeus)
                {
                    shotCd = 400;

                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (n.Center - Projectile.Center).SafeNormalize(Vector2.Zero), ModContent.ProjectileType<AstralShot>(), 460, 2, Projectile.owner);
                }
            }
        }


    }
}
