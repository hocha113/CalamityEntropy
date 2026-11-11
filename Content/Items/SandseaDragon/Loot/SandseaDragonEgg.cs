using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.SandseaDragon.Loot
{
    public class SandseaDragonEgg : ModItem
    {
        private const int BuffTime = 3600;  //使用当帧给的增益时长(帧),之后由增益自己每帧续

        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.ZephyrFish);
            Item.width = 24;
            Item.height = 30;
            Item.shoot = ModContent.ProjectileType<SandseaHatchling>();
            Item.buffType = ModContent.BuffType<SandseaHatchlingBuff>();
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.AddBuff(Item.buffType, BuffTime);
            }
            return true;
        }
    }

    public class SandseaHatchlingBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<SandseaHatchling>());
        }
    }

    /// <summary>
    /// 沙海幼龙,头、三节身、尾五件的侧视短链,游在主人身后上方
    /// 节链每端本地跟头走,不进同步;贴图头朝左,朝右的节水平翻转
    /// </summary>
    public class SandseaHatchling : ModProjectile
    {
        //Body1..Body3,由前到后渐小
        [VaultLoaden("CalamityEntropy/Content/Items/SandseaDragon/Loot/SandseaHatchlingBody", 1, 3, AssetMode = AssetMode.TextureValueArray)]
        internal static Texture2D[] BodyTex;
        [VaultLoaden("CalamityEntropy/Content/Items/SandseaDragon/Loot/SandseaHatchlingTail")]
        internal static Asset<Texture2D> TailTex;

        #region 数值
        private const int SegmentCount = 5;  //头 + 三节身 + 尾
        private static readonly float[] Gaps = { 15f, 9f, 7f, 11f };  //相邻两节枢轴间距(像素,头在前),按贴图宽留 4 像素重叠
        private static readonly float[] PivotY = { 11f, 13f, 10f, 7f, 10f };  //各节枢轴纵坐标(贴图像素,自顶),取躯干中线不算背鳍;横坐标取贴图正中
        private const float FollowBehind = 46f;  //目标点落在主人身后的水平距离(像素)
        private const float FollowAbove = 40f;  //目标点高出主人中心(像素)
        private const float IdleSpeedFade = 4f;  //主人速度升到此值(像素/帧)时绕圈半径收到 0
        private const float LoopSpeed = 0.035f;  //待机绕圈相位速度(弧度/帧),一圈约 3 秒
        private const float LoopRadiusX = 42f;  //待机横 8 字半宽(像素)
        private const float LoopRadiusY = 12f;  //待机横 8 字半高(像素)
        private const float ChaseGain = 0.09f;  //期望游速 = 离目标点的距离 x 此值
        private const float SwimSpeedMin = 1.6f;  //游速下限(像素/帧),到了目标点也不停
        private const float SwimSpeedMax = 20f;  //游速上限(像素/帧)
        private const float Steer = 0.08f;  //速度每帧向期望速度靠拢的比例
        private const float TeleportDistance = 1400f;  //离目标点超过此值(像素)直接回到身边,同本仓库其它宠物
        private const float TurnSpeedSq = 0.2f;  //速度平方低于此值时头不转向,防原地抖
        private const float HeadTurn = 0.25f;  //头朝向每帧向速度方向靠拢的比例
        private const float Stiffness = 0.3f;  //身节每帧向前一节正后方靠拢的比例
        private const float WaveSpeed = 0.22f;  //摆尾相位速度(弧度/帧),一个来回约半秒
        private const float WavePhase = 0.9f;  //相邻两节的摆动相位差(弧度)
        private const float WaveAmp = 0.35f;  //尾端摆幅(弧度)
        private const float WaveHeadShare = 0.5f;  //头后第一节的摆幅占尾端的比例,往尾部线性升到 1
        private const float FlipDeadzone = 0.2f;  //朝向余弦的绝对值低于此值时不换左右,防竖着游时来回翻面
        private const int SandDustChance = 45;  //每帧 1/45 概率从尾部掉一粒沙
        private const float SandDustFall = 0.6f;  //沙粒初速(像素/帧,向下)
        private const int SandDustAlpha = 120;  //沙粒透明度(0-255,越大越淡)
        private const float SandDustScale = 0.7f;  //沙粒缩放
        private const float PreviewHeading = MathHelper.Pi;  //选人界面头的朝向(弧度),朝左看向角色
        private const float PreviewCurl = 0.7f;  //选人界面每往后一节多转的角度(弧度,约 40 度),整条盘成 C 形塞进角色右侧
        private const float PreviewWave = 0.12f;  //选人界面各节朝向的摆幅(弧度)
        private const float PreviewWaveSpeed = 6f;  //选人界面摆动相位速度(弧度/秒)
        private static readonly Vector2 PreviewHeadOffset = new Vector2(2f, -24f);  //选人界面头部相对假弹幕中心的偏移(像素)
        #endregion

        private readonly Vector2[] segPos = new Vector2[SegmentCount];  //各节枢轴的世界坐标,0 为头
        private readonly float[] segRot = new float[SegmentCount];  //各节朝向(弧度),指向前一节
        private readonly bool[] faceRight = new bool[SegmentCount];  //各节当前是否朝右
        private bool chainReady;
        private int timer;  //本地帧计数,只驱动绕圈与摆尾相位

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;
            ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, 1).WithCode(MarkPreviewWalking);
        }

        //localAI[0] 只给选人界面的假弹幕用,1 = 角色被选中
        private static void MarkPreviewWalking(Projectile proj, bool walking) {
            proj.localAI[0] = walking ? 1f : 0f;
        }

        public override void SetDefaults() {
            Projectile.CloneDefaults(ProjectileID.ZephyrFish);
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) {
                Projectile.active = false;
                return;
            }
            if (!owner.dead && owner.HasBuff(ModContent.BuffType<SandseaHatchlingBuff>())) {
                Projectile.timeLeft = 2;
            }
            timer++;

            //目标点在主人身后上方,主人停下时绕着它画横 8 字
            float idle = MathHelper.Clamp(1f - owner.velocity.Length() / IdleSpeedFade, 0f, 1f);
            float phase = timer * LoopSpeed;
            Vector2 loop = new Vector2(MathF.Cos(phase) * LoopRadiusX, MathF.Sin(phase * 2f) * LoopRadiusY) * idle;
            Vector2 target = owner.MountedCenter + new Vector2(-owner.direction * FollowBehind, -FollowAbove) + loop;
            Vector2 toTarget = target - Projectile.Center;
            float dist = toTarget.Length();
            if (dist > TeleportDistance) {
                Projectile.Center = target;
                Projectile.velocity = Vector2.Zero;
                chainReady = false;
                if (Projectile.owner == Main.myPlayer) {
                    Projectile.netUpdate = true;
                }
            }
            else {
                float speed = MathHelper.Clamp(dist * ChaseGain, SwimSpeedMin, SwimSpeedMax);
                Vector2 want = dist > 1f ? toTarget / dist * speed : Vector2.Zero;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, Steer);
            }

            //AI 在位移之前跑,链头取本帧位移后的位置,画出来才不落后一帧
            UpdateChain(Projectile.Center + Projectile.velocity, owner.direction > 0 ? 0f : MathHelper.Pi);

            if (!Main.dedServ && Main.rand.NextBool(SandDustChance)) {
                Dust.NewDustPerfect(segPos[SegmentCount - 1], DustID.Sand, new Vector2(0f, SandDustFall), SandDustAlpha, default, SandDustScale);
            }
        }

        private void UpdateChain(Vector2 headPos, float spawnHeading) {
            if (!chainReady) {
                for (int i = 0; i < SegmentCount; i++) {
                    segRot[i] = spawnHeading;
                    segPos[i] = i == 0 ? headPos : segPos[i - 1] - spawnHeading.ToRotationVector2() * Gaps[i - 1];
                    faceRight[i] = MathF.Cos(spawnHeading) > 0f;
                }
                chainReady = true;
            }
            if (Projectile.velocity.LengthSquared() > TurnSpeedSq) {
                segRot[0] = Utils.AngleLerp(segRot[0], Projectile.velocity.ToRotation(), HeadTurn);
            }
            segPos[0] = headPos;
            for (int i = 1; i < SegmentCount; i++) {
                Vector2 prev = segPos[i - 1];
                //前一节正后方叠一道往尾部传的行波
                float share = MathHelper.Lerp(WaveHeadShare, 1f, (i - 1f) / (SegmentCount - 2));
                float wave = MathF.Sin(timer * WaveSpeed - i * WavePhase) * WaveAmp * share;
                //Utils.AngleLerp 不归一入参,两角相差超过一圈会绕远路,先收进 (-π, π]
                float back = MathHelper.WrapAngle(segRot[i - 1] + MathHelper.Pi + wave);
                float angle = Utils.AngleLerp((segPos[i] - prev).ToRotation(), back, Stiffness);
                segPos[i] = prev + angle.ToRotationVector2() * Gaps[i - 1];
                segRot[i] = MathHelper.WrapAngle(angle + MathHelper.Pi);
            }
            for (int i = 0; i < SegmentCount; i++) {
                float cos = MathF.Cos(segRot[i]);
                if (MathF.Abs(cos) > FlipDeadzone) {
                    faceRight[i] = cos > 0f;
                }
            }
        }

        //选人界面的假弹幕不跑 AI,按固定姿态盘成 C 形;全程不翻面,背鳍一直在外圈
        private void PosePreview() {
            float phase = Projectile.localAI[0] == 1f ? Main.GlobalTimeWrappedHourly * PreviewWaveSpeed : 0f;
            for (int i = 0; i < SegmentCount; i++) {
                segRot[i] = PreviewHeading + i * PreviewCurl + MathF.Sin(phase - i * WavePhase) * PreviewWave;
                segPos[i] = i == 0 ? Projectile.Center + PreviewHeadOffset : segPos[i - 1] - segRot[i].ToRotationVector2() * Gaps[i - 1];
                faceRight[i] = false;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tail = TailTex?.Value;
            if (BodyTex == null || BodyTex.Length < SegmentCount - 2 || tail == null) {
                return false;
            }
            bool preview = Projectile.isAPreviewDummy;
            if (preview) {
                PosePreview();
            }
            else if (!chainReady) {
                return false;
            }
            Texture2D head = TextureAssets.Projectile[Type].Value;
            //从尾画到头,前一节压住后一节
            for (int i = SegmentCount - 1; i >= 0; i--) {
                Texture2D tex = i == 0 ? head : i == SegmentCount - 1 ? tail : BodyTex[i - 1];
                Vector2 origin = new Vector2(tex.Width / 2f, PivotY[i]);
                bool right = faceRight[i];
                float rotation = right ? segRot[i] : segRot[i] + MathHelper.Pi;
                SpriteEffects effects = right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Color color = preview ? Color.White : Lighting.GetColor((int)(segPos[i].X / 16f), (int)(segPos[i].Y / 16f));
                Main.EntitySpriteDraw(tex, segPos[i] - Main.screenPosition, null, color, rotation, origin, Projectile.scale, effects, 0);
            }
            return false;
        }
    }
}
