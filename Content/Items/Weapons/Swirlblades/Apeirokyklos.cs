using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Particles;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class Apeirokyklos : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.DamageType = CEUtils.RogueDC;
            Item.useAnimation = Item.useTime = 30;
            Item.width = 42;
            Item.height = 46;
            Item.damage = 900;
            Item.crit = 10;
            Item.ArmorPenetration = 40;
            Item.UseSound = CEUtils.GetSound("ApeirokyklosThrow", 1, 12, 0.5f) with { PitchRange = (0.3f, 0.55f) };
            Item.value = CalamityGlobalItem.RarityCalamityRedBuyPrice;
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.shoot = ModContent.ProjectileType<ApeirokyklosProj>();
            Item.shootSpeed = 32f;
            Item.knockBack = 2f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override float StealthDamageMultiplier => 0.9f;
        public override float StealthVelocityMultiplier => 1.2f;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (player.Calamity().StealthStrikeAvailable() && p.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[p].Calamity().stealthStrike = true;
                CEUtils.SyncProj(p);
            }
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<SawofMacrocosm>())
                .AddIngredient(ModContent.ItemType<WyrmTooth>(), 12)
                .AddIngredient(ModContent.ItemType<FadingRunestone>(), 1)
                .AddTile(ModContent.TileType<AbyssalAltarTile>())
                .Register();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class ApeirokyklosProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<Apeirokyklos>();
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
            Projectile.MaxUpdates = 2;
            Projectile.tileCollide = false;
            Projectile.light = 1;
        }
        public override int BladeOpenTime => 15;
        public override float Radius => 210 * (Projectile.Calamity().stealthStrike ? (stealthHitted ? 0.7f : 1.5f) : 1);
        public override int FlyTime => 600;
        public override Rectangle CollisionRect => Projectile.Center.getRectCentered(90, 90);
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers);
        }
        public override int SpreadTime => Projectile.Calamity().stealthStrike ? 120 : 56;
        public bool stealthHitted = false;
        public float chargeBloom = 0;
        public override int OldPosLength => 18;
        public override bool CollideWithNPC => !stealthHitted;
        public override void OnCollideWithNPC(NPC npc)
        {
            if (Projectile.Calamity().stealthStrike)
            {
                stealthHitted = true;
                Projectile.velocity = Projectile.velocity.RotatedByRandom(0.2f).normalize() * 30 * -1;
                CEUtils.PlaySound("ApeirokyklosCharging", 1, Projectile.Center);
                CEUtils.PlaySound("ApeirokyklosCharging", 1, Projectile.Center);
                CEUtils.PlaySound("crystalShieldBreak", 1.4f, npc.Center);
                chargeBloom = 1;
                Counter = FlyTime - 80;
                ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.ScreenShake(Projectile.velocity.normalize() * 6, 12), Main.LocalPlayer.Distance(npc.Center), 3200);

                for (int i = 0; i < 60; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.6f, 1f) * 6f;
                    dust.velocity = CEUtils.randomPointInCircle(58);
                    dust.noGravity = true;
                    dust.color = Main.rand.NextBool() ? Color.LightBlue : Color.LightSkyBlue;
                    dust.fadeIn = 2f;
                }
                for (float i = 1; i >= 0.1f; i -= 0.1f)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(new Color(100, 180, 255), new Color(180, 200, 255), i), "CalamityMod/Particles/ShatteredExplosion", Vector2.One, CEUtils.randomRot(), 0.005f * i * (Radius / 180f), 10 * 0.07f * (Radius / 180f) * i * i, 12 + (int)(i * 12)));
                }
                if(Main.myPlayer == Projectile.owner)
                {
                    CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromThis(), Projectile.GetOwner(), Projectile.Center, Projectile.damage * 6, 370, Projectile.DamageType);
                }
            }
        }
        public override void AI()
        {
            base.AI();
            CEUtils.AddLight(Projectile.Center, new Color(230, 230, 255));
            if (!stealthHitted && Counter > 25)
            {
                if (Counter < FlyTime)
                {
                    Counter = FlyTime;
                }
            }
            if (stealthHitted)
            {
                Projectile.velocity *= 0.95f;
            }
            if (chargeBloom > 0)
                chargeBloom -= 0.02f;
            if(Spreaded && Projectile.Calamity().stealthStrike)
            {
                if(stealthHitted && Counter % 6 == 0)
                {
                    NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 4000);
                    if (target != null)
                    {
                        Vector2 pos = Projectile.Center + CEUtils.randomPointInCircle(Radius * 1.2f);
                        float r = (target.Center - pos).ToRotation();
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, r.ToRotationVector2() * 32, ModContent.ProjectileType<ApeirokyklosSpike>(), (int)(Projectile.damage * 1f), Projectile.knockBack * 12, Projectile.owner, Main.rand.NextFloat(300, 324), 0.88f);
                    }
                }
            }
            SpikeCD--;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for (int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++)
                {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = Color.LightBlue * 0.58f * p;
                    Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                }
                for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
                {
                    Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, overridePos: Projectile.Center + i.ToRotationVector2() * 4));
                }
                Main.spriteBatch.ExitShaderRegion();

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 24 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(90, 90, 255));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center));
            if (BladeScale > 0)
            {
                Texture2D smear = CEUtils.getExtraTex("CircularSmear");
                Texture2D co = CEUtils.getExtraTex("Corona");
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                void Draw()
                {
                    Main.spriteBatch.Draw(co, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 40, 255) * Projectile.Opacity * BladeScale, time * 18f, co.Size() * 0.5f, scale * 0.25f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(co, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(40, 40, 255) * Projectile.Opacity * BladeScale, time * -18f, co.Size() * 0.5f, scale * 0.25f, SpriteEffects.None, 0);

                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(140, 140, 255) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 1f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(90, 90, 255) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 0.98f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(90, 90, 255) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 0.97f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(90, 90, 255) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.96f, SpriteEffects.None, 0);
                }
                void Draw2()
                {
                    Main.spriteBatch.Draw(co, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity * BladeScale, time * 18f, co.Size() * 0.5f, scale * 0.25f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(co, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity * BladeScale, time * -18f, co.Size() * 0.5f, scale * 0.25f, SpriteEffects.None, 0);

                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.975f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 0.96f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 0.85f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(255, 255, 255) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.8f, SpriteEffects.None, 0);
                }
                Main.spriteBatch.UseBlendState(CEUtils.SubtractiveBlending);
                Draw2();
                ApplyShader(new Color(240, 240, 255));
                Draw();
            }
            if (chargeBloom > 0)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Texture2D b = CEUtils.getExtraTex("BloomRing");
                Main.spriteBatch.Draw(b, Projectile.Center - Main.screenPosition, null, Color.LightBlue, 0, b.Size() * 0.5f, Projectile.scale * 3 * chargeBloom * chargeBloom * chargeBloom, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(b, Projectile.Center - Main.screenPosition, null, Color.LightBlue, 0, b.Size() * 0.5f, Projectile.scale * 3 * chargeBloom * chargeBloom, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(b, Projectile.Center - Main.screenPosition, null, Color.LightBlue, 0, b.Size() * 0.5f, Projectile.scale * 3 * chargeBloom, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.75f, 1f), Projectile.Center);
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i / 12f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.LightBlue : Color.SkyBlue, new Vector2(2.4f, 0.6f), true));
        }
        public int SpikeCD = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<LifeOppress>(300);
            if (!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.4f, 1.7f), target.Center,  60, 0.6f);
            if(!stealthHitted && SpikeCD <= 0)
            {
                SpikeCD = 2;
                float r = Main.rand.NextFloat(-0.5f, 0.5f) + MathHelper.PiOver2;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center + r.ToRotationVector2() * Main.rand.NextFloat(700, 800), r.ToRotationVector2() * -16, ModContent.ProjectileType<ApeirokyklosSpike>(), (int)(Projectile.damage * 0.48f), Projectile.knockBack * 8, Projectile.owner, Main.rand.NextFloat(235, 250), 0.85f);
            }
            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, 0.04f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.LightBlue : Color.SkyBlue, new Vector2(2.4f, 0.6f), true));
        }
    }
    public class ApeirokyklosSpike : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.timeLeft = 26;
            Projectile.MaxUpdates = 1;
            Projectile.localNPCHitCooldown = -1;
        }
        public float Length { get { return Projectile.ai[2]; } set { Projectile.ai[2] = value; } }
        public float LengthVel { get { return Projectile.ai[0]; } set { Projectile.ai[0] = value; } }
        public float LengthVelMul { get { return Projectile.ai[1]; } set { Projectile.ai[1] = value; } }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.normalize() * Length, targetHitbox, 40);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.5f, 1.8f), target.Center, 60, 0.4f);
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.6f, 1f) * 3.6f;
                dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.07f) * Main.rand.NextFloat(0.5f, 1) * 38;
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.LightBlue : Color.LightSkyBlue;
                dust.fadeIn = 2f;
            }
            target.AddBuff<LifeOppress>(300);
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Length += LengthVel;
            LengthVel *= LengthVelMul;
            if (Projectile.Entropy().FirstFrames)
            {
                ScreenShaker.AddShakeWithRangeFade(new ScreenShaker.NoDirQuickShake(8), Main.LocalPlayer.Distance(Projectile.Center), 3200);
                CEUtils.PlaySound("AbyssalSpikeHit" + Main.rand.Next(0, 3), Main.rand.NextFloat(0.5f, 0.8f), Projectile.Center, 100, 0.32f);

                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.6f, 1f) * 3f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.1f) * Main.rand.NextFloat(0.5f, 1) * 46;
                    dust.noGravity = false;
                    dust.color = Main.rand.NextBool() ? Color.LightBlue : Color.LightSkyBlue;
                    dust.fadeIn = 2f;
                }
            }
            for (float i = 0; i <= 1; i += 0.05f)
            {
                CEUtils.AddLight(Projectile.Center + Projectile.rotation.ToRotationVector2() * Length, Color.LightBlue);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> points = new List<CEUtils.VertexPointSets>();
            for (float i = 1; i >= 0; i -= 0.0025f)
            {
                float alpha = 1;
                if (i < 0.1f)
                    alpha = i * 10;
                float w = 30;
                if (i > 0.8f)
                    w *= 1 - (i - 0.8f) / 0.2f;
                if (Projectile.timeLeft < 7)
                    w *= (Projectile.timeLeft / 7f);
                points.Add(new CEUtils.VertexPointSets(Projectile.Center + Projectile.rotation.ToRotationVector2() * Length * i, Color.White * alpha * Projectile.Opacity, w * Projectile.scale, 0));
            }
            ThalassianWaterBolt.DrawTrail(points, new Color(180, 255, 255), new Color(20, 140, 255));
            return false;
        }
    }
}
