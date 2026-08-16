using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Books;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework.Graphics;
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
    public class MagicHat : ModItem, IBaitItem
    {
        public static int TagDamage = 3;
        public static float DamageMult = 0.6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(TagDamage);

        public override void SetDefaults()
        {
            Item.damage = 22;
            Item.knockBack = 0;
            Item.shootSpeed = 32;
            Item.useAnimation = Item.useTime = 27;
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
            Item.shoot = ModContent.ProjectileType<MagicHatProjectile>();
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
                .AddIngredient(ItemID.ShadowScale, 8)
                .AddIngredient(ItemID.Silk, 20)
                .AddTile(TileID.WorkBenches)
                .Register();
            CreateRecipe()
                .AddIngredient(ItemID.TissueSample, 8)
                .AddIngredient(ItemID.Silk, 20)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class MagicHatProjectile : BaitProj
    {
        public override string Texture => CEUtils.ItemTexPath<MagicHat>();
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
                Projectile.rotation = 0;
                Projectile.Center = npc.Center + StickOffset;
                ActiveCounter++;
                if (ActiveCounter == 60 || ActiveCounter == 74 || ActiveCounter == 88)
                {
                    SetActive();
                    if(ActiveCounter != 88)
                        IsActive = true;
                    CEUtils.SyncProj(Projectile.whoAmI);
                }
            }
            activeEffectAlpha = float.Lerp(activeEffectAlpha, (StickNPC >= 0 && IsActive) ? 1 : 0, 0.04f);
            Counter++;
        }
        public override void ActiveEffect(float damageMul)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (new Vector2(0, -16)).RotateRandom(0.6f), ModContent.ProjectileType<PigeonMinion>(), (int)(Projectile.damage * damageMul), 6, Projectile.owner, 0, Main.rand.Next(0, 30));
            }
        }
        public float activeEffectAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.UseAdditiveClamp();
            for(float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
            {
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, null, Projectile.Center + i.ToRotationVector2() * 2));
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White, null, Projectile.Center + i.ToRotationVector2() * 4));
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
            CEUtils.PlaySound("portal_emerge", Main.rand.NextFloat(1.7f, 1.9f), pos, 8, 0.65f);
            for(int i = 0; i < 32; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(-8, 8), 0, new Color(200, 160, 255), Main.rand.NextFloat(1.4f, 1.6f));
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 32; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6), 0, new Color(200, 160, 255), Main.rand.NextFloat(1.4f, 1.6f));
                }
                float r = CEUtils.randomRot();
                EParticle.NewParticle(new AbyssalLine() { lx = 3f, xadd = 0.9f, spawnColor = Color.LightBlue, endColor = Color.Purple }, Projectile.Center, Vector2.Zero, Color.White, 1, 1, true, BlendState.Additive, r);
                EParticle.NewParticle(new AbyssalLine() { lx = 3f, xadd = 0.9f, spawnColor = Color.LightBlue, endColor = Color.Purple }, Projectile.Center, Vector2.Zero, Color.White, 1, 1, true, BlendState.Additive, r + MathHelper.PiOver2);
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
    }
    public class PigeonMinion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Summon, false, -1);
            Projectile.width = Projectile.height = 36;
            Projectile.timeLeft = 340;
            Projectile.localNPCHitCooldown = 12;
        }
        public float Counter
        {
            get { return Projectile.ai[0]; }
            set { Projectile.ai[0] = value; }
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if(Projectile.frameCounter % 4 == 0)
            {
                Projectile.frame++;
                if (Projectile.frame > 3)
                    Projectile.frame = 0;
            }
            Player player = Projectile.GetOwner();
            NPC target = Projectile.FindMinionTarget(2000);
            if (Projectile.timeLeft <= 180 || target == null)
            {
                Projectile.velocity *= 0.94f;
                Projectile.velocity.X += ((Projectile.Center.X > player.Center.X) ? 1 : -1) * 1.6f;
                Projectile.velocity.Y += -1.2f;
            }
            else
            {
                Projectile.pushByOther(1);
                if (Counter > 24)
                {
                    if(CEUtils.getDistance(Projectile.Center, target.Center) > 200 && !Projectile.Colliding(Projectile.Hitbox, target.Hitbox))
                    {
                        Projectile.velocity *= 0.86f;
                        Projectile.velocity += (target.Center - Projectile.Center).normalize() * 6f;
                    }
                    else
                    {
                        if (Projectile.velocity.Length() < 28)
                            Projectile.velocity *= 1.1f;
                    }
                }
                else
                {
                    if(Counter > 6)
                        if(Math.Abs(Projectile.velocity.X) < 16)
                            Projectile.velocity.X *= 1.08f;
                    Projectile.velocity.Y *= 0.98f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.X < 0)
                Projectile.rotation += MathHelper.Pi;
            Counter++;
        }
        public override bool? CanDamage()
        {
            return Counter > 24;
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
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;
            Main.spriteBatch.UseAdditiveClamp();
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4)
            {
                Main.EntitySpriteDraw(Projectile.getDrawData(Color.White * 0.6f, null, Projectile.Center + i.ToRotationVector2() * 2));
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            return false;
        }
    }
}
