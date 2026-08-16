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
    public class MechanicalTransmitter : ModItem, IBaitItem
    {
        public static int TagDamage = 6;
        public static float DamageMult = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 44;
            Item.knockBack = 0;
            Item.shootSpeed = 32;
            Item.useAnimation = Item.useTime = 24;
            Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
            Item.rare = ItemRarityID.Yellow;
            Item.width = 42;
            Item.height = 42; 
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<MechanicalTransmitterProjectile>();
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
                .AddIngredient(ItemID.HallowedBar, 8)
                .AddIngredient(ItemID.Wire, 20)
                .AddIngredient<MysteriousCircuitry>(2)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class MechanicalTransmitterProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<MechanicalTransmitter>();
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
                    if (ActiveCounter % 16 == 0)
                        CEUtils.PlaySound("WulfrumPingReady",  1.2f, Projectile.Center);
                }
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if(ActiveCounter > 100)
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
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.GetOwner().Center + new Vector2(Main.rand.NextFloat(-100, 100), -800), Vector2.Zero, ModContent.ProjectileType<DetectorMinion>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner, 0, Main.rand.Next(0, 30));
                }
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
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.Silver * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.2f, SpriteEffects.None, 0);
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
            Projectile.tileCollide = false;
            OnHitEffect(Projectile.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/WulfrumProsthesisSucc") with { Volume = 0.34f}, target.Center);
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("ExoHit1", Main.rand.NextFloat(1.7f, 1.9f), pos, 8, 0.65f);
            for(int i = 0; i < 2; i++)
            {
                float scale = 0.05f + 0.02f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.Silver, new Color(225, 225, 225), (i / 2f)), "CalamityEntropy/Assets/Extra/HollowCircleSoftEdge", Vector2.One, CEUtils.randomRot(), scale * 0.2f, scale, 12 + i * 4));
            }

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 1, false, 12, 0.04f * Main.rand.NextFloat(0.65f, 1f), new Color(200, 200, 200), new Vector2(2f, 0.6f), true));
        }
        public override void OnKill(int timeLeft)
        {
            if(timeLeft > 0 && !Main.dedServ)
            {
                OnHitEffect(Projectile.Center);
            }
        }
    }
    public class DetectorMinion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.timeLeft = 600;
            Projectile.light = 1;
        }
        public float Alpha = 0;
        public override bool? CanDamage()
        {
            return false;
        }
        public float Counter
        {
            get { return Projectile.ai[0]; }
            set { Projectile.ai[0] = value; }
        }
        public int NoTargetCounter = 0;
        public int ShootDelay = 80;
        public int ShootCount = 0;
        public int NoUpdate = Main.rand.Next(0, 16);
        public override void AI()
        {
            if (NoUpdate-- > 0)
                return;
            Player player = Projectile.GetOwner();
            if (Projectile.timeLeft == 20)
            {
                Alpha = 1;
            }
            if (Projectile.timeLeft <= 160)
            {
                Projectile.velocity *= 0.98f;
                Projectile.velocity.Y += -2.2f;
                if (Projectile.timeLeft < 20)
                {
                    Alpha -= 0.05f;
                }
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
                        Projectile.velocity *= 0.9f;
                        if(CEUtils.getDistance(Projectile.Center, targetPos) > 120)
                            Projectile.velocity += (targetPos - Projectile.Center).normalize() * 1.8f;
                        Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
                        Projectile.pushByOther(2);
                        if (ShootDelay <= 0)
                        {
                            ShootDelay = 40;
                            ShootCount++;
                            if (ShootCount > 4)
                            {
                                Projectile.timeLeft = 66;
                                Projectile.velocity -= (target.Center - Projectile.Center).normalize() * 14f;
                            }
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Vector2 vel = (target.Center - Projectile.Center).normalize() * 36;
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + vel * 0.2f, vel, ModContent.ProjectileType<DetectorMinionLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                            }
                            if (ShootCount > 4)
                                Projectile.timeLeft = 160;
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
            void Draw(Vector2 pos, Color color)
            {
                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, color, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * 1.5f, SpriteEffects.None, 0);
            }
            Draw(Projectile.Center, Color.White * Alpha);
            return false;
        }
    }
    public class DetectorMinionLaser : EBookBaseProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public int ColorType = Main.rand.Next(0, 4);
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
            }
            for (float i = 0.05f; i <= 1f; i += 0.05f)
            {
                odp.Add(Projectile.Center + Projectile.velocity * i);
                if (odp.Count > 280)
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
        public Color EffectColor()
        {
            if (ColorType == 1)
                return new Color(255, 20, 20);
            if (ColorType == 2)
                return new Color(20, 20, 255);
            if (ColorType == 3)
                return new Color(20, 255, 20);
            return Color.Silver;
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
                    dust.color = EffectColor();
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
                vp.Add(new CEUtils.VertexPointSets(odp[i], Color.White * alpha, 18 * Projectile.scale * width, 0));
            }
            ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), EffectColor());
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
