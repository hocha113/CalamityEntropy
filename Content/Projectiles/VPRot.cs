using CalamityEntropy.Content.NPCs.VoidInvasion;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class VPRot : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 5000;

        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 3;

        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            if (!((int)(Projectile.ai[2])).ToNPC().active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = ((int)(Projectile.ai[2])).ToNPC().Center + ((int)(Projectile.ai[2])).ToNPC().rotation.ToRotationVector2() * 47;
            if (((VoidPopeHand)((int)(Projectile.ai[2])).ToNPC().ModNPC).circle)
            {
                Projectile.timeLeft = 3;
            }
            odp.Add(Projectile.Center);
            if (odp.Count > 2)
            {
                odp.RemoveAt(0);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 1; i < odp.Count; i++)
            {
                if (CEUtils.LineThroughRect(odp[i - 1], odp[i], targetHitbox, 90))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {

            return false;
        }
    }

}