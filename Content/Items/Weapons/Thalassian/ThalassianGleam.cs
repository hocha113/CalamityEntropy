using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Donator.RocketLauncher;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Content.UI.EntropyBookUI;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityEntropy.Content.Items.Weapons.Thalassian
{
    public class ThalassianGleam : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.useTime = Item.useAnimation = 20;
            Item.damage = 16;
            Item.crit = 10;
            Item.mana = 5;
            Item.shootSpeed = 15;
            Item.value = Item.buyPrice(gold: 5);
        }
        public override Texture2D BookMarkTexture => CEUtils.pixelTex;
        public override int HeldProjectileType => ModContent.ProjectileType<ThalassianGleamHeld>();
        public override int SlotCount => GetSlotCount();
        public static int GetSlotCount() => Level() switch
        {
            0 => 1,
            1 => 1,
            2 => 2,
            3 => 2,
            4 => 3,
            5 => 3,
            6 => 4,
            7 => 4,
            8 => 5,
            9 => 5,
            10 => 5,
            11 => 5,
            12 => 5,
            13 => 6,
            14 => 6,
            15 => 6,
            _ => 6
        };
        public static int ShootCount() => Level() switch
        {
            0 => 2,
            1 => 2,
            2 => 3,
            3 => 3,
            4 => 3,
            5 => 3,
            6 => 3,
            7 => 4,
            8 => 4,
            9 => 4,
            10 => 4,
            11 => 5,
            12 => 5,
            13 => 5,
            14 => 5,
            15 => 6,
            _ => 6
        };
        public override void AddRecipes()
        {

        }
        #region SlotStaffs
        public override bool PreDrawBookmarkSlot(Item bookmark, Vector2 pos, float alpha, float scale, float outlineAlpha)
        {
            scale *= 1.2f;
            if (!bookmark.IsAir)
            {
                if (BookMarkLoader.IsABookMark(bookmark) && bookmark.ModItem is not ThalassianLegendGem && BookMarkLoader.GetUITexture(bookmark) != null)
                {
                    Main.spriteBatch.Draw(BookMarkLoader.GetUITexture(bookmark), pos, null, Color.White * alpha, 0, BookMarkLoader.GetUITexture(bookmark).Size() / 2, 1 * scale, SpriteEffects.None, 0);
                }
                else
                {
                    ItemSlot.DrawItemIcon(bookmark, 1, Main.spriteBatch, pos, 0.8f * scale, 256, Color.White * alpha);
                }
            }

            //Frame
            bool gemShape = false;
            if(bookmark.ModItem != null)
            {
                if (bookmark.ModItem is ThalassianLegendGem)
                    gemShape = true;
            }
            else
            {
                gemShape = true;
                if (BookMarkLoader.IsABookMark(Main.mouseItem) && !(Main.mouseItem is ThalassianLegendGem))
                    gemShape = false;
            }
            float num = 46;
            float l = 28 * scale;
            float w = 14 * scale;
            float h = 24 * scale;
            Vector2 center = pos;
            List<Vector2> points = new List<Vector2>();
            List<CEUtils.VertexPointSets> sets = new List<CEUtils.VertexPointSets>();
            if (gemShape)
            {
                Vector2[] vertices = new Vector2[]
                {
                    new Vector2(l / 2f, 0),
                    new Vector2(0, l / 2f),
                    new Vector2(-l / 2f, 0),
                    new Vector2(0, -l / 2f)
                };
                int n = 4;
                float[] edgeLen = new float[n];
                float[] cumLen = new float[n];
                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;
                    edgeLen[i] = Vector2.Distance(vertices[i], vertices[next]);
                    cumLen[i] = (i == 0) ? edgeLen[i] : cumLen[i - 1] + edgeLen[i];
                }
                float perimeter = cumLen[n - 1];

                float step = perimeter / (num - 1);

                for (int i = 0; i < num - 1; i++)
                {
                    float t = i * step;
                    int edgeIdx = 0;
                    while (edgeIdx < n - 1 && t > cumLen[edgeIdx])
                        edgeIdx++;

                    float localT = (edgeIdx == 0) ? t : t - cumLen[edgeIdx - 1];
                    float frac = localT / edgeLen[edgeIdx];

                    Vector2 start = vertices[edgeIdx];
                    Vector2 end = vertices[(edgeIdx + 1) % n];
                    points.Add(Vector2.Lerp(start, end, frac));

                }
                points.Add(points[0]);
            }
            else
            {
                Vector2[] vertices = new Vector2[]
                {
                    new Vector2(w / 2f, h / 2f),
                    new Vector2(w / 2f, -h / 2f),
                    new Vector2(-w / 2f, -h / 2f),
                    new Vector2(-w / 2f, h / 2f)
                };

                int n = 4;
                float[] edgeLen = new float[n];
                float[] cumLen = new float[n];
                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;
                    edgeLen[i] = Vector2.Distance(vertices[i], vertices[next]);
                    cumLen[i] = (i == 0) ? edgeLen[i] : cumLen[i - 1] + edgeLen[i];
                }
                float perimeter = cumLen[n - 1];

                float step = perimeter / (num - 1);

                for (int i = 0; i < num - 1; i++)
                {
                    float t = i * step;
                    int edgeIdx = 0;
                    while (edgeIdx < n - 1 && t > cumLen[edgeIdx])
                        edgeIdx++;

                    float localT = (edgeIdx == 0) ? t : t - cumLen[edgeIdx - 1];
                    float frac = localT / edgeLen[edgeIdx];

                    Vector2 start = vertices[edgeIdx];
                    Vector2 end = vertices[(edgeIdx + 1) % n];
                    points.Add(Vector2.Lerp(start, end, frac));
                }
                points.Add(points[0]);
            }
            for(int i = 0; i < points.Count; i++)
            {
                float pr = i / (points.Count - 1f);
                float width = 1;
                if(gemShape)
                {
                    width = CEUtils.Parabola(pr, 1);
                }
                else
                {
                    width = CEUtils.Parabola(Utils.Remap(pr, 0.1f, 0.95f, 0, 1), 1);
                }
                width *= 2.4f * scale;
                sets.Add(new CEUtils.VertexPointSets(points[i] + center, Color.White * alpha, width, 0));
            }
            ThalassianWaterBolt.DrawTrail(sets, Color.Lerp(Color.Aqua, Color.Yellow, outlineAlpha), new Color(60, 255, 255), CEExtraAssets.VoronoiShapes, CEExtraAssets.StreakSolid, true);

            return false;
        }
        #endregion
        public static int Level()
        {
            // 装灾厄走 3.33 细档(含 14/13/12/10 死档回生),无灾厄保持 4.0 形状
            if (CECal.DownedExoMechs(EDownedBosses.downedCruiser) && CECal.DownedCalamitas(EDownedBosses.downedCruiser))
                return 15;
            if (CECal.DownedExoMechs(false) || CECal.DownedCalamitas(false))
                return 14;
            if (CECal.DownedYharon(false))
                return 13;
            if (CECal.DownedDoG(false))
                return 12;
            if ((CECal.DownedStormWeaver || CECal.DownedCeaselessVoid || CECal.DownedSignus) && CECal.DownedPolterghast)
                return 11;
            if (CECal.DownedProvidence(false))
                return 10;
            if (NPC.downedMoonlord)
                return 9;
            if (NPC.downedGolemBoss || NPC.downedAncientCultist || CECal.DownedRavager || CECal.DownedAstrumDeus)
                return 8;
            if (NPC.downedPlantBoss && CECal.DownedCalamitasClone(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3))
                return 7;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                return 6;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                return 5;
            if (Main.hardMode)
                return 4;
            if (NPC.downedQueenBee || NPC.downedBoss3)
                return 3;
            if (NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind)
                return 2;
            if (NPC.downedBoss1 || CECal.DownedDesertScourge || NPC.downedSlimeKing)
                return 1;
            return 0;
        }
        public override void PlayBookmarkInsertSound()
        {
            CEUtils.PlaySound("gem/gemInsert" + Main.rand.Next(5), Main.rand.NextFloat(0.9f, 1.1f));
        }
    }
    public interface ThalassianLegendGem
    { }
    public class ThalassianGleamHeld : EntropyBookDrawingAlt
    {
        //闪光贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/BrightFlash")]
        internal static Asset<Texture2D> BrightFlashTex;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Thalassian/ThalassianGleam";
        public override string OpenAnimationPath => CEUtils.WhiteTexPath;
        public override string PageAnimationPath => CEUtils.WhiteTexPath;
        public override string UIOpenAnimationPath => CEUtils.WhiteTexPath;
        public override int OpenAnmCount => 1;
        public override int PageAnmCount => 1;
        public override int UIOpenAnmCount => 1;

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.Size += ThalassianGleam.Level() / 15f;
            m.shotSpeed += ThalassianGleam.Level() / 12f;
            return m;
        }
        public override float randomShootRotMax => 0.05f;
        public int LongerCD = ThalassianGleam.ShootCount();
        public override bool Shoot()
        {
            Vector2 ovel = Projectile.velocity;
            Vector2 opos = Projectile.Center;
            Projectile.Center = Projectile.Center + Projectile.rotation.ToRotationVector2() * 66 * Projectile.scale;
            Projectile.velocity = (Main.MouseWorld - Projectile.Center).normalize() * Projectile.velocity.Length();
            bool s = base.Shoot();
            if (s)
            {
                float scale = getBaseModifer().Size;
                CEUtils.PlaySound("ThalassianShoot", Main.rand.NextFloat(0.6f, 1.1f), Projectile.Center, volume: 0.32f);
                for (int i = 0; i < 10 + ThalassianGleam.Level() / 3; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.4f, 1f) * scale * 1.4f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 1f) * 18 * scale;
                    dust.noGravity = false;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
                for(int i = 0; i < 4; i++)
                {
                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Projectile.GetOwner().velocity, new Color(180, 255, 255), 0.01f).Configure("CalamityEntropy/Assets/Extra/Star2", Vector2.One, CEUtils.randomRot(), 0.01f, 0.8f * scale, 16);
                }
            }
            Projectile.Center = opos;
            Projectile.velocity = ovel;
            
            return s;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
        public override void SetShootCooldown(int cd)
        {
            base.SetShootCooldown(cd);

            LongerCD--;
            if(LongerCD <= 0)
            {
                LongerCD = ThalassianGleam.ShootCount();
            }
            else
            {
                shotCooldown /= 3;
            }
        }
        public float orot = 0;
        public override void SetPosision()
        {
            base.SetPosision();
            if (UIOpen)
            {
                if (UIOpenAnmSCount < 20)
                    UIOpenAnmSCount++;
                Projectile.Center += Vector2.UnitY * (50 - CEUtils.GetRepeatedCosFromZeroToOne(UIOpenAnmSCount / 20f, 2) * 24) * Projectile.scale;
            }
            else
            {
                UIOpenAnmSCount = 0;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (Projectile.velocity.X > 0 ? 0 : MathHelper.Pi), 0.5f, false);
                orot = Projectile.rotation;
                Projectile.rotation += (Projectile.velocity.X > 0 ? 1 : -1) * -1f;
            }
        }
        public override void playTurnPageAnimation()
        {
        }
        public override Texture2D getTexture()
        {
            return Projectile.GetTexture();
        }
        public override Rectangle GetFrame()
        {
            Texture2D tex = getTexture();
            return new Rectangle(0, 0, tex.Width, tex.Height);
        }
        public override Vector2 GetOrigin()
        {
            return new Vector2(36, 80);
        }
        public override void AI()
        {
            base.AI();
            if (UIOpen)
            {
                Projectile.rotation = -MathHelper.PiOver2;
                orot = Projectile.rotation;
            }
            Projectile.GetOwner().heldProj = Projectile.whoAmI;
            Projectile.GetOwner().SetHandRotWithDir(orot, Projectile.velocity.X > 0 ? 1 : -1);
            UpdateRope();
        }
        public int UIOpenAnmSCount = 0;
        public override int frameChange => 1;
        public override int baseProjectileType => ModContent.ProjectileType<ThalassianWaterBolt>();
        public Rope rope = null;
        public int lastDir = 1;
        public void UpdateRope()
        {
            int d = Projectile.velocity.X > 0 ? 1 : -1;
            Vector2 ropePoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * 38 * Projectile.scale;
            if (rope == null)
            {
                rope = new Rope(ropePoint, 10, 9.8f * Projectile.scale, new Vector2(0, 0.4f), 0.15f, 30);
            }
            rope.gravity = active ? Projectile.velocity.normalize() * -1.2f : new Vector2(0, 0.4f);
            if (d != lastDir)
            {
                for (int i = 0; i < rope.segments.Count; i++)
                {
                    rope.segments[i].position = new Vector2(Projectile.Center.X * 2 - rope.segments[i].position.X, rope.segments[i].position.Y);
                    rope.segments[i].oldPosition = rope.segments[i].position;
                    rope.segments[i].velocity *= 0;
                }
                rope.Start = ropePoint;
            }
            lastDir = d;
            Vector2 lst = rope.Start;
            for (float r = 0.5f; r <= 1; r += 0.5f)
            {
                rope.Start = Vector2.Lerp(lst, ropePoint, r);
                rope.Update();
            }
            return;
        }
        public void DrawRope()
        {
            if (rope != null)
            {
                var odp = rope.GetPoints();
                float w = 9 * Projectile.scale;
                int xp = 0;
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = Lighting.GetColor((odp[0] / 16).ToPoint());
                ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * w,
                          new Vector3((float)0, 1, 1),
                          b));
                ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[0] - Main.screenPosition + (odp[1] - odp[0]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * w,
                      new Vector3((float)0, 0, 1),
                      b));
                for (int i = 1; i < odp.Count; i++)
                {
                    b = Lighting.GetColor((odp[i] / 16).ToPoint());
                    ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(90)) * w,
                          new Vector3((float)(i + 1) / odp.Count, 1, 1),
                          b));
                    ve.Add(new ColoredVertex(new Vector2(xp, 0) + odp[i] - Main.screenPosition + (odp[i] - odp[i - 1]).ToRotation().ToRotationVector2().RotatedBy(MathHelper.ToRadians(-90)) * w,
                          new Vector3((float)(i + 1) / odp.Count, 0, 1),
                          b));

                }
                Texture2D texst = this.getTextureAlt("String");
                var gd = Main.graphics.GraphicsDevice;
                gd.Textures[0] = texst;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = getTexture();
            Vector2 origin = GetOrigin();
            float rot = MathHelper.PiOver4;
            int dir = Projectile.velocity.X > 0 ? 1 : -1;
            if (UIOpen)
                dir = 1;
            if(dir < 0)
            {
                rot *= -1;
                origin.Y = texture.Height - origin.Y;
            }
            DrawRope();
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + rot, origin, Projectile.scale, (Projectile.velocity.X > 0 || UIOpen ? SpriteEffects.None : SpriteEffects.FlipVertically), 0);
            if (UIOpen)
            {
                Main.spriteBatch.UseAdditive();
                float sa = float.Min(1, UIOpenAnmSCount / 16f);
                float sine = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12) * 0.2f;
                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 50 * Projectile.scale;
                Texture2D shine = BrightFlashTex.Value;
                Main.spriteBatch.Draw(shine, pos - Main.screenPosition, null, new Color(90, 160, 255) * sa, Main.GlobalTimeWrappedHourly * 6, shine.Size().Half(), Projectile.scale * 0.4f * (1 + sine), SpriteEffects.None, 0);
                Main.spriteBatch.Draw(shine, pos - Main.screenPosition, null, new Color(90, 160, 255) * sa, Main.GlobalTimeWrappedHourly * -6, shine.Size().Half(), Projectile.scale * 0.4f * (1 - sine), SpriteEffects.FlipHorizontally, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
    }
    public class ThalassianWaterBolt : EBookBaseProjectile
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = ThalassianGleam.Level() < 8;
            Projectile.timeLeft = 360;
            Projectile.MaxUpdates = 3;
        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            for(float i = 0.05f; i <= 1f; i += 0.05f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 240)
                {
                    odp.RemoveAt(0);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.AddLight(Projectile.Center, Color.SkyBlue, Projectile.scale);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            OnKill(Projectile.timeLeft);
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            if (timeLeft > 0)
            {
                float scale = Projectile.scale;
                CEUtils.PlaySound("ThalassianHit", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
                for (int i = 0; i < 10 + ThalassianGleam.Level(); i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.4f, 1f) * scale * 1.6f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 1f) * 18 * scale;
                    dust.noGravity = true;
                    dust.color = Color.LightBlue;
                    dust.fadeIn = 2f;
                }
                for(int i = 0; i < 3; i++)
                    PRTLoader.NewParticle<PRT_CustomPulse>(Projectile.Center, Vector2.Zero, new Color(180, 255, 255), 0.01f).Configure("CalamityEntropy/Assets/Extra/Star2", Vector2.One, CEUtils.randomRot(), 0.01f, 0.8f * scale, 12);
            }
        }
        public static void DrawTrail(List<CEUtils.VertexPointSets> sets)
        {
            DrawTrail(sets, new Color(30, 120, 205), Color.White);
        }
        public static void DrawTrail(List<CEUtils.VertexPointSets> sets, Color a, Color b)
        {
            DrawTrail(sets, a, b, CEExtraAssets.Streak1, CEExtraAssets.Streak2);
        }
        public static void DrawTrail(List<CEUtils.VertexPointSets> sets, Color a, Color b, bool UI)
        {
            DrawTrail(sets, a, b, CEExtraAssets.Streak1, CEExtraAssets.Streak2, UI);
        }
        public static void DrawTrail(List<CEUtils.VertexPointSets> sets, Color a, Color b, Texture2D trail1, Texture2D trail2, bool UI = false, float innerWidth = 1)
        {
            if (sets.Count > 1)
            {
                List<CEUtils.VertexPointSets> sets1 = new List<CEUtils.VertexPointSets>();
                List<CEUtils.VertexPointSets> sets2 = new List<CEUtils.VertexPointSets>();
                List<CEUtils.VertexPointSets> sets3 = new List<CEUtils.VertexPointSets>();
                Vector2 lastPoint = Vector2.Zero;
                float cxOffset = 0;
                for (int i = 0; i < sets.Count; i++)
                {
                    var s = sets[i];

                    if (i > 0)
                        cxOffset += CEUtils.getDistance(lastPoint, s.Position) * 0.007f;
                    float opc = (s.Color.A / 255f);
                    sets1.Add(new CEUtils.VertexPointSets(s.Position, a * opc, s.Width * innerWidth, cxOffset + Main.GlobalTimeWrappedHourly * 4));
                    sets2.Add(new CEUtils.VertexPointSets(s.Position, b * opc * 0.8f, s.Width * 1f, cxOffset + Main.GlobalTimeWrappedHourly * 5));
                    sets3.Add(new CEUtils.VertexPointSets(s.Position, b * opc * 0.5f, s.Width * 1f, cxOffset + Main.GlobalTimeWrappedHourly * 5));
                    lastPoint = s.Position;
                }
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (UI)
                    Main.spriteBatch.UseBlendState_UI(BlendState.Additive, SamplerState.LinearWrap);
                else
                    Main.spriteBatch.UseAdditive();

                Texture2D trail3 = CEExtraAssets.MegaStreakBacking2;
                List<ColoredVertex> lt;
                lt = sets1.GetVertexesList(false, !UI);
                gd.Textures[0] = trail1;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, lt.ToArray(), 0, lt.Count - 2);

                lt = sets2.GetVertexesList(false, !UI);
                gd.Textures[0] = trail2;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, lt.ToArray(), 0, lt.Count - 2);

                lt = sets3.GetVertexesList(false, !UI);
                gd.Textures[0] = trail3;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, lt.ToArray(), 0, lt.Count - 2);

                if (UI)
                    Main.spriteBatch.UseBlendState_UI(BlendState.AlphaBlend);
                else
                    Main.spriteBatch.ExitShaderRegion();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for(int i = 0; i < odp.Count; i++)
            {
                float p = (i / (odp.Count - 1f));
                float alpha = p < 0.7f ? p / 0.7f : 1;
                float width = 1;
                if (p < 0.8f)
                    width = p / 0.8f;
                else
                    width = CEUtils.Parabola(0.5f + (p - 0.8f) / 0.4f, 1);
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 18 * Projectile.scale * width, 0));
            }
            DrawTrail(vp);
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
