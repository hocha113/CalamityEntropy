using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.LuminarisShoots;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.Jy
{
    public class Silentpeak : ModItem, IDonatorItem, IGetFromStarterBag
    {
        public string DonatorName => "静岳";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.DamageType = DamageClass.Summon;
            Item.width = 64;
            Item.height = 64;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<SilentpeakSword>();
            Item.shootSpeed = 2f;
            Item.value = CalamityGlobalItem.RarityGreenBuyPrice;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 10;
            Item.buffType = ModContent.BuffType<SilentpeakBuff>();
            Item.rare = ItemRarityID.Cyan;
        }
        public override bool AllowPrefix(int pre)
        {
            return false;
        }
        public override void UpdateInventory(Player player)
        {
            Item.damage = GetDamage(Level());
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }
        public static int GetDamage(int level) => level switch
        {
            0 => 5,
            1 => 9,
            2 => 12,
            3 => 16,
            4 => 22,
            5 => 42,
            6 => 56,
            7 => 88,
            8 => 115,
            9 => 150,
            10 => 210,
            11 => 500,
            _ => 500
        };
        public static int Level()
        {
            if (DownedBossSystem.downedCalamitas && DownedBossSystem.downedExoMechs)
                return 11;
            if (DownedBossSystem.downedYharon)
                return 10;
            if (DownedBossSystem.downedDoG)
                return 9;
            if (DownedBossSystem.downedProvidence)
                return 8;
            if (NPC.downedMoonlord)
                return 7;
            if (NPC.downedPlantBoss && DownedBossSystem.downedCalamitasClone)
                return 6;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                return 5;
            if (Main.hardMode)
                return 4;
            if (NPC.downedQueenBee || NPC.downedBoss3)
                return 3;
            if (NPC.downedBoss2 || DownedBossSystem.downedPerforator || DownedBossSystem.downedHiveMind)
                return 2;
            if (NPC.downedBoss1 || DownedBossSystem.downedDesertScourge || NPC.downedSlimeKing)
                return 1;
            return 0;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.CopperShortsword)
                .AddIngredient(ItemID.Feather, 2)
                .AddIngredient(ItemID.Cloud, 8)
                .NearShimmer()
                .Register();
        }
        public bool OwnAble(Player player, ref int count)
        {
            return StartBagGItem.NameContains(player, "息衍");
        }
    }
    public class SilentpeakBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<SilentpeakSword>();
    }
    public class SilentpeakSword : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.light = 0.8f;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
            Projectile.ArmorPenetration = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.MaxUpdates = 2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1f, 1.3f), target.Center, volume: 0.6f);

            for (int i = 0; i < 8; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 9, 0.04f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.LightGreen : Color.LightGray, new Vector2(2.4f, 0.6f), true));
        }
        public int LEVEL = Silentpeak.Level();
        public NPC target = null;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            int newLv = Silentpeak.Level();
            if (newLv != LEVEL)
            {
                Projectile.Kill();
            }
            Projectile.MinionCheck<SilentpeakBuff>();
            if (Projectile.localAI[0] == 0)
            {
                for (int i = 0; i < 16; i++)
                    EParticle.spawnNew(new GlowLightParticle(), Projectile.Center, CEUtils.randomPointInCircle(9), Color.Gray, Main.rand.NextFloat(0.6f, 1f), 1, true, BlendState.Additive, 0, 22);
            }
            if (Projectile.localAI[0]++ < 3)
            {
                Projectile.timeLeft++;
                return;
            }
            if (Projectile.Distance(player.Center) > 8000)
            {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            if(target != null)
            {
                if (!target.active || target.dontTakeDamage)
                    target = null;
            }
            if (AICounter < 2 || aiStyle == AIStyle.FollowOwner)
            {
                target = Projectile.FindMinionTarget(1000 + Silentpeak.Level() * 200);
            }
            if (target == null)
            {
                aiStyle = AIStyle.FollowOwner;
            }
            else
            {
                if (aiStyle == AIStyle.FollowOwner)
                    NextAI();
            }
            if (aiStyle == AIStyle.FollowOwner)
            {
                int MyID = 0;
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.owner == Projectile.owner && proj.type == Projectile.type)
                    {
                        if (proj.whoAmI == Projectile.whoAmI)
                            break;
                        else
                            MyID++;
                    }
                }
                Vector2 targetPos = player.Center + player.velocity * 2 + new Vector2(-(40 + MyID * 16) * player.direction, -80 + 10 * (float)(Math.Sin(Main.GameUpdateCount / 16f - MyID * 0.3f)));
                Projectile.velocity = (targetPos - Projectile.Center) * 0.06f;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, MathHelper.PiOver2 + MyID * player.direction * 0.1f, 0.064f, false);
            }
            if(aiStyle == AIStyle.CloseTarget)
            {
                Projectile.velocity *= 0.94f;
                if (AICounter < 10)
                {
                    Projectile.velocity += (target.Center - Projectile.Center).normalize() * 4f;
                    if (Projectile.velocity.Length() < 20)
                        Projectile.velocity = Projectile.velocity.normalize() * 20;
                }
                else
                {
                    Projectile.velocity += ((target.Center + (Projectile.Center - target.Center).normalize() * 250) - Projectile.Center) * 0.006f;
                }
                if (AICounter > 120)
                    NextAI();
                Projectile.pushByOther(0.7f);
                Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
            }
            if (aiStyle == AIStyle.Swing)
            {
                Projectile.velocity *= 0.9f;
                if (AICounter == 0)
                {
                    num1 = Main.rand.NextBool() ? 1 : -1;
                    vec1 = Projectile.Center;
                }
                float p = AICounter / 64f;
                if (p >= 1)
                    NextAI();
                else
                {
                    Projectile.Center = vec1 + (target.Center - vec1).RotatedBy((CEUtils.GetRepeatedCosFromZeroToOne(p, 2) - 0.5f) * 5.2f * num1) * CEUtils.Parabola(p, 1);
                    Projectile.rotation = (Projectile.Center - vec1).ToRotation();
                }
            }
            if (aiStyle == AIStyle.MultiSwing)
            {
                Projectile.velocity *= 0.9f;
                Projectile.velocity += ((target.Center + (Projectile.Center - target.Center).normalize() * 190) - Projectile.Center) * 0.007f;
                vec1 += Projectile.velocity;
                if (AICounter == 0)
                {
                    num1 = Main.rand.NextBool() ? 1 : -1;
                    vec1 = Projectile.Center;
                }
                Projectile.localNPCHitCooldown = 6;
                float p = AICounter / 130f;
                if (p >= 1)
                {
                    Projectile.localNPCHitCooldown = 30;
                    NextAI();
                }
                else
                {
                    Projectile.Center = vec1 + (target.Center - vec1).RotatedBy((CEUtils.GetRepeatedCosFromZeroToOne(p, 2) - 0.5f) * 18 * num1) * CEUtils.Parabola(p, 1);
                    Projectile.rotation = (Projectile.Center - vec1).ToRotation();
                }
            }
            if (aiStyle == AIStyle.Dash)
            {
                if(AICounter == 0)
                {
                    Projectile.velocity = (target.Center - Projectile.Center).normalize() * 24;
                    Projectile.ResetLocalNPCHitImmunity();
                }
                if(AICounter < 16)
                {
                    Projectile.velocity = Vector2.UnitX.RotatedBy((target.Center - Projectile.Center).ToRotation()) * Projectile.velocity.Length();
                    if (Projectile.Colliding(Projectile.getRect(), target.getRect()))
                        AICounter = 20;
                }
                if(AICounter > 30)
                {
                    Projectile.velocity *= 0.9f;
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (AICounter > 50)
                    NextAI();
            }
            if (aiStyle == AIStyle.Spin)
            {
                if (AICounter == 0)
                {
                    Projectile.localNPCHitCooldown = 10;
                    Projectile.ResetLocalNPCHitImmunity();
                }
                Projectile.velocity *= 0.96f;
                Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1.1f;
                Projectile.pushByOther(0.12f);
                Projectile.rotation += 0.6f;
                if (AICounter > 160)
                {
                    Projectile.velocity = CEUtils.randomPointInCircle(24);
                    Projectile.localNPCHitCooldown = 30;
                    NextAI();
                }
            }
            if (oldPos.Count > 2)
            {
                Vector2 vc = Projectile.Center - oldPos[oldPos.Count - 1];
                if(vc.Length() > 4)
                {
                    for(int i = 0; i < 3; i++)
                    {
                        GeneralParticleHandler.SpawnParticle(new CritSpark(Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(30, -30) * Projectile.scale, vc * Main.rand.NextFloat(-0.2f, -0.05f) * float.Min(1, vc.Length() * 0.14f), Color.LightGreen * float.Min(1, vc.Length() * 0.14f), Color.Green, Main.rand.NextFloat(0.4f, 1.2f) * float.Min(1, vc.Length() * 0.14f), 16, 1, 1, 0.01f));
                    }
                }
            }
            AICounter++;
            if (aiStyle == AIStyle.Spin)
            {
                if (csAlpha < 1)
                    csAlpha += 0.1f;
            }
            else
            {
                if (csAlpha > 0)
                    csAlpha -= 0.1f;
            }
            oldPos.Add(Projectile.Center);
            oldRot.Add(Projectile.rotation);
            if(oldPos.Count > 30)
            {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (aiStyle == AIStyle.Spin)
                modifiers.SourceDamage *= 0.2f;
        }
        public Vector2 vec1 = Vector2.Zero;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AICounter);
            writer.Write((byte)aiStyle);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AICounter = reader.ReadInt32();
            aiStyle = (AIStyle)reader.ReadByte();
        }
        public AIStyle aiStyle = AIStyle.FollowOwner;
        public void NextAI()
        {
            AICounter = 0;
            int attackStyle = NPC.downedMoonlord ? 2 : (Main.hardMode ? 1 : 0);
            if (attackStyle == 0)
            {
                if (aiStyle == AIStyle.CloseTarget)
                    aiStyle = AIStyle.Swing;
                else
                    aiStyle = AIStyle.CloseTarget;
            }
            else if (attackStyle == 1)
            {
                if (aiStyle == AIStyle.Dash)
                    aiStyle = AIStyle.Spin;
                else if (aiStyle == AIStyle.CloseTarget)
                    aiStyle = AIStyle.Swing;
                else if (aiStyle == AIStyle.Swing)
                    aiStyle = AIStyle.Dash;
                else
                    aiStyle = AIStyle.CloseTarget;
            }
            else
            {
                if (aiStyle == AIStyle.MultiSwing)
                    aiStyle = AIStyle.Dash;
                else if (aiStyle == AIStyle.CloseTarget)
                    aiStyle = AIStyle.MultiSwing;
                else if (aiStyle == AIStyle.Dash)
                    aiStyle = AIStyle.Spin;
                else
                    aiStyle = AIStyle.CloseTarget;
            }
        }
        public float csAlpha = 0;
        public int num1 = 1;
        public int AICounter = 0;
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public enum AIStyle
        {
            FollowOwner,
            CloseTarget,
            Swing,
            Dash,
            Spin,
            MultiSwing
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = CEUtils.getExtraTex("PatchyTallNoise");
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 2)
            {
                List<ColoredVertex> ve = new List<ColoredVertex>();
                Color b = new Color(200, 255, 200) * float.Min(1, (Projectile.Center - oldPos[oldPos.Count - 2]).Length() * 0.1f);
                oldPos.Add(Projectile.Center);
                oldRot.Add(Projectile.rotation);
                for (int i = 0; i < oldRot.Count; i++)
                {
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + (new Vector2(36 * (i / ((float)oldRot.Count - 1)) * Projectile.scale, 0).RotatedBy(oldRot[i])),
                          new Vector3((i / ((float)oldRot.Count - 1)) * 10 + Main.GlobalTimeWrappedHourly * 6, 1, 1),
                          b * (i / (oldPos.Count - 1f))));
                    ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + (new Vector2(-24 * (i / ((float)oldRot.Count - 1)) * Projectile.scale, 0).RotatedBy(oldRot[i])),
                          new Vector3((i / ((float)oldRot.Count - 1)) * 10 + Main.GlobalTimeWrappedHourly * 6, 0, 1),
                          b * (i / (oldPos.Count - 1f))));
                }
                oldPos.RemoveAt(oldPos.Count - 1);
                oldRot.RemoveAt(oldRot.Count - 1);
                if (ve.Count >= 3)
                {
                    var gd = Main.graphics.GraphicsDevice;
                    SpriteBatch sb = Main.spriteBatch;
                    sb.End();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                    gd.Textures[0] = trail;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    trail = CEUtils.getExtraTex("Perlin");
                    gd.Textures[0] = trail;
                    gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearClamp);
            for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4 * 0.5f)
            {
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + i.ToRotationVector2() * 3, null, Color.White, Projectile.rotation + MathHelper.PiOver2, tex.Size() * 0.5f, Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver2, tex.Size() * 0.5f, Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            if(csAlpha > 0.016f)
            {
                Main.spriteBatch.UseAdditive();
                Texture2D cs = CEUtils.getExtraTex("CircularSmear");
                Main.spriteBatch.Draw(cs, Projectile.Center - Main.screenPosition, null, new Color(204, 255, 196) * csAlpha, Projectile.rotation + MathHelper.PiOver4, cs.Size() / 2, Projectile.scale * 0.66f, SpriteEffects.None, 0);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }
    }
}
