using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    public class FetalDream : ModItem, IDevItem, IGetFromStarterBag
    {
        public string DevName => "银九";

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock)
                .AddIngredient(ItemID.BlackLens, 5)
                .AddIngredient<BloodOrb>(9)
                .AddIngredient<Bloodstone>(10)
                .AddCondition(Condition.BloodMoon)
                .AddCondition(Condition.NearWater)
                .DisableDecraft()
                .Register();
        }

        public override void SetDefaults()
        {
            Item.width = 90;
            Item.height = 90;
            Item.damage = 514;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<FetalDreamSlash>();
            Item.shootSpeed = 16;
            Item.DamageType = DamageClass.Default;
            Item.rare = ModContent.RarityType<PureGreen>();
            Item.Entropy().stroke = true;
            Item.Entropy().NameColor = Color.LightGreen;
            Item.Entropy().strokeColor = Color.DarkGreen;
            Item.Entropy().tooltipStyle = 4;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (var tl in tooltips)
            {
                if (tl.Mod == "Terraria" && tl.Name == "Damage")
                {
                    tl.Text = $"{Main.LocalPlayer.GetWeaponDamage(Item, true).ToString()} {Mod.GetLocalization("FetalDreamDamage").Value}";
                }
            }
        }

        public bool OwnAble(Player player, ref int count)
        {
            return StartBagGItem.NameContains(player, DevName) || StartBagGItem.NameContains(player, "阿九");
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.HasCooldown(FetalDreamCooldown.ID))
                damage *= 0.0098f;
        }
    }
    public class FetalDreamSlash : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/FetalDream";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 8;
            Projectile.timeLeft = 22 * 8;
        }
        public float rScale = 1;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 120 * Projectile.scale, targetHitbox, 32);
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if (Projectile.localAI[0]++ == 0)
            {
                Vector2 targetPos = player.Center;
                if (Main.myPlayer == Projectile.owner)
                {
                    NPC target = CEUtils.FindTarget_HomingProj(Projectile, Main.MouseWorld, 300);
                    if (target != null)
                    {
                        player.Entropy().screenPos = player.Center;
                        player.Entropy().screenShift = 1;
                        targetPos = target.Center;
                        while (targetPos.getRectCentered(player.width, player.height).Intersects(target.Hitbox))
                        {
                            targetPos += (target.Center - player.Center).SafeNormalize(Vector2.UnitX) * 4;
                        }

                        targetPos += (target.Center - player.Center).SafeNormalize(Vector2.UnitX) * 70;
                        Vector2 centerPoint = (player.Center + targetPos) / 2f + (targetPos - player.Center).normalize().RotatedBy(MathHelper.PiOver2) * CEUtils.getDistance(player.Center, targetPos) * 0.4f;
                        for (float i = 0; i <= 1; i += 0.1f)
                        {
                            Vector2 pos = CEUtils.Bezier(new List<Vector2>() { player.position, centerPoint, targetPos }, i);
                            EParticle.NewParticle(new PlayerShadowBlack() { alpha = 0.6f, plr = player }, pos, Vector2.Zero, Color.Black, 1, 0.5f, true, BlendState.AlphaBlend, 0, 120);
                        }
                    }

                    player.Center = targetPos;
                    if (target != null)
                    {
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((target.Center - player.Center).ToRotation());

                        player.velocity *= 0;
                    }
                }
                CEUtils.PlaySound("ExobladeBeamSlash", Main.rand.NextFloat(1.6f, 2f), Projectile.GetOwner().Center);
            }

            player.Entropy().CruiserAntiGravTime = 5;
            Projectile.ai[1]++;
            Projectile.Center = player.GetDrawCenter();
            float MaxUpdateTimes = 26 * Projectile.MaxUpdates;
            float progress = (Projectile.ai[1] / MaxUpdateTimes);
            float RotF = 4.5f;

            float slashP = 0.6f;
            float cr = MathHelper.ToRadians(0);
            float rot = progress <= 0.5f ? ((RotF * -0.5f + CEUtils.Parabola(progress, RotF + cr))) : ((RotF * 0.5f + cr - CEUtils.GetRepeatedCosFromZeroToOne(2 * (progress - 0.5f), 1) * cr));
            rot *= Projectile.velocity.X > 0 ? 1 : -1;
            Vector2 v = rot.ToRotationVector2() * new Vector2(1, slashP);
            Projectile.rotation = v.ToRotation() + Projectile.velocity.ToRotation();
            Projectile.Center = Projectile.GetOwner().GetDrawCenter();
            rScale = v.Length();
            odr.Add(Projectile.rotation);
            ods.Add(rScale);
            if (odr.Count > 42)
            {
                odr.RemoveAt(0);
                ods.RemoveAt(0);
            }
        }
        List<float> odr = new List<float>();
        List<float> ods = new List<float>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 6));
            EParticle.spawnNew(new DOracleSlash() { centerColor = Color.White }, target.Center - Projectile.velocity.ToRotation().ToRotationVector2() * 290, Vector2.Zero, new Color(255, 5, 5), 260, 1f, true, BlendState.Additive, Projectile.velocity.ToRotation(), 20);
            EParticle.spawnNew(new DOracleSlash() { centerColor = Color.White, widthMult = 0.4f }, target.Center - (Projectile.velocity.ToRotation() + MathHelper.PiOver4 * (Projectile.velocity.X > 0 ? 1 : -1)).ToRotationVector2() * 340, Vector2.Zero, new Color(255, 5, 5), 360, 1f, true, BlendState.Additive, Projectile.velocity.ToRotation() + MathHelper.PiOver4 * (Projectile.velocity.X > 0 ? 1 : -1), 20);

            CEUtils.PlaySound("ystn_hit", 2.7f, target.Center);
            var player = Projectile.GetOwner();
            target.AddBuff<MarkedforDeath>(12 * 60);
            if (!player.HasCooldown(FetalDreamCooldown.ID))
            {
                CalamityEntropy.FlashEffectStrength = 0.6f;
                player.AddCooldown(FetalDreamCooldown.ID, 4320);
                CEUtils.PlaySound("ThunderStrike", Main.rand.NextFloat(0.8f, 1.2f), target.Center, 6, 0.6f);
                int dmg = (int)(target.lifeMax * 0.025);
                var info = target.CalculateHitInfo(dmg, Projectile.velocity.X > 0 ? 1 : -1, false, 6, DamageClass.Default);
                info.Damage = dmg;
                target.StrikeNPC(info);
            }
            CalamityEntropy.FlashEffectStrength = 0.2f;
            player.Entropy().immune = 40;
            target.AddBuff<Koishi>(16 * 60);
            player.AddBuff(ModContent.BuffType<Koishi>(), 600);
            player.Entropy().DmgAdd20 = 300;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = CEUtils.getExtraTex("MotionTrail2");
            List<ColoredVertex> ve = new List<ColoredVertex>();
            float MaxUpdateTimes = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;
            float progress = (Projectile.ai[1] / MaxUpdateTimes);

            for (int i = 0; i < odr.Count; i++)
            {
                Color b = new Color(220, 255, 200);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(116 * Projectile.scale * ods[i], 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition,
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = ModContent.Request<Effect>("CalamityEntropy/Assets/Effects/SwordTrail", AssetRequestMode.ImmediateLoad).Value;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(255, 90, 90)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(200, 60, 60)).ToVector4());
                shader.Parameters["alpha"].SetValue(1 - progress);
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                trail = CEUtils.getExtraTex("SplitTrail");
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);

                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }

    }
    public class Koishi : ModBuff
    {
        public override void Update(NPC npc, ref int buffIndex)
        {
            if (Math.Abs(npc.velocity.X) > 16)
                npc.velocity.X = 16 * Math.Sign(npc.velocity.X);
            if (Math.Abs(npc.velocity.Y) > 16)
                npc.velocity.Y = 16 * Math.Sign(npc.velocity.Y);
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) *= 1.05f;
        }
    }
}
