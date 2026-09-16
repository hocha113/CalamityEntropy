using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Rarities;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkVoid : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.value = Item.buyPrice(platinum: 1, gold: 75);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Void");
        public override EBookProjectileEffect getEffect() {
            return new VoidBMEffect();
        }
        public override Color tooltipColor => Color.Lerp(Color.MediumPurple, Color.Gold, (0.5f + (float)Math.Cos(Main.GlobalTimeWrappedHourly * 6)));
    }

    public class VoidBMEffect : EBookProjectileEffect
    {
        public override void OnProjectileSpawn(Projectile projectile, bool ownerClient) {
            (projectile.ModProjectile as EBookBaseProjectile).color = Color.MediumPurple;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 80, 1.5f, 800, 18);
            if (Main.rand.NextBool(projectile.HasEBookEffect<APlusBMEffect>() ? 4 : 5) && CECooldowns.CheckCD(ref CECooldowns.BMVoid, 60)) {
                Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<VoidBurst>(), EBookProjectileEffect.FixedDamage(projectile.GetOwner(), 2000, projectile.DamageType), 1, projectile.owner);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), 0, 1, projectile.owner);

                for (int i = 0; i < 24; i++) {
                    //PRT_Smoke双色系各24颗,timeleftmax/Lifetime spawn后直赋
                    var p = PRTLoader.NewParticle<PRT_Smoke>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6, 16), new Color(140, 140, 255), 0.3f);
                    p.Lifetime = 26;
                    p.timeleftmax = 26;
                    p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0f, 26);
                    p = PRTLoader.NewParticle<PRT_Smoke>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6, 16), Color.LightGoldenrodYellow, 0.3f);
                    p.Lifetime = 26;
                    p.timeleftmax = 26;
                    p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0f, 26);
                }
            }
        }
    }
}
