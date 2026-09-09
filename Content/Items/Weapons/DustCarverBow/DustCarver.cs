using CalamityEntropy;
using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Items;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityEntropy.Content.Items.Weapons.DustCarverBow
{
    public class DustCarver : ModItem
    {
        public int LevelNow = 0;
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item.QuickDrawItemWithBloomToWorld(spriteBatch, Color.Red, ref scale, rotation);
            return false;
        }
        /// <summary>装灾厄走 3.33 的 16 段细档阶梯,无灾厄保持 4.0 常数 6 级</summary>
        public static int GetLevel()
        {
            if (!CERef.Has)
            {
                return 6;
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
            Check(NPC.downedBoss1);
            Check(NPC.downedBoss2 || CECal.DownedPerforator || CECal.DownedHiveMind);
            Check(CECal.DownedSlimeGod);
            Check(Main.hardMode);
            Check(CECal.DownedBrimstoneElemental);
            Check(CECal.DownedCalamitasClone(NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3));
            Check(NPC.downedPlantBoss);
            Check(CECal.DownedRavager);
            Check(NPC.downedAncientCultist);
            Check(NPC.downedMoonlord);
            Check(CECal.DownedProvidence(EDownedBosses.downedNihilityTwin));
            Check(CECal.DownedPolterghast);
            Check(CECal.DownedDoG(EDownedBosses.downedCruiser));
            Check(CECal.DownedYharon(EDownedBosses.downedCruiser));
            Check(CECal.DownedCalamitas(EDownedBosses.downedCruiser) && CECal.DownedExoMechs(EDownedBosses.downedCruiser));
            Check(CECal.DownedPrimordialWyrm);
            return Level;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string Get(string path)
            {
                return Mod.GetLocalization($"LegendaryAbility.{path}").Value;
            }
            tooltips.Replace("[LV]", LevelNow.ToString());
            foreach (var line in tooltips)
            {
                if (line.Text.StartsWith("$"))
                {
                    line.Text = line.Text.Replace("$", "");
                    line.OverrideColor = LevelNow > 1 ? Color.Yellow : Color.Gray;
                    if (LevelNow < 2 && line.Text.StartsWith("^"))
                    {
                        line.Text += $" {Get("General.Locked")} {Get("TlipocasScytheLegend.Downed.TLevel2")}";
                    }
                    line.Text = line.Text.Replace("^", "");
                }
                if (line.Text.StartsWith("%"))
                {
                    line.Text = line.Text.Replace("%", "");
                    line.OverrideColor = Main.hardMode ? Color.Yellow : Color.Gray;
                    if (!Main.hardMode)
                    {
                        line.Text += $" {Get("General.Locked")} {Get("TlipocasScytheLegend.Downed.TWOF")}";
                    }
                }
                if (line.Text.StartsWith("&"))
                {
                    bool flag = NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;
                    line.Text = line.Text.Replace("&", "");
                    line.OverrideColor = (flag) ? Color.Yellow : Color.Gray;
                    if (!flag)
                    {
                        line.Text += $" {Get("General.Locked")} {Get("TlipocasScytheLegend.Downed.TALLMECHBOSS")}";
                    }
                }

            }
            tooltips.Add(new TooltipLine(Mod, "Lore", Language.GetOrRegister("Mods.CalamityEntropy.LegendaryAbility.DCarverDia" + LevelNow.ToString()).Value) { OverrideColor = Color.Crimson });
        }
        public int SpiritCount => CERef.Has ? int.Min(6, GetLevel() / 2) : 4;
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 150;
            Item.damage = 25;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(1, 0);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = null;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<DustCarverHeld>();
            Item.shootSpeed = 18f;
            Item.useAmmo = AmmoID.Arrow;
            Item.channel = true;
            Item.noUseGraphic = true;
            LevelNow = 0;
            Item.Entropy().Legend = true;
            RecheckStats = true;
            CheckLevel(GetLevel());
        }
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[Item.shoot] < 1 && Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, (Main.MouseWorld - player.Center).normalize() * Item.shootSpeed, Item.shoot, 0, 0, player.whoAmI);
            }
            int spirit = ModContent.ProjectileType<CarverSpirit>();
            if (player.ownedProjectileCounts[spirit] < SpiritCount)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, (Main.MouseWorld - player.Center).normalize() * Item.shootSpeed, spirit, player.GetWeaponDamage(Item) / 8, 0, player.whoAmI);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }
        public override void UpdateInventory(Player player)
        {
            CheckLevel(GetLevel());
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_BloodOrb))
            {
                CreateRecipe()
                    .AddIngredient(CEID.Item_BloodOrb, 5)
                    .AddRecipeGroup(CERecipeGroups.evilBar, 4)
                    .AddIngredient(ItemID.Silk, 4)
                    .AddIngredient(ItemID.RichMahogany, 12)
                    .AddTile(TileID.WorkBenches)
                    .Register();
                return;
            }
            // 2026-08-31 平衡案:配方=血雨弓+消逝符石,虚空井合成
            CreateRecipe()
                .AddIngredient(ItemID.BloodRainBow)
                .AddIngredient(ModContent.ItemType<FadingRunestone>())
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
        public void CheckLevel(int lv)
        {
            if (LevelNow != lv || RecheckStats)
            {
                RecheckStats = false;
                LevelNow = lv;
                int dmg = 25;
                switch (lv)
                {
                    case 0: dmg = 25; break;
                    case 1: dmg = 32; break;
                    case 2: dmg = 42; break;
                    case 3: dmg = 52; break;
                    case 4: dmg = 70; break;
                    case 5: dmg = 75; break;
                    case 6: dmg = 80; break;
                    case 7: dmg = 95; break;
                    case 8: dmg = 120; break;
                    case 9: dmg = 130; break;
                    case 10: dmg = 180; break;
                    case 11: dmg = 215; break;
                    case 12: dmg = 280; break;
                    case 13: dmg = 375; break;
                    case 14: dmg = 480; break;
                    case 15: dmg = 900; break;
                    case 16: dmg = 1250; break;
                }

                Item.damage = dmg;
                Item.crit = lv;
                Item.knockBack = lv / 2;
                Item.scale = 1;
                Item.useTime = Item.useAnimation = 30;
                Item.Prefix(Item.prefix);
            }

        }
        public bool RecheckStats = true;
        public int GetUseTime(Player player)
        {
            int ret = 14;
            switch (LevelNow)
            {
                case 0: ret = 30; break;
                case 1: ret = 29; break;
                case 2: ret = 28; break;
                case 3: ret = 27; break;
                case 4: ret = 26; break;
                case 5: ret = 25; break;
                case 6: ret = 24; break;
                case 7: ret = 23; break;
                case 8: ret = 22; break;
                case 9: ret = 21; break;
                case 10: ret = 20; break;
                case 11: ret = 19; break;
                case 12: ret = 18; break;
                case 13: ret = 17; break;
                case 14: ret = 16; break;
                case 15: ret = 15; break;
                case 16: ret = 14; break;
            }
            ret = (int)((Item.useTime / 30f) * ret);
            return ret;
        }

        public override bool RangedPrefix()
        {
            return true;
        }
        public int PenetAddition => LevelNow / 4 + 1;
    }
    public class DustCarverHeld : ModProjectile
    {
        public float Charging = 0;
        public int ShootDelay = 0;
        public int ShootEffect = 0;
        public bool active = false;
        public int SpikeTimer = 0;
        public int BoltTimer = 60;
        public bool RMBLast = false;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = Projectile.height = 12;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(active);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            active = reader.ReadBoolean();
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            Projectile.StickToPlayer();
            player.SetHandRot(Projectile.rotation);

            if (!(player.HeldItem.ModItem is DustCarver) || player.dead)
            {
                Projectile.Kill();
                return;
            }
            int sprType = ModContent.ProjectileType<CarverSpirit>();

            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                player.itemTime = player.itemAnimation = 3;
                Projectile clost_ = null;
                float dist_ = 99999;
                foreach (var proj in Main.ActiveProjectiles)
                {
                    if (proj.owner == player.whoAmI && proj.type == sprType)
                    {
                        if (clost_ == null || CEUtils.getDistance(proj.Center, Main.MouseWorld) < dist_)
                        {
                            dist_ = CEUtils.getDistance(proj.Center, Main.MouseWorld);
                            clost_ = proj;
                        }
                    }
                }
                if (clost_ != null)
                    if (clost_.ModProjectile is CarverSpirit spirit)
                    {
                        spirit.white = 3;
                    }
            }
            if (Main.myPlayer == Projectile.owner && !RMBLast && Main.mouseRight && !player.mouseInterface)
            {
                if (player.ownedProjectileCounts[sprType] > 0)
                {

                    void Toggle(Projectile proj)
                    {
                        proj.ai[0] += 1;
                        if (proj.ai[0] > 2)
                        {
                            proj.ai[0] = 0;
                        }
                        //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
                        PRTLoader.NewParticle<PRT_PulseRing>(proj.Center, Vector2.Zero, Color.Crimson, 0.1f).Configure(0.4f, 18);
                        CEUtils.SyncProj(proj.whoAmI);
                        for (float i = 0; i <= 1f; i += 0.1f)
                            PRTLoader.NewParticle<PRT_AltLineCal>(Vector2.Lerp(Projectile.Center, proj.Center, i), (proj.Center - Projectile.Center).normalize() * 0.04f, Color.Crimson, 1.4f).Configure(false, 12);
                    }
                    SoundEngine.PlaySound(SoundID.Item4 with { PitchRange = (-0.4f, -0.2f) }, Projectile.Center);
                    List<Projectile> SetList = new();
                    Projectile clost = null;
                    float dist = 99999;
                    foreach (var proj in Main.ActiveProjectiles)
                    {
                        if (proj.owner == player.whoAmI && proj.type == sprType)
                        {
                            if (clost == null || CEUtils.getDistance(proj.Center, Main.MouseWorld) < dist)
                            {
                                dist = CEUtils.getDistance(proj.Center, Main.MouseWorld);
                                clost = proj;
                            }

                            SetList.Add(proj);
                        }
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                    {
                        if (clost != null)
                            Toggle(clost);
                    }
                    else
                    {
                        foreach (var proj in SetList)
                            Toggle(proj);
                    }
                }
            }
            RMBLast = Main.mouseRight;
            Vector2 particlePos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 32;
            if (sParticle != null)
            {
                sParticle.Position = particlePos;
                sParticle.Rotation = Projectile.rotation;
            }
            if (sParticle2 != null)
            {
                sParticle2.Position = particlePos;
                sParticle2.Rotation = Projectile.rotation;
            }
            if (Main.myPlayer == Projectile.owner)
            {
                bool fl = active;
                active = Main.mouseLeft && !player.mouseInterface;
                if (active != fl)
                {
                    Projectile.netSpam = 0;
                    Projectile.netUpdate = true;
                }
            }
            Projectile.timeLeft = 5;
            if (ShootEffect > 0)
                ShootEffect--;
            var dc = ((DustCarver)player.HeldItem.ModItem);
            int useTime = dc.GetUseTime(player);
            float chargeAdd = 1f / useTime;
            if (Charging > 0)
            {
                player.itemTime = player.itemAnimation = 3;
                Charging += chargeAdd;
            }
            else
            {
                if (active)
                {
                    Charging += chargeAdd;
                }
            }
            if (active)
            {
                if (Main.hardMode)
                {
                    SpikeTimer--;
                    if (SpikeTimer <= 10 && SpikeTimer % 2 == 0)
                    {
                        if (Main.myPlayer == Projectile.owner)
                        {
                            var vec = new Vector2(0, 900);
                            player.PickAmmo(player.HeldItem, out int projID, out float shootSpeed, out int damage, out float kb, out var ammoID, false);
                            var shoot = Projectile.Center + vec + CEUtils.randomPointInCircle(400);
                            var targetPos = Main.MouseWorld;
                            int type = ModContent.ProjectileType<CarverSpike>();
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), shoot, (targetPos - shoot).normalize() * 16, type, damage / 14, kb / 10, Projectile.owner);
                            shoot = Projectile.Center - vec + CEUtils.randomPointInCircle(400);
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), shoot, (targetPos - shoot).normalize() * 16, type, damage / 14, kb / 10, Projectile.owner);
                        }
                    }
                    if (SpikeTimer <= 0)
                    {
                        SpikeTimer = 32 - dc.LevelNow;
                    }
                }
                if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                {
                    if (BoltTimer-- == 1)
                    {
                        if (Main.myPlayer == Projectile.owner)
                        {
                            int type = ModContent.ProjectileType<CarverBolt>();

                            player.PickAmmo(player.HeldItem, out int projID, out float shootSpeed, out int damage, out float kb, out var ammoID, true);
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 18, CEUtils.randomPointInCircle(16), type, damage / 2, kb, Projectile.owner);
                        }
                    }
                    if (BoltTimer <= 0)
                    {
                        BoltTimer = 72 - dc.LevelNow * 4;
                    }
                }
            }
            if (Charging >= 1)
            {
                Charging = 0;
                ShootDelay = useTime / 4;
                sParticle = PRTLoader.NewParticle<PRT_HeavenfallStar2>(particlePos, Vector2.Zero, new Color(255, 40, 40), 4f);
                sParticle.Inverse = true;
                sParticle.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation, 18);
                sParticle2 = PRTLoader.NewParticle<PRT_HeavenfallStar2>(particlePos, Vector2.Zero, new Color(255, 160, 160), 3.5f);
                sParticle2.Inverse = true;
                sParticle2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.rotation, 18);
                CEUtils.PlaySound("DustCarverShoot", Main.rand.NextFloat(1.6f, 2f), Projectile.Center, 6, 0.6f);
                CEUtils.PlaySound("CarverShoot2", Main.rand.NextFloat(1.4f, 1.8f), Projectile.Center, 6, 0.6f);

                if (Main.myPlayer == Projectile.owner)
                {
                    player.PickAmmo(player.HeldItem, out int projID, out float shootSpeed, out int damage, out float kb, out var ammoID, false);
                    int p = Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), Projectile.Center, Projectile.rotation.ToRotationVector2() * shootSpeed, projID, damage, kb, Projectile.owner);
                    Projectile projectile = p.ToProj();
                    if (!projectile.usesLocalNPCImmunity)
                    {
                        projectile.usesLocalNPCImmunity = true;

                    }

                    projectile.localNPCHitCooldown = -1;
                    if (projectile.penetrate > 0)
                        projectile.penetrate += dc.PenetAddition;
                    DustCarverArrorGProj mproj = projectile.GetGlobalProjectile<DustCarverArrorGProj>();
                    mproj.HomingRange = 200 + dc.LevelNow * 40;
                    mproj.active = true;
                    CEUtils.SyncProj(p);
                }
            }
            Vector2 p1 = Projectile.Center - Projectile.rotation.ToRotationVector2() * 30 * Projectile.scale + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 66 * Projectile.scale;
            Vector2 p2 = Projectile.Center - Projectile.rotation.ToRotationVector2() * 30 * Projectile.scale - Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 66 * Projectile.scale;

            if (trail1 == null)
            {
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                trail1 = PRTLoader.NewParticle<PRT_TrailParticle>(p1, Vector2.Zero, new Color(180, 0, 0), 0.6f);
                trail1.maxLength = 19;
                trail1.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
                trail2 = PRTLoader.NewParticle<PRT_TrailParticle>(p2, Vector2.Zero, new Color(180, 0, 0), 0.6f);
                trail2.maxLength = 19;
                trail2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
            }
            trail1.Lifetime = 30;
            trail1.AddPoint(p1);

            trail2.Lifetime = 30;
            trail2.AddPoint(p2);
        }
        public PRT_HeavenfallStar2 sParticle;
        public PRT_HeavenfallStar2 sParticle2;
        public PRT_TrailParticle trail1;
        public PRT_TrailParticle trail2;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D eye = this.getTextureGlow();
            SpriteEffects effect = Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Vector2 origin = Projectile.velocity.X > 0 ? new Vector2(tex.Width / 2, 83) : new Vector2(tex.Width / 2, tex.Height - 83);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, effect, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            Color eyeColor = Color.White * (0.6f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2));
            Main.spriteBatch.Draw(eye, Projectile.Center - Main.screenPosition, null, eyeColor, Projectile.rotation, origin, Projectile.scale, effect, 0);
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            float stringOffset = 0;
            if (Charging > 0.5f)
            {
                stringOffset -= CEUtils.Parabola((Charging - 0.5f), 1) * 70 * Projectile.scale;
            }
            Vector2 p1 = Projectile.Center - Projectile.rotation.ToRotationVector2() * 24 + new Vector2(0, Projectile.velocity.X > 0 ? 64 : 64).RotatedBy(Projectile.rotation);
            Vector2 p2 = Projectile.Center - Projectile.rotation.ToRotationVector2() * 28 + new Vector2(0, Projectile.velocity.X <= 0 ? -64 : -64).RotatedBy(Projectile.rotation);
            Vector2 pc = Projectile.Center - Projectile.rotation.ToRotationVector2() * (26 - stringOffset);
            CEUtils.drawLine(p1, pc, Color.Crimson, 2, 2);
            CEUtils.drawLine(p2, pc, Color.Crimson, 2, 2);

            if (Charging > 0)
            {
                Texture2D star = CEExtraAssets.StarTexture_White;
                Vector2 sScale = Charging < 0.5f ? new Vector2(1, 0.8f) : new Vector2(1.8f, 0.7f);
                float sOffset = Charging < 0.5f ? 2 * (0.5f - Charging) : 0;
                float sAlpha = Charging / 0.5f * 0.4f + 0.6f;
                if (sAlpha > 1)
                    sAlpha = 1;
                Color c1 = new Color(60, 26, 26).MultiplyRGBA(new Color(1, 1, 1, sAlpha));
                Color c2 = new Color(255, 230, 230).MultiplyRGBA(new Color(1, 1, 1, sAlpha));

                float w = 64 * Projectile.scale;
                Vector2 pos = Projectile.Center - Projectile.rotation.ToRotationVector2() * (24 - stringOffset);
                Main.spriteBatch.Draw(star, pos + new Vector2(0, sOffset * w).RotatedBy(Projectile.rotation) - Main.screenPosition, null, c1, Projectile.rotation, star.Size() / 2f, sScale * 0.3f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(star, pos + new Vector2(0, sOffset * -w).RotatedBy(Projectile.rotation) - Main.screenPosition, null, c1, Projectile.rotation, star.Size() / 2f, sScale * 0.3f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(star, pos + new Vector2(0, sOffset * w).RotatedBy(Projectile.rotation) - Main.screenPosition, null, c2, Projectile.rotation, star.Size() / 2f, sScale * 0.26f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(star, pos + new Vector2(0, sOffset * -w).RotatedBy(Projectile.rotation) - Main.screenPosition, null, c2, Projectile.rotation, star.Size() / 2f, sScale * 0.26f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    public class DustCarverArrorGProj : GlobalProjectile
    {
        //箭矢贴图与拖尾用 Asset,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/DustArrow")]
        internal static Asset<Texture2D> DustArrowTex;
        public override bool InstancePerEntity => true;
        public bool active = false;
        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write(active);
            writer.Write(HomingRange);
        }
        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
        {
            active = reader.ReadBoolean();
            HomingRange = reader.ReadInt32();
        }
        public List<Vector2> oldPos = new();
        public Color ColorFunction(float completionRatio, Vector2 vertex)
        {
            return Color.Lerp(Color.Crimson, Color.DarkRed, MathHelper.Clamp(completionRatio * 0.8f, 0f, 1f)) * projectile.Opacity;
        }
        public Projectile projectile;
        public float WidthFunction(float completionRatio, Vector2 vertex)
        {
            float num = 22;
            float num2 = ((!(completionRatio < 0.1f)) ? MathHelper.Lerp(num, 0f, Utils.GetLerpValue(0.1f, 1f, completionRatio, clamped: true)) : ((float)Math.Sin(completionRatio / 0.1f * (MathF.PI / 2f)) * num + 0.1f));
            return num2 * projectile.Opacity * projectile.scale;
        }
        public bool init = true;
        public override bool PreAI(Projectile projectile)
        {
            if (active)
            {
                if (init)
                {
                    init = false;
                    projectile.MaxUpdates *= 4;
                }
                oldPos.Insert(0, projectile.Center + projectile.velocity.normalize() * 24);
                if (oldPos.Count > 24)
                    oldPos.RemoveAt(oldPos.Count - 1);

                if (NPC.downedBoss3 && HomingRange > 0)
                {
                    Homing = float.Lerp(Homing, 12, 0.01f);
                    NPC target = CEUtils.FindTarget_HomingProj(projectile, projectile.Center, HomingRange, (npc) => (projectile.localNPCImmunity[npc] == 0) && CEUtils.GetAngleBetweenVectors(projectile.velocity, (npc.ToNPC().Center - projectile.Center)) < MathHelper.ToRadians(112));

                    if (target != null)
                    {
                        projectile.velocity = projectile.velocity.RotatedBy(CEUtils.getRotateAngle(projectile.velocity.ToRotation(), (target.Center - projectile.Center).ToRotation(), 0.5f * Homing));
                    }
                }
                return true;
            }
            return true;
        }
        public int HomingRange = 0;
        public float Homing = 0;
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            this.projectile = projectile;
            if (!active)
            {
                return true;
            }
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityEntropy:ArtAttack"].SetShaderTexture(CEExtraAssets.Streak1Asset);
            GameShaders.Misc["CalamityEntropy:ArtAttack"].Apply();
            CEPrimitiveRenderer.RenderTrail(oldPos, new CEPrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ArtAttack"]), 180);
            Main.spriteBatch.ExitShaderRegion();
            Texture2D glow2 = CEExtraAssets.SpearArrowGlow2;
            Texture2D arrow = DustArrowTex.Value;
            float rot = projectile.velocity.ToRotation();
            Main.spriteBatch.Draw(arrow, projectile.Center - Main.screenPosition, null, Color.White * 0.6f, rot, arrow.Size() / 2f, projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            Main.spriteBatch.Draw(glow2, projectile.Center - Main.screenPosition, null, new Color(230, 4, 4), rot, glow2.Size() / 2f, new Vector2(2, 0.8f) * projectile.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow2, projectile.Center - Main.screenPosition, null, new Color(255, 180, 180), rot, glow2.Size() / 2f, new Vector2(2, 0.8f) * projectile.scale * 0.3f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(glow2, projectile.Center - Main.screenPosition - rot.ToRotationVector2() * 24, null, new Color(255, 180, 180), rot, glow2.Size() / 2f, new Vector2(4, 0.36f) * projectile.scale * 0.3f, SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (active)
            {
                CEUtils.PlaySound("CarverHit", Main.rand.NextFloat(1.4f, 1.6f), target.Center, 6, 0.2f);
                CEUtils.PlaySound("GrassSwordHit0", Main.rand.NextFloat(1.4f, 1.8f), target.Center, 6, 0.25f);
                CEUtils.PlaySound("bne_hit", Main.rand.NextFloat(1.2f, 1.4f), target.Center, 4, 0.8f);
                for (int i = 0; i < 12; i++)
                {
                    float prog = Main.rand.NextFloat();
                    Color clr = new Color(255, 24, 24);
                    var vel = projectile.velocity.normalize().RotatedBy(0.25f * prog * (Main.rand.NextBool() ? 1 : -1)) * 64 * (1.2f - prog) * Main.rand.NextFloat(0.2f, 1);
                    //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                    var hs = PRTLoader.NewParticle<PRT_HeavenfallStar2>(target.Center, vel, clr, (1.2f - prog) * 1.2f);
                    hs.drawScale = new Vector2(0.4f, 1);
                    hs.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, vel.ToRotation(), 24);
                    hs = PRTLoader.NewParticle<PRT_HeavenfallStar2>(target.Center, vel, new Color(255, 200, 200), (1.2f - prog) * 0.7f * 1.2f);
                    hs.drawScale = new Vector2(0.4f, 1);
                    hs.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, vel.ToRotation(), 24);
                }
                PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 50, 50), 1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);
                PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(255, 255, 255), 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 6);

            }
        }
    }
}
