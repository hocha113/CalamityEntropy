using CalamityEntropy.Common;
using CalamityEntropy.Content.Tiles;
using CalamityEntropy.Content.Buffs.PortsDoT;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Rarities;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityEntropy.Core.CalamityRef;

namespace CalamityEntropy.Content.Items.Books
{
    public class ControlTerminal : EntropyBook
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 165;
            Item.useAnimation = Item.useTime = 100;
            Item.crit = 10;
            Item.mana = 30;
            Item.shootSpeed = 45;
            Item.rare = CECal.RarityExoticRainbow(ModContent.RarityType<Golden>());
            Item.value = Item.buyPrice(platinum: 2, gold: 40);
        }
        [VaultLoaden("CalamityEntropy/Content/UI/EntropyBookUI/BookMark7")]
        internal static Asset<Texture2D> BookMarkSlotTex;
        public override Texture2D BookMarkTexture => BookMarkSlotTex.Value;
        public override int HeldProjectileType => ModContent.ProjectileType<ControlTerminalHeld>();
        public override int SlotCount => 5;

        public override void AddRecipes()
        {
            if (CECal.CalChainReady(CEID.Item_ExoPrism, CEID.Tile_DraedonsForge))
            {
                CreateRecipe().AddIngredient<ProphecyMasterpiece>()
                .AddIngredient(CEID.Item_ExoPrism, 5)
                .AddTile(CEID.Tile_DraedonsForge)
                .Register();
                return;
            }
            CreateRecipe().AddIngredient<CosmicBlessing>()
                .AddIngredient<VoidBar>(5)
                .AddTile(ModContent.TileType<VoidWellTile>())
                .Register();
        }
    }

    public class ControlTerminalHeld : EntropyBookHeldProjectile
    {
        public override string OpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/ControlTerminal/ControlTerminalOpen";
        public override string PageAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/ControlTerminal/ControlTerminalPage";
        public override string UIOpenAnimationPath => "CalamityEntropy/Content/Items/Books/Textures/ControlTerminal/ControlTerminalUI";

        public override EBookStatModifer getBaseModifer()
        {
            var m = base.getBaseModifer();
            m.Homing += 1f;
            m.HomingRange += 1f;
            return m;
        }
        public override float randomShootRotMax => 0;
        public override int frameChange => 2;
        public override int baseProjectileType => ModContent.ProjectileType<WhirlExobeam>();
        public override EBookProjectileEffect getEffect()
        {
            return new ControlTerminalBookBaseEffect();
        }
        public override bool Shoot()
        {
            int type = ModContent.ProjectileType<ExoWhirl>();
            ShootSingleProjectile(type, Projectile.Center, Projectile.velocity, MainProjectile: true);
            return true;
        }
        public override bool canApplyShootCDModifer => false;
        public int getShootCd()
        {
            int _shotCooldown = bookItem.useTime;

            EBookStatModifer m = getBaseModifer();
            for (int i = 0; i < Projectile.GetOwner().GetMyMaxActiveBookMarks(bookItem); i++)
            {
                Item it = Projectile.GetOwner().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(it))
                {
                    BookMarkLoader.ModifyStat(it, m);
                    BookMarkLoader.modifyShootCooldown(it, ref _shotCooldown);
                }
            }
            return (int)(0.6f * (float)_shotCooldown / m.attackSpeed);
        }
        public void shootBaseProj(Vector2 pos, Vector2 vel)
        {
            int type = getShootProjectileType();

            for (int i = 0; i < Projectile.GetOwner().GetMyMaxActiveBookMarks(bookItem); i++)
            {
                var bm = Projectile.owner.ToPlayer().Entropy().EBookStackItems[i];
                if (BookMarkLoader.IsABookMark(bm))
                {
                    int pn = BookMarkLoader.ModifyProjectile(bm, type);
                    if (pn >= 0)
                    {
                        type = pn;
                    }
                }
            }
            ShootSingleProjectile(type, pos, vel, shotSpeedMul: 0.6f, MainProjectile: true);
        }
    }
    public class ControlTerminalBookBaseEffect : EBookProjectileEffect
    {
        public override void OnProjectileSpawn(Projectile projectile, bool ownerClient)
        {
            base.OnProjectileSpawn(projectile, ownerClient);
            projectile.tileCollide = false;
            if (projectile.ModProjectile is EBookBaseProjectile e)
            {
                e.gravity = 0;
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 60, false);
        }
    }

    public class ExoWhirl : EBookBaseProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[base.Projectile.type] = 18;
            ProjectileID.Sets.TrailingMode[base.Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = Projectile.height = 80;
            Projectile.extraUpdates = 1;
            Projectile.localNPCHitCooldown = 3;
            Projectile.light = 1;
        }
        public override bool PreAI()
        {
            Projectile.penetrate = -1;
            return base.PreAI();
        }
        public float r = 0;
        public int shootCd = 9;
        public bool ri = true;
        public override void AI()
        {
            if (ri)
            {
                ri = false;
                r = Projectile.velocity.ToRotation();
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Projectile.oldPos[i] = Projectile.Center;
                    Projectile.oldRot[i] = Projectile.rotation;
                }
            }
            base.AI();
            shootCd--;
            if (shootCd <= 0)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    var book = (ControlTerminalHeld)(ShooterModProjectile);
                    NPC target = null;

                    target = Projectile.FindTargetWithinRange(homingRange * 3);

                    if (target != null)
                    {
                        book.shootBaseProj(Projectile.Center, target.Center - Projectile.Center);
                    }
                    shootCd = book.getShootCd();
                }

            }
            if (++Projectile.localAI[0] > 18 && hitCount == 0)
            {
                hitCount++;
                Projectile.ai[0] = 120;
            }
            if (Projectile.ai[0] > 0)
            {
                if (Projectile.velocity.Length() > 6)
                {
                    Projectile.velocity = Projectile.velocity.normalize() * 6;
                }
            }
            Projectile.ai[0]--;
            if (hitCount == 0)
            {
                r = Projectile.velocity.ToRotation();
            }
            else
            {
                if (Projectile.ai[0] < 10)
                {
                    if (Projectile.ai[2] < 16)
                    {
                        Projectile.ai[2] += 0.15f;
                    }
                    r = Projectile.velocity.ToRotation();
                    Projectile.velocity = new Vector2(Projectile.velocity.Length() + 1f, 0).RotatedBy(CEUtils.RotateTowardsAngle(r, (Projectile.GetOwner().Center - Projectile.Center).ToRotation(), 0.5f * Projectile.ai[2], false));
                    Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0).RotatedBy(CEUtils.RotateTowardsAngle(r, (Projectile.GetOwner().Center - Projectile.Center).ToRotation(), (1f * Projectile.ai[2]).ToRadians(), true));
                    Projectile.velocity *= 0.97f;
                    if (CEUtils.getDistance(Projectile.Center, Projectile.GetOwner().Center) < Projectile.velocity.Length() * 1.2f)
                    {
                        Projectile.Kill();
                    }
                }
            }
            Projectile.rotation = Main.GameUpdateCount * 0.4f;
            if (Projectile.velocity.Length() < 2)
            {
                Projectile.velocity = r.ToRotationVector2() * 2;
            }
            NoMoveTime--;
        }
        public int NoMoveTime = 0;
        public override bool ShouldUpdatePosition()
        {
            return NoMoveTime <= 0;
        }
        public float TrailWidth(float completionRatio, Vector2 vertex)
        {
            return Utils.GetLerpValue(1f, 0.4f, completionRatio, clamped: true) * (float)Math.Sin(Math.Acos(1f - Utils.GetLerpValue(0f, 0.15f, completionRatio, clamped: true))) * Utils.GetLerpValue(0f, 0.1f, (float)base.Projectile.timeLeft / 600f, clamped: true) * 6;
        }
        public Color TrailColor(float completionRatio, Vector2 vertex)
        {
            return Color.Lerp(Color.Cyan, new Color(0, 0, 255), completionRatio);
        }

        public float MiniTrailWidth(float completionRatio, Vector2 vertex)
        {
            return TrailWidth(completionRatio, vertex) * 0.8f;
        }

        public Color MiniTrailColor(float completionRatio, Vector2 vertex)
        {
            return Color.White;
        }

        public override void ApplyHoming()
        {
            if (hitCount == 0 && ++Projectile.ai[1] > 10)
            {
                base.ApplyHoming();
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/SwiftSlice") with { Pitch = 0.1f * Projectile.numHits, MaxInstances = 12 }, Projectile.Center);
            if (hitCount == 1)
            {
                Projectile.ai[0] = 120;
            }
            if (Projectile.ai[0] <= 0)
            {
                NoMoveTime = 4;
            }
            if (Projectile.ai[0] < 6)
            {
                Projectile.ai[0] = 6;
            }
            for (int i = 0; i < 4; i++)
            {
                //PRT_GlowSpark AdditiveBlend走Configure,旧EParticle统一尾参
                PRTLoader.NewParticle<PRT_GlowSpark>(target.Center, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(5, 10), Color.LightGreen, Main.rand.NextFloat(0.04f, 0.08f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            for (int i = 0; i < 4; i++)
            {
                float ra = i * MathHelper.PiOver2;
                Main.spriteBatch.EnterShaderRegion();
                Color color1 = CEUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + (float)base.Projectile.whoAmI * 0.12f) % 1f, Color.Cyan, Color.Lime, Color.GreenYellow, Color.Goldenrod, Color.Orange);
                Color color2 = CEUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + (float)base.Projectile.whoAmI * 0.12f + 0.2f) % 1f, Color.Cyan, Color.Lime, Color.GreenYellow, Color.Goldenrod, Color.Orange);

                //拖尾贴图改走 PRTSharedAssets 共享字段
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].SetShaderTexture(PRTSharedAssets.BasicTrail);
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseImage2("Images/Extra_189");
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseColor(color1);
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseSecondaryColor(color2);
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].Apply();
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].Apply();
                Vector2[] tpos = new Vector2[ProjectileID.Sets.TrailCacheLength[Type]];
                for (int k = 0; k < ProjectileID.Sets.TrailCacheLength[Type]; k++)
                {
                    tpos[k] = Projectile.oldPos[k] + (Projectile.oldRot[k] + ra).ToRotationVector2() * 37 * Projectile.scale;
                }
                CEPrimitiveRenderer.RenderTrail(tpos, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => base.Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ExobladePierce"]), 30);
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseColor(Color.White);
                GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseSecondaryColor(Color.White);
                CEPrimitiveRenderer.RenderTrail(tpos, new CEPrimitiveSettings(MiniTrailWidth, MiniTrailColor, (_, _) => base.Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ExobladePierce"]), 30);
                Main.spriteBatch.ExitShaderRegion();
            }
            Main.EntitySpriteDraw(Projectile.GetTexture(), Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, Projectile.GetTexture().Size() / 2f, Projectile.scale, SpriteEffects.None); ;
            return false;
        }
    }
    public class WhirlExobeam : EBookBaseProjectile
    {
        public int TargetIndex = -1;

        public static float MaxWidth = 30f;

        public static Asset<Texture2D> SlashTex;


        public ref float Time => ref base.Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[base.Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[base.Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            base.Projectile.width = 20;
            base.Projectile.height = 20;
            base.Projectile.friendly = true;
            base.Projectile.DamageType = DamageClass.Magic;
            base.Projectile.ignoreWater = true;
            base.Projectile.tileCollide = false;
            base.Projectile.extraUpdates = 1;
            base.Projectile.alpha = 255;
            base.Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        public override void AI()
        {
            base.AI();
            if (Time >= (float)10)
            {
                if (TargetIndex >= 0)
                {
                    if (!Main.npc[TargetIndex].active || !Main.npc[TargetIndex].CanBeChasedBy())
                    {
                        TargetIndex = -1;
                    }
                    else
                    {
                        Vector2 value = base.Projectile.SafeDirectionTo(Main.npc[TargetIndex].Center) * (base.Projectile.velocity.Length() + 6.5f);
                        base.Projectile.velocity = Vector2.Lerp(base.Projectile.velocity, value, 0.08f);
                    }
                }

                if (TargetIndex == -1)
                {
                    NPC nPC = base.Projectile.Center.ClosestNPCAt(1600f, ignoreTiles: false);
                    if (nPC != null)
                    {
                        TargetIndex = nPC.whoAmI;
                    }
                    else
                    {
                        base.Projectile.velocity *= 0.99f;
                    }
                }
            }

            base.Projectile.rotation = base.Projectile.velocity.ToRotation();
            if (Main.rand.NextBool())
            {
                Color newColor = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.9f);
                Dust dust = Dust.NewDustPerfect(base.Projectile.Center + Main.rand.NextVector2Circular(20f, 20f) + base.Projectile.velocity, 267, base.Projectile.velocity * -2.6f, 0, newColor);
                dust.scale = 0.3f;
                dust.fadeIn = Main.rand.NextFloat() * 1.2f;
                dust.noGravity = true;
            }

            base.Projectile.scale = Utils.GetLerpValue(0f, 0.1f, (float)base.Projectile.timeLeft / 600f, clamped: true);
            if (base.Projectile.FinalExtraUpdate())
            {
                Time += 1f;
            }
        }

        public static readonly SoundStyle BeamHitSound = new SoundStyle("CalamityEntropy/Assets/Sounds/SwiftSlice") { Volume = 0.4f, PitchVariance = 0.2f };
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(in BeamHitSound, target.Center);
            if (Main.myPlayer == base.Projectile.owner)
            {
                int num = Projectile.NewProjectile(base.Projectile.GetSource_FromAI(), target.Center, base.Projectile.velocity * 0.1f, ModContent.ProjectileType<ExobeamSlashBurst>(), base.Projectile.damage, 0f, base.Projectile.owner, target.whoAmI, base.Projectile.velocity.ToRotation());
                if (Main.projectile.IndexInRange(num))
                {
                    Main.projectile[num].timeLeft = 20;
                }
            }

            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300);
            base.OnHitNPC(target, hit, damageDone);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color white = Color.White;
            white.A = 0;
            return white * base.Projectile.Opacity;
        }

        public float TrailWidth(float completionRatio, Vector2 vertex)
        {
            return Utils.GetLerpValue(1f, 0.4f, completionRatio, clamped: true) * (float)Math.Sin(Math.Acos(1f - Utils.GetLerpValue(0f, 0.15f, completionRatio, clamped: true))) * Utils.GetLerpValue(0f, 0.1f, (float)base.Projectile.timeLeft / 600f, clamped: true) * MaxWidth;
        }

        public Color TrailColor(float completionRatio, Vector2 vertex)
        {
            return Color.Lerp(Color.Cyan, new Color(0, 0, 255), completionRatio);
        }

        public float MiniTrailWidth(float completionRatio, Vector2 vertex)
        {
            return TrailWidth(completionRatio, vertex) * 0.8f;
        }

        public Color MiniTrailColor(float completionRatio, Vector2 vertex)
        {
            return Color.White;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (base.Projectile.timeLeft > 595)
            {
                return false;
            }

            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            float num = Utils.GetLerpValue(3f, 13f, base.Projectile.velocity.Length(), clamped: true) * 1.2f;
            Vector2 position = base.Projectile.oldPos[2] + base.Projectile.Size / 2f - Main.screenPosition;
            Color white = Color.White;
            white.A = 0;
            Main.EntitySpriteDraw(value, position, null, white, base.Projectile.rotation + MathF.PI / 4f, value.Size() / 2f, num * base.Projectile.scale, SpriteEffects.None);
            //光晕贴图改走 PRTSharedAssets 共享字段
            Texture2D value2 = PRTSharedAssets.BloomCircle.Value;
            Color color = CEUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + (float)base.Projectile.whoAmI * 0.12f) % 1f, Color.Cyan, Color.Lime, Color.GreenYellow, Color.Goldenrod, Color.Orange);
            Color color2 = CEUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.5f + (float)base.Projectile.whoAmI * 0.12f + 0.2f) % 1f, Color.Cyan, Color.Lime, Color.GreenYellow, Color.Goldenrod, Color.Orange);
            Vector2 position2 = base.Projectile.oldPos[2] + base.Projectile.Size / 2f - Main.screenPosition;
            white = color * 0.1f;
            white.A = 0;
            Main.EntitySpriteDraw(value2, position2, null, white, 0f, value2.Size() / 2f, 1.3f * base.Projectile.scale, SpriteEffects.None);
            Vector2 position3 = base.Projectile.oldPos[1] + base.Projectile.Size / 2f - Main.screenPosition;
            white = color * 0.5f;
            white.A = 0;
            Main.EntitySpriteDraw(value2, position3, null, white, 0f, value2.Size() / 2f, 0.34f * base.Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.EnterShaderRegion();
            //拖尾贴图改走 PRTSharedAssets 共享字段
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].SetShaderTexture(PRTSharedAssets.BasicTrail);
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseColor(color);
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseSecondaryColor(color2);
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].Apply();
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].Apply();
            CEPrimitiveRenderer.RenderTrail(base.Projectile.oldPos, new CEPrimitiveSettings(TrailWidth, TrailColor, (_, _) => base.Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ExobladePierce"]), 30);
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseColor(Color.White);
            GameShaders.Misc["CalamityEntropy:ExobladePierce"].UseSecondaryColor(Color.White);
            CEPrimitiveRenderer.RenderTrail(base.Projectile.oldPos, new CEPrimitiveSettings(MiniTrailWidth, MiniTrailColor, (_, _) => base.Projectile.Size * 0.5f, smoothen: true, pixelate: false, GameShaders.Misc["CalamityEntropy:ExobladePierce"]), 30);
            Main.spriteBatch.ExitShaderRegion();
            Vector2 position4 = base.Projectile.oldPos[2] + base.Projectile.Size / 2f - Main.screenPosition;
            white = Color.White * 0.2f;
            white.A = 0;
            Main.EntitySpriteDraw(value2, position4, null, white, 0f, value2.Size() / 2f, 0.78f * base.Projectile.scale, SpriteEffects.None);
            Vector2 position5 = base.Projectile.oldPos[1] + base.Projectile.Size / 2f - Main.screenPosition;
            white = Color.White * 0.5f;
            white.A = 0;
            Main.EntitySpriteDraw(value2, position5, null, white, 0f, value2.Size() / 2f, 0.2f * base.Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    // 原灾厄 ExobeamSlashCreator 的自有等效: 在目标周围分两波引发能量斩爆
    public class ExobeamSlashBurst : ModProjectile
    {
        public NPC Target => Main.npc[(int)Projectile.ai[0]];
        public float SlashDirection
        {
            get
            {
                if (Projectile.ai[1] > MathHelper.Pi)
                    return Main.rand.NextFloatDirection();
                return Projectile.ai[1] + Main.rand.NextFloatDirection() * 0.2f;
            }
        }

        public override string Texture => "CalamityEntropy/Assets/Extra/Ports/Invisible";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.MaxUpdates = 2;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            if (Main.myPlayer == Projectile.owner && Projectile.timeLeft % 20 == 19 && Target.active)
            {
                float maxOffset = Math.Min(Target.width * 0.4f, 300f);
                Vector2 spawnOffset = SlashDirection.ToRotationVector2() * (Main.rand.NextFloatDirection() * maxOffset);
                Vector2 pos = Target.Center + spawnOffset;
                CEUtils.SpawnExplotionFriendly(Projectile.GetSource_FromAI(), Projectile.GetOwner(), pos, Projectile.damage, 80, Projectile.DamageType);
                for (int i = 0; i < 6; i++)
                    PRTLoader.NewParticle<PRT_GlowSpark>(pos, CEUtils.randomRot().ToRotationVector2() * Main.rand.NextFloat(6, 12), Color.LightGreen, Main.rand.NextFloat(0.05f, 0.09f)).Configure(1, true, PRTDrawModeEnum.AdditiveBlend, 0);
            }
        }

        public override bool? CanDamage() => false;
    }
}
