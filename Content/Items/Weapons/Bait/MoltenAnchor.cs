using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.NPCs.AquaticScourge;
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
    public class MoltenAnchor : ModItem, IBaitItem
    {
        public static int TagDamage = 4;
        public static float DamageMult = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.knockBack = 0;
            Item.shootSpeed = 26;
            Item.useAnimation = Item.useTime = 26;
            Item.value = CalamityGlobalItem.RarityGreenBuyPrice;
            Item.rare = ItemRarityID.Green;
            Item.width = 38;
            Item.height = 38; 
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.Swing;
            var snd = CEUtils.GetSound("BaitThrow", 1, 6);
            snd.PitchRange = (0f, 0.4f);
            Item.UseSound = snd;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<MoltenAnchorProjectile>();
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
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddIngredient(ItemID.Bone, 15)
                .AddTile(TileID.Hellforge)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class MoltenAnchorProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<MoltenAnchor>();
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, true, -1);
            Projectile.width = Projectile.height = 30;
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
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
        }
        public override void ActiveEffect(float damageMul)
        {
            if(Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(Main.rand.NextFloat(800, 900) * (Main.rand.NextBool() ? 1 : -1), 640), Vector2.Zero, ModContent.ProjectileType<BoneSerpentFriendly>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner);
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
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.OrangeRed * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.12f, SpriteEffects.None, 0);
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
            target.AddBuff(BuffID.OnFire3, 180);
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
            CEUtils.PlaySound("Smash" + Main.rand.Next(1, 3), 1, pos);
            for(int i = 0; i < 20; i++)
            {
                var d = Dust.NewDustDirect(pos - Projectile.Size * 0.5f, Projectile.width, Projectile.height, DustID.Lava);
                d.velocity = CEUtils.randomPointInCircle(6);
                d.scale = Main.rand.NextFloat(1.2f, 1.7f);
                d.noGravity = true;
            }
            float r = CEUtils.randomRot();
            for (int i = 0; i < 3; i++)
            {
                float scale = 0.6f + 0.4f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.Orange, new Color(230, 198, 104), (i / 2f)), "CalamityEntropy/Assets/Extra/StarTexture", Vector2.One, r, scale, 0, 12 + i * 2, true, 1, false));
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
    public class BoneSerpentFriendly : ModProjectile, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; }
            set { Projectile.rotation = value; } }
        public Vector2 Center { get { return Projectile.Center; } 
            set { Projectile.Center = value; } }
        public override string Texture => CEUtils.WhiteTexPath;

        public bool spawnSeg = true;
        public List<WyrmSeg> segs;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4500;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 220;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 30;
            if(target.type == NPCID.TheDestroyerBody || target.type == ModContent.NPCType<AquaticScourgeBody>() || target.type == ModContent.NPCType<AquaticScourgeBodyAlt>())
            {
                modifiers.SourceDamage *= 0.2f;
            }
        }
        public bool InGround = true;
        public static int Segments = 240;

        public override void AI()
        {
            if (Projectile.timeLeft < 18)
                Projectile.Opacity -= 1 / 18f;
            if (spawnSeg)
            {
                spawnSeg = false;
                segs = new List<WyrmSeg>();
                iWyrmSeg seg = this;
                List<int> spacings = new List<int>();
                for (int i = 0; i < Segments; i++)
                {
                    spacings.Add(16);
                    WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center + Vector2.UnitY * i * 0, follow = seg, rotC = 0.2f, spacing = spacings[i], AlwaysFollow = false };
                    segs.Add(spawn);
                    seg = spawn;
                }
            }
            Player player = Projectile.GetOwner();
            Vector2 targetPos = Vector2.Zero;
            Projectile.velocity.Y += 0.7f;
            if (Projectile.Entropy().FirstFrames)
            {
                if (player.MinionAttackTargetNPC >= 0)
                {
                    targetPos = player.MinionAttackTargetNPC.ToNPC().Center;
                }
                else
                {
                    targetPos = player.Calamity().mouseWorld;
                }
                Projectile.velocity = CEUtils.CalculateSourceVel(Projectile.Center, targetPos, int.Clamp((int)(Projectile.Distance(targetPos) / 30f), 0, 50), 0.7f);
            }

            for (int i = 0; i < 3; i++)
            {
                var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Lava);
                d.scale = Main.rand.NextFloat(0.6f, 1f);
                d.velocity = Projectile.velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(0.6f, 1);
                d.noGravity = true;
            }

            if (InGround)
            {
                if (!CEUtils.CheckSolidTile(Projectile.getRect()))
                {
                    InGround = false;
                    for (int i = 0; i < 60; i++)
                    {
                        var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Lava);
                        d.scale = Main.rand.NextFloat(1.6f, 2f);
                        d.velocity = Projectile.velocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.2f, 1);
                        d.noGravity = true;
                    }
                    SoundEngine.PlaySound(SoundID.NPCDeath25, Projectile.Center);
                }
            }

            if (!InGround)
            {
                if (CEUtils.CheckSolidTile(Projectile.getRect()))
                {
                    InGround = true;
                    for (int i = 0; i < 60; i++)
                    {
                        var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Lava);
                        d.scale = Main.rand.NextFloat(1.6f, 2f);
                        d.velocity = -Projectile.velocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.04f, 0.6f);
                        d.noGravity = true;
                    }
                    SoundEngine.PlaySound(SoundID.NPCDeath25 with { Pitch = 0.6f, Volume = 0.8f}, Projectile.Center);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.position += Projectile.velocity;
            foreach (WyrmSeg seg in segs)
            {
                seg.update();
            }
            Projectile.position -= Projectile.velocity;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            SoundStyle burn = new("CalamityMod/Sounds/Item/WeldingBurn");
            SoundEngine.PlaySound(burn with { Volume = 0.28f, Pitch = 0.5f }, target.Center);
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(2f, 2.6f);
                dust.velocity = (new Vector2(24, 24).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.7f)) * Main.rand.NextFloat(0.4f, 1f);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.OrangeRed : Color.Firebrick;
                dust.fadeIn = 2f;
            }
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
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            foreach (var seg in segs)
            {
                if (seg.Center.getRectCentered(54, 54).Intersects(targetHitbox))
                    return true;
            }
            return null;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.Entropy().FirstFrames)
            {
                Main.instance.LoadNPC(39);
                Main.instance.LoadNPC(40);
                Main.instance.LoadNPC(41);
            }
            Texture2D s1 = TextureAssets.Npc[39].Value;
            Texture2D s2 = TextureAssets.Npc[40].Value;
            Texture2D s3 = TextureAssets.Npc[41].Value;
            if (segs != null)
            {
                for(int i = segs.Count - 1; i >= 0; i--)
                {
                    Texture2D tex = i == segs.Count - 1 ? s3 : s2;

                    DrawSeg(tex, segs[i].Center, null, segs[i].rot, new Vector2(tex.Width / 2, tex.Height / 2), CEUtils.GetLight(segs[i].Center));
                }
            }
            DrawSeg(s1, Projectile.Center, null, Projectile.rotation, new Vector2(s1.Width / 2, s1.Height / 2), lightColor);

            return false;
        }
        public void DrawSeg(Texture2D tex, Vector2 pos, Rectangle? frame, float rot, Vector2 origin, Color color)
        {
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color * Projectile.Opacity, rot + MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None);
        }
    }
}
