using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Cooldowns;
using InnoVault.PRT;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureProtectiveCannon : ModItem, IAzafureEnhancable
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Summon;
            Item.width = 44;
            Item.height = 38;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.knockBack = 6;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.shoot = ModContent.ProjectileType<AzafureProtectiveCannonShot>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(0, 5);
            Item.autoReuse = true;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.mana = 10;
            Item.rare = ModContent.RarityType<AzafureOrange>();
        }
        public static int Cooldown = 20 * 60;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            damage *= 16;
            player.AddCooldown(ProtectiveCannonCooldown.ID, Cooldown / (player.AzafureEnhance() ? 2 : 1));
            position = Main.MouseWorld + new Vector2(-600, -2000);
            Vector2 vel = (Main.MouseWorld - position) / 10f;
            Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI, Main.MouseWorld.X, Main.MouseWorld.Y);
            return false;
        }
        public override bool CanUseItem(Player player)
        {
            return !player.HasCooldown(ProtectiveCannonCooldown.ID);
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_MysteriousCircuitry))
            {
                CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(4)
                .AddIngredient(CEID.Item_MysteriousCircuitry)
                .AddIngredient(ItemID.HallowedBar, 6)
                .AddRecipeGroup(CERecipeGroups.IronBar, 6)
                .AddIngredient(ItemID.Wire, 12)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient<AzafureCircuitry>(4)
                .AddIngredient(ItemID.Wire, 20)
                .AddIngredient(ItemID.ChlorophyteBar, 12)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class AzafureProtectiveCannonShot : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 12;
            Projectile.timeLeft = 70;
        }
        public override bool ShouldUpdatePosition()
        {
            return Projectile.localAI[0] > 60;
        }
        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0)
            {
                CEUtils.PlaySound("Alarm", 1, Projectile.GetOwner().Center);
                //PRT_APRCAlarm 光效走AdditiveBlend,Configure尾参lifetime对齐旧timeLeft
                PRTLoader.NewParticle<PRT_APRCAlarm>(new Vector2(Projectile.ai[0], Projectile.ai[1]), Vector2.Zero, Color.White).Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, Projectile.timeLeft / Projectile.MaxUpdates);
            }
            if (Projectile.localAI[0] == 60)
                CEUtils.PlaySound("aprclaunch", 1, Projectile.GetOwner().Center);

            if (ShouldUpdatePosition())
            {
                //Smoke timeleftmax/Lifetime直赋+Configure,跟旧EParticle初始化器一一对应
                for (float i = 0; i < 1; i += 0.1f)
                {
                    var p = PRTLoader.NewParticle<PRT_Smoke>(Projectile.Center - Projectile.velocity * i, CEUtils.randomPointInCircle(0.5f), Color.OrangeRed, Main.rand.NextFloat(0.06f, 0.08f));
                    p.timeleftmax = 26;
                    p.Lifetime = 26;
                    p.Configure(0.6f, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot(), 26);
                    PRTLoader.NewParticle<PRT_EMediumSmoke>(Projectile.Center + Projectile.velocity * i, new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.2f, 0.2f)), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(0.8f, 1.4f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override void OnKill(int timeLeft)
        {
            CEUtils.PlaySound("explosionbig", 1, Projectile.Center, 4, 1.4f);
            CEUtils.PlaySound("pulseBlast", 0.6f, Projectile.Center, 4, 1.4f);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(3.2f, 20);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.Firebrick, 7.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 24);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 5.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 24);
            if (Projectile.owner == Main.myPlayer)
            {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center, Projectile.damage, 280, Projectile.DamageType);
            }
            for (int i = 0; i < 64; i++)
            {
                var d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Firework_Yellow);
                d.scale = 0.8f;
                d.velocity = CEUtils.randomPointInCircle(20);
                d.position += d.velocity * 4;
            }
            CEUtils.SetShake(Projectile.Center, 12, 6000);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!ShouldUpdatePosition())
            {
                return false;
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
    }
}
