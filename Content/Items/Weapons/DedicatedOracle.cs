using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class DedicatedOracle : ModItem, ICEChargeWeapon
    {
        // 周期就绪 12 秒；未就绪时武器不可使用（原武器全部行为即大招）
        public CEChargeProfile ChargeProfile => CEChargeProfile.Periodic(12f);

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 76;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = -1;
            Item.damage = 500;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<DedicatedOracleSlashProj>();
            Item.shootSpeed = 8;
        }
        public NPC castTarget = null;
        public override bool CanUseItem(Player player)
        {
            NPC target = CEUtils.FindTarget_HomingProj(player, Main.MouseWorld, 360, null);
            castTarget = target;
            return CEChargeWeapon.IsReady(Item) && target != null;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (castTarget != null)
            {
                Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, castTarget.whoAmI);
                CEChargeWeapon.TryConsume(player, Item);
            }
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_MeldBlob, CEID.Item_SolarVeil))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_MeldBlob, 9)
                .AddIngredient(CEID.Item_SolarVeil, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.SpookyWood, 100)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class DedicatedOracleSlashProj : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/ImprisonmentEye";
        //黑幕光晕贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/blackg")]
        internal static Asset<Texture2D> BlackGlowTex;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public NPC target => ((int)Projectile.ai[0]).ToNPC();
        public Vector2 origPos = Vector2.Zero;
        public Vector2 lastPlrPos = Vector2.Zero;
        public Vector2 targetPos = Vector2.Zero;
        public override void AI()
        {
            if (target == null && !target.active)
            {
                Projectile.Kill();
                return;
            }
            Player player = Projectile.GetOwner();
            int counter = (int)Projectile.ai[1]++;
            if (counter == 0)
            {
                targetPos = target.Center;
                origPos = player.Center;
            }
            if (target == null || !target.active)
            {
                if (counter < 59)
                    Projectile.ai[1] = 59;
            }
            target.Center = targetPos;
            player.Entropy().immune = 52;
            player.itemTime = player.itemAnimation = 4;
            Projectile.Center = target.Center;
            if (counter <= 8)
            {
                Vector2 plrPos = Vector2.Lerp(origPos, Projectile.Center, counter / 8f);
                player.Center = plrPos;
                if (counter > 0)
                {
                    Vector2 from = lastPlrPos;
                    Vector2 to = plrPos;
                    for (float i = 0; i < 1; i += 0.05f)
                    {
                        //DOracleSlash旧Blend落NonPremultiplied第三桶,ShadeDash的c1/c2/TL Configure前赋
                        var slash = PRTLoader.NewParticle<PRT_SlashDarkRed>(Vector2.Lerp(from, to, i), (to - from) * 0.1f, Color.Red, Main.rand.NextFloat(0.14f, 0.16f));
                        slash.scw = 1f;
                        slash.Configure(1, true, PRTDrawModeEnum.AlphaBlend, (to - from).ToRotation(), 7);
                    }
                }
                lastPlrPos = plrPos;
            }
            else
            {
                player.Entropy().DontDrawTime = 2;
                player.Center = Projectile.Center;
            }
            if (counter < 60)
            {
                Projectile.timeLeft = 2;
                if (counter > 6 && counter % 4 == 0)
                {
                    CEUtils.SetShake(Projectile.Center, 4);
                    CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), player, Projectile.Center, Projectile.damage / 9, 280, Projectile.DamageType).ArmorPenetration = 200;

                    CEUtils.PlaySound("slice", Main.rand.NextFloat(0.9f, 1.2f), Projectile.Center, 16, 0.5f);
                    for (int i = 0; i < 3; i++)
                    {
                        //旧Blend既不是Additive也不是AlphaBlend,Configure传NonPremultipliedBlend落第三桶
                        var dash = PRTLoader.NewParticle<PRT_ShadeDashParticle>(player.Center, CEUtils.randomRot().ToRotationVector2() * 12, Color.White, Main.rand.NextFloat(0.4f, 0.6f));
                        dash.TL = Main.rand.Next(80, 102);
                        dash.c1 = Color.Red;
                        dash.c2 = Color.Black;
                        dash.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, 18);

                        float rot = CEUtils.randomRot();
                        PRTLoader.NewParticle<PRT_DOracleSlash>(Projectile.Center, Vector2.Zero, Color.Red, Main.rand.NextFloat(250, 280)).Configure(1, true, PRTDrawModeEnum.NonPremultiplied, rot, 8);
                    }
                }
            }
            if (counter == 0) CEUtils.PlaySound("swing4", 1.6f, Projectile.Center);
            if (counter == 60)
            {
                for (int i = 0; i < 16; i++)
                {
                    var vel = (-Vector2.UnitY).RotatedByRandom(2.2f) * Main.rand.NextFloat(8, 15);
                    var dash = PRTLoader.NewParticle<PRT_ShadeDashParticle>(player.Center, vel * 2, Color.White, Main.rand.NextFloat(0.9f, 1.2f));
                    dash.TL = Main.rand.Next(40, 72);
                    dash.c1 = Color.Red;
                    dash.c2 = Color.Black;
                    dash.Configure(1, true, PRTDrawModeEnum.AlphaBlend, 0, 18);
                    PRTLoader.NewParticle<PRT_SlashDarkRed>(player.Center, vel, Color.Red, Main.rand.NextFloat(0.5f, 1.4f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, vel.ToRotation(), 38);
                }
                CEUtils.PlaySound("AbyssalBladeLaunch", 1, Projectile.Center);
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), player, Projectile.Center, Projectile.damage * 2, 600, Projectile.DamageType).SetEmpowered();
                player.velocity = ((origPos - Projectile.Center) * new Vector2(1, 0.6f)).normalize() * 46;
                if (player.velocity.Y < -24)
                    player.velocity.Y = -24;
                player.Entropy().XSpeedSlowdownTime = 34;
                player.Entropy().gravAddTime = 34;
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake(player.velocity.normalize() * 4, 16));
                CalamityEntropy.Instance.screenShakeAmp = 4;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            int counter = (int)Projectile.ai[1];
            int eyeFrame = 0;
            if (counter < 4)
            {
                eyeFrame = 2;
            }
            else if (counter < 8)
            {
                eyeFrame = 1;
            }
            if (target != null)
            {
                int adjustDrawingYPos = -20 - target.height / 2;
                Vector2 drawPos = Projectile.Center + new Vector2(0, adjustDrawingYPos);
                Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, CEUtils.GetCutTexRect(tex, 3, eyeFrame, false), Color.White, 0, tex.Size() / 2f, CEUtils.CustomLerp2(float.Min(counter / 10f, 1)), SpriteEffects.None);
                float balpha = float.Min(1, counter / 16f);
                float bsize = 0.8f;
                for (float i = 1; i <= 1.6f; i += 0.05f)
                {
                    Main.spriteBatch.Draw(BlackGlowTex.Value, Projectile.Center - Main.screenPosition, null, Color.Black * 0.06f * balpha, Main.GameUpdateCount * 0.9f * (i - 0.9f), new Vector2(1200, 1200), (1 + (i - 1) * 0.1f) * bsize * 6f, SpriteEffects.None, 0);
                }
            }
            return false;
        }
    }

}
