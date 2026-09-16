using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class LashingBramblerodProjectile : BaseWhip
    {
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.MaxUpdates = 6;
            this.segments = 36;
            this.rangeMult = 2f;
        }
        public override string getTagEffectName => "LashingBramblerod";
        public override SoundStyle? WhipSound => SoundID.Grass;
        public override Color StringColor => Color.DarkGreen;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            base.OnHitNPC(target, hit, damageDone);
            CEUtils.PlaySound("beeSting", 1, target.Center);
            target.AddBuff(ModContent.BuffType<LashingBramblerodWhipDebuff>(), 240);
        }
        public override bool PreAI() {
            var points = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, points);
            if (FlyProgress > 0.44f && FlyProgress < 0.88f)
                //LifeLeaf bramble/silva装饰,旧PRT/EParticle LifeLeaf
                PRTLoader.NewParticle<PRT_LifeLeaf>(points[points.Count - 1], CEUtils.randomPointInCircle(4), Color.White, Main.rand.NextFloat(0.8f, 1.2f))
                    .Configure(1, false, PRTDrawModeEnum.AlphaBlend, CEUtils.randomRot());  //LifeLeaf bramble/silva装饰,旧EParticle LifeLeaf

            return base.PreAI();
        }
        public override int handleHeight => 30;
        public override int segHeight => 20;
        public override int endHeight => 30;
        public override int segTypes => 2;
    }

    public class SilvaVine : ModProjectile
    {
        public static int MaxFlowers = 4;
        public override void SetDefaults() {
            Projectile.friendly = true;
            Projectile.width = Projectile.height = 64;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
        }

        public int flowerCount = 0;
        public class SilvaFlower
        {
            public Vector2 offset;
            public int type;
            public float rotation;

            public SilvaFlower() {
                offset = CEUtils.randomPointInCircle(28);
                type = Main.rand.Next(3);
                rotation = CEUtils.randomRot();
            }

            public void Draw(Vector2 projPos, Color lightColor) {
                Texture2D tex = CEUtils.getExtraTex("flower" + type.ToString());
                Main.EntitySpriteDraw(tex, projPos + offset - Main.screenPosition, null, lightColor, rotation, tex.Size() / 2f, 1, SpriteEffects.None);
            }
        }
        public List<SilvaFlower> flowers = new();
        public override void OnSpawn(IEntitySource source) {
            CEUtils.PlaySound("VineSpawn", 1, Projectile.Center);
        }
        public override void AI() {
            var player = Projectile.GetOwner();
            Projectile.Center = player.MountedCenter + player.gfxOffY * Vector2.UnitY;
            if (flowers.Count < flowerCount) {
                flowers.Add(new SilvaFlower());
            }
            Projectile.timeLeft = 3;
            if (player.GetModPlayer<SilvaVineDRPlayer>().HitCounter > 0) {
                CEUtils.PlaySound("FractalHit", 1, Projectile.Center);
                Projectile.timeLeft = 0;
                Projectile.Kill();
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public override bool PreDraw(ref Color lightColor) {
            Main.EntitySpriteDraw(Projectile.getDrawData(lightColor));
            foreach (var flower in flowers) {
                flower.Draw(Projectile.Center, lightColor);
            }
            return false;
        }
        public static float baseDR = 0.1f;
        public static float DREachFlower = 0.05f;
        public static int RegenPerFlower = 2;
        public float DamageReduce => baseDR + DREachFlower * flowerCount;
        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(flowerCount);
        }
        public override void ReceiveExtraAI(BinaryReader reader) {
            flowerCount = reader.ReadInt32();
        }
    }

    public class SilvaVineDRPlayer : ModPlayer
    {
        public static int VineType = -1;
        public int HitCounter = 0;
        public override void PostUpdateEquips() {
            HitCounter--;
            if (VineType == -1) {
                VineType = ModContent.ProjectileType<SilvaVine>();
            }
            if (Player.ownedProjectileCounts[VineType] > 0) {
                foreach (Projectile p in Main.ActiveProjectiles) {
                    if (p.type == VineType) {
                        if (p.ModProjectile is SilvaVine sv) {
                            Player.endurance += sv.DamageReduce;
                            Player.Entropy().lifeRegenPerSec += sv.flowerCount * SilvaVine.RegenPerFlower;
                        }
                    }
                }
            }
        }
        public override void OnHurt(Player.HurtInfo info) {
            HitCounter = 2;
        }
    }
}