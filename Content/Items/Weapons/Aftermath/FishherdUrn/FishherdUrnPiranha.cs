using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.FishherdUrn
{
    //幽灵食人鱼:扑向目标后咬住不放,记住咬合方位随目标移动,目标死亡或离玩家过远就松口
    //ai[0]=状态(0待机 1扑咬 2咬住) ai[1]=目标NPC下标,-1为无 ai[2]=咬合方位(弧度,目标中心指向鱼)
    //localAI[0]=上一帧状态,各端据此认出刚咬住的那一帧 localAI[1]=存活帧数
    public class FishherdUrnPiranha : ModProjectile
    {
        private const int StateIdle = 0;
        private const int StateLunge = 1;
        private const int StateLatched = 2;

        //索敌半径60格;松口半径多留4格,免得在边界上反复咬放;超过100格直接拉回玩家身边(像素)
        private const float AttackRange = 960f;
        private const float ReleaseRange = 1024f;
        private const float TeleportRange = 1600f;
        //每口的护甲穿透;本地无敌帧15即每秒4口
        private const int ArmorPen = 30;
        private const int HitCooldown = 15;
        //扑咬速度(像素/帧)与转向惯性(帧,越大转得越慢)
        private const float LungeSpeed = 22f;
        private const float LungeInertia = 6f;
        //咬合点在目标中心到碰撞箱边缘之间的比例;同主人的鱼按下标错开的角度(弧度)
        private const float LatchDepth = 0.8f;
        private const float LatchJitter = 0.35f;
        //待机:绕玩家的水平半径、头顶高度、上下浮动(像素)、环绕角速度(弧度/帧)
        private const float IdleRadius = 72f;
        private const float IdleHeight = -48f;
        private const float IdleBob = 14f;
        private const double IdleSpin = 0.02;
        //待机游动:最高速(像素/帧)、距离换算速度的系数、惯性(帧)
        private const float IdleMaxSpeed = 12f;
        private const float IdleSteer = 0.08f;
        private const float IdleInertia = 14f;
        //动画每帧停留的游戏帧数:待机、扑咬、咬住
        private const int IdleFrameTicks = 8;
        private const int LungeFrameTicks = 4;
        private const int BiteFrameTicks = 7;
        //画面:向白色插值的自发光、本体不透明度、扑咬残影数与最浓不透明度
        private const float SelfLight = 0.7f;
        private const float BodyAlpha = 0.85f;
        private const int TrailLength = 6;
        private const float TrailAlpha = 0.5f;
        //蓝色底光:Glow2原图256像素,0.22倍约56像素
        private const float GlowScale = 0.22f;
        private const float GlowAlpha = 0.45f;
        //嘴与尾到贴图中心的距离(像素),贴图朝右
        private const float MouthOffset = 16f;
        private const float TailOffset = 16f;
        //魂尘:游动时每帧1/N概率;咬住后每咬一口冒一次;咬住瞬间与召唤时的粒数
        private const int WispChance = 6;
        private const int BiteDustEvery = HitCooldown;
        private const int LatchDust = 8;
        private const int SpawnDust = 14;

        //帧序(贴图4帧:0闭嘴 1尾上摆 2张嘴 3尾下摆):待机多半闭嘴,偶尔摆尾;扑咬整套;咬住张合
        private static readonly int[] IdleFrames = { 0, 0, 1, 0, 0, 3 };
        private static readonly int[] LungeFrames = { 0, 1, 2, 3 };
        private static readonly int[] BiteFrames = { 2, 0 };

        private static readonly Color GlowColor = new(90, 140, 255, 0);
        private static readonly Vector3 GhostLight = new(0.25f, 0.4f, 0.9f);

        //帧序进度,纯本地表现不同步
        private int animStep;

        private int State => (int)Projectile.ai[0];

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 4;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.netImportant = true;
        }

        //projPet的弹幕原版默认不造成接触伤害
        public override bool MinionContactDamage() => true;

        public override bool? CanCutTiles() => false;

        public override bool? CanHitNPC(NPC target) {
            if (State == StateIdle || target.whoAmI != (int)Projectile.ai[1])
                return false;
            return null;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.ArmorPenetration += ArmorPen;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            int buff = ModContent.BuffType<FishherdUrnBuff>();
            if (player.dead || !player.active) {
                player.ClearBuff(buff);
                return;
            }
            if (player.HasBuff(buff))
                Projectile.timeLeft = 2;

            if (Projectile.owner == Main.myPlayer)
                Think(player);
            NPC target = Target();
            if (State == StateLatched && target != null)
                Bite(target);
            else if (State == StateLunge && target != null)
                Chase(target);
            else
                Swim(player);

            Projectile.localAI[1]++;
            Animate();
            if (!Main.dedServ)
                Effects();
        }

        //索敌与状态切换只在主人端做,结果写进ai[]同步
        private void Think(Player player) {
            if (Projectile.Distance(player.Center) > TeleportRange) {
                Projectile.Center = player.Center;
                Projectile.velocity = Vector2.Zero;
                SetState(StateIdle, -1);
                return;
            }
            NPC target = Target();
            if (State != StateIdle && (target == null || !target.CanBeChasedBy(Projectile) || target.Distance(player.Center) > ReleaseRange)) {
                SetState(StateIdle, -1);
                target = null;
            }
            if (player.HasMinionAttackTargetNPC) {
                NPC forced = Main.npc[player.MinionAttackTargetNPC];
                if (forced.CanBeChasedBy(Projectile) && forced.Distance(player.Center) <= AttackRange) {
                    if (target == null || target.whoAmI != forced.whoAmI)
                        SetState(StateLunge, forced.whoAmI);
                    return;
                }
            }
            if (State == StateIdle) {
                NPC found = FindTarget(player);
                if (found != null)
                    SetState(StateLunge, found.whoAmI);
            }
        }

        private NPC FindTarget(Player player) {
            NPC best = null;
            float bestDist = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile) || npc.Distance(player.Center) > AttackRange)
                    continue;
                float dist = Projectile.DistanceSQ(npc.Center);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        private void SetState(int state, int target, float angle = 0f) {
            Projectile.ai[0] = state;
            Projectile.ai[1] = target;
            Projectile.ai[2] = angle;
            Projectile.netUpdate = true;
        }

        private NPC Target() {
            int index = (int)Projectile.ai[1];
            if (State == StateIdle || index < 0 || index >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[index];
            return npc.active && npc.life > 0 ? npc : null;
        }

        private void Chase(NPC target) {
            Vector2 want = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * LungeSpeed + target.velocity;
            Projectile.velocity = (Projectile.velocity * (LungeInertia - 1f) + want) / LungeInertia;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.owner == Main.myPlayer && Projectile.Hitbox.Intersects(target.Hitbox)) {
                float angle = (Projectile.Center - target.Center).ToRotation() + LatchJitter * (Projectile.whoAmI % 5 - 2);
                SetState(StateLatched, target.whoAmI, angle);
            }
        }

        private void Bite(NPC target) {
            Vector2 dir = Projectile.ai[2].ToRotationVector2();
            Projectile.Center = target.Center + dir * EdgeDistance(target, dir) * LatchDepth;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = (-dir).ToRotation();
            Projectile.direction = dir.X > 0f ? -1 : 1;
        }

        //从碰撞箱中心沿方向到边缘的距离
        private static float EdgeDistance(NPC npc, Vector2 dir) {
            float x = Math.Abs(dir.X) > 0.001f ? npc.width * 0.5f / Math.Abs(dir.X) : float.MaxValue;
            float y = Math.Abs(dir.Y) > 0.001f ? npc.height * 0.5f / Math.Abs(dir.Y) : float.MaxValue;
            return Math.Min(x, y);
        }

        private void Swim(Player player) {
            int index = 0;
            int count = 0;
            foreach (Projectile other in Main.ActiveProjectiles) {
                if (other.type != Type || other.owner != Projectile.owner)
                    continue;
                if (other.whoAmI < Projectile.whoAmI)
                    index++;
                count++;
            }
            //用全局帧数当时钟,同主人的鱼在同一椭圆上均匀错开
            float phase = (float)(Main.GameUpdateCount * IdleSpin % MathHelper.TwoPi) + MathHelper.TwoPi * index / Math.Max(count, 1);
            Vector2 home = player.Center + new Vector2((float)Math.Cos(phase) * IdleRadius, IdleHeight + (float)Math.Sin(phase * 2f) * IdleBob);
            Vector2 want = (home - Projectile.Center) * IdleSteer;
            if (want.Length() > IdleMaxSpeed)
                want = want.SafeNormalize(Vector2.Zero) * IdleMaxSpeed;
            Projectile.velocity = (Projectile.velocity * (IdleInertia - 1f) + want) / IdleInertia;
            if (Projectile.velocity.LengthSquared() > 0.25f)
                Projectile.rotation = Projectile.velocity.ToRotation();
        }

        private void Animate() {
            int[] frames = State == StateLatched ? BiteFrames : State == StateLunge ? LungeFrames : IdleFrames;
            int ticks = State == StateLatched ? BiteFrameTicks : State == StateLunge ? LungeFrameTicks : IdleFrameTicks;
            if (++Projectile.frameCounter >= ticks) {
                Projectile.frameCounter = 0;
                animStep++;
            }
            Projectile.frame = frames[animStep % frames.Length];
        }

        private void Effects() {
            Vector2 facing = Projectile.rotation.ToRotationVector2();
            Lighting.AddLight(Projectile.Center, GhostLight);
            if (Projectile.localAI[1] == 1f) {
                for (int i = 0; i < SpawnDust; i++)
                    Ghost(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), 1.3f);
            }
            if (State == StateLatched && Projectile.localAI[0] != StateLatched) {
                SoundEngine.PlaySound(SoundID.NPCHit54 with { Volume = 0.35f, Pitch = 0.3f, PitchVariance = 0.15f, MaxInstances = 3 }, Projectile.Center);
                for (int i = 0; i < LatchDust; i++)
                    Ghost(Projectile.Center + facing * MouthOffset, Main.rand.NextVector2Circular(2.5f, 2.5f), 1.2f);
            }
            Projectile.localAI[0] = State;
            if (State == StateLatched) {
                if (Projectile.localAI[1] % BiteDustEvery == 0f)
                    Ghost(Projectile.Center + facing * MouthOffset, Main.rand.NextVector2Circular(1.5f, 1.5f), 1f);
            }
            else if (Main.rand.NextBool(WispChance)) {
                Ghost(Projectile.Center - facing * TailOffset, -Projectile.velocity * 0.2f, 1.1f);
            }
        }

        private static void Ghost(Vector2 position, Vector2 velocity, float scale) {
            Dust dust = Dust.NewDustPerfect(position, DustID.DungeonSpirit, velocity, 100, default, scale);
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            Color body = Color.Lerp(lightColor, Color.White, SelfLight) * BodyAlpha;
            if (State == StateLunge) {
                for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float k = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, pos, frame, body * (TrailAlpha * k), Projectile.oldRot[i], origin, Projectile.scale, Flip(Projectile.oldRot[i]));
                }
            }
            Texture2D glow = CEUtils.getExtraTex("Glow2");
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, center, null, GlowColor * GlowAlpha, 0f, glow.Size() / 2f, GlowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, center, frame, body, Projectile.rotation, origin, Projectile.scale, Flip(Projectile.rotation));
            return false;
        }

        //贴图朝右,朝左游时上下翻转免得肚皮朝天
        private static SpriteEffects Flip(float rotation) => Math.Cos(rotation) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
    }
}
