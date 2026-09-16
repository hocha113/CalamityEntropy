using CalamityEntropy.Content.Items.Weapons;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class SamsaraCasketProj : ModProjectile
    {
        //棺体前后层(a/c 按等级 0~6)与剑格(b0~b5)贴图数组,加载期就位,PreDraw 不再拼接路径逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/a", 0, 7, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] BackFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/c", 0, 7, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] FrontFrames;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/b", 0, 6, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] SwordFrames;

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }
        public override bool? CanCutTiles() {
            return false;
        }
        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            // 2026-08-31 平衡案:视界之键重做为8栏召唤武器,棺体按仆从记账
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = HorizonssKey.MinionSlotCost;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;

        }
        public bool lastPlayerCasketState = false;
        public int spawnAnm = 0;
        public int spawnCounter = 4;
        public bool[] swords = new bool[7];
        public class SpawnLater
        {
            public int type;
            public int index;
            public float rot;
            public float circleRot;
            public int hideTime;
            public SpawnLater(int type, int index, float rot, float circleRot, int hideTime = 0) {
                this.type = type;
                this.index = index;
                this.rot = rot;
                this.circleRot = circleRot;
                this.hideTime = hideTime;
            }

        }
        public override void OnSpawn(IEntitySource source) {
            for (int i = 0; i < 7; i++) {
                swords[i] = true;
            }
        }
        public override void AI() {
            Player player = Projectile.owner.ToPlayer();
            if (player.dead) {

                Projectile.Kill();
                return;
            }
            var modPlayer = player.Entropy();
            Projectile.Center = player.Center + player.gfxOffY * Vector2.UnitY;
            if (player.HeldItem.type == ModContent.ItemType<HorizonssKey>()) {
                Projectile.timeLeft = 3;
            }
            else {
                int count = 0;
                foreach (Projectile p in Main.projectile) {
                    if (p.active && p.ModProjectile is SamsaraSword && p.owner == Projectile.owner) {
                        count++;
                    }
                }
                if (count > 0) {
                    Projectile.timeLeft = 3;
                }
                modPlayer.samsaraCasketOpened = false;
            }
            if (!lastPlayerCasketState && modPlayer.samsaraCasketOpened) {
                if (modPlayer.sCasketLevel >= 2) {
                    spawnAnm = 1;
                }
                Projectile.netUpdate = true;
            }
            int level = modPlayer.sCasketLevel;
            if (modPlayer.samsaraCasketOpened) {
                if (spawnAnm > 0) {
                    bool addAnm = true;

                    if (addAnm) {
                        spawnCounter--;
                        if (spawnCounter <= 0) {
                            spawnCounter = 4;
                            spawnAnm++;
                        }

                    }
                    if ((level <= 1 && spawnAnm > 1) || (level <= 3 && spawnAnm > 2) || spawnAnm > 3) {
                        spawnAnm = 0;
                        Projectile.netUpdate = true;
                    }
                }
                else {
                    NPC target = CEUtils.findTarget(player, Projectile, SamsaraSword.getRange(player), modPlayer.sCasketLevel <= 3);
                    if (target != null && CEUtils.getDistance(player.Center, target.Center) > Math.Min(SamsaraSword.getRange(player), 1400)) {
                        target = null;
                    }
                    if (later.Count > 0) {
                        for (int i = later.Count - 1; i >= 0; i--) {
                            if (later[i].hideTime-- <= 0) {
                                spawnSword(later[i].type, later[i].index, later[i].rot, later[i].circleRot, 0);
                                later.RemoveAt(i);
                            }

                        }
                    }
                    if (later.Count == 0) {
                        float[] rots = new float[6];
                        float r = 0;
                        for (int i = 0; i < 6; i++) {
                            rots[i] = r;
                            r += MathHelper.ToRadians(360 / 6);
                        }
                        int spawnIndex = 0;
                        int timeMul = 0;
                        if (level >= 6) {
                            timeMul = 1;
                        }
                        if (swords[spawnIndex] && level >= spawnIndex && (level >= 6 || target != null)) {
                            spawnSword(ModContent.ProjectileType<Asleep>(), spawnIndex, MathHelper.ToRadians(-90 - 16), rots[spawnIndex], 0 * timeMul);

                        }
                        spawnIndex = 1;
                        if (swords[spawnIndex] && level >= spawnIndex && (level >= 6 || target != null)) {
                            spawnSword(ModContent.ProjectileType<CyanSunshine>(), spawnIndex, MathHelper.ToRadians(-90 + 16), rots[spawnIndex], 0 * timeMul);

                        }

                        spawnIndex = 2;
                        if (swords[spawnIndex] && level >= spawnIndex) {
                            spawnSword(ModContent.ProjectileType<SacredJudge>(), spawnIndex, MathHelper.ToRadians(-90 - 32), rots[spawnIndex], 20 * timeMul);

                        }
                        spawnIndex = 3;
                        if (swords[spawnIndex] && level >= spawnIndex && (level >= 6 || target != null)) {
                            spawnSword(ModContent.ProjectileType<Recovery>(), spawnIndex, MathHelper.ToRadians(-90 + 32), rots[spawnIndex], 20 * timeMul);

                        }

                        spawnIndex = 4;
                        if (swords[spawnIndex] && level >= spawnIndex && (level >= 6 || target != null)) {
                            spawnSword(ModContent.ProjectileType<Dirge>(), spawnIndex, MathHelper.ToRadians(-90 - 48), rots[spawnIndex], 40 * timeMul);

                        }
                        spawnIndex = 5;
                        if (swords[spawnIndex] && level >= spawnIndex && (level >= 6 || target != null)) {
                            spawnSword(ModContent.ProjectileType<SoulDissipate>(), spawnIndex, MathHelper.ToRadians(-90 + 48), rots[spawnIndex], 40 * timeMul);

                        }
                        spawnIndex = 6;
                        if (swords[spawnIndex] && level >= spawnIndex && (level >= 6 || target != null)) {
                            spawnSword(ModContent.ProjectileType<ZeratosHeart>(), spawnIndex, MathHelper.ToRadians(-90), 0, 40 * timeMul);

                        }
                    }
                }
            }
            lastPlayerCasketState = modPlayer.samsaraCasketOpened;
        }
        public override void OnKill(int timeLeft) {
            Projectile.owner.ToPlayer().Entropy().samsaraCasketOpened = false;
        }
        public List<SpawnLater> later = new List<SpawnLater>();
        public void spawnSword(int type, int index, float rot, float circleRot, int hideTime = 0) {
            if (Projectile.owner.ToPlayer().HeldItem.ModItem is HorizonssKey && Main.myPlayer == Projectile.owner) {
                if (hideTime > 0) {
                    later.Add(new SpawnLater(type, index, rot, circleRot, hideTime));
                }
                else {
                    Projectile p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, type, Projectile.damage, Projectile.knockBack, Projectile.owner).ToProj();
                    p.Entropy().IndexOfTwistedTwinShootedThisProj = Projectile.Entropy().IndexOfTwistedTwinShootedThisProj;
                    SamsaraSword ss = ((SamsaraSword)p.ModProjectile);
                    ss.casket = Projectile.whoAmI;
                    ss.index = index;
                    p.CritChance = Projectile.CritChance;
                    ss.spawnRot = rot;
                    ss.circleRot = circleRot;
                    ss.hideTime = hideTime;
                    p.originalDamage = Projectile.originalDamage;
                    swords[index] = false;
                    if (Main.netMode == NetmodeID.MultiplayerClient) {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);

                    }
                }

            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return false;
        }
        public override bool PreDraw(ref Color lightColor) {
            Player player = Projectile.owner.ToPlayer();
            var modPlayer = player.Entropy();
            int tex = 0;
            int maxDrawSwords = 0;
            int count = 0;
            foreach (Projectile p in Main.projectile) {
                if (p.active && p.ModProjectile is SamsaraSword && p.owner == Projectile.owner) {
                    count++;
                }
            }
            if (modPlayer.samsaraCasketOpened || count > 0) {
                tex = (int)MathHelper.Min(modPlayer.sCasketLevel + 1, 6);
                maxDrawSwords = modPlayer.sCasketLevel + 1;
            }
            Vector2 pos = Projectile.Center - new Vector2(0, 20);
            if (spawnAnm > 0) {

                if (spawnAnm == 1) {
                    tex = 2;
                    maxDrawSwords = 2;
                }
                if (spawnAnm == 2) {
                    tex = 4;
                    maxDrawSwords = 4;
                }
                if (spawnAnm == 3) {
                    tex = 6;
                    maxDrawSwords = 6;
                }
                Texture2D back_ = BackFrames[tex];
                Texture2D front_ = FrontFrames[tex];
                Texture2D middleTex_;

                Main.spriteBatch.Draw(back_, pos - Main.screenPosition, null, lightColor, player.fullRotation, back_.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
                for (int i = 0; i < 6; i++) {
                    if (i >= maxDrawSwords) {
                        break;
                    }
                    if (i <= modPlayer.sCasketLevel) {
                        middleTex_ = SwordFrames[i];
                        bool glow = false;
                        if (swords[i]) {
                            Main.spriteBatch.Draw(middleTex_, pos - Main.screenPosition, null, (glow ? Color.White : lightColor), player.fullRotation, back_.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
                        }
                    }
                    else {
                        break;
                    }
                }
                Main.spriteBatch.Draw(front_, pos - Main.screenPosition, null, lightColor, player.fullRotation, back_.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

                return false;
            }
            Texture2D back = BackFrames[tex];
            Texture2D front = FrontFrames[tex];
            Texture2D middleTex;

            Main.spriteBatch.Draw(back, pos - Main.screenPosition, null, lightColor, player.fullRotation, back.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            for (int i = 0; i < 6; i++) {
                if (i >= maxDrawSwords) {
                    break;
                }
                if (i <= modPlayer.sCasketLevel) {
                    middleTex = SwordFrames[i];
                    bool glow = false;
                    if (swords[i]) {
                        Main.spriteBatch.Draw(middleTex, pos - Main.screenPosition, null, (glow ? Color.White : lightColor), player.fullRotation, back.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
                    }
                }
                else {
                    break;
                }
            }
            Main.spriteBatch.Draw(front, pos - Main.screenPosition, null, lightColor, player.fullRotation, back.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
