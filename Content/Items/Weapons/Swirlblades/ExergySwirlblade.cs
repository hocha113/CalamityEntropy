using CalamityEntropy.Content.Dusts;
using CalamityEntropy.Content.Items.Weapons.Thalassian;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using CalamityEntropy.Core.Weapons;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Weapons.Swirlblades
{
    public class ExergySwirlblade : ModItem, ICEChargeWeapon
    {
        // 充能条 5 秒；原潜伏乘数 伤害0.6/弹速1.2 并入释放乘数
        public CEChargeProfile ChargeProfile => CEChargeProfile.ChargeBar(5f, 0.6f, 1.2f);

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = Item.useTime = 42;
            Item.width = 70;
            Item.height = 70;
            Item.damage = 70;
            Item.crit = 6;
            Item.ArmorPenetration = 20;
            Item.UseSound = SoundID.Item1 with { Volume = 1.2f };
            Item.value = Item.buyPrice(platinum: 1);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<ExergySwirlbladeProj>();
            Item.shootSpeed = 49f;
            Item.knockBack = 3f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool ult = CEChargeWeapon.TryConsume(player, Item);
            int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (ult && p >= 0 && p < Main.maxProjectiles)
            {
                CEChargeWeapon.Empower(p);
            }
            return false;
        }
        public override void AddRecipes()
        {
            // 2026-08-31 平衡案:"灵魂轮刃"按轮刃链裁定为上一级符文轮刃
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<RunicSwirlblade>())
                .AddCalOrOwn(CEID.Item_MeldBlob, 8, ItemID.FragmentNebula, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
        public override bool RangedPrefix()
        {
            return true;
        }
    }
    public class ExergySwirlbladeProj : BaseSwirlblade
    {
        //拖影贴图,本文件两个类共用,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Extra/CircularSmearAlpha")]
        internal static Asset<Texture2D> SmearAlphaTex;
        public override string Texture => CEUtils.ItemTexPath<ExergySwirlblade>();
        public override int OldPosLength => 11;
        public override int FlyTime => Projectile.MaxUpdates * 17;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 6;
            Projectile.width = Projectile.height = 70;
            Projectile.tileCollide = false;
        }
        public override float Radius => 200 * (Projectile.IsEmpowered() ? 1.4f : 1) * Radius2;
        public float OriginalRadius { get { float r2 = Radius2; Radius2 = 1f; float rt = Radius; Radius2 = r2; return rt; } }
        public float Radius2 = 1;
        public override int SpreadTime => Projectile.IsEmpowered() ? 70 : 42;
        public Vector2 OffsetS = Vector2.Zero;
        public int Stick = -1;
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(Stick);
            writer.WriteVector2(OffsetS);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            Stick = reader.ReadInt32();
            OffsetS = reader.ReadVector2();
        }
        public override void OnCollideWithNPC(NPC npc)
        {
            Stick = npc.whoAmI;
            OffsetS = Projectile.Center - npc.Center;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Projectile.GetTexture();
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for(int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int i = 0; i < posC.Count; i++)
                {
                    float p = ((float)(1 + i) / posC.Count);
                    Color clr = Color.LightGreen * 0.6f * p;
                    Main.spriteBatch.Draw(tex, posC[i] - Main.screenPosition, null, clr, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * p, SpriteEffects.None, 0);
                }
                Main.spriteBatch.ExitShaderRegion();

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(140, 255, 140));
            }
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor, overridePos: Projectile.Center + (Spreaded ? CEUtils.randomPointInCircle(4) : Vector2.Zero)));
            if (BladeScale > 0)
            {
                Texture2D smear = SmearAlphaTex.Value;
                float scale = Radius / 78f * Projectile.scale * BladeScale;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 o = smear.Size() * 0.5f;
                Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.PointClamp);
                
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.7f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 1f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.7f, SpriteEffects.None, 0);

                BaseSwirlblade.ApplyShader(new Color(180, 255, 180));
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.97f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * -40f, o, scale * 0.96f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * 38f, o, scale * 0.95f, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(22, 255, 22) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.94f, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override void AI()
        {
            base.AI();
            if(BladeScale >= 0.2f)
            {
                float particleRot = CEUtils.randomRot();
                PRTLoader.NewParticle<PRT_AltLineCal>(Projectile.Center + particleRot.ToRotationVector2() * Radius * BladeScale * Projectile.scale, particleRot.ToRotationVector2().RotatedBy(-1.86f) * Main.rand.NextFloat(12, 18), (Main.rand.NextBool() ? Color.Black : Color.LightGreen) * BladeScale, Main.rand.NextFloat(0.6f, 1f) * 2.2f * BladeScale * Projectile.scale).Configure(false, Main.rand.Next(12, 16));
            }
            NPC stickNpc = null;
            if(Stick >= 0)
            {
                stickNpc = Stick.ToNPC();
                if(!stickNpc.active)
                {
                    stickNpc = null;
                    Stick = -1;
                }
            }
            float p = (Counter - FlyTime) / (float)SpreadTime;
            p = float.Clamp(p, 0, 1);
            if (Spreaded && Stick >= 0)
            {
                Projectile.Center = stickNpc.Center + OffsetS;
            }
            if(Spreaded)
            {
                Radius2 = 1 - p * 0.5f;
                if (++Projectile.localAI[1] % 13 == 0)
                {
                    int sawType = ModContent.ProjectileType<ExergySwirlbladeSaw>();
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(32, 38) * (Projectile.IsEmpowered() ? 1.5f : 1), sawType, Projectile.damage / 5, 6, Projectile.owner, OriginalRadius * 0.3f);
                    }
                }
            }
            CEUtils.AddLight(Projectile.Center, new Color(200, 255, 200));
        }
        public override void OnSpread()
        {
            CEUtils.PlaySound("SCSlash", Main.rand.NextFloat(0.9f, 1.2f), Projectile.Center);
            for (int i = 0; i < 10; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, (i / 10f * MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, (Main.rand.NextBool() ? Color.LightGreen : Color.SeaGreen) * 0.8f, Radius / 2400f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);
        }
        public override void OnRetract()
        {
            if(Projectile.IsEmpowered())
            {
                NPC target = CEUtils.FindTarget_HomingProj(Projectile, Projectile.Center, 2000);
                float rot = target == null ? CEUtils.randomRot() : (target.Center - Projectile.Center).ToRotation();
                int sawType = ModContent.ProjectileType<ExergySwirlbladeSaw>();
                if (Main.myPlayer == Projectile.owner)
                {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, rot.ToRotationVector2() * 50, sawType, Projectile.damage, 6, Projectile.owner, OriginalRadius * 0.5f, 1);
                    CEChargeWeapon.Empower(p);
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if(!target.boss)
            {
                target.velocity *= 0.6f;
            }
            CEUtils.PlaySound("VividClarityBeamAppear", Main.rand.NextFloat(1.2f, 1.5f), target.Center, volume: 0.5f);

            for (int i = 0; i < 12; i++)
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(0.6f, 1) * 8, Main.rand.NextBool() ? Color.LightGreen : Color.LightSeaGreen, 0.04f * Main.rand.NextFloat(0.65f, 1f)).Configure(false, 11, new Vector2(2.4f, 0.6f), true);

            float lrot = (Projectile.Center - target.Center).ToRotation() + (Main.rand.NextBool() ? 1 : -1) * 1.2f + Main.rand.NextFloat(-0.1f, 0.1f);
            for(int i = 0; i < 3; i++)
            {
                var line = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center + (target.Center - Projectile.Center).normalize() * 66, Vector2.Zero, Color.Black, 1);
                line.xadd = 2.4f;
                line.lx = 1.8f;
                line.endColor = Color.Black;
                line.spawnColor = Color.Black;
                line.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, lrot, 30);
            }
            var line2 = PRTLoader.NewParticle<PRT_AbyssalLine>(Projectile.Center + (target.Center - Projectile.Center).normalize() * 66, Vector2.Zero, Color.LightGreen, 1);
            line2.xadd = 2f;
            line2.lx = 1.5f;
            line2.endColor = Color.LightGreen * 1.2f;
            line2.spawnColor = Color.LightGreen * 1.2f;
            line2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, lrot, 30);
        }
    }
    public class ExergySwirlbladeSaw : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Ranged, false, -1);
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 148;
            Projectile.localNPCHitCooldown = 18;
            Projectile.light = 0.7f;
        }
        public override bool ShouldUpdatePosition()
        {
            return NoPosUpdate <= 0;
        }
        public void SpawnVParticles(int num = 1, float scale = 1)
        {
            float num2 = 360f / num;
            Color color1 = Color.LightGreen;
            Color color2 = Color.Black;
            for (int j = 0; (float)j < num; j++)
            {
                float num3 = CEUtils.randomRot();
                Vector2 vector = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Vector2 vector2 = (Vector2.UnitX * Main.rand.NextFloat(12, 3.1f)).RotatedBy(num3 * Main.rand.NextFloat(1.1f, 9.1f));
                Dust dust = Dust.NewDustPerfect(Projectile.Center + vector, Main.rand.NextBool(4) ? ModContent.DustType<LightDust>() : (ModContent.DustType<VoidDustInverted>()), vector2);
                dust.noGravity = dust.type != 278;
                dust.color = color1;
                dust.velocity = vector2 * scale;
                dust.scale = Main.rand.NextFloat(1.6f, 2.2f) * 0.7f * scale;
            }
        }
        public override void AI()
        {
            if (Projectile.Entropy().FirstFrames)
            {
                SoundStyle ShootSound = new("CalamityEntropy/Assets/Sounds/SawShot", 2) { PitchRange = (0.2f, 0.7f), Volume = 0.4f };
                SoundEngine.PlaySound(ShootSound, Projectile.Center);
                //ai[1] 随生成包同步，各端首帧本地打标即可
                if (Projectile.ai[1] > 0)
                    Projectile.SetEmpowered(false);
                for (int i = 0; i < 16; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                    dust.scale = Main.rand.NextFloat(0.6f, 1f) * 3f;
                    dust.velocity = Projectile.velocity.normalize().RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f, 1) * 44;
                    dust.noGravity = false;
                    dust.color = Main.rand.NextBool() ? Color.LightGreen : Color.LightSeaGreen;
                    dust.fadeIn = 2f;
                }
            }
            SpawnVParticles();
            if (NoPosUpdate > 0)
            {
                NoPosUpdate--;
            }
            else if (CD > 0)
            {
                CD--;
            }
            if (Projectile.timeLeft < 20)
                Projectile.Opacity -= 1 / 20f;
            else if (Projectile.localAI[0] ++ > 9)
                if(Projectile.IsEmpowered() || Projectile.numHits == 0)
                    Projectile.HomingToNPCNearby(4.2f, 0.94f, 1600);
            for(float i = 0.2f; i <= 1f; i += 0.2f)
            {
                oldPos.Add(Projectile.Center + Projectile.velocity * i);
                if (oldPos.Count > 60)
                    oldPos.RemoveAt(0);
            }
        }
        public int NoPosUpdate = 0;
        public int CD = 0;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CD <= 0)
            {
                Projectile.velocity = Projectile.velocity.normalize() * float.Max(Projectile.velocity.Length(), Projectile.IsEmpowered() ? 68 : 54);
                NoPosUpdate = 4;
                CD = 8;
                for (int i = 0; i < 6; i++)
                {
                    float rot = 2;
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16) * Projectile.scale, Color.LightGreen, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center + Projectile.velocity.normalize() * Radius * Projectile.scale, Projectile.velocity.normalize().RotatedBy(-rot).RotatedByRandom(0.3f) * Main.rand.NextFloat(4, 16) * Projectile.scale, Color.LightGreen, Projectile.scale * 0.04f).Configure(false, 16, new Vector2(0.3f, 1), false, false);
                }
            }
            for(int ii = 0; ii < 2; ii++)
            {
                float lrot = (Projectile.Center - target.Center).ToRotation() + (Main.rand.NextBool() ? 1 : -1) * 1.2f + Main.rand.NextFloat(-0.1f, 0.1f);
                for (int i = 0; i < 3; i++)
                {
                    var line = PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, Color.Black, 1);
                    line.xadd = 1.4f;
                    line.lx = 1.4f;
                    line.endColor = Color.Black;
                    line.spawnColor = Color.Black;
                    line.Configure(1, true, PRTDrawModeEnum.NonPremultiplied, lrot, 30);
                }
                var line2 = PRTLoader.NewParticle<PRT_AbyssalLine>(target.Center, Vector2.Zero, Color.LightGreen, 1);
                line2.xadd = 1.2f;
                line2.lx = 1.1f;
                line2.endColor = Color.LightGreen * 1.2f;
                line2.spawnColor = Color.LightGreen * 1.2f;
                line2.Configure(1, true, PRTDrawModeEnum.AdditiveBlend, lrot, 30);

            }
            CEUtils.PlaySound("slice", Main.rand.NextFloat(1, 1.4f), target.Center, 8, 0.6f);
            float scale = 1.5f;
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, ModContent.DustType<SquashDust>(), Vector2.Zero);
                dust.scale = Main.rand.NextFloat(0.3f, 1f) * scale * 1.6f * Projectile.scale;
                dust.velocity = CEUtils.randomPointInCircle(30 * Projectile.scale);
                dust.noGravity = false;
                dust.color = Main.rand.NextBool() ? Color.LightGreen : Color.LightSeaGreen;
                dust.fadeIn = 2f;
            }
            scale = 1.6f;
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.LightSeaGreen * 0.8f, scale * 1f * Projectile.scale).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, Color.White * 0.8f, scale * 0.5f * Projectile.scale).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 7);
        }

        public float BladeScale => 1;
        public float Radius => Projectile.ai[0];
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return new Circle(projHitbox.Center.ToVector2(), Radius * Projectile.scale * BladeScale).Intersects(targetHitbox);
        }
        public List<Vector2> oldPos = new List<Vector2>();
        public override bool PreDraw(ref Color lightColor)
        {
            if (oldPos.Count > 1)
            {
                List<CEUtils.VertexPointSets> vp = new();
                List<Vector2> posC = new List<Vector2>();
                for (int i = 1; i < oldPos.Count; i++)
                {
                    for (float j = 0.2f; j <= 1f; j += 0.2f)
                        posC.Add(Vector2.Lerp(oldPos[i - 1], oldPos[i], j));
                }

                for (int i = 0; i < posC.Count; i++)
                {
                    float p = (i / (posC.Count - 1f));
                    float alpha = p * 0.8f + 0.2f;
                    float width = p;
                    vp.Add(new CEUtils.VertexPointSets(posC[i], Color.White * alpha, 22 * Projectile.scale * width, 0));
                }
                ThalassianWaterBolt.DrawTrail(vp, new Color(255, 255, 255), new Color(140, 255, 140));
            }
            Texture2D smear = ExergySwirlbladeProj.SmearAlphaTex.Value;
            float scale = Radius / 78f * Projectile.scale * BladeScale;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 o = smear.Size() * 0.5f;
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.PointClamp);

            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -42f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * -36f, o, scale * 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(0, 0, 0) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.7f, SpriteEffects.None, 0);

            Main.spriteBatch.UseBlendState(BlendState.Additive, SamplerState.PointClamp);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(190, 246, 190) * Projectile.Opacity * BladeScale, time * 42f, o, scale * 0.9f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(190, 246, 190) * Projectile.Opacity * BladeScale, time * -34f, o, scale * 0.9f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center + CEUtils.randomPointInCircle(4 * Projectile.scale) - Main.screenPosition, null, new Color(190, 246, 190) * Projectile.Opacity * BladeScale, time * 36f, o, scale * 0.64f, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return (Projectile.Opacity > 0.6f && Projectile.localAI[0] > 10) ? null : false;
        }
        public override string Texture => CEUtils.WhiteTexPath;
    }
}
