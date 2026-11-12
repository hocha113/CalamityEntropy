using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using CalamityEntropy.Content.NPCs.VoidSlime;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.VoidSlime.CelestialSlimeStaff
{
    /// <summary>
    /// 可见的天体史莱姆,自己不占栏位;栏位数 = 主人的计数弹幕个数,决定形态和伤害成长。
    /// 目标与开火只在主人端决定;ai[1] 是开火序号,其他端看到它变了就让图腾发光
    /// </summary>
    public class CelestialSlimeStaffSlime : ModProjectile
    {
        #region 调参旋钮
        //每多1个栏位,面板增加基础面板的30%(策划)
        public const float GrowthPerSlot = 0.3f;
        //5栏起超新星、9栏起奇点(策划)
        public const int NovaSlots = 5;
        public const int SingularitySlots = 9;
        //索敌距离(像素,120格,策划)
        public const float SeekRange = 1920f;
        //判定尺寸(像素);续命帧数(有增益时每帧续上,没了就在这么多帧后消失)
        public const int HitboxWidth = 28;
        public const int HitboxHeight = 20;
        public const int KeepAliveFrames = 2;
        //悬停点相对玩家中心(像素,参照光女照明宠物的 -36,-50);上下浮动幅度(像素)与角速度(弧度/秒)
        public const float HoverX = -40f;
        public const float HoverY = -52f;
        public const float BobAmplitude = 5f;
        public const float BobSpeed = 2f;
        //跟随:超过这个距离直接瞬移(像素);追随系数、最大速度(像素/帧)、速度平滑系数
        public const float TeleportDistance = 2000f;
        public const float FollowFactor = 0.12f;
        public const float FollowMaxSpeed = 18f;
        public const float FollowSmoothing = 0.15f;
        //宇宙能量:间隔(帧)、每次发数、伤害倍率(策划);出手速度(像素/更新)、扇形半张角(弧度)、出手方向偏向目标的比例
        public const int EnergyInterval = 30;
        public const int EnergyCount = 3;
        public const float EnergyMult = 1f;
        public const float EnergyLaunchSpeed = 7f;
        public const float EnergySpread = 0.6f;
        public const float EnergyAimBias = 0.35f;
        //伽玛射线:间隔(帧,2.5秒)与伤害倍率(策划)
        public const int RayInterval = 150;
        public const float RayMult = 5f;
        //黑洞星:间隔(帧)、每次发数、伤害倍率(策划:每秒3发、33%);出手速度(像素/更新)
        public const int StarInterval = 60;
        public const int StarCount = 3;
        public const float StarMult = 0.33f;
        public const float StarLaunchSpeed = 6f;
        //贴图:每种形态一列、每列的帧数,每帧尺寸(像素)、每帧持续(帧);图腾与光环中心相对帧中心的高度(像素,和贴图脚本一致)
        public const int Frames = 4;
        public const int FrameWidth = 32;
        public const int FrameHeight = 22;
        public const int FrameTicks = 8;
        public const float TotemOffsetY = -3f;
        public const float HaloOffsetY = -18f;
        //照明强度:平时、超新星(散发白光)、开火时叠加的
        public const float LightBase = 0.6f;
        public const float LightNova = 1f;
        public const float LightFire = 0.4f;
        //图腾发光:每帧衰减;柔光平时与开火时叠加的强度、直径与开火时变大的量(像素)
        public const float GlowDecay = 0.05f;
        public const float TotemGlowBase = 0.3f;
        public const float TotemGlowFire = 0.6f;
        public const float TotemGlowDiameter = 64f;
        public const float TotemGlowGrow = 24f;
        //超新星白光:强度、直径(像素)
        public const float NovaGlowOpacity = 0.5f;
        public const float NovaGlowDiameter = 96f;
        //光环:上下浮动的角速度(弧度/秒)与幅度(像素)、底光强度与直径(像素)
        public const float HaloBobSpeed = 3f;
        public const float HaloBobAmplitude = 1f;
        public const float HaloGlowOpacity = 0.4f;
        public const float HaloGlowDiameter = 36f;
        //形态切换:闪光帧数、强度、起始直径与扩大量(像素);爆闪粒子大小;音效音高、音量与同时发声上限
        public const int FlashFrames = 20;
        public const float FlashOpacity = 1.2f;
        public const float FlashDiameter = 48f;
        public const float FlashGrow = 80f;
        public const float FlashBurstSize = 0.7f;
        public const float FlashPitch = 1.2f;
        public const float FlashVolume = 0.5f;
        public const int FlashMaxSounds = 2;
        #endregion

        //光环底光的浅金
        public static readonly Color HaloGlowColor = new(255, 214, 110);

        private static readonly string Dir = typeof(CelestialSlimeStaffSlime).Namespace.Replace('.', '/') + "/";
        private static Texture2D TotemTex => ModContent.Request<Texture2D>(Dir + "CelestialSlimeStaffTotem", AssetRequestMode.ImmediateLoad).Value;
        private static Texture2D HaloTex => ModContent.Request<Texture2D>(Dir + "CelestialSlimeStaffHalo", AssetRequestMode.ImmediateLoad).Value;

        //当前形态:0 太阳、1 超新星、2 奇点;-1 是还没算过
        private int form = -1;
        //图腾发光 0..1、形态切换闪光剩余帧数、上次见到的开火序号
        private float glow;
        private int flashTimer;
        private int lastSerial;
        //攻击计时(帧),只在主人端走
        private int energyTimer;
        private int rayTimer;
        private int starTimer;

        public Vector2 TotemCenter => Projectile.Center + new Vector2(0f, TotemOffsetY);

        //形态主色:太阳橘黄、超新星白、奇点金
        public static Color MainColor(int form) => form == 1 ? new Color(236, 244, 255) : form == 2 ? new Color(255, 206, 86) : new Color(255, 164, 58);
        //拖尾与光晕色
        public static Color TrailColor(int form) => form == 1 ? new Color(196, 220, 255) : form == 2 ? new Color(255, 178, 52) : new Color(255, 112, 36);

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = Frames;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxWidth;
            Projectile.height = HitboxHeight;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.timeLeft = KeepAliveFrames;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;
        public override bool? CanDamage() => false;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            int counterType = ModContent.ProjectileType<CelestialSlimeStaffCounter>();
            int slots = 0;
            int baseDamage = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.owner == Projectile.owner && p.type == counterType) {
                    slots++;
                    baseDamage = Math.Max(baseDamage, p.originalDamage);
                }
            }
            //没有计数弹幕或增益就不再续命,自然消失
            if (!owner.active || owner.dead || slots == 0 || !owner.HasBuff(ModContent.BuffType<CelestialSlimeStaffBuff>())) {
                return;
            }
            Projectile.timeLeft = KeepAliveFrames;
            Projectile.originalDamage = (int)(baseDamage * (1f + GrowthPerSlot * (slots - 1)));

            int newForm = slots >= SingularitySlots ? 2 : slots >= NovaSlots ? 1 : 0;
            if (newForm != form) {
                if (form >= 0) {
                    FormFlash(newForm);
                }
                form = newForm;
            }

            Follow(owner);
            if (++Projectile.frameCounter >= FrameTicks) {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            int serial = (int)Projectile.ai[1];
            if (serial != lastSerial) {
                lastSerial = serial;
                glow = 1f;
            }
            glow = Math.Max(0f, glow - GlowDecay);
            if (flashTimer > 0) {
                flashTimer--;
            }
            float light = (form == 1 ? LightNova : LightBase) + LightFire * glow;
            Lighting.AddLight(Projectile.Center, MainColor(form).ToVector3() * light);

            if (Projectile.owner == Main.myPlayer) {
                Attack();
            }
        }

        private void Follow(Player owner) {
            float bob = MathF.Sin(Main.GlobalTimeWrappedHourly * BobSpeed + Projectile.identity) * BobAmplitude;
            Vector2 home = owner.MountedCenter + new Vector2(HoverX * owner.direction, HoverY + bob);
            Vector2 diff = home - Projectile.Center;
            if (diff.Length() > TeleportDistance) {
                Projectile.Center = home;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                return;
            }
            Vector2 want = diff * FollowFactor;
            if (want.Length() > FollowMaxSpeed) {
                want *= FollowMaxSpeed / want.Length();
            }
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, FollowSmoothing);
        }

        private void Attack() {
            if (energyTimer < EnergyInterval) {
                energyTimer++;
            }
            if (form >= 1 && rayTimer < RayInterval) {
                rayTimer++;
            }
            if (form >= 2 && starTimer < StarInterval) {
                starTimer++;
            }
            NPC target = FindTarget();
            if (target == null) {
                return;
            }
            IEntitySource source = Projectile.GetSource_FromThis();
            int damage = Projectile.damage;
            bool fired = false;
            if (energyTimer >= EnergyInterval) {
                energyTimer = 0;
                fired = true;
                Vector2 aim = (target.Center - TotemCenter).SafeNormalize(-Vector2.UnitY);
                Vector2 launch = Vector2.Lerp(-Vector2.UnitY, aim, EnergyAimBias).SafeNormalize(-Vector2.UnitY);
                for (int i = 0; i < EnergyCount; i++) {
                    float offset = MathHelper.Lerp(-EnergySpread, EnergySpread, i / (float)(EnergyCount - 1));
                    Projectile.NewProjectile(source, TotemCenter, launch.RotatedBy(offset) * EnergyLaunchSpeed, ModContent.ProjectileType<CelestialSlimeStaffEnergy>(),
                        (int)(damage * EnergyMult), Projectile.knockBack, Projectile.owner, target.whoAmI, form);
                }
            }
            if (form >= 1 && rayTimer >= RayInterval) {
                rayTimer = 0;
                fired = true;
                float rotation = (target.Center - TotemCenter).ToRotation();
                Projectile.NewProjectile(source, TotemCenter, Vector2.Zero, ModContent.ProjectileType<CelestialSlimeStaffRay>(),
                    (int)(damage * RayMult), 0f, Projectile.owner, rotation, form);
            }
            if (form >= 2 && starTimer >= StarInterval) {
                starTimer = 0;
                fired = true;
                float start = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < StarCount; i++) {
                    Vector2 velocity = (start + MathHelper.TwoPi * i / StarCount).ToRotationVector2() * StarLaunchSpeed;
                    Projectile.NewProjectile(source, TotemCenter, velocity, ModContent.ProjectileType<CelestialSlimeStaffDarkStar>(),
                        (int)(damage * StarMult), Projectile.knockBack, Projectile.owner, target.whoAmI, damage);
                }
            }
            if (fired) {
                Projectile.ai[1]++;
                Projectile.netUpdate = true;
            }
        }

        //玩家手动锁定的目标优先,否则取索敌距离内最近的
        private NPC FindTarget() {
            NPC manual = Projectile.OwnerMinionAttackTargetNPC;
            if (manual != null && manual.CanBeChasedBy(Projectile) && Projectile.Distance(manual.Center) <= SeekRange) {
                return manual;
            }
            NPC best = null;
            float bestDistance = SeekRange;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }
                float distance = Projectile.Distance(npc.Center);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }

        private void FormFlash(int newForm) {
            flashTimer = FlashFrames;
            if (Main.dedServ) {
                return;
            }
            VSlimeVfx.Burst(Projectile.Center, FlashBurstSize, TrailColor(newForm), MainColor(newForm));
            VSlimeVfx.Cue("soulshine", Projectile.Center, FlashPitch, FlashVolume, FlashMaxSounds);
        }

        //画序:图腾柔光 → 图腾 → 开火时图腾叠加亮色 → 史莱姆 → 光环与形态切换闪光(加法)
        public override bool PreDraw(ref Color lightColor) {
            if (form < 0) {
                return false;
            }
            Texture2D sheet = TextureAssets.Projectile[Type].Value;
            Texture2D totem = TotemTex;
            Texture2D halo = HaloTex;
            Texture2D soft = GodDraw.Glow;
            Vector2 center = Projectile.Center;
            Vector2 totemPos = TotemCenter;
            Vector2 haloPos = center + new Vector2(0f, HaloOffsetY + MathF.Sin(Main.GlobalTimeWrappedHourly * HaloBobSpeed) * HaloBobAmplitude);
            Vector2 totemScreen = (totemPos - Main.screenPosition).Floor();
            Color main = MainColor(form);
            Color trail = TrailColor(form);
            float flash = flashTimer / (float)FlashFrames;

            GodDraw.BeginAdditive();
            GodDraw.Centered(soft, totemPos, GodDraw.Add(trail, TotemGlowBase + TotemGlowFire * glow), 0f, (TotemGlowDiameter + TotemGlowGrow * glow) / soft.Width);
            if (form == 1) {
                GodDraw.Centered(soft, center, GodDraw.Add(Color.White, NovaGlowOpacity), 0f, NovaGlowDiameter / soft.Width);
            }
            GodDraw.End();

            Main.spriteBatch.Draw(totem, totemScreen, null, Color.White, 0f, totem.Size() / 2f, 1f, SpriteEffects.None, 0f);
            if (glow > 0f) {
                GodDraw.BeginAdditive();
                Main.spriteBatch.Draw(totem, totemScreen, null, GodDraw.Add(main, glow), 0f, totem.Size() / 2f, 1f, SpriteEffects.None, 0f);
                GodDraw.End();
            }

            Rectangle frame = new Rectangle(form * FrameWidth, Projectile.frame * FrameHeight, FrameWidth, FrameHeight);
            Main.spriteBatch.Draw(sheet, (center - Main.screenPosition).Floor(), frame, Color.White, 0f, frame.Size() / 2f, 1f, SpriteEffects.None, 0f);

            GodDraw.BeginAdditive();
            GodDraw.Centered(soft, haloPos, GodDraw.Add(HaloGlowColor, HaloGlowOpacity), 0f, HaloGlowDiameter / soft.Width);
            Main.spriteBatch.Draw(halo, (haloPos - Main.screenPosition).Floor(), null, Color.White, 0f, halo.Size() / 2f, 1f, SpriteEffects.None, 0f);
            if (flash > 0f) {
                GodDraw.Centered(soft, center, GodDraw.Add(Color.White, FlashOpacity * flash), 0f, (FlashDiameter + FlashGrow * (1f - flash)) / soft.Width);
            }
            GodDraw.End();
            return false;
        }
    }
}
