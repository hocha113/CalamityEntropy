using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
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

namespace CalamityEntropy.Content.Items.Weapons.GrassSword
{
    public class Bramblecleave : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 60;
            Item.useTime = 24;
            Item.crit = 12;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BramblecleaveHeld>();
            Item.shootSpeed = 12f;
            Item.Entropy().Legend = true;
            LastLevel = -1;
            UpdateInventory(Main.LocalPlayer);
        }
        public int useCounter = 0;
        public int atkType = 1;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<BrambleVine>()] > 0 || player.ownedProjectileCounts[ModContent.ProjectileType<BramblecleaveAlt>()] > 0)
            {
                return false;
            }
            if (player.altFunctionUse == 2)
            {
                bool flag = false;
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (!n.friendly && !n.dontTakeDamage)
                    {
                        if (CEUtils.LineThroughRect(position, position + velocity.normalize() * 1600, n.Hitbox, 30))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (!flag)
                {
                    NPC target = CEUtils.FindTarget_HomingProj(player, Main.MouseWorld, 600);
                    if (target == null)
                    {
                        flag = true;
                    }
                    else
                    {
                        velocity = (target.Center - position).normalize() * player.HeldItem.shootSpeed;
                    }
                }
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0, 1, 1);
                return false;
            }
            useCounter++;
            damage *= useCounter % 5 == 0 ? 2 : 1;
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType == 0 ? -1 : atkType, useCounter % 5 == 0 ? 1.6f : 1);
            atkType *= -1;
            return false;
        }
        public override bool AltFunctionUse(Player player)
        {
            return AllowLunge();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        private static float UpdatePos
        {
            get
            {
                return ((float)(MathF.Sin(Main.GlobalTimeWrappedHourly * 1f) * 1.2f + 1.4f)).ToClamp(1.0f, 2.4f);
            }
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item.QuickDrawItemWithBloomToWorld(spriteBatch, Color.LimeGreen, ref scale, rotation);
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_LoreAwakening, CEID.Item_PlantyMush))
            {
                CreateRecipe()
                    .AddIngredient(CEID.Item_LoreAwakening)
                    .AddIngredient(CEID.Item_PlantyMush, 10)
                    .AddIngredient(ItemID.JungleSpores, 6)
                    .AddTile(TileID.Anvils)
                    .Register();
                return;
            }
            // 2026-08-31 平衡案:时期移至月后,配方=草剑+种子弯刀+10夜明锭
            CreateRecipe()
                .AddIngredient(ItemID.BladeofGrass)
                .AddIngredient(ItemID.Seedler)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();

        }
        /// <summary>装灾厄走 3.33 的 15 段细档阶梯,无灾厄保持 4.0 常数 10 级</summary>
        public static int GetLevel()
        {
            if (!CERef.Has)
            {
                return 10;
            }
            int Level = 0;
            bool flag = true;
            void Check(bool f)
            {
                if (f && flag)
                {
                    Level++;
                }
                else
                {
                    flag = false;
                }
            }
            Check(NPC.downedSlimeKing);
            Check(NPC.downedBoss1);
            Check(CECal.DownedHiveMind || CECal.DownedPerforator);
            Check(CECal.DownedSlimeGod);
            Check(Main.hardMode);
            Check(NPC.downedMechBossAny);
            Check(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3);
            Check(NPC.downedPlantBoss);
            Check(NPC.downedGolemBoss);
            Check(NPC.downedMoonlord);
            Check(CECal.DownedProvidence(EDownedBosses.downedNihilityTwin));
            Check(CECal.DownedDoG(EDownedBosses.downedCruiser));
            Check(CECal.DownedYharon(EDownedBosses.downedCruiser));
            Check(CECal.DownedExoMechs(EDownedBosses.downedCruiser) || CECal.DownedCalamitas(EDownedBosses.downedCruiser));
            Check(CECal.DownedCalamitas(EDownedBosses.downedCruiser) && CECal.DownedExoMechs(EDownedBosses.downedCruiser));
            return Level;
        }
        public int LastLevel = -1;
        public override void UpdateInventory(Player player)
        {
            int level = GetLevel();
            if (LastLevel != level)
            {
                if (CERef.Has)
                {
                    int dmg = Item.damage;
                    switch (level)
                    {
                        case 0: dmg = 12; break;
                        case 1: dmg = 18; break;
                        case 2: dmg = 24; break;
                        case 3: dmg = 28; break;
                        case 4: dmg = 36; break;
                        case 5: dmg = 80; break;
                        case 6: dmg = 100; break;
                        case 7: dmg = 120; break;
                        case 8: dmg = 140; break;
                        case 9: dmg = 150; break;
                        case 10: dmg = 480; break;
                        case 11: dmg = 600; break;
                        case 12: dmg = 1250; break;
                        case 13: dmg = 1400; break;
                        case 14: dmg = 1600; break;
                        case 15: dmg = 3600; break;
                    }
                    Item.damage = dmg;
                }
                else
                {
                    Item.damage = 480;
                }
                Item.useTime = Item.useAnimation = int.Max(10, 16 - level / 4);
                LastLevel = level;
                Item.crit = level * 3;
                Item.knockBack = level / 2;
                Item.scale = 1;
                Item.Prefix(Item.prefix);
            }
            if (player.HeldItem == Item)
            {
                player.Entropy().BBarNoDecrease = 120;
                player.Entropy().MouseWorldListener = true;
            }
        }
        public override bool AllowPrefix(int pre)
        {
            return true;
        }
        public static bool AllowLunge()
        {
            if (ModContent.GetInstance<ServerConfig>().BramblecleaveAlwaysUnlockAllSkill)
                return true;
            return NPC.downedSlimeKing;
        }
        public static bool AllowPull()
        {
            if (ModContent.GetInstance<ServerConfig>().BramblecleaveAlwaysUnlockAllSkill)
                return true;
            return NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind;
        }
        public static bool AllowStick()
        {
            if (ModContent.GetInstance<ServerConfig>().BramblecleaveAlwaysUnlockAllSkill)
                return true;
            return Main.hardMode;
        }
        public static bool AllowSpin()
        {
            if (ModContent.GetInstance<ServerConfig>().BramblecleaveAlwaysUnlockAllSkill)
                return true;
            return NPC.downedPlantBoss;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[LV]", GetLevel());
            tooltips.Replace("[C1]", Mod.GetLocalization("BCC1").Value);
            tooltips.Replace("[C2]", Mod.GetLocalization("BCC2").Value);
            tooltips.Replace("[C3]", Mod.GetLocalization("BCC3").Value);
            tooltips.Replace("[C4]", Mod.GetLocalization("BCC4").Value);

            tooltips.Replace("[U1]", AllowLunge() ? "" : Mod.GetLocalization("LOCKED").Value + " " + Mod.GetLocalization("BCU1").Value);
            tooltips.Replace("[U2]", AllowPull() ? "" : Mod.GetLocalization("LOCKED").Value + " " + Mod.GetLocalization("BCU2").Value);
            tooltips.Replace("[U3]", AllowStick() ? "" : Mod.GetLocalization("LOCKED").Value + " " + Mod.GetLocalization("BCU3").Value);
            tooltips.Replace("[U4]", AllowSpin() ? "" : Mod.GetLocalization("LOCKED").Value + " " + Mod.GetLocalization("BCU4").Value);

            for (int i = 0; i < tooltips.Count; i++)
            {
                var tt = tooltips[i];
                if (tt.Text.StartsWith("@"))
                {
                    tt.Text = tt.Text.Replace("@", "");
                    tt.OverrideColor = AllowLunge() ? Color.Yellow : Color.Gray;
                }
                if (tt.Text.StartsWith("#"))
                {
                    tt.Text = tt.Text.Replace("#", "");
                    tt.OverrideColor = AllowPull() ? Color.Yellow : Color.Gray;
                }
                if (tt.Text.StartsWith("$"))
                {
                    tt.Text = tt.Text.Replace("$", "");
                    tt.OverrideColor = AllowStick() ? Color.Yellow : Color.Gray;
                }
                if (tt.Text.StartsWith("%"))
                {
                    tt.Text = tt.Text.Replace("%", "");
                    tt.OverrideColor = AllowSpin() ? Color.Yellow : Color.Gray;
                }
            }
        }
    }
    public class BramblecleaveHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/GrassSword/Bramblecleave";
        List<float> odr = new List<float>();
        List<float> ods = new List<float>();
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
            Projectile.MaxUpdates = 16;
        }
        public float rotRP = Main.rand.NextFloat(-0.1f, 0.1f);
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public bool RightHold = true;
        public bool LeftClicked = false;
        public bool Spin = false;
        public bool snd = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.GetOwner().Entropy().BrambleBarAdd = 20;
            if (snd)
            {
                CEUtils.PlaySound("GrassSwordHitMetal", Main.rand.NextFloat(0.7f, 1.3f) / Projectile.ai[1], target.Center, 10, CEUtils.WeapSound * 0.7f);
                if (target.Organic())
                {
                }
                else
                {
                    CEUtils.PlaySound("metalhit", Main.rand.NextFloat(0.8f, 1.2f) / Projectile.ai[1], target.Center, 6, CEUtils.WeapSound * 0.7f);
                }
                CEUtils.PlaySound("GrassSwordHit" + Main.rand.Next(4).ToString(), 1 / Projectile.ai[1], target.Center, 16, CEUtils.WeapSound * 0.7f);
                snd = false;
            }
            Color impactColor = Color.LightGreen;
            float impactParticleScale = Main.rand.NextFloat(1.4f, 1.6f);

            //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, impactParticleScale).Configure(Color.LawnGreen, 8, 0, 2.5f);


            float sparkCount = 16 + Bramblecleave.GetLevel() / 2;
            for (int i = 0; i < sparkCount; i++)
            {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(6, 20 * (2 - p)) * (1 + Bramblecleave.GetLevel() * 0.1f) * 0.7f;
                sparkVelocity2 = sparkVelocity2.RotatedBy((Projectile.ai[1] > 1 ? 0 : 1) * 0.42f * Projectile.ai[0]) * Projectile.ai[1];
                int sparkLifetime2 = (int)((2 - p) * 16);
                float sparkScale2 = 0.6f + (1 - p);
                sparkScale2 *= (1 + Bramblecleave.GetLevel() * 0.06f);
                Color sparkColor2 = Color.Lerp(Color.Green, Color.LightGreen, p);
                if (Main.rand.NextBool())
                {
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                }
                else
                {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2, Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2));
                }
            }
        }
        public float rScale = 1;
        public float slashP = Main.rand.NextFloat(0.5f, 0.7f);
        public override void AI()
        {
            CEUtils.AddLight(Projectile.Center + Projectile.velocity.normalize() * 20 * Projectile.scale, Color.LightGreen, Projectile.scale);
            Player owner = Projectile.GetOwner();
            if (owner.dead)
            {
                Projectile.Kill();
                return;
            }
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates * Projectile.ai[1];
            if (Projectile.ai[2] == 1)
            {
                MaxUpdateTimes = 26 * Projectile.MaxUpdates;
            }
            float progress = (counter / MaxUpdateTimes);
            counter++;
            if (init)
            {
                CEUtils.PlaySound("powerwhip", Projectile.ai[2] == 0 ? 1.75f / Projectile.ai[1] : 0.6f, Projectile.Center);
                Projectile.scale = 1.25f + 0.1f * Bramblecleave.GetLevel();
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                init = false;
                if (Main.myPlayer == Projectile.owner)
                {

                }
            }
            float p = Main.rand.NextFloat();
            rScale = 1;
            Projectile.timeLeft = 3;
            float RotF = 4.5f;
            alpha = 1;
            scale = 1f * Projectile.ai[1];
            float cr = MathHelper.ToRadians(10);

            if (Projectile.ai[2] == 0)
            {
                RightHold = false;
                float rot = progress <= 0.5f ? ((RotF * -0.5f + CEUtils.Parabola(progress, RotF + cr)) * Projectile.ai[0]) : ((RotF * 0.5f + cr - CEUtils.GetRepeatedCosFromZeroToOne(2 * (progress - 0.5f), 1) * cr) * Projectile.ai[0]);
                Vector2 v = rot.ToRotationVector2() * new Vector2(1, slashP / Projectile.ai[1]);
                Projectile.rotation = v.ToRotation() + Projectile.velocity.ToRotation() + rotRP;
                Projectile.Center = Projectile.GetOwner().GetDrawCenter();
                rScale = v.Length();

            }
            else
            {
                if (Projectile.ai[2] == 1)
                {
                    if (Main.myPlayer == Projectile.owner && progress < 1)
                    {
                        if (!Main.mouseRight)
                        {
                            RightHold = false;
                        }
                        if (Main.mouseLeft && Bramblecleave.AllowStick())
                        {
                            LeftClicked = true;
                            counter = MaxUpdateTimes + 1;
                            NoDraw = true;
                        }
                        if (!RightHold && Main.mouseRight && Bramblecleave.AllowSpin())
                        {
                            Spin = true;
                            counter = MaxUpdateTimes + 1;
                            NoDraw = true;
                        }
                    }
                    Projectile.Center = Projectile.GetOwner().GetDrawCenter() - Projectile.velocity.normalize() * (CEUtils.Parabola(progress * 0.5f, 32) + 24);
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (progress <= 1)
                    {
                        Vector2 position = Projectile.Center;
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((owner.mouseWorld() - Projectile.Center).ToRotation());
                        bool flag = false;
                        foreach (NPC n in Main.ActiveNPCs)
                        {
                            if (!n.friendly && !n.dontTakeDamage)
                            {
                                if (CEUtils.LineThroughRect(position, position + Projectile.velocity.normalize() * 1600, n.Hitbox, 30))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag)
                        {
                            NPC target = CEUtils.FindTarget_HomingProj(owner, Main.MouseWorld, 600);
                            if (target == null)
                            {
                                flag = true;
                            }
                            else
                            {
                                Projectile.velocity = (target.Center - position).normalize() * owner.HeldItem.shootSpeed;
                            }
                        }
                    }
                }
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

                if (Projectile.ai[2] == 1)
                {
                    if (Projectile.localAI[1]++ == 0)
                    {
                        if (Main.myPlayer == Projectile.owner)
                        {

                            if (Spin)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<BramblecleaveAlt>(), (int)(Projectile.damage * 0.66f), 0, Projectile.owner);
                            }
                            else
                            {
                                int VineType = 0;
                                if (Bramblecleave.AllowPull() && RightHold && Projectile.GetOwner().Entropy().BrambleBarCharge >= 0.2f)
                                {
                                    VineType = 1;
                                }
                                if (Bramblecleave.AllowStick() && LeftClicked)
                                {
                                    VineType = 2;
                                }
                                if (VineType == 1)
                                {
                                }
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<BrambleVine>(), Projectile.damage, 0, Projectile.owner, VineType);
                            }
                        }
                    }
                    if (progress >= 2.5f)
                    {
                        owner.itemTime = 0;
                        owner.itemAnimation = 0;
                        Projectile.Kill();
                        return;
                    }
                    else
                    {
                        Projectile.Center = Projectile.GetOwner().GetDrawCenter() - Projectile.velocity.normalize() * (CEUtils.Parabola((float.Clamp(progress - 1, 0, 1)) * 0.46f, 64) - 32 + 24);

                    }
                    if (progress > 2.4f)
                    {
                        NoDraw = true;
                    }
                }
                else
                {
                    owner.itemTime = 1;
                    owner.itemAnimation = 1;
                    Projectile.Kill();
                }
            }
            if (Projectile.ai[2] == 0)
            {

                if (progress < 0.45f)
                {
                    SpawnParticle();
                    odr.Add(Projectile.rotation);
                    ods.Add(rScale);
                    if (odr.Count > 120)
                    {
                        odr.RemoveAt(0);
                        ods.RemoveAt(0);
                    }
                }
                else
                {
                    if (odr.Count > 0)
                    {
                        ods.RemoveAt(0);
                        odr.RemoveAt(0);
                    }
                }
            }

        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public bool NoDraw = false;
        public Vector2 lPos = Vector2.Zero;
        public void SpawnParticle()
        {
            Vector2 vpos = Projectile.rotation.ToRotationVector2() * rScale * scale * Projectile.scale * 116;
            if (lPos == Vector2.Zero)
            {
                lPos = vpos;
                return;
            }

            Vector2 sparkVelocity2 = (vpos - lPos).normalize() * 4 * Projectile.ai[1];
            int sparkLifetime2 = (int)(Main.rand.NextFloat() * 16);
            float sparkScale2 = 0.6f * Main.rand.NextFloat();
            sparkScale2 *= (1 + Bramblecleave.GetLevel() * 0.06f);
            Color sparkColor2 = Color.Lerp(Color.Green, Color.LightGreen, Main.rand.NextFloat());
            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 116 * scale * Projectile.scale * rScale;
            if (Main.rand.NextBool())
            {
                //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
                PRTLoader.NewParticle<PRT_AltSpark>(pos, sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1.4f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
            }
            else
            {
                PRTLoader.NewParticle<PRT_LineCal>(pos, sparkVelocity2, Main.rand.NextBool() ? Color.LightGreen : Color.LimeGreen, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2));
            }
            //旧spawnNew第5参是Scale不是Opacity,0.1~0.2这个小值得走NewParticle第4参,Configure首参对齐旧a=1
            var p = PRTLoader.NewParticle<PRT_GlowLightParticle>(Projectile.Center + Projectile.rotation.ToRotationVector2() * 100 * scale * Projectile.scale * rScale * Main.rand.NextFloat(0.25f, 1), sparkVelocity2 * 0.2f, Color.LawnGreen, Main.rand.NextFloat(0.1f, 0.2f) * scale * Projectile.scale);
            p.lightColor = Color.LightGreen * 0.5f;
            p.HideTime = 16;
            p.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 20);
            lPos = vpos;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (NoDraw)
            {
                return false;
            }
            Texture2D tex = Projectile.GetTexture();
            Texture2D trail = CEExtraAssets.MotionTrail2;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);

            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(220, 255, 200);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(116 * Projectile.scale * scale * ods[i], 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(32 * Projectile.scale * scale * ods[i], 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CEEffectAssets.SwordTrail;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(90, 255, 90)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(60, 200, 60)).ToVector4());
                shader.Parameters["alpha"].SetValue(1 - progress);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                trail = CEExtraAssets.SplitTrail;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                Main.spriteBatch.ExitShaderRegion();
            }


            int dir = (int)(Projectile.ai[0]);
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha, rot, origin, Projectile.scale * scale * rScale, effect);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[2] == 1)
                return false;
            return null;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale * scale * rScale, targetHitbox, 64);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale * scale * rScale, 54, DelegateMethods.CutTiles);
        }
    }

}
