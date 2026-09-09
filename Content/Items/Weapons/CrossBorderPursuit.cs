using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class CrossBorderPursuit : ModItem, ICEChargeWeapon
    {
        // 周期就绪 10 秒；未就绪时武器不可使用（原武器全部行为即大招）
        public CEChargeProfile ChargeProfile => CEChargeProfile.Periodic(10f);

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
            Item.damage = 1300;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.shoot = ModContent.ProjectileType<CrossBorderPursuitProj>();
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
            if (CECal.CalChainReady(CEID.Item_AscendantSpiritEssence))
            {
                CreateRecipe()
                .AddIngredient<DedicatedOracle>()
                .AddIngredient<AnimaSola>()
                .AddIngredient<VoidBar>(5)
                .AddIngredient(CEID.Item_AscendantSpiritEssence, 2)
                .AddTile<VoidWellTile>()
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<DedicatedOracle>()
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
    public class CrossBorderPursuitProj : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/TheoEye";
        //魔法阵与锁链贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/TheoCircle")]
        internal static Asset<Texture2D> TheoCircleTex;
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/CrossBorderPursuitAlt")]
        internal static Asset<Texture2D> ChainTex;
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
            int MaxTime = 102;
            if (target == null || !target.active)
            {
                if (counter < MaxTime - 1)
                    Projectile.ai[1] = MaxTime - 1;
            }
            if (counter == 0)
            {
                targetPos = target.Center;
                origPos = player.Center;
            }
            target.Center = targetPos;
            player.Entropy().immune = 84;
            player.itemTime = player.itemAnimation = 4;
            Projectile.Center = target.Center;
            if (counter <= 6)
            {
                Vector2 plrPos = Vector2.Lerp(origPos, Projectile.Center, counter / 6f);
                player.Center = plrPos;
                if (counter > 0)
                {
                    Vector2 from = lastPlrPos;
                    Vector2 to = plrPos;
                    for (float i = 0; i < 1; i += 0.05f)
                    {
                        //scw/colorInside旧初始化器字段,Configure只管opacity+PRTDrawMode+rotation
                        var slash = PRTLoader.NewParticle<PRT_SlashDarkRed>(Vector2.Lerp(from, to, i), (to - from) * 0.1f, Color.LightSkyBlue, Main.rand.NextFloat(0.06f, 0.07f));
                        slash.scw = 1f;
                        slash.colorInside = Color.White;
                        slash.Configure(1, true, PRTDrawModeEnum.AlphaBlend, (to - from).ToRotation(), 7);
                    }
                }
                lastPlrPos = plrPos;
            }
            else
            {
                if (counter < MaxTime - 22)
                {
                    player.Entropy().DontDrawTime = 2;
                }
                player.Center = Projectile.Center;
            }
            if (counter < MaxTime)
            {
                Projectile.timeLeft = 2;
                if (counter > 6 && counter < MaxTime - 24 && counter % 3 == 0)
                {
                    if (counter % 9 == 0)
                    {
                        var shard = PRTLoader.NewParticle<PRT_PrismShard>(target.Center + CEUtils.randomPointInCircle(128), Vector2.Zero, Color.White, 1f);
                        shard.PixelPass = true;
                        shard.Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
                    }
                    CEUtils.SetShake(Projectile.Center, 4);
                    CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), player, Projectile.Center, Projectile.damage / 4, 280, Projectile.DamageType).ArmorPenetration = 200;
                    float rot = CEUtils.randomRot();
                    for (int i = 0; i < 24; i++)
                    {
                        var vp = PRTLoader.NewParticle<PRT_Void>(Projectile.Center + -rot.ToRotationVector2() * 180, (rot.ToRotationVector2() * 38 + CEUtils.randomPointInCircle(4)) * Main.rand.NextFloat(0.2f, 1), Color.White, 1f);
                        vp.Opacity = Main.rand.NextFloat(0.3f, 0.4f);
                        vp.shape = 4;
                        vp.vd = 0.96f;
                    }
                    //DOracleSlash旧Blend既不是Additive也不是AlphaBlend,迁移落NonPremultiplied桶
                    var ds = PRTLoader.NewParticle<PRT_DOracleSlash>(target.Hitbox.randomPoint(), Vector2.Zero, new Color(180, 180, 255), Main.rand.NextFloat(250, 280));
                    ds.centerColor = Color.White;
                    ds.PixelPass = true;
                    ds.Configure(0.6f, true, PRTDrawModeEnum.NonPremultiplied, rot, 8);

                    CEUtils.PlaySound("AntivoidDash", Main.rand.NextFloat(1.4f, 1.8f), Projectile.Center, 16, 0.5f);
                }
            }
            if (counter == MaxTime - 18)
            {
                for (int i = 0; i < 32; i++)
                {
                    var ps = target.Center + new Vector2(Main.rand.NextFloat(-120, 120), Main.rand.NextFloat(-80, 80));
                    PRTLoader.NewParticle<PRT_ShineParticle>(ps, (target.Center + new Vector2(0, -1000) - ps) / 12f, new Color(140, 140, 255), 0.5f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
                }
            }
            if (counter == MaxTime - 14)
            {
                var ds2 = PRTLoader.NewParticle<PRT_DOracleSlash>(target.Center + new Vector2(0, -2400), Vector2.Zero, new Color(80, 80, 255), Main.rand.NextFloat(250, 280));
                ds2.widthMult = 2;
                ds2.centerColor = Color.White;
                ds2.vel = 2;
                ds2.Configure(16f, true, PRTDrawModeEnum.NonPremultiplied, MathHelper.PiOver2, 16);

                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center - new Vector2(0, 1200), new Vector2(0, 26), ModContent.ProjectileType<CBPSmash>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            if (counter == 0) CEUtils.PlaySound("AntivoidDashHit", 1.2f, Projectile.Center);
            if (counter == MaxTime)
            {
                CEUtils.PlaySound("CastTriangles", 0.8f, Projectile.Center);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<NetherRiftCrack>(), Projectile.damage * 2, 1, Projectile.owner).ToProj().DamageType = Projectile.DamageType;
                for (int i = 0; i < 64; i++)
                {
                    var vp = PRTLoader.NewParticle<PRT_Void>(Projectile.Center, CEUtils.randomPointInCircle(16), Color.White, 1f);
                    vp.Opacity = Main.rand.NextFloat(0.6f, 0.8f);
                    vp.shape = 4;
                    vp.vd = 0.96f;
                }
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
            Texture2D circle = TheoCircleTex.Value;
            if (target != null)
            {
                int adjustDrawingYPos = -20 - target.height / 2;
                Vector2 drawPos = Projectile.Center + new Vector2(0, adjustDrawingYPos);
                float alpha = float.Min(counter / 12, 1);
                DrawChain(target.Center, alpha + 0.3f + (counter > 46 ? ((counter - 46) / 8f) : 0));
                Main.EntitySpriteDraw(circle, target.Center - Main.screenPosition, null, Color.White * (0.6f + 0.2f * (float)(Math.Cos(Main.GameUpdateCount * 0.15f))) * float.Min(1, counter / 12f), Main.GlobalTimeWrappedHourly * 1.2f, circle.Size() / 2f, Projectile.scale * 1.5f, SpriteEffects.None);

            }
            return false;
        }
        public void DrawEye()
        {
            Texture2D tex = Projectile.GetTexture();
            int counter = (int)Projectile.ai[1];
            if (target != null)
            {
                int adjustDrawingYPos = -20 - target.height / 2;
                Vector2 drawPos = Projectile.Center + new Vector2(0, adjustDrawingYPos);
                Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, null, Color.White, 0, tex.Size() / 2f, CEUtils.CustomLerp2(float.Min(counter / 10f, 1)), SpriteEffects.None);
            }
        }
        public void DrawChain(Vector2 center, float alpha)
        {
            Texture2D chain = ChainTex.Value;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            while (alpha > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float rot = -MathHelper.PiOver2 + MathHelper.TwoPi / 3 * i;
                    for (int j = 0; j < 32; j++)
                    {
                        Vector2 drawPos = center + rot.ToRotationVector2() * chain.Width * j;
                        Main.spriteBatch.Draw(chain, drawPos - Main.screenPosition, null, Color.White * ((32f - j) / 32f) * (float.Min(1, alpha)), rot, new Vector2(0, chain.Height / 2), 1, SpriteEffects.None, 0);
                    }
                }
                alpha -= 1;
            }
            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend);
        }
    }

}
