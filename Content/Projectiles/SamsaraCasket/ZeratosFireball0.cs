using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class ZeratosFireball0 : ModProjectile
    {
        //帧动画数组(ZeratosFireball0~4),加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/ZeratosFireball", 0, 5, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] Frames;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 164;
            Projectile.height = 164;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        int framecounter = 3;
        int frame = 0;
        public override void AI() {
            Projectile.ArmorPenetration = HorizonssKey.getArmorPen();
            if (frame == 0 && framecounter == 3) {
                SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            }
            framecounter--;
            if (framecounter == 0) {
                frame++;
                framecounter = 3;
                if (frame > 4) {
                    Projectile.Kill();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (HorizonssKey.getVoidTouchLevel() > 0) {
                EGlobalNPC.AddVoidTouch(target, 80, HorizonssKey.getVoidTouchLevel(), 800, 16);
            }
        }

        public override bool PreDraw(ref Color lightColor) {

            lightColor = Color.White;
            Texture2D tex = Frames[frame];
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * 3, SpriteEffects.None, 0);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }

    }

}