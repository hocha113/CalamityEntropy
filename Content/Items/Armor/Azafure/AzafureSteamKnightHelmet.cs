using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.Weapons;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.Azafure
{
    [AutoloadEquip(EquipType.Head)]
    public class AzafureSteamKnightHelmet : ModItem
    {
        public override void SetDefaults() {
            Item.width = 48;
            Item.height = 48;
            Item.value = Item.buyPrice(gold: 20);
            Item.defense = 12;
            Item.rare = ItemRarityID.Pink;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return head.type == Type && body.type == ModContent.ItemType<AzafureSteamKnightArmor>() && legs.type == ModContent.ItemType<AzafureSteamKnightLeggings>();
        }

        public override void UpdateArmorSet(Player player) {
            player.setBonus = Mod.GetLocalization("AzafureSet2").Value;
            player.GetModPlayer<AzafureSteamKnightArmorPlayer>().ArmorSetBonus = true;
            // 潜行体系退役:原潜行条(上限0.8)按容量×10%换算为大招充能速度;灾厄肾上腺素抑制随 ripper 系统退役删除
            player.GetModPlayer<CEChargePlayer>().ChargeRateMult += 0.08f;
            player.maxMinions += 1;
        }
        public override void UpdateEquip(Player player) {
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<AzafureHeavyHelmet>()
                .AddIngredient(ItemID.ChlorophyteBar, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class AzafureSteamKnightArmorPlayer : ModPlayer
    {
        public bool ArmorSetBonus = false;
        public float durability = 1;
        public int DurabilityRegenDelay = 0;
        public bool DurabilityActive = true;
        public int DeathExplosion = -1;
        public int DeathExplosionCD = 0;
        public bool ExplosionFlag = false;
        public PlayerDeathReason dmgSource = null;
        public LoopSound chargeSnd = null;
        public override void PostUpdate() {
            if (!Main.dedServ) {
                if (chargeSnd != null && chargeSnd.timeleft <= 0)
                    chargeSnd = null;
                if (chargeSnd != null) {
                    chargeSnd.setVolume_Dist(Player.Center, 100, 1600, 1);
                    chargeSnd.instance.Pitch = (1 - (DeathExplosion / 80f)) * 2f + 1.9f;
                }
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) {
            if (ExplosionFlag && DeathExplosion > 0)
                return false;
            if (ArmorSetBonus && !ExplosionFlag && DeathExplosionCD <= 0) {
                //damageSource = PlayerDeathReason.ByCustomReason(Mod.GetLocalization("Death").ToNetworkText(Player.name));
            }
            return true;
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
            if (ArmorSetBonus) {
                if (DeathExplosionCD <= 0 && !ExplosionFlag) {
                    ExplosionFlag = true;
                    DeathExplosionCD = 10 * 60;
                    DeathExplosion = 80;
                    dmgSource = damageSource;
                    Player.dead = false;
                    if (!Main.dedServ) {
                        chargeSnd = new LoopSound(CalamityEntropy.ofCharge);
                        chargeSnd.instance.Pitch = 0;
                        chargeSnd.instance.Volume = 0;
                        chargeSnd.play();
                        chargeSnd.timeleft = 80;
                    }
                }
                else
                    ExplosionFlag = false;
                if (Main.myPlayer == Player.whoAmI && Main.netMode == NetmodeID.MultiplayerClient) {
                    var mp = Mod.GetPacket();
                    mp.Write((byte)CEMessageType.SyncPlayerDead);
                    mp.Write(Player.whoAmI);
                    mp.Write(Player.dead);
                    mp.WriteVector2(Player.position);
                    mp.Send();
                }
            }
        }
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) {
            return DeathExplosion < 0;
        }
        public override bool CanBeHitByProjectile(Projectile proj) {
            return DeathExplosion < 0;
        }
        public override void ResetEffects() {
            ArmorSetBonus = false;
            if (DeathExplosion > 0 && ExplosionFlag) {
                DeathExplosion--;
                Player.velocity *= 0;
                Player.Entropy().noItemTime = 5;
                if (DeathExplosion < 70 && DeathExplosion % 2 == 0) {
                    if (DeathExplosion % 6 == 0)
                        ScreenShaker.AddShake(new ScreenShaker.ScreenShake(Vector2.Zero, Utils.Remap(Main.LocalPlayer.Center.Distance(Player.Center), 4000, 1000, 0, 12)));

                    //SteamKnight自爆环,ShockParticle2+PulseRing照搬Azafure甲那套
                    PRTLoader.NewParticle<PRT_ShockParticle2>(Player.Center, Vector2.Zero, Color.White, 0.1f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, CEUtils.randomRot());
                }
                if (DeathExplosion == 0 || DeathExplosion == 3 || DeathExplosion == 6 || DeathExplosion == 9 || DeathExplosion == 12 || DeathExplosion == 15 || DeathExplosion == 18 || DeathExplosion == 21) {
                    CEUtils.PlaySound("pulseBlast", 0.6f, Player.Center, 6, 1f);
                    CEUtils.PlaySound("blackholeEnd", 0.6f, Player.Center, 6, 1f);

                    PRTLoader.NewParticle<PRT_PulseRing>(Player.Center, Vector2.Zero, Color.Firebrick, 0.1f).Configure(12f, 8);
                    PRTLoader.NewParticle<PRT_ShineParticle>(Player.Center, Vector2.Zero, Color.Firebrick, 20f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                    PRTLoader.NewParticle<PRT_ShineParticle>(Player.Center, Vector2.Zero, Color.White, 16f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                    ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Vector2.Zero, 100), 1200);
                    var proj = CEUtils.SpawnExplotionFriendly(Player.GetSource_FromThis(), Player, Player.Center, ((int)(Player.GetBestClassDamage().ApplyTo(1900))).ApplyAccArmorDamageBonus(Player), 1200, DamageClass.Generic);
                    proj.ArmorPenetration = 60;
                    if (proj.ModProjectile is CommonExplotionFriendly cef) {
                        cef.DamageMulToWormSegs = 0.16f;
                    }
                }
                if (DeathExplosion == 0) {
                    DeathExplosion = -1;
                    ExplosionFlag = false;
                    Player.KillMe(PlayerDeathReason.ByCustomReason(Mod.GetLocalization("DeathExplode").ToNetworkText(Player.name)), 10000, 0);
                    if (Main.myPlayer == Player.whoAmI && Main.netMode == NetmodeID.MultiplayerClient) {
                        var mp = Mod.GetPacket();
                        mp.Write((byte)CEMessageType.SyncPlayerDead);
                        mp.Write(Player.whoAmI);
                        mp.Write(Player.dead);
                        mp.WriteVector2(Player.position);
                        mp.Send();
                    }
                }
            }
        }
        public override void PostUpdateEquips() {
            if (!ExplosionFlag || DeathExplosion == 0)
                DeathExplosion = -1;
            DeathExplosionCD--;
            if (ArmorSetBonus) {
                Player.Entropy().moveSpeed += 0.1f;
                DurabilityRegenDelay--;
                if (DurabilityActive) {
                    Player.Entropy().EDamageReduce += durability * 0.2f;
                    Player.statDefense += (int)(durability * 20);
                    Player.noKnockback = true;
                }
                else {
                    DurabilityRegenDelay = -1;
                }
                if (DurabilityRegenDelay <= 0) {
                    durability += 0.001f;
                    if (durability >= 1) {
                        durability = 1;
                        if (!DurabilityActive)
                            CEUtils.PlaySound("AuricQuantumCoolingCellInstallNew", 0.7f, Player.Center);
                        DurabilityActive = true;
                    }
                }
            }
        }
        public override void OnHurt(Player.HurtInfo info) {
            if (ArmorSetBonus) {
                if (DurabilityActive) {
                    CEUtils.PlaySound($"ExoHit{Main.rand.Next(1, 5)}", Main.rand.NextFloat(0.6f, 0.8f), Player.Center, 6, 0.45f);
                    if (DurabilityRegenDelay < 5 * 60)
                        DurabilityRegenDelay = 5 * 60;
                    durability -= float.Min(0.52f, info.SourceDamage / 700f);

                    //耐久没了暂时失效
                    if (durability <= 0) {
                        durability = 0;
                        DurabilityActive = false;
                        CEUtils.PlaySound("chainsaw_break", 1.3f, Player.Center, 6, 0.6f);
                        for (int i = 0; i < 16; i++) {
                            PRTLoader.NewParticle<PRT_EMediumSmoke>(Player.Center + CEUtils.randomPointInCircle(12), CEUtils.randomPointInCircle(16), Color.Lerp(new Color(255, 255, 0), Color.White, (float)Main.rand.NextDouble()), Main.rand.NextFloat(1f, 2f)).Configure(1, true, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot(), 120);
                        }
                    }
                }
            }
        }
        public static void DrawDuraBar(float dura) {
            Main.spriteBatch.UseSampleState_UI(SamplerState.PointClamp);
            // 脱离灾厄:灾厄客户端配置不可用,耐久条固定挂在原肾上腺素条默认屏幕位
            Vector2 pos = new Vector2(35.77f, 8.85f);
            pos.X = (int)(pos.X * 0.01f * Main.screenWidth);
            pos.Y = (int)(pos.Y * 0.01f * Main.screenHeight);

            var mplayer = Main.LocalPlayer.GetModPlayer<AzafureSteamKnightArmorPlayer>();
            Color color = mplayer.DurabilityActive ? Color.White : new Color(255, 80, 80) * 0.5f;
            Color color2 = mplayer.DurabilityActive ? Color.White : new Color(255, 142, 142) * 0.7f;
            Vector2 Center = pos;// Main.ScreenSize.ToVector2() * 0.5f + new Vector2(0, -60);
            if (dura < 0.32f && mplayer.DurabilityActive) {
                Center += new Vector2(Main.rand.NextFloat() * ((0.32f - dura) * 20), Main.rand.NextFloat() * ((0.32f - dura) * 20));
            }
            Texture2D tex1 = AzafureHeavyArmorPlayer.DuraBarFrontTex.Value;
            Texture2D tex2 = AzafureHeavyArmorPlayer.DuraBarBackTex.Value;
            Main.spriteBatch.Draw(tex2, Center, null, color, 0, tex2.Size() / 2f, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex1, Center, new Rectangle(0, 0, (int)(tex1.Width * dura), tex1.Height), color2, 0, tex1.Size() / 2f, 1, SpriteEffects.None, 0);
            bool hover = Center.getRectCentered(100, 40).Intersects(Main.MouseScreen.getRectCentered(2, 2));
            if (hover)
                Main.instance.MouseText(CalamityEntropy.Instance.GetLocalization("DuraBar").Value + $": {dura.ToPercent()}%");
        }
    }
}
