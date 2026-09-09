using CalamityEntropy;
using CalamityEntropy.Content.Items.Armor.NihTwins;
using CalamityEntropy.Content.Items.Weapons.Miracle;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class WulfrumSniper : ModItem
    {
        public new string LocalizationCategory => "Items.Weapons.Ranged";

        public override bool RangedPrefix()
        {
            return true;
        }
        public override void SetDefaults()
        {
            Item.width = 134;
            Item.height = 38;
            Item.damage = 34;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 46;
            Item.reuseDelay = 4;
            Item.useAnimation = 46;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = CEUtils.GetSound("gunshot");
            Item.autoReuse = true;
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 6f;
            Item.useAmmo = AmmoID.Bullet;
            Item.crit = 8;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Main.zenithWorld)
                damage *= 160;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0, 0);
        }

        public int ShootCount = 0;
        #region Shooting
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (ShootCount < 5)
            {
                Projectile.NewProjectile(source, position + velocity.SafeNormalize(Vector2.Zero) * 50, velocity, type, damage, knockback, player.whoAmI);
                ShootCount++;
            }
            else
            {
                Item.noUseGraphic = true;
                ShootCount = 0;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<WulfrumSniperSpecialAttack>(), damage, knockback, player.whoAmI, 0, Item.scale);
            }
            return false;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_EnergyCore, CEID.Item_WulfrumMetalScrap, CEID.Item_MeldBlob, CEID.Item_AstralBar))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_EnergyCore, 2)
                .AddIngredient(CEID.Item_WulfrumMetalScrap, 8)
                .AddCondition(Mod.GetLocalization("NonZenithWorld"), () => !Main.zenithWorld)
                .AddTile(TileID.Anvils)
                .Register();
                CreateRecipe()
                .AddIngredient(CEID.Item_MeldBlob, 4)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddIngredient(CEID.Item_AstralBar, 8)
                .AddCondition(Mod.GetLocalization("ZenithWorld"), () => Main.zenithWorld)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddRecipeGroup(CERecipeGroups.IronBar, 10)
                .AddIngredient(ItemID.TungstenBar, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
        #endregion

        #region Animations
        public override void HoldItem(Player player) => player.Entropy().MouseWorldListener = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 76f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(36, 0);



            CEUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Entropy().MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.Entropy().MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.5)
                rotation += (-0.15f) * (float)Math.Pow((0.5f - animProgress) / 0.5f, 2) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);

            if (animProgress > 0.5f)
            {
                float backArmRotation = rotation + 0.52f * player.direction;

                Player.CompositeArmStretchAmount stretch = ((float)Math.Sin(MathHelper.Pi * (animProgress - 0.5f) / 0.36f)).ToStretchAmount();
                player.SetCompositeArmBack(true, stretch, backArmRotation);
            }

        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position += new Vector2(0, -8 * player.direction).RotatedBy((player.Entropy().MouseWorld - player.Center).ToRotation());
        }
        #endregion
    }
    public class WulfrumSniperSpecialAttack : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/WulfrumSniper";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.timeLeft = 60;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public int counter = 0;
        public float offset = 0;
        public bool thrown = false;
        public override void AI()
        {
            if (Projectile.GetOwner().dead)
            {
                Projectile.Kill();
                return;
            }
            Projectile.scale = Projectile.ai[1];
            if (counter == 0 || counter == 16)
            {
                offset = -12;
                CEUtils.PlaySound("gunshot_small" + Main.rand.Next(1, 4).ToString(), 1, Projectile.Center);
            }
            if (counter == 20)
            {
                thrown = true;
                Projectile.GetOwner().HeldItem.noUseGraphic = false;
                Vector2 fpos = Projectile.Center + Projectile.velocity.normalize() * 18 * Projectile.scale;
                for (int i = 0; i < 12; i++)
                {
                    //EParticle→PRT,EMediumSmoke Configure+PRTDrawMode AlphaBlend
                    //PRTDrawMode AlphaBlend桶,枪口烟别走Additive会糊
                    PRTLoader.NewParticle<PRT_EMediumSmoke>(fpos, Projectile.velocity.normalize().RotatedByRandom(1) * Main.rand.NextFloat(2, 9), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(0.7f, 1f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());
                }
                CEUtils.PlaySound("chainsaw_break", 1.4f, Projectile.Center);
                if (Main.myPlayer == Projectile.owner)
                {
                    int type = ModContent.ProjectileType<SniperWulfrumScrap>();
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), fpos, Projectile.velocity.normalize().RotatedByRandom(0.16f) * Main.rand.NextFloat(36, 42), type, (int)(Projectile.damage * 0.7f), Projectile.knockBack, Projectile.owner);
                    }
                    if (Main.zenithWorld)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), fpos, Projectile.velocity.normalize() * 12, ModContent.ProjectileType<Blackhole>(), (int)(Projectile.damage * 0.2f), Projectile.knockBack, Projectile.owner, 0, -1);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), fpos, Projectile.velocity.normalize() * 256, ModContent.ProjectileType<AbyssalCrack>(), (int)(Projectile.damage * 0.2f), Projectile.knockBack, Projectile.owner, 0, -1);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), fpos, Projectile.velocity.normalize() * 16, ModContent.ProjectileType<VENihilityLaser>(), (int)(Projectile.damage * 0.2f), Projectile.knockBack, Projectile.owner, 0, -1);
                    }
                }
                Projectile.GetOwner().velocity -= Projectile.velocity.normalize() * 8;
                Projectile.velocity = Projectile.velocity.RotatedBy(-2.7f * Projectile.GetOwner().direction).RotatedByRandom(0.32f).normalize() * 12;
            }
            if (!thrown)
            {
                Projectile.timeLeft = 200;
                Projectile.StickToPlayer();
                Projectile.GetOwner().SetHandRot(Projectile.rotation);
                Projectile.position += Projectile.rotation.ToRotationVector2() * (offset + 26 * Projectile.scale);
                Projectile.GetOwner().itemTime = Projectile.GetOwner().itemAnimation = 30;
                dir = Projectile.GetOwner().direction;
                if (counter >= 19)
                    Projectile.position += CEUtils.randomPointInCircle(2);
            }
            else
            {
                Projectile.velocity.Y += 0.36f;
                Projectile.rotation += Projectile.velocity.X * 0.003f;
            }
            offset *= 0.8f;
            counter++;
            if (Main.myPlayer == Projectile.owner && !Main.mouseLeft)
                flag = false;
            if (counter == 20)
            {
                if (Main.myPlayer != Projectile.owner || !Main.mouseLeft || flag)
                    counter--;
                else
                {
                    CEUtils.SyncProj(Projectile.whoAmI);
                }
            }
        }
        public bool flag = true;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(counter);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            counter = reader.ReadInt32();
        }
        public int dir = 1;
        public override bool ShouldUpdatePosition()
        {
            return thrown;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.spriteDirection = dir;
            var tx = Projectile.GetTexture();
            if (thrown)
            {
                tx = this.getTextureAlt("Alt2");
            }
            else if (counter > 8)
                tx = this.getTextureAlt();
            Projectile projectile = Projectile;
            var data = new Terraria.DataStructures.DrawData(tx, projectile.Center - Main.screenPosition, new Rectangle(0, 0, tx.Width, tx.Height), lightColor * projectile.Opacity, projectile.rotation, new Vector2(tx.Width, Main.projFrames[projectile.type] > 1 ? (tx.Height / Main.projFrames[projectile.type]) - 2 : tx.Height) / 2, projectile.scale, projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(data);
            return false;
        }
    }
    public class SniperWulfrumScrap : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, true, 1);
            Projectile.timeLeft = 480;
            Projectile.width = Projectile.height = 16;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center - Projectile.velocity, Projectile.Center, targetHitbox, Projectile.height);
        }
        public override void AI()
        {
            if (Projectile.localAI[2]++ > 6)
            {
                Projectile.velocity.Y += 0.3f;
                Projectile.velocity *= 0.998f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.01f;
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 16; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            int type = ItemID.IronBar;
            Main.instance.LoadItem(type);
            Texture2D tex = TextureAssets.Item[type].Value;
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, tex));
            return false;
        }
    }
}
