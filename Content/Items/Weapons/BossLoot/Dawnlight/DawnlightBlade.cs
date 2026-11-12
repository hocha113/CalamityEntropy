using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.BossLoot.Dawnlight
{
    //晨曦之刃:原版泰拉棱镜(弹幕946,AI_156)的移植,只改两处:锁敌半径翻倍,索敌不查视线
    //ai[0]=计时(-1回位 0待机 1~40横斩 41~80突刺,出招时逐帧递减) ai[1]=目标NPC下标 ai[2]=下一招(0横斩 1突刺)
    //ai[2]由主人端出招时预掷,各端换招时照它取;localAI[0..1]=本招起点,各端在出招首帧自己记
    public class DawnlightBlade : ModProjectile
    {
        private const float StateReturn = -1f;
        private const float StateIdle = 0f;
        //招式计时(帧):横斩40递减到0,突刺80递减到41;计时不低于41即突刺
        private const int SlashStart = 40;
        private const int SlashEnd = 0;
        private const int ThrustStart = 80;
        private const int ThrustEnd = 41;
        private const float NextSlash = 0f;
        private const float NextThrust = 1f;

        //锁敌半径(像素,以玩家为圆心):原版1000,翻倍
        private const float LockRange = 2000f;
        //离玩家超过此距离强制回待机(像素):原版2000,随锁敌半径翻倍
        private const float LeashRange = 4000f;
        //待机时每帧1/N概率尝试出击
        private const int AttackChance = 20;
        //回位速度(像素/帧)、到位距离(像素)、转角插值;待机贴位与转角插值
        private const float ReturnSpeed = 32f;
        private const float ReturnArrive = 2f;
        private const float ReturnTurn = 0.2f;
        private const float IdleLerp = 0.45f;
        //待机扇面:每把错开1/60圈、离挂点10像素;挂点在玩家身后16+6×序号、头顶15像素
        private const float FanSteps = 60f;
        private const float FanRadius = 10f;
        private const float FanBack = 16f;
        private const float FanSpacing = 6f;
        private const float FanHeight = 15f;
        //待机浮动:半径2像素的小圆,转速随序号递增(圈/miscCounter周期)
        private const float BobRadius = 2f;
        private const float BobSpeed = 2f;
        private const float BobPhase = 0.5f;
        private const float BobFacing = 1.3f;
        //横斩:前半程绕起点与目标的中点,后半程换成目标与玩家的中点;椭圆短轴按identity起伏
        private const float SlashSwitch = 0.5f;
        private const float SlashFlatten = 0.5f;
        private const float SlashWobbleBase = 0.8f;
        private const float SlashWobble = 0.2f;
        private const float SlashWobbleSeed = 2.3f;
        //横斩时的名义速度(像素/帧),供拖尾用,位置已预先减掉
        private const float SlashSpeed = 10f;
        //突刺:前40%进度上抬100像素,40%~60%刺到目标,其后穿出60~150像素;转向上限(弧度/帧)
        private const float ThrustRise = 100f;
        private const float ThrustAim = 0.4f;
        private const float ThrustHit = 0.6f;
        private const float ThrustPassMin = 60f;
        private const float ThrustPassMax = 150f;
        private const float ThrustTurn = MathHelper.Pi / 5f;

        //日耀爆炸伤害倍率(策划定值)
        private const float BlastRatio = 0.4f;
        //判定边长(像素)、绘制缩放、拖尾长度(帧),同原版946
        private const int HitboxSize = 10;
        private const float BladeScale = 0.75f;
        private const int TrailLength = 20;
        //色相流动周期(秒);拖尾尾端取相差半圈的颜色
        private const float HueCycle = 3f;
        private const float TrailHueOffset = 0.5f;
        //本体alpha系数;光晕总强度与颜色alpha;贴身光晕强度
        private const float BodyAlpha = 0.7f;
        private const float GlowStrength = 0.3f;
        private const float GlowAlpha = 0.5f;
        private const float CoreGlow = 0.5f;
        //突刺残影:计时70→50淡入、45→40淡出,60→50间拉长到120像素;6层,1.5倍大
        private const float StreakInFrom = 70f;
        private const float StreakInTo = 50f;
        private const float StreakOutFrom = 45f;
        private const float StreakOutTo = 40f;
        private const float StreakReachFrom = 60f;
        private const float StreakReachTo = 50f;
        private const float StreakLength = 120f;
        private const int StreakLayers = 6;
        private const float StreakScale = 1.5f;
        //四向描边光晕的偏移(像素,随缩放)
        private const int RimLayers = 4;
        private const float RimOffset = 4f;

        //朝阳配色:橘红与金黄之间往返
        private static readonly Color DawnRed = new(255, 90, 20);
        private static readonly Color DawnGold = new(255, 200, 60);
        private static readonly Vector3 BladeLight = new(0.15f, 0.08f, 0.02f);

        public override void SetStaticDefaults() {
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.scale = BladeScale;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.manualDirectionChange = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
        }

        public override bool MinionContactDamage() => true;

        //待机时不造成接触伤害,回位途中照样伤人
        public override bool? CanDamage() {
            if (Projectile.ai[0] == StateIdle)
                return false;
            return null;
        }

        public override bool? CanCutTiles() => false;

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            int buff = ModContent.BuffType<DawnlightBuff>();
            if (player.dead || !player.active) {
                player.ClearBuff(buff);
                return;
            }
            if (player.HasBuff(buff))
                Projectile.timeLeft = 2;

            if (Projectile.Distance(player.Center) > LeashRange)
                SetState(StateIdle, 0);
            if (Projectile.ai[0] == StateReturn)
                Return(player);
            else if (Projectile.ai[0] == StateIdle)
                Idle(player);
            else
                Attack(player);

            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center, BladeLight);
        }

        private void Return(Player player) {
            IdlePose(player, out Vector2 spot, out float rotation);
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = Projectile.Center.MoveTowards(spot, ReturnSpeed);
            Projectile.rotation = Projectile.rotation.AngleLerp(rotation, ReturnTurn);
            if (Projectile.Distance(spot) < ReturnArrive)
                SetState(StateIdle, 0);
        }

        //出击只在主人端掷点,随netUpdate同步
        private void Idle(Player player) {
            IdlePose(player, out Vector2 spot, out float rotation);
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = Vector2.SmoothStep(Projectile.Center, spot, IdleLerp);
            Projectile.rotation = Projectile.rotation.AngleLerp(rotation, IdleLerp);
            if (Projectile.owner == Main.myPlayer && Main.rand.NextBool(AttackChance)) {
                int target = FindTarget(player);
                if (target != -1)
                    StartAttack(ThrustStart, target);
            }
        }

        private void Attack(Player player) {
            bool thrust = Projectile.ai[0] >= ThrustEnd;
            int first = (thrust ? ThrustStart : SlashStart) - 1;
            int last = thrust ? ThrustEnd : SlashEnd;
            int index = (int)Projectile.ai[1];
            if (!Main.npc.IndexInRange(index) || !Main.npc[index].CanBeChasedBy(Projectile)) {
                Retarget(player);
                return;
            }
            NPC target = Main.npc[index];
            Projectile.ai[0]--;
            if (Projectile.ai[0] >= first) {
                Projectile.direction = Projectile.Center.X < target.Center.X ? 1 : -1;
                if (Projectile.ai[0] == first) {
                    Projectile.localAI[0] = Projectile.Center.X;
                    Projectile.localAI[1] = Projectile.Center.Y;
                }
            }
            float progress = Utils.GetLerpValue(first, last, Projectile.ai[0], true);
            if (thrust)
                Thrust(target, progress);
            else
                Slash(player, target, progress);
            if (Projectile.ai[0] == last)
                Retarget(player);
        }

        private void Slash(Player player, NPC target, float progress) {
            Vector2 start = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
            if (progress >= SlashSwitch)
                start = Vector2.Lerp(target.Center, player.Center, 0.5f);
            Vector2 toTarget = target.Center - start;
            float baseAngle = toTarget.ToRotation();
            float sweep = Projectile.direction == 1 ? -MathHelper.Pi : MathHelper.Pi;
            float angle = sweep - sweep * progress * 2f;
            Vector2 arc = angle.ToRotationVector2();
            arc.Y *= SlashFlatten * (SlashWobbleBase + (float)Math.Sin(Projectile.identity * SlashWobbleSeed) * SlashWobble);
            Projectile.Center = Vector2.Lerp(start, target.Center, 0.5f) + arc.RotatedBy(baseAngle) * toTarget.Length() / 2f;
            float facing = MathHelper.WrapAngle(baseAngle + angle);
            Projectile.rotation = facing + MathHelper.PiOver2;
            Projectile.velocity = facing.ToRotationVector2() * SlashSpeed;
            Projectile.position -= Projectile.velocity;
        }

        //不动velocity,紧接横斩时带着名义速度偏移,原版如此
        private void Thrust(NPC target, float progress) {
            Vector2 start = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
            start.Y -= Utils.GetLerpValue(0f, ThrustAim, progress, true) * ThrustRise;
            Vector2 toTarget = target.Center - start;
            Vector2 exit = target.Center + toTarget.SafeNormalize(Vector2.Zero) * MathHelper.Clamp(toTarget.Length(), ThrustPassMin, ThrustPassMax);
            float approach = Utils.GetLerpValue(ThrustAim, ThrustHit, progress, true);
            float pass = Utils.GetLerpValue(ThrustHit, 1f, progress, true);
            float aim = toTarget.SafeNormalize(Vector2.Zero).ToRotation() + MathHelper.PiOver2;
            Projectile.rotation = Projectile.rotation.AngleTowards(aim, ThrustTurn);
            Projectile.Center = Vector2.Lerp(start, target.Center, approach);
            if (pass > 0f)
                Projectile.Center = Vector2.Lerp(target.Center, exit, pass);
        }

        private void Retarget(Player player) {
            int target = FindTarget(player);
            if (target == -1) {
                SetState(StateReturn, 0);
                return;
            }
            StartAttack(Projectile.ai[2] == NextThrust ? ThrustStart : SlashStart, target);
        }

        private void StartAttack(int timer, int target) {
            Array.Clear(Projectile.localNPCImmunity);
            SetState(timer, target);
            if (Projectile.owner == Main.myPlayer)
                Projectile.ai[2] = Main.rand.NextBool() ? NextThrust : NextSlash;
        }

        private void SetState(float state, int target) {
            Projectile.ai[0] = state;
            Projectile.ai[1] = target;
            Projectile.netUpdate = true;
        }

        //不查视线,也不限弹幕到目标的距离
        private int FindTarget(Player player) {
            NPC forced = Projectile.OwnerMinionAttackTargetNPC;
            if (forced != null && forced.CanBeChasedBy(Projectile) && forced.Distance(player.Center) <= LockRange)
                return forced.whoAmI;
            int best = -1;
            float bestDist = -1f;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;
                float dist = npc.Distance(player.Center);
                if (dist <= LockRange && (dist <= bestDist || best == -1)) {
                    bestDist = dist;
                    best = npc.whoAmI;
                }
            }
            return best;
        }

        private void IdlePose(Player player, out Vector2 spot, out float rotation) {
            GroupIndex(out int index, out int total);
            int slot = index + 1;
            rotation = MathHelper.WrapAngle(slot * MathHelper.TwoPi / FanSteps * player.direction + MathHelper.PiOver2);
            int bobIndex = slot % total;
            float bobTurns = player.miscCounterNormalized * (BobSpeed + bobIndex) + bobIndex * BobPhase + player.direction * BobFacing;
            Vector2 bob = new Vector2(0f, BobRadius).RotatedBy(bobTurns * MathHelper.TwoPi);
            Vector2 anchor = new Vector2(player.direction * (slot * -FanSpacing - FanBack), player.gravDir * -FanHeight);
            spot = rotation.ToRotationVector2() * FanRadius + player.MountedCenter + anchor + bob;
            rotation += MathHelper.PiOver2;
        }

        //按identity排序号,联机各端一致
        private void GroupIndex(out int index, out int total) {
            index = 0;
            total = 0;
            foreach (Projectile other in Main.ActiveProjectiles) {
                if (other.owner != Projectile.owner || other.type != Type)
                    continue;
                if (other.identity < Projectile.identity)
                    index++;
                total++;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Vector2 spot = Vector2.Clamp(Projectile.Center, target.TopLeft, target.BottomRight);
            int damage = (int)(Projectile.damage * BlastRatio);
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), spot, Vector2.Zero, ModContent.ProjectileType<DawnlightBlast>(),
                damage, 0f, Projectile.owner, Projectile.rotation - MathHelper.PiOver2);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            behindProjectiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor) {
            float slots = Math.Max(1f, Main.player[Projectile.owner].maxMinions);
            float hue = Projectile.identity % slots / slots + Main.GlobalTimeWrappedHourly % HueCycle / HueCycle;
            var drawer = new Terraria.Graphics.EmpressBladeDrawer {
                ColorStart = SunColor(hue, 0f),
                ColorEnd = SunColor(hue + TrailHueOffset, 0f)
            };
            drawer.Draw(Projectile);
            DrawBlade(SunColor(hue, GlowAlpha));
            return false;
        }

        //光晕层用原版946灰度贴图染色,本体用晨曦贴图
        private void DrawBlade(Color glow) {
            Main.instance.LoadProjectile(ProjectileID.EmpressBlade);
            Texture2D shape = TextureAssets.Projectile[ProjectileID.EmpressBlade].Value;
            Texture2D body = TextureAssets.Projectile[Type].Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 origin = shape.Size() / 2f;
            float rotation = Projectile.rotation - MathHelper.PiOver2;
            float scale = Projectile.scale;
            Color main = Color.White * Projectile.Opacity;
            main.A = (byte)(main.A * BodyAlpha);
            glow.A /= 2;
            float strength = Projectile.Opacity * GlowStrength;
            if (strength > 0f) {
                float reach = Utils.GetLerpValue(StreakReachFrom, StreakReachTo, Projectile.ai[0], true);
                float fade = Utils.GetLerpValue(StreakInFrom, StreakInTo, Projectile.ai[0], true) * Utils.GetLerpValue(StreakOutTo, StreakOutFrom, Projectile.ai[0], true);
                for (int i = 0; i < StreakLayers; i++) {
                    float k = i / (float)StreakLayers;
                    Vector2 offset = rotation.ToRotationVector2() * -StreakLength * k * reach;
                    Main.EntitySpriteDraw(shape, center + offset, null, glow * (strength * (1f - k) * fade), rotation, origin, scale * StreakScale, SpriteEffects.None);
                }
                for (int i = 0; i < RimLayers; i++) {
                    Vector2 offset = (MathHelper.TwoPi * i / RimLayers + rotation).ToRotationVector2() * RimOffset * scale;
                    Main.EntitySpriteDraw(shape, center + offset, null, glow * strength, rotation, origin, scale, SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(body, center, null, main, rotation, body.Size() / 2f, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(shape, center, null, glow * (strength * CoreGlow), rotation, origin, scale, SpriteEffects.None);
        }

        private static Color SunColor(float hue, float alpha) {
            Color color = Color.Lerp(DawnRed, DawnGold, Utils.Turn01ToCyclic010(hue));
            color.A = (byte)(alpha * byte.MaxValue);
            return color;
        }
    }
}
