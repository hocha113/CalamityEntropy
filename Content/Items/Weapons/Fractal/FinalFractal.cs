using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Fractal
{
    public class FinalFractal : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 760;
            Item.DamageType = DamageClass.Melee;
            Item.width = 48;
            Item.height = 60;
            Item.useTime = Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 7;
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
            Item.rare = ModContent.RarityType<AbyssalBlue>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FinalFractalHeld>();
            Item.shootSpeed = 12f;
            Item.scale *= 0.66f;
        }
        public int atkType = 0;
        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2 && player.chaosState) {
                return false;
            }
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse == 2) {
                CEUtils.PlaySound("VoidAnticipation", 1, position, volume: CEUtils.WeapSound);
                player.AddBuff(BuffID.ChaosState, 10 * 60);
                Projectile.NewProjectile(source, position, velocity * 4, ModContent.ProjectileType<VoidSlash>(), damage * 10, 0, player.whoAmI, 1);
                return false;
            }
            int at = 2;
            if (atkType == 0 || atkType == 2 || atkType == 4 || atkType == 6) {
                at = -1;
            }
            if (atkType == 1 || atkType == 3 || atkType == 5 || atkType == 7) {
                at = 1;
            }
            if (atkType == 9) {
                at = 3;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, at, 0, Main.MouseWorld.Distance(position) + 180);
            atkType += 1;
            if (atkType > 9) {
                atkType = 0;
            }
            return false;
        }
        public override bool MeleePrefix() {
            return true;
        }
        public override bool AltFunctionUse(Player player) {
            return true;
        }
        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Zenith)
                .AddIngredient<VoidFractal>()
                .AddIngredient<FadingRunestone>()
                .AddTile<VoidWellTile>()
                .Register();
        }
    }
    public class FinalFracRightClick : ModProjectile
    {
        //拖尾着色器,本文件多个类共用,加载期由 VaultLoaden 赋值,仅绘制路径读取
        [VaultLoaden("CalamityEntropy/Assets/Effects/FinalFrac", AssetMode.EffectValue, "EffectPass")]
        internal static Effect FinalFracShader;
        public int s = 0;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.ai[0] == 3) {
                return null;
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (120) * Projectile.scale, targetHitbox, 64);
        }
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/FinalFractal";
        List<float> odr = new List<float>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 9;
            Projectile.light = 1;
        }
        public float rotSpeed = 0;
        public override void AI() {
            Player owner = Projectile.GetOwner();

            rotSpeed += 0.0002f;
            rotSpeed *= 0.99f;
            Projectile.rotation += rotSpeed;
            odr.Add(Projectile.rotation);
            if (odr.Count > 42) {
                odr.RemoveAt(0);
            }
            if (owner.whoAmI != Main.myPlayer || (owner.whoAmI == Main.myPlayer && Mouse.GetState().RightButton == ButtonState.Pressed)) {
                owner.heldProj = Projectile.whoAmI;
                owner.itemTime = 4;
                owner.itemAnimation = 4;
                Projectile.timeLeft = 4;
            }
            if (owner.whoAmI == Main.myPlayer) {
                Projectile.ai[1]++;
                if (Projectile.ai[1] > Projectile.MaxUpdates * 0.8f && s < 3) {
                    Projectile.ai[1] = 0;
                    s++;
                    int max = 8 * s;
                    float a = 0;
                    for (float i = 0; i < 358; i += 360f / max) {
                        a += 360f / max;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FinalFractalBlade>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner, 1, s * 360, a);
                    }
                }
            }
        }
        public override void OnKill(int timeLeft) {
            if (s >= 3) {
                Projectile.GetOwner().Teleport(Projectile.GetOwner().mouseWorld(), -1);
            }
        }
        public override bool PreDraw(ref Color lightColor) {
            float alpha = 1;
            float texAlpha = 1;
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            Texture2D tex = Projectile.GetTexture();
            Texture2D phantom = this.getTextureGlow();
            GraphicsDevice gd = Main.spriteBatch.GraphicsDevice;
            Texture2D trail = CEExtraAssets.MotionTrail2;
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < odr.Count; i++) {
                Color b = new Color(220, 200, 255);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(240, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition,
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = FinalFracRightClick.FinalFracShader;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue(new Color(220, 200, 255).ToVector4());
                shader.Parameters["color1"].SetValue(new Color(100, 100, 150).ToVector4());
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }
            Vector2 origin = new Vector2(0, tex.Height);
            SpriteEffects effect = SpriteEffects.None;
            float rot = Projectile.rotation + MathHelper.PiOver4;

            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha * texAlpha, rot, origin, Projectile.scale * 1.1f, effect);

            return false;
        }
    }

    public class FinalFractalHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/FinalFractal";
        List<float> odr = new List<float>();
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 100000;
            Projectile.extraUpdates = 9;
            Projectile.light = 1;
        }
        public float counter = 0;
        public float scale = 1;
        public float alpha = 0;
        public bool init = true;
        public bool shoot = true;
        public float length = 1200;
        public float spawnProjCounter = 0;
        public override void AI() {
            Player owner = Projectile.GetOwner();

            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            if (!(Projectile.ai[0] == 3 && OnNPC != null && OnNPCTime > 0)) {
                counter += 1 * (Projectile.ai[0] == 3 ? 1f : Projectile.ai[0] == 2 ? 1 : 2.5f);
            }
            else {
                OnNPCTime--;
            }

            if (init) {
                float scale_ = owner.HeldItem.scale;
                owner.ApplyMeleeScale(ref scale_);
                Projectile.scale *= scale_;
                if (Projectile.ai[0] == 2) {
                    CEUtils.PlaySound("sf_use", 0.6f, Projectile.Center, volume: 0.8f * CEUtils.WeapSound);
                    CEUtils.PlaySound("CastTriangles", 1, Projectile.Center, volume: CEUtils.WeapSound);

                }
                if (Projectile.ai[0] < 2) {
                    CEUtils.PlaySound("sf_use", 1 + Projectile.ai[0] * 0.12f, Projectile.Center, volume: 0.6f * CEUtils.WeapSound);
                }
                if (Projectile.ai[0] == 3) {
                    CEUtils.PlaySound("sf_use", 0.75f, Projectile.Center, volume: CEUtils.WeapSound);
                }
                init = false;
            }

            Projectile.timeLeft = 3;
            if (Projectile.ai[0] == 2) {
                if (shoot) {
                    shoot = false;
                    if (Main.myPlayer == Projectile.owner) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity.normalize() * 1100, Projectile.velocity.normalize() * 10, ModContent.ProjectileType<FractalLaser>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
                    }
                    CalamityEntropy.FlashEffectStrength = 0.3f;
                    CEUtils.PlaySound("sf_shoot", 1, Projectile.Center, volume: CEUtils.WeapSound);
                }
                float l = (float)(Math.Cos(progress * MathHelper.Pi - MathHelper.PiOver2));
                Projectile.rotation = Projectile.velocity.ToRotation();
                scale = 1f + l * 2f;
                alpha = l;
                Projectile.Center = Projectile.GetOwner().MountedCenter + Projectile.velocity.normalize() * (-34 + l * 34);

            }
            else {
                if (Projectile.ai[0] == 3) {
                    Projectile.Resize(64, 64);
                    Projectile.Center = CEUtils.GetOwner(Projectile).MountedCenter + CEUtils.normalize(Projectile.velocity) * (CEUtils.Parabola(progress, length));
                    if (OnNPC != null && OnNPCTime > 0) {
                        Projectile.Center = OnNPC.Center;
                        length = CEUtils.getDistance(owner.Center, OnNPC.Center);
                        Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy((OnNPC.Center - owner.Center).ToRotation());
                        if (!OnNPC.active) {
                            OnNPCTime = 0;
                        }
                        counter = MaxUpdateTimes / 2f;
                    }
                    Projectile.rotation = (Projectile.Center - owner.Center).ToRotation();
                    alpha = 1;
                    scale = 2;
                }
                else {
                    if (shoot) {
                        shoot = false;

                        if (Projectile.owner == Main.myPlayer) {
                            int type = ModContent.ProjectileType<FinalFractalBlade>();
                            for (int i = 0; i < 6; i++) {
                                Vector2 spawnPos = Projectile.GetOwner().mouseWorld() + CEUtils.randomRot().ToRotationVector2() * Main.rand.Next(1200, 2400);
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPos, (Projectile.GetOwner().mouseWorld() - spawnPos) * 0.01f * Main.rand.NextFloat(0.8f, 1.2f), type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                            }
                        }
                    }
                    float RotF = 4f;
                    alpha = 1;
                    scale = 1.8f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + (RotF * -0.5f + RotF * CEUtils.GetRepeatedCosFromZeroToOne(progress, 3)) * Projectile.ai[0] * (Projectile.velocity.X > 0 ? -1 : 1);
                    Projectile.rotation = Projectile.rotation.ToRotationVector2().ToRotation();
                    Projectile.Center = Projectile.GetOwner().MountedCenter;
                    odr.Add(Projectile.rotation);
                    if (odr.Count > 20) {
                        odr.RemoveAt(0);
                    }
                }
                if (Projectile.velocity.X > 0) {
                    owner.direction = 1;
                    owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                }
                else {
                    owner.direction = -1;
                    owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)(Math.PI * 0.5f));
                }
            }


            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            if (counter > MaxUpdateTimes) {
                Projectile.Kill();
                owner.itemTime = 1;
                owner.itemAnimation = 1;
            }
        }
        public NPC OnNPC = null;
        public int OnNPCTime = 140;
        public override bool ShouldUpdatePosition() {
            return false;
        }

        public bool playHitSound = true;
        public override bool? CanHitNPC(NPC target) {
            if (Projectile.ai[0] == 3) {
                Projectile.localNPCHitCooldown = 2 * 10;
            }
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            //dedServ时PRTLoader.NewParticle给孤儿实例,Configure照常
            PRTLoader.NewParticle<PRT_ShineParticle>(target.Center, Vector2.Zero, new Color(225, 200, 255), 0.6f)
                .Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0, 12);

            if (Projectile.ai[0] == 3 && OnNPCTime > 0 && OnNPC == null) {
                OnNPC = target;
            }
            if (Projectile.ai[0] == 3) {
                scale = 2;
                //dedServ时NewParticle给孤儿实例不是null,后面字段赋值照常别挡
                if (!Main.dedServ) {
                    for (int i = 0; i < 6; i++) {
                        Vector2 ver = CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(-12, 12);
                        //PRT_Light burst,Configure opacity/glow/mode对齐旧spawnNew
                        PRTLoader.NewParticle<PRT_Light>(target.Center, ver, new Color(220, 180, 255), Main.rand.NextFloat(1.3f, 1.7f))
                            .Configure(0.15f, lifetime: 60);
                    }
                }
                CEUtils.PlaySound("runesonghit", Main.rand.NextFloat(0.6f, 1.4f), target.Center, 64, volume: CEUtils.WeapSound);
            }
            EGlobalNPC.AddVoidTouch(target, 40, 1.4f, 600, 16);
            if (playHitSound) {
                playHitSound = false;
                CEUtils.PlaySound("sf_hit", 1, Projectile.Center, volume: CEUtils.WeapSound);
                CEUtils.PlaySound("FractalHit", 1, Projectile.Center, volume: CEUtils.WeapSound);

            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.TrueExcalibur, new ParticleOrchestraSettings {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        public bool spawnProj = true;
        public override bool PreDraw(ref Color lightColor) {
            Player owner = Projectile.GetOwner();
            float MaxUpdateTimes = owner.itemTimeMax * Projectile.MaxUpdates;
            float progress = (counter / MaxUpdateTimes);
            Texture2D tex = Projectile.GetTexture();
            Texture2D phantom = this.getTextureGlow();
            GraphicsDevice gd = Main.spriteBatch.GraphicsDevice;
            if (Projectile.ai[0] == 3) {
                CEUtils.drawChain(Projectile.Center, CEUtils.GetOwner(Projectile).Center, 18, "CalamityEntropy/Assets/Extra/FFChain");
            }
            Texture2D trail = CEExtraAssets.MotionTrail2;
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < odr.Count; i++) {
                Color b = new Color(220, 200, 255);
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition + (new Vector2(240 * Projectile.scale, 0).RotatedBy(odr[i])),
                      new Vector3((i) / ((float)odr.Count - 1), 1, 1),
                      b));
                ve.Add(new ColoredVertex(Projectile.Center - Main.screenPosition,
                      new Vector3((i) / ((float)odr.Count - 1), 0, 1),
                      b));
            }
            if (ve.Count >= 3) {
                SpriteBatch sb = Main.spriteBatch;
                Effect shader = FinalFracRightClick.FinalFracShader;
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Parameters["color2"].SetValue(new Color(220, 200, 255).ToVector4());
                shader.Parameters["color1"].SetValue(new Color(100, 100, 150).ToVector4());
                shader.CurrentTechnique.Passes["EffectPass"].Apply();

                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
                Main.spriteBatch.ExitShaderRegion();
            }


            float texAlpha = 1;
            float phantomAlpha = 0;
            int dir = (int)(Projectile.ai[0] <= 1 ? Projectile.ai[0] : -1) * (Projectile.velocity.X > 0 ? -1 : 1);
            if (Projectile.ai[0] == 2) {
                dir = Math.Sign(Projectile.velocity.X);
            }
            Vector2 origin = dir > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height);
            if (Projectile.ai[0] == 3) {
                origin = tex.Size() / 2f;
            }
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? Projectile.rotation + MathHelper.PiOver4 : Projectile.rotation + MathHelper.Pi * 0.75f;

            float MaxUpdateTime = Projectile.GetOwner().itemTimeMax * Projectile.MaxUpdates;


            Main.EntitySpriteDraw(tex, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, lightColor * alpha * texAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);
            Main.EntitySpriteDraw(phantom, Projectile.Center + Projectile.GetOwner().gfxOffY * Vector2.UnitY - Main.screenPosition, null, Color.White * alpha * phantomAlpha, rot, origin, Projectile.scale * scale * 1.1f, effect);

            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.ai[0] == 3) {
                return null;
            }
            return CEUtils.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (120) * Projectile.scale * scale, targetHitbox, 64);
        }
        public override void CutTiles() {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * (120) * Projectile.scale * scale, 84, DelegateMethods.CutTiles);
        }

    }
    public class FinalFractalBlade : ModProjectile
    {
        public static List<int> swords;
        public override void Load() {
            swords = new List<int>();
        }
        public override void Unload() {
            swords = null;
        }
        public override void AI() {
            Projectile.GetOwner().Entropy().MouseWorldListener = true;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[0] == 1) {
                Projectile.timeLeft = 200 * 8;
                Projectile.localAI[0] += (Projectile.ai[1] - Projectile.localAI[0]) * 0.001f;
                Projectile.ai[2] += 0.0001f;
                Projectile.Center = Projectile.GetOwner().MountedCenter + new Vector2(Projectile.localAI[0], 0).RotatedBy(Projectile.ai[2] + Main.GameUpdateCount * 0.06f * (Projectile.ai[1] == 720 ? -1 : 1));
                if (Projectile.GetOwner().itemTime == 0) {
                    if (Projectile.localAI[0] > 0.9f) {
                        Projectile.ai[0] = 0;
                        Projectile.velocity = (Projectile.GetOwner().Entropy().MouseWorld - Projectile.Center) * 0.02f;
                    }
                    else {
                        Projectile.Kill();
                    }
                }
                Projectile.rotation = (Projectile.Center - Projectile.GetOwner().Center).RotatedBy(MathHelper.PiOver2 * (Projectile.ai[1] == 720 ? -1 : 1)).ToRotation();
            }

        }
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80 * 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 8;
            Projectile.tileCollide = false;
            Projectile.scale = 2;
        }
        bool init = true;
        public float counter = 0;
        public float rotSpeed;
        public float pg = 0;
        public override bool PreDraw(ref Color lightColor) {
            if (texType == -1) {
                texType = Main.rand.Next(swords.Count);
            }
            if (swords.Count == 0) {
                swords = new List<int>(){
                    ModContent.ItemType<FinalFractal>(),
                ModContent.ItemType<SpiritFractal>(),
                ModContent.ItemType<StarlitFractal>(),
                ModContent.ItemType<VoidFractal>(),
                ModContent.ItemType<AbyssFractal>(),
                ModContent.ItemType<ShatteredFractal>(),
                ModContent.ItemType<BrilliantFractal>(),
                ModContent.ItemType<WelkinFractal>(),
                ModContent.ItemType<ElementalFractal>(),
                ModContent.ItemType<BrokenHilt>(),
                ItemID.StarWrath,
                ItemID.Meowmere,
                ItemID.InfluxWaver,
                ModContent.ItemType<BrokenHilt>(),
                ModContent.ItemType<Voidshade>(),
                ItemID.Zenith,
                ItemID.TerraBlade,
                ItemID.BeamSword,
                ItemID.EnchantedSword,
                ItemID.Starfury,
                ItemID.TrueExcalibur,
                ItemID.TrueNightsEdge,
                ItemID.BladeofGrass,
                ItemID.IceBlade,
                3063
                }
                ;
            }
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++) {
                float prog = ((float)i / ProjectileID.Sets.TrailCacheLength[Type]);
                Color clr = Color.White * 0.36f * (1 - prog);
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i], (int)Projectile.ai[1]);
            }

            Main.spriteBatch.ExitShaderRegion();
            Draw(Projectile.Center, Color.White * 0.8f, Projectile.rotation, (int)Projectile.ai[1]);
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CEUtils.PlaySound("runesonghit", Main.rand.NextFloat(0.6f, 1.4f), target.Center, volume: CEUtils.WeapSound);
        }
        public int texType = -1;
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Fractal/FinalFractalGlow";
        public void Draw(Vector2 pos, Color lightColor, float rotation, int dir) {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            Main.instance.LoadItem(swords[texType]);
            var tex = TextureAssets.Item[swords[texType]].Value;
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, lightColor, rot, tex.Size() * 0.5f, Projectile.scale, effect);
        }
    }
}
