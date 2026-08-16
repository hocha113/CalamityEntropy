using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityMod.Items;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class IlmeranAsylum : ModItem
    {
        public static float DMGMult = 0.10f;
        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 52;
            Item.value = CalamityGlobalItem.RarityRedBuyPrice;
            Item.rare = ModContent.RarityType<SkyBlue>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().ilmeranAsylum = true;
        }

        public override void AddRecipes()
        {
        }
    }
    public class IlmeranVortex : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.width = Projectile.height = 56;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.timeLeft = 3;
            Projectile.rotation += 0.24f;
            float mc = 0;
            float max = 0;
            bool j = true;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (!p.friendly)
                    continue;
                if (p.type == Projectile.type && p.owner == Projectile.owner)
                {
                    max++;
                    if (p.whoAmI != Projectile.whoAmI)
                    {
                        if (j)
                        {
                            mc++;
                        }
                    }
                    else { j = false; }
                }
                else
                {
                    if (p.damage > 0 && p.owner == Projectile.owner && p.Hitbox.Intersects(Projectile.Hitbox))
                    {
                        if (!p.Entropy().IlmeranEnhanced)
                        {
                            if (Projectile.ai[0] < 1)
                            {
                                Projectile.ai[0] += 0.1f;
                            }
                            p.Entropy().IlmeranEnhanced = true;
                            var target = CEUtils.FindTarget_HomingProj(p, p.Center, 6000);
                            if (target != null)
                            {
                                p.velocity = new Vector2(p.velocity.Length(), 0).RotatedBy((target.Center - p.Center).ToRotation());
                            }
                            EParticle.NewParticle(new HadCircle2() { CScale = 0.4f }, Projectile.Center, p.velocity.normalize() * 16, Color.SkyBlue, 0.4f, 1, true, BlendState.Additive, 0);
                            CEUtils.SyncProj(Projectile.whoAmI);
                            CEUtils.SyncProj(p.whoAmI);
                            CEUtils.PlaySound("charm", Main.rand.NextFloat(0.6f, 1.4f), Projectile.Center, volume: 0.4f);
                            Projectile.Resize((int)(56 * Projectile.scale), (int)(56 * Projectile.scale));
                        }
                    }
                }
            }
            var targetz = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 6000);
            if (Projectile.ai[0] >= 1 && targetz != null)
            {
                Vector2 targetPos = targetz.Center;
                Projectile.velocity += (targetPos - Projectile.Center).normalize() * 1;
                Projectile.velocity *= 0.98f;
            }
            else
            {
                Vector2 targetPos = Projectile.GetOwner().Center + (mc * (MathHelper.TwoPi / max)).ToRotationVector2().RotatedBy(Main.GameUpdateCount * 0.03f) * 320;
                Projectile.velocity += (targetPos - Projectile.Center).normalize() * 3.6f;
                Projectile.velocity *= 0.94f;
                if (CEUtils.getDistance(Projectile.Center, targetPos) > 1600)
                {
                    Projectile.Center = targetPos;
                }
            }
            if (!Projectile.GetOwner().Entropy().ilmeranAsylum)
            {
                Projectile.Kill();
            }
            Projectile.scale = 1 + Projectile.ai[0];
        }
        public override void OnKill(int timeLeft)
        {
            EParticle.NewParticle(new HadCircle2() { CScale = 0.8f }, Projectile.Center, Vector2.Zero, Color.SkyBlue, 0.4f, 1, true, BlendState.Additive, 0);
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[0] < 1)
            {
                return false;
            }
            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Effect effect = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/Vortex", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(16);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEUtils.getExtraTex("VoronoiShapes"), Projectile.Center - Main.screenPosition, null, new Color(220, 220, 255), Main.GlobalTimeWrappedHourly * 2, CEUtils.getExtraTex("VoronoiShapes").Size() / 2f, 0.2f * Projectile.scale, SpriteEffects.None, 0);
            CEUtils.DrawGlow(Projectile.Center, Color.White * 0.7f, 0.9f);
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));

            return false;
        }
    }
}
