using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Armor.Azafure;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Terraria.GameContent.Animations.IL_Actions.Sprites;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons
{
    public class AzafureExhaustGasInjector : ModItem, IAzafureEnhancable
    {
        public override void SetDefaults()
        {
            Item.damage = 33;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 76;
            Item.height = 46;
            Item.useTime = 7;
            Item.useAnimation = 7;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 0;
            Item.value = Item.buyPrice(0, 20);
            Item.rare = ModContent.RarityType<AzafureOrange>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AzafureExhaustGasInjectorHeld>();
            Item.shootSpeed = 26;
            Item.channel = true;
            Item.noUseGraphic = true;
        }
        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_SparkSpreader, CEID.Item_AerialiteBar))
            {
                CreateRecipe()
                .AddIngredient(CEID.Item_SparkSpreader)
                .AddIngredient<HellIndustrialComponents>(6)
                .AddIngredient(CEID.Item_AerialiteBar, 8)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient<HellIndustrialComponents>(5)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public class AzafureExhaustGasInjectorHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/AzafureExhaustGasInjector";
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void SetDefaults()
        {
            Projectile.HeldProjSetDefaults(DamageClass.Ranged);
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void AI()
        {
            Projectile.GetOwner().Entropy().MouseWorldListener = true;
            Player player = Projectile.GetOwner();
            if (player.dead)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.Entropy().FirstFrames)
                Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.StickToPlayer(0.3f);
            if (player.channel)
            {
                Projectile.timeLeft = 3;
                player.itemTime = player.itemAnimation = 2;
            }
            player.SetHandRot(Projectile.rotation);
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(80, -10 * (Projectile.velocity.X > 0 ? 1 : -1)).RotatedBy(Projectile.rotation), Projectile.velocity.RotatedBy((float)(Math.Sin(Main.GameUpdateCount * 0.32f) * 0.04f)).RotatedByRandom(0.05f) * 0.6f, ModContent.ProjectileType<ExhaustGas>(), Projectile.damage / 4, Projectile.knockBack, Projectile.owner);
            }
            if (!Main.dedServ)
            {
                if (snd == null)
                {
                    snd = new LoopSound(UrnOfSoulsHoldout.loopSnd);
                    snd.play();
                }
                snd.setVolume_Dist(Projectile.Center, 80, 800, 0.4f);
                snd.timeleft = 3;

            }

        }
        public LoopSound snd;
        public override void OnKill(int timeLeft)
        {
            Projectile.GetOwner().itemTime = Projectile.GetOwner().itemAnimation = 0;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D t = Projectile.GetTexture();
            Main.EntitySpriteDraw(t, Projectile.Center - Main.screenPosition + new Vector2(18, -2 * (Projectile.velocity.X > 0 ? 1 : -1)).RotatedBy(Projectile.rotation), null, lightColor, Projectile.rotation, t.Size() * new Vector2(0.5f, 0.5f), Projectile.scale, (Projectile.velocity.X > 0) ? SpriteEffects.None : SpriteEffects.FlipVertically);

            return false;
        }
    }
    public class ExhaustGas : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000000;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += target.defense;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 140;
            Projectile.MaxUpdates = 3;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
        public float Size { get { return Projectile.ai[0]; } set { Projectile.ai[0] = value; } }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.Center.getRectCentered(50 * Size).Intersects(targetHitbox);
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
                Projectile.idStaticNPCHitCooldown = Projectile.GetOwner().AzafureEnhance() ? 5 : 6;
            foreach (NPC n in Main.ActiveNPCs)
            {
                var b = ProjectileLoader.CanHitNPC(Projectile, n);
                if (!n.friendly && (!b.HasValue || (b.HasValue && b.Value)) && Projectile.Colliding(Projectile.Hitbox, n.Hitbox))
                {
                    Projectile.ai[1] = float.Lerp(Projectile.ai[1], 1, 0.02f);
                    Projectile.velocity *= 0.94f;
                }
            }
            if (Size < 0.08f)
            {
                Projectile.rotation = CEUtils.randomRot();
                Size = 0.08f;
            }
            Size += 0.024f;
            Projectile.velocity.Y -= 0.02f;
            Projectile.velocity *= 0.982f;
            Projectile.rotation += Projectile.velocity.X * 0.01f;
            if (Projectile.timeLeft < 100)
                Projectile.ai[1] = float.Lerp(Projectile.ai[1], 1, 0.014f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if(p.type == Projectile.type)
                {
                    if (p.whoAmI != Projectile.whoAmI)
                        return false;
                    else
                        break;
                }
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            foreach(Projectile p in Main.ActiveProjectiles)
            {
                if(p.type == Projectile.type)
                {
                    for (float i = 0; i < 1; i+=0.05f)
                    {
                        CEUtils.DrawRotatedGlow(p.Center - p.velocity * i * 8, new Color(255, 160, 40) * p.Opacity * 0.4f * (1 - p.ai[1]) * (1 - p.ai[1]), p.ai[0] * (1 - i), Projectile.rotation, true, CEExtraAssets.Smoke, false);
                    }
                }
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool? CanDamage()
        {
            return Projectile.ai[1] > 0.3f ? false : null;
        }
    }
}
