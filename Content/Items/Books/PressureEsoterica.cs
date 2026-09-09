using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.PortsDoT;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books
{
    public class PressureEsoterica : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 66;
            Item.useAnimation = Item.useTime = 24;
            Item.crit = 4;
            Item.mana = 18;
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.buyPrice(gold: 45);
            Item.width = 40;
            Item.height = 54;
            Item.shootSpeed = 18;
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/PE")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<PressureEsotericaHeld>();
        public override int SlotCount => 3;

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_PrimordialEarth, CEID.Item_DepthCells))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_PrimordialEarth)
                .AddIngredient(CEID.Item_DepthCells, 3)
                .AddIngredient(ItemID.Ectoplasm, 8)
                .AddTile(TileID.Bookcases)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.WaterBolt)
                .AddIngredient(ItemID.SpectreBar, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class PressureEsotericaHeld : EntropyBookDrawingAlt
    {
        public override int frameChange => 4;
        public override string OpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/PressureEsoterica/Open";
        public override string PageAnimationPath => $"{EntropyBook.BaseFolder}/Textures/PressureEsoterica/Page";
        public override string UIOpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/PressureEsoterica/UI";

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.armorPenetration += 36;
            m.Homing += 0.1f;
            m.HomingRange += 0.2f;
            return m;
        }
        public override EBookProjectileEffect getEffect()
        {
            return new PEBookBaseEffect();
        }
        public override float randomShootRotMax => 0;
        public override int baseProjectileType => ModContent.ProjectileType<AbyssalRift>();
    }
    public class PEBookBaseEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 400);
        }
    }
    public class AbyssalRift : EBookBaseProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.light = 0.4f;
            Projectile.timeLeft = 150;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.MaxUpdates = 2;
        }
        public override Color baseColor => Color.DarkBlue;
        public List<Vector2> points = new List<Vector2>();
        int d = 0;
        public Vector2 tpos = Vector2.Zero;
        public float r = 0;
        public float damage = 1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            damage *= 0.98f;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= damage;
            modifiers.ArmorPenetration += 16;
            target.Entropy().Decrease20DR = 7;
            base.ModifyHitNPC(target, ref modifiers);
        }
        public override void ApplyHoming()
        {
            if (homing <= 0)
            {
                return;
            }
            NPC homingTarget = CEUtils.findTarget(Projectile.GetOwner(), Projectile, (int)homingRange, (Projectile.tileCollide ? true : false));
            if (homingTarget != null)
            {
                tpos = Vector2.Lerp(tpos, Projectile.Center + ((homingTarget.Center - Projectile.Center).normalize() * tpos.Distance(Projectile.Center)), homing * 0.6f);
            }
        }
        public override void AI()
        {
            if (tpos == Vector2.Zero)
                tpos = Projectile.Center + Projectile.velocity.normalize() * 4600;
            base.AI();
            if (Main.rand.NextBool(12))
                r += Main.rand.NextFloat(-0.04f, 0.04f);
            Projectile.velocity = Projectile.velocity.RotatedBy(r);
            r *= 0.95f;
            Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (tpos - Projectile.Center).ToRotation(), 0.04f + r * 0.4f).ToRotationVector2() * Projectile.velocity.Length();
            Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (tpos - Projectile.Center).ToRotation(), 0.1f + r, false).ToRotationVector2() * Projectile.velocity.Length();
            points.Add(Projectile.Center + CEUtils.randomPointInCircle(Projectile.velocity.Length() * 0.4f));
            if (points.Count > 19)
                points.RemoveAt(0);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 1)
            {
                return false;
            }
            for (int i = 1; i < points.Count; i++)
            {
                if (CEUtils.LineThroughRect(points[i - 1], points[i], targetHitbox, 30))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!ModContent.GetInstance<Config>().EnablePixelEffect)
                draw();
            return false;
        }

        public void draw()
        {
            if (points.Count < 1)
            {
                return;
            }
            Texture2D px = CEExtraAssets.white;
            float jd = 1;
            float lw = damage;
            if (Projectile.timeLeft < 60)
                lw *= Projectile.timeLeft / 60f;
            Color color = baseColor;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 jv = Vector2.Zero;
                CEUtils.drawLine(Main.spriteBatch, px, points[i - 1], points[i] + jv, color * jd, 1f * lw * (new Vector2(-30, 0).RotatedBy(MathHelper.ToRadians(180 * ((float)i / points.Count)))).Y, 3);
            }
        }
    }
}
