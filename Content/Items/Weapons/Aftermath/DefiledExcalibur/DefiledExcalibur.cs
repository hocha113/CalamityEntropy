using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.DefiledExcalibur
{
    public class DefiledExcalibur : ModItem
    {
        public const float BeamDamageMult = 1.25f;
        public const float OrbDamageMult = 0.66f;
        public const int BeatsPerCycle = 5;           //第五拍旋斩
        public const int ComboResetFrames = 60;       //停手满 1 秒回到第一拍
        public const float SpinTimeMult = 1.4f;
        private const int BaseDamage = 160;
        private const int UseFrames = 20;
        private const float BaseKnockback = 6f;
        private const int SpriteSize = 78;
        private const int SellGold = 24;
        private const int BearingCost = 10;

        public override void SetDefaults() {
            Item.width = SpriteSize;
            Item.height = SpriteSize;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = UseFrames;
            Item.useAnimation = UseFrames;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = BaseKnockback;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<DefiledExcaliburSwing>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: SellGold);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int beat = player.GetModPlayer<DefiledExcaliburPlayer>().NextBeat();
            int facing = velocity.X >= 0f ? 1 : -1;
            float side = beat % 2 == 0 ? facing : -facing;
            float frames = player.itemAnimationMax * (beat == BeatsPerCycle - 1 ? SpinTimeMult : 1f);
            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * facing);
            Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback, player.whoAmI, side, beat, frames);
            return false;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Excalibur)
                .AddIngredient<DimBearing>(BearingCost)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public class DefiledExcaliburPlayer : ModPlayer
    {
        private int nextBeat;
        private int idleFrames;

        public int NextBeat() {
            if (idleFrames >= DefiledExcalibur.ComboResetFrames)
                nextBeat = 0;
            int beat = nextBeat;
            nextBeat = (nextBeat + 1) % DefiledExcalibur.BeatsPerCycle;
            idleFrames = 0;
            return beat;
        }

        public override void PostUpdate() {
            if (Player.itemAnimation > 0)
                idleFrames = 0;
            else if (idleFrames < DefiledExcalibur.ComboResetFrames)
                idleFrames++;
        }
    }

    public class DefiledExcaliburSwing : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/Aftermath/DefiledExcalibur/DefiledExcalibur";

        private const int Substeps = 6;
        private const float NormalArc = 3.8f;                  //弧度
        private const float SpinArc = MathHelper.TwoPi + 0.9f;
        private const float Windback = 0.2f;
        private const float Overshoot = 0.22f;
        private const float NormalSquash = 0.62f;              //椭圆短轴比
        private const float SpinSquash = 0.8f;
        private const float WindupEnd = 0.14f;                 //进度 0-1
        private const float StrikeEnd = 0.62f;
        private const float BladeReach = 125f;                 //握点到剑尖,像素
        private const int HitWidth = 48;
        private const float SpinReachMult = 1.3f;
        private const float DrawScale = 1.4f;
        private const float GripInset = 16f;                   //握点离贴图左下角
        private const float SelfLight = 0.35f;
        private const float TrailInner = 0.28f;                //占剑长
        private const int TrailMax = 64;
        private const float LightAlong = 0.6f;
        private const float BeamSpawnDist = 40f;
        private const float BeamSpeed = 9f;
        private const float OrbSpeed = 7.5f;
        private static readonly float[] OrbSpawnAt = { 0.30f, 0.38f, 0.46f };    //近到远
        private static readonly float[] OrbSideDist = { 56f, 92f, 128f };        //沿法线离玩家
        private const float PitchMin = 0.92f;
        private const float PitchMax = 1.08f;
        private const float SwingVolume = 0.7f;
        private const float SpinVolume = 0.85f;
        private const float HitVolume = 0.6f;
        private const int SoundInstances = 4;
        private const float OrbPitchBase = 0.1f;               //SoundStyle 偏移
        private const float OrbPitchStep = 0.18f;
        private const float OrbVolume = 0.45f;
        private const float SpinShake = 3f;
        private const float ShakeRange = 1600f;
        private const int HitSparks = 10;
        private const float SparkSpread = 0.45f;
        private const float SparkSpeedMin = 4f;
        private const float SparkSpeedMax = 13f;
        private const float SparkScale = 0.045f;
        private const int SparkLife = 14;
        private const int TipSparkEvery = 3;                   //子步
        private const float TipSparkSpeed = 2.5f;
        private const float TipSparkScale = 0.03f;
        private const int TipSparkLife = 10;
        private const float CutRingStart = 0.1f;
        private const float CutRingEnd = 1.1f;
        private const int CutRingLife = 22;
        private static readonly Vector2 SparkSquash = new(0.7f, 1f);
        private static readonly Vector2 CutRingSquish = new(0.22f, 1f);
        private static readonly Color TrailDark = new(112, 62, 170);
        private static readonly Color TrailLight = new(222, 198, 255);
        private static readonly Color TrailVertex = new(235, 225, 255);
        private static readonly Color SparkPurple = new(190, 150, 255);
        private static readonly Color SparkGold = new(255, 214, 96);
        private static readonly Vector3 BladeLight = new(0.42f, 0.24f, 0.66f);

        private float Side => Projectile.ai[0];                //+1 角度递增,-1 递减
        private int Beat => (int)Projectile.ai[1];             //0-4,4 为旋斩
        private float Frames => Projectile.ai[2];              //本拍帧数
        private bool IsSpin => Beat == DefiledExcalibur.BeatsPerCycle - 1;
        private float ReachMult => IsSpin ? SpinReachMult : 1f;
        private bool InStrike => progress >= WindupEnd && progress <= StrikeEnd;

        private int counter;
        private float progress;
        private float rScale = 1f;
        private bool beamFired;
        private int orbsFired;
        private bool hitSounded;
        private readonly List<float> trailRot = new();
        private readonly List<float> trailLen = new();
        private readonly bool[] fxShown = new bool[Main.maxNPCs];  //远端命中特效去重

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = Substeps;
            Projectile.timeLeft = Substeps;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }
            if (counter == 0)
                Begin(owner);
            float total = Math.Max(Frames, 1f) * Substeps;
            counter++;
            progress = Math.Min(counter / total, 1f);

            Vector2 ellipse = SwingAngle(progress).ToRotationVector2() * new Vector2(1f, IsSpin ? SpinSquash : NormalSquash);
            Projectile.rotation = Projectile.velocity.ToRotation() + ellipse.ToRotation();
            rScale = ellipse.Length();
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter);
            Projectile.timeLeft = Substeps;

            owner.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            Lighting.AddLight(Projectile.Center + Projectile.rotation.ToRotationVector2() * BladeLength() * LightAlong, BladeLight);

            UpdateTrail();
            if (InStrike) {
                if (counter % TipSparkEvery == 0)
                    TipSpark();
                if (IsSpin)
                    FireOrbs(owner);
                else
                    FireBeam();
                if (Projectile.owner != Main.myPlayer)
                    RemoteHitFx();
            }
            if (counter >= total) {
                owner.itemTime = 1;
                owner.itemAnimation = 1;
                Projectile.Kill();
            }
        }

        private void Begin(Player owner) {
            Projectile.scale = owner.GetAdjustedItemScale(owner.HeldItem);
            string sound = IsSpin ? "sword_spin1" : Beat % 2 == 0 ? "DemonSwordSwing1" : "DemonSwordSwing2";
            CEUtils.PlaySound(sound, Main.rand.NextFloat(PitchMin, PitchMax), owner.Center, SoundInstances, CEUtils.WeapSound * (IsSpin ? SpinVolume : SwingVolume));
        }

        private float SwingAngle(float p) {
            float arc = IsSpin ? SpinArc : NormalArc;
            float angle;
            if (p < WindupEnd)
                angle = -arc * 0.5f - Windback * EaseOut(p / WindupEnd);
            else if (p < StrikeEnd)
                angle = -arc * 0.5f - Windback + (arc + Windback + Overshoot) * EaseInOut((p - WindupEnd) / (StrikeEnd - WindupEnd));
            else
                angle = arc * 0.5f + Overshoot * (1f - EaseOut((p - StrikeEnd) / (1f - StrikeEnd)));
            return angle * Side;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - 4f * (1f - t) * (1f - t) * (1f - t);

        private void UpdateTrail() {
            if (InStrike) {
                trailRot.Add(Projectile.rotation);
                trailLen.Add(rScale);
                if (trailRot.Count <= TrailMax)
                    return;
            }
            else if (progress < StrikeEnd || trailRot.Count == 0)
                return;
            trailRot.RemoveAt(0);
            trailLen.RemoveAt(0);
        }

        private void FireBeam() {
            if (beamFired || SwingAngle(progress) * Side < 0f)
                return;
            beamFired = true;
            if (Projectile.owner != Main.myPlayer)
                return;
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + aim * BeamSpawnDist * Projectile.scale, aim * BeamSpeed,
                ModContent.ProjectileType<DefiledExcaliburBeam>(), (int)(Projectile.damage * DefiledExcalibur.BeamDamageMult), Projectile.knockBack, Projectile.owner);
        }

        private void FireOrbs(Player owner) {
            while (orbsFired < OrbSpawnAt.Length && progress >= OrbSpawnAt[orbsFired]) {
                int ring = orbsFired++;
                SoundEngine.PlaySound(SoundID.Item9 with { Pitch = OrbPitchBase + ring * OrbPitchStep, Volume = OrbVolume }, owner.Center);
                if (Projectile.owner != Main.myPlayer)
                    continue;
                Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
                int damage = (int)(Projectile.damage * DefiledExcalibur.OrbDamageMult);
                for (int s = -1; s <= 1; s += 2) {
                    Vector2 pos = owner.Center + normal * (s * OrbSideDist[ring]);
                    Vector2 vel = (Main.MouseWorld - pos).SafeNormalize(aim) * OrbSpeed;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<DefiledExcaliburOrb>(), damage, Projectile.knockBack, Projectile.owner);
                }
            }
        }

        private float BladeLength() => BladeReach * Projectile.scale * rScale * ReachMult;

        private Vector2 BladeTip() => Projectile.Center + Projectile.rotation.ToRotationVector2() * BladeLength();

        private bool BladeHits(Rectangle box) => CEUtils.LineThroughRect(Projectile.Center, BladeTip(), box, (int)(HitWidth * ReachMult));

        public override bool? CanDamage() => InStrike ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => BladeHits(targetHitbox);

        public override void CutTiles() => Utils.PlotTileLine(Projectile.Center, BladeTip(), HitWidth * ReachMult, DelegateMethods.CutTiles);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitFx(target);

        private void RemoteHitFx() {
            if (Main.dedServ)
                return;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (fxShown[npc.whoAmI] || !npc.CanBeChasedBy() || !BladeHits(npc.Hitbox))
                    continue;
                fxShown[npc.whoAmI] = true;
                HitFx(npc);
            }
        }

        private void HitFx(NPC target) {
            if (Main.dedServ)
                return;
            if (!hitSounded) {
                hitSounded = true;
                CEUtils.PlaySound(IsSpin ? "DemonSwordImpact2" : "SwordHit1", Main.rand.NextFloat(PitchMin, PitchMax), target.Center, SoundInstances, CEUtils.WeapSound * HitVolume);
                if (IsSpin)
                    CEUtils.SetShake(target.Center, SpinShake, ShakeRange);
            }
            Vector2 sweep = (Projectile.rotation + Side * MathHelper.PiOver2).ToRotationVector2();
            for (int i = 0; i < HitSparks; i++) {
                Vector2 vel = sweep.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, Main.rand.NextBool() ? SparkGold : SparkPurple, SparkScale).Configure(false, SparkLife, SparkSquash);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(target.Center, Vector2.Zero, SparkPurple, CutRingStart).Configure(CutRingSquish, Projectile.rotation, CutRingEnd, CutRingLife);
        }

        private void TipSpark() {
            if (Main.dedServ)
                return;
            Vector2 vel = (Projectile.rotation + Side * MathHelper.PiOver2).ToRotationVector2() * TipSparkSpeed;
            PRTLoader.NewParticle<PRT_GlowSparkCal>(BladeTip(), vel, Main.rand.NextBool() ? SparkGold : SparkPurple, TipSparkScale).Configure(false, TipSparkLife, SparkSquash);
        }

        public override bool PreDraw(ref Color lightColor) {
            DrawTrail();
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            bool flip = Side < 0f;
            Vector2 origin = new(flip ? tex.Width - GripInset : GripInset, tex.Height - GripInset);
            float rot = Projectile.rotation + (flip ? MathHelper.PiOver4 * 3f : MathHelper.PiOver4);
            float scale = Projectile.scale * DrawScale * rScale * ReachMult;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, SelfLight), rot, origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            return false;
        }

        private void DrawTrail() {
            if (trailRot.Count < 2)
                return;
            float reach = BladeReach * Projectile.scale * ReachMult;
            Vector2 center = Projectile.Center - Main.screenPosition;
            ColoredVertex[] verts = new ColoredVertex[trailRot.Count * 2];
            for (int i = 0; i < trailRot.Count; i++) {
                float u = i / (float)(trailRot.Count - 1);
                Vector2 edge = trailRot[i].ToRotationVector2() * (trailLen[i] * reach);
                verts[i * 2] = new ColoredVertex(center + edge, new Vector3(u, 1f, 1f), TrailVertex);
                verts[i * 2 + 1] = new ColoredVertex(center + edge * TrailInner, new Vector3(u, 0f, 1f), TrailVertex);
            }
            float alpha = progress <= StrikeEnd ? 1f : 1f - (progress - StrikeEnd) / (1f - StrikeEnd);
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Effect shader = CEEffectAssets.SwordTrail;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            shader.Parameters["color1"].SetValue(TrailDark.ToVector4());
            shader.Parameters["color2"].SetValue(TrailLight.ToVector4());
            shader.Parameters["alpha"].SetValue(alpha);
            shader.CurrentTechnique.Passes["EffectPass"].Apply();
            gd.Textures[0] = CEExtraAssets.MotionTrail2;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts, 0, verts.Length - 2);
            gd.Textures[0] = CEExtraAssets.SplitTrail;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts, 0, verts.Length - 2);
            sb.ExitShaderRegion();
        }
    }

    public class DefiledExcaliburBeam : ModProjectile
    {
        private const float MaxTravel = 800f;          //像素
        private const float FadeTravel = 140f;
        private const int SafetyUpdates = 120;
        private const float HalfChord = 42f;           //月牙半弦长
        private const int HitWidth = 26;
        private const int Afterimages = 8;
        private const float AfterimageAlpha = 0.55f;
        private const float AfterimageMinScale = 0.7f;
        private const float GlowAlpha = 0.5f;
        private const float GlowScale = 1.12f;
        private const float PitchMin = 1.25f;
        private const float PitchMax = 1.4f;
        private const float SoundVolume = 0.35f;
        private const int SoundInstances = 4;
        private const int MoteEvery = 4;                //更新次数
        private const float MoteDrift = 0.15f;
        private const float MoteScale = 0.03f;
        private const int MoteLife = 12;
        private const int HitSparks = 6;
        private const float SparkSpread = 0.5f;
        private const float SparkSpeedMin = 3f;
        private const float SparkSpeedMax = 9f;
        private const float SparkScale = 0.04f;
        private const int SparkLife = 12;
        private static readonly Vector2 SparkSquash = new(0.7f, 1f);
        private static readonly Color BodyTint = new(225, 210, 255);
        private static readonly Color GlowColor = new(150, 110, 228, 0);
        private static readonly Color SparkColor = new(196, 168, 248);
        private static readonly Vector3 LightColor = new(0.35f, 0.22f, 0.6f);

        private float Travel { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }  //已飞像素
        private readonly bool[] fxShown = new bool[Main.maxNPCs];

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = Afterimages;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = SafetyUpdates;
        }

        public override void AI() {
            if (Travel == 0f)
                CEUtils.PlaySound("ExobladeBeamSlash", Main.rand.NextFloat(PitchMin, PitchMax), Projectile.Center, SoundInstances, CEUtils.WeapSound * SoundVolume);
            Travel += Projectile.velocity.Length();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(MaxTravel, MaxTravel - FadeTravel, Travel, true);
            Lighting.AddLight(Projectile.Center, LightColor * Projectile.Opacity);
            if (!Main.dedServ && Projectile.timeLeft % MoteEvery == 0)
                Mote();
            if (Projectile.owner != Main.myPlayer)
                RemoteHitFx();
            if (Travel >= MaxTravel)
                Projectile.Kill();
        }

        private Vector2 HalfChordVector() => (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * HalfChord * Projectile.scale;

        private void Mote() {
            Vector2 at = Projectile.Center + HalfChordVector() * Main.rand.NextFloat(-1f, 1f);
            PRTLoader.NewParticle<PRT_GlowSparkCal>(at, -Projectile.velocity * MoteDrift, SparkColor * Projectile.Opacity, MoteScale).Configure(false, MoteLife, SparkSquash);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            Vector2 chord = HalfChordVector();
            return CEUtils.LineThroughRect(Projectile.Center - chord, Projectile.Center + chord, targetHitbox, HitWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitFx(target);

        private void RemoteHitFx() {
            if (Main.dedServ)
                return;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (fxShown[npc.whoAmI] || !npc.CanBeChasedBy() || Colliding(Projectile.Hitbox, npc.Hitbox) != true)
                    continue;
                fxShown[npc.whoAmI] = true;
                HitFx(npc);
            }
        }

        private void HitFx(NPC target) {
            if (Main.dedServ)
                return;
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < HitSparks; i++) {
                Vector2 vel = dir.RotatedByRandom(SparkSpread) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(target.Center, vel, SparkColor, SparkScale).Configure(false, SparkLife, SparkSquash);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 half = Projectile.Size * 0.5f;
            float opacity = Projectile.Opacity;
            for (int i = Afterimages - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Afterimages;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, BodyTint * (k * AfterimageAlpha * opacity), Projectile.oldRot[i], origin,
                    Projectile.scale * MathHelper.Lerp(AfterimageMinScale, 1f, k), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, pos, null, GlowColor * (GlowAlpha * opacity), Projectile.rotation, origin, Projectile.scale * GlowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class DefiledExcaliburOrb : ModProjectile
    {
        private const int Lifetime = 120;              //更新次数,含额外更新约 60 帧
        private const int GrowUpdates = 8;
        private const float GrowFrom = 0.4f;
        private const int Afterimages = 6;
        private const float AfterimageAlpha = 0.5f;
        private const float AfterimageMinScale = 0.5f;
        private const float GlowScale = 0.24f;         //Glow2 边长 256
        private const float GlowAlpha = 0.6f;
        private const int BurstSparks = 8;
        private const float BurstSpeedMin = 2f;
        private const float BurstSpeedMax = 7f;
        private const float BurstScale = 0.035f;
        private const int BurstLife = 14;
        private const float RingStart = 0.05f;
        private const float RingEnd = 0.45f;
        private const int RingLife = 16;
        private static readonly Vector2 SparkSquash = new(0.7f, 1f);
        private static readonly Color GlowColor = new(255, 196, 80, 0);
        private static readonly Color SparkColor = new(255, 214, 96);
        private static readonly Vector3 LightColor = new(0.6f, 0.45f, 0.12f);

        private float Age { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = Afterimages;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            Age++;
            Projectile.scale = MathHelper.Lerp(GrowFrom, 1f, Utils.GetLerpValue(0f, GrowUpdates, Age, true));
            Lighting.AddLight(Projectile.Center, LightColor);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ)
                return;
            for (int i = 0; i < BurstSparks; i++) {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(BurstSpeedMin, BurstSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(Projectile.Center, vel, SparkColor, BurstScale).Configure(false, BurstLife, SparkSquash);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(Projectile.Center, Vector2.Zero, SparkColor, RingStart).Configure(Vector2.One, 0f, RingEnd, RingLife);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 half = Projectile.Size * 0.5f;
            for (int i = Afterimages - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Afterimages;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, Color.White * (k * AfterimageAlpha), 0f, origin,
                    Projectile.scale * MathHelper.Lerp(AfterimageMinScale, 1f, k), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, pos, null, GlowColor * GlowAlpha, 0f, glow.Size() * 0.5f, GlowScale * Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, Color.White, 0f, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
