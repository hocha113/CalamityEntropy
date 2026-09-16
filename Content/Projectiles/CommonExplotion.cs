using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{

    public class CommonExplotion : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 10;
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.getDistance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < Projectile.ai[0];
        }
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[1] > 0)
                return true;

            return null;
        }
    }

    public class CommonExplotionFriendly : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults() {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ignoreWater = true;
        }
        public Action<NPC, NPC.HitInfo, int> onHitAction = null;
        public Action<NPC> modifyHitAction = null;
        public float DamageMulToWormSegs = 1;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifyHitAction?.Invoke(target);
            if (target.realLife >= 0)
                modifiers.SourceDamage *= DamageMulToWormSegs;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            onHitAction?.Invoke(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor) {
            Projectile.width = Projectile.height = (int)(Projectile.ai[2] * 2);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.ai[0] < 400)
                return Projectile.Center.getRectCentered(Projectile.ai[0] * 2, Projectile.ai[0] * 2).Intersects(targetHitbox);
            return CEUtils.getDistance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < Projectile.ai[0];
        }
    }
}