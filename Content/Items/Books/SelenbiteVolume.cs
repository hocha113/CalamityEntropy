using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.UI.EntropyBookUI;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class SelenbiteVolume : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 40;
            Item.height = 52;
            Item.damage = 48;
            Item.useAnimation = Item.useTime = 18;
            Item.crit = 5;
            Item.mana = 15;
            Item.ArmorPenetration = 25;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(platinum: 1);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/SV")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<SelenbiteVolumeHeld>();
        public override int SlotCount => 4;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<NightEpic>()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class SelenbiteVolumeHeld : EntropyBookDrawingAlt
    {
        public override int frameChange => 2;
        public override string OpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/SelenbiteVolume/Open";
        public override string PageAnimationPath => $"{EntropyBook.BaseFolder}/Textures/SelenbiteVolume/Page";
        public override string UIOpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/SelenbiteVolume/UI";

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.Homing = 1f;
            m.HomingRange = 0.8f;
            return m;
        }
        public override bool Shoot()
        {
            int type = getShootProjectileType();
            for (int i = 0; i < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, bookItem), Projectile.GetOwner().Entropy().EBookStackItems.Count); i++)
            {
                var bm = Projectile.owner.ToPlayer().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(bm))
                {
                    int pn = BookMarkLoader.ModifyProjectile(bm, type);
                    if (pn >= 0)
                    {
                        type = pn;
                    }
                }
            }
            Player player = Projectile.GetOwner();
            for (int i = 0; i < Main.rand.Next(6, 10); i++)
            {
                ShootSingleProjectile(type, Projectile.Center, Projectile.velocity, 1, 1, Main.rand.NextFloat(0.2f, 1), MainProjectile: true);

                //PRT_GlowLightParticle lightColor spawn后赋,Configure只管Additive和lifetime
                var glow = PRTLoader.NewParticle<PRT_GlowLightParticle>(player.MountedCenter, Projectile.rotation.ToRotationVector2().RotatedByRandom(randomShootRotMax) * Main.rand.NextFloat(4, 20) * 2.5f, Color.YellowGreen, Main.rand.NextFloat(0.4f, 0.8f));
                glow.lightColor = Color.YellowGreen * 0.25f;
                glow.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
                for (int s = 0; s < 3; s++)
                {
                    var smoke = PRTLoader.NewParticle<PRT_Smoke>(player.MountedCenter, Projectile.rotation.ToRotationVector2().RotatedByRandom(randomShootRotMax) * Main.rand.NextFloat(4, 20) * 1f, Color.YellowGreen, 0.5f);
                    smoke.timeleftmax = 30;
                    smoke.Lifetime = 30;
                    smoke.scaleStart = 0.05f;
                    smoke.scaleEnd = Main.rand.NextFloat(0.36f, 0.8f) * 0.6f;
                    smoke.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 30);
                }
            }
            CEUtils.PlaySound("SporeGas", Main.rand.NextFloat(2.2f, 2.6f), Projectile.Center, volume: 0.6f);
            return true;
        }
        public override float randomShootRotMax => 0.46f;
        public override int baseProjectileType => ModContent.ProjectileType<SelenbiteStar>();
    }
    public class SelenbiteStar : EBookBaseProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = true;
            Projectile.light = 0.8f;
            Projectile.timeLeft = 400;
            Projectile.penetrate = 1;
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            CEUtils.PlaySound("LunarStar", Main.rand.NextFloat(1f, 1.3f), Projectile.Center, volume: 1);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, color, 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
        }
        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0)
            {
                Projectile.localAI[1] = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.Opacity = 0;
            }
            base.AI();
            if (Projectile.timeLeft < 25)
                Projectile.Opacity -= 1 / 25f;
            else
                if (Projectile.Opacity < 1)
                Projectile.Opacity += 1 / 24f;
            Projectile.velocity *= 0.97f;
        }
        public override Color baseColor => Color.YellowGreen;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = CEExtraAssets.StarTexture;
            float num = 0.5f * (float)Math.Sin(Projectile.localAI[1] + Main.GameUpdateCount / 10f);
            float num2 = 0.5f * (float)Math.Sin(Projectile.localAI[1] + Main.GameUpdateCount / 10f + MathHelper.PiOver4);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, color * Projectile.Opacity, 0, tex.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.24f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, color * Projectile.Opacity, 0, tex.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.24f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, tex.Size() / 2f, new Vector2(1 + num, 1 - num) * Projectile.scale * 0.16f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, color * Projectile.Opacity, MathHelper.PiOver4, tex.Size() / 2f, new Vector2(1 - num2, 1 + num2) * Projectile.scale * 0.16f, SpriteEffects.None, 0);


            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
