using CalamityEntropy.Content.NPCs.AbyssalWraith;
using CalamityEntropy.Content.Rarities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items
{
    /// <summary>
    /// 虚空祭印:深渊亡魂的可靠召唤物(progression-map §四)。
    /// 夜晚使用,在玩家上方展开仪式法阵完成召唤;虚空邪教徒仪式路径保留不动。
    /// </summary>
    public class AbyssalSigil : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 15;
        }

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 52;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.UseSound = CEUtils.GetSound("bell", 0.8f, 4, 0.9f);
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossItem;
        }

        public override bool CanUseItem(Player player)
        {
            return !Main.dayTime
                && !NPC.AnyNPCs(ModContent.NPCType<AbyssalWraith>())
                && player.ownedProjectileCounts[ModContent.ProjectileType<AbyssalSigilRitual>()] <= 0;
        }

        public override bool? UseItem(Player player)
        {
            // 仪式弹幕由所有者端生成并自动同步,亡魂本体在弹幕内由服务器端生成
            if (player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center + new Vector2(0, -110),
                    Vector2.Zero, ModContent.ProjectileType<AbyssalSigilRitual>(), 0, 0, player.whoAmI);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ModContent.ItemType<NihilityFragments>(), 8).
                AddIngredient(ItemID.Obsidian, 20).
                AddIngredient(ItemID.SoulofNight, 10).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }

    /// <summary>
    /// 祭印仪式法阵:短仪式版的召唤法阵,视觉复用 VoidRitualCircle 贴图,
    /// 涨光约 3.5 秒后由服务器端生成深渊亡魂,随后淡出。
    /// </summary>
    public class AbyssalSigilRitual : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Projectiles/VoidRitualCircle";

        /// <summary>仪式蓄能帧数,走完即召唤。</summary>
        public const int RitualTime = 210;

        public float alpha = 0;
        public bool summoned = false;
        private float rotCount = 0;

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 15000;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;

        public override void AI()
        {
            Projectile.ai[0]++;
            if (Projectile.ai[0] <= RitualTime)
            {
                alpha = Projectile.ai[0] / RitualTime;
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + CEUtils.randomPointInCircle(90 * alpha),
                        DustID.DungeonSpirit, new Vector2(0, -Main.rand.NextFloat(1f, 3f)), 0, default, Main.rand.NextFloat(0.8f, 1.4f));
                    d.noGravity = true;
                }
            }
            else if (!summoned)
            {
                summoned = true;
                SoundEngine.PlaySound(SoundID.Roar, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int np = NPC.NewNPC(new EntitySource_WorldEvent(), (int)Projectile.Center.X, (int)Projectile.Center.Y + 42, ModContent.NPCType<AbyssalWraith>());
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, np);
                }
            }
            else
            {
                alpha -= 0.025f;
                if (alpha <= 0)
                    Projectile.Kill();
            }
            Projectile.light = alpha * 2;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            rotCount += 0.16f;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White, rotCount, tex.Size() / 2, Projectile.scale * 2 * alpha, SpriteEffects.None, 0);
            return false;
        }
    }
}
