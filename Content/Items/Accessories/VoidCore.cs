using CalamityEntropy.Content.Items.Weapons.Fractal;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Dash;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Accessories
{
    public class VoidCore : ModItem
    {
        public const int DashDamage = 200;
        public const float DashKnockback = 6f;
        public const int DashImmuneFrames = 12;
        public const int DashDuration = 20;
        public const float DashDistance = 25 * 16f;
        public const int DashCooldown = 30;
        /// <summary>每次冲刺首次撞击敌怪时释放一道虚空斩的基础伤害。</summary>
        public const int VoidSlashDamage = 750;
        public static int MaxShield = 60;
        public static int ShieldRecharge = 20 * 60;
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 60;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.defense = 8;
            Item.accessory = true;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Entropy().VoidShieldVisual = !hideVisual;
            player.Entropy().VoidCoreItem = Item;
            player.GetModPlayer<CEDashPlayer>().Offer(CEDashRegistry.Get<VoidCoreDash>());
        }
        public override void UpdateVanity(Player player)
        {
            player.Entropy().VoidShieldVisual = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[S]", MaxShield.ToString());
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_RuinousSoul))
            {
                CreateRecipe()
                .AddIngredient<AzafureDriverCore>()
                .AddIngredient<NihilityFragments>(10)
                .AddIngredient(CEID.Item_RuinousSoul, 6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            // 脱离灾厄:原 RuinousSoul×6 按 material-map 换虚无碎片并与原有 10 枚合并
            CreateRecipe()
                .AddIngredient<AzafureDriverCore>()
                .AddIngredient<NihilityFragments>(16)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class VoidCoreDash : CEDashEffect
    {
        public override string ID => "VoidCoreDash";
        public override int Priority => 12;
        public override bool HitsEnemies => true;
        public override int Duration => VoidCore.DashDuration;
        public override float Distance => VoidCore.DashDistance;
        public override int Cooldown => VoidCore.DashCooldown;
        public override float Curve => 1.6f;

        public override void OnStart(Player player, CEDashState state)
        {
            CEUtils.PlaySound("Dash2", Main.rand.NextFloat(0.7f, 0.85f), player.Center, 6, 0.6f);
            for (int i = 0; i < 14; i++)
            {
                PRTLoader.NewParticle<PRT_GlowSpark>(player.Center + CEUtils.randomPointInCircle(16), -state.Direction.RotatedByRandom(0.6f) * Main.rand.NextFloat(6f, 14f),
                    Color.Lerp(new Color(100, 100, 255), Color.LightBlue, Main.rand.NextFloat()), Main.rand.NextFloat(0.1f, 0.16f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, -state.Direction.ToRotation(), 18);
            }
        }

        public override void OnVisuals(Player player, CEDashState state)
        {
            float intensity = 1f - state.Progress;
            Vector2 axis = state.Direction;
            Vector2 back = -axis * Math.Max(6f, state.CurrentSpeed);

            int sparkCount = 2 + (int)(6 * intensity);
            for (int i = 0; i < sparkCount; i++)
            {
                PRTLoader.NewParticle<PRT_GlowSpark>(CEUtils.randomPointInCircle(18) + player.Center - back * Main.rand.NextFloat(), back.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.4f, 0.6f),
                    Color.Lerp(new Color(100, 100, 255), Color.LightBlue, Main.rand.NextFloat()), Main.rand.NextFloat(0.1f, 0.14f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, -axis.ToRotation(), 16);
            }
            int dustCount = 2 + (int)(4 * intensity);
            for (int i = 0; i < dustCount; i++)
            {
                float f = axis.ToRotation() + state.Timer / 5f;
                float radius = 15f + (float)Math.Cos(state.Timer / 3f) * 12f;
                Dust dust = Dust.NewDustPerfect(player.Center - axis * 24f + f.ToRotationVector2().RotatedBy(i / 5f * MathHelper.TwoPi) * radius, Main.rand.NextBool(5) ? DustID.BlueTorch : DustID.CosmicCarKeys);
                dust.alpha = 140;
                dust.noGravity = true;
                dust.velocity = back * 0.8f;
                dust.scale = 0.7f;
                dust.shader = GameShaders.Armor.GetSecondaryShader(player.cShield, player);
                Dust spark = Dust.NewDustPerfect(player.Center + CEUtils.randomVec(10f), Main.rand.NextBool(6) ? DustID.SparksMech : DustID.MinecartSpark, back.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.1f, 0.8f), 0, default, 0.7f);
                spark.alpha = 140;
                spark.noGravity = true;
                spark.shader = GameShaders.Armor.GetSecondaryShader(player.cShield, player);
            }
            for (int i = 0; i < 3; i++)
            {
                PRTLoader.NewParticle<PRT_LineCal>(CEUtils.randomPointInCircle(18) + player.Center - back * Main.rand.NextFloat(), back * Main.rand.NextFloat(0.4f, 0.6f), Color.LightBlue, Main.rand.NextFloat(0.6f, 1)).Configure(false, 8);
            }
            //AbyssalLine被EffectLoader捞起走RT合成,xadd/lx得spawn后赋
            var dashLine = PRTLoader.NewParticle<PRT_AbyssalLine>(player.Center - axis * 8f, Vector2.Zero, Color.LightBlue, 1);
            dashLine.xadd = 0.84f;
            dashLine.lx = 0.84f;
            dashLine.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, axis.ToRotation(), 26);
        }

        public override void OnHit(Player player, NPC npc, CEDashState state, ref CEDashHit hit)
        {
            float r = state.Direction.ToRotation();
            if (state.HitCount == 1)
            {
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, 6));
                SpawnVoidSlash(player, npc, state);
            }

            PRTLoader.NewParticle<PRT_ShineParticle>(player.Center, Vector2.Zero, Color.Blue, 1.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            PRTLoader.NewParticle<PRT_ShineParticle>(player.Center, Vector2.Zero, Color.White, 0.8f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
            SpawnHitLine(player.Center, new Color(30, 10, 50), 1.4f, 3.2f, PRTDrawModeEnum.NonPremultiplied, r, 30);
            SpawnHitLine(player.Center, new Color(80, 40, 120), 1.36f, 3.2f, PRTDrawModeEnum.NonPremultiplied, r, 30);
            SpawnHitLine(player.Center, Color.LightBlue, 1.34f, 3f, PRTDrawModeEnum.AdditiveBlend, r, 36);

            CEUtils.PlaySound("ExoHit" + Main.rand.Next(1, 5), Main.rand.NextFloat(1.6f, 1.9f), npc.Center, 6, 0.3f);

            hit.Damage = VoidCore.DashDamage;
            hit.Knockback = VoidCore.DashKnockback;
            hit.PlayerImmuneFrames = VoidCore.DashImmuneFrames;
            hit.DamageClass = DamageClass.Generic;
        }

        private static void SpawnHitLine(Vector2 pos, Color color, float xadd, float lx, PRTDrawModeEnum mode, float rotation, int lifetime)
        {
            var line = PRTLoader.NewParticle<PRT_AbyssalLine>(pos, Vector2.Zero, color, 1);
            line.xadd = xadd;
            line.lx = lx;
            line.Configure(1, true, mode, rotation, lifetime);
        }

        /// <summary>虚空斩:斜切过撞击点的一道斩击弹幕,一次冲刺只放一道。伤害在这里预套无职业加成,暴击由弹幕自己按无职业暴击率判。</summary>
        private static void SpawnVoidSlash(Player player, NPC npc, CEDashState state)
        {
            if (player.whoAmI != Main.myPlayer)
                return;
            int damage = (int)player.GetTotalDamage(DamageClass.Generic).ApplyTo(VoidCore.VoidSlashDamage);
            float tilt = Main.rand.NextFloat(0.55f, 0.9f) * (Main.rand.NextBool() ? 1 : -1);
            Vector2 axis = state.Direction.RotatedBy(tilt);
            Item coreItem = player.Entropy().VoidCoreItem;
            var source = coreItem != null ? player.GetSource_Accessory(coreItem) : player.GetSource_FromThis();
            int proj = Projectile.NewProjectile(source, npc.Center, axis, ModContent.ProjectileType<VoidCoreSlash>(), damage, 8f, player.whoAmI);
            CEUtils.SyncProj(proj);
            CEUtils.PlaySound("amethyst_break", 1, npc.Center, 6, 0.6f);
            CEUtils.PlaySound("AntivoidDash", 1, npc.Center, 6, 0.6f);
        }
    }

    /// <summary>
    /// 虚空核心的虚空斩。velocity 是斩击轴的单位向量,弹幕本身不位移;
    /// 只在前几帧有判定,之后只剩残影收束。
    /// </summary>
    public class VoidCoreSlash : ModProjectile
    {
        public const int LifeTime = 26;
        public const int ActiveFrames = 6;
        public const float HalfLength = 190f;
        public const float HalfWidth = 46f;

        public override string Texture => CEUtils.WhiteTexPath;

        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Generic, false, -1);
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.timeLeft = LifeTime;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        private float Life => 1f - Projectile.timeLeft / (float)LifeTime;
        private Vector2 Axis => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private Vector2 Start => Projectile.Center - Axis * HalfLength;
        private Vector2 End => Projectile.Center + Axis * HalfLength;

        public override void AI()
        {
            Projectile.friendly = Projectile.timeLeft > LifeTime - ActiveFrames;
            if (Projectile.timeLeft == LifeTime)
            {
                Vector2 axis = Axis;
                for (int i = 0; i < 24; i++)
                {
                    float along = Main.rand.NextFloat(-1f, 1f);
                    Vector2 pos = Projectile.Center + axis * along * HalfLength;
                    Vector2 vel = axis.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1 : -1)).RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 7f) * (1f - MathF.Abs(along) * 0.6f);
                    PRTLoader.NewParticle<PRT_GlowSpark>(pos, vel, Color.Lerp(new Color(160, 80, 255), Color.LightBlue, Main.rand.NextFloat()), Main.rand.NextFloat(0.08f, 0.14f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, vel.ToRotation(), 20);
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Start, End, targetHitbox, (int)HalfWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("ExoHit" + Main.rand.Next(1, 5), Main.rand.NextFloat(1.2f, 1.5f), target.Center, 6, 0.4f);
            var slash = PRTLoader.NewParticle<PRT_MultiSlash>(target.Center, Vector2.Zero, Color.LightBlue, 1);
            slash.xadd = 1f;
            slash.lx = 1f;
            slash.endColor = new Color(120, 40, 200);
            slash.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, -1);
        }

        private static void BuildLens(Vector2 start, Vector2 end, float halfWidth, int segments, List<Vector2> left, List<Vector2> right)
        {
            Vector2 normal = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float w = MathF.Pow(MathF.Sin(t * MathHelper.Pi), 0.7f) * halfWidth;
                Vector2 c = Vector2.Lerp(start, end, t);
                left.Add(c + normal * w);
                right.Add(c - normal * w);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float life = Life;
            // 前 20% 张开,之后收窄淡出
            float open = MathHelper.Clamp(life / 0.2f, 0f, 1f);
            float fade = 1f - MathHelper.Clamp((life - 0.2f) / 0.8f, 0f, 1f);
            float width = HalfWidth * open * (0.35f + 0.65f * fade);
            if (width <= 0.5f)
                return false;

            Vector2 start = Start;
            Vector2 end = End;
            var left = new List<Vector2>();
            var right = new List<Vector2>();

            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearClamp);
            BuildLens(start, end, width * 1.6f, 40, left, right);
            Color outer = new Color(90, 20, 160, 150) * fade;
            VoidSlash.DrawSlashPart(left, right, CEUtils.pixelTex, outer, outer);

            left.Clear();
            right.Clear();
            BuildLens(start, end, width, 40, left, right);
            VoidSlash.DrawSlashPart(left, right, CEUtils.pixelTex, new Color(180, 90, 255) * fade, new Color(230, 220, 255, 0) * fade);

            left.Clear();
            right.Clear();
            BuildLens(Vector2.Lerp(start, end, 0.15f), Vector2.Lerp(end, start, 0.15f), width * 0.45f, 40, left, right);
            VoidSlash.DrawSlashPart(left, right, CEUtils.pixelTex, Color.Black * fade, Color.Black * fade);

            CEUtils.ReSetToEndShader();
            return false;
        }
    }
}
