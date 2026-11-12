using CalamityEntropy.Core.Weapons;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.SunAloft
{
    //左键交替挥砍, 命中蓄势满后右键突刺并按昼夜召唤日或月; 蓄势走 ICEChargeWeapon, 计数跟着物品走
    public class SunAloft : ModItem, ICEChargeWeapon
    {
        public const int ChargeHits = 20;
        public const float ThrustDamageMult = 8f;
        public const float ThrustKnockbackMult = 1.5f;
        public const float CelestialHover = 150f;
        public const float InventoryGlowOffset = 2f;

        public static readonly Color DayGlow = new(255, 200, 90);
        public static readonly Color NightGlow = new(150, 200, 255);

        private int swingDir = -1;

        public CEChargeProfile ChargeProfile => CEChargeProfile.HitCount(ChargeHits);

        public override void SetDefaults() {
            Item.width = 92;
            Item.height = 92;
            Item.damage = 230;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SunAloftSwing>();
            Item.shootSpeed = 10f;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 25);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) => player.altFunctionUse != 2 || CEChargeWeapon.IsReady(Item);

        public override bool MeleePrefix() => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            float night = Main.dayTime ? 0f : 1f;
            if (player.altFunctionUse == 2) {
                if (!CEChargeWeapon.TryConsume(player, Item))
                    return false;
                Projectile.NewProjectile(source, player.MountedCenter, velocity, ModContent.ProjectileType<SunAloftThrust>(), (int)(damage * ThrustDamageMult), knockback * ThrustKnockbackMult, player.whoAmI, night);
                int celestial = ModContent.ProjectileType<SunAloftCelestial>();
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.owner == player.whoAmI && p.type == celestial)
                        p.Kill();
                }
                Projectile.NewProjectile(source, player.MountedCenter - Vector2.UnitY * CelestialHover, Vector2.Zero, celestial, damage, 0f, player.whoAmI, night);
                return false;
            }
            swingDir = -swingDir;
            Projectile.NewProjectile(source, player.MountedCenter, velocity, type, damage, knockback, player.whoAmI, swingDir, player.itemAnimationMax, CEChargeWeapon.IsReady(Item) ? 1f : 0f);
            return false;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!CEChargeWeapon.IsReady(Item))
                return true;
            Texture2D tex = TextureAssets.Item[Type].Value;
            float pulse = 0.65f + 0.35f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Color glow = (Main.dayTime ? DayGlow : NightGlow) with { A = 0 };
            for (int i = 0; i < 4; i++) {
                Vector2 offset = (MathHelper.PiOver2 * i).ToRotationVector2() * InventoryGlowOffset;
                spriteBatch.Draw(tex, position + offset, frame, glow * pulse, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<BlazingSunlight>(5)
                .AddIngredient<WraithSoulEssence>(5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
