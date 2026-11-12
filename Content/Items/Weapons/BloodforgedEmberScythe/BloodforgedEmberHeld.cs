using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BloodforgedEmberScythe
{
    //身后的血手握镰三段连斩:下劈、上撩、横劈。手臂和镰刀绕肩点整体旋转,一刀扫过 270°,只有正前方的半圈带伤害;横劈把挥砍面压扁成长椭圆
    public class BloodforgedEmberHeld : ModProjectile
    {
        public override string Texture => "CalamityEntropy/Content/Items/Weapons/BloodforgedEmberScythe/BloodforgedEmber";

        [VaultLoaden("CalamityEntropy/Content/Items/Weapons/BloodforgedEmberScythe/BloodforgedEmberHand")]
        internal static Asset<Texture2D> HandTex;

        #region 数值
        public const int Updates = 8;
        //一拍里前摇与挥砍各占的比例,剩下的是收势
        public const float WindupPart = 0.42f;
        public const float StrikePart = 0.3f;
        public const float ArmLength = 26f;
        public const float ScytheScale = 1.3f;
        public const float CleaveReach = 1.5f;
        public const float CleaveSquash = 0.3f;
        public const float CleaveDamageMult = 1.2f;
        public const int RetreatTicks = 12;
        public const int FlipTicks = 12;
        //刀尖局部角,0 指向瞄准方向;一刀从 -140° 扫到 130°,伤害只在 ±90° 以内
        private static readonly float StartAngle = MathHelper.ToRadians(140f);
        private static readonly float EndAngle = MathHelper.ToRadians(130f);
        private static readonly float RestAngle = MathHelper.ToRadians(170f);
        private const float ArcHalf = MathHelper.PiOver2;
        private const float ArcRetract = MathHelper.Pi / (2 * Updates);
        //前摇里先往反方向回拉的角度
        private const float Anticipation = 0.35f;
        private const int ArcSegments = 24;
        #endregion

        #region 贴图几何
        private static readonly Vector2 GripPx = new(31, 84);
        private static readonly Vector2 TipPx = new(130, 96);
        private static readonly Vector2 BladeRootPx = new(96, 30);
        //握点到刀尖,换到镰杆朝 +X 的坐标系
        private static readonly Vector2 TipShaft = (TipPx - GripPx).RotatedBy(MathHelper.PiOver4);
        private static readonly Vector2 HandGripPx = new(16, 18);
        private static readonly Vector2 HandWristPx = new(2, 28);
        #endregion

        public bool Retreating { get => Projectile.ai[0] == 1f; set => Projectile.ai[0] = value ? 1f : 0f; }
        public int BeatCount { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        public float Timer { get => Projectile.ai[2]; set => Projectile.ai[2] = value; }
        public int Beat => BeatCount % 3;
        public int BeatTicks = 24;

        private Vector2 pivot, handPos, tipPos, lastTipPos;
        private float shaftRot, twist = 1f, drawScale = 1f, rigAlpha, glow;
        //世界姿态:刀尖朝向、整体缩放、手臂伸展、翻腕;from 是本拍开始时的姿态
        private float poseTheta, poseScale = 1f, poseArm = 1f, poseTwist = 1f;
        private float fromTheta, fromScale = 1f, fromTwist = 1f;
        //刀光弧的首尾,按本刀挥向归一后的局部角
        private float arcHead, arcTail;
        private bool rigReady, striking, strikeStarted, shook;
        private int seenBeat = -1, flipTimer;
        private readonly ColoredVertex[] stripBuffer = new ColoredVertex[(ArcSegments + 1) * 2];
        private static readonly ColoredVertex[] quadBuffer = new ColoredVertex[4];

        public float TomeAlpha { get; private set; }
        public float TomeFlash { get; private set; }
        public int TomeFrame => flipTimer > 0 ? Math.Clamp(1 + (int)(FlipProgress * 4f), 1, 4) : 0;
        private float FlipProgress => flipTimer > 0 ? 1f - flipTimer / (float)(FlipTicks * Updates) : 0f;

        private static int SpinOf(int beat) => beat == 1 ? -1 : 1;
        private static float SquashOf(int beat) => beat == 2 ? CleaveSquash : 1f;
        private static float ReachOf(int beat) => beat == 2 ? CleaveReach : 1f;

        public static BloodforgedEmberHeld FindFor(Player player) {
            int type = ModContent.ProjectileType<BloodforgedEmberHeld>();
            if (player.ownedProjectileCounts[type] <= 0) {
                return null;
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.type == type && p.owner == player.whoAmI && p.ModProjectile is BloodforgedEmberHeld held) {
                    return held;
                }
            }
            return null;
        }

        public override void SetDefaults() {
            Projectile.FriendlySetDefaults(DamageClass.Magic, false, -1);
            Projectile.extraUpdates = Updates - 1;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ownerHitCheck = true;
            Projectile.timeLeft = 2;
        }

        public override void OnSpawn(IEntitySource source) {
            BeatTicks = Math.Max(10, Projectile.GetOwner().itemAnimationMax);
            Projectile.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((short)BeatTicks);

        public override void ReceiveExtraAI(BinaryReader reader) => BeatTicks = reader.ReadInt16();

        public override void AI() {
            Player player = Projectile.GetOwner();
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<BloodforgedEmber>()) {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            int dir = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.direction = Projectile.spriteDirection = dir;
            if (flipTimer > 0) {
                flipTimer--;
            }
            TomeFlash = Math.Max(0f, TomeFlash - 0.12f / Updates);
            HoldPose(player, dir);
            if (Retreating) {
                RetreatUpdate(player, dir);
            }
            else {
                SwingUpdate(player, dir);
            }
            Projectile.Center = pivot;
        }

        private void HoldPose(Player player, int dir) {
            player.ChangeDir(dir);
            player.itemTime = player.itemAnimation = 2;
            float front = (MathHelper.PiOver2 - 0.46f) * -dir;
            float back = front + MathHelper.Lerp(0.23f, 0.97f, MathF.Sin(MathHelper.Pi * FlipProgress)) * -dir;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, front);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, back);
        }

        private void SwingUpdate(Player player, int dir) {
            bool owner = Main.myPlayer == Projectile.owner;
            if (seenBeat != BeatCount) {
                seenBeat = BeatCount;
                OnBeatStart(player, dir);
            }
            float total = BeatTicks * Updates;
            float p = Timer / total;
            //后续几拍在前摇里松手就收手,这一刀不扣蓝
            if (owner && BeatCount > 0 && p < WindupPart && !player.channel) {
                BeginRetreat();
                RetreatUpdate(player, dir);
                return;
            }
            if (!strikeStarted && p >= WindupPart) {
                if (owner && BeatCount > 0 && !player.CheckMana(player.HeldItem, -1, true)) {
                    BeginRetreat();
                    RetreatUpdate(player, dir);
                    return;
                }
                strikeStarted = true;
                OnStrikeStart(player);
            }

            float sweep = EvaluateSwing(p, dir);
            ApplyRig(player, dir);
            striking = MathF.Abs(sweep) <= ArcHalf;
            if (sweep >= -ArcHalf) {
                arcHead = sweep;
                if (sweep > ArcHalf) {
                    arcTail = Math.Min(arcHead, arcTail + ArcRetract);
                }
            }
            rigAlpha = BeatCount == 0 ? Utils.GetLerpValue(0f, 0.6f, p / WindupPart, true) : 1f;
            glow = p < WindupPart ? MathHelper.Lerp(0.3f, 1f, p / WindupPart) : 0.3f + 0.7f * Utils.GetLerpValue(1f, WindupPart + StrikePart, p, true);
            TomeAlpha = Math.Min(1f, TomeAlpha + 0.04f);
            if (Projectile.numUpdates == -1) {
                SwingEffects(p);
            }

            Timer++;
            if (Timer >= total) {
                EndBeat(player);
            }
        }

        //前摇在世界空间里从当前姿态补间到下一刀起点,换方向、转身都不跳变;返回挥砍中的归一局部角,前摇里为 NaN
        private float EvaluateSwing(float p, int dir) {
            int beat = Beat;
            int spin = SpinOf(beat);
            float sigma = ScytheScale * ReachOf(beat);
            float aimRot = Projectile.velocity.ToRotation();
            float toTwist = dir * spin;
            if (p < WindupPart) {
                float w = p / WindupPart;
                float e = w * w * (3f - 2f * w);
                Vector2 to = Project((-spin * StartAngle).ToRotationVector2(), SquashOf(beat), dir, aimRot);
                poseTheta = fromTheta + MathHelper.WrapAngle(to.ToRotation() - fromTheta) * e - toTwist * Anticipation * MathF.Sin(MathHelper.Pi * w);
                poseScale = MathHelper.Lerp(fromScale, to.Length() * sigma, e);
                poseArm = 1f - 0.22f * MathF.Sin(MathHelper.Pi * w);
                //换向时在前摇前半段翻腕,刀刃转到下一刀前进的那一侧
                poseTwist = MathF.Sign(fromTwist) == MathF.Sign(toTwist) ? toTwist : MathF.Sign(fromTwist) * MathF.Cos(MathHelper.Pi * Utils.GetLerpValue(0f, 0.5f, w, true));
                return float.NaN;
            }
            float q = Utils.GetLerpValue(WindupPart, WindupPart + StrikePart, p, true);
            float s = -StartAngle + (StartAngle + EndAngle) * (1f - (1f - q) * (1f - q));
            Vector2 v = Project((spin * s).ToRotationVector2(), SquashOf(beat), dir, aimRot);
            poseTheta = v.ToRotation();
            poseScale = v.Length() * sigma;
            poseArm = 1f;
            poseTwist = toTwist;
            return s;
        }

        private void ApplyRig(Player player, int dir, float scaleMult = 1f, float armMult = 1f) {
            pivot = player.MountedCenter + new Vector2(-dir * 8f, -12f + player.gfxOffY);
            float tw = MathF.Abs(poseTwist) < 0.12f ? (poseTwist < 0f ? -0.12f : 0.12f) : poseTwist;
            float tipLen = new Vector2(TipShaft.X, TipShaft.Y * tw).Length();
            float scale = poseScale * scaleMult;
            float arm = ArmLength * poseArm * armMult;
            Vector2 ray = poseTheta.ToRotationVector2() * scale;
            Vector2 newTip = pivot + ray * (arm + tipLen);
            lastTipPos = rigReady ? tipPos : newTip;
            rigReady = true;
            handPos = pivot + ray * arm;
            tipPos = newTip;
            drawScale = scale;
            shaftRot = poseTheta - MathF.Atan2(TipShaft.Y * tw, TipShaft.X);
            twist = tw;
        }

        private static Vector2 Project(Vector2 local, float squash, int dir, float aimRot)
            => new Vector2(local.X, local.Y * squash * dir).RotatedBy(aimRot);

        private Vector2 ScytheWorld(Vector2 px) {
            Vector2 l = (px - GripPx).RotatedBy(MathHelper.PiOver4);
            return handPos + new Vector2(l.X, l.Y * twist).RotatedBy(shaftRot) * drawScale;
        }

        private Vector2 HandWorld(Vector2 px) {
            Vector2 l = (px - HandGripPx).RotatedBy(MathHelper.PiOver2);
            return handPos + new Vector2(l.X, l.Y * twist).RotatedBy(shaftRot) * drawScale;
        }

        private void OnBeatStart(Player player, int dir) {
            strikeStarted = false;
            shook = false;
            flipTimer = FlipTicks * Updates;
            if (rigReady) {
                fromTheta = poseTheta;
                fromScale = poseScale;
                fromTwist = poseTwist;
            }
            else {
                //首刀从身后低处浮现
                fromTheta = Project(RestAngle.ToRotationVector2(), 1f, dir, Projectile.velocity.ToRotation()).ToRotation();
                fromScale = ScytheScale * 0.55f;
                fromTwist = dir * SpinOf(Beat);
            }
            CEUtils.PlaySound("pageflip", Main.rand.NextFloat(0.9f, 1.1f), player.Center, 4, 0.5f * CEUtils.WeapSound);
            if (BeatCount == 0) {
                CEUtils.PlaySound("brimstonevortexshoot", 0.7f, player.Center, 2, 0.45f * CEUtils.WeapSound);
            }
        }

        private void OnStrikeStart(Player player) {
            arcHead = arcTail = -ArcHalf;
            Array.Clear(Projectile.localNPCImmunity);
            TomeFlash = 1f;
            float pitch = Beat switch { 0 => 0.85f, 1 => 1f, _ => 0.7f };
            CEUtils.PlaySound("scytheswing", pitch + Main.rand.NextFloat(-0.05f, 0.05f), player.Center, 4, CEUtils.WeapSound);
            CEUtils.PlaySound(Main.rand.NextBool() ? "HellkiteSwing1" : "HellkiteSwing2", pitch * 0.8f, player.Center, 4, 0.55f * CEUtils.WeapSound);
        }

        private void EndBeat(Player player) {
            if (Main.myPlayer != Projectile.owner) {
                BeatCount++;
                Timer = 0;
                return;
            }
            if (player.channel && !player.noItems && !player.CCed) {
                BeatCount++;
                Timer = 0;
                Projectile.velocity = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Projectile.velocity);
                Projectile.netUpdate = true;
            }
            else {
                BeginRetreat();
            }
        }

        private void BeginRetreat() {
            Retreating = true;
            Timer = 0;
            Projectile.netUpdate = true;
        }

        private void RetreatUpdate(Player player, int dir) {
            float r = Timer / (RetreatTicks * Updates);
            float ease = r * r;
            striking = false;
            rigAlpha = Math.Min(rigAlpha, 1f - r);
            TomeAlpha = Math.Min(TomeAlpha, 1f - r);
            glow *= 0.98f;
            ApplyRig(player, dir, 1f - 0.35f * ease, 1f - 0.6f * ease);
            arcTail = Math.Min(arcHead, arcTail + ArcRetract);
            if (Projectile.numUpdates == -1 && !Main.dedServ && rigAlpha > 0.05f) {
                //收手时镰刀化成烬火和血雾散掉
                Vector2 at = ScytheWorld(Vector2.Lerp(BladeRootPx, TipPx, Main.rand.NextFloat()));
                PRTLoader.NewParticle<PRT_FlameCal>(at, new Vector2(0f, -1f) + Main.rand.NextVector2Circular(0.8f, 0.8f), new Color(255, 110, 50), Main.rand.NextFloat(0.04f, 0.06f) * drawScale).Configure(22, 0.4f, new Color(110, 0, 10));
                PRTLoader.NewParticle<PRT_HeavySmokeCal>(Vector2.Lerp(handPos, tipPos, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f), new Color(110, 8, 16), Main.rand.NextFloat(0.4f, 0.6f) * drawScale).Configure(0.4f * rigAlpha, 30, 0.02f);
            }
            Timer++;
            if (r >= 1f) {
                Projectile.Kill();
            }
        }

        private void SwingEffects(float p) {
            if (Main.dedServ) {
                return;
            }
            Vector2 tangent = (tipPos - lastTipPos).SafeNormalize(Vector2.Zero);
            if (!striking) {
                if (p < WindupPart && Main.rand.NextBool(2)) {
                    Vector2 at = ScytheWorld(Vector2.Lerp(BladeRootPx, TipPx, Main.rand.NextFloat()));
                    PRTLoader.NewParticle<PRT_FlameCal>(at, new Vector2(0f, -0.6f) + Main.rand.NextVector2Circular(0.6f, 0.6f), new Color(255, 120, 60), Main.rand.NextFloat(0.03f, 0.045f) * drawScale).Configure(20, Main.rand.NextFloat(0.2f, 0.4f), new Color(120, 10, 10));
                }
                return;
            }
            for (int i = 0; i < 2; i++) {
                Vector2 at = Vector2.Lerp(handPos, tipPos, Main.rand.NextFloat(0.55f, 1f));
                PRTLoader.NewParticle<PRT_FlameCal>(at, tangent * Main.rand.NextFloat(1f, 3f), new Color(255, 110, 50), Main.rand.NextFloat(0.05f, 0.07f) * drawScale).Configure(18, Main.rand.NextFloat(0.3f, 0.5f), new Color(110, 0, 10));
            }
            PRTLoader.NewParticle<PRT_GlowSparkCal>(tipPos, tangent.RotatedByRandom(0.3f) * Main.rand.NextFloat(8f, 14f), new Color(255, 80, 60), 0.035f).Configure(true, 14, new Vector2(0.6f, 1.4f));
            PRTLoader.NewParticle<PRT_HeavySmokeCal>(Vector2.Lerp(handPos, tipPos, Main.rand.NextFloat(0.4f, 0.9f)), tangent * 1.2f + Main.rand.NextVector2Circular(0.5f, 0.5f), new Color(110, 8, 16), Main.rand.NextFloat(0.45f, 0.7f) * drawScale).Configure(0.45f, 32, 0.02f);
            if (Main.rand.NextBool(2)) {
                PRTLoader.NewParticle<PRT_BloodCal>(tipPos, tangent * Main.rand.NextFloat(5f, 9f) + new Vector2(0f, -2f), new Color(190, 12, 22), Main.rand.NextFloat(0.5f, 0.8f)).Configure(26);
            }
        }

        public override bool? CanDamage() => striking ? null : false;

        public override bool? CanCutTiles() => striking;

        public override void CutTiles() {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(handPos, tipPos, 48f * drawScale, DelegateMethods.CutTiles);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            int width = (int)(40f * drawScale);
            return CEUtils.LineThroughRect(handPos, tipPos, targetHitbox, (int)(width * 1.2f))
                || CEUtils.LineThroughRect(ScytheWorld(BladeRootPx), tipPos, targetHitbox, width)
                || CEUtils.LineThroughRect(lastTipPos, tipPos, targetHitbox, (int)(width * 0.8f));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (Beat == 2) {
                modifiers.SourceDamage *= CleaveDamageMult;
            }
            modifiers.HitDirectionOverride = target.Center.X > Projectile.GetOwner().Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Vector2 away = (target.Center - Projectile.GetOwner().Center).SafeNormalize(Vector2.UnitX);
            if (!shook) {
                shook = true;
                CEUtils.SetShake(target.Center, Beat == 2 ? 6f : 3.5f);
                CEUtils.PlaySound(target.Organic() ? "FleshWhipHit" : "metalhit", Main.rand.NextFloat(0.85f, 1f), target.Center, 6, 0.7f * CEUtils.WeapSound);
            }
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 10; i++) {
                Vector2 at = target.Center + Main.rand.NextVector2Circular(target.width * 0.3f, target.height * 0.3f);
                PRTLoader.NewParticle<PRT_BloodCal>(at, away.RotatedByRandom(0.9f) * Main.rand.NextFloat(4f, 11f) + new Vector2(0f, -3f), new Color(200, 14, 24), Main.rand.NextFloat(0.6f, 1.1f)).Configure(30);
            }
            for (int i = 0; i < 6; i++) {
                Color spark = Color.Lerp(new Color(255, 70, 40), new Color(255, 180, 90), Main.rand.NextFloat());
                PRTLoader.NewParticle<PRT_AltSpark>(target.Center, away.RotatedByRandom(0.6f) * Main.rand.NextFloat(8f, 18f), spark, Main.rand.NextFloat(0.9f, 1.4f)).Configure(false, 16);
            }
            PRTLoader.NewParticle<PRT_BloomCal>(target.Center, Vector2.Zero, new Color(255, 40, 30), 0.1f).Configure(0.7f, 12, false);
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!rigReady || rigAlpha <= 0.01f) {
                return false;
            }
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            DrawArm(sb);
            if (arcHead - arcTail > 0.02f) {
                DrawArcLayer(sb, BlendState.NonPremultiplied, 1.06f, 0.42f, new Color(70, 0, 6), new Color(150, 10, 20), 0.85f);
                DrawArcLayer(sb, BlendState.Additive, 1f, 0.36f, new Color(200, 30, 20), new Color(255, 120, 50), 0.9f);
                DrawArcLayer(sb, BlendState.Additive, 1f, 0.1f, new Color(255, 120, 80), new Color(255, 230, 190), 0.8f);
            }
            DrawRig(sb, lightColor);
            sb.ExitShaderRegion();
            return false;
        }

        private void DrawArm(SpriteBatch sb) {
            Effect shader = CEEffectAssets.SwordTrail3;
            if (shader == null) {
                return;
            }
            const int Segments = 10;
            Vector2 wrist = HandWorld(HandWristPx);
            Vector2 span = wrist - pivot;
            Vector2 bend = span.RotatedBy(MathHelper.PiOver2 * Projectile.direction).SafeNormalize(Vector2.Zero) * span.Length() * 0.18f;
            Vector2 control = (pivot + wrist) / 2f + bend;
            for (int i = 0; i <= Segments; i++) {
                float t = i / (float)Segments;
                Vector2 at = Vector2.Lerp(Vector2.Lerp(pivot, control, t), Vector2.Lerp(control, wrist, t), t);
                Vector2 along = (2f * (1f - t) * (control - pivot) + 2f * t * (wrist - control)).SafeNormalize(Vector2.UnitX);
                Vector2 side = along.RotatedBy(MathHelper.PiOver2) * MathHelper.Lerp(3f, 13f, t) * drawScale;
                Vector2 screen = at - Main.screenPosition;
                stripBuffer[i * 2] = new ColoredVertex(screen + side, new Vector3(t, 0f, 1f), Color.White);
                stripBuffer[i * 2 + 1] = new ColoredVertex(screen - side, new Vector3(t, 1f, 1f), Color.White);
            }
            DrawStrip(sb, shader, BlendState.Additive, CEExtraAssets.Streak1, new Color(120, 8, 16), new Color(255, 70, 50), 0.85f * rigAlpha, (Segments + 1) * 2);
        }

        //刀光画在刀尖的解析轨迹上,头始终贴着刀尖;刀过 90° 时正好铺满伤害的那半圈(横劈是半个长椭圆),之后尾端追着刀身收回
        private void DrawArcLayer(SpriteBatch sb, BlendState blend, float outer, float thickness, Color c1, Color c2, float alpha) {
            Effect shader = CEEffectAssets.SwordTrail3;
            if (shader == null) {
                return;
            }
            int beat = Beat;
            int spin = SpinOf(beat);
            float squash = SquashOf(beat);
            float radius = (ArmLength + TipShaft.Length()) * ScytheScale * ReachOf(beat);
            float aimRot = Projectile.velocity.ToRotation();
            Vector2 origin = pivot - Main.screenPosition;
            for (int i = 0; i <= ArcSegments; i++) {
                float u = i / (float)ArcSegments;
                float s = MathHelper.Lerp(arcTail, arcHead, u);
                Vector2 v = Project((spin * s).ToRotationVector2(), squash, Projectile.direction, aimRot) * radius;
                stripBuffer[i * 2] = new ColoredVertex(origin + v * outer, new Vector3(u, 0f, 1f), Color.White);
                stripBuffer[i * 2 + 1] = new ColoredVertex(origin + v * (outer - thickness), new Vector3(u, 1f, 1f), Color.White);
            }
            DrawStrip(sb, shader, blend, CEExtraAssets.Streak2, c1, c2, alpha * rigAlpha, (ArcSegments + 1) * 2);
        }

        private void DrawStrip(SpriteBatch sb, Effect shader, BlendState blend, Texture2D tex, Color c1, Color c2, float alpha, int vertexCount) {
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            shader.Parameters["color1"]?.SetValue(c1.ToVector4());
            shader.Parameters["color2"]?.SetValue(c2.ToVector4());
            shader.Parameters["uTime"]?.SetValue(-Main.GlobalTimeWrappedHourly * 1.6f);
            shader.Parameters["alpha"]?.SetValue(alpha);
            sb.Begin(SpriteSortMode.Immediate, blend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes["EffectPass"].Apply();
            gd.Textures[0] = tex;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, stripBuffer, 0, vertexCount - 2);
            sb.End();
        }

        private void DrawRig(SpriteBatch sb, Color lightColor) {
            Texture2D scythe = TextureAssets.Projectile[Type].Value;
            Texture2D hand = HandTex.Value;
            Vector2 scytheSize = scythe.Size();
            Vector2 handSize = hand.Size();
            Vector2[] glowOffsets = [new(2f, 0f), new(-2f, 0f), new(0f, 2f), new(0f, -2f)];

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (Vector2 o in glowOffsets) {
                DrawQuad(scythe, scytheSize, ScytheWorld, o * drawScale, new Color(255, 40, 30) * (0.45f * glow * rigAlpha));
                DrawQuad(hand, handSize, HandWorld, o * drawScale, new Color(255, 60, 50) * (0.6f * rigAlpha));
            }
            sb.End();

            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawQuad(scythe, scytheSize, ScytheWorld, Vector2.Zero, Color.Lerp(lightColor, Color.White, 0.5f) * rigAlpha);
            DrawQuad(hand, handSize, HandWorld, Vector2.Zero, Color.White * (0.92f * rigAlpha));
        }

        private static void DrawQuad(Texture2D tex, Vector2 size, Func<Vector2, Vector2> toWorld, Vector2 offset, Color color) {
            Vector2 shift = offset - Main.screenPosition;
            quadBuffer[0] = new ColoredVertex(toWorld(Vector2.Zero) + shift, new Vector3(0f, 0f, 1f), color);
            quadBuffer[1] = new ColoredVertex(toWorld(new Vector2(size.X, 0f)) + shift, new Vector3(1f, 0f, 1f), color);
            quadBuffer[2] = new ColoredVertex(toWorld(new Vector2(0f, size.Y)) + shift, new Vector3(0f, 1f, 1f), color);
            quadBuffer[3] = new ColoredVertex(toWorld(size) + shift, new Vector3(1f, 1f, 1f), color);
            Main.graphics.GraphicsDevice.Textures[0] = tex;
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, quadBuffer, 0, 2);
        }
    }
}
