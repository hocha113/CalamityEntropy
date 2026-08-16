using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Books;
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
    public class BlueFlatTopMushroom : ModItem, IBaitItem
    {
        public static int TagDamage = 3;
        public static float DamageMult = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.knockBack = 0;
            Item.shootSpeed = 25;
            Item.useAnimation = Item.useTime = 18;
            Item.value = CalamityGlobalItem.RarityGreenBuyPrice;
            Item.rare = ItemRarityID.Green;
            Item.width = 46;
            Item.height = 46; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<BlueFlatTopMushroomProjectile>();
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

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class BlueFlatTopMushroomProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<BlueFlatTopMushroom>();
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
                for (int i = 0; i < 1; i++)
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
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), randomPos, Vector2.Zero, ModContent.ProjectileType<BlueFox>(), (int)(Projectile.damage * damageMul), Projectile.knockBack, Projectile.owner);
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
                GeneralParticleHandler.SpawnParticle(new PointParticle(Projectile.Center, new Vector2(Main.rand.NextFloat(15), 0).RotatedByRandom(MathHelper.TwoPi), false, 10, Projectile.ai[0] == 1 ? 1.2f : 0.6f, Projectile.ai[0] == 1 ? Color.Blue : new Color(100, 110, 255), false, true));
                dust_splash += 1;
            }
        }
        public override void OnKill(int timeLeft)
        {
            if(timeLeft > 0 && !Main.dedServ)
            {
                OnHitEffect(Projectile.Center);
            }
            if(Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BlueSporeCloud>(), Projectile.damage, 0, Projectile.owner);
            }
        }
    }
    public class BlueFox : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.timeLeft = 2400;
            Projectile.width = Projectile.height = 27;
            Projectile.localNPCHitCooldown = -1;
        }
        public int SpawnTime = 30;
        public int dir = 1;
        public int Frame = 0;
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
            }
            Counter++;

            if (Projectile.velocity.X > 0.1f)
                dir = 1;
            if (Projectile.velocity.X < -0.1f)
                dir = -1;
            Projectile.rotation = 0;
            if (Leaving > 0)
            {
                if (Projectile.ai[2] == 0)
                {
                    Vector2 acornPos = Vector2.Zero;
                    Projectile acorn = null;
                    NPC target = null;
                    foreach(Projectile p in Main.ActiveProjectiles)
                    {
                        if(p.ModProjectile != null && p.ModProjectile is BaitProj bp && !bp.IsActive)
                        {
                            target = bp.StickNPC.ToNPC();
                            acorn = p;
                            acornPos = p.Center;
                            break;
                        }
                    }
                    if(Counter2++ > 1800 || acornPos.Distance(Projectile.Center) > 3000 * Projectile.scale || acorn == null || GrabedArcon || Projectile.timeLeft < 60)
                    {
                        Projectile.ai[2] = 1;
                        return;
                    }
                    if(Math.Abs(Projectile.velocity.Y) <= 0.01f)
                    {
                        Vector2 velj = CEUtils.CalculateSourceVel(Projectile.Center, acornPos + new Vector2(0, -16), int.Clamp((int)((Projectile.Distance(acornPos) / 38f) / Projectile.scale), 3, 30), 2f * Projectile.scale);
                        Projectile.velocity = velj;
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, new Color(60, 60, 255), "CalamityMod/Particles/BloomRing", Vector2.One, CEUtils.randomRot(), 0.01f, Projectile.scale * 0.36f, 13));
                        SoundEngine.PlaySound(SoundID.Item56 with { Volume = 1f, Pitch = Main.rand.NextFloat(-0.4f, 0.4f) }, Projectile.Center);
                    }
                    if(Jump-- <= 0)
                    {
                        Projectile.velocity.Y += 2f * Projectile.scale;
                    }
                    if(Projectile.getRect().Intersects(acorn.Center.getRectCentered(72, 72)) || (target != null && Projectile.Colliding(Projectile.getRect(), target.getRect())))
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
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
                Frame++;
                if (Frame > 3)
                    Frame = 0;
            }
            
            if(SpawnTime <= 0)
                Leaving = 1;
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
            SpriteEffects se = dir < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Texture2D tex = Projectile.GetTexture();
            Rectangle frame = CEUtils.GetCutTexRect(tex, 4, Frame, false);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor * Projectile.Opacity, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, se, 0);
            if(GrabedArcon)
            {
                Texture2D arcon = TextureAssets.Projectile[AcornProjType].Value;
                Main.EntitySpriteDraw(arcon, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, arcon.Size() * 0.5f, 1, SpriteEffects.None);
            }
            return false;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
    public class BlueSporeCloud : ModProjectile
    {
        public Color baseColor => Color.Blue;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.localNPCHitCooldown = 16;
            Projectile.width = Projectile.height = 172;
            Projectile.timeLeft = 120;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
        public override void AI()
        {
            if (Projectile.localAI[2]++ == 0)
            {
                CEUtils.PlaySound("SporeGas", 1, Projectile.Center);
                for (int i = 0; i < 36; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(Projectile.Center, CEUtils.randomPointInCircle(14), Color.Lerp(baseColor, Color.White, 0.5f), baseColor, Projectile.scale, 230, 0.005f));
                }
            }
            if (Main.GameUpdateCount % 2 == 0)
                for (int i = 0; i < 4; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(Projectile.Center + CEUtils.randomPointInCircle(100), CEUtils.randomPointInCircle(2), Color.Lerp(baseColor, Color.White, 0.5f), baseColor, Projectile.scale, 230, 0.005f));
                }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.velocity.Length() > 12)
                target.velocity *= 0.7f;
        }
    }
}
