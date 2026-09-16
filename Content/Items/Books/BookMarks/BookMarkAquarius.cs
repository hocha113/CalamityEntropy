using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkAquarius : BookMark
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Orange;
            Item.Entropy().stroke = true;
            Item.Entropy().NameColor = Color.LightBlue;
            Item.Entropy().strokeColor = Color.DarkBlue;
            Item.Entropy().tooltipStyle = 4;
            Item.value = Item.buyPrice(gold: 1);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Aquarius");
        public override Color tooltipColor => Color.LightBlue;
        public override EBookProjectileEffect getEffect() {
            return new AquariusBMEffect();
        }
    }

    /// <summary>宝瓶座书签(2026-08-31 平衡案重做):击中敌人爆出的水滴不再造成伤害,
    /// 而是飞向玩家,接触时消失并回复1生命值与3魔力值。</summary>
    public class AquariusBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            for (int i = 0; i < 5; i++) {
                Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center,
                    CEUtils.randomRot().ToRotationVector2() * 8, ModContent.ProjectileType<AquariusWaterBolt>(), 0, 0, projectile.owner);
            }
        }
    }

    // 治疗水滴:短暂飞散后追向主人,接触回复1生命/3魔力
    public class AquariusWaterBolt : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
        }
        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.DeepSkyBlue.ToVector3() * 0.2f);
            Player owner = Projectile.GetOwner();
            if (owner == null || !owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[0]++ > 15) {
                Projectile.velocity = (Projectile.velocity + (owner.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1.6f);
                if (Projectile.velocity.Length() > 16) {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 16;
                }
                if (CEUtils.getDistance(owner.Center, Projectile.Center) < 32) {
                    if (Projectile.owner == Main.myPlayer) {
                        if (owner.statLife < owner.statLifeMax2) {
                            owner.Heal(1);
                        }
                        owner.statMana = int.Min(owner.statManaMax2, owner.statMana + 3);
                        owner.ManaEffect(3);
                    }
                    Projectile.Kill();
                    return;
                }
            }
            else {
                Projectile.velocity *= 0.94f;
            }
            for (float i = 0; i < 1; i += 0.5f) {
                var p = PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center - Projectile.velocity * i, CEUtils.randomPointInCircle(1), Color.DeepSkyBlue, Main.rand.NextFloat(0.4f, 0.7f));
                p.lightColor = Color.DeepSkyBlue * 0.1f;
                p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            }
        }
        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 6; i++) {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water);
                d.noGravity = true;
                d.velocity = CEUtils.randomPointInCircle(3);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }
}