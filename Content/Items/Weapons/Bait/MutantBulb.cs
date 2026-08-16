using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Projectiles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class MutantBulb : ModItem, IBaitItem
    {
        public static int TagDamage = 7;
        public static float DamageMult = 1.0f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.knockBack = 0;
            Item.shootSpeed = 39;
            Item.useAnimation = Item.useTime = 22;
            Item.value = CalamityGlobalItem.RarityLimeBuyPrice;
            Item.rare = ItemRarityID.Lime;
            Item.width = 50;
            Item.height = 50; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            var snd = CEUtils.GetSound("BaitThrow", 1, 8);
            snd.PitchRange = (0.1f, 0.4f);
            Item.UseSound = snd;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<MutantBulbProjectile>();
            Item.autoReuse = true;
        }
        public override bool CanUseItem(Player player)
        {
            return player.Entropy().BaitUsable;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, (int)(damage * DamageMult), knockback, player.whoAmI, 0, 0, TagDamage);
            return false;
        }

        public override void AddRecipes()
        {
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class MutantBulbProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<MutantBulb>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 46;
            Projectile.light = 1;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.GetOwner().Entropy().BaitCharge--;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (StickNPC < 0)
            {
                if (Counter > 12)
                {
                    Projectile.velocity.Y += 0.26f;
                }
            }
            else
            {
                NPC npc = StickNPC.ToNPC();
                if (!npc.active)
                {
                    Projectile.Kill();
                    return;
                }
                Main.player[Projectile.owner].MinionAttackTargetNPC = npc.whoAmI;
                npc.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 2;
                if (IsActive)
                {
                    Projectile.GetOwner().Calamity().mouseWorldListener = true;
                    npc.GetGlobalNPC<WhipDebuffNPC>().ClearBaitTags();
                    npc.GetGlobalNPC<WhipDebuffNPC>().Tags.Add(new WhipTag(this.GetType().Name, 5, this.TagDamage, 1, 0, this.GetType().Name) { IsABaitTag = true });
                }
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if (ActiveCounter > 120 && ActiveCounter < 160)
                {
                    if(ActiveCounter % 2 == 0)
                    {
                        SetActive();
                        if(ActiveCounter < 158)
                            IsActive = true;
                        CEUtils.SyncProj(Projectile.whoAmI);
                    }
                }
            }
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
            if (StickNPC >= 0)
            {
                oldPos.Clear();
            }
            else
            {
                for (float i = 0; i < 1; i += 0.1f)
                {
                    oldPos.Add(Projectile.Center + Projectile.velocity * i);
                    if (oldPos.Count > 80)
                        oldPos.RemoveAt(0);
                }
            }
        }
        public override void ActiveEffect(float damageMul)
        {
            if(Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), StickNPC.ToNPC().Center + CEUtils.randomPointInCircle(60) + StickNPC.ToNPC().velocity * 36, (new Vector2(0, -16)).RotatedByRandom(0.5f), ModContent.ProjectileType<MutantVine>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner, 24);
            }
        }
        public float activeEffectAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            if (activeEffectAlpha >= 0.01f)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Texture2D pulse = CEUtils.getExtraTex("SoftRoundExplosion");
                for(float i = 0; i < 1f; i += 0.5f)
                {
                    float scale = CEUtils.Frac(i - Main.GlobalTimeWrappedHourly * 1.4f);
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.Pink * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.12f, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.GetTexture();
            for (int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, Color.White * p * 0.2f, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
        public override bool? CanDamage()
        {
            return StickNPC == -1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.tileCollide = false;
            OnHitEffect(Projectile.Center);
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("BaitHit", Main.rand.NextFloat(0.55f, 0.7f), pos);
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(2f, 2.5f);
                dust.velocity = (new Vector2(35, 35).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                dust.noGravity = false;
                dust.color = Color.Pink * 1.2f;
                dust.fadeIn = 2f;
            }
        }
        public override void OnKill(int timeLeft)
        {
            if(timeLeft > 0 && !Main.dedServ)
            {
                OnHitEffect(Projectile.Center);
            }
        }
    }
    public class MutantVine : EBookBaseProjectile
    {
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.timeLeft = 100;
            Projectile.light = 1;
            Projectile.localNPCHitCooldown = -1;
        }
        public float Length = 2400;
        public float LengthNow = 0;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center - Projectile.rotation.ToRotationVector2() * Length, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length, targetHitbox, 40);
        }
        
        public override void AI()
        {
            if (Projectile.ai[0] == 22)
                CEUtils.PlaySound("typ3", Main.rand.NextFloat(2f, 2.4f), Projectile.Center, 100, 0.6f);
            if (Projectile.ai[0]-- == -10)
            {
                CEUtils.PlaySound("VineSpawn", Main.rand.NextFloat(1.5f, 1.8f), Projectile.Center, 36);
            }
            if (Projectile.ai[0] < 0)
            {
                if (LengthNow < 1)
                    LengthNow += 0.05f;
            }
            if (Projectile.ai[0] < -20)
            {
                Projectile.scale -= 0.05f;
                if(Projectile.scale <= 0)
                {
                    Projectile.Kill();
                }
            }
        }
        public override bool? CanDamage()
        {
            return Projectile.ai[0] < 0;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 60;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.GetGlobalNPC<WhipDebuffNPC>().BaitStick > 0)
            {
                target.GetGlobalNPC<WhipDebuffNPC>().BaitStick = 0;
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.ModProjectile != null && p.ModProjectile is BaitProj ibp && ibp.StickNPC == target.whoAmI)
                    {
                        if (!ibp.IsActive)
                            p.Kill();
                    }
                }
            }
            CEUtils.PlaySound("VoidBomb", 1.4f, target.Center);
            for(int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(CEUtils.randomPoint(target.getRect()), Projectile.velocity.normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(6, 18), true, 20, Main.rand.NextFloat(0.02f, 0.03f), Color.LightGreen, new Vector2(0.2f, 1), false, false));
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D vine = Projectile.GetTexture();
            if (Projectile.ai[0] < 24)
            {
                Main.spriteBatch.UseAdditiveClamp();
                float v = Projectile.ai[0] / 24f;
                v = 1 - v * v;
                if (Projectile.ai[0] <= 0)
                    v = 1 * Projectile.scale;

                CEUtils.drawLineBetter(Projectile.Center - Projectile.velocity.normalize() * Length / 2f, Projectile.Center + Projectile.velocity.normalize() * Length / 2, new Color(60, 255, 60) * v, 8 * v);
                CEUtils.drawLineBetter(Projectile.Center - Projectile.velocity.normalize() * Length / 2f, Projectile.Center + Projectile.velocity.normalize() * Length / 2, new Color(160, 255, 160) * v, 2 * v);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.UseSampleState(SamplerState.PointWrap);
            int lg = (int)(Length * LengthNow * 2);
            Main.spriteBatch.Draw(vine, Projectile.Center - Main.screenPosition - Projectile.velocity.normalize() * Length, new Rectangle(-lg, 0, lg, vine.Height), lightColor, Projectile.velocity.ToRotation(), new Vector2(Length / 2, vine.Height / 2), new Vector2(1, Projectile.scale * 2), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
