using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Malign
{
    public class Malign : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 23;
            Item.crit = 5;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 0;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 20);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<MalignHeld>();
            Item.shootSpeed = 16f;
            Item.mana = 5;
            Item.DamageType = DamageClass.Magic;
            Item.channel = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
        }
        public override bool MagicPrefix()
        {
            return true;
        }
        public override void HoldItem(Player player)
        {
            player.CheckAndSpawnHeldProj(Item.shoot);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_AshesofCalamity))
            {
                CreateRecipe()
                .AddIngredient(ItemID.CrystalSerpent)
                .AddIngredient(ItemID.Ectoplasm, 6)
                .AddIngredient(CEID.Item_AshesofCalamity, 4)
                .AddTile(TileID.MythrilAnvil)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.CrystalSerpent)
                .AddIngredient(ItemID.SpectreStaff)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
    public class MalignHeld : ModProjectile
    {
        //咬合贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Particles/Jaws")]
        internal static Asset<Texture2D> JawsTex;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return false;
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Malign/Malign";
        public Texture2D tCircle1 => this.getTextureAlt("Circle1");
        public Texture2D tCircle2 => this.getTextureAlt("Circle2");
        public Texture2D tPart1 => this.getTextureAlt("P1");
        public Texture2D tPart2 => this.getTextureAlt("P2");
        public float ActiveProgress = 0;
        public bool MousePressed = false;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(MousePressed);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            MousePressed = reader.ReadBoolean();
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            Projectile.ai[1]--;
            if (player.HeldItem.ModItem is Malign && !player.dead)
            {
                Projectile.timeLeft = 2;
                Projectile.StickToPlayer();
                player.SetHandRot(Projectile.rotation);
                if (Main.myPlayer == Projectile.owner)
                {
                    if ((!player.mouseInterface && Main.mouseLeft) != MousePressed)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                    }
                    MousePressed = !player.mouseInterface && Main.mouseLeft;
                    if (MousePressed && ActiveProgress > 0.8f)
                    {
                        int cMana = int.Max(1, (int)(player.HeldItem.mana * player.manaCost));
                        player.channel = true;
                        if (player.manaRegenDelay < 16 && player.CheckMana(cMana, false))
                            player.manaRegenDelay = 16;
                        if (Projectile.ai[1] <= 0)
                        {
                            Projectile.ai[1] = player.HeldItem.useTime;
                            if (player.CheckMana(cMana, true))
                            {
                                PlayerLoader.OnConsumeMana(player, player.HeldItem, cMana);
                                Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), Projectile.Center + Projectile.rotation.ToRotationVector2() * 90, Projectile.velocity.RotatedByRandom(0.4f) * 2, ModContent.ProjectileType<MalignBullet>(), player.GetWeaponDamage(player.HeldItem), player.GetWeaponKnockback(player.HeldItem), player.whoAmI);
                            }
                        }
                    }
                }
                if (MousePressed)
                {
                    if (ActiveProgress < 1)
                    {
                        ActiveProgress = float.Lerp(ActiveProgress, 1, 0.1f);
                    }

                }
                else
                {
                    if (ActiveProgress > 0)
                    {
                        ActiveProgress = float.Lerp(ActiveProgress, 0, 0.1f);
                    }
                }
                if (MousePressed || ActiveProgress > 0.3)
                {
                    player.itemTime = player.itemAnimation = 3;
                }
            }
            else
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();

            Vector2 top = Projectile.Center + Projectile.rotation.ToRotationVector2() * (56 + 10 * ActiveProgress) * Projectile.scale;

            Main.EntitySpriteDraw(tPart1, top - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver4) * (ActiveProgress * 18 - 12), null, Color.White * 0.5f * ActiveProgress, Projectile.rotation + MathHelper.PiOver4 + 0.3f * ActiveProgress, new Vector2(0, tPart1.Height / 2), Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tPart2, top - Main.screenPosition + Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.PiOver4) * (ActiveProgress * 18 - 12), null, Color.White * 0.5f * ActiveProgress, Projectile.rotation + MathHelper.PiOver4 + -0.3f * ActiveProgress, new Vector2(tPart2.Width / 2, tPart2.Height), Projectile.scale, SpriteEffects.None);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4, new Vector2(6, tex.Height - 6), Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tCircle1, Projectile.Center + Projectile.rotation.ToRotationVector2() * 94 * Projectile.scale - Main.screenPosition, null, Color.White * (ActiveProgress * ActiveProgress * ActiveProgress * 0.8f), Main.GlobalTimeWrappedHourly * 16, tCircle1.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tCircle2, Projectile.Center + Projectile.rotation.ToRotationVector2() * 94 * Projectile.scale - Main.screenPosition, null, Color.White * (ActiveProgress * ActiveProgress * ActiveProgress * 0.8f), Main.GlobalTimeWrappedHourly * -16, tCircle2.Size() / 2f, Projectile.scale, SpriteEffects.None);
            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 94 * Projectile.scale, new Color(255, 200, 255) * 0.66f * (ActiveProgress * ActiveProgress * ActiveProgress), 1.2f);

            /*Main.spriteBatch.UseBlendState(BlendState.Additive);
            Texture2D jaw = JawsTex.Value;
            Main.spriteBatch.Draw(jaw, top - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 12, null, Color.MediumPurple * ActiveProgress, Projectile.rotation + MathHelper.PiOver2, jaw.Size() / 2f, 0.42f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();*/

            return false;
        }
    }
    public class MalignLaser : ModProjectile
    {
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 8;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.light = 2;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void AI()
        {
            if (Projectile.localAI[2]++ == 0)
            {
                for (float i = 0; i <= 1; i += 0.005f)
                {
                    //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                    var hs = PRTLoader.NewParticle<PRT_HeavenfallStar2>(Projectile.Center + Projectile.velocity * i, Vector2.Zero, new Color(255, 40, 255), CEUtils.CustomLerp2(1 - i) * 0.7f + 0.1f);
                    hs.drawScale = new Vector2(0.2f, 1f);
                    hs.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 16);
                }
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 200, 255), 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 v = Projectile.velocity.RotateRandom(0.4f);
                    var hs = PRTLoader.NewParticle<PRT_HeavenfallStar2>(Projectile.Center, v * -0.0002f, new Color(255, 200, 255), 1.2f);
                    hs.drawScale = new Vector2(0.4f, 1.5f);
                    hs.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 16);
                    hs = PRTLoader.NewParticle<PRT_HeavenfallStar2>(Projectile.Center, v * -0.0002f, new Color(255, 200, 255), 1.2f);
                    hs.drawScale = new Vector2(0.4f, 1.5f);
                    hs.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, v.ToRotation(), 16);

                }
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity, targetHitbox, 32);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class MalignBullet : ModProjectile
    {
        public PRT_TrailParticle trail;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Magic, true, 1);
            Projectile.width = Projectile.height = 16;
            Projectile.timeLeft = 24;
            Projectile.light = 1;
        }
        public override void AI()
        {
            if (Projectile.localAI[2]++ == 0)
            {
                //旧对象初始化器拆成字段直赋+Configure,顺序别反
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity, Vector2.Zero, new Color(255, 190, 255), 0.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 5);
                CEUtils.PlaySound("malignShoot", Main.rand.NextFloat(0.8f, 1.4f), Projectile.Center, volume: 0.68f);
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (Projectile.timeLeft == 23 || Projectile.timeLeft == 13)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<MalignLightning>(), Projectile.damage / 2, Projectile.knockBack / 4, Projectile.owner);
                }
            }
            if (trail == null)
            {
                //轨迹类maxLength/SameAlpha字段Configure前先赋,PRTDrawMode只能走Configure
                trail = PRTLoader.NewParticle<PRT_TrailParticle>(Projectile.Center, Vector2.Zero, new Color(255, 100, 255), 1.2f);
                trail.maxLength = 12;
                trail.SameAlpha = true;
                trail.Configure(1, true, PRTDrawModeEnum.AdditiveBlend);
            }

            trail.AddPoint(Projectile.Center + Projectile.velocity);
            trail.Lifetime = 13;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 22)
            {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 340);
                if (target != null)
                {
                    Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (target.Center - Projectile.Center).ToRotation(), 0.12f, true).ToRotationVector2() * Projectile.velocity.Length();
                }
            }
            var lp = PRTLoader.NewParticle<PRT_ELineParticle>(Projectile.Center + CEUtils.randomPointInCircle(2), Projectile.velocity.RotatedByRandom(0.04f), new Color(255, 190, 255) * 0.8f, 2f);
            lp.width = 3.4f;
            lp.c = 0.86f;
            lp.r = 0.86f;
            lp.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, Projectile.velocity.ToRotation(), 6);

        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D glow = this.getTextureGlow();
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, new Color(255, 95, 255), Projectile.velocity.ToRotation(), glow.Size() / 2f, Projectile.scale * 0.14f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 95, 255), Projectile.velocity.ToRotation(), tex.Size() / 2f, Projectile.scale * 0.1f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 15;
        }
        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.normalize() * 500, ModContent.ProjectileType<MalignLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(255, 190, 255), 1).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);
        }
    }

}
