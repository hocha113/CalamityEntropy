using CalamityEntropy.Common;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.NihTwins
{
    [AutoloadEquip(EquipType.Head)]
    public class ChaoticHelmet : ModItem
    {
        public static int MaxCells = 8;
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.defense = 36;
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ChaoticBodyArmor>() && legs.type == ModContent.ItemType<ChaoticLeggings>();
        }


        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Mod.GetLocalization("ChaoticSetBonus").Value;
            // 脱离灾厄:灾厄套装键改自有 EModPlayer.ArmorSetBonusHotKey,键名提示走自有扩展
            player.setBonus = player.setBonus.Replace("[KEY]", EModPlayer.ArmorSetBonusHotKey.TooltipKeyHint());
            player.setBonus = player.setBonus.Replace("[KN]", EModPlayer.ArmorSetBonusHotKey.DisplayName.Value);
            player.setBonus = player.setBonus.Replace("[LIMIT]", MaxCells.ToString());
            string cnctStr = Mod.GetLocalization("NihArmorConnet").Value;
            cnctStr = cnctStr.Replace("[ANOTHERSET]", Mod.GetLocalization("VoidEaterSet").Value);
            cnctStr = cnctStr.Replace("[CONNECT]", CEKeybinds.NihilityAndChaoticArmorConnectKey.TooltipKeyHint());
            player.setBonus += "\n" + cnctStr;
            // 潜行体系退役:原潜行条(上限1.2)按容量×10%换算为大招充能速度
            // 2026-08-31 平衡案:去除12%伤害减免,魔力+120→+60,生命+40→+80
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.12f;
            player.statLifeMax2 += 80;
            player.GetDamage(DamageClass.Generic) += 0.12f;
            player.maxMinions += 3;
            player.statManaMax2 += 60;
            player.Entropy().ChaoticSet = true;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.08f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ExodiumCluster))
            {
                CreateRecipe()
                .AddIngredient<ChaoticPiece>(5)
                .AddIngredient(CEID.Item_ExodiumCluster, 6)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<ChaoticPiece>(5)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class ChaoticCellMinion : ModProjectile
    {
        public static int BaseDamage = 400;
        public override void SetDefaults()
        {
            // 脱离灾厄:灾厄 AverageDamageClass 收敛为通用伤害(player-api.md §5)
            Projectile.FriendlySetDefaults(DamageClass.Generic, false, -1);
            Projectile.width = Projectile.height = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = 26 * 60;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits < 6)
            {
                Projectile.velocity *= -1f;
            }
            Projectile.netUpdate = true;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.numHits);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.numHits = reader.ReadInt32();
        }
        public float ChasePlayer = 1;
        public override void AI()
        {
            if (Projectile.localAI[1] == 0)
                Projectile.velocity *= Main.rand.NextFloat(0.4f, 1);
            if (Projectile.localAI[1]++ < 30)
            {
                Projectile.velocity *= 0.96f;
            }
            else
            {
                if (Projectile.numHits < 6 && Projectile.timeLeft > 10 * 60)
                {
                    NPC target = Projectile.FindMinionTarget(6000);
                    if (target != null)
                    {
                        Projectile.velocity *= 0.97f;
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 1;
                    }
                    else
                    {
                        if (Projectile.Distance(Projectile.GetOwner().Center) > 120)
                        {
                            Projectile.velocity *= 0.96f;
                            Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * 1.2f;
                        }
                    }
                }
                else
                {
                    if (ChasePlayer < 25)
                        ChasePlayer += 0.025f;
                    Projectile.velocity *= 1 - ChasePlayer * 0.02f;
                    Projectile.velocity += (Projectile.GetOwner().Center - Projectile.Center).normalize() * ChasePlayer;
                    if (Projectile.Distance(Projectile.GetOwner().Center) < Projectile.velocity.Length() + 32)
                    {
                        Projectile.Kill();
                        int heal = (int)(1 + Projectile.GetOwner().statLifeMax2 * 0.015f);
                        Projectile.GetOwner().Heal(heal);

                        CEUtils.PlaySound("cellheal", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center, 8, 0.9f);
                        Projectile.Kill();
                    }
                }
            }
            Projectile.rotation += Projectile.velocity.X * 0.01f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (float i = 0; i < 360; i += 60)
            {
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, null, Projectile.Center + i.ToRadians().ToRotationVector2() * 2));
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
