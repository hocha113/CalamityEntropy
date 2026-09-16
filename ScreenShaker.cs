using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityEntropy
{
    public static class ScreenShaker
    {
        public class ScreenShake
        {
            public Vector2 Direction;
            public float amplitude;
            public float Counter = Main.rand.NextFloat() * MathHelper.TwoPi;
            public bool active => ScreenShaker.shakes.Contains(this);
            public ScreenShake(Vector2 direction, float amplitude) {
                this.Direction = direction;
                this.amplitude = amplitude;
            }
            public virtual void Update() {
                amplitude -= 0.01f;
                amplitude *= 0.96f;
                if (amplitude < 0)
                    amplitude = 0;
                Counter += Direction.Length() == 0 ? 1 : amplitude;
            }
            public virtual Vector2 GetShiftVec() {
                return Direction * (0.2f + ((float)(Math.Cos(Counter * 0.04f)) * 0.5f + 0.5f)) * amplitude + Direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1, 1) * amplitude * 0.32f + new Vector2((float)(Math.Cos(Counter * 0.67f)) * Main.rand.NextFloat(), (float)(Math.Cos(Counter * -0.73f + MathHelper.PiOver4)) * Main.rand.NextFloat()) * (1 - float.Min(1, Direction.Length())) * amplitude;
            }
        }
        public class NoDirQuickShake : ScreenShake
        {
            public NoDirQuickShake(float amplitude) : base(Vector2.Zero, amplitude) {
            }
            public override void Update() {
                amplitude -= 0.01f;
                amplitude *= 0.94f;
                if (amplitude < 0)
                    amplitude = 0;
                Counter += Direction.Length() == 0 ? 1 : amplitude;
            }
            public override Vector2 GetShiftVec() {
                return new Vector2((float)(Math.Cos(Counter * 0.83f)) * Main.rand.NextFloat(), (float)(Math.Cos(Counter * -0.777f + MathHelper.PiOver4)) * Main.rand.NextFloat()) * amplitude;
            }
        }
        public static List<ScreenShake> shakes;
        public static void Init() {
            shakes = new List<ScreenShake>();
        }
        public static void Unload() {
            shakes = null;
        }
        public static void Update() {
            foreach (var ss in shakes) {
                ss.Update();
            }
            for (int i = shakes.Count - 1; i >= 0; i--) {
                if (shakes[i].amplitude < 0.01f) {
                    shakes.RemoveAt(i);
                }
            }
        }
        public static void AddShake(ScreenShake shake) {
            shakes.Add(shake);
        }
        public static void AddShakeWithRangeFade(ScreenShake shake, float distance, float maxDist = 1600) {
            shake.amplitude *= Utils.Remap(distance, 0, maxDist, 1, 0);
            shakes.Add(shake);
        }
        public static void AddShakeWithRangeFade(ScreenShake shake, Vector2 pos, float maxDist = 1600) {
            shake.amplitude *= Utils.Remap(CEUtils.getDistance(pos, Main.LocalPlayer.Center), 0, maxDist, 1, 0);
            shakes.Add(shake);
        }
        public static ScreenShake AddShake(float direction, float amp) {
            var s = new ScreenShake(direction.ToRotationVector2(), amp);
            AddShake(s);
            return s;
        }
        public static ScreenShake AddShake(Vector2 direction, float amp) {
            var s = new ScreenShake(direction, amp);
            AddShake(s);
            return s;
        }
        public static Vector2 GetShiftVec() {
            Vector2 vec = Vector2.Zero;
            foreach (var ss in shakes) {
                vec += ss.GetShiftVec();
            }
            // 震动强度走自有客户端配置 Config.ScreenShakePower(替代原灾厄 ScreenshakePower)
            return vec * Common.Config.Instance.ScreenShakePower;
        }
    }
}
