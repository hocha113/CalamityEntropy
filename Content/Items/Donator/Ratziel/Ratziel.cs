using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Donator.Ratziel
{
    public class Ratziel : ModItem, IGetFromStarterBag, IDonatorItem
    {
        public string DonatorName => "勿忘草与永远";

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 70;
            Item.damage = 7;
            Item.mana = 10;
            Item.useTime = Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.rare = ModContent.RarityType<ShiningViolet>();
            Item.value = 10000;
            Item.UseSound = SoundID.Item4;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RatzielSentry>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Summon;
            Item.sentry = true;
            Item.Entropy().Legend = true;
        }
        public static int MaxShield(int lv) => 10 + lv * 4;
        public static int Level()
        {
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
        public static int GetMaxTarget(int lv) => lv / 2 + 1;
        public static float TargetDist(int lv) => 600 + lv * 200;
        public override void UpdateInventory(Player player)
        {
            int level = Level();
            Item.damage = GetDamage(level);
        }
        public static int GetDamage(int level) => level switch
        {
            0 => 5,
            1 => 7,
            2 => 9,
            3 => 12,
            4 => 24,
            5 => 30,
            6 => 45,
            7 => 80,
            8 => 100,
            9 => 140,
            10 => 180,
            _ => 180
        };
        public override bool AllowPrefix(int pre)
        {
            return false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.type == type && proj.owner == player.whoAmI)
                {
                    proj.timeLeft = 1;
                    proj.netUpdate = true;
                }
            }
            position = Main.MouseWorld;
            int p = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, 20f);
            if (Main.projectile.IndexInRange(p))
                Main.projectile[p].originalDamage = Item.damage;
            player.UpdateMaxTurrets();
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Book)
                .AddIngredient(ItemID.ManaCrystal)
                .AddIngredient(ItemID.Sapphire, 5)
                .AddCondition(Mod.GetLocalization("NearShimmer", () => "Near shimmer"), () => (Main.LocalPlayer.ZoneShimmer))
                .Register();

        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Replace("[LV]", Level());
            tooltips.Replace("[MAXLV]", 10);
        }

        public bool OwnAble(Player player, ref int count)
        {
            return player.name == "本条二亚";
        }
    }
    public class RatzielSentry : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9000;
        }
        public override void SetDefaults()
        {
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.sentry = true;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.Opacity = 0;
            Projectile.scale = Ratziel.Level() * 0.1f + 1;
        }
        public List<Vector2> targetVecs = new List<Vector2>();
        int attackTimer = 36;
        public float rayAlpha = 0;
        public List<float> raySize = new List<float>();
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.velocity *= 1 - 0.38f * target.knockBackResist;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.Entropy().nextHitCrit = true;
            modifiers.CritDamage += -0.5f + Projectile.GetOwner().maxMinions * 0.05f;
        }
        public override void AI()
        {
            Projectile.GetOwner().Entropy().RatzielShieldTime = 3;
            if (Projectile.Opacity == 0)
            {
                EParticle.spawnNew(new HadCircle2() { CScale = 0.7f * Projectile.scale }, Projectile.Center, Vector2.Zero, Color.Yellow, 1, 1, true, BlendState.Additive);
                EParticle.spawnNew(new HadCircle2() { CScale = 1f * Projectile.scale }, Projectile.Center, Vector2.Zero, Color.Yellow, 1, 1, true, BlendState.Additive);
            }
            if (Main.GameUpdateCount % 30 == 0)
                GeneralParticleHandler.SpawnParticle(new PulseRing(Projectile.Center, Vector2.Zero, Color.Aqua * 0.5f, 1.2f * Projectile.scale, 1.7f * Projectile.scale, 80));
            if (Main.GameUpdateCount % 26 == 0)
                GeneralParticleHandler.SpawnParticle(new PulseRing(Projectile.Center, Vector2.Zero, Color.Aqua * 0.4f, 1.08f * Projectile.scale, 1.12f * Projectile.scale, 60));

            if (Projectile.Opacity < 1)
                Projectile.Opacity += 0.05f;
            Player player = Main.player[Projectile.owner];
            attackTimer--;
            if (attackTimer <= 0)
            {
                attackTimer = 36;
                rayAlpha = 0;
                targetVecs.Clear();
                raySize.Clear();
                var targets = FindNearestHostileNPCs(Projectile.Center, Ratziel.GetMaxTarget(Ratziel.Level()));
                foreach (var t in targets)
                {
                    raySize.Add(Main.rand.NextFloat(0.36f, 1.2f));
                    targetVecs.Add(t.Center + (t.Center - Projectile.Center).normalize() * Main.rand.NextFloat(80, 200));
                }
            }
            rayAlpha = float.Lerp(rayAlpha, (attackTimer > 12) ? 1 : 0, 0.08f);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            foreach (Vector2 v in targetVecs)
            {
                if (CEUtils.LineThroughRect(projHitbox.Center.ToVector2(), v, targetHitbox, 32))
                    return true;
            }
            return false;
        }
        public static List<NPC> FindNearestHostileNPCs(Vector2 center, int maxCount = 5)
        {
            if (maxCount <= 0)
                return new List<NPC>();
            var list = new List<NPC>();
            foreach (var npC in Main.ActiveNPCs)
                list.Add(npC);
            var result = list
                .Where(npc => npc.active
                           && !npc.friendly
                           && !npc.dontTakeDamage
                           && npc.lifeMax > 5
                           && npc.Distance(center) < Ratziel.TargetDist(Ratziel.Level()))
                .Select(npc => new
                {
                    NPC = npc,
                    DistanceSq = Vector2.DistanceSquared(center, npc.Center)
                })
                .OrderBy(x => x.DistanceSq)
                .Take(maxCount)
                .Select(x => x.NPC)
                .ToList();

            return result;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D tex2 = this.getTextureAlt("Book");
            Main.spriteBatch.Draw(tex, Projectile.Center + new Vector2(0, -40) * Projectile.scale - Main.screenPosition, null, new Color(140, 140, 255) * Projectile.Opacity, 0, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex2, Projectile.Center + new Vector2(0, 38) * Projectile.scale - Main.screenPosition, null, new Color(140, 140, 255) * Projectile.Opacity, 0, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            DrawForce();
            DrayRays();
            return false;
        }
        public void DrawForce()
        {
            string key = "Nebula";
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            float num232 = 0.2f;
            Terraria.Graphics.Effects.Filters.Scene[key].GetShader().UseIntensity(1.2f).UseProgress(0f);
            DrawData value61 = new DrawData(Main.Assets.Request<Texture2D>("Images/Misc/Perlin", (AssetRequestMode)1).Value, Projectile.Center - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle(0, 0, 600, 600), Microsoft.Xna.Framework.Color.White * Projectile.Opacity, Projectile.rotation, new Vector2(300f, 300f), new Vector2(1.05f, 0.7f) * Projectile.scale * 0.68f, SpriteEffects.None);
            GameShaders.Misc["ForceField"].UseColor(Color.LightSkyBlue.ToVector3());
            GameShaders.Misc["ForceField"].Apply(value61);
            value61.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
        public void DrayRays()
        {
            Texture2D ray = CEUtils.getExtraTex("GlowCone");
            Main.spriteBatch.UseAdditive();
            for (int i = 0; i < targetVecs.Count; i++)
            {
                Vector2 scaling = new Vector2(CEUtils.getDistance(targetVecs[i], Projectile.Center) / 360f, raySize[i] * 0.6f);
                Main.spriteBatch.Draw(ray, Projectile.Center - Main.screenPosition, null, Color.Aqua * 1.2f * rayAlpha, (targetVecs[i] - Projectile.Center).ToRotation(), new Vector2(0, ray.Height / 2), scaling, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
    }
}
