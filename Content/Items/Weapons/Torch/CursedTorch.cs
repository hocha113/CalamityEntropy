using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Torch
{
    public class CursedTorch : ModItem
    {
        public override void SetStaticDefaults() {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 2;
        }
        public override void SetDefaults() {
            Item.damage = 10;
            Item.DamageType = DamageClass.Summon;
            Item.width = 46;
            Item.height = 46;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<CursedTorchMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(gold: 5);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item8;
            Item.noMelee = true;
            Item.mana = 5;
            Item.buffType = ModContent.BuffType<CursingFire>();
            Item.rare = ItemRarityID.Orange;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            player.AddBuff(Item.buffType, 3);
            int projectile = Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;
            return false;
        }
    }
    public class CursingFire : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<CursedTorchMinion>();
    }
    public class CursedTorchMinion : ModProjectile
    {
        //咒眼贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/Torch/CursedEye")]
        internal static Asset<Texture2D> CursedEyeTex;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 2;
            Projectile.light = 0.4f;
        }
        public override bool? CanHitNPC(NPC target) {
            return false;
        }
        public override bool? CanCutTiles() {
            return false;
        }

        public override void AI() {
            Player player = Projectile.GetOwner();
            Projectile.MinionCheck<CursingFire>();

            if (Projectile.localAI[0] == 0) {
                for (int i = 0; i < 64; i++) {
                    //FlameCal CalamityPorts,寿命走Configure尾参
                    PRTLoader.NewParticle<PRT_FlameCal>(base.Projectile.Center, CEUtils.randomPointInCircle(6), Color.Yellow, 0.05f).Configure(20, Main.rand.NextFloat(0.3f, 0.6f), Color.Firebrick);
                }
            }
            //拖尾FlameCal单发,Configure寿命/color参数跟批量spawn那套一样
            PRTLoader.NewParticle<PRT_FlameCal>(base.Projectile.Center, new Vector2(Main.rand.NextFloat(-2, 2), -10f).RotatedByRandom(0.004999999888241291) * Main.rand.NextFloat(0.8f, 1f), Color.Yellow, 0.05f).Configure(18, Main.rand.NextFloat(0.5f, 0.65f), Color.Firebrick);
            PRTLoader.NewParticle<PRT_FlameCal>(base.Projectile.Center, new Vector2(Main.rand.NextFloat(-2, 2), -10f).RotatedByRandom(0.004999999888241291) * Main.rand.NextFloat(0.8f, 1f), Color.Yellow, 0.05f).Configure(18, Main.rand.NextFloat(0.5f, 0.65f), Color.Firebrick);
            if (Projectile.localAI[0]++ < 3) {
                Projectile.timeLeft++;
                return;
            }
            if (Projectile.Distance(player.Center) > 4000) {
                Projectile.Center = player.Center + CEUtils.randomPointInCircle(128);
            }
            Projectile.pushByOther(0.6f);
            Projectile.rotation = Projectile.velocity.X * 0.02f;
            Projectile.velocity *= 0.95f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 4)
                    Projectile.frame = 0;
            }
            NPC target = Projectile.FindMinionTarget(1400, true);
            if (target != null) {
                if (CEUtils.getDistance(target.Center + new Vector2(0, -60), Projectile.Center) > 240)
                    Projectile.velocity += (target.Center + new Vector2(0, -60) - Projectile.Center).normalize() * 1.4f;
                if (Projectile.ai[0]++ > 12) {
                    Projectile.ai[0] = 0;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(0, -16).RotatedBy(Projectile.rotation), (target.Center + target.velocity * 6 - Projectile.Center - Projectile.velocity * 10).normalize() * 8 + Projectile.velocity * 0.25f, ModContent.ProjectileType<CursingFlame>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                }
            }
            else {
                if (CEUtils.getDistance(Projectile.Center, player.Center) > 200)
                    Projectile.velocity += (player.Center - Projectile.Center).normalize() * 0.9f;
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = Projectile.GetTexture();
            Texture2D eye = CursedEyeTex.Value;
            int total = 5;
            int num = tex.Height / total;
            Rectangle frame = new Rectangle(0, num * Projectile.frame, tex.Width, num - 2);
            Main.EntitySpriteDraw(eye, Projectile.Center - Main.screenPosition + new Vector2(0, -16).RotatedBy(Projectile.rotation), null, Color.White, Projectile.rotation, eye.Size() / 2f, Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, new Vector2(tex.Width / 2f, num / 2 - 1), Projectile.scale, (Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));
            return false;
        }
    }
    public class CursingFlame : ModProjectile
    {
        //雾团贴图,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Particles/MediumMist")]
        internal static Asset<Texture2D> MediumMistTex;
        public override string Texture => "CalamityEntropy/Assets/Extra/Ports/FireProj";
        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public static int Lifetime => 100;
        public static int Fadetime => 30;
        public ref float Time => ref Projectile.ai[0];
        public int MistType = -1;

        public override void SetDefaults() {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.ArmorPenetration = 16;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.tileCollide = false;
        }

        public override void AI() {
            Time++;
            if (Time < Fadetime && Main.rand.NextBool(6)) {
                Vector2 cinderPos = Projectile.Center + Main.rand.NextVector2Circular(60f, 60f) * Utils.Remap(Time, 0f, Lifetime, 0.5f, 1f);
                float cinderSize = Utils.GetLerpValue(2f, 4f, Time, true);
                Dust cinder = Dust.NewDustDirect(cinderPos, 4, 4, DustID.CursedTorch, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f);
                if (Main.rand.NextBool(3)) {
                    cinder.scale *= 2f;
                    cinder.velocity *= 2f;
                }
                cinder.noGravity = true;
                cinder.scale *= cinderSize * 1.2f;
                cinder.velocity += Projectile.velocity * Utils.Remap(Time, 0f, Fadetime * 0.75f, 1f, 0.1f) * Utils.Remap(Time, 0f, Fadetime * 0.1f, 0.1f, 1f);
            }
            if (Projectile.timeLeft < 16)
                Projectile.Opacity = Projectile.timeLeft / 16f;
            if (MistType == -1)
                MistType = Main.rand.Next(3);

            Lighting.AddLight(Projectile.Center, 0.2f, 0.6f, 0.2f);
        }


        public override void ModifyDamageHitbox(ref Rectangle hitbox) {
            int size = (int)Utils.Remap(Time, 0f, Fadetime, 4f, 15f);

            if (Time > Fadetime)
                size = (int)Utils.Remap(Time, Fadetime, Lifetime, 15f, 0f);
            hitbox.Inflate(size, size);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D fire = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Texture2D mist = MediumMistTex.Value;

            // The conga line of colors to sift through
            Color color1 = new Color(178, 255, 170, 200);
            Color color2 = new Color(144, 255, 145, 70);
            Color color3 = new Color(190, 255, 185, 100);
            Color color4 = new Color(126, 255, 120, 100);
            float length = ((Time > Fadetime - 10f) ? 0.1f : 0.15f);
            float vOffset = Math.Min(Time, 20f);
            float timeRatio = Utils.GetLerpValue(0f, Lifetime, Time);
            float fireSize = Utils.Remap(timeRatio, 0.2f, 0.5f, 0.2f, 0.4f);

            if (timeRatio >= 1f)
                return false;

            for (float j = 1f; j >= 0f; j -= length) {
                Color fireColor = ((timeRatio < 0.1f) ? Color.Lerp(Color.Transparent, color1, Utils.GetLerpValue(0f, 0.1f, timeRatio)) :
                ((timeRatio < 0.2f) ? Color.Lerp(color1, color2, Utils.GetLerpValue(0.1f, 0.2f, timeRatio)) :
                ((timeRatio < 0.35f) ? color2 :
                ((timeRatio < 0.7f) ? Color.Lerp(color2, color3, Utils.GetLerpValue(0.35f, 0.7f, timeRatio)) :
                ((timeRatio < 0.85f) ? Color.Lerp(color3, color4, Utils.GetLerpValue(0.7f, 0.85f, timeRatio)) :
                Color.Lerp(color4, Color.Transparent, Utils.GetLerpValue(0.85f, 1f, timeRatio)))))));
                fireColor *= (1f - j) * Utils.GetLerpValue(0f, 0.2f, timeRatio, true);
                Color innerColor = Color.Lerp(fireColor, Color.Black * 0.6f, 0.3f);
                innerColor *= Projectile.Opacity;
                Vector2 firePos = Projectile.Center - Main.screenPosition - Projectile.velocity * vOffset * j;
                float mainRot = -j * MathHelper.PiOver2 - Main.GlobalTimeWrappedHourly * (j + 1f) * 2f / length;
                float trailRot = MathHelper.PiOver4 - mainRot;

                Vector2 trailOffset = Projectile.velocity * vOffset * length * 0.5f;
                Main.EntitySpriteDraw(fire, firePos - trailOffset, null, innerColor * 0.25f, trailRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

                Main.EntitySpriteDraw(fire, firePos, null, innerColor, mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

                if (MistType > 2 || MistType < 0)
                    return false;
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Rectangle frame = mist.Frame(1, 3, 0, MistType);
                Main.EntitySpriteDraw(mist, firePos, frame, Color.Lerp(fireColor, Color.White, 0.3f), mainRot, frame.Size() * 0.5f, fireSize, SpriteEffects.None);
                Main.EntitySpriteDraw(mist, firePos, frame, fireColor, mainRot, frame.Size() * 0.5f, fireSize * 3f, SpriteEffects.None);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }
            return false;
        }
    }

}
