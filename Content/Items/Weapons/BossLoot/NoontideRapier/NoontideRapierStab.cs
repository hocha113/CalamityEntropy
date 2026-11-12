using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.NoontideRapier
{
    //按住期间常驻,照原版星光 927 的写法;ai[0] 8 帧刺击循环,ai[1] 刺击缩放(主人端算好再同步)
    public class NoontideRapierStab : ModProjectile
    {
        //帧、px、弧度
        public const float SizeMult = 1.25f;            //策划,星光的 1.25 倍
        public const float MaxSpeedScale = 2f;          //策划,攻速最多让大小翻倍
        public const int HitCooldown = 4;               //策划,局部无敌帧
        //以下照抄原版 927 的 AI 与绘制
        public const int CycleFrames = 8;
        public const int HoldFrames = 9;
        public const float ArmJitter = 0.1f * MathHelper.Pi;
        public const int BoxSize = 40;
        public const float ReachBoxes = 5f;
        public const int ReachSamples = 20;
        public const float CutLength = 220f;
        public const float CutWidth = 80f;
        public const float LightLength = 80f;
        public const float LightWidth = 16f;
        public const int SoundFrames = 6;
        public const float DrawBack = 2f;
        public const float SpriteNear = 8f;
        public const float SpriteFar = 20f;
        public const float SpritePush = 6f;
        public const float SpriteJitter = 8f;
        public const float SpriteSpread = 0.04f;
        public const float StreakPerScale = 3f;
        public const float StreakBaseLength = 2.8f;
        public const float StreakNear = 30f;
        public const float StreakFar = 50f;
        public const float StreakLengthPush = 16f;
        public const float StreakJitter = 20f;
        public const float StreakLead = 56f;
        public const float StreakSpread = 0.03f;
        public const float StreakSpreadStep = 0.012f;
        public const float StreakMinThickness = 0.6f;
        public const float EnvelopeRise = 0.3f;
        public const float EnvelopeFall = 0.5f;
        public const float CoreOpacity = 0.5f;
        public const float CoreShrink = 0.6f;
        //以下本路自定,火花参数取自 TwinDragonsPearl
        public const float ScaleResync = 0.02f;         //缩放变化超过它才同步
        public const float StabVolume = 0.55f;
        public const float StabPitchVariance = 0.15f;
        public const int StabMaxSounds = 3;
        public const int VfxInterval = 4;               //各端扫命中火花的间隔,与无敌帧一致
        public const int HitSparkCount = 2;
        public const float HitSparkSpread = 0.5f;
        public const float HitSparkMinSpeed = 4f;
        public const float HitSparkMaxSpeed = 11f;
        public const float HitSparkScale = 0.05f;
        public const int HitSparkLife = 14;
        public const float HitFlashScale = 0.3f;
        public const float HitFlashOpacity = 0.9f;
        public const int HitFlashLife = 10;
        public const float SpriteSelfLight = 0.6f;      //剑身半自发光,暗处也看得清
        public const float EdgeOpacity = 0.6f;          //原版彩色层 0.5,金色偏淡略提

        public static readonly Color EdgeGold = new Color(255, 206, 72, 64);
        public static readonly Color EdgeOrange = new Color(255, 122, 24, 64);
        public static readonly Color CoreColor = new Color(255, 250, 222, 128);
        public static readonly Color SparkGold = new Color(255, 220, 120);
        public static readonly Color FlashGold = new Color(255, 170, 50);
        public static readonly Vector3 LightColor = new Vector3(0.75f, 0.55f, 0.2f);
        public static readonly Vector2 SparkSquash = new Vector2(0.4f, 1f);

        private int vfxTimer;
        private int boltSide = 1;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PiercingStarlight;

        public override void SetDefaults() {
            Projectile.width = BoxSize;
            Projectile.height = BoxSize;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
        }

        public static float GetStabScale(Player player, Item item) {
            float speed = MathHelper.Clamp(player.GetTotalAttackSpeed(DamageClass.Melee), 1f, MaxSpeedScale);
            return SizeMult * player.GetAdjustedItemScale(item) * speed;
        }

        private static float Envelope(float r) => Utils.GetLerpValue(0f, EnvelopeRise, r, true) * Utils.GetLerpValue(1f, EnvelopeFall, r, true);

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            Projectile.ai[0] = (Projectile.ai[0] + 1f) % CycleFrames;
            if (Main.myPlayer == Projectile.owner) {
                if (!owner.channel || owner.noItems || owner.CCed) {
                    Projectile.Kill();
                    return;
                }
                float scale = GetStabScale(owner, owner.HeldItem);
                if (Math.Abs(scale - Projectile.ai[1]) > ScaleResync) {
                    Projectile.ai[1] = scale;
                    Projectile.netUpdate = true;
                }
            }
            Projectile.scale = Projectile.ai[1] > 0f ? Projectile.ai[1] : SizeMult;

            Vector2 aimFrom = owner.RotatedRelativePoint(owner.MountedCenter);
            Vector2 dir = (owner.mouseWorld() - aimFrom).SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.velocity = dir * NoontideRapier.AimSpeed;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter, false, false);
            Projectile.rotation = dir.ToRotation();
            Projectile.direction = Projectile.spriteDirection = dir.X >= 0f ? 1 : -1;
            Projectile.timeLeft = 2;
            owner.ChangeDir(Projectile.direction);
            owner.heldProj = Projectile.whoAmI;
            owner.SetDummyItemTime(HoldFrames);
            owner.itemRotation = MathHelper.WrapAngle((float)Math.Atan2(dir.Y * Projectile.direction, dir.X * Projectile.direction) + Main.rand.NextFloatDirection() * ArmJitter);
            owner.itemAnimation = HoldFrames - (int)Projectile.ai[0];

            if (Main.myPlayer == Projectile.owner && owner.GetModPlayer<NoontideRapierPlayer>().TryFireBolt())
                FireBolt(owner, dir);

            DelegateMethods.v3_1 = LightColor;
            Utils.PlotTileLine(Projectile.Center - Projectile.velocity, Projectile.Center + dir * LightLength * Projectile.scale, LightWidth, DelegateMethods.CastLightOpen);

            if (--Projectile.soundDelay <= 0) {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = StabVolume * CEUtils.WeapSound, PitchVariance = StabPitchVariance, MaxInstances = StabMaxSounds }, Projectile.Center);
                Projectile.soundDelay = SoundFrames;
            }
            if (!Main.dedServ && ++vfxTimer >= VfxInterval) {
                vfxTimer = 0;
                HitSparks(dir);
            }
        }

        private void FireBolt(Player owner, Vector2 dir) {
            Vector2 mouse = owner.mouseWorld();
            NPC target = CEUtils.FindTarget_HomingProj(Projectile, mouse, NoontideRapierBolt.SeekRadius);
            boltSide = -boltSide;
            float spread = boltSide * Main.rand.NextFloat(NoontideRapierBolt.SpreadMin, NoontideRapierBolt.SpreadMax);
            Vector2 velocity = dir.RotatedBy(spread) * NoontideRapierBolt.LaunchSpeed;
            int damage = (int)(Projectile.damage * NoontideRapierBolt.DamageMult);
            float knockback = Projectile.knockBack * NoontideRapierBolt.KnockbackMult;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + dir * NoontideRapierBolt.SpawnOffset, velocity, ModContent.ProjectileType<NoontideRapierBolt>(), damage, knockback, Projectile.owner, target?.whoAmI ?? -1, mouse.X, mouse.Y);
        }

        private bool StabHits(Rectangle box, Rectangle target) {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
            int grow = (int)(BoxSize * (Projectile.scale - 1f) / 2f);
            box.Inflate(grow, grow);
            for (int i = 0; i <= ReachSamples; i++) {
                Vector2 step = dir * BoxSize * MathHelper.Lerp(1f, ReachBoxes, i / (float)ReachSamples) * Projectile.scale;
                Rectangle r = box;
                r.Offset((int)step.X, (int)step.Y);
                if (r.Intersects(target))
                    return true;
            }
            return false;
        }

        //各端自己扫一遍,队友也看得到命中火花;判定框要加上本帧移动后的位移才与 Colliding 对齐
        private void HitSparks(Vector2 dir) {
            Rectangle box = Projectile.Hitbox;
            box.Offset((int)Projectile.velocity.X, (int)Projectile.velocity.Y);
            Vector2 start = box.Center.ToVector2();
            float reach = BoxSize * ReachBoxes * Projectile.scale;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.friendly || npc.dontTakeDamage || !StabHits(box, npc.Hitbox))
                    continue;
                float along = MathHelper.Clamp(Vector2.Dot(npc.Center - start, dir), 0f, reach);
                Vector2 at = npc.Hitbox.ClosestPointInRect(start + dir * along);
                for (int i = 0; i < HitSparkCount; i++) {
                    Vector2 vel = dir.RotatedByRandom(HitSparkSpread) * Main.rand.NextFloat(HitSparkMinSpeed, HitSparkMaxSpeed);
                    PRTLoader.NewParticle<PRT_GlowSparkCal>(at, vel, SparkGold, HitSparkScale).Configure(false, HitSparkLife, SparkSquash, true);
                }
                PRTLoader.NewParticle<PRT_ShineParticle>(at, Vector2.Zero, FlashGold, HitFlashScale).Configure(HitFlashOpacity, true, PRTDrawModeEnum.AdditiveBlend, 0f, HitFlashLife);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => StabHits(projHitbox, targetHitbox);

        public override void CutTiles() {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + dir * CutLength * Projectile.scale, CutWidth * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.HitDirectionOverride = Projectile.direction;
        }

        public override bool PreDraw(ref Color lightColor) {
            float s = Projectile.scale;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 basePos = Projectile.Center - dir * DrawBack - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            DrawRapier(basePos, lightColor);

            Texture2D streak = Projectile.GetTexture();
            Vector2 origin = streak.Size() / 2f;
            int count = (int)Math.Ceiling(StreakPerScale * s);
            float bonus = (s - 1f) / 2f;
            for (int i = 0; i < count; i++) {
                float r = Main.rand.NextFloat();
                float env = Envelope(r);
                float length = Main.rand.NextFloat() * 2f * s + bonus;
                Vector2 size = new Vector2(StreakBaseLength + length * (1f + bonus), 1f) * MathHelper.Lerp(StreakMinThickness, 1f, env);
                float angle = Projectile.rotation + Main.rand.NextFloatDirection() * MathHelper.TwoPi * (StreakSpread - i * StreakSpreadStep) / s;
                float dist = StreakNear + MathHelper.Lerp(0f, StreakFar * s, r) + length * StreakLengthPush;
                Vector2 pos = basePos + angle.ToRotationVector2() * dist + Main.rand.NextVector2Circular(StreakJitter, StreakJitter) + dir * (i >= 1 ? StreakLead : 0f);
                Color edge = Color.Lerp(EdgeOrange, EdgeGold, Main.rand.NextFloat()) * (env * EdgeOpacity);
                Main.EntitySpriteDraw(streak, pos, null, edge, angle, origin, size, SpriteEffects.None);
                Main.EntitySpriteDraw(streak, pos, null, CoreColor * (env * CoreOpacity), angle, origin, size * CoreShrink, SpriteEffects.None);
            }
            return false;
        }

        private void DrawRapier(Vector2 basePos, Color lightColor) {
            Texture2D tex = TextureAssets.Item[ModContent.ItemType<NoontideRapier>()].Value;
            float r = Main.rand.NextFloat();
            float angle = Projectile.rotation + Main.rand.NextFloatDirection() * MathHelper.TwoPi * SpriteSpread;
            float dist = SpriteNear + MathHelper.Lerp(0f, SpriteFar, r) + Main.rand.NextFloat() * SpritePush;
            Vector2 pos = basePos + angle.ToRotationVector2() * dist + Main.rand.NextVector2Circular(SpriteJitter, SpriteJitter);
            float rot = angle + MathHelper.PiOver4;
            SpriteEffects fx = SpriteEffects.None;
            if (Projectile.rotation < -MathHelper.PiOver2 || Projectile.rotation > MathHelper.PiOver2) {
                rot += MathHelper.PiOver2;
                fx = SpriteEffects.FlipHorizontally;
            }
            Color c = Color.Lerp(lightColor, Color.White, SpriteSelfLight) * Envelope(r);
            Main.EntitySpriteDraw(tex, pos, null, c, rot, tex.Size() / 2f, 1f, fx);
        }
    }
}
