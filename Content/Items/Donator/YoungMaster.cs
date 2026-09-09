using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Cooldowns;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Cooldowns;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityEntropy.CEUtils;
using CalamityEntropy.Core.CalamityRef;
namespace CalamityEntropy.Content.Items.Donator
{
    public class YoungMaster : ModItem, IDonatorItem
    {
        public string DonatorName => "无华";

        public override void AddRecipes()
        {
            // 灾厄原料按 material-map.md 替换：SaharaSlicers→光辉飞盘
            CreateRecipe()
                .AddCalOrOwn(CEID.Item_SaharaSlicers, ItemID.LightDisc)
                .AddIngredient(ItemID.TitaniumBar, 10)
                .AddIngredient(ItemID.SoulofMight, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        public override void SetDefaults()
        {
            Item.width = 120;
            Item.height = 120;
            Item.damage = 125;
            Item.crit = 10;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<YoungMasterProj>();
            Item.shootSpeed = 11;
            Item.DamageType = DamageClass.Melee;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 && player.ownedProjectileCounts[Item.shoot] > 0)
                return false;
            return true;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse == 2 && !player.HasCooldown(YoungMasterDashCD.ID))
            {
                type = ModContent.ProjectileType<YoungMasterDash>();
                player.AddCooldown(YoungMasterDashCD.ID, 30 * 60);
                velocity = new Vector2(velocity.X > 0 ? 1 : -1, 0) * 16;
                position += velocity.normalize() * 40;
            }
        }
    }
    public class YoungMasterProj : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/YoungMaster";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16 * 4;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1600;
            Projectile.MaxUpdates = 4;
            Projectile.width = Projectile.height = 140;
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                float sm = Mp.EnhancedTime-- > 0 ? 1.8f : 1.4f;
                Projectile.scale *= sm;
                Projectile.width = Projectile.height = (int)(Projectile.width * sm);
            }
            if (Projectile.ai[2]-- <= 0)
            {
                HCooldown--;
                Player player = Projectile.GetOwner();
                if (Projectile.localAI[0]++ > 90)
                {
                    Projectile.velocity *= 0.98f;
                    Projectile.velocity += (player.Center - Projectile.Center).normalize() * 0.54f;
                    if (Projectile.Hitbox.Intersects(player.Center.getRectCentered(80, 80)))
                        Projectile.Kill();
                }
                if (Projectile.localAI[1]++ > Projectile.MaxUpdates * 9)
                {
                    Projectile.localAI[1] -= Projectile.MaxUpdates * 9;
                    CEUtils.PlaySound("spin" + Main.rand.Next(1, 3), Main.rand.NextFloat(0.7f, 1f), Projectile.Center, 60, 0.6f);
                }
            }
            Projectile.rotation += 0.1f * (Projectile.whoAmI % 2 == 0 ? 1 : -1);
            oldPos.Add(Projectile.Center + (Projectile.ai[2] <= 0 ? Projectile.velocity : Vector2.Zero));
            oldRot.Add(Projectile.rotation);
            if (oldPos.Count > 36)
            {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
            }
        }
        public override bool ShouldUpdatePosition()
        {
            return Projectile.ai[2] <= 0;
        }
        public int HCooldown = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (HCooldown <= 0)
            {
                HCooldown = 3 * Projectile.MaxUpdates;
                Projectile.ai[2] = 5 * Projectile.MaxUpdates;
            } 
            CEUtils.PlaySound("slice", 1, target.Center);
            Color c = Mp.EnhancedTime > 0 ? new Color(255, 120, 120) : Color.Silver;
            for (int i = 0; i < 6; i++)
            {
                float rot = 2;
                //PRT_GlowSparkCal CalamityPorts,Configure签名对齐Calamity原构造
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * 60 * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.2f) * Main.rand.NextFloat(4, 14), c, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * 60 * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.2f) * Main.rand.NextFloat(4, 14), c, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
            }
            if (Mp.EnhancedTime > 0)
            {
                CEUtils.PlaySound("pulseBlast", 0.8f, Projectile.Center, 6, 0.8f);
                PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center + Projectile.velocity.normalize() * 80, Vector2.Zero, Color.Firebrick, 0.1f).Configure(1.5f, 8);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity.normalize() * 80, Vector2.Zero, Color.Firebrick, 3.6f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity.normalize() * 80, Vector2.Zero, Color.White, 2.4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
                if (Projectile.owner == Main.myPlayer)
                {
                    CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center + Projectile.velocity.normalize() * 80, Projectile.damage / 8, 160, Projectile.DamageType);
                }
                for (int i = 0; i < 32; i++)
                {
                    var d = Dust.NewDustDirect(Projectile.Center + Projectile.velocity.normalize() * 80, 0, 0, DustID.Firework_Yellow);
                    d.scale = 0.8f;
                    d.velocity = CEUtils.randomPointInCircle(14);
                    d.position += d.velocity * 4;
                }
            }
        }
        public YMPlayer Mp => Projectile.GetOwner().GetModPlayer<YMPlayer>();
        public override bool PreDraw(ref Color lightColor)
        {
            if (oldPos.Count > 2)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.LinearWrap);
                List<VertexPointSets> sets1 = new List<VertexPointSets>();
                List<VertexPointSets> sets2 = new List<VertexPointSets>();
                Color c = Mp.EnhancedTime > 0 ? new Color(255, 10, 10) : Color.Silver;
                for (int i = 0; i < oldPos.Count; i++)
                {
                    sets1.Add(new VertexPointSets(oldPos[i] + oldRot[i].ToRotationVector2() * 74 * Projectile.scale, c, 10, (i / (oldPos.Count - 1f)) + Main.GlobalTimeWrappedHourly * 4));
                    sets2.Add(new VertexPointSets(oldPos[i] - oldRot[i].ToRotationVector2() * 74 * Projectile.scale, c, 10, (i / (oldPos.Count - 1f)) + Main.GlobalTimeWrappedHourly * 4));
                }
                var gd = Main.graphics.GraphicsDevice;

                var ve = CEUtils.GetVertexesList(sets1);
                var ve2 = CEUtils.GetVertexesList(sets2);

                gd.Textures[0] = CEExtraAssets.Streak2;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
                gd.Textures[0] = CEExtraAssets.SylvestaffStreak;
                for (int i = 0; i < sets1.Count; i++)
                {
                    sets1[i].Color = Color.White;
                    sets2[i].Color = Color.White;
                    sets1[i].Width = 36f;
                    sets2[i].Width = 36f;
                }
                ve = CEUtils.GetVertexesList(sets1);
                ve2 = CEUtils.GetVertexesList(sets2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve2.ToArray(), 0, ve2.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }

            Texture2D tex = Mp.EnhancedTime > 0 ? this.getTextureAlt() : Projectile.GetTexture();
            Vector2 position = Projectile.Center - Main.screenPosition;
            float adr = 1.14f;
            Main.spriteBatch.Draw(tex, position, null, Color.White, Projectile.rotation + adr, new Vector2(10, tex.Height - 10), Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, position, null, Color.White, Projectile.rotation + adr + MathHelper.Pi, new Vector2(10, tex.Height - 10), Projectile.scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
    public class YoungMasterDash : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Donator/YoungMasterAlt";
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, true, -1);
            Projectile.timeLeft = 30;
            Projectile.width = Projectile.height = 40;
        }
        public float v = 1f;
        public Vector2 target = Vector2.Zero;
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                CEUtils.PlaySound("AntivoidDash", 1.4f, Projectile.Center, 8, 0.5f);
            }
            if (Projectile.ai[0] == 0)
            {
                Projectile.GetOwner().gfxOffY = 0;
                Projectile.Center += v * Projectile.velocity * 1.8f;
                Projectile.GetOwner().Center = Projectile.Center - Projectile.velocity.normalize() * 40;
                Projectile.GetOwner().velocity *= 0f;
                v *= 0.976f;
                Projectile.GetOwner().itemAnimation = Projectile.GetOwner().itemTime = 5;
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center - Projectile.velocity.normalize() * 46f + CEUtils.randomPointInCircle(24), Projectile.velocity * -0.4f, new Color(255, 100, 100), Projectile.scale * 0.06f).Configure(false, 20, new Vector2(0.2f, 1), false, false);
            }
            else
            {
                Player player = Projectile.GetOwner();
                player.wingTime = 0;
                Projectile.Center = player.Center;
                Projectile.ai[0]++;
                if (Projectile.ai[0] == 2)
                {
                    player.velocity += new Vector2(Projectile.velocity.normalize().X * -4, -14);
                    CEUtils.PlaySound("GrassSwordHit0", 1, Projectile.Center);
                }
                player.Entropy().gravAddTime = 2;

                if (Projectile.ai[0] == 30)
                {
                    Vector2 tb = (target - player.Center).normalize();
                    PRTLoader.NewParticle<PRT_DirectionalPulseRing>(player.Center, tb * 1, new Color(255, 120, 120), 0.8f).Configure(new Vector2(0.16f, 1), tb.ToRotation(), 3f, 32);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        player.velocity += tb * -25;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.Center, tb * 90, ModContent.ProjectileType<YMSlash>(), Projectile.damage * 8, 8, Projectile.owner);
                    }
                    CEUtils.PlaySound("swing1", 1, Projectile.Center);
                    player.Entropy().CruiserAntiGravTime = 12;
                }
                if (Projectile.ai[0] < 30)
                    player.GetModPlayer<YMPlayer>().Rotation += player.velocity.X * 0.08f;
                else
                    player.GetModPlayer<YMPlayer>().Rotation += Math.Sign(player.velocity.X) * 0.5f;
                if (Projectile.ai[0] > 40)
                {
                    if (player.velocity.Y == 0)
                        Projectile.Kill();
                }
                else
                {
                    Projectile.GetOwner().itemAnimation = Projectile.GetOwner().itemTime = 5;
                }
            }
            Projectile.OwnerEntropy().immune = 30;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool? CanDamage()
        {
            return Projectile.ai[0] == 0;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.GetOwner().GetModPlayer<YMPlayer>().EnhancedTime = 10;
            Projectile.ai[0]++;
            Projectile.timeLeft = 120;
            this.target = target.Center;
            for (int i = 0; i < 32; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center - Projectile.velocity.normalize() * 0.5f + CEUtils.randomPointInCircle(24), Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(-0.8f, -3), new Color(255, 100, 100), Projectile.scale * 0.06f).Configure(false, 20, new Vector2(0.2f, 1), false, false);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[0] != 0)
                return false;
            int dir = Projectile.velocity.X > 0 ? 1 : -1;
            Texture2D tex = Projectile.GetTexture();
            Main.spriteBatch.Draw(tex, Projectile.Center - Projectile.velocity.normalize() * 18 - Main.screenPosition, null, Color.White, Projectile.rotation + 0.52f * dir, tex.Size() * 0.5f, Projectile.scale * 0.6f, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Projectile.velocity.normalize() * 18 - Main.screenPosition, null, Color.White, Projectile.rotation - 0.32f * dir, tex.Size() * 0.5f, Projectile.scale * 0.6f, dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            return false;
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
    }
    public class YMPlayer : ModPlayer
    {
        public int EnhancedTime = 0;
        public float Rotation = 0;
        public override void PostUpdate()
        {
            if (Player.Entropy().gravAddTime <= 0)
                Rotation = CEUtils.RotateTowardsAngle(Rotation, 0, 0.16f, false);
            if (Math.Abs(Rotation) > 0.02f)
            {
                Player.fullRotation = Rotation;
                Player.fullRotationOrigin = new Vector2(12, 17);
            }
            else
            {
                if (Rotation != 0)
                {
                    Rotation = 0;
                    Player.fullRotation = 0;
                }
            }
        }
    }
    public class YMSlash : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.width = Projectile.height = 160;
            Projectile.timeLeft = 17;
            Projectile.localNPCHitCooldown = -1;
        }
        Vector2 lp1 = Vector2.Zero;
        Vector2 lp2 = Vector2.Zero;
        public override void AI()
        {
            Projectile.velocity *= 0.9f;
            if (Projectile.timeLeft < 8)
                Projectile.Opacity -= 1 / 8f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Vector2 p1 = Projectile.Center + new Vector2(-10, 118).RotatedBy(Projectile.rotation) * Projectile.scale * Projectile.Opacity;
            Vector2 p2 = Projectile.Center + new Vector2(-10, -118).RotatedBy(Projectile.rotation) * Projectile.scale * Projectile.Opacity;
            if (lp1 == Vector2.Zero)
            {
                lp1 = p1 - Projectile.velocity;
                lp2 = p2 - Projectile.velocity;
            }
            PRTLoader.NewParticle<PRT_GlowSparkCal>(p1, (p1 - lp1).normalize() * 6, new Color(255, 100, 100) * Projectile.Opacity, Projectile.scale * 0.036f * Projectile.Opacity).Configure(false, 16, new Vector2(0.4f, 1), false, false);
            PRTLoader.NewParticle<PRT_GlowSparkCal>(p2, (p2 - lp2).normalize() * 6, new Color(255, 100, 100) * Projectile.Opacity, Projectile.scale * 0.036f * Projectile.Opacity).Configure(false, 16, new Vector2(0.4f, 1), false, false);
            lp1 = p1;
            lp2 = p2;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Texture2D tex = Projectile.GetTexture();
            float wd = 0;
            Vector2 s = tex.Size() * 2 * Projectile.scale * Projectile.Opacity;
            float wh = s.X * 0.5f;
            float hh = s.Y * 0.5f;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            {
                wd = 36 * Projectile.scale;
                List<Vector2> points = new List<Vector2>()
                {
                new Vector2(-wh, -hh),
                new Vector2(wh, -hh),
                new Vector2(-wh, hh),
                new Vector2(wh, hh)
                };
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = points[i].RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * wd * (points[i].Y > 0 ? -1 : 1) + Projectile.Center - Main.screenPosition;
                }
                CEUtils.drawTextureToPoint(Main.spriteBatch, tex, new Color(255, 40, 40) with { A = (byte)(255 * Projectile.Opacity) }, points[0], points[1], points[2], points[3]);
            }
            {
                wd = -36 * Projectile.scale;
                List<Vector2> points = new List<Vector2>()
                {
                new Vector2(-wh, -hh),
                new Vector2(wh, -hh),
                new Vector2(-wh, hh),
                new Vector2(wh, hh)
                };
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = points[i].RotatedBy(Projectile.rotation) + Projectile.rotation.ToRotationVector2() * wd * (points[i].Y > 0 ? -1 : 1) + Projectile.Center - Main.screenPosition;
                }
                CEUtils.drawTextureToPoint(Main.spriteBatch, tex, new Color(255, 40, 40) with { A = (byte)(255 * Projectile.Opacity) } * Projectile.Opacity, points[0], points[1], points[2], points[3]);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CEUtils.PlaySound("VividClarityBeamAppear", 1, Projectile.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<YoungMasterMark>(), Projectile.damage / 2, 0, Projectile.owner);
        }
    }
    public class YoungMasterMark : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Melee, false, -1);
            Projectile.timeLeft = 30;
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.owner.ToPlayer(), Projectile.Center + Projectile.velocity.normalize() * 80, Projectile.damage, 240, Projectile.DamageType);
            }
            CEUtils.PlaySound("pulseBlast", 0.8f, Projectile.Center, 6, 0.8f);
            PRTLoader.NewParticle<PRT_PulseRing>(Projectile.Center + Projectile.velocity.normalize() * 80, Vector2.Zero, Color.Firebrick, 0.1f).Configure(1.8f, 8);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity.normalize() * 80, Vector2.Zero, Color.Firebrick, 4f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
            PRTLoader.NewParticle<PRT_ShineParticle>(Projectile.Center + Projectile.velocity.normalize() * 80, Vector2.Zero, Color.White, 3f).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 16);
        }
    }
}
