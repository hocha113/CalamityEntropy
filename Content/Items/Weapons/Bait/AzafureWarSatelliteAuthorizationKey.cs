using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class AzafureWarSatelliteAuthorizationKey : ModItem, IBaitItem, IAzafureEnhancable
    {
        public static int TagDamage = 9;
        public static float DamageMult = 2f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 72;
            Item.knockBack = 0;
            Item.shootSpeed = 36;
            Item.useAnimation = Item.useTime = 32;
            Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.width = 38;
            Item.height = 50; 
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<AzafureWarSatelliteAuthorizationKeyProjectile>();
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
            CreateRecipe()
                .AddIngredient<AzafureMicroBeacon>()
                .AddIngredient<ScoriaBar>(8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class AzafureWarSatelliteAuthorizationKeyProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<AzafureWarSatelliteAuthorizationKey>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, -1);
            Projectile.width = Projectile.height = 24;
        }

        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.GetOwner().Entropy().BaitCharge--;
            }
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (StickNPC < 0)
            {
                if (Counter > 15)
                {
                    Projectile.velocity *= 0.99f;
                    Projectile.velocity.Y += 0.6f;
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
                    if (ActiveCounter % 20 == 0)
                        CEUtils.PlaySound("WulfrumPingReady",  2f + ActiveCounter / 280f, Projectile.Center);
                }
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if(ActiveCounter > 120)
                {
                    if(IsActive)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                        SetActive();
                    }
                }
            }
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
        }
        public override void ActiveEffect(float damageMul)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.GetOwner().Center + new Vector2(Main.rand.NextFloat(-100, 100), -800), Vector2.Zero, ModContent.ProjectileType<WarSatellite>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner, 0, Main.rand.Next(0, 30));
            }
        }
        public float activeEffectAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            if (activeEffectAlpha >= 0.01f)
            {
                Main.spriteBatch.UseAdditiveClamp();
                Texture2D pulse = CEUtils.getExtraTex("HollowCircleSoftEdge");
                for(float i = 0; i < 1f; i += 0.5f)
                {
                    float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly * 2f);
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, new Color(255, 80, 80) * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.25f, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();
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
            target.AddBuff<MechanicalTrauma>(260);
            Projectile.tileCollide = false;
            OnHitEffect(Projectile.Center); 
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/WulfrumPing"), target.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/WulfrumProsthesisSucc") with { Volume = 0.34f}, target.Center);
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            target.AddBuff<MechanicalTrauma>(260);
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("ExoHit1", Main.rand.NextFloat(1.2f, 1.5f), pos, 8, 0.65f);
            for(int i = 0; i < 2; i++)
            {
                float scale = 0.05f + 0.02f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.Red, new Color(255, 108, 108), (i / 2f)), "CalamityEntropy/Assets/Extra/HollowCircleSoftEdge", Vector2.One, CEUtils.randomRot(), scale * 0.2f, scale, 12 + i * 4));
            }

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 1, false, 12, 0.04f * Main.rand.NextFloat(0.65f, 1f), new Color(255, 180, 180), new Vector2(2f, 0.6f), true));
        }
        public override void OnKill(int timeLeft)
        {
            if(timeLeft > 0 && !Main.dedServ)
            {
                OnHitEffect(Projectile.Center);
            }
        }
    }
    public class WarSatellite : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            Main.projFrames[Type] = 10;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 32;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 380;
        }
        public int Delay = 60;
        public int ChargeTime = 100;
        public int Leaving = 0;
        public override bool? CanDamage()
        {
            return false;
        }
        public override void AI()
        {
            if(Projectile.Entropy().FirstFrames)
            {
                Projectile.timeLeft += (Projectile.GetOwner().AzafureEnhance() ? 60 : 0);
            }
            if(Projectile.frameCounter++ % 4 == 0)
            {
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
            }
            if (Projectile.timeLeft < 20)
                Projectile.Opacity -= 1 / 20f;
            Player player = Projectile.GetOwner();
            NPC target = Projectile.FindMinionTarget(2000, true);
            if (Projectile.timeLeft < 100)
                if (Leaving < 1)
                    Leaving = 1;
            if (Leaving > 0)
            {
                Leaving++;
                Projectile.velocity.Y -= 1f;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, -MathHelper.PiOver2, 0.12f, false);
            }
            else
            {
                player.Calamity().mouseWorldListener = true;
                Vector2 targetPos = (target == null ? player.Calamity().mouseWorld : target.Center);

                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (targetPos - Projectile.Center).ToRotation(), 0.04f, true);
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, (targetPos - Projectile.Center).ToRotation(), 0.05f, false);

                if (Delay > 0)
                {
                    Delay--;
                    if (Delay == 0)
                        CEUtils.PlaySound("lasercharge", 1, Projectile.Center);
                    Projectile.velocity += (player.Center + new Vector2(0, -300) - Projectile.Center) * 0.01f;
                    Projectile.velocity *= 0.9f;
                }
                else
                {
                    Projectile.velocity *= 0.8f;
                    if (ChargeTime > 0)
                    {
                        ChargeTime--;
                        for (int i = 0; i < (int)(7 * (1 - (ChargeTime / 100f) * (ChargeTime / 100f))); i++)
                        {
                            Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 58 + CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(38, 70);

                            Dust dust = Dust.NewDustPerfect(pos, ModContent.DustType<SquashDust>(), Vector2.Zero);
                            dust.scale = Main.rand.NextFloat(0.6f, 1f) * 2f;
                            dust.velocity = (Projectile.Center + Projectile.rotation.ToRotationVector2() * 58 - pos) * 0.1f;
                            dust.noGravity = true;
                            dust.color = Color.OrangeRed * (1 - (ChargeTime / 100f) * (ChargeTime / 100f));
                            dust.fadeIn = 1.6f;
                        }
                        if (ChargeTime == 0)
                        {
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 58, Projectile.rotation.ToRotationVector2() * 16, ModContent.ProjectileType<SateliteLaser>(), Projectile.damage, 0, Projectile.owner, Projectile.identity);
                            }
                        }
                    }
                }
            }
        }
        public float gunRot = MathHelper.PiOver2;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            int wh = tex.Height / Main.projFrames[Type];
            Rectangle rect = new Rectangle(0, wh * Projectile.frame, tex.Width, wh - 2);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, rect, lightColor, Projectile.rotation - MathHelper.PiOver2, rect.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            float l = (1 - ChargeTime / 100f);
            if (ChargeTime <= 0)
                l *= 1.4f;
            if(Projectile.timeLeft < 120)
            {
                l *= (Projectile.timeLeft - 100) / 20f;
                if (l < 0)
                    l = 0;
            }
            Main.spriteBatch.UseAdditiveClamp();
            Texture2D bloom = CEUtils.getExtraTex("BloomRing");
            if (ChargeTime > 0)
                Main.spriteBatch.Draw(bloom, Projectile.Center + Projectile.rotation.ToRotationVector2() * 54 - Main.screenPosition, null, new Color(255, 150, 150) * (1 - (ChargeTime / 100f) * (ChargeTime / 100f)), 0, bloom.Size() * 0.5f, (ChargeTime / 100f) * 1.6f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 58, Color.OrangeRed, 0.8f * l);
            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 58, Color.White, 0.4f * l);
            CEUtils.DrawGlow(Projectile.Center + Projectile.rotation.ToRotationVector2() * 58, Color.White, 0.4f * l);
            return false;
        }
    }
    public class SateliteLaser : EBookBaseProjectile
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
            Projectile.timeLeft = 120;
            Projectile.light = 1;
            Projectile.localNPCHitCooldown = 6;
        }
        public float Length = 3000;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length, targetHitbox, 24);
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Projectile.timeLeft += (Projectile.GetOwner().AzafureEnhance() ? 60 : 0);
            }
            if (Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("CruiserDash", 1f, Projectile.Center);
                CEUtils.PlaySound("DoGLaserWallSpawn", 1f, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/TeslaCannonFire") with { Pitch = 0.6f }, Projectile.Center);
            }
            Projectile.ai[2] = CEUtils.Parabola(Projectile.timeLeft / (120f + (Projectile.GetOwner().AzafureEnhance() ? 60 : 0)), 1);
            Projectile.ai[2] = (1 - Projectile.ai[2]);
            Projectile.ai[2] *= Projectile.ai[2] * Projectile.ai[2] * Projectile.ai[2];
            Projectile.ai[2] = (1 - Projectile.ai[2]);
            Projectile p = ((int)Projectile.ai[0]).ToProj_Identity();
            Projectile.Center = p.Center + p.rotation.ToRotationVector2() * 58;
            Projectile.rotation = p.rotation;
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * 16;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff<MechanicalTrauma>(260);
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
            float scale = Projectile.scale;
            CEUtils.PlaySound("ThalassianHit", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.rotation.ToRotationVector2() * target.Distance(Projectile.Center), ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.7f, 1f) * scale * 2.2f;
                dust.velocity = CEUtils.randomPointInCircle(32);
                dust.noGravity = true;
                dust.color = Color.OrangeRed;
                dust.fadeIn = 2f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            List<CEUtils.VertexPointSets> vp2 = new();
            for (float i = 1; i >= 0f; i -= 0.0025f)
            {
                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * i * Length;
                float wm = Projectile.ai[2];
                if (i < 0.02f)
                    wm *= (i / 0.02f);
                if (i > 0.9f)
                    wm *= 1 - (i - 0.9f) / 0.1f;
                vp.Add(new CEUtils.VertexPointSets(pos, Color.White, wm * 9, 0));
                vp2.Add(new CEUtils.VertexPointSets(pos + CEUtils.randomPointInCircle(18), Color.White, wm * 5, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), Color.OrangeRed);
            ThalassianWaterBolt.DrawTrail(vp2, new Color(255, 230, 230), Color.Red);
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
