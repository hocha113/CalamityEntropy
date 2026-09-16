using CalamityEntropy;
using CalamityEntropy.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Whips
{
    public class Crystedge : BaseWhipItem
    {
        public override int TagDamage => 6;
        public override float TagCritChance => 0.03f;

        public override void SetDefaults() {
            Item.DefaultToWhip(ModContent.ProjectileType<CrystedgeWhipSpawner>(), 48, 3, 5, 72);
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 20);
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = null;
        }
        public override bool CanUseItem(Player player) {
            return true;
        }
    }
    public class CrystedgeWhipSpawner : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public int projType = ModContent.ProjectileType<CrystedgeProj>();
        public override void SetStaticDefaults() {
            ProjectileID.Sets.IsAWhip[Projectile.type] = true;
        }
        public override void SetDefaults() {
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = Projectile.height = 1;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }

        public override void AI() {
            float atkSpeed = Projectile.GetOwner().GetTotalAttackSpeed(DamageClass.SummonMeleeSpeed);
            if (Main.myPlayer == Projectile.owner) {
                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((Main.MouseWorld - Projectile.GetOwner().HandPosition.Value).ToRotation());
            }
            Projectile.Center = Projectile.GetOwner().HandPosition.Value;
            if (Projectile.ai[0] > Projectile.ai[1] + 5 && Projectile.ai[0] < 41 && Main.myPlayer == Projectile.owner) {
                Projectile.ai[1] += 5;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, projType, Projectile.damage / 5, Projectile.knockBack / 3, Projectile.owner, 0, 0, 0);
            }
            if (Projectile.ai[0] >= 52 && Main.myPlayer == Projectile.owner) {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, projType, Projectile.damage, Projectile.knockBack, Projectile.owner, 0, 0, 1);
                Projectile.Kill();
            }
            Projectile.ai[0] += 1 * atkSpeed;
        }
    }
}
