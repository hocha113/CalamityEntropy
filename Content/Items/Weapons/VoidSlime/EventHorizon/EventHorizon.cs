using CalamityEntropy.Content.Items.VoidSlime;
using CalamityEntropy.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.EventHorizon
{
    //左键原版挥砍加断钢剑式弧光并发射黑剑,右键仿混沌传送杖传送到鼠标处
    public class EventHorizon : ModItem
    {
        //时间单位帧,速度单位像素每次更新
        public const int UseFrames = 20;
        public const float BladeSpeed = 15f;
        //策划: 黑剑与引力冲击 75%,剑形幻影 66%,都按面板伤害算
        public const float BladeDamageMult = 0.75f;
        public const float PulseDamageMult = 0.75f;
        public const float PhantomDamageMult = 0.66f;
        //策划: 混沌状态 15 秒
        public const int ChaosStateFrames = 15 * 60;
        //原版混沌传送杖用的传送样式,两端带原版粉尘与音效
        public const int TeleportStyle = 1;
        //原版传送杖: 落点离世界边缘至少 50 像素
        private const float WorldEdgeMargin = 50f;
        //原版传送杖交给 CanBeTeleportedTo 的场景名,别的模组按它拦传送
        private const string TeleportContext = "TeleportRod";

        public override void SetDefaults() {
            Item.width = 64;
            Item.height = 64;
            Item.damage = 170;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 32);
            Item.shoot = ModContent.ProjectileType<EventHorizonBlade>();
            Item.shootSpeed = BladeSpeed;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) {
            bool teleport = player.altFunctionUse == 2;
            if (teleport && player.HasBuff(BuffID.ChaosState))
                return false;
            //右键举起不挥剑;只有拥有者知道是右键,别的客户端仍画成挥剑
            if (teleport) {
                Item.useStyle = ItemUseStyleID.HoldUp;
                Item.noMelee = true;
                Item.UseSound = null;
            }
            else {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = false;
                Item.UseSound = SoundID.Item1;
            }
            return true;
        }

        public override bool CanShoot(Player player) => player.altFunctionUse != 2;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //弧光照原版断钢剑的生成参数: ai0 挥向,ai1 挥砍总帧数,ai2 物品缩放
            Projectile.NewProjectile(source, player.MountedCenter, new Vector2(player.direction, 0f), ModContent.ProjectileType<EventHorizonSlash>(), damage, knockback, player.whoAmI, player.direction * player.gravDir, player.itemAnimationMax, player.GetAdjustedItemScale(Item));
            Projectile.NewProjectile(source, position, velocity, type, (int)(damage * BladeDamageMult), knockback, player.whoAmI);
            //原版生成弧光后同步一次朝向
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);
            return false;
        }

        public override bool? UseItem(Player player) {
            if (player.altFunctionUse != 2)
                return null;
            if (player.whoAmI == Main.myPlayer)
                TryTeleport(player);
            //返回 true 让原版当帧写入物品时间,否则下一帧会再传一次
            return true;
        }

        //落点算法与各项检查照原版 Player.ItemCheck_UseTeleportRod
        private void TryTeleport(Player player) {
            Vector2 target = default;
            target.X = Main.mouseX + Main.screenPosition.X;
            if (player.gravDir == 1f)
                target.Y = Main.mouseY + Main.screenPosition.Y - player.height;
            else
                target.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY;
            target.X -= player.width / 2;
            player.LimitPointToPlayerReachableArea(ref target);
            if (target.X <= WorldEdgeMargin || target.X >= Main.maxTilesX * 16 - WorldEdgeMargin || target.Y <= WorldEdgeMargin || target.Y >= Main.maxTilesY * 16 - WorldEdgeMargin)
                return;
            int i = (int)(target.X / 16f);
            int j = (int)(target.Y / 16f);
            if (Main.tile[i, j].WallType == WallID.LihzahrdBrickUnsafe && !NPC.downedPlantBoss && (Main.remixWorld || j > Main.worldSurface))
                return;
            if (Collision.SolidCollision(target, player.width, player.height) || !CombinedHooks.CanBeTeleportedTo(player, target, i, j, TeleportContext))
                return;
            Vector2 from = player.Center;
            player.Teleport(target, TeleportStyle);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, target.X, target.Y, TeleportStyle);
            player.AddBuff(BuffID.ChaosState, ChaosStateFrames);
            IEntitySource source = player.GetSource_ItemUse(Item);
            int hole = ModContent.ProjectileType<EventHorizonHole>();
            Projectile.NewProjectile(source, from, Vector2.Zero, hole, 0, 0f, player.whoAmI, EventHorizonHole.Entry);
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, hole, 0, 0f, player.whoAmI, EventHorizonHole.Exit);
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
            SpawnTerraBladeHit(target, new Vector2(player.direction, 0f), player.whoAmI);
        }

        //原版泰拉刃一族的命中闪光,落在目标碰撞箱内随机一点,clientOnly false 会同步给其他客户端
        internal static void SpawnTerraBladeHit(NPC target, Vector2 direction, int owner) {
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.TerraBlade, new ParticleOrchestraSettings {
                PositionInWorld = Main.rand.NextVector2FromRectangle(target.Hitbox),
                MovementVector = direction
            }, owner);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.InfluxWaver)
                .AddIngredient<SupernovaGel>(8)
                .AddIngredient<WraithSoulEssence>(20)
                .AddTile<GalaxySolidifierTile>()
                .Register();
        }
    }
}
