using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class HelhieimBlaster : ModProjectile
    {
        //枪体帧动画(hb0~hb17),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/HB/hb", 0, 18, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        int frame = 0;

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public float rp = 0;
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(rp);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            rp = reader.ReadSingle();
        }
        public bool sspl = false;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 1000;
        }
        public float back = -36;
        public int up = 0;
        bool shooted = false;
        public override void AI() {
            back *= 0.9f;
            if (Projectile.ai[0] > 0 && Projectile.ai[0] % 3 == 0) {
                if (frame < 8) {
                    frame++;
                }

                if (Projectile.ai[0] > 200) {
                    frame++;
                    if (frame > 17) {
                        Projectile.Kill();
                        SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/hbdisapear"), Projectile.Center);
                    }
                }
            }
            if (!sspl) {
                SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/hbapear"), Projectile.Center);
                sspl = true;
            }
            if (Projectile.ai[0] == 14) {


                if (!Main.dedServ && Projectile.owner == Main.myPlayer) {
                    SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/hblaser"), Projectile.Center);
                }

            }
            if (frame == 8 && Projectile.owner == Main.myPlayer) {
                if (!shooted || true) {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 120, Projectile.rotation.ToRotationVector2() * 40, ModContent.ProjectileType<MercyShoot>(), Projectile.damage, 0, Projectile.owner, 0, 0, Projectile.whoAmI);

                    p.ToProj().rotation = Projectile.rotation;
                    p.ToProj().scale = Projectile.scale;
                    p.ToProj().netUpdate = true;
                    shooted = true;
                }
            }
            if (Projectile.ai[0] != 13 || !Projectile.owner.ToPlayer().channel) {
                Projectile.ai[0]++;
            }
            Vector2 c = Projectile.owner.ToPlayer().Center;
            if (Projectile.Entropy().OnProj != -1) {
                c = Projectile.Entropy().OnProj.ToProj().Center;
            }
            if (Projectile.owner == Main.myPlayer) {
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Main.MouseWorld - Projectile.Center).ToRotation(), 0.2f, false);
                Projectile.netUpdate = true;
                rp = Projectile.rotation;

            }
            else {
                Projectile.rotation = rp;
            }
            if (Projectile.ai[0] < 13 && !Projectile.owner.ToPlayer().channel) {
                Projectile.Kill();
            }
            if (frame <= 4 || true) {
                Projectile.Center = Projectile.Center + (Projectile.owner.ToPlayer().Center + new Vector2(Projectile.ai[1], Projectile.ai[2]) - Projectile.Center) * 0.1f;
            }
            ct++;
            if (frame <= 4) {
                Projectile.timeLeft = 1000;
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public int ct = 0;
        public override bool PreDraw(ref Color lightColor) {

            if (frame >= 18) {
                return false;
            }
            Texture2D tx = Frames[frame];
            SpriteEffects ef = SpriteEffects.None;
            if (Projectile.rotation.ToRotationVector2().X < 0) {
                ef = SpriteEffects.FlipVertically;
            }
            if (frame == 8) {
                tx = Frames[(ct / 3) % 3 + 6];
            }
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * back + new Vector2(0, up), null, Projectile.owner.ToPlayer().Entropy().WeaponBoost > 0 ? Color.Purple : Color.White, Projectile.rotation, new Vector2(165, 144) / 2, Projectile.scale, ef, 0);
            return false;
        }
    }

}