using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class CosmicBlessing : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.shootSpeed = 18;
            Item.width = 28;
            Item.height = 40;
            Item.damage = 110;
            Item.useAnimation = Item.useTime = 8;
            Item.crit = 10;
            Item.mana = 12;
            Item.ArmorPenetration = 32;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.value = Item.buyPrice(platinum: 2);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/CB")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<CosmicBlessingHeld>();
        public override int SlotCount => 4;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SelenbiteVolume>()
                .AddIngredient<NihilityFragments>(10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class CosmicBlessingHeld : EntropyBookDrawingAlt
    {
        public override int frameChange => 2;
        public override string OpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/CosmicBlessing/Open";
        public override string PageAnimationPath => $"{EntropyBook.BaseFolder}/Textures/CosmicBlessing/Page";
        public override string UIOpenAnimationPath => $"{EntropyBook.BaseFolder}/Textures/CosmicBlessing/Open";
        public override int UIOpenAnmCount => 4;

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            return m;
        }
        public override void SetPosision()
        {
            Projectile.Center = Projectile.GetOwner().GetDrawCenter() + (UIOpen ? Vector2.Zero : new Vector2(heldOffset.X, heldOffset.Y * (Projectile.velocity.X > 0 ? 1 : -1)).RotatedBy(Projectile.rotation));
        }
        public override Texture2D getTexture()
        {
            if (openAnim < OpenAnmCount - 1 || UIOpen)
            {
                return OpenAnimations()[0];
            }
            else
            {
                return PageAnimations()[0];
            }
        }
        public override Rectangle GetFrame()
        {
            Rectangle frame = new Rectangle();
            Rectangle GetRect(Texture2D tex, int frame, int totalFrame)
            {
                int h = tex.Height / totalFrame;
                return new Rectangle(0, h * frame, tex.Width, h - 2);
            }
            Texture2D tex = null;
            switch (Style)
            {
                case 0: tex = OpenAnimations()[0]; break;
                case 1: tex = PageAnimations()[0]; break;
                case 2: tex = UIOpenAnimations()[0]; break;
                default: break;
            }
            switch (Style)
            {
                case 0: frame = GetRect(tex, OpenAnmCount - 1 - openAnim, OpenAnmCount); break;
                case 1: frame = GetRect(tex, pageTurnAnm, PageAnmCount); break;
                case 2: frame = GetRect(tex, UIOpenAnmCount - 1 - UIOpenAnm, UIOpenAnmCount); break;
                default: break;
            }
            return frame;
        }
        public bool purple = false;
        public override void ShootSingleProjectile(int type, Vector2 pos, Vector2 velocity, float damageMul = 1, float scaleMul = 1, float shotSpeedMul = 1, Action<Projectile> initAction = null, float randomRotMult = 1, bool MainProjectile = false, Color colorMult = default, int fixedBaseDamage = -1)
        {
            base.ShootSingleProjectile(type, pos, velocity, damageMul, scaleMul, shotSpeedMul, initAction, randomRotMult, MainProjectile, purple ? new Color(255, 200, 255) : Color.White, fixedBaseDamage);
        }
        public override bool Shoot()
        {
            Vector2 oPos = Projectile.Center;
            float oRot = Projectile.rotation;
            Vector2 odv = Projectile.velocity;

            Projectile.rotation = Projectile.rotation + (float)(Math.Sin(Main.GameUpdateCount * 0.08f)) * 1.2f;// + Main.rand.NextFloat(-1.2f, 1.2f);
            Projectile.Center -= Projectile.rotation.ToRotationVector2() * 100;
            Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
            Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
            purple = false;
            SoundEngine.PlaySound(SoundID.Item12 with { Pitch = 0.4f, Volume = 0.45f }, Projectile.Center);
            base.Shoot();
            ProduceWarpCrossDust(Projectile.Center);

            Projectile.velocity = odv;
            Projectile.Center = oPos;
            Projectile.rotation = oRot;

            Projectile.rotation = Projectile.rotation + (float)(Math.Sin(Main.GameUpdateCount * 0.08f)) * -1.2f;// + Main.rand.NextFloat(-1.2f, 1.2f);
            Projectile.Center -= Projectile.rotation.ToRotationVector2() * 100;
            Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
            Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
            purple = true;
            base.Shoot();
            ProduceWarpCrossDust(Projectile.Center);
            purple = false;
            Projectile.velocity = odv;
            Projectile.Center = oPos;
            Projectile.rotation = oRot;
            return true;
        }
        public override EBookProjectileEffect getEffect()
        {
            return new CosmicBlessingEffect();
        }
        public static void ProduceWarpCrossDust(Vector2 dustPos, float scale = 1)
        {
            float speedMultiplier = 1;
            int dustID = ModContent.DustType<SquashDust>();
            Color color = Color.Cyan;
            if (speedMultiplier > 0.8f)
            {
                for (int i = 0; i < 2; ++i)
                {
                    //CustomPulse叠黑色实心圆+BloomRing,CalamityPorts Configure签名跟Calamity原构造对齐
                    //黑圆走 AlphaBlend 必须用带真实 alpha 的 BasicCircle(原灾厄也是这张);BloomCircle 是黑底不透明强度图,AlphaBlend 下会画成黑方块
                    PRTLoader.NewParticle<PRT_CustomPulse>(dustPos, Vector2.Zero, Color.Black, 0.4f * speedMultiplier * scale).Configure("CalamityEntropy/Assets/Particles/BasicCircle", Vector2.One, 0, 0.4f * speedMultiplier * scale, 0.05f * speedMultiplier * scale, 12, PRTDrawModeEnum.AlphaBlend);
                    PRTLoader.NewParticle<PRT_CustomPulse>(dustPos, Vector2.Zero, color, 0.15f * (1 + i * 0.2f) * speedMultiplier * scale).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, 0, 0.15f * (1 + i * 0.2f) * speedMultiplier * scale, 0.025f * (1 + i * 0.2f) * speedMultiplier * scale, 12);
                }
            }
            else
            {
                PRTLoader.NewParticle<PRT_CustomSpark>(dustPos, Vector2.Zero, color, 0.3f * scale * speedMultiplier).Configure("CalamityEntropy/Assets/Particles/BloomCircle", false, 10, new Vector2(1f, 1f), true, false, 0f, false, false);
            }


            for (int i = 0; i < 5; ++i)
            {
                float speed = Main.rand.NextFloat(3f, 6f);
                Vector2 dustVel = Vector2.UnitX * speed * speedMultiplier * 1.5f;
                Dust d = Dust.NewDustDirect(dustPos, 0, 0, dustID);
                d.position = dustPos;
                d.velocity = dustVel * scale;
                d.noGravity = true;
                d.scale *= Main.rand.NextFloat(1.8f, 2.2f) * (1 - speed / 7) * scale;
                d.color = color;
                d.fadeIn = 1;
                Dust.CloneDust(d).velocity = dustVel.RotatedBy(MathHelper.PiOver2);
                Dust.CloneDust(d).velocity = dustVel.RotatedBy(MathHelper.Pi);
                Dust.CloneDust(d).velocity = dustVel.RotatedBy(-MathHelper.PiOver2);
            }
        }
        public override void AI()
        {
            base.AI();
            PortalAlpha = float.Lerp(PortalAlpha, UIOpen ? 1 : 0, 0.1f);
        }
        public float PortalAlpha;

        public void DrawPortal()
        {
            float alpha = PortalAlpha;
            var Position = Projectile.Center;
            float Scale = Projectile.scale;
            DrawVortex(Position, Color.Blue * alpha, Scale);
            DrawVortex(Position, Color.White * alpha, Scale * 0.7f);
        }
        public void DrawVortex(Vector2 pos, Color color, float Size = 1, float glow = 1f)
        {
            Main.spriteBatch.End();
            Effect effect = CEEffectAssets.Vortex;
            effect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
            effect.Parameters["Strength"].SetValue(22);
            effect.Parameters["AspectRatio"].SetValue(1);
            effect.Parameters["TexOffset"].SetValue(new Vector2(Main.GlobalTimeWrappedHourly * 0.1f, -Main.GlobalTimeWrappedHourly * 0.07f));
            float fadeOutDistance = 0.06f;
            float fadeOutWidth = 0.3f;
            effect.Parameters["FadeOutDistance"].SetValue(fadeOutDistance);
            effect.Parameters["FadeOutWidth"].SetValue(fadeOutWidth);
            effect.Parameters["enhanceLightAlpha"].SetValue(0.8f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(CEExtraAssets.VoronoiShapes, pos - Main.screenPosition, null, color, Main.GlobalTimeWrappedHourly * 12, CEExtraAssets.VoronoiShapes.Size() / 2f, 0.2f * Size, SpriteEffects.None, 0);
            CEUtils.DrawGlow(pos, Color.White * 0.4f * glow * (color.A / 255f), 0.8f * Size * glow);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        }
        public override void playPageSound()
        {
        }
        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            DrawPortal();
            return false;
        }
        public override float randomShootRotMax => 0f;
        public override int baseProjectileType => ModContent.ProjectileType<CosmicDeathRay>();
    }
    public class CosmicBlessingEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff<GodSlayerInferno>(300);
        }
    }

    public class CosmicDeathRay : EBookBaseProjectile
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.tileCollide = false;
            Projectile.MaxUpdates = 5;
            Projectile.timeLeft = 100 * 5;
            Projectile.penetrate = 4;
        }
        public override void OnKill(int timeLeft)
        {
            Vector2 dustPos = Projectile.Center;
            float scale = 2;
            for (int i = 0; i < 2; ++i)
            {
                //同上,AlphaBlend 黑圆用 BasicCircle
                PRTLoader.NewParticle<PRT_CustomPulse>(dustPos, Vector2.Zero, Color.Black, 0.4f * scale).Configure("CalamityEntropy/Assets/Particles/BasicCircle", Vector2.One, 0, 0.4f * scale, 0.05f * scale, 16, PRTDrawModeEnum.AlphaBlend);
                PRTLoader.NewParticle<PRT_CustomPulse>(dustPos, Vector2.Zero, color, 0.15f * (1 + i * 0.2f) * scale).Configure("CalamityEntropy/Assets/Particles/BloomRing", Vector2.One, 0, 0.15f * (1 + i * 0.2f) * scale, 0.025f * (1 + i * 0.2f) * scale, 16);
            }
        }
        public List<Vector2> OldPos = new();
        public List<float> OldRot = new();
        public override void AI()
        {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();
            OldPos.Add(Projectile.Center);
            OldRot.Add(Projectile.rotation);
            if (OldPos.Count > 32)
            {
                OldRot.RemoveAt(0);
                OldPos.RemoveAt(0);
            }
            CEUtils.AddLight(Projectile.Center, new Color(160, 160, 255));
        }
        public override Color baseColor => new Color(255, 255, 255);
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Black, Projectile.rotation, tex.Size() / 2f, new Vector2(0.82f, 0.3f) * Projectile.scale, SpriteEffects.None);

            if (OldPos.Count > 2)
            {
                for (int i = 1; i < OldPos.Count; i++)
                {
                    for (float j = 0; j < 1; j += 0.5f)
                    {
                        Main.EntitySpriteDraw(tex, Vector2.Lerp(OldPos[i - 1], OldPos[i], j) - Main.screenPosition, null, color * 0.1f * ((float)i / OldPos.Count), OldRot[i], tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
                    }
                }
            }

            return false;
        }
        public float DamageMul = 1;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            if (Projectile.numHits < 2)
                CEUtils.PlaySound("GrassSwordHit1", Main.rand.NextFloat(2f, 2.4f), target.Center, 60, 0.32f);
            DamageMul = float.Max(0.5f, DamageMul * 0.7f);
            CosmicBlessingHeld.ProduceWarpCrossDust(Projectile.Center, 0.8f);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers);
            modifiers.SourceDamage *= DamageMul;
        }

    }
}
