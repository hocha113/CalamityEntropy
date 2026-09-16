using CalamityEntropy.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class CreeperWand : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults() {
            Item.damage = 32;
            Item.DamageType = DamageClass.Summon;
            Item.width = 46;
            Item.height = 46;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 6;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<CreeperMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(0, 5);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 5;
            Item.buffType = ModContent.BuffType<CreeperBuff>();
            Item.rare = ItemRarityID.Orange;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;
            return false;
        }
    }
    public class CreeperBuff : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<CreeperMinion>();
    }
    public class CreeperMinion : ModProjectile
    {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1;
            Projectile.ArmorPenetration = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }
        // 原灾厄肾上腺素(AdrenalineMode)检测随 ripper 系统退役，保留周期激活常态分支
        public bool Active => ((int)Main.GameUpdateCount + Projectile.ai[1]) % 450 < 120;
        public override bool? CanHitNPC(NPC target) {
            return Active ? null : false;
        }
        public override bool? CanCutTiles() {
            return false;
        }

        public override void AI() {
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<CreeperBuff>();
            if (Projectile.localAI[0] == 0) {
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.TwoPi * 0.05f) {
                    var dust = Dust.NewDustDirect(Projectile.Center, 0, 0, Main.rand.NextBool() ? DustID.Blood : DustID.GreenBlood);
                    dust.position = Projectile.Center;
                    dust.noGravity = true;
                    dust.velocity = i.ToRotationVector2() * 6;
                    dust.scale = 1.5f;
                }
            }
            if (Projectile.localAI[0]++ < 3) {
                Projectile.timeLeft++;
                return;
            }
            if (Projectile.Distance(player.Center) > 4000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            Projectile.pushByOther(0.2f);
            Projectile.rotation += Projectile.velocity.X * 0.012f;

            int sum = 0;
            int self = 0;

            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == Projectile.type) {
                    if (p.whoAmI == Projectile.whoAmI) {
                        self = sum;
                    }
                    sum++;
                }
            }
            Projectile.ai[1] = self * 60;
            NPC target = Projectile.FindMinionTarget(1400, true);
            if (Active && target != null) {
                if (!flag) {
                    for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.TwoPi * 0.05f) {
                        var dust = Dust.NewDustDirect(Projectile.Center, 0, 0, Main.rand.NextBool() ? DustID.Blood : DustID.GreenBlood);
                        dust.position = Projectile.Center;
                        dust.noGravity = true;
                        dust.velocity = i.ToRotationVector2() * 6;
                        dust.scale = 1.5f;
                    }
                }
                flag = true;
                Projectile.velocity *= 0.82f;
                Projectile.velocity += num2.ToRotationVector2() * 6f;
                if (CEUtils.getDistance(Projectile.Center, target.Center) > 80) {
                    num += 0.1f;
                }
                else {
                    num = 0;
                }
                if (num > 1)
                    num = 1;
                num2 = CEUtils.RotateTowardsAngle(num2, (target.Center - Projectile.Center).ToRotation(), num, false);


                trailAlpha = 1;
            }
            else {
                if (trailAlpha > 0)
                    trailAlpha -= 0.05f;
                num = 0;
                if (target != null) {
                    num2 = (target.Center - Projectile.Center).ToRotation();
                }
                flag = false;
                Vector2 tp = Projectile.Center + Projectile.velocity;
                if (Projectile.Distance(player.Center) > 100) {
                    Projectile.velocity *= 0.97f;
                    Projectile.velocity += (player.Center - Projectile.Center).normalize() * 0.95f;
                }
            }
            if (oldPos.Count > 0) {
                Vector2 sp = oldPos[oldPos.Count - 1];
                Vector2 tp = Projectile.Center + Projectile.velocity;
                for (float i = 0.2f; i <= 1; i += 0.2f) {
                    oldPos.Add(Vector2.Lerp(sp, tp, i));
                    if (oldPos.Count > 36)
                        oldPos.RemoveAt(0);
                }
            }
            else {
                oldPos.Add(Projectile.Center + Projectile.velocity);
            }
        }
        public bool flag = false;
        public List<Vector2> oldPos = new List<Vector2>();
        public float num = 0;
        public float num2 = 0;
        public float trailAlpha = 0;
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));

            for (int i = 0; i < oldPos.Count; i++) {
                float p = (i + 1f) / oldPos.Count;
                Color color = lightColor * p * 0.4f * trailAlpha;
                Main.EntitySpriteDraw(tex, oldPos[i] - Main.screenPosition, null, color, Projectile.rotation, tex.Size() / 2f, Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            }
            return false;
        }
    }
}
