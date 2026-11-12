using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>隐形计数弹幕:每个占1个召唤栏位、可被献祭,个数就是史莱姆的栏位数;不画、不造成伤害</summary>
    public class CelestialSlimeStaffCounter : ModProjectile
    {
        //判定边长(像素,不参与碰撞);续命帧数(有增益时每帧续上,没了就在这么多帧后消失)
        public const int Size = 4;
        public const int KeepAliveFrames = 2;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetStaticDefaults() {
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = Projectile.height = Size;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = KeepAliveFrames;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;
        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                owner.ClearBuff(ModContent.BuffType<CelestialSlimeStaffBuff>());
                return;
            }
            if (owner.HasBuff(ModContent.BuffType<CelestialSlimeStaffBuff>())) {
                Projectile.timeLeft = KeepAliveFrames;
            }
            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
