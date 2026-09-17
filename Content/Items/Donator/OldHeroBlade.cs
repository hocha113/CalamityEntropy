using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator
{
    /// <summary>
    /// 昔日的英雄之刃(捐赠者:littlefish)。终局近战剑,虚空井合成,配方吃一面腐化/猩红兔兔旗帜。
    /// 每次挥砍放出英雄残影(穿透剑气),每第四次挥砍改为巨大的昔日之斩;
    /// 剑刃本体或残影命中敌怪时,会从身后唤出两只英灵兔扑向目标。
    /// </summary>
    public class OldHeroBlade : ModItem, IDonatorItem
    {
        public string DonatorName => "littlefish";

        #region 调参旋钮
        //残影剑气伤害倍率、每几次挥砍触发昔日之斩、昔日之斩的倍率
        public const float PhantomDamageMult = 0.6f;
        public const int GreatSlashEvery = 4;
        public const float GreatSlashDamageMult = 1.5f;
        //英灵兔:每次唤出数量、伤害倍率、内置冷却帧
        public const int SpiritCount = 2;
        public const float SpiritDamageMult = 0.8f;
        public const int SpiritCooldown = 30;
        #endregion

        private int swingCount;
        private int spiritCooldown;

        public override void SetDefaults() {
            Item.width = 122;
            Item.height = 106;
            Item.damage = 780;
            Item.crit = 12;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7f;
            Item.scale = 1.15f;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(platinum: 2);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<HeroPhantomSlash>();
            Item.shootSpeed = 18f;
        }

        public override void UpdateInventory(Player player) {
            if (spiritCooldown > 0) {
                spiritCooldown--;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            swingCount++;
            bool great = swingCount % GreatSlashEvery == 0;
            int dmg = (int)(damage * (great ? GreatSlashDamageMult : PhantomDamageMult));
            Projectile.NewProjectile(source, player.MountedCenter, velocity, type, dmg, knockback, player.whoAmI, great ? 1f : 0f);
            if (great) {
                CEUtils.PlaySound("swing3", 0.85f, player.Center, 6, 0.9f);
            }
            return false;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
            TrySummonSpirits(player, target, Item);
        }

        /// <summary>剑刃或残影命中时唤出英灵兔,共用物品实例上的内置冷却;只在所有者端生成。</summary>
        public static void TrySummonSpirits(Player player, NPC target, Item weapon) {
            if (Main.myPlayer != player.whoAmI || weapon?.ModItem is not OldHeroBlade blade || blade.spiritCooldown > 0) {
                return;
            }
            blade.spiritCooldown = SpiritCooldown;
            int damage = (int)(player.GetWeaponDamage(weapon) * SpiritDamageMult);
            float kb = player.GetWeaponKnockback(weapon);
            for (int i = 0; i < SpiritCount; i++) {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector2 pos = player.MountedCenter + new Vector2(-player.direction * 50f, side * 34f);
                Vector2 vel = (target.Center - pos).SafeNormalize(Vector2.UnitX) * 4f + new Vector2(0, -5f - i);
                Projectile.NewProjectile(player.GetSource_ItemUse(weapon), pos, vel, ModContent.ProjectileType<HeroSpiritBunny>(), damage, kb, player.whoAmI, target.whoAmI);
            }
            CEUtils.PlaySound("SoulSpawn" + Main.rand.Next(2), 1.1f, player.Center, 4, 0.6f);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<VoidBar>(5)
                .AddIngredient(ItemID.CorruptBunnyBanner)
                .AddTile<VoidWellTile>()
                .Register();
            CreateRecipe()
                .AddIngredient<VoidBar>(5)
                .AddIngredient(ItemID.CrimsonBunnyBanner)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }

    /// <summary>
    /// 英雄残影:挥砍放出的弧形剑气。ai[0] = 1 为昔日之斩(更大、无限穿透)。
    /// </summary>
    public class HeroPhantomSlash : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Assets/Extra/slash";

        public const int Life = 42;
        public const int TrailLength = 7;
        public static readonly Color SteelColor = new Color(170, 215, 255);
        public static readonly Color EdgeColor = new Color(240, 250, 255);

        private bool Great => Projectile.ai[0] > 0.5f;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, 4);
            Projectile.width = Projectile.height = 60;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Life;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Projectile.localAI[0] = 1f;
                if (Great) {
                    Projectile.penetrate = -1;
                    Projectile.localNPCHitCooldown = 30;
                    Projectile.scale = 1.7f;
                    Vector2 center = Projectile.Center;
                    Projectile.width = Projectile.height = 110;
                    Projectile.Center = center;
                }
            }
            Projectile.velocity *= 0.985f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 12) {
                Projectile.Opacity = Projectile.timeLeft / 12f;
            }
            Lighting.AddLight(Projectile.Center, SteelColor.ToVector3() * 0.5f * Projectile.Opacity);
            if (Main.rand.NextBool(Great ? 1 : 2)) {
                Vector2 side = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-26f, 26f) * Projectile.scale;
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + side, Projectile.velocity * 0.2f, SteelColor, Main.rand.NextFloat(0.3f, 0.5f)).Configure(0.8f, lifetime: 14);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player owner = Projectile.GetOwner();
            OldHeroBlade.TrySummonSpirits(owner, target, owner.HeldItem);
            CEUtils.PlaySound("SwordHit" + Main.rand.Next(2), Main.rand.NextFloat(0.9f, 1.1f), target.Center, 6, 0.5f * CEUtils.WeapSound);
            for (int i = 0; i < 8; i++) {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.7f) * Main.rand.NextFloat(5f, 12f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, SteelColor, 0.05f).Configure(false, 14, new Vector2(0.4f, 1f), true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Vector2 origin = tex.Size() / 2f;
            float scale = Projectile.scale * 2.1f;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 1; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float fade = (1f - i / (float)Projectile.oldPos.Length) * Projectile.Opacity;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, pos, null, SteelColor * fade * 0.35f, Projectile.oldRot[i], origin, scale * (0.85f + 0.15f * fade), SpriteEffects.None);
            }
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos, null, SteelColor * Projectile.Opacity, Projectile.rotation, origin, scale * 1.08f, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, drawPos, null, EdgeColor * Projectile.Opacity, Projectile.rotation, origin, scale * 0.8f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    /// <summary>
    /// 英灵兔:昔日英雄的旧伴,以腐化/猩红兔的幽影形态从身后跃出扑向目标。ai[0] = 目标编号。
    /// 贴图取原版兔子 NPC 图集按世界邪恶类型着色,加法混合成幽灵态。
    /// </summary>
    public class HeroSpiritBunny : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;

        public const int Life = 100;
        public const float Accel = 0.9f;
        public const float MaxSpeed = 22f;
        public static readonly Color GhostColor = new Color(170, 210, 255);

        private int frame;
        private int frameTimer;

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, 1);
            Projectile.width = Projectile.height = 30;
            Projectile.timeLeft = Life;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            int idx = (int)Projectile.ai[0];
            NPC target = idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active && Main.npc[idx].CanBeChasedBy(Projectile) ? Main.npc[idx] : null;
            if (target == null) {
                target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 800f);
                if (target != null) {
                    Projectile.ai[0] = target.whoAmI;
                }
            }
            if (target != null) {
                Vector2 want = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * MaxSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.08f);
                if (Projectile.velocity.Length() < MaxSpeed) {
                    Projectile.velocity += want.SafeNormalize(Vector2.Zero) * Accel;
                }
            }
            else {
                Projectile.velocity.Y += 0.3f;
            }
            Projectile.spriteDirection = Projectile.velocity.X >= 0 ? 1 : -1;
            Projectile.rotation = Projectile.velocity.X * 0.02f;
            if (++frameTimer >= 4) {
                frameTimer = 0;
                frame++;
            }
            if (Projectile.timeLeft < 15) {
                Projectile.Opacity = Projectile.timeLeft / 15f;
            }
            Lighting.AddLight(Projectile.Center, GhostColor.ToVector3() * 0.4f);
            if (Main.rand.NextBool(2)) {
                PRTLoader.NewParticle<PRT_Light>(Projectile.Center + CEUtils.randomPointInCircle(8f), -Projectile.velocity * 0.1f, GhostColor, Main.rand.NextFloat(0.25f, 0.4f)).Configure(0.7f, lifetime: 16);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("soul", 1.3f, target.Center, 6, 0.5f);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, GhostColor, 1.4f).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, 0f, 10);
            for (int i = 0; i < 10; i++) {
                Vector2 vel = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, GhostColor, 0.045f).Configure(false, 12, new Vector2(0.4f, 1f), true);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            int npcType = WorldGen.crimson ? NPCID.CrimsonBunny : NPCID.CorruptBunny;
            Main.instance.LoadNPC(npcType);
            Texture2D tex = TextureAssets.Npc[npcType].Value;
            int frames = Math.Max(1, Main.npcFrameCount[npcType]);
            int frameHeight = tex.Height / frames;
            Rectangle rect = new Rectangle(0, frameHeight * (frame % frames), tex.Width, frameHeight);
            Vector2 origin = rect.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float a = Projectile.Opacity;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(tex, pos, rect, GhostColor * a * 0.9f, Projectile.rotation, origin, Projectile.scale * 1.1f, effects);
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(tex, pos, rect, Color.White * a * 0.55f, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }
}
