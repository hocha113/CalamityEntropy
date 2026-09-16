
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Training
{
    public class TrainingSword : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 12;
            Item.DamageType = DamageClass.Melee;
            Item.width = 28;
            Item.height = 68;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 2);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TrainingSwordProj>();
            Item.shootSpeed = 8f;
        }
        public int atkType = 0;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, atkType);
            atkType++;
            if (atkType > 6) {
                atkType = 0;
            }
            return false;
        }

        public override bool MeleePrefix() {
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 12)
                .AddIngredient(ItemID.BambooBlock, 4)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
    public class TrainingSwordProj : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 10000;
        }
        public int counter = 0;
        public float FrameTime => 4 / Projectile.GetOwner().GetTotalAttackSpeed(Projectile.DamageType);
        public float frameCounter = 0;
        public int frame = 0;
        //斩击帧贴图(Slash0~6),按序号批量加载,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/Training/Slash/Slash", 0, 7, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] SlashFrames;
        public int atkType => (int)Projectile.ai[0];
        public int TotalFrame() => atkType switch {
            0 => 4,
            1 => 6,
            2 => 4,
            3 => 5,
            4 => 4,
            5 => 4,
            6 => 4,
            _ => 4
        };
        public float DamageMult() => atkType switch {
            0 => 0.8f,
            1 => 1,
            2 => 1.15f,
            3 => 1.2f,
            4 => 1,
            5 => 1.2f,
            6 => 1.5f,
            _ => 4
        };
        public MultiRectHitbox GetInitHitboxs() => atkType switch {
            0 => new MultiRectHitbox(new List<Rectf>() { new Rectf(Vector2.Zero, new Vector2(128, 12)) }),
            1 => new MultiRectHitbox(new List<Rectf>() { new Rectf(Vector2.Zero, new Vector2(132, 128)) }),
            2 => new MultiRectHitbox(new List<Rectf>() { new Rectf(new Vector2(58, 0), new Vector2(42, 126)), new Rectf(new Vector2(76, 0), new Vector2(136, 32)) { ActiveFrame = 1 } }),
            3 => new MultiRectHitbox(new List<Rectf>() { new Rectf(new Vector2(0, 30), new Vector2(258, 90)) { ActiveFrame = 1, DeactiveFrame = 1 }, new Rectf(new Vector2(66, 0), new Vector2(132, 142)) { ActiveFrame = 2 } }),
            4 => new MultiRectHitbox(new List<Rectf>() { new Rectf(Vector2.Zero, new Vector2(152, 118)) }),
            5 => new MultiRectHitbox(new List<Rectf>() { new Rectf(new Vector2(-6, 0), new Vector2(152, 116)) }),
            6 => new MultiRectHitbox(new List<Rectf>() { new Rectf(new Vector2(24, -20), new Vector2(86, 156)) }),
            _ => new MultiRectHitbox()
        };
        public MultiRectHitbox hitbox;
        public float HandRot = 0;
        public float rotV = 0;
        public float rotC = 0.8f;
        public void HandleHandRotation() {
            if (Projectile.localAI[2] == 0) {
                if (atkType == 0) {
                    HandRot = 2;
                    rotV = -1f;
                }
                if (atkType == 1) {
                    HandRot = 2;
                    rotV = -1.1f;
                    rotC = 0.86f;
                }
                if (atkType == 3) {
                    HandRot = 2;
                    rotV = -0.8f;
                }
                if (atkType == 4) {
                    HandRot = 3.5f;
                    rotV = -0.9f;
                    rotC = 0.84f;
                }
                if (atkType == 5) {
                    HandRot = -2.2f;
                    rotV = 1.2f;
                }
                if (atkType == 6) {
                    HandRot = -2.8f;
                    rotV = 1f;
                    rotC = 0.74f;
                }
            }

            int strc = 0;
            if (atkType == 2) {
                strc = counter / 2;
                if (strc > 3)
                    strc = 3;
                if (strc == 3) {
                    strc = 0;
                }
                else {
                    strc++;
                }
            }
            float p = counter / (TotalFrame() * FrameTime);
            float ats = Projectile.GetOwner().GetTotalAttackSpeed(Projectile.DamageType);
            HandRot += rotV * ats;
            rotV *= (float)Math.Pow(rotC, ats);
            float rot = HandRot;
            if (Projectile.GetOwner().direction < 0)
                rot = (rot.ToRotationVector2() * new Vector2(-1, 1)).ToRotation();
            Projectile.GetOwner().SetHandRotWithDir(rot, Math.Sign(Projectile.velocity.X), strc);
        }
        public override void AI() {
            HandleHandRotation();
            if (Projectile.localAI[2]++ == 0) {
                Projectile.scale *= Projectile.GetOwner().HeldItem.scale;
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 1f / DamageMult() / DamageMult() - 1 }, Projectile.Center);
            }
            if (hitbox == null)
                hitbox = GetInitHitboxs();
            Player player = Projectile.GetOwner();
            Projectile.Center = player.GetDrawCenter();
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 2;
            Projectile.velocity = new Vector2(Math.Sign(Projectile.velocity.X) * Projectile.velocity.Length(), 0);
            Projectile.rotation = Projectile.velocity.ToRotation();
            frameCounter++;
            counter++;
            if (frame == 0)
                CEUtils.AddLight(Projectile.Center, new Color(124, 124, 255));
            if (frameCounter > FrameTime) {
                frameCounter -= FrameTime;
                frame++;
                if (atkType == 6 && frame == 3) {
                    if (Main.myPlayer == Projectile.owner) {
                        int type = ModContent.ProjectileType<WaterBolt>();
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(64, 4), Vector2.Zero, type, Projectile.damage, 0, Projectile.owner, 7, 1);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(-64, 4), Vector2.Zero, type, Projectile.damage, 0, Projectile.owner, 7, -1);
                    }
                }
                if (frame >= TotalFrame()) {
                    frame--;
                    Projectile.Kill();
                    player.itemTime = player.itemAnimation = 0;
                }
            }
        }
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public bool playHitSound = true;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (playHitSound) {
                ScreenShaker.AddShake(new ScreenShaker.ScreenShake((target.Center - Projectile.Center).normalize() * 10, 0.7f));

                playHitSound = false;
                CEUtils.PlaySound("GrassSwordHit1", Main.rand.NextFloat(1.2f, 1.4f), Projectile.Center, volume: CEUtils.WeapSound * 0.3f * DamageMult());
            }

            float impactParticleScale = 1.2f;
            Color impactColor = Color.LightBlue;
            //带Cal后缀是CalamityPorts,Configure签名对齐Calamity原构造不是统一五参
            PRTLoader.NewParticle<PRT_SparkleCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.75f, target.height * 0.75f), Vector2.Zero, impactColor, impactParticleScale).Configure(new Color(150, 101500, 255), 12, 0, 2.5f);

            float sparkCount = 12;
            for (int i = 0; i < sparkCount; i++) {
                float p = Main.rand.NextFloat();
                Vector2 sparkVelocity2 = (target.Center - Projectile.Center).normalize().RotatedByRandom(p * 0.4f) * Main.rand.NextFloat(4, 16 * (2 - p));
                int sparkLifetime2 = (int)((2 - p) * 18);
                float sparkScale2 = 0.4f + (1 - p);
                Color sparkColor2 = Color.Lerp(new Color(80, 80, 255), Color.Aqua, p);
                if (Main.rand.NextBool()) {
                    //AltSpark/LineCal随机二选一,都是CalamityPorts短Configure
                    PRTLoader.NewParticle<PRT_AltSpark>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (1f), sparkColor2, sparkScale2 * (1f)).Configure(false, (int)(sparkLifetime2 * (1.2f)));
                }
                else {
                    PRTLoader.NewParticle<PRT_LineCal>(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), sparkVelocity2 * (Projectile.frame == 7 ? 1f : 0.65f), Main.rand.NextBool() ? Color.Aqua : Color.SkyBlue, sparkScale2 * (Projectile.frame == 7 ? 1.4f : 1f)).Configure(false, (int)(sparkLifetime2 * (Projectile.frame == 7 ? 1.2f : 1f)));
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= DamageMult();
        }

        public override bool PreDraw(ref Color lightColor) {
            if (true) {
                Texture2D tex = TextureAssets.Item[ModContent.ItemType<TrainingSword>()].Value;
                int dir = rotV > 0 ? 1 : -1 * Projectile.velocity.X > 0 ? 1 : -1;
                float rot = (dir > 0 ? MathHelper.ToRadians(70) : MathHelper.ToRadians(-70)) + (HandRot.ToRotationVector2() * new Vector2(Projectile.velocity.X > 0 ? 1 : -1, 1)).ToRotation();
                SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
                Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(0, 0);
                Main.EntitySpriteDraw(tex, Projectile.GetOwner().GetFrontHandPositionImproved(Projectile.GetOwner().compositeFrontArm) - Main.screenPosition, null, lightColor, rot, origin, Projectile.scale * 0.76f, effect);
            }

            Texture2D slash = SlashFrames[atkType];
            int num1 = slash.Height / TotalFrame();
            Rectangle sourceRect = new Rectangle(0, num1 * frame, slash.Width, num1 - 2);
            Main.EntitySpriteDraw(slash, Projectile.Center + new Vector2(16 * Projectile.GetOwner().direction, -16) * Projectile.scale - Main.screenPosition, sourceRect, Color.White, 0, new Vector2(slash.Width / 2f, (num1 - 2) / 2), Projectile.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            //if (hitbox != null)
            //hitbox.Testing_DrawBox(Color.Yellow, 2, Projectile.Center, 0, Projectile.velocity.X < 0, Projectile.scale);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (hitbox != null)
                return hitbox.CheckCollidingBetter(Projectile.Center, targetHitbox, 0, frame, Projectile.velocity.X < 0, Projectile.scale);
            return false;
        }
        public override void CutTiles() {
            //Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (86 * (Projectile.ai[0] == 2 ? 1.24f : 1)) * Projectile.scale, 84, DelegateMethods.CutTiles);
        }
    }

}
