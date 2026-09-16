using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BMDirtProj : EBookBaseProjectile
    {
        public int itemType => (int)Projectile.ai[0];
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.penetrate = 5;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 480;
            Projectile.tileCollide = true;
            Projectile.width = Projectile.height = 26;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.ai[1] > 0) {
                NPC targeth = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 800, (i) => (i != target.whoAmI && Projectile.localNPCImmunity[i] == 0));
                if (targeth != null) {
                    Projectile.localAI[2] = 1;
                    Projectile.velocity = new Vector2(Projectile.velocity.Length() * 1f, 0).RotatedBy((targeth.Center - Projectile.Center).ToRotation());
                }
                else {
                    Projectile.velocity = Projectile.velocity.RotatedByRandom(0.6f) * 1.2f;
                }
                SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            }
            if (Projectile.ai[1] == -1) {
                target.velocity *= 0.4f;
                Projectile.Kill();
            }
        }
        public override void AI() {
            base.AI();
            if (Projectile.localAI[2]++ > 6) {
                Projectile.velocity.Y += 0.4f;
                Projectile.velocity *= 0.998f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;
        }
        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 16; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, Projectile.ai[0] != ItemID.StoneBlock ? DustID.Dirt : DustID.Stone);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        }
        public override bool PreDraw(ref Color lightColor) {
            int type = itemType;
            if (type > 0) {
                Main.instance.LoadItem(type);
                Texture2D tex = TextureAssets.Item[type].Value;
                Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, tex));
            }
            {

            }
            return false;
        }
    }
}