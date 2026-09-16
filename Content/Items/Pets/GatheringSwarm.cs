using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Pets
{
    public class GatheringSwarm : ModItem
    {
        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.shoot = ModContent.ProjectileType<PickingSwarm>();
            Item.buffType = ModContent.BuffType<PickingSwarmBuff>();
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.accessory = true;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.Entropy().addEquip("GSwarm", !hideVisual);
            player.AddBuff(Item.buffType, 3600);
        }
        public override void UpdateVanity(Player player) {
            player.AddBuff(Item.buffType, 3600);
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.StoneBlock, 40)
                .AddIngredient(ItemID.Hive, 16)
                .Register();
        }
    }
    public class PickingSwarmBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            //Main.vanityPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<PickingSwarm>());
        }
    }
    public class PickingSwarm : ModProjectile
    {
        public class Swarm
        {
            public Vector2 Position = Vector2.Zero;
            public Vector2 velocity = Vector2.Zero;
            public int CurrentCarryItem = -1;
            public float Alpha = 1;
            public static Texture2D tex = null;
            public float scale = Main.rand.NextFloat(0.6f, 1.2f);
            public int num = Main.rand.Next();
            public int ct = Main.rand.Next(0, 3);
            public void Draw() {
                tex ??= TextureAssets.Projectile[ModContent.ProjectileType<PickingSwarm>()].Value;
                Main.EntitySpriteDraw(tex, Position - Main.screenPosition + new Vector2(0, -8 * scale), CEUtils.GetCutTexRect(tex, 2, ((int)ct++ / 3 + num) % 2, false), Lighting.GetColor((Position / 16).ToPoint()) * Alpha, 0, new Vector2(tex.Width / 2, tex.Height / 4), scale, velocity.X < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            }
        }
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.width = 12;
            Projectile.height = 12;
        }
        public override bool PreDraw(ref Color lightColor) {
            if (Main.gameMenu || swarms == null) {
                return false;
            }

            foreach (Swarm swarm in swarms) {
                swarm.Draw();
            }

            return false;

        }
        public override bool PreAI() {
            Player player = Main.player[Projectile.owner];
            player.zephyrfish = false;
            return true;
        }
        public List<Swarm> swarms = null;
        public static int itype = -1;
        public override void AI() {
            if (itype == -1) {
                itype = ModContent.ItemType<GatheringSwarm>();
            }
            Player player = Projectile.GetOwner();
            if (swarms == null) {
                swarms = new List<Swarm>();
                for (int i = 0; i < 16; i++) {
                    swarms.Add(new Swarm() { Position = Projectile.Center + CEUtils.randomPointInCircle(8) });
                }
            }
            Vector2 tpos = player.Center + new Vector2(0, -50);
            Projectile.velocity += (tpos - Projectile.Center).normalize() * 1.6f;
            Projectile.velocity *= 0.98f;
            if (CEUtils.getDistance(Projectile.Center, player.Center) > 2600) {
                Projectile.Center = tpos;
                Projectile.velocity *= 0;
            }
            List<int> itemCarrying = new List<int>();
            foreach (Swarm swc in swarms) {
                if (swc.CurrentCarryItem >= 0)
                    itemCarrying.Add(swc.CurrentCarryItem);
            }
            List<int> fav = new List<int>();
            for (int i = 0; i < player.inventory.Length; i++) {
                if (player.inventory[i].favorited) {
                    if (!fav.Contains(player.inventory[i].type)) {
                        fav.Add(player.inventory[i].type);
                    }
                }
            }
            foreach (Swarm swarm in swarms) {
                foreach (Swarm sw in swarms) {
                    if (swarm != sw && CEUtils.getDistance(sw.Position, swarm.Position) < 8) {
                        swarm.velocity += (swarm.Position - sw.Position).normalize() * 0.4f;
                        sw.velocity += (sw.Position - swarm.Position).normalize() * 0.4f;
                    }
                }
                if (swarm.CurrentCarryItem >= 0) {
                    Item item = Main.item[swarm.CurrentCarryItem];
                    if (item == null || !item.active) {
                        swarm.CurrentCarryItem = -1;
                    }
                }
                bool flag = true;
                if (swarm.CurrentCarryItem >= 0) {
                    Item item = Main.item[swarm.CurrentCarryItem];
                    if (CEUtils.getDistance(swarm.Position, item.Center) < 40) {
                        item.Center = swarm.Position + swarm.velocity;
                        item.velocity *= 0;
                    }
                    if (CEUtils.getDistance(swarm.Position, item.Center) > 36) {
                        swarm.velocity += (item.Center - swarm.Position).normalize() * 1;
                    }
                    else {
                        swarm.velocity += (player.Center - swarm.Position).normalize() * 1;
                    }
                }
                else {
                    if (CEUtils.getDistance(tpos, swarm.Position) > 18) {
                        swarm.velocity += (tpos - swarm.Position).normalize() * 1;
                    }
                    else {
                        flag = false;
                    }
                    if (Main.rand.NextBool(6)) {
                        foreach (Item i in Main.ActiveItems) {
                            if (ItemID.Sets.CommonCoin[i.type] || player.HeldItem.type == itype || fav.Contains(i.type)) {
                                if (CEUtils.getDistance(i.Center, player.Center) < 4000) {
                                    if (!itemCarrying.Contains(i.whoAmI)) {
                                        swarm.CurrentCarryItem = i.whoAmI;
                                        itemCarrying.Add(i.whoAmI);
                                    }
                                }
                            }
                        }
                    }
                }
                swarm.Alpha = float.Lerp(swarm.Alpha, swarm.CurrentCarryItem < 0 ? ((player.Entropy().hasAcc("GSwarm") && !player.Entropy().hasAccVisual("GSwarm")) ? 0 : 0.66f) : 1, 0.08f);
                swarm.Position += swarm.velocity;
                if (flag) { swarm.velocity *= (CEUtils.getDistance(swarm.Position, player.Center) < 256 ? 0.955f : (swarm.CurrentCarryItem >= 0 ? 0.97f : 0.98f)); }
                if (CEUtils.getDistance(swarm.Position, player.Center) > 4400) {
                    swarm.Position = player.Center + CEUtils.randomPointInCircle(16);
                    swarm.CurrentCarryItem = -1;
                }
            }
            if (!player.dead && player.HasBuff(ModContent.BuffType<PickingSwarmBuff>())) {
                Projectile.timeLeft = 2;
            }
        }


    }
}