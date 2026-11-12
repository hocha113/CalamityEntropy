using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles.CalamityPorts;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.ManmadeCalamity
{
    //直线飞出、命中或到射程即回收,节奏照原版猪鲨链球 aiStyle 69(Projectile.cs 27081)
    public class ManmadeCalamityHead : ModProjectile
    {
        public const int RestState = 2;                         //收回手里,只画手柄,等下一次出手
        private const int OutState = 0;
        private const int ReturnState = 1;
        private const float MaxReach = 480f;                    //离玩家多远开始回收,像素;原版 400
        private const float ReturnSpeed = 24f;                  //像素每帧;原版 20
        private const float FastReturnDist = 600f;              //超过就提速回收,像素,同原版
        private const float FastReturnSpeed = 40f;              //像素每帧,同原版
        private const float KillDist = 1500f;                   //超过直接消失,像素,同原版
        private const int RestFrames = 4;                       //收回后到能再出手,帧;原版 10
        private const int RestGrace = 3;                        //没接着出手时手柄多留的帧数
        private const int HitStallFrames = 2;                   //命中后原地顿住,帧
        private const int SafetyFrames = 300;                   //兜底寿命,帧
        private const int HitboxSize = 32;                      //像素
        private const int TileHitboxSize = 20;                  //碰物块用的小判定,贴地扔不卡,像素
        private const float HeadRadius = 16f;                   //命中特效离中心,像素
        private const int LeechLifeMin = 5;                     //生命上限不超过它的目标不回血,同原版吸血(Projectile.cs 12696)
        private const int CellInterval = 4;                     //帧,策划 0.07 秒
        private const float CellSpread = MathHelper.PiOver4;    //偏离外抛方向的上限,弧度,同原版泡泡
        private const float CellSpeedMin = 4.5f;                //像素每帧,同原版泡泡
        private const float CellSpeedMax = 6.5f;

        private const float SpinPerSpeed = 0.012f;              //每帧自转 = 横向速度 × 此值,弧度
        private const int Afterimages = 6;                      //拖影帧数
        private const float AfterimageAlpha = 0.4f;
        private const float AfterimageMinScale = 0.75f;
        private const float SelfLight = 0.3f;                   //本体向白色插值,暗处也看得见
        private const float GlowScale = 0.32f;                  //Glow2 边长 256,约 80 像素
        private const float GlowAlpha = 0.45f;
        private const float CoreGlowScale = 0.11f;              //约 28 像素
        private const float CoreFlickerSpeed = 7f;              //弧度每秒
        private const float CoreFlicker = 0.2f;                 //核心亮度起伏幅度
        private const int PulseFrames = 10;                     //命中膨胀回弹,帧
        private const float PulseScale = 0.22f;                 //命中瞬间放大比例
        private const float ChainWaveAmp = 3f;                  //链条中段横摆,像素
        private const float ChainWaveSpeed = 9f;                //弧度每秒
        private const float ChainWavePhase = 6f;                //链条首尾相位差,弧度
        private const float ChainSelfLight = 0.15f;
        private const float HandleGrip = 14f;                   //握点离手柄图底边,像素,落在尾管中段
        private const float HandleSelfLight = 0.2f;

        private const float ThrowPitchMin = 0.75f;              //CEUtils.PlaySound 的音高,1 为原调
        private const float ThrowPitchMax = 0.85f;
        private const float ThrowVolume = 0.6f;
        private const float HitPitchMin = 0.85f;
        private const float HitPitchMax = 1f;
        private const float HitVolume = 0.75f;
        private const float HealPitchMin = 1.1f;
        private const float HealPitchMax = 1.3f;
        private const float HealVolume = 0.45f;
        private const int SoundInstances = 3;
        private const float HitShake = 2f;                      //每秒约两下,比污秽圣剑旋斩的 3 轻
        private const float ShakeRange = 1600f;                 //像素
        private const int HitSparks = 8;
        private const float SparkSpread = 0.6f;                 //弧度
        private const float SparkSpeedMin = 3f;                 //像素每帧
        private const float SparkSpeedMax = 10f;
        private const float SparkScale = 0.035f;                //GlowSpark 边长 2048
        private const int SparkLife = 14;                       //帧
        private const int HitDroplets = 5;                      //溅出的细胞液
        private const float DropletSpread = MathHelper.PiOver2; //朝命中方向的半圆,弧度
        private const float DropletSpeedMin = 1.5f;             //像素每帧
        private const float DropletSpeedMax = 5f;
        private const float DropletScale = 0.2f;                //GlowOrb 边长 72
        private const int DropletLife = 22;                     //帧
        private const float RingStart = 0.1f;                   //HollowCircleHardEdge 边长 156
        private const float RingEnd = 0.6f;
        private const int RingLife = 14;                        //帧
        private const int HealMotes = 5;
        private const float HealMoteSpeed = 2.5f;               //像素每帧
        private const float HealMoteRise = 1.5f;                //向上偏,像素每帧
        private const float HealMoteScale = 0.16f;              //GlowOrb 边长 72
        private const int HealMoteLife = 26;                    //帧
        private static readonly Vector2 SparkSquash = new(0.6f, 1f);
        private static readonly Vector2 RingSquish = new(0.5f, 1f);
        private static readonly SoundStyle SquelchSound = SoundID.NPCHit13 with { Volume = 0.45f, PitchVariance = 0.2f };   //湿响,原版血水母受击
        private static readonly Color SparkHot = new(252, 67, 239);         //取自 ChaoticCellSmall
        private static readonly Color SparkDeep = new(176, 70, 219);        //取自 NihilityActeriophage
        private static readonly Color DropletColor = new(216, 111, 225);    //取自 ChaoticCellMinion
        private static readonly Color HealColor = new(255, 155, 248);       //取自 ChaoticCellSmall
        private static readonly Color GlowColor = new(176, 70, 219, 0);     //A=0 即叠加
        private static readonly Color CoreColor = new(255, 120, 240, 0);
        private static readonly Vector3 HeadLight = new(0.45f, 0.2f, 0.6f);
        private const string ChainPath = "CalamityEntropy/Content/Items/Weapons/BossLoot/ManmadeCalamity/ManmadeCalamityChain";
        private const string HandlePath = "CalamityEntropy/Content/Items/Weapons/BossLoot/ManmadeCalamity/ManmadeCalamityHandle";

        private int State { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Timer { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }      //飞行帧数,决定放细胞的节拍
        private float Stall { get => Projectile.ai[2]; set => Projectile.ai[2] = value; }      //命中顿住剩余帧

        private bool started;
        private float throwAngle;
        private int restTimer;
        private int pulse;
        private readonly bool[] fxShown = new bool[Main.maxNPCs];  //远端命中特效去重

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = Afterimages;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = SafetyFrames;
        }

        public static bool HasFlyingHead(Player player) {
            int type = ModContent.ProjectileType<ManmadeCalamityHead>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == player.whoAmI && (int)p.ai[0] != RestState)
                    return true;
            }
            return false;
        }

        public static void ClearResting(Player player) {
            int type = ModContent.ProjectileType<ManmadeCalamityHead>();
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == player.whoAmI && (int)p.ai[0] == RestState)
                    p.Kill();
            }
        }

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || Projectile.Distance(owner.MountedCenter) > KillDist) {
                Projectile.Kill();
                return;
            }
            if (!started) {
                started = true;
                throwAngle = Projectile.velocity.ToRotation();
                CEUtils.PlaySound("throw", Main.rand.NextFloat(ThrowPitchMin, ThrowPitchMax), Projectile.Center, SoundInstances, CEUtils.WeapSound * ThrowVolume);
            }
            if (pulse > 0)
                pulse--;
            if (State == RestState)
                Rest(owner);
            else
                Fly(owner);
            Hold(owner);
        }

        private void Fly(Player owner) {
            owner.SetDummyItemTime(RestFrames);
            Timer++;
            Vector2 toOwner = owner.MountedCenter - Projectile.Center;
            float dist = toOwner.Length();
            if (State == OutState && dist > MaxReach)
                State = ReturnState;
            if (State == ReturnState) {
                Projectile.tileCollide = false;
                if (Stall > 0f) {
                    Stall--;
                    Projectile.velocity = Vector2.Zero;
                }
                else {
                    float speed = dist > FastReturnDist ? FastReturnSpeed : ReturnSpeed;
                    if (dist < speed) {
                        EnterRest(owner);
                        return;
                    }
                    Projectile.velocity = toOwner / dist * speed;
                }
            }
            Projectile.rotation += Projectile.velocity.X * SpinPerSpeed;
            Lighting.AddLight(Projectile.Center, HeadLight);
            if (Projectile.owner == Main.myPlayer) {
                if ((int)Timer % CellInterval == 0)
                    SpawnCell(owner);
            }
            else
                RemoteHitFx();
        }

        private void EnterRest(Player owner) {
            State = RestState;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = owner.MountedCenter;
            restTimer = 0;
        }

        private void Rest(Player owner) {
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = owner.MountedCenter;
            if (++restTimer > RestFrames + RestGrace)
                Projectile.Kill();
        }

        private void Hold(Player owner) {
            float aim = AimAngle(owner);
            owner.ChangeDir(aim.ToRotationVector2().X >= 0f ? 1 : -1);
            owner.heldProj = Projectile.whoAmI;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
        }

        private float AimAngle(Player owner) {
            Vector2 span = Projectile.Center - owner.MountedCenter;
            return State == RestState || span == Vector2.Zero ? throwAngle : span.ToRotation();
        }

        private void SpawnCell(Player owner) {
            Vector2 outward = (Projectile.Center - owner.MountedCenter).SafeNormalize(throwAngle.ToRotationVector2());
            Vector2 vel = outward.RotatedBy(Main.rand.NextFloat(-CellSpread, CellSpread)) * Main.rand.NextFloat(CellSpeedMin, CellSpeedMax);
            int damage = (int)(Projectile.damage * ManmadeCalamity.CellDamageMult);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, vel, ModContent.ProjectileType<ManmadeCalamityCell>(), damage,
                Projectile.knockBack * ManmadeCalamity.CellKnockbackMult, Projectile.owner, -ManmadeCalamityCell.DriftFrames);
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            width = TileHitboxSize;
            height = TileHitboxSize;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (State == OutState) {
                State = ReturnState;
                Projectile.netUpdate = true;
                Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            }
            return false;
        }

        public override bool? CanDamage() => State == RestState ? false : null;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= ManmadeCalamity.HeadDamageMult;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), ManmadeCalamity.VirusFrames);
            if (State == OutState) {
                State = ReturnState;
                Stall = HitStallFrames;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            TryHeal(target, damageDone);
            HitFx(target);
            CEUtils.SetShake(target.Center, HitShake, ShakeRange);
        }

        private void TryHeal(NPC target, int damageDone) {
            Player owner = Main.player[Projectile.owner];
            if (Projectile.owner != Main.myPlayer || damageDone <= 0 || target.lifeMax <= LeechLifeMin || target.immortal || owner.moonLeech || owner.statLife >= owner.statLifeMax2)
                return;
            if (!owner.GetModPlayer<ManmadeCalamityPlayer>().TryStartHealCooldown())
                return;
            owner.Heal(ManmadeCalamity.HealAmount);
            HealFx(owner);
        }

        private void RemoteHitFx() {
            if (Main.dedServ)
                return;
            Player owner = Main.player[Projectile.owner];
            foreach (NPC npc in Main.ActiveNPCs) {
                if (fxShown[npc.whoAmI] || !npc.CanBeChasedBy() || !Projectile.Hitbox.Intersects(npc.Hitbox))
                    continue;
                fxShown[npc.whoAmI] = true;
                HitFx(npc);
                //真回血只在拥有者端,这里按同样的门槛给队友放特效
                if (!owner.moonLeech && owner.statLife < owner.statLifeMax2 && owner.GetModPlayer<ManmadeCalamityPlayer>().TryStartHealCooldown())
                    HealFx(owner);
            }
        }

        private void HitFx(NPC target) {
            if (Main.dedServ)
                return;
            pulse = PulseFrames;
            Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(throwAngle.ToRotationVector2());
            Vector2 at = Projectile.Center + dir * HeadRadius;
            CEUtils.PlaySound("ballandchainhit1", Main.rand.NextFloat(HitPitchMin, HitPitchMax), at, SoundInstances, CEUtils.WeapSound * HitVolume);
            SoundEngine.PlaySound(SquelchSound, at);
            for (int i = 0; i < HitSparks; i++) {
                Vector2 vel = dir.RotatedBy(Main.rand.NextFloat(-SparkSpread, SparkSpread)) * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSparkCal>(at, vel, Main.rand.NextBool() ? SparkHot : SparkDeep, SparkScale).Configure(false, SparkLife, SparkSquash);
            }
            for (int i = 0; i < HitDroplets; i++) {
                Vector2 vel = dir.RotatedBy(Main.rand.NextFloat(-DropletSpread, DropletSpread)) * Main.rand.NextFloat(DropletSpeedMin, DropletSpeedMax);
                PRTLoader.NewParticle<PRT_GlowOrbCal>(at, vel, DropletColor, DropletScale).Configure(true, DropletLife);
            }
            PRTLoader.NewParticle<PRT_DirectionalPulseRing>(at, Vector2.Zero, SparkDeep, RingStart).Configure(RingSquish, dir.ToRotation(), RingEnd, RingLife);
        }

        private static void HealFx(Player owner) {
            if (Main.dedServ)
                return;
            CEUtils.PlaySound("cellheal", Main.rand.NextFloat(HealPitchMin, HealPitchMax), owner.Center, SoundInstances, CEUtils.WeapSound * HealVolume);
            for (int i = 0; i < HealMotes; i++) {
                Vector2 vel = (MathHelper.TwoPi * i / HealMotes).ToRotationVector2() * HealMoteSpeed - Vector2.UnitY * HealMoteRise;
                PRTLoader.NewParticle<PRT_GlowOrbCal>(owner.Center, vel, HealColor, HealMoteScale).Configure(false, HealMoteLife);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Player owner = Main.player[Projectile.owner];
            float aim = AimAngle(owner);
            Texture2D handle = CEUtils.RequestTex(HandlePath);
            Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2) + Vector2.UnitY * Projectile.gfxOffY;
            if (State != RestState)
                DrawChain(hand + aim.ToRotationVector2() * (handle.Height - HandleGrip));
            DrawHandle(owner, handle, hand, aim);
            if (State != RestState)
                DrawHead(lightColor);
            return false;
        }

        private void DrawChain(Vector2 start) {
            Texture2D chain = CEUtils.RequestTex(ChainPath);
            Vector2 span = Projectile.Center - start;
            float length = span.Length();
            int links = (int)(length / chain.Height);
            if (links <= 0)
                return;
            Vector2 normal = span.RotatedBy(MathHelper.PiOver2) / length;
            float rotation = span.ToRotation() + MathHelper.PiOver2;
            float time = Main.GlobalTimeWrappedHourly * ChainWaveSpeed;
            Vector2 origin = chain.Size() / 2f;
            for (int i = 0; i < links; i++) {
                float t = (i + 0.5f) * chain.Height / length;
                float wave = MathF.Sin(t * MathHelper.Pi) * MathF.Sin(time + t * ChainWavePhase) * ChainWaveAmp;
                Vector2 pos = start + span * t + normal * wave;
                Color color = Color.Lerp(Lighting.GetColor(pos.ToTileCoordinates()), Color.White, ChainSelfLight);
                Main.EntitySpriteDraw(chain, pos - Main.screenPosition, null, color, rotation, origin, 1f, SpriteEffects.None);
            }
        }

        private static void DrawHandle(Player owner, Texture2D handle, Vector2 hand, float aim) {
            Vector2 origin = new(handle.Width / 2f, handle.Height - HandleGrip);
            Color color = Color.Lerp(Lighting.GetColor(hand.ToTileCoordinates()), Color.White, HandleSelfLight);
            SpriteEffects flip = owner.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(handle, hand - Main.screenPosition, null, color, aim + MathHelper.PiOver2, origin, 1f, flip);
        }

        private void DrawHead(Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 origin = tex.Size() / 2f;
            Vector2 glowOrigin = glow.Size() / 2f;
            Vector2 half = Projectile.Size / 2f;
            float scale = Projectile.scale * (1f + PulseScale * pulse / PulseFrames);
            Color body = Color.Lerp(lightColor, Color.White, SelfLight);
            for (int i = Afterimages - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float k = 1f - i / (float)Afterimages;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, null, body * (k * AfterimageAlpha), Projectile.oldRot[i], origin,
                    Projectile.scale * MathHelper.Lerp(AfterimageMinScale, 1f, k), SpriteEffects.None);
            }
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, pos, null, GlowColor * GlowAlpha, 0f, glowOrigin, GlowScale * scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, pos, null, body, Projectile.rotation, origin, scale, SpriteEffects.None);
            float flicker = 1f + CoreFlicker * MathF.Sin(Main.GlobalTimeWrappedHourly * CoreFlickerSpeed);
            Main.EntitySpriteDraw(glow, pos, null, CoreColor * flicker, 0f, glowOrigin, CoreGlowScale * scale, SpriteEffects.None);
        }
    }
}
