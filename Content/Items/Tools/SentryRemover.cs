using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Tools
{
    public class SentryRemover : ModItem
    {
        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 40;
            Item.damage = 6;
            Item.knockBack = 4f;
            Item.useTime = Item.useAnimation = 15;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.crit = 2;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shootSpeed = 8;
            Item.shoot = ModContent.ProjectileType<SentryRemoverHeld>();
        }

        public override void AddRecipes() {
            CreateRecipe().
                AddRecipeGroup(CERecipeGroups.IronBar, 8).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }

    public class SentryRemoverHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Tools/SentryRemover";
        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 40, targetHitbox, 24);
        }
        public bool flag = true;
        public override void AI() {
            Projectile.StickToPlayer();
            Projectile.GetOwner().SetHandRot(Projectile.rotation);
            Projectile.Center = Projectile.GetOwner().gfxOffY * Vector2.UnitY + Projectile.GetOwner().GetFrontHandPositionImproved(Projectile.GetOwner().compositeFrontArm);
            Projectile.ai[1] = CEUtils.Parabola((Projectile.ai[0]++ / (Projectile.GetOwner().itemTimeMax)), 1);
            Projectile.Center += Projectile.rotation.ToRotationVector2() * (Projectile.ai[1] * 24 - 30);
            if (flag) {
                foreach (var p in Main.ActiveProjectiles) {
                    if (p.sentry && p.owner == Projectile.owner && Projectile.Colliding(Projectile.getRect(), p.getRect())) {
                        p.Kill();
                        SoundEngine.PlaySound(SoundID.DD2_DefenseTowerSpawn with { Pitch = 1 }, Projectile.Center);
                        Projectile.GetOwner().UpdateMaxTurrets();
                        CEUtils.SyncProj(p.whoAmI);
                        flag = false;
                        break;
                    }
                }
            }
            if (Projectile.ai[0] >= Projectile.GetOwner().itemTimeMax)
                Projectile.Kill();
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 44 * Projectile.scale, 24, DelegateMethods.CutTiles);
        }
        public override bool PreDraw(ref Color lightColor) {
            var tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.ai[1], Projectile.rotation + MathHelper.PiOver2, new Vector2(tex.Width / 2, tex.Height), Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
