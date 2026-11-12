using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles.VoidSlime;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.Starpiercer
{
    /// <summary>
    /// 穿天星点的引力弹丸,没有伤害。追踪最近的一发合格的敌对射弹,撞上后把它消除,自己也消失;不撞物块
    /// 合格见 CanErase;激光类与场地类没有现成标志,用排除集合,再兔底把 Colliding 里调了线段碰撞的 ModProjectile 一律当作激光类
    /// 敌对射弹归服务器管,碰撞与消除只在服务器或单人端做;客户端只追踪与画,追踪目标各端各自找,拥有者定时同步位置
    /// localAI[0] 已飞行帧数
    /// </summary>
    public class StarpiercerGraviton : ModProjectile
    {
        public const float LaunchSpeed = 9f;            //出膛速度,像素每帧
        private const int Size = 20;                    //判定边长,像素
        private const int LifeFrames = 300;             //寿命,帧,5 秒
        private const int FadeFrames = 20;              //寿命最后这些帧里淡出
        private const float SeekRange = 1200f;          //索敌半径,像素,75 格
        private const float RetargetInterval = 10f;     //重新索敌的间隔,帧;目标没了立刻重找
        private const float SyncInterval = 20f;         //拥有者同步位置的间隔,帧,让服务器按拥有者看到的位置判碰撞
        private const float HomingSpeed = 15f;          //追踪时趋近的速度,像素每帧
        private const float HomingInertia = 10f;        //转向惯性,每帧朝目标方向修正 1/10
        private const float LeadFramesMax = 20f;        //瞄准目标前方的提前量上限,帧
        private const int CatchPadding = 8;             //消除判定在弹丸判定框外再放宽的距离,像素
        private const float SpinSpeed = 0.08f;          //弹体自转,弧度每帧
        private const int TrailRecordMode = 2;          //ProjectileID.Sets.TrailingMode 取值,逐帧记下旧位置与朝向
        private const int TrailLength = 8;              //拖尾段数,也是记下的旧位置数
        private const float TrailOpacity = 0.45f;       //拖尾首段不透明度,往后线性降到 0
        private const float TrailScaleEnd = 0.5f;       //拖尾末段缩放,不窄于弹体的一半
        private const float ShadeSize = 96f;            //弹体周围暗晕的贴图直径,像素
        private const float ShadeOpacity = 0.55f;
        private const float RimSize = 44f;              //金色边光的贴图直径,像素
        private const float RimOpacity = 0.5f;
        private const float RimPulse = 0.2f;            //边光起伏幅度,比例
        private const float RimPulseSpeed = 0.15f;      //边光起伏角频率,弧度每帧
        private const float InflowChance = 0.4f;        //每帧出一颗旋入粉尘的概率
        private const float InflowReach = 30f;          //旋入粉尘生成处离弹体的距离,像素
        private const float InflowPull = 0.1f;          //旋入粉尘初速取离弹体距离的比例
        private const float InflowSwirl = 0.9f;         //旋入粉尘的偏角,弧度
        private const int SwallowSparks = 10;           //消除射弹时迸出的金色火花数
        private const float SparkSpeedMin = 2f;         //火花初速,像素每帧
        private const float SparkSpeedMax = 6f;
        private const float SparkScaleMin = 0.05f;
        private const float SparkScaleMax = 0.09f;
        private const int SparkLifetime = 20;           //火花寿命,帧
        private const int FizzleDust = 8;               //寿命耗尽时散出的粉尘数
        private const float FizzleDustSpeed = 2f;       //散出粉尘初速上限,像素每帧
        private const float SwallowPitch = 1.2f;        //吞掉射弹的声音音高,CEUtils.PlaySound 以 1 为原调
        private const float SwallowVolume = 0.6f;
        private const int SwallowInstances = 3;
        private static readonly Color ShadeColor = new Color(12, 10, 20);       //暗晕,同弹体的黑芯
        private static readonly Color RimColor = new Color(255, 194, 11);       //金边,取自金弓银弹
        private static readonly Color SparkColor = new Color(255, 214, 92);
        private static readonly Vector3 LightRGB = new Vector3(0.5f, 0.4f, 0.1f);   //照明,RGB 0-1

        private static HashSet<int> excluded;                                           //激光类与场地类射弹,见 SetStaticDefaults
        private static readonly Dictionary<int, bool> lineCollision = new Dictionary<int, bool>();  //按类型缓存 Colliding 里是否调了线段碰撞
        private static Dictionary<short, OpCode> opCodes;                               //IL 操作码表,首次扫 IL 时建立

        private int target = -1;    //各端各记,正在追的射弹在本端的序号

        private ref float Age => ref Projectile.localAI[0];

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = TrailRecordMode;
            excluded = new HashSet<int> {
                //原版敌对激光、射线与闪电
                ProjectileID.EyeLaser, ProjectileID.PinkLaser, ProjectileID.PurpleLaser, ProjectileID.DeathLaser,
                ProjectileID.FrostBeam, ProjectileID.EyeBeam, ProjectileID.ShadowBeamHostile,
                ProjectileID.UFOLaser, ProjectileID.MartianTurretBolt, ProjectileID.RayGunnerLaser, ProjectileID.SaucerDeathray, ProjectileID.SaucerLaser,
                ProjectileID.PhantasmalDeathray, ProjectileID.MoonLeech, ProjectileID.CultistBossLightningOrbArc, ProjectileID.MedusaHeadRay,
                ProjectileID.StardustSoldierLaser, ProjectileID.NebulaLaser, ProjectileID.VortexLaser, ProjectileID.VortexVortexLightning, ProjectileID.VortexLightning,
                ProjectileID.MartianWalkerLaser, ProjectileID.MoonlordTurretLaser,
                //原版场地类: 贝莉斯吐息、光之女皇的长虹、极光、光枪与日舞
                ProjectileID.DD2BetsyFlameBreath, ProjectileID.HallowBossLastingRainbow, ProjectileID.HallowBossDeathAurora, ProjectileID.FairyQueenLance, ProjectileID.FairyQueenSunDance,
                //末世星史莱姆(虚空史莱姆的末世星形态)的激光、伽马射线、旋转十字激光与环形冲击波;它的边框是首领自身的逻辑,不是射弹
                ModContent.ProjectileType<VSlimeStarLaser>(), ModContent.ProjectileType<VSlimeGammaRay>(), ModContent.ProjectileType<VSlimeCrossStar>(), ModContent.ProjectileType<VSlimeShockwave>(),
            };
        }

        public override void SetDefaults() {
            Projectile.width = Size;
            Projectile.height = Size;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifeFrames;
        }

        public override void AI() {
            Age++;
            Player owner = Main.player[Projectile.owner];
            if (target < 0 || !CanErase(Main.projectile[target], owner) || Age % RetargetInterval == 0f) {
                target = FindTarget(owner);
            }
            if (target >= 0) {
                Projectile prey = Main.projectile[target];
                float distance = Vector2.Distance(prey.Center, Projectile.Center);
                Vector2 aimPoint = prey.Center + prey.velocity * (prey.extraUpdates + 1) * Math.Min(distance / HomingSpeed, LeadFramesMax);
                Vector2 desired = (aimPoint - Projectile.Center).SafeNormalize(Vector2.Zero) * HomingSpeed;
                Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desired) / HomingInertia;
            }
            if (Projectile.owner == Main.myPlayer && Age % SyncInterval == 0f) {
                Projectile.netUpdate = true;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && TryErase(owner)) {
                return;
            }
            Projectile.rotation += SpinSpeed;
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeFrames, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, LightRGB * Projectile.Opacity);
            if (!Main.dedServ && Main.rand.NextFloat() < InflowChance) {
                Vector2 offset = Main.rand.NextVector2CircularEdge(InflowReach, InflowReach);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GoldFlame, (-offset * InflowPull).RotatedBy(InflowSwirl) + Projectile.velocity);
                dust.noGravity = true;
            }
        }

        /// <summary>这发射弹能否被引力弹丸消除: 敌对、有伤害、判定框不比拥有者大,且不是激光类或场地类</summary>
        public static bool CanErase(Projectile projectile, Player owner) {
            if (!projectile.active || !projectile.hostile || projectile.damage <= 0) {
                return false;
            }
            if (projectile.width > owner.width || projectile.height > owner.height || excluded.Contains(projectile.type)) {
                return false;
            }
            return projectile.ModProjectile == null || !UsesLineCollision(projectile.ModProjectile);
        }

        private int FindTarget(Player owner) {
            int best = -1;
            float bestDistance = SeekRange;
            foreach (Projectile other in Main.ActiveProjectiles) {
                if (!CanErase(other, owner)) {
                    continue;
                }
                float distance = Vector2.Distance(other.Center, Projectile.Center);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = other.whoAmI;
                }
            }
            return best;
        }

        //只在服务器或单人端调;撞上一发就把它和自己一起消掉
        private bool TryErase(Player owner) {
            Rectangle reach = Projectile.Hitbox;
            reach.Inflate(CatchPadding, CatchPadding);
            foreach (Projectile other in Main.ActiveProjectiles) {
                if (!reach.Intersects(other.Hitbox) || !CanErase(other, owner)) {
                    continue;
                }
                KillSynced(other);
                KillSynced(Projectile);
                return true;
            }
            return false;
        }

        //服务器调 Kill 只会同步它自己拥有的射弹,玩家拥有的(包括引力弹丸自己)要另发消息
        private static void KillSynced(Projectile projectile) {
            projectile.Kill();
            if (Main.netMode == NetmodeID.Server && projectile.owner != Main.myPlayer) {
                NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, projectile.identity, projectile.owner);
            }
        }

        //Colliding 里调了线段碰撞的 ModProjectile 视作激光类;每种类型只扫一次 IL
        private static bool UsesLineCollision(ModProjectile modProjectile) {
            if (!lineCollision.TryGetValue(modProjectile.Type, out bool result)) {
                result = CollidingCallsLineCheck(modProjectile.GetType());
                lineCollision[modProjectile.Type] = result;
            }
            return result;
        }

        private static bool CollidingCallsLineCheck(System.Type type) {
            MethodInfo method = type.GetMethod(nameof(ModProjectile.Colliding), BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Rectangle), typeof(Rectangle) }, null);
            if (method == null || method.DeclaringType == typeof(ModProjectile)) {
                return false;
            }
            byte[] il;
            try {
                il = method.GetMethodBody()?.GetILAsByteArray();
            }
            catch (Exception) {
                return false;
            }
            if (il == null) {
                return false;
            }
            opCodes ??= BuildOpCodeTable();
            //逐条指令走,跳过操作数,只看 call 与 callvirt 的目标
            int i = 0;
            while (i < il.Length) {
                bool twoByte = il[i] == 0xFE;
                if (twoByte && i + 1 >= il.Length) {
                    return false;
                }
                short value = twoByte ? unchecked((short)(0xFE00 | il[i + 1])) : il[i];
                i += twoByte ? 2 : 1;
                if (!opCodes.TryGetValue(value, out OpCode op)) {
                    return false;
                }
                if ((op == OpCodes.Call || op == OpCodes.Callvirt) && i + 4 <= il.Length && IsLineCheck(method.Module, BitConverter.ToInt32(il, i))) {
                    return true;
                }
                i += OperandSize(op.OperandType, il, i);
            }
            return false;
        }

        private static Dictionary<short, OpCode> BuildOpCodeTable() {
            Dictionary<short, OpCode> table = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (field.GetValue(null) is OpCode op) {
                    table[op.Value] = op;
                }
            }
            return table;
        }

        private static int OperandSize(OperandType type, byte[] il, int at) {
            switch (type) {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    return at + 4 <= il.Length ? 4 + 4 * BitConverter.ToInt32(il, at) : il.Length;
                default:
                    return 4;
            }
        }

        private static bool IsLineCheck(Module module, int token) {
            MethodBase callee;
            try {
                callee = module.ResolveMethod(token);
            }
            catch (Exception) {
                return false;
            }
            if (callee == null) {
                return false;
            }
            System.Type declaring = callee.DeclaringType;
            return declaring == typeof(Collision) && callee.Name.StartsWith(nameof(Collision.CheckAABBvLineCollision)) || declaring == typeof(CEUtils) && callee.Name == nameof(CEUtils.LineThroughRect);
        }

        public override bool? CanDamage() {
            return false;
        }

        //消除射弹时服务器会把击杀发到各端;timeLeft 为 0 是寿命耗尽
        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            if (timeLeft <= 0) {
                for (int i = 0; i < FizzleDust; i++) {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(FizzleDustSpeed, FizzleDustSpeed));
                    dust.noGravity = true;
                }
                return;
            }
            CEUtils.PlaySound("voidSound", SwallowPitch, Projectile.Center, SwallowInstances, SwallowVolume);
            for (int i = 0; i < SwallowSparks; i++) {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(SparkSpeedMin, SparkSpeedMax);
                PRTLoader.NewParticle<PRT_GlowSpark>(Projectile.Center, velocity, SparkColor, Main.rand.NextFloat(SparkScaleMin, SparkScaleMax)).Configure(1f, true, PRTDrawModeEnum.AdditiveBlend, velocity.ToRotation(), SparkLifetime);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float opacity = Projectile.Opacity;
            //拖尾用同一张图的半透明叠色,越旧越淡越小;从最旧的画起
            for (int i = TrailLength - 1; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float progress = i / (float)TrailLength;
                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float scale = MathHelper.Lerp(1f, TrailScaleEnd, progress) * Projectile.scale;
                Main.EntitySpriteDraw(texture, position, null, Color.White * (TrailOpacity * (1f - progress) * opacity), Projectile.oldRot[i], origin, scale, SpriteEffects.None);
            }
            //先压一圈半透明的暗晕,再叠 A=0 的金色边光
            Texture2D glow = CEExtraAssets.Glow2;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float pulse = 1f + RimPulse * MathF.Sin(Age * RimPulseSpeed);
            Main.EntitySpriteDraw(glow, center, null, ShadeColor * (ShadeOpacity * opacity), 0f, glow.Size() * 0.5f, ShadeSize * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, center, null, RimColor with { A = 0 } * (RimOpacity * opacity), 0f, glow.Size() * 0.5f, RimSize * pulse * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, null, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
