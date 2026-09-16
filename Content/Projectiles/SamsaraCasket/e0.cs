using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class e0 : ModProjectile
    {
        //帧动画数组(e0~e4),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/e", 0, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
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
            frameAddCounter--;
            if (frameAddCounter == 0) {
                frameAddCounter = 3;
                frame++;
                if (frame > 3) {
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
            Texture2D tex = Frames[frame];
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, 0, tex.Size() / 2, Projectile.scale * 2, SpriteEffects.None, 0);
            return false;
        }
    }


}