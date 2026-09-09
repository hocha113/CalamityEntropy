using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.Content.Projectiles.OblivionHoldout;
using CalamityEntropy.Core.CalamityRef;
namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookmarkPactOfWar : BookMark
    {

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = CECal.RarityBurnishedAuric(ModContent.RarityType<Golden>());
            Item.value = Item.buyPrice(platinum: 2, gold: 40);

        }
        public override Texture2D UITexture => BookMark.GetUITexture("PactOfWar");
        public override Color tooltipColor => new Color(200, 255, 200);
        public override EBookProjectileEffect getEffect()
        {
            return new WarPactBMEffect();
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<FadingRunestone>()
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }

    public class WarPactBMEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 180);
        }
        public static void CheckProj(EntropyBookHeldProjectile book)
        {
            Player player = book.Projectile.GetOwner();
            int projtype1 = ModContent.ProjectileType<WarPactApollo>();
            if (player.ownedProjectileCounts[projtype1] < 1)
                book.ShootSingleProjectile(projtype1, book.Projectile.Center, (Main.MouseWorld - book.Projectile.Center), 1, 1, fixedBaseDamage: 500);

            int projtype2 = ModContent.ProjectileType<WarPactArtemis>();
            if (player.ownedProjectileCounts[projtype2] < 1)
                book.ShootSingleProjectile(projtype2, book.Projectile.Center, (Main.MouseWorld - book.Projectile.Center), 1, 1, fixedBaseDamage: 500);

        }

    }

    public class WarPactApollo : EBookBaseProjectile
    {
        public float yofs => (float)(Math.Sin(Main.GameUpdateCount * 0.06f) * 62);
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public bool sp = true;
        NPC target = null;
        public override void AI()
        {
            if (Projectile.owner != Main.myPlayer || BookMarkLoader.HeldingBookAndHasBookmarkEffect<WarPactBMEffect>(Projectile.GetOwner()))
            {
                Projectile.timeLeft = 4;
            }
            else
            {
                Projectile.Kill();
            }
            if (sp)
            {
                sp = false;
                for (int i = 0; i < 24; i++)
                {
                    v1.Add(new Vpoint() { pos = Projectile.Center });
                    v2.Add(new Vpoint() { pos = Projectile.Center });
                }
            }
            updatePoints();
            Projectile.penetrate = -1;
            base.AI();
            if (target == null || !target.active || target.dontTakeDamage)
            {
                target = Projectile.FindTargetWithinRange(2000);
            }
            Vector2 tpos = Projectile.GetOwner().Center;
            if (target != null)
            {
                tpos = target.Center;
            }
            tpos.Y += yofs * (target == null ? 0 : 1);
            tpos.X += 400;
            int s = 28;
            if (CEUtils.getDistance(tpos, Projectile.Center) > s)
            {
                Projectile.velocity = (tpos - Projectile.Center).normalize() * s;
            }
            else
            {
                Projectile.velocity = (tpos - Projectile.Center);
            }
            tpos.X -= 400;
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Projectile.velocity.Length() > 4 && target == null ? Projectile.velocity.ToRotation() : (tpos - Projectile.Center).ToRotation()), 20f.ToRadians(), true);
            if (target != null)
            {
                Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
            }
            if (target != null && Projectile.ai[0]++ % 40 <= 20 && Projectile.ai[0] % 4 == 0)
            {
                if (Projectile.owner == Main.myPlayer)
                    (this.ShooterModProjectile as EntropyBookHeldProjectile).ShootSingleProjectile(ModContent.ProjectileType<ExoShot1>(), Projectile.Center, (target.Center + target.velocity * 4 - Projectile.Center), 0.2f, 1, 2f);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(6))
            {
                base.OnHitNPC(target, hit, damageDone);
            }
        }
        public Color RibbonTrailColorFunction(float completionRatio, Vector2 vertex)
        {
            Color startingColor = new Color(34, 40, 48);
            Color endColor = new Color(40, 160, 32);
            return Color.Lerp(startingColor, endColor, (float)Math.Pow(completionRatio, 1.5D)) * Projectile.Opacity;
        }
        public float RibbonTrailWidthFunction(float completionRatio, Vector2 vertex)
        {
            float baseWidth = Utils.GetLerpValue(1f, 0.54f, completionRatio, true) * 5f;
            float endTipWidth = CEUtils.Convert01To010(Utils.GetLerpValue(0.96f, 0.89f, completionRatio, true)) * 2.4f;
            return (baseWidth + endTipWidth) * 0.6f * Projectile.scale;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 uposu = Projectile.Center + new Vector2(-4, -18).RotatedBy(Projectile.rotation) * Projectile.scale;
            Vector2 uposd = Projectile.Center + new Vector2(-4, 18).RotatedBy(Projectile.rotation) * Projectile.scale;
            List<Vector2> p1 = new List<Vector2>();
            List<Vector2> p2 = new List<Vector2>();
            p1.Add(uposu);
            p2.Add(uposd);
            for (int i = 0; i < v1.Count; i++)
            {
                p1.Add(v1[i].pos);
                p2.Add(v2[i].pos);
            }
            CEPrimitiveRenderer.RenderTrail(p1, new(RibbonTrailWidthFunction, RibbonTrailColorFunction), 66);
            CEPrimitiveRenderer.RenderTrail(p2, new(RibbonTrailWidthFunction, RibbonTrailColorFunction), 66);

            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
            Texture2D tex = CEUtils.getExtraTex("ExoTwinsMinion/A" + ((Main.GameUpdateCount / 4) % 3).ToString());
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver2, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }


        List<Vpoint> v1 = new List<Vpoint>();
        List<Vpoint> v2 = new List<Vpoint>();

        public void updatePoints()
        {
            for (int i = 0; i < v1.Count; i++)
            {
                float urotu = Projectile.rotation;
                float urotd = Projectile.rotation;
                Vector2 uposu = Projectile.Center + new Vector2(-4, -18).RotatedBy(Projectile.rotation) * Projectile.scale;
                Vector2 uposd = Projectile.Center + new Vector2(-4, 18).RotatedBy(Projectile.rotation) * Projectile.scale;
                if (i > 0)
                {
                    urotu = v1[i - 1].rot;
                    urotd = v2[i - 1].rot;
                    uposu = v1[i - 1].pos;
                    uposd = v2[i - 1].pos;
                }
                v1[i].pos = uposu - (uposu - v1[i].pos).SafeNormalize(-Vector2.UnitX) * 12;
                v1[i].rot = (uposu - v1[i].pos).ToRotation();
                v2[i].pos = uposd - (uposd - v2[i].pos).SafeNormalize(-Vector2.UnitX) * 12;
                v2[i].rot = (uposd - v2[i].pos).ToRotation();

                v1[i].rot = CEUtils.RotateTowardsAngle(v1[i].rot, urotu, 0.76f, false);
                v1[i].pos = uposu - v1[i].rot.ToRotationVector2() * 12 * Projectile.scale;
                v2[i].rot = CEUtils.RotateTowardsAngle(v2[i].rot, urotd, 0.76f, false);
                v2[i].pos = uposd - v2[i].rot.ToRotationVector2() * 12 * Projectile.scale;

                if (i == 0)
                {
                    v1[i].rot += (float)(Math.Cos(Main.GameUpdateCount * 0.14f) * 0.5f);
                    v2[i].rot -= (float)(Math.Cos(Main.GameUpdateCount * 0.14f) * 0.5f);
                }

            }
        }
        public override void ApplyHoming()
        {
        }
    }

    public class WarPactArtemis : EBookBaseProjectile
    {
        public float yofs => (float)(Math.Sin(Main.GameUpdateCount * 0.06f) * 62);
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }
        public override string Texture => "CalamityEntropy/Assets/Extra/white";
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public bool sp = true;
        NPC target = null;
        public override void ApplyHoming()
        {
        }
        public override void AI()
        {
            if (Projectile.owner != Main.myPlayer || BookMarkLoader.HeldingBookAndHasBookmarkEffect<WarPactBMEffect>(Projectile.GetOwner()))
            {
                Projectile.timeLeft = 4;
            }
            else
            {
                Projectile.Kill();
            }
            if (sp)
            {
                sp = false;
                for (int i = 0; i < 24; i++)
                {
                    v1.Add(new Vpoint() { pos = Projectile.Center });
                    v2.Add(new Vpoint() { pos = Projectile.Center });
                }
            }
            updatePoints();
            Projectile.penetrate = -1;
            base.AI();
            if (target == null || !target.active || target.dontTakeDamage)
            {
                target = Projectile.FindTargetWithinRange(2000);
            }
            Vector2 tpos = Projectile.GetOwner().Center;
            if (target != null)
            {
                tpos = target.Center;
            }
            tpos.X -= 400;
            tpos.Y += yofs * (target == null ? 0 : 1);
            int s = 28;
            if (CEUtils.getDistance(tpos, Projectile.Center) > s)
            {
                Projectile.velocity = (tpos - Projectile.Center).normalize() * s;
            }
            else
            {
                Projectile.velocity = (tpos - Projectile.Center);
            }
            tpos.X += 400;
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Projectile.velocity.Length() > 4 && target == null ? Projectile.velocity.ToRotation() : (tpos - Projectile.Center).ToRotation()), 20f.ToRadians(), true);
            if (target != null)
            {
                Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
            }
            if (target != null && Projectile.ai[0]++ % 60 <= 30 && Projectile.ai[0] % 9 == 0)
            {
                if (Projectile.owner == Main.myPlayer)
                    (this.ShooterModProjectile as EntropyBookHeldProjectile).ShootSingleProjectile(ModContent.ProjectileType<ArMinionLaser>(), Projectile.Center, (target.Center + target.velocity * 2 - Projectile.Center), 0.33f, 1, 1);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(6))
            {
                base.OnHitNPC(target, hit, damageDone);
            }
        }
        public float RibbonTrailWidthFunction(float completionRatio, Vector2 vertex)
        {
            float baseWidth = Utils.GetLerpValue(1f, 0.54f, completionRatio, true) * 5f;
            float endTipWidth = CEUtils.Convert01To010(Utils.GetLerpValue(0.96f, 0.89f, completionRatio, true)) * 2.4f;
            return (baseWidth + endTipWidth) * 0.6f * Projectile.scale;
        }
        public Color RibbonTrailColorFunction(float completionRatio, Vector2 vertex)
        {
            Color startingColor = new Color(34, 40, 48);
            Color endColor = new Color(219, 82, 28);
            return Color.Lerp(startingColor, endColor, (float)Math.Pow(completionRatio, 1.5D)) * Projectile.Opacity;
        }


        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 uposu = Projectile.Center + new Vector2(-4, -18).RotatedBy(Projectile.rotation) * Projectile.scale;
            Vector2 uposd = Projectile.Center + new Vector2(-4, 18).RotatedBy(Projectile.rotation) * Projectile.scale;
            List<Vector2> p1 = new List<Vector2>();
            List<Vector2> p2 = new List<Vector2>();
            p1.Add(uposu);
            p2.Add(uposd);
            for (int i = 0; i < v1.Count; i++)
            {
                p1.Add(v1[i].pos);
                p2.Add(v2[i].pos);
            }
            CEPrimitiveRenderer.RenderTrail(p1, new(RibbonTrailWidthFunction, RibbonTrailColorFunction), 66);
            CEPrimitiveRenderer.RenderTrail(p2, new(RibbonTrailWidthFunction, RibbonTrailColorFunction), 66);

            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
            Texture2D tex = CEUtils.getExtraTex("ExoTwinsMinion/B" + ((Main.GameUpdateCount / 4) % 3).ToString());
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver2, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }

        List<Vpoint> v1 = new List<Vpoint>();
        List<Vpoint> v2 = new List<Vpoint>();

        public void updatePoints()
        {
            for (int i = 0; i < v1.Count; i++)
            {
                float urotu = Projectile.rotation;
                float urotd = Projectile.rotation;
                Vector2 uposu = Projectile.Center + new Vector2(-4, -18).RotatedBy(Projectile.rotation) * Projectile.scale;
                Vector2 uposd = Projectile.Center + new Vector2(-4, 18).RotatedBy(Projectile.rotation) * Projectile.scale;
                if (i > 0)
                {
                    urotu = v1[i - 1].rot;
                    urotd = v2[i - 1].rot;
                    uposu = v1[i - 1].pos;
                    uposd = v2[i - 1].pos;
                }
                v1[i].pos = uposu - (uposu - v1[i].pos).SafeNormalize(-Vector2.UnitX) * 12;
                v1[i].rot = (uposu - v1[i].pos).ToRotation();
                v2[i].pos = uposd - (uposd - v2[i].pos).SafeNormalize(-Vector2.UnitX) * 12;
                v2[i].rot = (uposd - v2[i].pos).ToRotation();

                v1[i].rot = CEUtils.RotateTowardsAngle(v1[i].rot, urotu, 0.76f, false);
                v1[i].pos = uposu - v1[i].rot.ToRotationVector2() * 12 * Projectile.scale;
                v2[i].rot = CEUtils.RotateTowardsAngle(v2[i].rot, urotd, 0.76f, false);
                v2[i].pos = uposd - v2[i].rot.ToRotationVector2() * 12 * Projectile.scale;

                if (i == 0)
                {
                    v1[i].rot += (float)(Math.Cos(Main.GameUpdateCount * 0.14f) * 0.5f);
                    v2[i].rot -= (float)(Math.Cos(Main.GameUpdateCount * 0.14f) * 0.5f);
                }

            }
        }
    }
}
