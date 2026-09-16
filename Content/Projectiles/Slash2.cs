using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class Slash2 : ModProjectile
    {
        //斩击贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Slash2")]
        internal static Asset<Texture2D> SlashTex;
        public bool sPlayerd = false;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 600;
            Projectile.height = 600;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 30f;
            Projectile.timeLeft = 50;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 1024;
        }

        public override void AI() {
            if (!sPlayerd) {
                sPlayerd = true;
                //PRT_DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定
                PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, Color.Aqua, 0.1f).Configure(new Vector2(2f, 2f), 0, 0.85f, 36);  //DirectionalPulseRing Configure是Calamity ring原构造,scale/rotation/lifetime顺序固定

                PRTLoader.NewParticle<PRT_DetailedExplosionCal>(Projectile.Center, Vector2.Zero, Color.Magenta, 0f).Configure(Vector2.One, Main.rand.NextFloat(-5, 5), 0.65f, 26);
                SoundStyle s = new("CalamityEntropy/Assets/Sounds/swing" + Main.rand.Next(1, 4));
                s.Volume = 1f;
                s.Pitch = 0.8f;
                SoundEngine.PlaySound(s, Projectile.Center);
            }
            if (Projectile.ai[2] < 6) {
                Projectile.ai[0] += 100;
            }
            else {
                Projectile.ai[1] = Projectile.ai[1] + (Projectile.ai[0] - Projectile.ai[1]) * 0.3f;
            }
            Projectile.ai[2]++;
        }

        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.ai[2] < 5 && projHitbox.Intersects(targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 400);
        }
        public override bool PreDraw(ref Color lightColor) {

            Texture2D tx = SlashTex.Value;
            Main.spriteBatch.Draw(tx, Projectile.Center - Main.screenPosition + new Vector2((Projectile.ai[0] + Projectile.ai[1]) / 2 - 300, 0).RotatedBy(Projectile.rotation), null, Color.White, Projectile.rotation, new Vector2(tx.Width, tx.Height) / 2, new Vector2((Projectile.ai[0] - Projectile.ai[1]) / tx.Width, 1.2f), SpriteEffects.None, 0);

            return false;
        }


    }


}