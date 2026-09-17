using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    /// <summary>
    /// 暗夜灵杖(捐赠者:The Fool)。月后一阶魔法法杖,远古操纵机合成。
    /// 左键放出两道夜灵,蛇行一段后追踪敌怪,命中附加暗影焰并落下夜痕,夜痕积满时天降永夜之星;
    /// 右键在光标处降下夜幕,幕中敌怪持续受创,夜灵在幕中命中伤害更高、落痕翻倍。夜间全部伤害提升。
    /// </summary>
    public class NightSpiritStaff : ModItem, IDonatorItem
    {
        public string DonatorName => "The Fool";

        #region 调参旋钮
        public const int WispsPerCast = 2;
        public const float WispSpread = 0.22f;
        //夜痕积满层数、永夜之星伤害倍率(相对夜灵)
        public const int MarksToNightfall = 4;
        public const float NightfallDamageMult = 4f;
        //夜幕:魔力、持续帧、半径、周期伤害倍率、幕中夜灵伤害倍率
        public const int VeilMana = 40;
        public const int VeilLife = 360;
        public const float VeilRadius = 200f;
        public const float VeilDamageMult = 0.5f;
        public const float VeilWispDamageMult = 1.5f;
        //夜间伤害加成
        public const float NightBonus = 0.15f;
        #endregion

        public override void SetStaticDefaults() {
            Item.staff[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 86;
            Item.height = 148;
            Item.damage = 310;
            Item.crit = 6;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 14;
            Item.useTime = Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item8;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.rare = ModContent.RarityType<NihilityBlue>();
            Item.shoot = ModContent.ProjectileType<NightWisp>();
            Item.shootSpeed = 12f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2) {
                return player.ownedProjectileCounts[ModContent.ProjectileType<NightVeil>()] <= 0;
            }
            return true;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult) {
            if (player.altFunctionUse == 2) {
                mult *= VeilMana / (float)Item.mana;
            }
        }

        public override float UseTimeMultiplier(Player player) => player.altFunctionUse == 2 ? 1.6f : 1f;

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
            if (!Main.dayTime) {
                damage += NightBonus;
            }
        }

        public override Vector2? HoldoutOrigin() => new Vector2(8f, 8f);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse == 2) {
                Vector2 pos = player.mouseWorld();
                Projectile.NewProjectile(source, pos, Vector2.Zero, ModContent.ProjectileType<NightVeil>(), (int)(damage * VeilDamageMult), knockback * 0.5f, player.whoAmI);
                CEUtils.PlaySound("VoidAnticipation", 0.9f, pos, 4, 0.7f);
                return false;
            }
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float speed = velocity.Length();
            for (int i = 0; i < WispsPerCast; i++) {
                float off = MathHelper.Lerp(-WispSpread, WispSpread, WispsPerCast == 1 ? 0.5f : i / (WispsPerCast - 1f));
                Vector2 vel = aim.RotatedBy(off) * speed * Main.rand.NextFloat(0.9f, 1.1f);
                //ai[0] = 蛇行相位,ai[1] = 蛇行方向
                Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi), i % 2 == 0 ? 1f : -1f);
            }
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.SpectreStaff)
                .AddIngredient<NihilityFragments>(10)
                .AddIngredient(ItemID.SoulofNight, 15)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    /// <summary>暗夜灵杖调色:夜紫到黑,边缘泛冷白。</summary>
    internal static class NightPalette
    {
        public static readonly Color Deep = new Color(60, 20, 110);
        public static readonly Color Violet = new Color(140, 80, 230);
        public static readonly Color Pale = new Color(215, 195, 255);
        public static readonly Color VeilFill = new Color(18, 6, 36);

        /// <summary>目标中心是否落在该玩家的任意一片夜幕里。</summary>
        public static bool InsideOwnerVeil(int owner, Vector2 point) {
            int type = ModContent.ProjectileType<NightVeil>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == owner && p.type == type && p.Distance(point) <= NightSpiritStaff.VeilRadius) {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 夜痕:夜灵命中留下的层数,一段时间不刷新则消散。层数由所有者端结算,积满时召下永夜之星。
    /// </summary>
    public class NightMarkNPC : GlobalNPC
    {
        public const int MarkLinger = 300;
        public override bool InstancePerEntity => true;

        public int Marks;
        public int Timer;

        public override void PostAI(NPC npc) {
            if (Timer > 0) {
                Timer--;
                if (Timer == 0) {
                    Marks = 0;
                }
            }
        }

        /// <summary>落下若干层夜痕;满层则清空并由调用方降下永夜之星,返回 true。</summary>
        public bool AddMarks(int count) {
            Marks += count;
            Timer = MarkLinger;
            if (Marks >= NightSpiritStaff.MarksToNightfall) {
                Marks = 0;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 夜灵:蛇行一段后追踪敌怪的暗紫幽魂,穿墙。ai[0] = 蛇行相位,ai[1] = 蛇行方向。
    /// </summary>
    public class NightWisp : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 170;
        public const int SnakeFrames = 18;
        public const float SnakeAmp = 2.4f;
        public const float HomingRange = 900f;
        public const int TrailLength = 14;

        private int time;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, 1);
            Projectile.width = Projectile.height = 22;
            Projectile.timeLeft = Life;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            time++;
            if (time > SnakeFrames) {
                CEUtils.HomeInOnNPC(Projectile, true, HomingRange, 15f, 18f);
            }
            //蛇行:沿速度法向做正弦位移,追踪期逐渐收敛
            float snake = time <= SnakeFrames ? 1f : Math.Max(0f, 1f - (time - SnakeFrames) / 30f);
            Vector2 perp = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
            Projectile.position += perp * (float)Math.Sin(time * 0.45f + Projectile.ai[0]) * SnakeAmp * snake * Projectile.ai[1];
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 20) {
                Projectile.Opacity = Projectile.timeLeft / 20f;
            }
            Lighting.AddLight(Projectile.Center, NightPalette.Violet.ToVector3() * 0.5f * Projectile.Opacity);
            PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(5f), -Projectile.velocity * 0.08f, NightPalette.Violet * Projectile.Opacity, Main.rand.NextFloat(0.3f, 0.5f)).Configure(0.8f, lifetime: 16);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (NightPalette.InsideOwnerVeil(Projectile.owner, target.Center)) {
                modifiers.SourceDamage *= NightSpiritStaff.VeilWispDamageMult;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.ShadowFlame, 180);
            bool inVeil = NightPalette.InsideOwnerVeil(Projectile.owner, target.Center);
            if (Main.myPlayer == Projectile.owner && target.GetGlobalNPC<NightMarkNPC>().AddMarks(inVeil ? 2 : 1)) {
                Vector2 spawn = target.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), -640f);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawn, Vector2.UnitY * 6f, ModContent.ProjectileType<NightfallStar>(), (int)(Projectile.damage * NightSpiritStaff.NightfallDamageMult), Projectile.knockBack * 2f, Projectile.owner, target.whoAmI);
            }
            CEUtils.PlaySound("soul", 0.9f, target.Center, 6, 0.45f);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, NightPalette.Violet, 1.3f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 10);
            for (int i = 0; i < 8; i++) {
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, NightPalette.Pale, 0.045f).Configure(false, 12, new Vector2(0.4f, 1f), true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D glow = CEExtraAssets.Glow2;
            Texture2D star = CEUtils.getExtraTex("Star2");
            Vector2 origin = glow.Size() / 2f;
            float a = Projectile.Opacity;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(glow, pos, null, NightPalette.Deep * fade * 0.55f * a, 0f, origin, 0.16f * fade, SpriteEffects.None);
            }
            Vector2 c = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, c, null, NightPalette.Deep * 0.9f * a, 0f, origin, 0.22f, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, c, null, NightPalette.Violet * a, 0f, origin, 0.13f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, c, null, NightPalette.Pale * a, Projectile.rotation, star.Size() / 2f, new Vector2(0.55f, 0.28f), SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    /// <summary>
    /// 永夜之星:夜痕积满时从上方坠向目标(ai[0])的暗星,抵达或命中时范围爆发。
    /// </summary>
    public class NightfallStar : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 90;
        public const float Speed = 26f;
        public const float BurstRadius = 150f;
        public const int TrailLength = 10;

        private bool burst;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, 1);
            Projectile.width = Projectile.height = 34;
            Projectile.timeLeft = Life;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            int idx = (int)Projectile.ai[0];
            NPC target = idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active ? Main.npc[idx] : null;
            if (target != null) {
                Vector2 want = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.12f);
                if (Projectile.Distance(target.Center) < 24f) {
                    Projectile.Kill();
                    return;
                }
            }
            else {
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, Speed, 0.1f);
            }
            Projectile.rotation += 0.2f;
            Lighting.AddLight(Projectile.Center, NightPalette.Violet.ToVector3() * 0.8f);
            for (int i = 0; i < 2; i++) {
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(10f), -Projectile.velocity * 0.1f, NightPalette.Violet, Main.rand.NextFloat(0.4f, 0.7f)).Configure(0.9f, lifetime: 18);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.ShadowFlame, 240);
        }

        public override void OnKill(int timeLeft) {
            if (burst) {
                return;
            }
            burst = true;
            CEUtils.PlaySound("VoidBomb", 1.1f, Projectile.Center, 4, 0.8f);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f }, Projectile.Center);
            if (Main.myPlayer == Projectile.owner) {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), Projectile.Center, Projectile.damage, BurstRadius, Projectile.DamageType);
            }
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, NightPalette.Violet, 0.1f).Configure(2.2f, 18);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, NightPalette.Deep, 5f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 20);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, NightPalette.Pale, 3f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 14);
            for (int i = 0; i < 24; i++) {
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6f, 16f);
                Color col = Main.rand.NextBool() ? NightPalette.Violet : NightPalette.Pale;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, col, 0.06f).Configure(false, 18, new Vector2(0.4f, 1f), true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D star = CEUtils.getExtraTex("Star2");
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 origin = star.Size() / 2f;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(glow, pos, null, NightPalette.Deep * fade * 0.6f, 0f, glow.Size() / 2f, 0.3f * fade, SpriteEffects.None);
            }
            Vector2 c = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, c, null, NightPalette.Deep, 0f, glow.Size() / 2f, 0.42f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, c, null, NightPalette.Violet, Projectile.rotation, origin, 1.1f, SpriteEffects.None);
            Main.EntitySpriteDraw(star, c, null, NightPalette.Pale, -Projectile.rotation * 0.7f, origin, 0.7f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    /// <summary>
    /// 夜幕:光标处降下的圆形暗域,幕中敌怪周期受创并附加暗影焰;夜灵在幕中命中伤害更高、落痕翻倍。
    /// </summary>
    public class NightVeil : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int FadeFrames = 20;
        public const int HitCooldown = 30;

        private float Radius => NightSpiritStaff.VeilRadius;

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.width = Projectile.height = (int)(NightSpiritStaff.VeilRadius * 2f);
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.timeLeft = NightSpiritStaff.VeilLife;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            int age = NightSpiritStaff.VeilLife - Projectile.timeLeft;
            Projectile.Opacity = Math.Min(1f, Math.Min(age / (float)FadeFrames, Projectile.timeLeft / (float)FadeFrames));
            Projectile.rotation += 0.004f;
            Lighting.AddLight(Projectile.Center, NightPalette.Violet.ToVector3() * 0.6f * Projectile.Opacity);
            if (Main.dedServ) {
                return;
            }
            //幕内缓慢上浮的星尘,幕沿偶发的一缕紫光
            if (Main.rand.NextBool(2)) {
                Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(Radius * 0.95f);
                PRTLoader.NewParticle<PRT_Light>(pos, new Vector2(0, -0.5f) + CEUtils.randomPointInCircle(0.2f), NightPalette.Pale * Projectile.Opacity, Main.rand.NextFloat(0.15f, 0.3f)).Configure(0.6f, lifetime: 40);
            }
            if (Main.rand.NextBool(4)) {
                float ang = CEUtils.randomRot();
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * Radius;
                PRTLoader.NewParticle<PRT_Light>(pos, ang.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 1.5f, NightPalette.Violet * Projectile.Opacity, Main.rand.NextFloat(0.3f, 0.5f)).Configure(0.8f, lifetime: 24);
            }
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.5f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 nearest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.Distance(nearest, Projectile.Center) <= Radius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.ShadowFlame, 120);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, NightPalette.Violet, 0.9f).Configure(0.8f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 8);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D circle = CEExtraAssets.Circle;
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 c = Projectile.Center - Main.screenPosition;
            float a = Projectile.Opacity;
            float diameter = Radius * 2f;
            float pulse = 1f + 0.02f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            //幕体:非预乘深色圆盘,像一块被抠掉的夜空
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);
            Main.spriteBatch.Draw(circle, c, null, NightPalette.VeilFill * (0.78f * a), 0f, circle.Size() / 2f, diameter / circle.Width * pulse, SpriteEffects.None, 0f);
            //幕沿:加法紫晕与两圈反向缓转的星辉
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(glow, c, null, NightPalette.Deep * (0.55f * a), 0f, glow.Size() / 2f, diameter / glow.Width * 1.25f * pulse, SpriteEffects.None, 0f);
            Texture2D star = CEUtils.getExtraTex("Star2");
            for (int i = 0; i < 6; i++) {
                float ang = Projectile.rotation * (i % 2 == 0 ? 1f : -1.4f) + MathHelper.TwoPi * i / 6f;
                Vector2 pos = c + ang.ToRotationVector2() * Radius * 0.97f;
                Main.spriteBatch.Draw(star, pos, null, NightPalette.Violet * (0.8f * a), ang, star.Size() / 2f, 0.35f, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
