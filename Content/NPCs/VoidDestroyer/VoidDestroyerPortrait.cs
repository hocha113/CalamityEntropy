using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.Integrations.BossLog;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace CalamityEntropy.Content.NPCs.VoidDestroyer
{
    /// <summary>
    /// 虚空驱逐舰图鉴沙盒:「轨道封锁」星野里的招牌循环:巡航(能量翼环绕、尾焰、核心光)→
    /// 前方裂隙开门 → 全速冲入(全息残影)→ 对侧裂隙出舰(翼脉冲 + 火花爆散)→ 续航。
    /// 纯脚本驱动,不建骨架;几何常量与战斗端 VoidDestroyer.Draw 同源
    /// </summary>
    internal sealed class VoidDestroyerPortraitActor : CEBossPortraitActor
    {
        public static VoidDestroyerPortraitActor Instance => instance ??= new VoidDestroyerPortraitActor();
        private static VoidDestroyerPortraitActor instance;

        [VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/VoidDestroyerP2")]
        internal static Asset<Texture2D> BodyTex = null;
        [VaultLoaden("CalamityEntropy/Content/NPCs/VoidDestroyer/EnergyWing")]
        internal static Asset<Texture2D> WingTex = null;

        //与战斗端 Draw 同源的几何
        private const float ShipScale = 0.9f;
        private static readonly Vector2 WingPivotOffset = new(0f, -8f);
        private static readonly Vector2 CoreOffset = new(0f, 4f);
        private const float WingBaseRadius = 150f;
        private const float WingExpandRadius = 70f;
        private const float PortalAspect = 0.42f;
        private const float PortalSize = 150f;

        //循环编排(秒)
        private const float CycleLength = 11.5f;
        private const float PortalOpenAt = 6.6f;
        private const float DashAt = 7.15f;
        private const float EmergeAt = 7.6f;
        private const float PortalCloseAt = 8.5f;
        /// <summary>裂隙在舰前方多远开门(场景 px)</summary>
        private const float PortalAhead = 165f;

        private readonly CEPortraitMotes motes = new();
        private readonly Vector2[] oldPos = new Vector2[12];
        private Vector2 shipPos;
        private Vector2 prevShipPos;
        private float shipRot;
        private float shipAlpha;
        private float wingRot;
        private float wingExpand;
        /// <summary>巡航路径左右镜像(每穿一次门翻一次),也是舰的行进朝向</summary>
        private float mirror;
        private float cycle;
        private float dashIntensity;
        private Vector2 portalInPos;
        private Vector2 portalOutPos;
        private float portalIn;
        private float portalOut;
        private float dustTimer;
        private float pulseTimer;
        private bool emerged;

        public override Vector2 SceneHalfSize => new(300f, 250f);

        public override CEBossLogTheme Theme => VoidDestroyerLogTheme.Instance;

        private VoidDestroyerPortraitActor() { }

        protected override void Reset() {
            motes.Clear();
            mirror = 1f;
            cycle = 0f;
            shipPos = Patrol(0f);
            prevShipPos = shipPos;
            shipRot = 0f;
            shipAlpha = 1f;
            wingRot = 0f;
            wingExpand = 0f;
            dashIntensity = 0f;
            portalIn = portalOut = 0f;
            dustTimer = pulseTimer = 0f;
            emerged = false;
            for (int i = 0; i < oldPos.Length; i++) {
                oldPos[i] = shipPos;
            }
        }

        /// <summary>巡航路径:横向为主的缓 8 字,x 方向随镜像翻转</summary>
        private Vector2 Patrol(float t)
            => new(mirror * MathF.Sin(t * 0.5f) * 112f, MathF.Sin(t * 1.0f + 1.2f) * 34f - 22f);

        protected override void Update(float dt) {
            float frames = dt * 60f;
            prevShipPos = shipPos;
            cycle += dt;
            if (cycle >= CycleLength) {
                cycle -= CycleLength;
                emerged = false;
            }

            if (cycle < PortalOpenAt) {
                //巡航:向路径点收敛
                Vector2 want = Patrol(Time);
                shipPos += (want - shipPos) * MathHelper.Clamp(0.06f * frames, 0f, 1f);
                shipAlpha = MathF.Min(1f, shipAlpha + dt * 3f);
                portalIn = MathF.Max(0f, portalIn - dt * 2.5f);
                portalOut = MathF.Max(0f, portalOut - dt * 2.5f);
            }
            else if (cycle < DashAt) {
                //前方裂隙开门,舰悬停蓄势(轻微后坐)
                if (portalIn <= 0f && cycle - dt < PortalOpenAt) {
                    portalInPos = new Vector2(shipPos.X + mirror * PortalAhead, shipPos.Y);
                    portalInPos.X = MathHelper.Clamp(portalInPos.X, -SceneHalfSize.X + 70f, SceneHalfSize.X - 70f);
                }
                portalIn = CEBossLogSkin.Ease((cycle - PortalOpenAt) / (DashAt - PortalOpenAt));
                shipPos.X -= mirror * 0.35f * frames;
                shipPos.Y += MathF.Sin(Time * 6f) * 0.2f * frames;
            }
            else if (cycle < EmergeAt) {
                //全速冲入门心,临门渐隐
                float k = (cycle - DashAt) / (EmergeAt - DashAt);
                Vector2 toPortal = portalInPos - shipPos;
                shipPos += toPortal * MathHelper.Clamp(0.22f * frames, 0f, 1f);
                shipAlpha = 1f - CEBossLogSkin.Ease(MathHelper.Clamp((k - 0.45f) / 0.5f, 0f, 1f));
                if (!emerged && k >= 0.999f) {
                    Emerge();
                }
            }
            else if (cycle < PortalCloseAt) {
                if (!emerged) {
                    Emerge();
                }
                //出舰:沿新朝向推出门外,渐显;入口门收拢,出口门先开满后合上
                float k = (cycle - EmergeAt) / (PortalCloseAt - EmergeAt);
                shipAlpha = CEBossLogSkin.Ease(MathHelper.Clamp(k / 0.4f, 0f, 1f));
                shipPos.X += mirror * MathHelper.Lerp(5.5f, 1.2f, k) * frames;
                portalIn = MathF.Max(0f, portalIn - dt * 3f);
                portalOut = k < 0.35f ? 1f : 1f - CEBossLogSkin.Ease((k - 0.35f) / 0.65f);
            }
            else {
                Vector2 want = Patrol(Time);
                shipPos += (want - shipPos) * MathHelper.Clamp(0.05f * frames, 0f, 1f);
                shipAlpha = MathF.Min(1f, shipAlpha + dt * 3f);
                portalIn = MathF.Max(0f, portalIn - dt * 3f);
                portalOut = MathF.Max(0f, portalOut - dt * 3f);
            }

            //速度与倾侧:横向速度读作侧倾,残影强度按速度门控
            Vector2 vel = shipPos - prevShipPos;
            float speed = frames > 0.001f ? vel.Length() / frames : 0f;
            float bank = MathHelper.Clamp(vel.X / MathF.Max(frames, 0.001f) * 0.035f, -0.32f, 0.32f);
            shipRot = MathHelper.Lerp(shipRot, bank, MathHelper.Clamp(0.15f * frames, 0f, 1f));
            float wantGhost = MathHelper.Clamp((speed - 5f) / 9f, 0f, 1f);
            dashIntensity = MathF.Max(wantGhost, dashIntensity - dt * 2.2f);
            for (int i = oldPos.Length - 1; i >= 1; i--) {
                oldPos[i] = oldPos[i - 1];
            }
            oldPos[0] = shipPos;

            //能量翼:恒转,脉冲时加速并外扩;每 3 秒一次小脉冲
            wingExpand = MathF.Max(wingExpand * MathF.Pow(0.94f, frames), 0f);
            wingRot += (0.018f + wingExpand * 0.02f) * frames;
            pulseTimer += dt;
            if (pulseTimer > 3f && cycle < PortalOpenAt) {
                pulseTimer = 0f;
                wingExpand = MathF.Max(wingExpand, 0.4f);
                Sparks(shipPos + WingPivotOffset * ShipScale, 6, 0.6f);
            }

            //虚空尘:低速漂浮的加色小粒
            dustTimer += dt;
            if (dustTimer > 0.14f) {
                dustTimer = 0f;
                Vector2 half = SceneHalfSize;
                motes.Spawn(new Vector2(Main.rand.NextFloat(-half.X, half.X), Main.rand.NextFloat(-half.Y, half.Y)),
                    new Vector2(-mirror * Main.rand.NextFloat(0.15f, 0.5f), Main.rand.NextFloat(-0.15f, 0.15f)),
                    new Vector2(Main.rand.NextFloat(1.2f, 2.4f), Main.rand.NextFloat(1.2f, 2.4f)),
                    Color.Lerp(VDVfx.VoidDeep, VDVfx.VoidPurple, Main.rand.NextFloat()),
                    Main.rand.NextFloat(2.5f, 4.5f), drag: 1f, additive: true);
            }
            //尾焰余烬
            if (Main.rand.NextBool(3)) {
                Vector2 nozzle = shipPos + new Vector2(Main.rand.NextFloat(-22f, 22f), 32f) * ShipScale;
                motes.Spawn(nozzle, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(1.2f, 2.6f)),
                    new Vector2(2f, 2f), Color.Lerp(VDVfx.VoidPurple, VDVfx.VoidPink, Main.rand.NextFloat()) * shipAlpha,
                    Main.rand.NextFloat(0.35f, 0.7f), drag: 0.96f, additive: true);
            }
            motes.Update(frames);
        }

        /// <summary>穿门瞬间:换到对侧出口,翻转朝向,翼脉冲 + 火花</summary>
        private void Emerge() {
            emerged = true;
            portalOutPos = new Vector2(-portalInPos.X, MathHelper.Clamp(portalInPos.Y + Main.rand.NextFloat(-30f, 30f), -120f, 90f));
            //出口在对侧,舰出门后要朝远离出口的方向走:出口在左则向右,反之向左
            mirror = portalOutPos.X < 0f ? 1f : -1f;
            shipPos = portalOutPos;
            prevShipPos = shipPos;
            for (int i = 0; i < oldPos.Length; i++) {
                oldPos[i] = shipPos;
            }
            shipAlpha = 0f;
            portalOut = 1f;
            wingExpand = 1f;
            dashIntensity = 1f;
            Sparks(shipPos, 22, 1.4f);
        }

        private void Sparks(Vector2 pos, int count, float power) {
            for (int i = 0; i < count; i++) {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f) * power;
                motes.Spawn(pos + Main.rand.NextVector2Circular(10f, 10f), vel,
                    new Vector2(Main.rand.NextFloat(2f, 4f), Main.rand.NextFloat(1.2f, 2f)),
                    Color.Lerp(VDVfx.VoidPurple, VDVfx.VoidWhite, Main.rand.NextFloat(0.7f)),
                    Main.rand.NextFloat(0.4f, 0.9f), drag: 0.93f, rot: vel.ToRotation(), additive: true);
            }
        }

        //==================== 绘制 ====================

        public override void Draw(SpriteBatch sb, in CEPortraitFrame frame) {
            DrawSky(sb, in frame);
            DrawPortal(sb, in frame, portalInPos, portalIn);
            DrawPortal(sb, in frame, portalOutPos, portalOut);
            DrawShip(sb, in frame);
            motes.Draw(sb, in frame);
        }

        /// <summary>「轨道封锁」深空:竖向渐变 + 星云 + 星野 + 六边形力场网 + 下缘被侵蚀星球的弧光</summary>
        private void DrawSky(SpriteBatch sb, in CEPortraitFrame frame) {
            Vector2 half = frame.SceneHalf;
            Color top = frame.Dim(VDVfx.SkyTop);
            Color bottom = frame.Dim(VDVfx.SkyHorizon);
            CEPortraitDraw.VerticalGradient(sb, -half.X, half.X, -half.Y, half.Y, top, bottom, 24);
            if (frame.Masked) {
                return;
            }

            CEPortraitDraw.Glow(sb, new Vector2(half.X * 0.45f, -half.Y * 0.55f), new Vector2(half.X * 1.4f, half.Y * 1.1f), VDVfx.SkyNebula * 0.6f);
            CEPortraitDraw.Glow(sb, new Vector2(-half.X * 0.5f, half.Y * 0.4f), new Vector2(half.X * 1.1f, half.Y * 0.9f), VDVfx.VoidDeep * 0.3f);

            //星野
            for (int i = 0; i < 70; i++) {
                float hx = CEPortraitDraw.Hash01(i, 2.1f);
                float hy = CEPortraitDraw.Hash01(i, 5.7f);
                Vector2 p = new(-half.X + hx * half.X * 2f, -half.Y + hy * half.Y * 2f);
                float tw = 0.5f + 0.5f * MathF.Sin(Time * (0.9f + hx * 1.5f) + i * 2.3f);
                float a = 0.25f + 0.55f * tw;
                if (i % 9 == 0) {
                    CEPortraitDraw.Star(sb, p, 2.5f + 1.5f * tw, VDVfx.RiftWhite with { A = 0 } * (a * 0.8f));
                }
                else {
                    float s = 0.8f + hy;
                    CEPortraitDraw.Fill(sb, p, new Vector2(s, s), VDVfx.RiftWhite with { A = 0 } * a);
                }
            }

            //被侵蚀星球:下缘一大段弧光
            Vector2 planet = new(half.X * 0.3f, half.Y + 320f);
            CEPortraitDraw.Glow(sb, planet, 900f, VDVfx.SkyPlanetRim * 0.35f);
            CEPortraitDraw.Ellipse(sb, planet, new Vector2(400f, 400f), 0f, VDVfx.SkyErosion with { A = 0 } * 0.35f, 2.5f, 72);
            CEPortraitDraw.Ellipse(sb, planet, new Vector2(412f, 412f), 0f, VDVfx.SkyPlanetRim with { A = 0 } * 0.2f, 6f, 72);

            //六边形封锁网:向四缘渐显,一圈脉冲从中心扩散
            float wave = (Time * 90f) % 620f;
            CEPortraitDraw.HexGrid(sb, -half, half, 38f, new Vector2(Time * 4f, Time * 2f), c => {
                float d = c.Length();
                float edge = MathHelper.Clamp((d - 150f) / 220f, 0f, 1f) * 0.16f;
                float dw = d - wave;
                float pulse = MathF.Exp(-dw * dw / (2f * 40f * 40f)) * 0.25f;
                return edge + pulse;
            }, VDVfx.SkyRing with { A = 0 }, 1f);
        }

        /// <summary>
        /// 椭圆虚空裂隙(门面朝行进方向,侧视成窄椭圆):背光 + 暗核盘(扫描线填充)+ 紫宽环 + 白细环带转动亮弧。
        /// 剪影模式只留暗盘
        /// </summary>
        private void DrawPortal(SpriteBatch sb, in CEPortraitFrame frame, Vector2 pos, float openness) {
            if (openness <= 0.01f) {
                return;
            }
            float size = PortalSize * openness;
            Vector2 radii = new(size * PortalAspect, size);
            if (!frame.Masked) {
                CEPortraitDraw.Glow(sb, pos, new Vector2(size * PortalAspect * 3.2f, size * 2.6f), VDVfx.VoidPurple * (0.4f * openness));
            }
            //暗核盘:横向扫描线堆出椭圆实心,核心更黑
            Color disk = frame.Dim(VDVfx.VoidDeep) * MathHelper.Clamp(openness * 1.2f, 0f, 1f);
            Color core = frame.Dim(new Color(20, 6, 40)) * MathHelper.Clamp(openness * 1.2f, 0f, 1f);
            for (float y = -size; y <= size; y += 2.5f) {
                float k = y / size;
                float hw = radii.X * MathF.Sqrt(MathF.Max(0f, 1f - k * k));
                if (hw < 0.5f) {
                    continue;
                }
                float swirl = 0.72f + 0.28f * MathF.Sin(Time * 5f + y * 0.11f);
                Color c = Color.Lerp(disk, core, MathF.Max(0f, 1f - MathF.Abs(k) * 1.6f) * swirl);
                CEPortraitDraw.Fill(sb, new Vector2(pos.X - hw, pos.Y + y), new Vector2(hw * 2f, 2.6f), c);
            }
            if (frame.Masked) {
                return;
            }
            float glint = Time * 2.4f;
            CEPortraitDraw.Ellipse(sb, pos, radii, 0f, VDVfx.VoidPurple with { A = 0 } * (0.5f * openness), 6f, 40);
            CEPortraitDraw.Ellipse(sb, pos, radii, 0f, Color.White with { A = 0 } * (0.55f * openness), 2f, 40, glint, Color.White with { A = 0 } * (0.45f * openness));
        }

        /// <summary>本体整叠:尾焰 / 翼 → 全息残影 → 描边底光 → 本体 → 核心光。与战斗端 DrawBodyStack 同序</summary>
        private void DrawShip(SpriteBatch sb, in CEPortraitFrame frame) {
            Texture2D body = BodyTex?.Value;
            Texture2D wing = WingTex?.Value;
            if (body == null || shipAlpha <= 0.01f) {
                return;
            }
            float alpha = shipAlpha;
            Vector2 center = shipPos;
            Color skin = frame.Tint(Color.White) * alpha;

            if (!frame.Masked) {
                DrawBottomFlame(sb, center, alpha);
            }
            if (wing != null) {
                Vector2 pivot = center + WingPivotOffset * ShipScale;
                float radius = (WingBaseRadius + WingExpandRadius * wingExpand) * ShipScale;
                for (int i = 0; i < 4; i++) {
                    float ang = wingRot + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                    Vector2 p = pivot + ang.ToRotationVector2() * radius;
                    //贴图竖直、能量核在下端,让核朝向环心
                    sb.Draw(wing, p, null, skin, ang - MathHelper.PiOver2, wing.Size() * 0.5f, ShipScale, SpriteEffects.None, 0f);
                }
                if (!frame.Masked) {
                    for (int i = 0; i < 4; i++) {
                        float ang = wingRot + MathHelper.PiOver4 + MathHelper.PiOver2 * i;
                        Vector2 p = pivot + ang.ToRotationVector2() * (radius - 24f * ShipScale);
                        CEPortraitDraw.Glow(sb, p, 56f * ShipScale * (1f + wingExpand * 0.4f), VDVfx.VoidPurple * (0.5f * alpha));
                    }
                }
            }

            Vector2 origin = body.Size() * 0.5f;
            if (!frame.Masked) {
                //全息残影:速度门控,紫色加色投影
                if (dashIntensity > 0.02f) {
                    for (int i = 1; i < oldPos.Length; i++) {
                        float fade = 1f - i / (float)oldPos.Length;
                        Color c = VDVfx.VoidPurple with { A = 0 } * (0.5f * fade * dashIntensity * alpha);
                        sb.Draw(body, oldPos[i], null, c, shipRot, origin, ShipScale * (0.96f + 0.04f * fade), SpriteEffects.None, 0f);
                    }
                }
                //描边底光:同贴图略放大的紫色加色垫
                float rim = 0.22f + 0.1f * MathF.Sin(Time * 3f);
                sb.Draw(body, center, null, VDVfx.VoidPurple with { A = 0 } * (rim * alpha), shipRot, origin, ShipScale * 1.05f, SpriteEffects.None, 0f);
            }
            sb.Draw(body, center, null, skin, shipRot, origin, ShipScale, SpriteEffects.None, 0f);

            if (!frame.Masked) {
                //核心光:紫色脉动 + 白芯
                Vector2 core = center + CoreOffset.RotatedBy(shipRot) * ShipScale;
                float pulse = 0.8f + 0.2f * MathF.Sin(Time * 6f);
                CEPortraitDraw.Glow(sb, core, 96f * ShipScale, VDVfx.VoidPurple * (0.6f * pulse * alpha));
                CEPortraitDraw.Glow(sb, core, 40f * ShipScale, Color.White * (0.35f * alpha));
            }
        }

        /// <summary>底部紫色尾焰:三层锥形光加色,脉动 + 摇摆</summary>
        private void DrawBottomFlame(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D cone = CEExtraAssets.GlowCone;
            if (cone == null) {
                return;
            }
            Vector2 basePos = center + new Vector2(0f, 30f) * ShipScale;
            float flicker = 0.85f + 0.15f * MathF.Sin(Time * 23f);
            //GlowCone 尖端在左,旋转 90° 让尖端朝上、锥体向下张开
            for (int i = 0; i < 3; i++) {
                float wobble = MathF.Sin(Time * (9f + i * 4f) + i) * 0.12f;
                Vector2 sc = new Vector2(0.18f + i * 0.05f, 0.5f - i * 0.1f) * ShipScale * flicker;
                Color c = Color.Lerp(VDVfx.VoidPurple, VDVfx.VoidPink, i * 0.35f) with { A = 0 } * (0.55f * alpha);
                sb.Draw(cone, basePos, null, c, MathHelper.PiOver2 + wobble, new Vector2(0f, cone.Height / 2f), sc, SpriteEffects.None, 0f);
            }
        }
    }
}
