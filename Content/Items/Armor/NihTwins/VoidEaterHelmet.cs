using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Armor.NihTwins
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidEaterHelmet : ModItem
    {
        public static int ShieldRecharge = 10 * 60;
        public static int MaxShield = 80;
        public static int LaserDamage = 250;
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.value = Item.buyPrice(platinum: 1, gold: 50);
            Item.defense = 34;
            Item.rare = ModContent.RarityType<NihilityBlue>();
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<VoidEaterBodyArmor>() && legs.type == ModContent.ItemType<VoidEaterLeggings>();
        }


        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Mod.GetLocalization("VoidEaterBonus").Value;
            // 脱离灾厄:灾厄套装键改自有 EModPlayer.ArmorSetBonusHotKey,键名提示走自有扩展
            player.setBonus = player.setBonus.Replace("[KEY]", EModPlayer.ArmorSetBonusHotKey.TooltipKeyHint());
            player.setBonus = player.setBonus.Replace("[KN]", EModPlayer.ArmorSetBonusHotKey.DisplayName.Value);
            player.setBonus = player.setBonus.Replace("[SHIELD]", MaxShield.ToString());
            string cnctStr = Mod.GetLocalization("NihArmorConnet").Value;
            cnctStr = cnctStr.Replace("[ANOTHERSET]", Mod.GetLocalization("ChaoticSet").Value);
            cnctStr = cnctStr.Replace("[CONNECT]", CEKeybinds.NihilityAndChaoticArmorConnectKey.TooltipKeyHint());
            player.setBonus += "\n" + cnctStr;
            // 潜行体系退役:原潜行条(上限1.2)按容量×10%换算为大招充能速度
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.12f;
            player.Entropy().NihilitySet = true;
            // 修复:护盾吸收开关必须在装备阶段就打开。原先只在 PostUpdate 里从 NihilitySet
            // 推导,而受伤结算发生在 PostUpdate 之前,受击时开关恒为 false,护盾从不生效。
            player.Entropy().NihilityShieldEnabled = true;
            player.GetDamage(DamageClass.Generic) += 0.14f;
            player.GetCritChance(DamageClass.Generic) += 14;
            player.maxMinions += 2;
            player.statManaMax2 += 60;
            player.statLifeMax2 += 40;
            if (player.Entropy().NihilityShield <= 0)
                player.lifeRegen += 2;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += 12;
            player.GetDamage(DamageClass.Generic) += 0.12f;
        }

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_Necroplasm))
            {
                CreateRecipe()
                .AddIngredient<NihilityFragments>(5)
                .AddIngredient(CEID.Item_Necroplasm, 6)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
                return;
            }
            // 脱离灾厄:原 Necroplasm×6 换虚无碎片并与原有 5 枚合并
            CreateRecipe()
                .AddIngredient<NihilityFragments>(11)
                .AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
    public class VENihilityLaser : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 4 * 60);
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }
        public int counter = 0;
        List<Vector2> p = new List<Vector2>();
        List<Vector2> l = new List<Vector2>();
        public int length = 6000;
        NPC ownern = null;
        public float width = 0;
        public int aicounter = 0;
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            // 脱离灾厄:灾厄 AverageDamageClass 收敛为通用伤害(player-api.md §5)
            Projectile.DamageType = DamageClass.Generic;
        }
        public bool st = true;
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 80;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (st)
            {
                //VoidEater弹丸拖尾,四连ShineParticle是旧spawn原样
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, new Color(100, 100, 255), 0.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.32f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.32f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center, Vector2.Zero, Color.White, 0.32f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 14);

                st = false;
                for (int ii = 0; ii < 100; ii++)
                {
                    counter++;
                    var rand = Main.rand;
                    int tspeed = 46;
                    if (counter % 1 == 0)
                    {
                        p.Add(new Vector2(0, rand.Next(0, 41) - 20));
                    }
                    if (counter % 6 == 0)
                    {
                        l.Add(new Vector2(0, rand.Next(0, 17) - 8));
                    }
                    for (int i = 0; i < p.Count; i++)
                    {
                        p[i] = p[i] + new Vector2(tspeed, 0);
                    }
                    for (int i = 0; i < l.Count; i++)
                    {
                        l[i] = l[i] + new Vector2(tspeed, 0);
                    }
                    for (int i = 0; i < p.Count; i++)
                    {
                        if (p[i].X > length)
                        {
                            p.RemoveAt(i);
                            break;
                        }
                    }
                    for (int i = 0; i < l.Count; i++)
                    {
                        if (l[i].X > length)
                        {
                            l.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            // 脱离灾厄:灾厄 GeneralScreenShakePower 持续微震改用自有屏震(每帧小幅,距离衰减)
            ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, Utils.Remap(Main.LocalPlayer.Distance(Projectile.Center), 1600f, 100f, 0f, 0.4f)));

            if (Projectile.timeLeft < 6)
            {
                width -= 1f / 16f;
            }
            else
            {
                width += 1f / 16f;

            }
            aicounter++;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * length, targetHitbox, 30);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            counter++;
            var rand = Main.rand;
            int tspeed = 34;
            if (counter % 1 == 0)
            {
                p.Add(new Vector2(16, rand.Next(0, 41) - 20));
            }
            if (counter % 6 == 0)
            {
                l.Add(new Vector2(16, rand.Next(0, 17) - 8));
            }
            for (int i = 0; i < p.Count; i++)
            {
                p[i] = p[i] + new Vector2(tspeed, 0);
            }
            for (int i = 0; i < l.Count; i++)
            {
                l[i] = l[i] + new Vector2(tspeed, 0);
            }
            for (int i = 0; i < p.Count; i++)
            {
                if (p[i].X > length)
                {
                    p.RemoveAt(i);
                    break;
                }
            }
            for (int i = 0; i < l.Count; i++)
            {
                if (l[i].X > length)
                {
                    l.RemoveAt(i);
                    break;
                }
            }
            Texture2D tb = CEExtraAssets.clback;
            Texture2D px = CEExtraAssets.white;
            Texture2D tl = CEExtraAssets.cllight;
            Texture2D th = CEExtraAssets.clinghth;
            Texture2D tl2 = CEExtraAssets.cllight2;
            Main.spriteBatch.Draw(tb, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(0, tb.Height / 2), new Vector2(length, width), SpriteEffects.None, 0);
            foreach (Vector2 ps in p)
            {
                CEUtils.drawLine(Main.spriteBatch, px, Projectile.Center + (ps * new Vector2(1, width)).RotatedBy(Projectile.rotation), Projectile.Center + ((ps * new Vector2(1, width)) + new Vector2(40, 0)).RotatedBy(Projectile.rotation), Color.White, 2 * width);
            }
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tl2, Projectile.Center - Main.screenPosition, null, new Color(160, 160, 255) * 0.68f, Projectile.rotation, new Vector2(0, tl2.Height / 2), new Vector2(length, width * 1.2f), SpriteEffects.None, 0);

            foreach (Vector2 ps in l)
            {
                Main.spriteBatch.Draw(tl, Projectile.Center + (ps * new Vector2(1, width)).RotatedBy(Projectile.rotation) - Main.screenPosition, null, new Color(160, 160, 255) * 0.8f, Projectile.rotation, tl.Size() / 2, new Vector2(1.5f, 1.5f * width), SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(th, Projectile.Center - Main.screenPosition, null, new Color(160, 160, 255) * 0.5f, Projectile.rotation, new Vector2(0, th.Height / 2), new Vector2(1, width), SpriteEffects.None, 0);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
    }
}
