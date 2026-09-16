using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class MantleBreak : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 40;
            Projectile.penetrate = -1;
        }
        public int frame = 0;
        public int frameAddCounter = 3;
        public bool playedSound = false;
        public override void AI() {
            if (!playedSound) {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/holyshield_shatter") { Volume = 0.6f }, Projectile.Center);
                playedSound = true;
            }
            frameAddCounter--;
            if (frameAddCounter == 0) {
                frameAddCounter = 3;
                frame++;
                if (frame > 7) {
                    Projectile.Kill();
                }
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, CEUtils.GetCutTexRect(tex, 8, frame), Color.White, 0, new Vector2(32, 32), Projectile.scale * 2, SpriteEffects.None, 0);
            return false;
        }
    }


}