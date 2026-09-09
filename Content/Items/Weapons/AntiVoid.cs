using CalamityEntropy.Common;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AntiVoid : ModItem, IDevItem
    {
        public string DevName => "ChaLost";

        public override void SetStaticDefaults()
        {

            ItemID.Sets.AnimatesAsSoul[Type] = true;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 5));
        }
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 144;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 8;
            Item.useAnimation = 16;
            Item.autoReuse = true;
            Item.scale = 2f;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 325;
            Item.knockBack = 6;
            Item.crit = 44;
            Item.shoot = ModContent.ProjectileType<AntivoidSlash>();
            Item.shootSpeed = 12;
            Item.value = Item.buyPrice(0, 60);
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
        public override bool AltFunctionUse(Player player)
        {
            return !player.HasCooldown(AntivoidDashCooldown.ID);
        }
        public override bool CanShoot(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useTime = 20;
                Item.useAnimation = 20;
            }
            else
            {
                Item.useTime = 8;
                Item.useAnimation = 16;
            }
            return base.CanShoot(player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<AntivoidDash>();
                damage *= 4;
                player.AddCooldown(AntivoidDashCooldown.ID, 15 * 60);
                player.RemoveAllGrapplingHooks();
                if (player.mount.Active)
                    player.mount.Dismount(player);
            }
            else
            {
                velocity = CEUtils.randomRot().ToRotationVector2() * velocity.Length();
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, Main.rand.NextBool() ? -1 : 1);
            return false;
        }


        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<VoidBlade>()
                .AddCalOrOwn(CEID.Item_TwistingNether, ModContent.ItemType<WraithSoulEssence>(), 4)
                .AddCalOrOwn(CEID.Item_RuinousSoul, ModContent.ItemType<NihilityFragments>(), 4)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class AntivoidSlash : ModProjectile
    {
        //绘制用贴图与着色器,加载期由 VaultLoaden 统一赋值,只在客户端绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/RuneRibbon")]
        internal static Asset<Texture2D> RuneRibbonTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/MotionTrail3")]
        internal static Asset<Texture2D> MotionTrail3Tex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/Extra_202")]
        internal static Asset<Texture2D> Extra202Tex;
        [VaultLoaden("CalamityEntropy/Assets/Effects/AntivoidTrail", AssetMode.EffectValue, "EffectPass")]
        internal static Effect AntivoidTrailShader;
        List<float> odr = new List<float>();
        List<float> odl = new List<float>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.MaxUpdates = 12;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.SetShake(target.Center, 6, 3000);
            CEUtils.PlaySound("antivoidhit", Main.rand.NextFloat(0.8f, 1.2f), target.Center);
            Color impactColor = Color.LightBlue;
            float impactParticleScale = Main.rand.NextFloat(1.4f, 1.6f);

            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, impactParticleScale).Configure(Color.Blue, 8, 0, 2.5f);


            float sparkCount = 32;
            for (int i = 0; i < sparkCount; i++)
            {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 34 * (2 - p));
                int sparkLifetime2 = (int)((2 - p) * 7);
                float sparkScale2 = 0.6f + (1 - p);
                Color sparkColor2 = Color.Lerp(Color.DeepSkyBlue, Color.Purple, p);
                if (Main.rand.NextBool())
                {
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                }
                else
                {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Main.rand.NextBool() ? Color.Red : Color.Firebrick, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                }
            }
        }
        public float RotF = 0;
        public float rc = 0;
        public float yc = 0;
        public override void AI()
        {
            if (yc == 0)
            {
                yc = Main.rand.NextFloat(0.3f, 0.8f);
            }
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = 14 * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init)
            {
                CEUtils.PlaySound("antivoiduse", Main.rand.NextFloat(0.7f, 1.4f), Projectile.Center, 36);
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;
            }
            Projectile.timeLeft = 3;
            if (RotF == 0)
            {
                RotF = Main.rand.NextFloat(MathHelper.ToRadians(340), MathHelper.ToRadians(400));
                rc = Main.rand.NextFloat(0.2f, 0.4f);
            }
            alpha = 1;
            scale = 1f;
            float cr = MathHelper.ToRadians(140);
            Vector2 jw = ((RotF * -rc + CEUtils.Parabola(progress * 0.5f, RotF))).ToRotationVector2() * new Vector2(1, yc);
            lg = (jw).Length();

            Projectile.rotation = Projectile.velocity.ToRotation() + jw.ToRotation() * Projectile.ai[0];

            Projectile.Center = Projectile.GetOwner().MountedCenter;

            if (Projectile.velocity.X > 0)
            {
                owner.direction = 1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            else
            {
                owner.direction = -1;
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
            }
            if (counter > MaxUpdateTimes)
            {
                Projectile.Kill();
            }
            odr.Add(Projectile.rotation);
            odl.Add(lg);
            if (odr.Count > 110)
            {
                odl.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public float lg = 0;
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public Texture2D tex => Projectile.GetTexture();
        public float rofs = 0f;
        public override bool PreDraw(ref Color lightColor)
        {
            rofs += 0.01f;
            Texture2D trail = RuneRibbonTex.Value;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = 14 * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            float ofs = 0;
            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(255, 255, 255);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(170 * Projectile.scale * odl[i], 0).RotatedBy(odr[i])),
                      new Vector3(ofs, 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(Projectile.scale * odl[i], 0).RotatedBy(odr[i])),
                      new Vector3(ofs, 0, 1),
                      b));
                if (i < odr.Count - 1)
                {
                    ofs += 1f / odr.Count;
                }
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = AntivoidTrailShader;
                sb.End();
                shader.Parameters["alpha"].SetValue(1f - progress);
                shader.Parameters["offset"].SetValue(rofs);
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();
                gd.Textures[1] = MotionTrail3Tex.Value;
                gd.Textures[0] = trail;
                gd.Textures[2] = Extra202Tex.Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }


            int dir = (int)(Projectile.ai[0]);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.ToRadians(70 - 24) : Projectile.rotation + +MathHelper.ToRadians(110 + 24);


            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha * (1 - progress), rot, origin, (Projectile.scale) * scale * lg, effect);

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160 * lg) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160 * lg) * Projectile.scale * scale, 54, DelegateMethods.CutTiles);
        }
    }

    public class AntivoidDash : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public PRT_AntivoidTrail trail;
        public PRT_StarTrailParticle trail2;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, true, -1);
            Projectile.width = Projectile.height = 16;
            Projectile.MaxUpdates = 4;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 0)
            {
                CEUtils.PlaySound("AntivoidDash", 1, Projectile.Center);
            }
            var player = Projectile.GetOwner();
            player.Entropy().immune = 5;
            if (trail == null)
            {
                //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                trail = PRTLoader.NewParticle<PRT_AntivoidTrail>(Projectile.Center, Vector2.Zero, new Color(40, 10, 80, 255), 1f);
                trail.Configure(1, true, PRTDrawModeEnum.NonPremultiplied);
                trail2 = PRTLoader.NewParticle<PRT_StarTrailParticle>(Projectile.Center, Vector2.UnitX * 0.1f, new Color(255, 20, 20, 255), 1f);
                trail2.addPoint = false;
                trail2.maxLength = 60;
                trail2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
            }
            trail.AddPoint(Projectile.Center + Projectile.velocity);
            trail2.Position = (Projectile.Center + new Vector2(0, -10));
            trail2.AddPoint(trail2.Position);
            trail.Lifetime = trail2.Lifetime = 30;
            if (Projectile.ai[1]++ > 20)
            {
                Projectile.velocity *= 0.98f;
            }
            player.Center = Projectile.Center;
            player.velocity = Projectile.velocity;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[2]++ == 0)
            {
                CEUtils.PlaySound("AntivoidDashHit", 1, target.Center);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<AntivoidMark>(), Projectile.damage * 3, 0, Projectile.owner, target.whoAmI);
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.GetOwner().Center -= Projectile.velocity;
            return base.OnTileCollide(oldVelocity);
        }
    }

    public class AntivoidMark : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.light = 1;
            Projectile.timeLeft = 42;
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[1] < 39)
                return false;
            return null;
        }
        public override void AI()
        {
            Projectile.Center = ((int)Projectile.ai[0]).ToNPC().Center;
            Projectile.ai[1]++;
            if (Projectile.ai[1] == 24)
            {
                CEUtils.PlaySound("AntivoidDashSlash", 1, Projectile.Center);
            }
            if (Projectile.ai[1] == 39)
            {
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                var line1 = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, new Color(30, 10, 50), 1f);
                line1.xadd = 2.4f;
                line1.lx = 3.6f;
                line1.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0, 30);
                var line2 = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, new Color(80, 40, 120), 1f);
                line2.xadd = 2f;
                line2.lx = 3f;
                line2.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, 0, 30);
                var line3 = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center, Vector2.Zero, Color.LightBlue, 1);
                line3.xadd = 2f;
                line3.lx = 2.8f;
                line3.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] > 40)
            {
                return false;
            }
            Texture2D tex = Projectile.GetTexture();
            Vector2 drawPos = Projectile.Center + CEUtils.Parabola((Projectile.ai[1] / 40f), 180) * -Vector2.UnitY;
            float p = CEUtils.Parabola((Projectile.ai[1] * 0.5f) / 40f, 1);
            float s = 1 + (1 - p) * 2;
            float alpha = p;
            Vector2 scale = new Vector2(Math.Abs(s * (float)Math.Cos(Main.GameUpdateCount * 0.25f)), s);

            Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, null, Color.White * alpha, 0, tex.Size() / 2f, scale, SpriteEffects.None);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center + new Vector2(-240, 0), Projectile.Center + new Vector2(240, 0), targetHitbox);
        }
    }
}
