using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Particles;
using CalamityMod.Rarities;
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
    public class SacredStone : ModItem, IBaitItem
    {
        public static int TagDamage = 15;
        public static float DamageMult = 0.25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.knockBack = 0;
            Item.shootSpeed = 44;
            Item.useAnimation = Item.useTime = 24;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<Turquoise>();
            Item.width = 52;
            Item.height = 54; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            var snd = CEUtils.GetSound("SwingMid", 1, 8);
            snd.PitchRange = (0.2f, 0.4f);
            snd.Volume = 0.6f;
            Item.UseSound = snd;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<SacredStoneProjectile>();
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
    public class SacredStoneProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<SacredStone>();
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
                    Projectile.velocity.Y += 0.1f;
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
                if (ActiveCounter > 120)
                {
                    if (IsActive)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                        SetActive();
                        Projectile.Kill();
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
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SacredStoneSummon>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner);
            }
        }
        public float activeEffectAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            float pn = ActiveCounter / 120f;
            if (activeEffectAlpha >= 0.01f)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Texture2D pulse = CEUtils.getExtraTex("SoftRoundExplosion");
                Texture2D pulse2 = CEUtils.getExtraTex("ShatteredExplosion");
                for (float i = 0; i < 1f; i += 0.2f)
                {
                    float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly * 12);
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.Orange * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.2f * pn, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(pulse2, Projectile.Center - Main.screenPosition, null, Color.Orange * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse2.Size() * 0.5f, scale * Projectile.scale * 0.24f * pn,SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseAdditiveClamp();
            for (int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, Color.White * p * 0.5f, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));

            Main.spriteBatch.UseAdditiveClamp();
            Texture2D star = CEUtils.getExtraTex("StarChromatic");
            float sc = Projectile.scale;
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White, 0, star.Size() * 0.5f, pn * sc * 0.18f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.Orange, 0, star.Size() * 0.5f, pn * sc * 0.24f, SpriteEffects.None, 0);
            CEUtils.DrawGlow(Projectile.Center, Color.Orange, 2f * sc, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.White * 0.7f, 1.2f * sc, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.White, 1.4f * sc * pn, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.Orange, 2f * sc * pn, true, null, false);
            Main.spriteBatch.ExitShaderRegion();
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
            CEUtils.PlaySound("RockCrumble", Main.rand.NextFloat(1.6f, 1.9f), pos);
            for (int i = 0; i < 3; i++)
            {
                float r = 0;
                float scale = 0.6f + 1.2f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.White, Color.Orange, (i / 3f)), "CalamityEntropy/Assets/Extra/StarTexture", new Vector2(0.3f, 1f), r, scale * 0.2f, scale, 12 + i * 2, true, 1));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.White, Color.Orange, (i / 3f)), "CalamityEntropy/Assets/Extra/StarTexture", new Vector2(1f, 0.3f), r, scale * 0.2f, scale, 12 + i * 2, true, 1));
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
    public class SacredStoneSummon : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.timeLeft = 140;
            Projectile.light = 1;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public float Counter { get { return Projectile.ai[0]; }
            set { Projectile.ai[0] = value; } }
        public int ShootDelay = 40;
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if(Counter == 0)
            {
                Projectile.velocity = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(40, 46);
            }
            if (Counter < 40)
                Projectile.timeLeft++;
            Projectile.velocity *= 0.94f;
            ShootDelay--;
            NPC target = Projectile.FindMinionTarget(3200);
            if (target != null && Projectile.timeLeft > 25)
            {
                if(ShootDelay <= 0)
                {
                    ShootDelay = 2;
                    int type = ModContent.ProjectileType<SacredShoot>();
                    if (Main.myPlayer == Projectile.owner)
                    {
                        for(int i = -1; i <= 1; i++)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center).normalize() * 18, type, Projectile.damage, 2, Projectile.owner, i * 8);
                        }
                    }
                }
            }
            Counter++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float pn = 0.2f + 0.1f * (float)(Math.Sin(Counter * 1.5f)) + Projectile.timeLeft / 130f;
            Main.spriteBatch.UseAdditiveClamp();
            Texture2D star = CEUtils.getExtraTex("StarChromatic");
            float sc = Projectile.scale * float.Min(1, Projectile.timeLeft / 20f);
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White, 0, star.Size() * 0.5f, pn * sc * 0.18f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.Orange, 0, star.Size() * 0.5f, pn * sc * 0.24f, SpriteEffects.None, 0);
            CEUtils.DrawGlow(Projectile.Center, Color.Orange, 2f * sc, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.White * 0.7f, 1.2f * sc, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.White, 1.4f * sc * pn, true, null, false);
            CEUtils.DrawGlow(Projectile.Center, Color.Orange, 2f * sc * pn, true, null, false);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    public class SacredShoot : EBookBaseProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.timeLeft = 360;
            Projectile.MaxUpdates = 4;
        }
        public List<Vector2> odp = new List<Vector2>();
        public float whitel = 1;
        public override void AI()
        {
            whitel *= 0.9f;
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.localAI[0] = Main.GameUpdateCount * -0.36f;
                CEUtils.PlaySound("YharonFireball1", Main.rand.NextFloat(1f, 1.25f), Projectile.Center);
            }
            Vector2 adv = Projectile.velocity + Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize() * Projectile.ai[0] * (float)(Math.Sin(Projectile.localAI[0]++ * 0.44f));
            Projectile.rotation = adv.ToRotation();
            for (float i = 0; i < 1f; i += 0.1f)
            {
                odp.Add(Projectile.Center + adv * i);
                if (odp.Count > 180)
                {
                    odp.RemoveAt(0);
                }
            }
            Projectile.position += adv;
            CEUtils.AddLight(Projectile.Center, Color.Orange, Projectile.scale);
        }
        public static SoundStyle OnHitSound = SoundID.DD2_BetsyFireballShot with { PitchRange = (-0.2f, 0.2f), MaxInstances = 128};
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.timeLeft > 4)
                Projectile.timeLeft = 4;
            SoundEngine.PlaySound(OnHitSound, Projectile.Center);
            float scale = Projectile.scale;
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.6f, 1f) * scale * 3f;
                dust.velocity = CEUtils.randomPointInCircle(1) * 40 * scale;
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.Orange : Color.OrangeRed;
                dust.fadeIn = 2f;
            }
            EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, Color.Orange, 0.32f, 1, true, BlendState.Additive, 0, 10);
            EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, Color.White, 0.18f, 1, true, BlendState.Additive, 0, 10);

        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for (int i = 0; i < odp.Count; i++)
            {
                float p = (i / (odp.Count - 1f));
                float alpha = p < 0.5f ? p / 0.5f : 1;
                float width = 1;
                if (p < 0.75f)
                    width = p / 0.75f;
                else
                    width = CEUtils.Parabola(0.5f + (p - 0.75f) / 0.5f, 1);
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 14 * Projectile.scale * width, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), Color.Lerp(Color.OrangeRed, Color.White * 0.8f, whitel));
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += target.defense * 2;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
