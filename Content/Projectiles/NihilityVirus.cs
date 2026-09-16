using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Utilities;
using InnoVault;
using InnoVault.GameContent.BaseEntity;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class NihilityVirus : BaseHeldProj
    {
        public List<LightningAdvanced> lightnings = new List<LightningAdvanced>();
        private readonly List<ColoredVertex> vertexCache = new(64);
        private List<Vector2> odp = new List<Vector2>();
        private List<float> odr = new List<float>();
        private List<NPC> targets = new List<NPC>();
        public LoopSound sound = null;
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 0.56f;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 64;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            return true;
        }

        public override bool? CanHitNPC(NPC target) {
            if (targets.Contains(target)) return null;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(ModContent.BuffType<VoidVirus>(), 360);
            CEUtils.PlaySound("nvspark", Main.rand.NextFloat(0.6f, 1.4f), target.Center, 4, 1f);
        }

        public override bool PreUpdate() {
            if (lightnings.Count == 0) {
                for (int i = 0; i < 9; i++) {
                    lightnings.Add(new LightningAdvanced(Projectile.Center, Projectile.Center));
                }
            }
            return true;
        }

        private void FindTarget() {
            targets.Clear();
            float maxDistance = 640f + Owner.Entropy().WeaponBoost * 200f;
            float maxDistanceSq = maxDistance * maxDistance;

            List<(NPC npc, float distSq)> candidateList = new();

            foreach (var npc in Main.ActiveNPCs) {

                if (npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy(Projectile)) {
                    continue;
                }

                if (targets.Contains(npc)) {
                    continue;
                }

                float distSq = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (distSq <= maxDistanceSq) {
                    candidateList.Add((npc, distSq));
                }
            }

            if (candidateList.Count > 0) {
                //排序选最近的8个
                candidateList.Sort((a, b) => a.distSq.CompareTo(b.distSq));

                for (int i = 0; i < candidateList.Count && targets.Count < 8; i++) {
                    targets.Add(candidateList[i].npc);
                }
            }
        }

        public override void AI() {
            Projectile.timeLeft = 4;

            if (DownLeft && Projectile.ai[0] == 0) {
                FindTarget();
                Projectile.velocity = (InMousePos - Projectile.Center) * 0.12f;
                //这个检查防止弹幕的第一帧和玩家重叠，导致角度计算出现不变的0度弧度
                if (Projectile.Center == Owner.Center) {
                    Projectile.position += Projectile.velocity;
                }
            }
            else {
                targets.Clear();
                Projectile.position += Owner.velocity;
                Projectile.ChasingBehavior(Owner.Center, 36);
                if (Projectile.Distance(Owner.Center) < Projectile.width) {
                    Projectile.Kill();
                }
            }

            NetUpdate();

            Owner.direction = Projectile.Center.X + Projectile.velocity.X > Owner.Center.X ? 1 : -1;
            Owner.itemTime = 9;
            Owner.itemAnimation = 9;
            Owner.itemRotation = ((Projectile.Center - Owner.Center) * Owner.direction).ToRotation();
            SetHeld();

            if (Main.GameUpdateCount % 12 == 0) {
                Owner.manaRegenDelay = 30;
                Projectile.ai[0] = 0;
                if (!Owner.CheckMana(Owner.HeldItem.mana, true)) {
                    Projectile.ai[0] = 1;
                }
            }

            Projectile.rotation += 0.16f * Owner.direction;
            CEUtils.recordOldPosAndRots(Projectile, ref odp, ref odr, 12);

            if (VaultUtils.isServer) {
                return;
            }

            VaultUtils.ClockFrame(ref Projectile.frame, 3, 3);

            if (sound == null) {
                sound = new LoopSound(CalamityEntropy.ealaserSound2);
                sound.play();
            }
            sound.setVolume_Dist(Projectile.GetOwner().Center, 200, 1600, 0.5f);
            sound.timeleft = 2;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.SourceDamage *= 1 + (8 - targets.Count) / 8f;
        }

        public void Drawlightning(int index, float width, float lightSize) {
            var points = lightnings[index].GetPoints();
            if (points == null || points.Count < 2) {
                return;
            }

            Texture2D lightning = CEUtils.getExtraTex("Lightning");
            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied, SamplerState.LinearWrap);

            vertexCache.Clear();
            float p = -Main.GlobalTimeWrappedHourly * 2.5f;
            float scale = 10f * Projectile.scale * lightSize * (1f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f)) * width; //脉动效果

            //增强路径随机性，模拟扭曲
            List<Vector2> randomizedPoints = new List<Vector2>(points);
            for (int i = 1; i < randomizedPoints.Count - 1; i++) {
                float glitch = Main.rand.NextFloat(-8f, 8f); //更大随机偏移
                randomizedPoints[i] += new Vector2(glitch, glitch * 0.5f);
                //模拟分支效果
                if (Main.rand.NextBool(10) && i < randomizedPoints.Count - 2) {
                    Vector2 randSmd = new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-10f, 10f)) * lightSize * 2.6f;
                    randomizedPoints.Insert(i + 1, randomizedPoints[i] + randSmd);
                }
            }

            for (int i = 1; i < randomizedPoints.Count; i++) {
                Vector2 dir = randomizedPoints[i] - randomizedPoints[i - 1];
                Vector2 offset = dir.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * scale;

                Vector2 basePos = randomizedPoints[i] - Main.screenPosition;
                //紫色主题渐变与毛刺效果
                float brightness = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f + i); //更快闪烁
                float alpha = (float)(i / (float)randomizedPoints.Count);
                Color b = new Color(180, 100, 255, alpha * brightness); //紫色主题

                vertexCache.Add(new ColoredVertex(basePos + offset, new Vector3(p, 1, 1), b));
                vertexCache.Add(new ColoredVertex(basePos - offset, new Vector3(p, 0, 1), b));

                p += (dir.Length() / lightning.Width) * 0.6f; //调整流动速度
            }

            if (vertexCache.Count >= 3) {
                Main.graphics.GraphicsDevice.Textures[0] = lightning;
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexCache.ToArray(), 0, vertexCache.Count - 2);
            }

            //增强光球效果，添加腐蚀粒子
            Texture2D light = CEExtraAssets.lightball;
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            Vector2 endPos = randomizedPoints[^1] - Main.screenPosition;
            //绘制多层光晕，模拟腐蚀
            Main.spriteBatch.Draw(light, endPos, null, new Color(150, 50, 255, 0.2f), 0f, light.Size() / 2f, width * 0.25f * lightSize, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(light, endPos, null, new Color(180, 100, 255, 0.4f), 0f, light.Size() / 2f, width * 0.18f * lightSize, SpriteEffects.None, 0f);
            //绘制主光球
            Main.spriteBatch.Draw(light, endPos, null, new Color(200, 150, 255), 0f, light.Size() / 2f, width * 0.12f * lightSize, SpriteEffects.None, 0f);
            //添加腐蚀粒子效果
            //for (int i = 0; i < 13; i++)
            //{
            //    Vector2 particleOffset = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.Next(11, 62) * lightSize;
            //    float particleScale = Main.rand.NextFloat(0.05f, 0.1f) * lightSize;
            //    Main.spriteBatch.Draw(light, endPos + particleOffset, null, new Color(180, 100, 255, 0.3f), 0f, light.Size() / 2f, particleScale, SpriteEffects.None, 0f);
            //}

            Main.spriteBatch.UseBlendState(BlendState.AlphaBlend); //恢复默认混合状态
        }

        public override bool PreDraw(ref Color lightColor) {
            Player player = Projectile.GetOwner();
            Vector2 opos = player.MountedCenter + player.gfxOffY * Vector2.UnitY;
            Vector2 dir = (Projectile.Center - opos).SafeNormalize(Vector2.Zero);
            Vector2 handPos = opos + dir * 120f * player.HeldItem.scale;

            lightnings[8].Update(handPos, Projectile.Center);
            Drawlightning(8, 1.6f, 1.5f); //紫色主题主闪电

            int lc = 0;
            Vector2 projCenter = Projectile.Center;

            foreach (NPC npc in targets) {
                Vector2 npcCenter = npc.Center;
                if (Vector2.DistanceSquared(lightnings[lc].Point2, npcCenter) > 24 * 24) {
                    for (int i = 0; i < 48; i++)
                        lightnings[lc].Update(projCenter, npcCenter);
                }
                lightnings[lc].Update(projCenter, npcCenter);
                Drawlightning(lc, 2f - (targets.Count * 0.18f), 1.6f); //调整宽度和光球大小
                lc++;
            }

            Texture2D tex = Projectile.GetTexture();
            Rectangle frame = CEUtils.GetCutTexRect(tex, 4, Projectile.frame, false);
            Vector2 origin = new Vector2(78, 80) * 0.5f;

            float apStep = 1f / odp.Count;
            float ap = 0f;
            for (int i = 0; i < odp.Count; i++) {
                Main.spriteBatch.Draw(tex, odp[i] - Main.screenPosition, frame, Color.White * ap * 0.4f, odr[i], origin, 1f, SpriteEffects.None, 0f);
                ap += apStep;
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame
                , new Color(200, 150, 255), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}