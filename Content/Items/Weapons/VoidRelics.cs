using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class VoidRelics : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 4;
        }

        public override void SetDefaults() {
            Item.damage = 550;
            Item.crit = 0;
            Item.DamageType = DamageClass.Summon;
            Item.width = 64;
            Item.height = 64;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.knockBack = 2;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.shoot = ModContent.ProjectileType<VoidMark>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(platinum: 2);
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.scale = 0.6f;
            Item.mana = 5;
            Item.buffType = ModContent.BuffType<VoidStorm>();
            Item.rare = ModContent.RarityType<VoidPurple>();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            SoundStyle s = new("CalamityEntropy/Assets/Sounds/vmspawn");
            s.Volume = 0.6f;
            s.Pitch = 1f;
            SoundEngine.PlaySound(s, position);
            return false;
        }

        public override bool CanShoot(Player player) {
            return player.ownedProjectileCounts[Item.shoot] == 0 && player.maxMinions - player.slotsMinions >= ItemID.Sets.StaffMinionSlotsRequired[Item.type];
        }

        public override bool CanUseItem(Player player) {
            Item.channel = player.ownedProjectileCounts[Item.shoot] > 0;
            return true;
        }
    }
    public class VoidMark : ModProjectile
    {
        //符文帧动画贴图(rune0~rune9),按序号批量加载,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/VoidRunes/rune", 0, 10, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] RuneTexs;
        public override void SetStaticDefaults() {
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 360;
            Projectile.height = 360;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 5;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.minion = true;
            Projectile.minionSlots = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }
        public float Charge = 0;
        public override bool? CanDamage() {
            return translateFlex < 0.1f;
        }
        public class VoidMarksRune
        {
            public int ID;
            public float Rotation;
            public float Glow;
            public VoidMarksRune(int iD, float rotation) {
                ID = iD;
                Rotation = rotation;
                Glow = 0;
            }
        }
        public List<VoidMarksRune> runes;
        public void SetupRunes() {
            runes = new List<VoidMarksRune>();
            for (int i = 0; i < 10; i++) {
                runes.Add(new VoidMarksRune(i, (i / 10f) * MathHelper.TwoPi));
            }
        }
        public override void SendExtraAI(BinaryWriter writer) {
            if (runes == null)
                SetupRunes();
            for (int i = 0; i < 10; i++)
                writer.Write(runes[i].Glow);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            if (runes == null) SetupRunes();
            for (int i = 0; i < 10; i++)
                runes[i].Glow = reader.ReadSingle();
        }
        public int StormTime = 0;
        public bool translating = false;
        public override void AI() {
            Projectile.localAI[1] = float.Lerp(Projectile.localAI[1], 1, 0.05f);
            if (runes == null)
                SetupRunes();
            runes[0].Rotation += 0.02f * (1 + translateFlex * 9);
            for (int i = 1; i < 10; i++) {
                runes[i].Rotation = runes[0].Rotation + i * (MathHelper.TwoPi) / 10f;
            }
            for (int i = 0; i < 10; i++) {
                VoidMarksRune r = runes[i];
                r.Glow *= 0.9f;
            }
            Player player = Main.player[Projectile.owner];
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY;

            if (Projectile.Entropy().FirstFrames) {
                float scale = 2;
                //DOracleSlash Configure传NonPremultipliedBlend,同DedicatedOracle
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + new Vector2(0, -236), player.velocity, new Color(80, 40, 200), scale * 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + new Vector2(0, -236), player.velocity, Color.White, scale * 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + new Vector2(0, -236), player.velocity, Color.White, scale * 0.3f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 10);
            }
            if (player.HasBuff(ModContent.BuffType<VoidStorm>())) {
                Projectile.timeLeft = 3;
            }
            else {
                return;
            }
            bool channel = player.channel && player.HeldItem.type == ModContent.ItemType<VoidRelics>();
            if (channel) {
                player.itemTime = player.itemAnimation = 3;
            }
            translating = false;

            if (channel && !player.HasCooldown(AbyssalStorm.ID)) {
                translating = true;
                if (translateFlex >= 0.9f)
                    StormTime++;
            }
            if ((StormTime > 6 * 60 || !channel) && StormTime > 0) {
                translating = false;
                player.channel = false;
                player.AddCooldown(AbyssalStorm.ID, StormTime * 6);
                StormTime = 0;
            }
            player.Entropy().MouseWorldListener = true;
            float targetRot = (player.Entropy().MouseWorld - player.MountedCenter).ToRotation();
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.1f, false);
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, targetRot, 0.02f, true);
            if (Translate) {
                if (translateFlex < 1)
                    translateFlex += 0.025f;
            }
            else {
                translateFlex *= 0.9f;
            }
            if (Projectile.owner == Main.myPlayer) {
                NPC target = Projectile.FindMinionTarget(3600);
                if (true) {
                    if (translateFlex < 0.9f) {
                        if (target != null)
                            Projectile.ai[0] += player.HasCooldown(AbyssalStorm.ID) ? 1.15f : 1;
                    }
                    else {
                        Projectile.ai[0] += 10;
                    }
                    if (Projectile.ai[0] >= 30) {
                        Projectile.ai[0] -= 30;
                        if (Main.myPlayer == Projectile.owner) {
                            int id1 = Main.rand.Next(0, 9);
                            int id2 = Main.rand.Next(1, 10);
                            if (id1 == id2)
                                id2++;

                            CEUtils.PlaySound("CrystalBallActive", Main.rand.NextFloat(2.4f, 2.6f), Projectile.Center, 16, 0.3f);
                            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = Main.rand.NextFloat(0.2f, 0.4f), Volume = 0.6f }, Projectile.Center);

                            runes[id1].Glow += translateFlex < 0.9f ? 1 : 0.3f;
                            runes[id2].Glow = translateFlex < 0.9f ? 1 : 0.3f;
                            Vector2 tv = target != null ? (target.Center - Projectile.Center).normalize() : Vector2.Zero;
                            int type = ModContent.ProjectileType<VoisenBullet>();
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.MountedCenter + GetCPos(runes[id1].Rotation, Radius, circleScaling, circleOffset, rotation), ((translateFlex < 0.9f) ? (tv.RotatedByRandom(0.4f) * Main.rand.NextFloat(9, 12)) : Projectile.rotation.ToRotationVector2().RotatedByRandom(0.02f) * Main.rand.NextFloat(9, 12)), type, (translateFlex < 0.9f) ? Projectile.damage : (Projectile.damage / 4), 6, Projectile.owner);
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.MountedCenter + GetCPos(runes[id2].Rotation, Radius, circleScaling, circleOffset, rotation), ((translateFlex < 0.9f) ? (tv.RotatedByRandom(0.4f) * Main.rand.NextFloat(9, 12)) : Projectile.rotation.ToRotationVector2().RotatedByRandom(0.02f) * Main.rand.NextFloat(9, 12)), type, (translateFlex < 0.9f) ? Projectile.damage : (Projectile.damage / 4), 6, Projectile.owner);

                            CEUtils.SyncProj(Projectile.whoAmI);
                        }
                    }
                }
            }
        }
        public bool Translate { get { return translating; } }
        public float translateFlex { get { return Projectile.ai[2]; } set { Projectile.ai[2] = value; } }
        public static Vector2 GetCPos(float rot, float radius, Vector2 scaling, Vector2 offset, float FullRot) {
            Vector2 p = (rot - FullRot).ToRotationVector2() * radius;
            p = (p * scaling).RotatedBy(FullRot) + offset.RotatedBy(FullRot);
            return p;
        }
        public static void DrawRune(Vector2 pos, VoidMarksRune rune, float scale) {
            int texCount = int.Max(0, int.Min(9, rune.ID));
            Texture2D tex = RuneTexs[texCount];
            Main.spriteBatch.UseAdditive();
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4) {
                Main.spriteBatch.Draw(tex, pos + i.ToRotationVector2() * (2 + 4 * rune.Glow), null, Color.White * (0.5f + 0.5f * rune.Glow), 0, tex.Size().Half(), scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(tex, pos + i.ToRotationVector2() * (4 + 6 * rune.Glow), null, Color.White * (0.5f + 0.5f * rune.Glow), 0, tex.Size().Half(), scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(tex, pos, null, Color.White, 0, tex.Size().Half(), scale, SpriteEffects.None, 0);
            if (rune.Glow > 0.05f) {
                Main.spriteBatch.UseAdditive();
                Main.spriteBatch.Draw(tex, pos, null, Color.White * rune.Glow, 0, tex.Size().Half(), scale, SpriteEffects.None, 0);
                CEUtils.DrawGlow(pos + Main.screenPosition, new Color(240, 240, 255), 1.6f * rune.Glow, true, null, false);
                Main.spriteBatch.ExitShaderRegion();
            }
        }
        public float rotation => Projectile.rotation;
        public float Radius => float.Lerp(160, 60, translateFlex) * Projectile.scale * Projectile.localAI[1];
        public Vector2 circleScaling => Vector2.Lerp(Vector2.One, new Vector2(0.4f, 1f), translateFlex);
        public Vector2 circleOffset => Vector2.Lerp(Vector2.Zero, new Vector2(100, 0), translateFlex);

        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseAdditive();
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4) {
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White * 0.5f, 0, overridePos: Projectile.Center + new Vector2(0, -236) + (i + Main.GlobalTimeWrappedHourly * 6).ToRotationVector2() * 4));
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White * 0.5f, 0, overridePos: Projectile.Center + new Vector2(0, -236) + (i + Main.GlobalTimeWrappedHourly * 6).ToRotationVector2() * 8));
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White * 0.5f, 0, overridePos: Projectile.Center + new Vector2(0, -236) + (i - Main.GlobalTimeWrappedHourly * 6).ToRotationVector2() * 4));
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White * 0.5f, 0, overridePos: Projectile.Center + new Vector2(0, -236) + (i - Main.GlobalTimeWrappedHourly * 6).ToRotationVector2() * 8));
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, 0, overridePos: Projectile.Center + new Vector2(0, -236)));

            if (translateFlex > 0.02f) {
                Texture2D glow = CEExtraAssets.Glow2;
                Vector2 glowPos = Projectile.Center + circleOffset.RotatedBy(rotation) - Main.screenPosition;
                float glowScale = translateFlex;

                Main.spriteBatch.UseAdditive();
                Main.spriteBatch.Draw(glow, glowPos, null, new Color(140, 140, 255), rotation, glow.Size().Half(), circleScaling * glowScale * 1.1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(glow, glowPos, null, new Color(140, 140, 255), rotation, glow.Size().Half(), circleScaling * glowScale * 0.6f, SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }

            #region ring
            var gd = Main.graphics.GraphicsDevice;
            Texture2D tx = CEExtraAssets.DeathRay;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            gd.Textures[0] = tx;
            {
                List<Vector3> points = new List<Vector3>();
                for (float i = 0; i <= 1; i += 0.005f) {
                    float rot = i * MathHelper.TwoPi;
                    float m = (1f + 0.065f * (float)(Math.Sin(MathHelper.TwoPi * 6 * i - Main.GlobalTimeWrappedHourly * 4))) * (1 + translateFlex * 0.5f);
                    points.Add(new Vector3(GetCPos(rot, Radius * m, circleScaling, circleOffset, rotation), 1f + 0.28f * (float)(Math.Sin(MathHelper.TwoPi * 4 * i + Main.GlobalTimeWrappedHourly * 8)) * (1 + translateFlex * 0.5f)));
                }
                Vector3 lastPoint = points[points.Count - 2];
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                float alpha = 1f;
                float trailOffset = Main.GlobalTimeWrappedHourly * -4f;
                Vector2 center = Projectile.GetOwner().GetDrawCenter();
                for (int ii = 0; ii < points.Count; ii++) {
                    int i = ii;
                    Vector2 pos = points[i].xy();
                    float w = points[i].Z * 28 * (Radius / 160f);
                    Vector2 v = (lastPoint.xy() - pos).RotatedBy(MathHelper.PiOver2).normalize();
                    ve.Add(new ColoredVertex(center + pos + v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset * 0.6f, 1, 1),
                          new Color(80, 80, 255) * alpha));
                    ve.Add(new ColoredVertex(center + pos - v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset * 0.6f, 0, 1),
                          new Color(80, 80, 255) * alpha));
                    ve2.Add(new ColoredVertex(center + pos + v * w * Projectile.scale * 0.6f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset, 1, 1),
                          new Color(100, 100, 255) * alpha));
                    ve2.Add(new ColoredVertex(center + pos - v * w * Projectile.scale * 0.6f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset, 0, 1),
                          new Color(100, 100, 255) * alpha));

                    lastPoint = points[i];
                }
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
            }
            {
                List<Vector3> points = new List<Vector3>();
                for (float i = 0; i <= 1; i += 0.005f) {
                    float rot = i * MathHelper.TwoPi;
                    float m = (1.2f + 0.05f * (float)(Math.Sin(MathHelper.TwoPi * 8 * i + Main.GlobalTimeWrappedHourly * -3))) * (1 + translateFlex * 0.5f);
                    points.Add(new Vector3(GetCPos(rot, Radius * m, circleScaling, circleOffset, rotation), 0.6f + 0.44f * (float)(Math.Sin(MathHelper.TwoPi * 4 * i + Main.GlobalTimeWrappedHourly * 12)) * (1 + translateFlex * 0.5f)));
                }
                Vector3 lastPoint = points[points.Count - 2];
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                float alpha = 1f;
                float trailOffset = Main.GlobalTimeWrappedHourly * -2f;
                Vector2 center = Projectile.GetOwner().GetDrawCenter();
                for (int ii = 0; ii < points.Count; ii++) {
                    int i = ii;
                    Vector2 pos = points[i].xy();
                    alpha = points[i].Z;
                    float w = points[i].Z * 20 * (Radius / 160f);
                    Vector2 v = (lastPoint.xy() - pos).RotatedBy(MathHelper.PiOver2).normalize();
                    ve.Add(new ColoredVertex(center + pos + v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset * 0.6f, 1, 1),
                          new Color(80, 80, 255) * alpha));
                    ve.Add(new ColoredVertex(center + pos - v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset * 0.6f, 0, 1),
                          new Color(80, 80, 255) * alpha));
                    ve2.Add(new ColoredVertex(center + pos + v * w * Projectile.scale * 0.6f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset, 1, 1),
                          new Color(100, 100, 255) * alpha));
                    ve2.Add(new ColoredVertex(center + pos - v * w * Projectile.scale * 0.6f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset, 0, 1),
                          new Color(100, 100, 255) * alpha));

                    lastPoint = points[i];
                }
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
            }
            {
                List<Vector3> points = new List<Vector3>();
                for (float i = 0; i <= 1; i += 0.005f) {
                    float rot = i * MathHelper.TwoPi;
                    float m = (0.85f + 0.1f * (float)(Math.Sin(MathHelper.TwoPi * 8 * i - Main.GlobalTimeWrappedHourly * -3))) * (1 + translateFlex * 0.5f);
                    points.Add(new Vector3(GetCPos(rot, Radius * m, circleScaling, circleOffset, rotation), 0.6f + 0.44f * (float)(Math.Sin(MathHelper.TwoPi * 4 * i - Main.GlobalTimeWrappedHourly * 12)) * (1 + translateFlex * 0.5f)));
                }
                Vector3 lastPoint = points[points.Count - 2];
                List<ColoredVertex> ve = new List<ColoredVertex>();
                List<ColoredVertex> ve2 = new List<ColoredVertex>();
                float alpha = 1f;
                float trailOffset = Main.GlobalTimeWrappedHourly * -2f;
                Vector2 center = Projectile.GetOwner().GetDrawCenter();
                for (int ii = 0; ii < points.Count; ii++) {
                    int i = ii;
                    Vector2 pos = points[i].xy();
                    alpha = points[i].Z;
                    float w = points[i].Z * 15 * (Radius / 160f);
                    Vector2 v = (lastPoint.xy() - pos).RotatedBy(MathHelper.PiOver2).normalize();
                    ve.Add(new ColoredVertex(center + pos + v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset * 0.6f, 1, 1),
                          new Color(80, 80, 255) * alpha));
                    ve.Add(new ColoredVertex(center + pos - v * w * Projectile.scale - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset * 0.6f, 0, 1),
                          new Color(80, 80, 255) * alpha));
                    ve2.Add(new ColoredVertex(center + pos + v * w * Projectile.scale * 0.6f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset, 1, 1),
                          new Color(100, 100, 255) * alpha));
                    ve2.Add(new ColoredVertex(center + pos - v * w * Projectile.scale * 0.6f - Main.screenPosition,
                          new Vector3((ii / ((float)points.Count - 1f)) * 6 + trailOffset, 0, 1),
                          new Color(100, 100, 255) * alpha));

                    lastPoint = points[i];
                }
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
            }

            Main.spriteBatch.ExitShaderRegion();
            #endregion


            for (int i = 0; i < runes.Count; i++) {
                DrawRune(Projectile.GetOwner().GetDrawCenter() + GetCPos(runes[i].Rotation, Radius, circleScaling, circleOffset, rotation) - Main.screenPosition, runes[i], Projectile.scale);
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 80, 10);
        }
    }

    public class VoisenBullet : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 16 * 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.MaxUpdates = 4;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return Projectile.position.getRectCentered(20 * Projectile.scale, 20 * Projectile.scale).Intersects(targetHitbox);
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public override void AI() {
            if (Projectile.Entropy().FirstFrames)
                Projectile.ai[0] = Main.rand.NextFloat(100);
            NPC target = Projectile.FindMinionTarget(3600);
            if (Projectile.localAI[0]++ > 40 && target != null) {
                Projectile.velocity *= 0.97f;
                Vector2 v = target.Center - Projectile.position;
                v.Normalize();

                Projectile.velocity += v * 0.5f;
            }
            Vector2 adv = Projectile.velocity.RotatedBy((float)Math.Sin(Projectile.localAI[0] * 0.1f + Projectile.ai[0]) * 0.4f);
            Projectile.rotation = adv.ToRotation();
            Projectile.position += adv;

            oldPos.Add(Projectile.Center);
            oldRots.Add(Projectile.rotation);
            if (oldPos.Count > 46) {
                oldPos.RemoveAt(0);
                oldRots.RemoveAt(0);
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.AddVoidTouch(target, 80, 5);
            for (int i = 0; i < 6; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(2f, 3f);
                dust.velocity = new Vector2(24, 0).RotatedBy(CEUtils.randomRot()) * Main.rand.NextFloat(0.3f, 1f);
                dust.noGravity = false;
                dust.color = new Color(160, 160, 255);
                dust.fadeIn = 2f;
            }
            float r = CEUtils.randomRot();
            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            var slash = PRTLoader.NewParticle<PRT_DOracleSlash>(target.Center - r.ToRotationVector2() * 120, Vector2.Zero, new Color(140, 100, 255), Main.rand.NextFloat(250, 280));
            slash.centerColor = Color.White;
            slash.widthMult = 1.2f;
            slash.Configure(0.24f, true, PRTDrawModeEnum.NonPremultiplied, r, 11);
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1f, 1.3f), target.Center);
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override bool PreDraw(ref Color lightColor) {
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
            float alpha = 1;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            List<ColoredVertex> ve2 = new List<ColoredVertex>();
            float trailOffset = Main.GlobalTimeWrappedHourly * 6;
            for (int i = 0; i < oldPos.Count; i++) {
                alpha = i / (oldPos.Count - 1f);
                Vector2 m = oldPos[i];
                Vector2 l = oldRots[i].ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                ve.Add(new ColoredVertex(m + l * alpha * 14 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset, 1, 1),
                      new Color(99, 99, 255) * alpha));
                ve.Add(new ColoredVertex(m - l * alpha * 14 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset, 0, 1),
                      new Color(99, 99, 255) * alpha));

                ve2.Add(new ColoredVertex(m + l * alpha * 9 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset * 1.6f, 1, 1),
                      new Color(180, 180, 255) * alpha));
                ve2.Add(new ColoredVertex(m - l * alpha * 9 * Projectile.scale - Main.screenPosition,
                      new Vector3(alpha * 3 + trailOffset * 1.6f, 0, 1),
                      new Color(180, 180, 255) * alpha));
            }
            if (ve.Count >= 3) {
                Texture2D tx = CEExtraAssets.DeathRay;
                gd.Textures[0] = tx;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.Textures[0] = CEExtraAssets.Streak2;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
            }

            Texture2D glow = CEExtraAssets.Glow2;
            Texture2D tex = CEExtraAssets.Ray;

            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, new Color(60, 60, 255), Projectile.rotation, glow.Size().Half(), 0.32f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, new Color(230, 230, 255), Projectile.rotation, glow.Size().Half(), 0.16f * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(100, 100, 255), Projectile.rotation, tex.Size().Half(), new Vector2(2, 0.8f) * 0.48f * Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 255, 255), Projectile.rotation, tex.Size().Half(), new Vector2(2, 0.8f) * 0.3f * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
