using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class VoidBottleThrow : ModProjectile
    {
        //瓶子三形态贴图(首张无序号后缀,数组吃不下,逐字段声明),加载期就位
        [VaultLoaden("CalamityEntropy/Content/Projectiles/VoidBottleThrow")]
        internal static Asset<Texture2D> Bottle0Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/VoidBottleThrow1")]
        internal static Asset<Texture2D> Bottle1Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/VoidBottleThrow2")]
        internal static Asset<Texture2D> Bottle2Tex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 2f;
            Projectile.timeLeft = 1000;
            Projectile.penetrate = -1;
        }
        // 持续屏震：随蓄力时间增强，存活期间每帧刷新幅度
        private ScreenShaker.ScreenShake shake = null;
        public override void AI() {
            Projectile.ai[0]++;
            Projectile.rotation += MathHelper.ToRadians(Projectile.velocity.X * 0.5f);
            Projectile.velocity *= 0.99f;
            if (Projectile.ai[0] == 120) {
                Projectile.ai[1] = 1;
                SoundEngine.PlaySound(SoundID.Item107, Projectile.Center);
                for (int i = 0; i < 6; i++) {
                    Dust.NewDust(Projectile.Center, 8, 8, ModContent.DustType<GlassBreak>());
                }
            }
            if (Projectile.ai[0] == 200) {
                Projectile.ai[1] = 2;
                SoundEngine.PlaySound(SoundID.Item107, Projectile.Center);
                for (int i = 0; i < 12; i++) {
                    Dust.NewDust(Projectile.Center, 8, 8, ModContent.DustType<GlassBreak>());
                }
            }
            if (!Main.dedServ) {
                float shakeAmp = Utils.Remap(Main.LocalPlayer.Distance(Projectile.Center), 1800f, 1000f, 0f, 4.5f) * Projectile.ai[0] / 60f;
                if (shake == null || !shake.active)
                    shake = ScreenShaker.AddShake(Vector2.Zero, shakeAmp);
                else
                    shake.amplitude = shakeAmp;
            }
            if (Projectile.ai[0] > 200) {
                for (int i = 0; i < 9; i++) {
                    //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                    var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, CEUtils.randomPointInCircle(8), Color.White, 1f);
                    p.Opacity = Main.rand.NextFloat(0.8f, 1.6f);
                    p.shape = 4;
                    p.vd = 0.97f;
                }

            }
            else if (Projectile.ai[0] > 120) {
                for (int i = 0; i < 5; i++) {
                    //每帧拖尾Void,旧spawnNew也是AI里无脑刷
                    var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, CEUtils.randomPointInCircle(8), Color.White, 1f);
                    p.Opacity = Main.rand.NextFloat(0.8f, 1.6f);
                    p.vd = 0.97f;
                }
            }
            if (Projectile.ai[0] == 280) {
                Projectile.Kill();
                SoundEngine.PlaySound(SoundID.Item107, Projectile.Center);
                for (int i = 0; i < 30; i++) {
                    Dust.NewDust(Projectile.Center, 8, 8, ModContent.DustType<GlassBreak>());
                }
                /*if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(Projectile.owner, ModContent.NPCType<CruiserHead>());
                else
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, Projectile.owner, ModContent.NPCType<CruiserHead>());
*/
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override bool PreDraw(ref Color lightColor) {

            Texture2D tx1 = Bottle0Tex.Value;
            Texture2D tx2 = Bottle1Tex.Value;
            Texture2D tx3 = Bottle2Tex.Value;
            Texture2D tx = tx1;
            if (Projectile.ai[1] == 1) {
                tx = tx2;
            }
            if (Projectile.ai[1] == 2) {
                tx = tx3;
            }

            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, 1, SpriteEffects.None, 0);

            return false;
        }


    }


}