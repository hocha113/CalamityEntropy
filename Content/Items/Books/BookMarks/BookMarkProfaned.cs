using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkProfaned : BookMark
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = CECal.RarityTurquoise(ModContent.RarityType<NihilityBlue>());
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
        }
        public override Texture2D UITexture => BookMark.GetUITexture("Profaned");
        public override EBookProjectileEffect getEffect()
        {
            return new ProfanedBMEffect();
        }
        public override Color tooltipColor => Color.Firebrick;
    }

    /// <summary>祭祀书签(2026-08-31 平衡案重做):攻击时召唤2发拜月邪教徒式追踪火球,
    /// 同时从身后发射2发暗影火球(均固定基伤50)。</summary>
    public class ProfanedBMEffect : EBookProjectileEffect
    {
        public override void OnShoot(EntropyBookHeldProjectile book)
        {
            Projectile proj = book.Projectile;
            Player owner = proj.GetOwner();
            int dmg = FixedDamage(owner, 50, proj.DamageType);
            for (int i = 0; i < 2; i++)
            {
                // 追踪火球:朝准星方向散射
                int p = Projectile.NewProjectile(proj.GetSource_FromAI(), proj.Center,
                    (proj.rotation + Main.rand.NextFloat(-0.4f, 0.4f)).ToRotationVector2() * 9f,
                    ModContent.ProjectileType<ProfanedFireball>(), dmg, proj.knockBack, proj.owner, 0);
                // 暗影火球:从玩家身后射出
                int p2 = Projectile.NewProjectile(proj.GetSource_FromAI(), owner.Center - proj.rotation.ToRotationVector2() * 40,
                    (proj.rotation + MathHelper.Pi + Main.rand.NextFloat(-0.5f, 0.5f)).ToRotationVector2() * 7f,
                    ModContent.ProjectileType<ProfanedFireball>(), dmg, proj.knockBack, proj.owner, 1);
                if (p >= 0 && p < Main.maxProjectiles)
                    Main.projectile[p].DamageType = proj.DamageType;
                if (p2 >= 0 && p2 < Main.maxProjectiles)
                    Main.projectile[p2].DamageType = proj.DamageType;
            }
        }
    }

    /// <summary>拜月式追踪火球(ai[0]=0 金焰 / 1 暗影焰)。</summary>
    public class ProfanedFireball : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + Terraria.ID.ProjectileID.CultistBossFireBall;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1;
        }
        public bool Shadow => Projectile.ai[0] == 1;
        public override void AI()
        {
            if (++Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            NPC target = Projectile.FindTargetWithinRange(800, false);
            if (target != null && Projectile.localAI[0]++ > 10)
            {
                Projectile.velocity += (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.7f;
                if (Projectile.velocity.Length() > 13)
                {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 13;
                }
            }
            Dust d = Dust.NewDustPerfect(Projectile.Center, Shadow ? Terraria.ID.DustID.Shadowflame : Terraria.ID.DustID.GoldFlame, -Projectile.velocity * 0.2f);
            d.noGravity = true;
            d.scale = Main.rand.NextFloat(1.1f, 1.6f);
            Lighting.AddLight(Projectile.Center, Shadow ? new Vector3(0.3f, 0.1f, 0.45f) : new Vector3(0.5f, 0.4f, 0.1f));
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Shadow)
            {
                target.AddBuff(Terraria.ID.BuffID.ShadowFlame, 180);
            }
            else
            {
                target.AddBuff(Terraria.ID.BuffID.OnFire3, 180);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Microsoft.Xna.Framework.Graphics.Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Color c = Shadow ? new Color(160, 80, 255, 120) : new Color(255, 255, 255, 160);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, c, Projectile.rotation, frame.Size() / 2f, Projectile.scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
            return false;
        }
    }
}
