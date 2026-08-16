using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Projectiles;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public class AntlionShell : ModItem, IBaitItem
    {
        public static int TagDamage = 2;
        public static float DamageMult = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.knockBack = 0;
            Item.shootSpeed = 26;
            Item.useAnimation = Item.useTime = 20;
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
            Item.shoot = ModContent.ProjectileType<AntlionShellProjectile>();
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
    public class AntlionShellProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<AntlionShell>();
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
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(Main.rand.NextFloat(-400, 400), 700), Vector2.Zero, ModContent.ProjectileType<DesertNuisanceFriendly>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner);
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
                    Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.SandyBrown * Projectile.Opacity * (1 - scale) * activeEffectAlpha, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.12f, SpriteEffects.None, 0);
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
            Projectile.velocity *= 0;
            StickNPC = target.whoAmI;
            StickOffset = Projectile.Center - target.Center;
            Projectile.timeLeft = 480;
            CEUtils.SyncProj(Projectile.whoAmI);
        }
        public void OnHitEffect(Vector2 pos)
        {
            CEUtils.PlaySound("corruptwhip_hit2", Main.rand.NextFloat(0.8f, 1.2f), pos);
            for(int i = 0; i < 20; i++)
            {
                var d = Dust.NewDustDirect(pos - Projectile.Size * 0.5f, Projectile.width, Projectile.height, DustID.Sand);
                d.velocity = CEUtils.randomPointInCircle(18);
                d.scale = Main.rand.NextFloat(1.2f, 1.7f);
                d.noGravity = true;
            }
            for(int i = 0; i < 2; i++)
            {
                float scale = 0.05f + 0.02f * i;
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, Color.Lerp(Color.SandyBrown, new Color(230, 198, 104), (i / 2f)), "CalamityMod/Particles/FlameExplosion2", Vector2.One, CEUtils.randomRot(), scale * 0.2f, scale, 12 + i * 4));
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
    public class DesertNuisanceFriendly : ModProjectile, iWyrmSeg
    {
        public float rot { get { return Projectile.rotation; }
            set { Projectile.rotation = value; } }
        public Vector2 Center { get { return Projectile.Center; } 
            set { Projectile.Center = value; } }

        public bool spawnSeg = true;
        public List<WyrmSeg> segs;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 64;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 260;
        }
        public int headFrame = 0;
        public bool Bite = true;
        public bool InGround = true;
        public override void AI()
        {
            if (Projectile.timeLeft < 18)
                Projectile.Opacity -= 1 / 18f;
            if (spawnSeg)
            {
                spawnSeg = false;
                segs = new List<WyrmSeg>();
                iWyrmSeg seg = this;
                List<int> spacings = new List<int>() { 24, 60, 60, 60, 60, 60, 60, 48 };
                for (int i = 0; i < 8; i++)
                {
                    WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center + Vector2.UnitY * i * 60, follow = seg, rotC = 0.04f, spacing = spacings[i] };
                    segs.Add(spawn);
                    seg = spawn;
                }
            }
            Player player = Projectile.GetOwner();
            Vector2 targetPos = Vector2.Zero;
            if (Projectile.Entropy().FirstFrames)
            {
                if(player.MinionAttackTargetNPC >= 0)
                {
                    targetPos = player.MinionAttackTargetNPC.ToNPC().Center;
                }
                else
                {
                    targetPos = player.Calamity().mouseWorld;
                }
                Projectile.velocity = (targetPos - Projectile.Center).normalize() * 36;
            }

            if (player.MinionAttackTargetNPC >= 0 && Bite)
            {
                Projectile.frameCounter++;
                if(Projectile.frameCounter >= 4)
                {
                    Projectile.frameCounter = 0;
                    headFrame++;
                    if(headFrame > 5)
                        headFrame = 5;
                }
                targetPos = player.MinionAttackTargetNPC.ToNPC().Center;
                Projectile.velocity = CEUtils.RotateTowardsAngle(Projectile.velocity.ToRotation(), (targetPos - Projectile.Center).ToRotation(), 0.12f, true).ToRotationVector2() * Projectile.velocity.Length();
                if (Projectile.Colliding(Projectile.Hitbox, player.MinionAttackTargetNPC.ToNPC().Hitbox))
                {
                    Bite = false;
                    headFrame = 6;
                    Projectile.frameCounter = 0;
                    CEUtils.PlaySound("DnBite", 1, Projectile.Center);
                    for(int i = 0; i < 14; i++)
                    {
                        GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(CEUtils.randomPoint(player.MinionAttackTargetNPC.ToNPC().getRect()), Projectile.velocity.normalize().RotatedByRandom(0.4f) * Main.rand.NextFloat(9, 18), true, 24, Main.rand.NextFloat(1.2f, 1.6f) * 0.04f, Color.SandyBrown * 1.2f, new Vector2(0.2f, 1f)));
                    }
                    foreach(Projectile p in Main.ActiveProjectiles)
                    {
                        if(p.ModProjectile != null && p.ModProjectile is BaitProj ibp)
                        {
                            if (!ibp.IsActive && ibp.StickNPC == player.MinionAttackTargetNPC)
                                p.Kill();
                        }
                    }
                }
            }
            if(!Bite)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= 4)
                {
                    Projectile.frameCounter = 0;
                    headFrame = 0;
                }
                Projectile.velocity *= 0.99f;
                Projectile.velocity.Y += 1f;
                if(Math.Abs(Projectile.velocity.X) < 16)
                {
                    Projectile.velocity.X *= 1.1f;
                }
            }
            if (Bite)
            {
                if (InGround)
                {
                    if (!CEUtils.CheckSolidTile(Projectile.getRect()))
                    {
                        InGround = false;
                        for (int i = 0; i < 100; i++)
                        {
                            var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Sand);
                            d.scale = Main.rand.NextFloat(2.4f, 3.2f);
                            d.velocity = Projectile.velocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.2f, 1);
                            d.noGravity = true;
                        }
                        CEUtils.PlaySound("ksLand", 0.9f, Projectile.Center, 8, 0.5f);
                    }
                }
            }
            else
            {
                if (!InGround)
                {
                    if (CEUtils.CheckSolidTile(Projectile.getRect()))
                    {
                        InGround = true;
                        for (int i = 0; i < 100; i++)
                        {
                            var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Sand);
                            d.scale = Main.rand.NextFloat(2.4f, 3.2f);
                            d.velocity = -Projectile.velocity.RotatedByRandom(0.24f) * Main.rand.NextFloat(0.04f, 0.6f);
                            d.noGravity = true;
                        }
                        CEUtils.PlaySound("ksLand", 0.56f, Projectile.Center, 8, 0.5f);
                    }
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
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D s1 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/Bait/DN/DesertNuisanceBodyYoung2");
            Texture2D s2 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/Bait/DN/DesertNuisanceBodyYoung3");
            Texture2D s3 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/Bait/DN/DesertNuisanceBodyYoung4");
            Texture2D s4 = CEUtils.RequestTex("CalamityEntropy/Content/Items/Weapons/Bait/DN/DesertNuisanceTailYoung");
            if (segs != null)
            {
                for(int i = segs.Count - 1; i >= 0; i--)
                {
                    Texture2D tex = s1;
                    if (i > 2)
                        tex = s2;
                    if (i > 5)
                        tex = s3;
                    if(i > 6)
                        tex = s4;
                    DrawSeg(tex, segs[i].Center, null, segs[i].rot, new Vector2(tex.Width / 2, 6), CEUtils.GetLight(segs[i].Center));
                }
            }
            DrawSeg(Projectile.GetTexture(), Projectile.Center, CEUtils.GetCutTexRect(Projectile.GetTexture(), 7, headFrame, false), Projectile.rotation, new Vector2(Projectile.GetTexture().Width / 2, 40), lightColor);

            return false;
        }
        public void DrawSeg(Texture2D tex, Vector2 pos, Rectangle? frame, float rot, Vector2 origin, Color color)
        {
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color * Projectile.Opacity, rot + MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None);
        }
    }
}
