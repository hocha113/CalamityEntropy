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
    public class MoonlightCore : ModItem, IBaitItem
    {
        public static int TagDamage = 12;
        public static float DamageMult = 1.7f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.knockBack = 0;
            Item.shootSpeed = 44;
            Item.useAnimation = Item.useTime = 22;
            Item.value = CalamityGlobalItem.RarityRedBuyPrice;
            Item.rare = ItemRarityID.Red;
            Item.width = 38;
            Item.height = 38; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            var snd = CEUtils.GetSound("SwingMid", 1, 8);
            snd.PitchRange = (0.2f, 0.4f);
            snd.Volume = 0.6f;
            Item.UseSound = snd;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<MoonlightCoreProjectile>();
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
    public class MoonlightCoreProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<MoonlightCore>();
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
                        SetActive();
                        IsActive = false;
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
            if(Main.myPlayer == Projectile.owner && IsActive)
            {
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + CEUtils.randomRot().ToRotationVector2() * 380, Vector2.Zero, ModContent.ProjectileType<TrueEye>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner);
                }
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
                    float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly);
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.YellowGreen * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.12f, SpriteEffects.None, 0);
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
            CEUtils.PlaySound("CrystalBreak", Main.rand.NextFloat(1.6f, 1.9f), pos);
            for(int i = 0; i < 16; i++)
            {

            }
            for (int i = 0; i < 5; i++)
            {
                float r = CEUtils.randomRot();
                float scale = 0.8f + 0.3f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.YellowGreen, Color.Silver, (i / 5f)), "CalamityEntropy/Assets/Extra/StarTexture", Vector2.One, r, scale * 0.2f, scale, 12 + i * 2, true, 1));
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
    public class TrueEye : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.timeLeft = 400;
            Projectile.light = 1;
        }
        public float Alpha = 0;
        public Vector2 EyeOffset = Vector2.Zero;
        public override bool? CanDamage()
        {
            return false;
        }
        public float Counter { get { return Projectile.ai[0]; }
            set { Projectile.ai[0] = value; } }
        public int NoTargetCounter = 0;
        public int ShootDelay = 20;
        public int ShootCount = 0;
        public int NoUpdate = Main.rand.Next(0, 50);
        public override void AI()
        {
            if (NoUpdate-- > 0)
                return;
            Player player = Projectile.GetOwner();
            Projectile.frameCounter++;
            if(Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if(Projectile.frame > 3)
                {
                    Projectile.frame = 0;
                }
            }
            if(Projectile.timeLeft == 20)
            {
                Alpha = 1;
            }
            if(Projectile.timeLeft <= 20)
            {
                Alpha -= 0.05f;
                Projectile.velocity *= 0.9f;
                return;
            }
            if (Counter < 21)
            {
                if (Alpha < 1)
                    Alpha += 0.05f;
            }
            else
            {
                ShootDelay--;
                NPC target = Projectile.FindMinionTarget(3000);
                if (target != null)
                {
                    EyeOffset = Vector2.Lerp(EyeOffset, (target.Center - Projectile.Center).normalize() * 18, 0.12f);
                }
                else
                {
                    EyeOffset *= 0.92f;
                }
                if (ShootCount > 6)
                {
                    Projectile.velocity *= 0.9f;
                }
                else
                {
                    if (target != null)
                    {
                        NoTargetCounter = 0;
                        Vector2 targetPos = target.Center + (Projectile.Center - target.Center).normalize() * 340;
                        Projectile.velocity = (targetPos - Projectile.Center) * 0.06f;
                        Projectile.pushByOther(2);
                        if (ShootDelay <= 0)
                        {
                            ShootDelay = 12;
                            ShootCount++;
                            if (ShootCount > 6)
                            {
                                Projectile.timeLeft = 66;
                                Projectile.velocity -= (target.Center - Projectile.Center).normalize() * 14f;
                            }
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Vector2 vel = (target.Center - Projectile.Center).normalize() * 36;
                                if (ShootCount > 6)
                                {
                                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + vel * 0.2f, vel, ModContent.ProjectileType<MoonlightLaser>(), (int)(Projectile.damage * 1.5f), Projectile.knockBack, Projectile.owner, Projectile.velocity.X, Projectile.velocity.Y);
                                }
                                else
                                {
                                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + vel * 0.2f, vel, ModContent.ProjectileType<MoonlightBeam>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                                }
                            }
                        }
                    }
                    else
                    {
                        Projectile.velocity *= 0.9f;
                        NoTargetCounter++;
                        if (NoTargetCounter > 80)
                            if (Projectile.timeLeft > 20)
                                Projectile.timeLeft = 20;
                    }
                }
            }
            
            Counter++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Texture2D eye = this.getTextureAlt("1");
            void Draw(Vector2 pos, Color color)
            {
                int wh = tex.Height / Main.projFrames[Type];
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, new Rectangle(0, wh * Projectile.frame, tex.Width, wh - 2), color, Projectile.rotation, new Vector2(tex.Width / 2, 38), Projectile.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(eye, pos + EyeOffset - Main.screenPosition, null, color, Projectile.rotation, new Vector2(eye.Width / 2, eye.Height / 2), Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.UseAdditiveClamp();
            if (Alpha > 0) 
            {
                for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver2)
                {
                    Draw(Projectile.Center + (i + Main.GlobalTimeWrappedHourly * 16).ToRotationVector2() * (6 + (1 - Alpha) * 80 * Projectile.scale), Color.White * Alpha * 1f);
                }
            }
            Main.spriteBatch.ExitShaderRegion();
            Draw(Projectile.Center, Color.White * Alpha * Alpha * Alpha * Alpha);
            return false;
        }
    }
    public class MoonlightBeam : EBookBaseProjectile
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
            Projectile.light = 1;
        }
        public List<Vector2> odp = new List<Vector2>();
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("VoidBomb", 2f, Projectile.Center);
                SoundEngine.PlaySound(Main.rand.NextBool() ? SoundID.Item124 : SoundID.Item125, Projectile.Center);
            }
            for (float i = 0.05f; i <= 1f; i += 0.05f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 300)
                {
                    odp.RemoveAt(0);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            CEUtils.AddLight(Projectile.Center, Color.Silver, Projectile.scale);
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
            OnKill(Projectile.timeLeft);
        }
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            if (timeLeft > 0)
            {
                float scale = Projectile.scale;
                CEUtils.PlaySound("ThalassianHit", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.6f, 1f) * scale * 3f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.2f) * Main.rand.NextFloat(0.4f, 1f) * 40 * scale;
                    dust.noGravity = true;
                    dust.color = Color.YellowGreen;
                    dust.fadeIn = 2f;
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for (int i = 0; i < odp.Count; i++)
            {
                float p = (i / (odp.Count - 1f));
                float alpha = p < 0.7f ? p / 0.7f : 1;
                float width = 1;
                if (p < 0.8f)
                    width = p / 0.8f;
                else
                    width = CEUtils.Parabola(0.5f + (p - 0.8f) / 0.4f, 1);
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), Color.YellowGreen);
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
    public class MoonlightLaser : EBookBaseProjectile
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
            Projectile.timeLeft = 34;
            Projectile.light = 1;
            Projectile.localNPCHitCooldown = -1;
        }
        public float Length = 2000;
        public int rDir = Main.rand.NextBool() ? 1 : -1;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length, targetHitbox, 64);
        }
        public override void AI()
        {
            if(Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("CruiserDash", 1f, Projectile.Center);
                CEUtils.PlaySound("DoGLaserWallSpawn", 1f, Projectile.Center);

                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/TeslaCannonFire") with { Pitch = 0.6f}, Projectile.Center);
            }
            Projectile.ai[2] = CEUtils.Parabola(Projectile.timeLeft / 34f, 1);
            Projectile.ai[2] = (1 - Projectile.ai[2]);
            Projectile.ai[2] *= Projectile.ai[2];
            Projectile.ai[2] = (1 - Projectile.ai[2]);

            Projectile.rotation = Projectile.velocity.ToRotation() + (CEUtils.GetRepeatedCosFromZeroToOne(Projectile.timeLeft / 34f, 1) - 0.5f) * 0.95f * rDir;
            Projectile.position += new Vector2(Projectile.ai[0], Projectile.ai[1]);
            Projectile.ai[0] *= 0.9f;
            Projectile.ai[1] *= 0.9f;
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
            float scale = Projectile.scale;
            CEUtils.PlaySound("ThalassianHit", Main.rand.NextFloat(0.8f, 1.2f), Projectile.Center);
            for (int i = 0; i < 32; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.7f, 1f) * scale * 4.2f;
                dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.32f) * Main.rand.NextFloat(0.4f, 1f) * 60 * scale;
                dust.noGravity = true;
                dust.color = Color.YellowGreen;
                dust.fadeIn = 2f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            List<CEUtils.VertexPointSets> vp = new();
            for (float i = 1; i >= 0f; i-= 0.0025f)
            {
                Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * i * Length;
                float wm = Projectile.ai[2];
                if (i < 0.1f)
                    wm *= (i / 0.1f);
                if (i > 0.8f)
                    wm *= 1 - (i - 0.8f) / 0.2f;
                vp.Add(new CEUtils.VertexPointSets(pos, Color.White, wm * 32, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), Color.YellowGreen);
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
