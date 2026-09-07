using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles.Prophet;
using CalamityEntropy.Content.UI.EntropyBookUI;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books
{
    public class ProphecyMasterpiece : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 48;
            Item.useAnimation = Item.useTime = 11;
            Item.crit = 10;
            Item.mana = 7;
            Item.rare = ItemRarityID.Yellow;
            Item.expert = true;
            Item.value = Item.buyPrice(gold: 60);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/PM")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<ProphecyMasterpieceHeld>();
        public override int SlotCount => 3;
    }

    public class ProphecyMasterpieceHeld : EntropyBookHeldProjectile
    {
        public override void playPageSound()
        {
            CEUtils.PlaySound("pageflip", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, 4, 0.1f);
        }
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/ProphecyMasterpiece/ProphecyMasterpieceOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/ProphecyMasterpiece/ProphecyMasterpiecePage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/ProphecyMasterpiece/ProphecyMasterpieceUI";

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.PenetrateAddition += 2;
            return m;
        }
        public override float randomShootRotMax => 0;
        public override int baseProjectileType => ModContent.ProjectileType<ForeseeEye>();

        public override int frameChange => 3;
        public override EBookProjectileEffect getEffect()
        {
            return new PMBookEffect();
        }
        public override void AI()
        {
            base.AI();
        }
        public override bool Shoot()
        {
            return true;
        }
    }

    public class PMBookEffect : EBookProjectileEffect
    {
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<SoulDisorder>(), 600);
        }

        public override void OnActive(EntropyBookHeldProjectile book)
        {
            book.ShootSingleProjectile(book.baseProjectileType, book.Projectile.Center, book.Projectile.rotation.ToRotationVector2(), MainProjectile: true);
        }
    }

    public class ForeseeEye : EBookBaseLaser
    {
        public override float width => 100 * Projectile.scale;
        public override int OnHitEffectProb => 2;
        public override void SetDefaults()
        {
            base.SetDefaults();
            segLength = 6;
            segCounts = 360;
            Projectile.penetrate = 1000;
        }
        public bool playsound = true;
        public LoopSound sd;
        public override void AI()
        {
            base.AI();
            if (ShooterModProjectile == null && Projectile.GetOwner().heldProj >= 0)
            {
                ShooterModProjectile = Projectile.GetOwner().heldProj.ToProj().ModProjectile;
            }
            if (this.quickTime >= 0)
            {
                width2 = 1;
            }
            else
            {
                if (playsound && !Main.dedServ)
                {
                    playsound = false;
                    sd = new LoopSound(FableEye.sound);
                    sd.play();
                    sd.instance.Volume = 0;
                }
                if (sd != null)
                {
                    sd.setVolume_Dist(Projectile.Center, 400, 1500, width2 * 0.3f);
                    sd.timeleft = 4;
                    sd.instance.Pitch = width2;
                }
                if (ShooterModProjectile is EntropyBookHeldProjectile eb)
                {
                    if (Main.GameUpdateCount % 24 == 0)
                    {
                        Item bookItem = eb.bookItem;
                        for (int i = 0; i < Math.Min(EBookUI.getMaxSlots(Main.LocalPlayer, bookItem), Projectile.GetOwner().Entropy().EBookStackItems.Count); i++)
                        {
                            Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                            if (BookMarkLoader.IsABookMark(it))
                            {
                                var e = BookMarkLoader.GetEffect(it);
                                if (e != null)
                                {
                                    e.OnShoot(eb);
                                    e.OnProjectileSpawn(Projectile, Main.myPlayer == Projectile.owner);
                                }
                            }
                        }
                    }
                    Projectile.rotation = ShooterModProjectile.Projectile.rotation;
                    Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(Projectile.rotation);
                    if (Projectile.owner == Main.myPlayer)
                    {
                        if ((eb.active && eb.Projectile.active))
                        {
                            if (width2 < 1)
                            {
                                width2 += 0.01f;
                            }
                            Projectile.timeLeft = 5;
                        }
                        else
                        {
                            eb = null;
                            Projectile.Kill();
                        }
                    }
                }
                Projectile.GetOwner().Entropy().MouseWorldListener = true;
                Projectile.Center = Projectile.GetOwner().MountedCenter + Projectile.GetOwner().gfxOffY * Vector2.UnitY + (Projectile.GetOwner().Entropy().MouseWorld - Projectile.GetOwner().Center).normalize() * 80;

            }
            if (Projectile.owner != Main.myPlayer)
            {
                if (width2 < 1)
                {
                    width2 += 0.01f;
                }
                Projectile.timeLeft = 5;
            }
            StealLife = true;
        }
        public override void OnKill(int timeLeft)
        {
            if (this.quickTime < 0)
            {
                CEUtils.PlaySound("CrystalBreak", 1, Projectile.Center);
                if (Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Projectile.velocity * 0.04f, Mod.Find<ModGore>("FableEyeGore1").Type, Projectile.scale);
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Projectile.velocity * 0.04f, Mod.Find<ModGore>("FableEyeGore2").Type, Projectile.scale);
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Projectile.velocity * 0.04f, Mod.Find<ModGore>("FableEyeGore3").Type, Projectile.scale);
                }
            }
        }
        public float width2 = 0;
        public float yx = 0;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers);
            modifiers.SourceDamage *= width2;
            if (target.realLife >= 0)
                modifiers.SourceDamage *= 0.75f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            yx += 0.036f;
            float w = width2 * Projectile.scale;
            if (this.quickTime == -1)
            {
                Texture2D tex = Projectile.GetTexture();
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * width2, 0, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            }
            else
            {
                w = quickTime / 30f;
            }
            var points = this.getSamplePoints();
            w *= 0.6f;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                Texture2D tx = CEExtraAssets.MegaStreakBacking2;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(60, 60, 170);

                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++)
                {
                    float wd = 1;
                    if (i < 48)
                    {
                        wd = new Vector2(1, 0).RotatedBy((i / 48f) * MathHelper.PiOver2).Y;
                    }
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 120 * Projectile.scale * w * wd,
                          new Vector3((float)i / points.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 120 * Projectile.scale * w * wd,
                          new Vector3((float)i / points.Count, 0, 1),
                          b));
                }

                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }

            Main.spriteBatch.End();
            var effect = CEEffectAssets.fableeyelaser;
            effect.Parameters["yofs"].SetValue(yx);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes["fableeyelaser"].Apply();
            {
                Texture2D tx = CEExtraAssets.TurbulentNoise;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Color.SkyBlue * 0.66f;
                float p = -Main.GlobalTimeWrappedHourly;
                for (int i = 1; i < points.Count; i++)
                {
                    float wd = 1;
                    if (i < 48)
                    {
                        wd = new Vector2(1, 0).RotatedBy((i / 48f) * MathHelper.PiOver2).Y;
                    }
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 62 * Projectile.scale * w * wd,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 62 * Projectile.scale * w * wd,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.3f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            effect.Parameters["yofs"].SetValue(-yx);
            {
                Texture2D tx = CEExtraAssets.EternityStreak;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(255, 255, 255) * 0.66f;
                float p = -Main.GlobalTimeWrappedHourly * 2;
                for (int i = 1; i < points.Count; i++)
                {
                    float wd = 1;
                    if (i < 48)
                    {
                        wd = new Vector2(1, 0).RotatedBy((i / 48f) * MathHelper.PiOver2).Y;
                    }
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * 54 * Projectile.scale * w * wd,
                          new Vector3(p, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(points[i] - Main.screenPosition + (points[i] - points[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * 54 * Projectile.scale * w * wd,
                          new Vector3(p, 0, 1),
                          b));
                    p += (CEUtils.getDistance(points[i], points[i - 1]) / tx.Width) * 0.3f;
                }


                SpriteBatch sb = Main.spriteBatch;
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (ve.Count >= 3)
                {
                    gd.Textures[0] = tx;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
