using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
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
    public class MoonlightSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = ModContent.GetInstance<MeleeDamageClass>();
            Item.width = 48;
            Item.height = 60;
            Item.useTime = 32;
            Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 5;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MoonlightSwordHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public int atkType = 1;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType);
            atkType *= -1;
            return false;
        }

        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.HellstoneBar, 16)
                .AddCalOrOwn(CEID.Item_AerialiteBar, ItemID.MeteoriteBar, 8)
                .AddIngredient(ItemID.Obsidian, 10)
                .AddIngredient(ItemID.StoneBlock, 40)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class MoonlightSwordHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/MoonlightSword";
        List<float> odr = new List<float>();
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
            Projectile.MaxUpdates = 14;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public int stopTime = -1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (stopTime < 0)
            {
                stopTime = 6 * Projectile.MaxUpdates;
                ScreenShaker.AddShake(new ScreenShaker.NoDirQuickShake(8));
            }
            CEUtils.PlaySound("truemoonlighthit", Main.rand.NextFloat(0.7f, 1.3f), target.Center, volume: CEUtils.WeapSound);
        }
        public override void AI()
        {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            if (stopTime <= 0)
            {
                counter++;
            }
            if (init)
            {
                CEUtils.PlaySound("moonlightswordattack" + Main.rand.Next(2), 1 + Projectile.ai[0] * 0.08f, Projectile.Center);
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;

                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity * 2, ModContent.ProjectileType<MoonlightShoot>(), (int)(Projectile.damage * 1.3f), Projectile.knockBack / 2, Projectile.owner);
                }
            }
            odr.Add(Projectile.rotation);
            Projectile.timeLeft = 3;
            float RotF = 3.2f;
            alpha = 1;
            scale = 1f;
            float cr = MathHelper.ToRadians(90);
            if (progress <= 0.5f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + CEUtils.Parabola(progress, RotF + cr)) * Projectile.ai[0];
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * 0.5f + cr - CEUtils.GetRepeatedCosFromZeroToOne(2 * (progress - 0.5f), 1) * cr) * Projectile.ai[0];
            }
            Projectile.Center = Projectile.GetOwner().MountedCenter;


            if (odr.Count > 2600)
            {
                odr.RemoveAt(0);
            }
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
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;

            if (counter > MaxUpdateTimes)
            {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }

            if (stopTime > 0)
            {
                stopTime--;
            }
            else
            {
                odr.Add(Projectile.rotation);
                if (odr.Count > 16)
                {
                    odr.RemoveAt(0);
                }
            }
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D trail = CEExtraAssets.MotionTrail2;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(220, 200, 255);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(175 * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition,
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CEEffectAssets.SwordTrail;
                sb.End();
                //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(200, 255, 200)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(100, 160, 100)).ToVector4());
                shader.Parameters["alpha"].SetValue(float.Max(0, (0.5f - progress) / 0.5f));
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }


            int dir = (int)(Projectile.ai[0]);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale, effect);

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, targetHitbox, 20);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (160 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale * scale, 54, DelegateMethods.CutTiles);
        }
    }
    public class MoonlightShoot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        //斩击贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/GSlash")]
        internal static Asset<Texture2D> GSlashTex;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("HammerShoot" + Main.rand.Next(1, 4), Main.rand.NextFloat(1f, 1.4f), Projectile.Center);
            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            var line1 = PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, new Color(220, 255, 220), 1f);
            line1.xadd = 0.8f;
            line1.lx = 1.4f;
            line1.spawnColor = new Color(212, 255, 212);
            line1.endColor = Color.DarkSeaGreen;
            line1.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
            var line2 = PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, new Color(220, 255, 220), 1f);
            line2.xadd = 0.8f;
            line2.lx = 1.4f;
            line2.spawnColor = new Color(212, 255, 212);
            line2.endColor = Color.DarkSeaGreen;
            line2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 260;
            Projectile.height = 260;
            Projectile.MaxUpdates = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.timeLeft < 20)
                return false;
            return base.CanHitNPC(target);
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = float.Min(1, Projectile.timeLeft / 16f);
            Projectile.velocity *= 0.95f;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= Projectile.timeLeft / 80f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color clr = Color.Lerp(Color.LightSeaGreen, new Color(230, 255, 230), Projectile.timeLeft / 80f);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D tex = GSlashTex.Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, clr * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f + new Vector2(70, 0), Projectile.scale * 0.9f * new Vector2(1f, Projectile.timeLeft / 80f * 1.2f), SpriteEffects.None);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, clr * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f + new Vector2(80, 0), Projectile.scale * 0.9f * new Vector2(1.8f, Projectile.timeLeft / 80f * 1.2f), SpriteEffects.None);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, clr * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f + new Vector2(80, 0), Projectile.scale * 0.9f * new Vector2(1.8f, Projectile.timeLeft / 80f * 1.2f), SpriteEffects.None);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, clr * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f + new Vector2(70, 0), Projectile.scale * 0.9f * new Vector2(0.8f, Projectile.timeLeft / 80f * 1.1f), SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

}
