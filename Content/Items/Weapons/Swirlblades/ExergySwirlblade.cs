using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class ExergySwirlblade : RogueWeapon
    {
        public override void SetDefaults()
        {
            Item.DamageType = CEUtils.RogueDC;
            Item.useAnimation = Item.useTime = 42;
            Item.width = 70;
            Item.height = 70;
            Item.damage = 70;
            Item.crit = 6;
            Item.ArmorPenetration = 20;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f };
            Item.value = CalamityGlobalItem.RarityRedBuyPrice;
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<ExergySwirlbladeProj>();
            Item.shootSpeed = 49f;
            Item.knockBack = 3f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override float StealthDamageMultiplier => 0.6f;
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
                .AddIngredient(ModContent.ItemType<RunicSwirlblade>())
                .AddIngredient(ModContent.ItemType<MeldBlob>(), 8)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class ExergySwirlbladeProj : BaseSwirlblade
    {
        public override string Texture => CEUtils.ItemTexPath<ExergySwirlblade>();
        public override int OldPosLength => 11;
        public override int FlyTime => Projectile.MaxUpdates * 17;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
            Projectile.width = Projectile.height = 70;
            Projectile.tileCollide = false;
        }
        public override float Radius => 200 * (Projectile.Calamity().stealthStrike ? 1.4f : 1) * Radius2;
        public float OriginalRadius { get { float r2 = Radius2; Radius2 = 1f; float rt = Radius; Radius2 = r2; return rt; } }
        public float Radius2 = 1;
        public override int SpreadTime => Projectile.Calamity().stealthStrike ? 70 : 42;
        public Vector2 OffsetS = Vector2.Zero;
        public int Stick = -1;
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(Stick);
            writer.WriteVector2(OffsetS);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            Stick = reader.ReadInt32();
            OffsetS = reader.ReadVector2();
        }
        public override void OnCollideWithNPC(NPC npc)
        {
            Stick = npc.whoAmI;
            OffsetS = Projectile.Center - npc.Center;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for(int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++)
                {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = Color.LightGreen * 0.6f * p;
                    Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(140, 255, 140));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                Texture2D smear = CEUtils.getExtraTex("CircularSmearAlpha");
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.PointClamp);
                
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.7f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.7f, SpriteEffects.None, 0);

                BaseSwirlblade.ApplyShader(new Color(180, 255, 180));
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.97f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * -40f, o, scale * 0.96f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * 38f, o, scale * 0.95f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.94f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void AI()
        {
            base.AI();
            if(BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                GeneralParticleHandler.SpawnParticle(new AltLineParticle(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), false, Main.rand.Next(12, 16), Main.rand.NextFloat(0.6f, 1f) * 2.2f * BladeScale * Projectile.scale, (Main.rand.NextBool() ? Color.Black : Color.LightGreen) * BladeScale));
            }
            NPC stickNpc = null;
            if(Stick >= 0)
            {
                stickNpc = Stick.ToNPC();
                if(!stickNpc.active)
                {
                    stickNpc = null;
                    Stick = -1;
                }
            }
            float p = (Counter - FlyTime) / (float)SpreadTime;
            p = float.Clamp(p, 0, 1);
            if (Spreaded && Stick >= 0)
            {
                Projectile.Center = stickNpc.Center + OffsetS;
            }
            if(Spreaded)
            {
                Radius2 = 1 - p * 0.5f;
                if (++Projectile.localAI[1] % 13 == 0)
                {
                    int sawType = ModContent.ProjectileType<ExergySwirlbladeSaw>();
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(32, 38) * (Projectile.Calamity().stealthStrike ? 1.5f : 1), sawType, Projectile.damage / 5, 6, Projectile.owner, OriginalRadius * 0.3f);
                    }
                }
            }
            CEUtils.AddLight(Projectile.Center, new Color(200, 255, 200));
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.9f, 1.2f), Projectile.Center);
            for (int i = 0; i < 10; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, (i / 10f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f), (Main.rand.NextBool() ? Color.LightGreen : Color.SeaGreen) * 0.8f, new Vector2(2.4f, 0.6f), true));
        }
        public override void OnRetract()
        {
            if(Projectile.Calamity().stealthStrike)
            {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 2000);
                float rot = target == null ? CEUtils.randomRot() : (target.Center - Projectile.Center).ToRotation();
                int sawType = ModContent.ProjectileType<ExergySwirlbladeSaw>();
                if (Main.myPlayer == Projectile.owner)
                {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, rot.ToRotationVector2() * 50, sawType, Projectile.damage, 6, Projectile.owner, OriginalRadius * 0.5f, 1);
                    p.ToProj().Calamity().stealthStrike = true;
                    CEUtils.SyncProj(p);
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.2f, 1.5f), target.Center, volume: 0.5f);

            for (int i = 0; i < 12; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, false, 11, 0.04f * Main.rand.NextFloat(0.65f, 1f), Main.rand.NextBool() ? Color.LightGreen : Color.LightSeaGreen, new Vector2(2.4f, 0.6f), true));

            float lrot = (Projectile.Center - target.Center).ToRotation() + (Main.rand.NextBool() ? 1 : -1) * 1.2f + Main.rand.NextFloat(-0.1f, 0.1f);
            for(int i = 0; i < 3; i++)
            {
                EParticle.spawnNew(new AbyssalLine() { xadd = 2.4f, lx = 1.8f, endColor = Color.Black, spawnColor = Color.Black }, Projectile.Center + (target.Center - Projectile.Center).normalize() * 66, Vector2.Zero, Color.Black, 1, 1, true, BlendState.NonPremultiplied, lrot, 30);
            }
            EParticle.spawnNew(new AbyssalLine() { xadd = 2f, lx = 1.5f, endColor = Color.LightGreen * 1.2f, spawnColor = Color.LightGreen * 1.2f}, Projectile.Center + (target.Center - Projectile.Center).normalize() * 66, Vector2.Zero, Color.LightGreen, 1, 1, true, BlendState.Additive, lrot, 30);
        }
    }
    public class ExergySwirlbladeSaw : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(CEUtils.RogueDC, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 148;
            Projectile.localNPCHitCooldown = 18;
            Projectile.light = 0.7f;
        }
        public override bool ShouldUpdatePosition()
        {
            return NoPosUpdate <= 0;
        }
        public void SpawnVParticles(int num = 1, float scale = 1)
        {
            float num2 = 360f / num;
            Color color1 = Color.LightGreen;
            Color color2 = Color.Black;
            for (int j = 0; (float)j < num; j++)
            {
                float num3 = CEUtils.randomRot();
                Vector2 vector = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Vector2 vector2 = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Dust dust = Dust.NewDustPerfect(Projectile.Center + vector, Main.rand.NextBool(4) ? ModContent.DustType<LightDust>() : (ModContent.DustType<VoidDustInverted>()), vector2);
                dust.noGravity = dust.type != 278;
                dust.color = color1;
                dust.velocity = vector2 * scale;
                dust.scale = Main.rand.NextFloat(1.6f, 2.2f) * 0.7f * scale;
            }
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                SoundStyle ShootSound = new("CalamityMod/Sounds/Item/SawShot", 2) { PitchRange = (0.2f, 0.7f), Volume = 0.4f };
                SoundEngine.PlaySound(ShootSound, Projectile.Center);
                if (Projectile.ai[1] > 0)
                    Projectile.Calamity().stealthStrike = true;
                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.6f, 1f) * 3f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f, 1) * 44;
                    dust.noGravity = false;
                    dust.color = Main.rand.NextBool() ? Color.LightGreen : Color.LightSeaGreen;
                    dust.fadeIn = 2f;
                }
            }
            SpawnVParticles();
            if (NoPosUpdate > 0)
            {
                NoPosUpdate--;
            }
            else if (CD > 0)
            {
                CD--;
            }
            if (Projectile.timeLeft < 20)
                Projectile.Opacity -= 1 / 20f;
            else if (Projectile.localAI[0] ++ > 9)
                if(Projectile.Calamity().stealthStrike || Projectile.numHits == 0)
                    Projectile.HomingToNPCNearby(4.2f, 0.94f, 1600);
            for(float i = 0.2f; i <= 1f; i += 0.2f)
            {
                oldPos.Add(Projectile.Center + Projectile.velocity * i);
                if (oldPos.Count > 60)
                    oldPos.RemoveAt(0);
            }
        }
        public int NoPosUpdate = 0;
        public int CD = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CD <= 0)
            {
                Projectile.velocity = Projectile.velocity.normalize() * float.Max(Projectile.velocity.Length(), Projectile.Calamity().stealthStrike ? 68 : 54);
                NoPosUpdate = 4;
                CD = 8;
                for (int i = 0; i < 6; i++)
                {
                    float rot = 2;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16) * Projectile.scale, false, 16, Projectile.scale * 0.04f, Color.LightGreen, new Vector2(0.3f, 1), false, false));
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16) * Projectile.scale, false, 16, Projectile.scale * 0.04f, Color.LightGreen, new Vector2(0.3f, 1), false, false));
                }
            }
            for(int ii = 0; ii < 2; ii++)
            {
                float lrot = (Projectile.Center - target.Center).ToRotation() + (Main.rand.NextBool() ? 1 : -1) * 1.2f + Main.rand.NextFloat(-0.1f, 0.1f);
                for (int i = 0; i < 3; i++)
                {
                    EParticle.spawnNew(new AbyssalLine() { xadd = 1.4f, lx = 1.4f, endColor = Color.Black, spawnColor = Color.Black }, target.Center, Vector2.Zero, Color.Black, 1, 1, true, BlendState.NonPremultiplied, lrot, 30);
                }
                EParticle.spawnNew(new AbyssalLine() { xadd = 1.2f, lx = 1.1f, endColor = Color.LightGreen * 1.2f, spawnColor = Color.LightGreen * 1.2f }, target.Center, Vector2.Zero, Color.LightGreen, 1, 1, true, BlendState.Additive, lrot, 30);

            }
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1, 1.4f), target.Center, 8, 0.6f);
            float scale = 1.5f;
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f * Projectile.scale;
                dust.velocity = CEUtils.randomPointInCircle(30 * Projectile.scale);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.LightGreen : Color.LightSeaGreen;
                dust.fadeIn = 2f;
            }
            scale = 1.6f;
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.LightSeaGreen * 0.8f, scale * 1f * Projectile.scale, 1, true, BlendState.Additive, 0, 7);
            EParticle.spawnNew(new ShineParticle(), target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f * Projectile.scale, 1, true, BlendState.Additive, 0, 7);
        }

        public float BladeScale => 1;
        public float Radius => Projectile.ai[0];
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(projHitbox.Center.ToVector2(), Radius * Projectile.scale * BladeScale).Intersects(targetHitbox);
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override bool PreDraw(ref Color lightColor)
        {
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for (int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(140, 255, 140));
            }
            Texture2D smear = CEUtils.getExtraTex("CircularSmearAlpha");
            float scale = Radius / 78f * Projectile.scale * BladeScale;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 o = smear.Size() * 0.5f;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.PointClamp);

            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.7f, SpriteEffects.None, 0);

            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(190, 246, 190) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.9f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(190, 246, 190) * Projectile.Opacity * BladeScale, time * -34f, o, scale * 0.9f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(190, 246, 190) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 0.64f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return (Projectile.Opacity > 0.6f && Projectile.localAI[0] > 10) ? null : false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
