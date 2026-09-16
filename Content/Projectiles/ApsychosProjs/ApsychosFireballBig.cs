using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.ApsychosProjs
{
    public class ApsychosFireballBig : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 200;
            Projectile.penetrate = -1;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            float l = 128 * scale;
            hitbox = Projectile.Center.getRectCentered(l, l);
        }
        public float scale = 0;
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(BuffID.OnFire3, 180);
        }
        public float ft = 2;
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public override void AI() {
            if (Projectile.ai[2] == 0)
                Projectile.ai[2] = Main.rand.NextBool() ? 1 : -1;
            if (scale < 1)
                scale += 0.05f;
            if (Projectile.timeLeft > 10 && scale > 0.4f) {
                if (ft < 10)
                    ft += 0.32f;
                for (int i = 0; i < 3; i++) {
                    float r = i * (MathHelper.TwoPi / 3f) + Main.GameUpdateCount * 0.052f * (Projectile.ai[2] == 1 ? 1 : -1);
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, r.ToRotationVector2() * 42 + Projectile.velocity, ModContent.ProjectileType<ApsychosFire>(), (int)(Projectile.damage * 0.8f), 0, -1, (int)ft, Projectile.ai[1]);
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            float alpha = 1;
            if (Projectile.timeLeft < 20)
                alpha = Projectile.timeLeft / 20f;
            CEUtils.DrawGlow(Projectile.Center, (Projectile.ai[1] == 1 ? Color.OrangeRed : Color.LightBlue) * alpha, 4f * scale);
            CEUtils.DrawGlow(Projectile.Center, Color.White * 0.8f * alpha, 3f * scale);
            return false;
        }
    }


}
