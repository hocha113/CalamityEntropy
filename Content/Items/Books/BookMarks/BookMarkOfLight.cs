using CalamityEntropy.Common;
using CalamityEntropy.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkOfLight : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 20);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Light");
        public override Color tooltipColor => Color.GreenYellow;
        public override EBookProjectileEffect getEffect() {
            return new LightSoulBMEffect();
        }
    }
    /// <summary>光明书签(2026-08-31 平衡案重做):光明能量不再回血与消除弹幕,
    /// 而是直接飞向玩家并回复5点魔力;持书期间+20%跳跃速度。</summary>
    public class LightSoulBMEffect : EBookProjectileEffect
    {
        public override void BookUpdate(Projectile projectile, bool ownerClient) {
            projectile.GetOwner().Entropy().bmLightJumpTime = 2;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            if (Main.rand.NextBool(projectile.HasEBookEffect<APlusBMEffect>() ? 4 : 6) && CECooldowns.CheckCD(ref CECooldowns.BMLightCD, 100)) {
                (Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(30, 34), ModContent.ProjectileType<LightSoul>(), 0, projectile.knockBack / 3, projectile.owner).ToProj().ModProjectile as EBookBaseProjectile).homing = 0;
            }
        }
    }
}