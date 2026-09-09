using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Items.Donator;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Illusory
{
    public class Knightmare : ModItem, IDonatorItem
    {
        public string DonatorName => "鹰吹雪";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 2;
        }
        public override void SetDefaults()
        {
            Item.damage = 175;
            Item.DamageType = DamageClass.Summon;
            Item.width = 36;
            Item.height = 50;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.knockBack = 7;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.shoot = ModContent.ProjectileType<IllusoryBladeMinion>();
            Item.shootSpeed = 2f;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item4;
            Item.noMelee = true;
            Item.buffType = ModContent.BuffType<IllusoryBlade>();
            Item.rare = ModContent.RarityType<VoidPurple>();
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 5);
            int projectile = Projectile.NewProjectile(source, position, velocity, type, Item.damage, knockback, player.whoAmI, 0, 1, 0);
            Main.projectile[projectile].originalDamage = Item.damage;

            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
        .AddCalOrOwn(CEID.Item_DazzlingStabberStaff, ItemID.StardustDragonStaff)
                .AddIngredient(ItemID.EmpressBlade)
                .AddIngredient<VoidBar>(5)
                .AddTile<VoidWellTile>()
                .Register();
        }
        public override bool IsLoadingEnabled(Mod mod)
        {
            return true;
        }
    }
    public class IllusoryBlade : BaseMinionBuff
    {
        public override int ProjType => ModContent.ProjectileType<IllusoryBladeMinion>();
    }
    public class IllusoryBladeMinion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 2;
            Projectile.ArmorPenetration = 42;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
            Projectile.MaxUpdates = 6;
            Projectile.light = 1;
        }
        public int Counter = 0;
        public float WhiteAlpha = 0;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += 64;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1, 1.4f), target.Center);
            //GlowSparkCal单点slash,Configure里scale向量是Calamity原参
            //命中只spawn一次,PRTDrawMode走Configure
            PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, Projectile.velocity.normalize(), new Color(160, 160, 255), 5f).Configure(false, 6, new Vector2(0.016f, 0.036f), true, false);
            target.AddBuff<SoulDisorder>(180);
        }
        public NPC target;
        public override void AI()
        {
            if (Counter == 0)
            {
                Projectile.rotation = -MathHelper.PiOver2;
            }
            Counter++;
            Player player = Projectile.GetOwner();
            target = Projectile.FindMinionTarget();
            AttackAI(player);
            Projectile.MinionCheck<IllusoryBlade>();
        }


        public void AttackAI(Player player)
        {
            WhiteAlpha = 0;
            trailAlpha = 0;
            if (target == null)
            {
                int MyID = 0;
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.owner == Projectile.owner && proj.type == Projectile.type)
                    {
                        if (proj.whoAmI == Projectile.whoAmI)
                            break;
                        else
                            MyID++;
                    }
                }
                Vector2 targetPos = player.Center + player.velocity * 2 + new Vector2(-(40 + MyID * 16) * player.direction, -80 + 10 * (float)(Math.Sin(Main.GameUpdateCount / 16f - MyID * 0.3f)));
                Projectile.velocity = (targetPos - Projectile.Center) * 0.04f;
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, MathHelper.PiOver2 + MyID * player.direction * 0.1f, 0.04f, false);
                WhiteAlpha = ((float)(Math.Sin(Main.GameUpdateCount / 9f - MyID * 0.5f)) * 0.5f + 0.5f);
                trailAlpha = float.Min(1, Projectile.velocity.Length() * 0.4f);
                if (trailAlpha < 0.02f)
                {
                    odp.Clear();
                    odr.Clear();
                }
            }
            else
            {
                AttackTarget(target);
                WhiteAlpha = 0;
                trailAlpha = 1;
            }
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            if (odp.Count > 64)
            {
                odp.RemoveAt(0);
                odr.RemoveAt(0);
            }
        }
        public float num = 0;
        public float num2 = 0;
        internal void AttackTarget(NPC target)
        {
            Projectile.velocity += num2.ToRotationVector2() * 1.4f;
            if (CEUtils.getDistance(Projectile.Center, target.Center) > 150)
            {
                num += 0.05f;
            }
            else
            {
                num = 0;
            }
            if (num > 1)
                num = 1;
            num2 = CEUtils.RotateTowardsAngle(num2, (target.Center - Projectile.Center).ToRotation(), num, false);
            Projectile.velocity *= 0.96f;
            Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation, MathHelper.Pi * 1.5f + (target.Center - Projectile.Center).ToRotation(), 0.06f, false);
            Projectile.pushByOther(0.1f);
        }
        List<float> odr = new List<float>();
        List<Vector2> odp = new List<Vector2>();
        public float trailAlpha = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = CEExtraAssets.PatchyTallNoise;
            List<ColoredVertex> ve = new List<ColoredVertex>();
            Color b = new Color(200, 200, 255);
            odp.Add(Projectile.Center);
            odr.Add(Projectile.rotation);
            for (int i = 0; i < odr.Count; i++)
            {
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (new Vector2(74 * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(odp[i] - Main.screenPosition + (new Vector2(-80 * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            odp.RemoveAt(odp.Count - 1);
            odr.RemoveAt(odr.Count - 1);
            if (ve.Count >= 3)
            {
                var gd = Main.graphics.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = CEEffectAssets.SwordTrail5;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue((new Color(190, 190, 255)).ToVector4());
                shader.Parameters["color1"].SetValue((new Color(160, 160, 255)).ToVector4());
                shader.Parameters["alpha"].SetValue(trailAlpha);
                shader.Parameters["xoffset"].SetValue(Main.GlobalTimeWrappedHourly * 4);

                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[1] = CEExtraAssets.Extra_201;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                trail = CEExtraAssets.SwordSlashTexture;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                trail = CEExtraAssets.Perlin;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }
            Texture2D tex = Projectile.GetTexture();
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * WhiteAlpha, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
