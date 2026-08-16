using CalamityEntropy.Content.Dusts;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class IceSpike : ModProjectile
    {
        public override void SetStaticDefaults()
        {

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 30;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 0;
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.Center, 8, 8, ModContent.DustType<IcePiece1>());
            }
            CEUtils.PlaySound("CryogenHit" + Main.rand.Next(1, 4), 1, Projectile.Center);
            target.AddBuff(ModContent.BuffType<FrozenLungs>(), 600);
            target.AddBuff(BuffID.Frostburn, 1080);
            Main.player[Projectile.owner].AddBuff(ModContent.BuffType<CosmicFreeze>(), 600);
        }
        public override void AI()
        {

            Projectile.velocity.Y += 0f;
            Projectile.rotation = Projectile.velocity.ToRotation();

        }


        public override bool PreDraw(ref Color lightColor)
        {
            CEUtils.DrawGlow(Projectile.Center, Color.LightBlue * 0.2f, 1f);
            Main.spriteBatch.Draw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, Projectile.GetTexture().Size() * 0.5f, 1, SpriteEffects.None, 0);
            return false;
        }
    }


}