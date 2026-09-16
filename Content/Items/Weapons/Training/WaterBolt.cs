using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Training
{
    public class WaterBolt : ModProjectile
    {
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 60;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 10000;
        }
        public int counter = 0;
        public float FrameTime => 4;
        public float frameCounter = 0;
        public int frame = 0;
        public int TotalFrame() => 6;
        public bool active = false;
        public override void AI() {
            if (!CEUtils.CheckSolidTileOrPlatform(Projectile.getRect())) {
                Projectile.Kill();
            }
            else
                active = true;


            if (Projectile.localAI[2] == 0 && !Main.dedServ && active)
                SoundEngine.PlaySound(SoundID.Splash with { Pitch = 0.25f }, Projectile.Center);
            if (active)
                CEUtils.AddLight(Projectile.Center, new Color(124, 124, 255));
            if (Projectile.localAI[2]++ == 3) {
                if (Projectile.ai[0] > 0 && Main.myPlayer == Projectile.owner) {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(64 * Projectile.ai[1], 0), Vector2.Zero, Projectile.type, Projectile.damage, 0, Projectile.owner, Projectile.ai[0] - 1, Projectile.ai[1]);
                }
                for (int i = 0; i < 32; i++) {
                    Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0, -16 * Main.rand.NextFloat());
                }
            }
            Player player = Projectile.GetOwner();
            frameCounter++;
            if (frameCounter > FrameTime) {
                frameCounter -= FrameTime;
                frame++;
                if (frame >= TotalFrame()) {
                    frame--;
                    Projectile.Kill();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.velocity.Y -= 16 * target.knockBackResist;
            target.velocity.X *= 1 - 0.8f * target.knockBackResist;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!active)
                return false;
            return null;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!active)
                return false;
            Texture2D tex = Projectile.GetTexture();
            int num1 = tex.Height / TotalFrame();
            Rectangle sourceRect = new Rectangle(0, num1 * frame, tex.Width, num1 - 2);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, sourceRect, Color.White, 0, new Vector2(tex.Width / 2f, (num1 - 2) / 2), Projectile.scale, SpriteEffects.None);

            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }
    }

}