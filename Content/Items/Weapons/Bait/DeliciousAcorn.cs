using CalamityEntropy.Content.Buffs;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class DeliciousAcorn : ModItem, IBaitItem
    {
        public static int TagDamage = 1;
        public static float DamageMult = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 6;
            Item.knockBack = 0;
            Item.shootSpeed = 25;
            Item.useAnimation = Item.useTime = 18;
            Item.value = CalamityGlobalItem.RarityWhiteBuyPrice;
            Item.rare = ItemRarityID.White;
            Item.width = 24;
            Item.height = 38; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<DeliciousAcornProjectile>();
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
                .AddIngredient(ItemID.Acorn, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class DeliciousAcornProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<DeliciousAcorn>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, -1);
            Projectile.width = Projectile.height = 24;
            Projectile.timeLeft = 300;
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
                if (Counter > 12)
                {
                    Projectile.velocity *= 0.99f;
                    Projectile.velocity.Y += 0.8f;
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
                if(ActiveCounter > 120)
                {
                    if(IsActive)
                    {
                        CEUtils.SyncProj(Projectile.whoAmI);
                        SetActive();
                    }
                }
            }
            Counter++;
        }
        public override void ActiveEffect(float damageMul)
        {
            if(Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool f = false;
                    Vector2 randomPos = Projectile.Center + new Vector2(Main.rand.NextFloat(300, 400) * (Main.rand.NextBool() ? 1 : -1), 0);
                    if (CEUtils.isAir(randomPos, true))
                    {
                        for (int c = 0; c < 160; c++)
                        {
                            randomPos.Y += 8;
                            if (CEUtils.HasTile(randomPos, true))
                            {
                                f = true;
                                break;
                            }
                        }
                        randomPos.Y += 64;
                    }
                    else
                    {

                        for (int c = 0; c < 120; c++)
                        {
                            randomPos.Y -= 8;
                            if (CEUtils.isAir(randomPos, true))
                            {
                                f = true;
                                break;
                            }
                        }
                        randomPos.Y += 64 + 8;
                    }
                    if (!f)
                        randomPos = Projectile.Center + new Vector2(0, 200);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), randomPos, Vector2.Zero, ModContent.ProjectileType<SquirrerMinion>(), (int)(Projectile.damage * damageMul), Projectile.knockBack, Projectile.owner, 0, 0, i == 0 ? 1 : 0);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
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
            Projectile.timeLeft = 780;
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            SoundEngine.PlaySound(SoundID.Dig.WithPitchOffset(Main.rand.NextFloat(0.5f, 1f)), Projectile.position);
            SoundEngine.PlaySound(SoundID.Dig.WithPitchOffset(Main.rand.NextFloat(-1f, -0.5f)), Projectile.position); 
            
            int dust_splash = 0;
            while (dust_splash < 18)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(Projectile.Center, new Vector2(Main.rand.NextFloat(15), 0).RotatedByRandom(MathHelper.TwoPi), false, 10, Projectile.ai[0] == 1 ? 1.2f : 0.6f, Projectile.ai[0] == 1 ? Color.GreenYellow : new Color(128, 110, 50), false, true));
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Copper, 0f, 0f, 0, default, 0.5f);
                dust_splash += 1;
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
    public class SquirrerMinion : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.timeLeft = 2400;
            Projectile.width = Projectile.height = 32;
        }
        public int SpawnTime = 30;
        public int dir = 1;
        public int ShootCount = 6;
        public int ShootDelay = 40;
        public int Frame = 0;
        public int ShootFrame = -1;
        public int Leaving = 0;
        public int Counter = 0;
        public int Jump = 0;
        public int Counter2 = 0;
        public bool GrabedArcon = false;
        public int AcornProjType = 0;
        public override void AI()
        {
            if (Counter == 0)
            {
                Projectile.Opacity = 0;
                if(Main.rand.NextBool(180))
                {
                    Projectile.scale *= 3.2f;
                }
                else
                {
                    Projectile.scale *= Main.rand.NextFloat(0.8f, 1.4f);
                }
                Projectile.width = (int)(Projectile.width * Projectile.scale);
                Projectile.height = (int)(Projectile.height * Projectile.scale);
            }
            Counter++;

            NPC target = Projectile.FindMinionTarget();
            if(target != null)
                dir = (Math.Sign(target.Center.X - Projectile.Center.X));
            if (Projectile.velocity.X > 0.1f)
                dir = 1;
            if (Projectile.velocity.X < -0.1f)
                dir = -1;
            if (Projectile.velocity.Length() < 1 && ShootFrame < 0)
            {
                Frame = 1;
                Projectile.frameCounter = 0;
            }
            Projectile.rotation = 0;
            if (Leaving > 0)
            {
                ShootFrame = 0;
                if (Projectile.ai[2] > 0)
                {
                    Vector2 acornPos = Vector2.Zero;
                    Projectile acorn = null;
                    foreach(Projectile p in Main.ActiveProjectiles)
                    {
                        if(p.ModProjectile != null && p.ModProjectile is BaitProj bp && !bp.IsActive)
                        {
                            acorn = p;
                            acornPos = p.Center;
                            break;
                        }
                    }
                    if(Counter2++ > 1800 || acornPos.Distance(Projectile.Center) > 3000 * Projectile.scale || acorn == null || GrabedArcon || Projectile.timeLeft < 60)
                    {
                        Projectile.ai[2] = 0;
                        Leaving = 0;
                        return;
                    }
                    if(Math.Abs(Projectile.velocity.Y) <= 0.01f)
                    {
                        Vector2 velj = CEUtils.CalculateSourceVel(Projectile.Center, acornPos, int.Clamp((int)((Projectile.Distance(acornPos) / 20f) / Projectile.scale), 3, 60), 1f * Projectile.scale);
                        Projectile.velocity = velj;
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.SandyBrown, "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 0.36f, 13));
                        SoundEngine.PlaySound(SoundID.Item56 with { Volume = 1f, Pitch = Main.rand.NextFloat(-0.4f, 0.4f) }, Projectile.Center);
                    }
                    if(Jump-- <= 0)
                    {
                        Projectile.velocity.Y += 1f * Projectile.scale;
                    }
                    if(Projectile.getRect().Intersects(acorn.Center.getRectCentered(72, 72)))
                    {
                        acorn.Kill();
                        GrabedArcon = true;
                        Counter2 = 0;
                        AcornProjType = acorn.type;
                    }
                    if(Projectile.velocity.Y != 0)
                    {
                        Projectile.rotation = Projectile.velocity.ToRotation();
                        if (dir < 0)
                            Projectile.rotation += MathHelper.Pi;
                    }
                    return;
                }
                else
                {
                    Projectile.velocity *= 0.986f;
                    if(GrabedArcon)
                    {
                        if (Counter2 > 0)
                        {
                            if (Math.Abs(Projectile.velocity.Y) <= 0.6f)
                            {
                                Projectile.velocity.X *= 0.92f;
                                Counter2--;
                            }
                            else
                            {
                                Projectile.velocity.X *= 0.94f;
                            }
                            Projectile.velocity.Y += 0.6f * Projectile.scale;
                            return;
                        }
                    }
                    Leaving++;
                    Projectile.Opacity -= 0.05f;
                    Projectile.tileCollide = false;
                    Projectile.velocity.Y = 8 * Projectile.scale;
                    if (Leaving > 10)
                        Projectile.Kill();
                }
            }
            if(Leaving == 0)
                if (Projectile.Opacity < 1)
                    Projectile.Opacity += 0.05f;
            if (SpawnTime > 0)
            {
                SpawnTime--;
                Projectile.velocity = new Vector2(0, -8 * Projectile.scale);
                if (SpawnTime < 20 && !CEUtils.CheckSolidTileOrPlatform(Projectile.getRect()))
                {
                    SpawnTime = 0;
                    Projectile.tileCollide = false;
                }
                if (SpawnTime <= 0)
                    Projectile.tileCollide = true;
            }
            if(ShootFrame >= 0)
            {
                Projectile.frameCounter++;
                if(Projectile.frameCounter > 1)
                {
                    Projectile.frameCounter = 0;
                    ShootFrame++;
                    if (ShootFrame > 3)
                        ShootFrame = -1;
                }
            }
            else
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 3)
                {
                    Projectile.frameCounter = 0;
                    Frame++;
                    if (Frame > 3)
                        Frame = 0;
                }
            }
            ShootDelay--;
            if (Math.Abs(Projectile.velocity.Y) <= 0.6f)
                Projectile.velocity.X *= 0.94f;
            if(Projectile.timeLeft < 60)
                Leaving = 1;
            if (SpawnTime <= 0)
            {
                Projectile.pushByOther(0.8f);
                if (((target == null && Counter > 60) || ShootCount <= 0) && ShootFrame == -1)
                {
                    if (Math.Abs(Projectile.velocity.Y) <= 0.02f)
                    {
                        Leaving = 1;
                        return;
                    }
                }
                if (target != null)
                {
                    if(ShootCount <= 0)
                    {
                        ShootDelay = 10;
                    }
                    if (ShootDelay > 0 || CEUtils.getDistance(Projectile.Center, target.Center) > 600 * Projectile.scale)
                    {
                        if(target.Center.Y < Projectile.Center.Y - 460 * Projectile.scale)
                        {
                            if (Math.Abs(Projectile.velocity.Y) <= 0.01f)
                            {
                                Projectile.velocity = (target.Center - Projectile.Center).normalize() * 30 * Projectile.scale;
                                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.SandyBrown, "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 0.36f, 13));
                                SoundEngine.PlaySound(SoundID.Item56 with { Volume = 1f, Pitch = Main.rand.NextFloat(-0.4f, 0.4f) }, Projectile.Center);
                            }
                        }
                        if (ShootFrame == -1 && Math.Abs(Projectile.Center.X - target.Center.X) > 400)
                        {
                            Projectile.velocity.X += Math.Sign(target.Center.X - Projectile.Center.X) * 0.75f;
                            if (CEUtils.CheckSolidTile((Projectile.Center + Projectile.velocity * 2).getRectCentered(Projectile.width, Projectile.height * 0.75f)))
                            {
                                if (Math.Abs(Projectile.velocity.Y) <= 0.6f)
                                {
                                    Projectile.velocity.Y = -18 * Projectile.scale;

                                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.SandyBrown, "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 0.36f, 13));
                                    SoundEngine.PlaySound(SoundID.Item56 with { Volume = 1f, Pitch = Main.rand.NextFloat(-0.4f, 0.4f) }, Projectile.Center);
                                }
                            }
                        }
                        else
                        {
                            if(Math.Abs(Projectile.velocity.Y) <= 0.6f)
                                Projectile.velocity.X *= 0.99f;
                        }
                    }
                    else
                    {
                        ShootCount--;
                        ShootFrame = 0;
                        Projectile.frameCounter = 0;
                        ShootDelay = 20;
                        if(Main.myPlayer == Projectile.owner)
                        {
                            Vector2 targetPos = target.Center;
                            Vector2 myPos = Projectile.Center;
                            Vector2 vel = CEUtils.CalculateSourceVel(myPos, targetPos, 100, SquirrelStone.Gravity);
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(20 * dir, 0) * Projectile.scale, vel, ModContent.ProjectileType<SquirrelStone>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                        dir = (target.Center.X > Projectile.Center.X) ? 1 : -1;
                    }
                }
            }
            if (SpawnTime <= 0)
            {
                if (Math.Abs(Projectile.velocity.Y) > 1f && Counter > 40)
                {
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (dir < 0)
                        Projectile.rotation += MathHelper.Pi;
                }
                Projectile.velocity.Y += 0.9f * Projectile.scale;
                Projectile.velocity *= 0.98f;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
        
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            NPC target = Projectile.FindMinionTarget();
            if(target != null)
                fallThrough = target.Center.Y > Projectile.Center.Y + 100;
            return true;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects se = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Texture2D tex = ShootFrame >= 0 ? this.getTextureAlt("Throw") : Projectile.GetTexture();
            Rectangle frame = CEUtils.GetCutTexRect(tex, 4, ShootFrame >= 0 ? ShootFrame : Frame, false);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor * Projectile.Opacity, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, se, 0);
            if(GrabedArcon)
            {
                Texture2D arcon = TextureAssets.Projectile[AcornProjType].Value;
                Main.EntitySpriteDraw(arcon, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, 0, arcon.Size() * 0.5f, 1, SpriteEffects.None);
            }
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
    public class SquirrelStone : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = true;
        }
        public static float Gravity = 0.2f;
        public List<Vector2> oldPos = new List<Vector2>();
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.Entropy().FirstFrames)
            {
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
            }
            Projectile.velocity.Y += Gravity;
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 22)
                oldPos.RemoveAt(0);
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item50, Projectile.position);
            SoundEngine.PlaySound(SoundID.Dig.WithPitchOffset(Main.rand.NextFloat(0.5f, 1f)), Projectile.position);
            SoundEngine.PlaySound(SoundID.Dig.WithPitchOffset(Main.rand.NextFloat(-1f, -0.5f)), Projectile.position);
            int dust_splash = 0;
            while (dust_splash < 6)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(Projectile.Center, new Vector2(Main.rand.NextFloat(15), 0).RotatedByRandom(MathHelper.TwoPi), false, 10, Projectile.ai[0] == 1 ? 1.2f : 0.6f, Projectile.ai[0] == 1 ? Color.Gray : Color.DarkGray, false, true));
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Copper, 0f, 0f, 0, default, 0.5f);
                dust_splash += 1;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.UseAdditiveClamp();
            for(int i = 0; i < oldPos.Count; i++)
            {
                float p = (i + 1f) / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, Color.White * p, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
