using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureRailgun : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 82;
            Item.height = 32;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 14;
            Item.value = Item.buyPrice(0, 5);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AzafureRailgunHeld>();
            Item.shootSpeed = 6;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_SeaPrism))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_SeaPrism, 8)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.Minishark)
                .AddIngredient<HellIndustrialComponents>(5)
                .AddTile(TileID.Anvils)
                .Register();
        }

    }
    public class AzafureRailgunHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzafureRailgun";
        //蓄力指示线贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Particles/CrLine")]
        internal static Asset<Texture2D> CrLineTex;
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void SetDefaults()
        {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public float Charge = 0;
        public LoopSound chargeSnd = null;
        public Vector2 FirePos => Projectile.Center + new Vector2(23, 4 * Math.Sign(Projectile.velocity.X)).RotatedBy(Projectile.rotation);
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if (!Main.dedServ)
            {
                if (chargeSnd == null)
                {
                    chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                    chargeSnd.instance.Pitch = 0;
                    chargeSnd.instance.Volume = 0;
                    chargeSnd.play();
                }
                chargeSnd.setVolume_Dist(Projectile.Center, 100, 700, Charge * 0.7f);
                chargeSnd.instance.Pitch = Charge * 0.15f;
                if (Charge < 1)
                {
                    chargeSnd.timeleft = 3;
                }
            }
            if (player.channel)
            {
                if (Charge < 1)
                {
                    Charge += 0.01f;
                    if (Charge >= 1f)
                    {
                        Charge = 1;
                        PRTLoader.NewParticle<PRT_PulseRing>(FirePos, player.velocity, Color.Firebrick, 0.1f).Configure(0.6f, 10);
                    }
                }
                Projectile.timeLeft = 3;
                player.Entropy().MouseWorldListener = true;
                Projectile.rotation = (player.Entropy().MouseWorld - player.Center).ToRotation();
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
                player.SetHandRot(((player.Entropy().MouseWorld - player.Center).ToRotation().ToRotationVector2() + new Vector2(0, 1f)).ToRotation());
                player.itemAnimation = player.itemTime = 4;
                player.heldProj = Projectile.whoAmI;
            }
            else
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    if (Charge >= 1)
                    {
                        CEUtils.PlaySound("railgunShoot", 1, FirePos);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), FirePos, Projectile.velocity * 0.32f, ModContent.ProjectileType<RailgunChargeShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                    else
                    {
                        CEUtils.PlaySound("shockBlast", 1.5f - 0.5f * Charge, FirePos, volume: Charge);
                        int bulletCounts = 1 + (int)((Charge + player.AzafureDurability() * 0.4f) * 9);
                        for (int i = 0; i < bulletCounts; i++)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), FirePos, Projectile.velocity.RotatedByRandom((1 - Charge)) * Main.rand.NextFloat(0.6f, 1) * 2.6f * (0.3f + 0.7f * Charge), ModContent.ProjectileType<RailgunSmallShot>(), (int)(Charge * Projectile.damage / bulletCounts), Projectile.knockBack / 10, Projectile.owner);
                        }
                    }
                }
                player.velocity += Projectile.velocity * -0.4f * Charge;
                Projectile.Kill();
            }
            Projectile.light = Charge * 0.6f;
            Projectile.Center = player.GetDrawCenter() + new Vector2(0, -4);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D t = Projectile.GetTexture();
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 16, null, lightColor, Projectile.rotation, t.Size() / 2f, Projectile.scale, (Projectile.velocity.X > 0) ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Vector2 pos = FirePos;
            float size = Charge;
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(250, 250, 250), Projectile.rotation, tex.Size() * 0.5f, size * 0.25f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 80, 80), Projectile.rotation, tex.Size() * 0.5f, size * 0.4f, SpriteEffects.None, 0);
            Texture2D line = CrLineTex.Value;
            float offset = (1.14f - Charge) * 56;
            //Main.spriteBatch.End();
            //GraphicsDevice gdv = Main.graphics.GraphicsDevice;
            //EffectLoader.PreparePixelShader(gdv);
            //Main.spriteBatch.End();
            //Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            float Mxl = 1;
            for (float i = 0; i <= 1; i += 0.0025f)
            {
                Mxl = i;
                Vector2 tpos = FirePos + Projectile.rotation.ToRotationVector2() * i * 800;
                if (!CEUtils.isAir(tpos))
                {
                    break;
                }
            }
            if (Mxl > Charge)
            {
                Mxl = Charge;
            }
            Main.spriteBatch.Draw(line, FirePos - Main.screenPosition + new Vector2(0, offset).RotatedBy(Projectile.rotation), null, (Charge >= 1 ? Color.OrangeRed : Color.Firebrick) * Charge, Projectile.rotation, new Vector2(0, 10), new Vector2(0.14f * Mxl, 0.4f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(line, FirePos - Main.screenPosition - new Vector2(0, offset).RotatedBy(Projectile.rotation), null, (Charge >= 1 ? Color.OrangeRed : Color.Firebrick) * Charge, Projectile.rotation, new Vector2(0, 10), new Vector2(0.14f * Mxl, 0.4f), SpriteEffects.None, 0);

            Main.spriteBatch.End();
            //EffectLoader.ApplyPixelShader(gdv);
            Main.spriteBatch.begin_();
            return false;
        }
    }
    public class RailgunChargeShot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.light = 0.4f;
            Projectile.timeLeft = 700;
            Projectile.penetrate = 6;
            Projectile.friendly = true;
            Projectile.MaxUpdates = 32;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[1]++ % (int)((1 - Projectile.GetOwner().AzafureDurability()) * 8 + 16) == 0)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(MathHelper.PiOver2) * 0.6f, ModContent.ProjectileType<RailgunSmallShot>(), Projectile.damage / 8, Projectile.knockBack / 10, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedBy(-MathHelper.PiOver2) * 0.6f, ModContent.ProjectileType<RailgunSmallShot>(), Projectile.damage / 8, Projectile.knockBack / 10, Projectile.owner);
            }
            if (trail == null)
            {
                //双PRT_TrailParticle叠色,maxLength Configure前先赋
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.Red, 1.6f);
                trail.maxLength = 1200;
                trail.Lifetime = 20;
                trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
                trail2 = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.7f);
                trail2.maxLength = 1200;
                trail2.Lifetime = 20;
                trail2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);

            }
            trail.Lifetime = 20;
            trail2.Lifetime = 20;
            trail.AddPoint(Projectile.Center + Projectile.velocity);
            trail2.AddPoint(Projectile.Center + Projectile.velocity);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scale = 1 * Projectile.scale;
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            CEUtils.PlaySound("pulseBlast", 0.8f, Projectile.Center);
            //PulseRing+双Shine,PRTDrawMode/lifetime走Configure
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(0.6f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 1.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage, 128, Projectile.DamageType);
        }

        public void DrawEnergyBall(Vector2 pos, float size, float alpha)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(250, 250, 250) * alpha, Projectile.rotation, tex.Size() * 0.5f, new Vector2(1 + (Projectile.velocity.Length() * 0.4f), 1) * size * 0.18f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 80, 80) * alpha, Projectile.rotation, tex.Size() * 0.5f, new Vector2(1 + (Projectile.velocity.Length() * 0.4f), 1) * size * 0.32f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
        public PRT_TrailParticle trail = null;
        public PRT_TrailParticle trail2 = null;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(300);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(0.4f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 1.2f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.7f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);

            CEUtils.PlaySound("pulseBlast", 1f, Projectile.Center, 6, 0.7f);
        }
    }
    public class RailgunSmallShot : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
        }
        public PRT_TrailParticle trail = null;

        public override void AI()
        {
            if (Projectile.damage < 1)
                Projectile.damage = 1;
            Projectile.velocity *= 0.98f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 600);
            if (target != null)
            {
                Projectile.velocity *= 0.92f;
                Projectile.velocity += (target.Center - Projectile.Center).normalize() * 2f;
            }
            if (trail == null)
            {
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.8f);
                trail.maxLength = 32;
                trail.Lifetime = 12;
                trail.ShouldDraw = false;
                trail.Configure(1f, true, PRTDrawModeEnum.AdditiveBlend);
            }
            trail.Lifetime = 12;
            trail.AddPoint(Projectile.Center + Projectile.velocity);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            trail?.DrawTrail(Main.spriteBatch);
            float scale = 0.4f * Projectile.scale;
            DrawEnergyBall(Projectile.Center, scale, Projectile.Opacity);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.24f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.06f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);

        }

        public void DrawEnergyBall(Vector2 pos, float size, float alpha)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Texture2D tex = CEExtraAssets.a_circle;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(250, 250, 250) * alpha, Projectile.rotation, tex.Size() * 0.5f, size * 0.18f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 80, 80) * alpha, Projectile.rotation, tex.Size() * 0.5f, size * 0.32f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.begin_();
        }
    }
}
