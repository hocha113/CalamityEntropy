using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Armor.VoidFaquir
{
    [AutoloadEquip(EquipType.Head)]
    public class VoidFaquirDevourerHelm : ModItem
    {
        public static int BaseDamage = 8000;
        public static float GetChargeValue(int damage)
        {
            //根据伤害获取充能数，充能最高为1
            return damage / 75000f;
        }
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.defense = 50;
            Item.rare = ModContent.RarityType<VoidPurple>();
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<VoidFaquirBodyArmor>() && legs.type == ModContent.ItemType<VoidFaquirCuises>();
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawOutlines = true;
        }
        public LocalizedText SetBonusText => Mod.GetLocalization("VoidFaquirMeleeBonus");
        public override void UpdateArmorSet(Player player)
        {
            player.GetArmorPenetration(DamageClass.Generic) += 20;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
            player.Entropy().VoidFaquirBonusAny = true;
            player.Entropy().VoidFaquirBonusMelee = true;
            player.setBonus = SetBonusText.Value;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.20f;
            player.GetCritChance(DamageClass.Melee) += 15;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<VoidBar>(), 14)
                .AddIngredient(ModContent.ItemType<TwistingNether>(), 4)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }

    public class VoidFaquirEnergyBallMelee: ModProjectile
    {
        public override bool? CanDamage()
        {
            return false;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.timeLeft = 3;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public float Scale => Projectile.scale * Projectile.ai[1];
        public int ShootDelay = 20;
        public override bool PreDraw(ref Color lightColor)
        {
            float pn = 1;
            Main.spriteBatch.UseAdditiveClamp();
            Texture2D pulse = CEUtils.getExtraTex("ShatteredExplosion");
            for (float i = 0; i < 1f; i += 0.2f)
            {
                float scale = CEUtils.Frac(i + Main.GlobalTimeWrappedHourly * 12);
                Main.spriteBatch.Draw(pulse, Projectile.Center - Main.screenPosition, null, Color.LightBlue * Projectile.Opacity * (1 - scale) * Scale, i * MathHelper.TwoPi, pulse.Size() * 0.5f, scale * Projectile.scale * 0.2f * pn, SpriteEffects.None, 0);
            }
            CEUtils.DrawGlow(Projectile.Center, new Color(160, 150, 255), 1.4f * Scale, setState: false);
            CEUtils.DrawGlow(Projectile.Center, new Color(160, 150, 255), 1.4f * Scale, setState: false);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void AI()
        {
            Player player = Projectile.owner.ToPlayer();
            player.Calamity().mouseWorldListener = true;
            Vector2 targetPos = (player.Calamity().mouseWorld - player.MountedCenter).normalize() * 95 + player.MountedCenter;
            Projectile.velocity *= 0.8f;
            Projectile.velocity += (targetPos - Projectile.Center) * 0.04f;
            Projectile.ai[1] = float.Lerp(Projectile.ai[1], Projectile.OwnerEntropy().VFMeleeCharge + (20 - ShootDelay) * 0.012f, 0.12f);
            if (Projectile.OwnerEntropy().VFMeleeCharge <= 0 && Projectile.ai[1] <= 0.02f)
                Projectile.ai[1] = 0;
            if (Scale > 0)
            {
                for(int i = 0; i < 12; i++)
                {
                    float r = CEUtils.randomRot();
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + r.ToRotationVector2() * Main.rand.NextFloat(2, 4) * Scale, r.ToRotationVector2() * Main.rand.NextFloat(1, 11), Color.Purple, 7, 1.4f * Scale, Scale * 0.5f, Main.rand.NextFloat(-0.05f, 0.05f), false), false, CalamityMod.Enums.GeneralDrawLayer.BeforeProjectiles);
                }
                float rt = CEUtils.randomRot();
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + rt.ToRotationVector2() * Main.rand.NextFloat(4, 10) * Scale, rt.ToRotationVector2() * Main.rand.NextFloat(1, 6), new Color(160, 160, 255), 6, 1f * Scale, Scale * 0.75f, Main.rand.NextFloat(-0.05f, 0.05f), true), false, CalamityMod.Enums.GeneralDrawLayer.AfterProjectiles);
            }
            if(player.Entropy().VFMeleeCharge >= 1)
            {
                if (ShootDelay-- <= 0)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (player.Entropy().VFMeleeCharge >= 1)
                        {
                            player.Entropy().VFMeleeCharge -= 1f;
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (player.Calamity().mouseWorld - Projectile.Center).normalize() * 14, ModContent.ProjectileType<VoidFaquirBeam>(), (int)player.GetDamage(DamageClass.Melee).ApplyTo(VoidFaquirDevourerHelm.BaseDamage), 8, player.whoAmI);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    player.Entropy().VFMeleeCharge = 0;
                }
            }
            else
            {
                ShootDelay = 20;
            }
            if (player.Entropy().VoidFaquirBonusMelee)
            {
                Projectile.timeLeft = 3;
                player.Entropy().VoidChargeBarValue = float.Clamp(Scale, 0, 1);
                player.Entropy().VoidChargeBarDraw = 2;
            }
            player.Entropy().VFProjectilePos = Projectile.Center + Projectile.velocity;
        }
    }
    public class VFChargeFXProj : ModProjectile
    {
        public override bool? CanDamage()
        {
            return false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> OldPos = new List<Vector2>();
        public override void AI()
        {
            for (int i = 0; i < 1; i++)
            {
                float r = CEUtils.randomRot();
                Color color = new Color(80, 80, 255);
                if (Main.rand.NextBool(3))
                    color = Color.Purple;
                if (Main.rand.NextBool(3))
                    color = Color.Blue;

                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + CEUtils.randomPointInCircle(4) + Projectile.velocity * Main.rand.NextFloat(), Projectile.velocity * Main.rand.NextFloat(0.2f, 0.64f), color, 15, 0.4f, 0.24f, Main.rand.NextFloat(-0.05f, 0.05f), false), false, CalamityMod.Enums.GeneralDrawLayer.BeforeProjectiles);
            }
            OldPos.Add(Projectile.Center);
            if(OldPos.Count > 40)
            {
                OldPos.RemoveAt(0);
            }
            Player player = Projectile.GetOwner();
            Vector2 tpos = player.Entropy().VFProjectilePos;
            if (Projectile.localAI[0]++ > 6)
            {
                Projectile.velocity *= 0.97f;
                Projectile.velocity += (tpos - Projectile.Center).normalize() * 0.3f;
                if (CEUtils.getDistance(Projectile.Center, tpos) < Projectile.velocity.Length() + 40)
                {
                    Projectile.Kill();
                    for (int i = 0; i < 24; i++)
                    {
                        float r = CEUtils.randomRot();
                        GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + CEUtils.randomPointInCircle(2), CEUtils.randomPointInCircle(6), new Color(200, 200, 255), 30, 0.4f, 0.8f, Main.rand.NextFloat(-0.03f, 0.03f), true), false, CalamityMod.Enums.GeneralDrawLayer.AfterProjectiles);
                    }
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (OldPos.Count > 1) 
            {
                List<CEUtils.VertexPointSets> ve = new List<CEUtils.VertexPointSets>();
                for (int i = 0; i < OldPos.Count; i++)
                {
                    float p = i / (OldPos.Count - 1f);
                    float w = p < 0.76f ? p / 0.76f : CEUtils.Parabola((p - 0.76f) / 0.24f * 0.5f + 0.5f, 1);
                    ve.Add(new CEUtils.VertexPointSets(OldPos[i], Color.White, w * 24, 0));
                }
                ThalassianWaterBolt.DrawTrail(ve, new Color(220, 210, 255), new Color(40, 20, 180), CEUtils.getExtraTex("Streak1"), CEUtils.getExtraTex("Streak2"), false, 0.8f);
            }

            return false;
        }
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.MaxUpdates = 8;
            Projectile.timeLeft = 1200;
        }
    }
    public class VoidFaquirBeam : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 6;
            Projectile.width = Projectile.height = 40;
            Projectile.timeLeft = 220;
        }
        public override string Texture => CEUtils.WhiteTexPath;
        public List<Vector2> OldPos = new List<Vector2>();
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("ApoctosisShoot", 1.2f, Projectile.Center, 16, 0.48f);
                for (int i = 0; i < 24; i++)
                {
                    float p = Main.rand.NextFloat();
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Projectile.velocity.RotatedBy(p * 0.2f * (Main.rand.NextBool() ? 1 : -1)).normalize() * Main.rand.NextFloat(0.3f, 1f) * (1.2f - p) * 42, "CalamityMod/Particles/SquareRotated", false, 16, 0.1f + (1 - p) * 0.6f, Color.Lerp(new Color(90, 90, 255), new Color(80, 60, 160), p), new Vector2(0.3f, 1f), true, true));
                }
            }
            if (Projectile.localAI[0]++ < 20)
            {
                Projectile.velocity = Projectile.velocity.normalize() * 6;
                if (OldPos.Count > 12)
                {
                    OldPos.RemoveAt(0);
                }
            }
            else
            {
                Projectile.velocity = Projectile.velocity.normalize() * 26;
            }
            OldPos.Add(Projectile.Center);
            if (OldPos.Count > 32)
            {
                OldPos.RemoveAt(0);
            }
            Color color = new Color(80, 10, 100);

            for (int i = 0; i < 1; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center + Projectile.velocity * Main.rand.NextFloat(), Projectile.velocity * Main.rand.NextFloat(0.2f, 0.64f), color, 15, 1.4f, 0.4f, Main.rand.NextFloat(-0.05f, 0.05f), false), false, CalamityMod.Enums.GeneralDrawLayer.BeforeProjectiles);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits < 2)
            {
                SoundStyle style = DeadSunsWind.Explosion with
                {
                    Pitch = 0.6f + Main.rand.NextFloat(-0.2f, 0.2f),
                    Volume = 0.2f
                };
                SoundEngine.PlaySound(in style, base.Projectile.Center);
                for (int i = 0; i < 24; i++)
                {
                    float p = Main.rand.NextFloat();
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Projectile.velocity.RotatedBy(p * 0.2f * (Main.rand.NextBool() ? 1 : -1)).normalize() * Main.rand.NextFloat(0.3f, 1f) * (1.2f - p) * 64, "CalamityMod/Particles/SquareRotated", false, 16, 0.05f + (1 - p) * 0.6f, Color.Lerp(new Color(90, 90, 255), new Color(80, 60, 160), p), new Vector2(0.3f, 1f), true, true));
                }
                EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, Color.White, 0.7f, 1, true, BlendState.Additive, 0, 12);
                EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, Color.White, 0.7f, 1, true, BlendState.Additive, 0, 12);
                EParticle.spawnNew(new ShineParticle(), Projectile.Center, Vector2.Zero, new Color(100, 100, 255), 1f, 1, true, BlendState.Additive, 0, 12);

                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, Projectile.velocity.RotatedBy(MathHelper.PiOver2).normalize() * 12, false, 18, 0.05f, Color.LightBlue, new Vector2(3f, 0.5f), true));
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(Projectile.Center, Projectile.velocity.RotatedBy(-MathHelper.PiOver2).normalize() * 12, false, 18, 0.05f, Color.LightBlue, new Vector2(3f, 0.5f), true));
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (OldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> ve = new List<CEUtils.VertexPointSets>();
                for (int i = 0; i < OldPos.Count; i++)
                {
                    float p = i / (OldPos.Count - 1f);
                    float w = p < 0.5f ? p / 0.5f : 1 - (p - 0.5f) / 0.5f;
                    w *= 1 + (1 - OldPos.Count / 32f) * 1.8f;
                    ve.Add(new CEUtils.VertexPointSets(OldPos[i], Color.White, w * 24, 0));
                }
                ThalassianWaterBolt.DrawTrail(ve, new Color(220, 210, 255), new Color(120, 120, 255), CEUtils.getExtraTex("Streak1"), CEUtils.getExtraTex("BasicTrailThin"), false, 0.6f);
            }
            return false;
        }
    }
}
