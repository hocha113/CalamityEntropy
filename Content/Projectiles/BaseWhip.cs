using CalamityEntropy.Common;
using CalamityEntropy.Content.Buffs;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public abstract class BaseWhip : ModProjectile
    {
        protected int segments;
        protected float rangeMult;
        public override void SetStaticDefaults() {
            ProjectileID.Sets.IsAWhip[Projectile.type] = true;
        }
        public override void SetDefaults() {
            segments = 12;
            rangeMult = 1.6f;
            Projectile.DefaultToWhip();
        }
        public virtual bool HitboxActive() {
            return Utils.GetLerpValue(0.1f, 0.7f, Projectile.ai[0] / flyTime, true) * Utils.GetLerpValue(0.9f, 0.7f, Projectile.ai[0] / flyTime, true) > 0.5f;
        }
        protected float flyTime;
        public int flyMax = 0;
        public virtual float FlyProgress => Projectile.ai[0] / flyTime;
        public virtual int getFlyTime() {
            if (Projectile.GetOwner().itemAnimationMax > flyMax) {
                flyMax = Projectile.GetOwner().itemAnimationMax;
            }
            return flyMax;
        }
        public bool init = true;
        public override bool PreAI() {
            if (init) {
                init = false;
                if (ownerItem == null) {
                    if (Projectile.GetOwner().HeldItem.ModItem is BaseWhipItem) {
                        ownerItem = Projectile.GetOwner().HeldItem;
                    }
                }
            }
            Player player = Main.player[Projectile.owner];
            flyTime = this.getFlyTime() * Projectile.MaxUpdates;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.ai[0]++;
            Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * (Projectile.ai[0] - 1f);
            Projectile.spriteDirection = (!(Vector2.Dot(Projectile.velocity, Vector2.UnitX) < 0f)) ? 1 : -1;
            Projectile.direction = (!(Vector2.Dot(Projectile.velocity, Vector2.UnitX) < 0f)) ? 1 : -1;

            if (Projectile.ai[0] >= flyTime) {
                Projectile.Kill();
                return false;
            }

            player.heldProj = Projectile.whoAmI;

            if (Projectile.ai[0] == (int)(flyTime / 2f)) {
                Projectile.WhipPointsForCollision.Clear();
                Projectile.FillWhipControlPoints(Projectile, Projectile.WhipPointsForCollision);
                Vector2 position = Projectile.WhipPointsForCollision[^1];
                if (WhipSound != null) {
                    SoundEngine.PlaySound(WhipSound.Value, position);
                }
            }

            if (HitboxActive()) {
                Projectile.WhipPointsForCollision.Clear();
                Projectile.FillWhipControlPoints(Projectile, Projectile.WhipPointsForCollision);
            }

            WhipAI();
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            // TML 把 ProjectileLoader.Colliding 提前到原版 IsAWhip 鞭节判定之前。
            // 这里再恒返回 false 会吞掉整条鞭子的命中, 必须自己做节碰撞。
            List<Vector2> points = Projectile.WhipPointsForCollision;
            points.Clear();
            Projectile.FillWhipControlPoints(Projectile, points);
            int halfW = projHitbox.Width / 2;
            int halfH = projHitbox.Height / 2;
            for (int i = 0; i < points.Count; i++) {
                Point p = points[i].ToPoint();
                projHitbox.Location = new Point(p.X - halfW, p.Y - halfH);
                if (projHitbox.Intersects(targetHitbox)) {
                    return true;
                }
            }
            return false;
        }
        public virtual void WhipAI() {

        }
        public virtual SoundStyle? WhipSound => SoundID.Item153;
        public virtual void ModifyControlPoints(List<Vector2> points) {

        }
        public virtual void ModifyWhipSettings(ref float timeToFlyOut, ref int segs, ref float rangeMul) {
            segs = this.segments;
            rangeMul = this.rangeMult;
        }

        public override void CutTiles() {
            var value = new Vector2(Projectile.width * Projectile.scale * 0.5f, 0f);

            for (int i = 0; i < Projectile.WhipPointsForCollision.Count; i++) {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                Utils.PlotTileLine(Projectile.WhipPointsForCollision[i] - value, Projectile.WhipPointsForCollision[i] + value, Projectile.height * Projectile.scale, DelegateMethods.CutTiles);
            }
        }
        public Vector2 EndPoint {
            get {
                if (Projectile.WhipPointsForCollision == null || Projectile.WhipPointsForCollision.Count == 0)
                    return Main.player[Projectile.owner].Center;
                return Projectile.WhipPointsForCollision[Projectile.WhipPointsForCollision.Count - 1] + new Vector2(Projectile.width * 0.5f, Projectile.height * 0.5f);
            }
        }
        public Item ownerItem = null;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            EGlobalNPC.RemoveAllTags(target);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            if (ownerItem != null && ownerItem.ModItem is BaseWhipItem bw) {
                bool flag = true;
                foreach (var tp in target.GetGlobalNPC<WhipDebuffNPC>().Tags) {
                    if (tp.ItemFullName.Equals(ownerItem.ModItem.FullName)) {
                        flag = false;
                        if (tp.TimeLeft < bw.TagTime) {
                            tp.TimeLeft = bw.TagTime;
                        }
                        return;
                    }
                }
                if (flag) {
                    target.GetGlobalNPC<WhipDebuffNPC>().Tags.Add(new WhipTag(ownerItem.ModItem.FullName, bw.TagTime, bw.TagDamage, bw.TagDamageMult, bw.TagCritChance, getTagEffectName));
                }
            }
        }
        public virtual string getTagEffectName => "";
        public virtual void getFrame(int segCount, int segCounts, ref int frameY, ref int frameHeight, ref Vector2 origin) {
            if (segCount == 0) {
                frameY = 0;
                frameHeight = handleHeight;
                origin = new Vector2(Projectile.GetTexture().Width, handleHeight) * 0.5f;
            }
            else if (segCount == segCounts - 1) {
                frameY = Projectile.GetTexture().Height - endHeight;
                frameHeight = endHeight;
                origin = new Vector2(Projectile.GetTexture().Width, endHeight) * 0.5f;
            }
            else {
                frameY = handleHeight + (segCount - 1) % segTypes * segHeight;
                frameHeight = segHeight;
                origin = new Vector2(Projectile.GetTexture().Width, segHeight) * 0.5f;
            }
        }
        public virtual float getSegScale(int segCount, int segCounts) {
            return 1;
        }

        public virtual void DrawStrings(List<Vector2> points) {
            for (int i = 1; i < points.Count; i++) {
                Vector2 lightPos = Vector2.Lerp(points[i - 1], points[i], 0.5f);
                Color color = Color.Lerp(Lighting.GetColor((int)(lightPos.X / 16f), (int)(lightPos.Y / 16f)).MultiplyRGBA(this.StringColor), this.StringColor, Projectile.light);
                CEUtils.drawLine(points[i - 1], points[i], color, 2 * Projectile.scale);
            }
        }
        public virtual Color modifySegColor(int segCount, Color color) {
            return color;
        }
        public virtual void DrawSegs(List<Vector2> points) {
            for (int i = 0; i < points.Count; i++) {
                int frameY = 0;
                int frameHeight = 0;
                Vector2 origin = Vector2.Zero;
                this.getFrame(i, points.Count, ref frameY, ref frameHeight, ref origin);
                float drawScale = Projectile.scale * this.getSegScale(i, points.Count);
                Vector2 lightPos = i == 0 ? points[i] : Vector2.Lerp(points[i - 1], points[i], 0.5f);
                Color color = Color.Lerp(Lighting.GetColor((int)(lightPos.X / 16f), (int)(lightPos.Y / 16f)), this.StringColor, Projectile.light);
                color = modifySegColor(i, color);
                float rot = 0;
                if (i == points.Count - 1) {
                    rot = (points[i] - points[i - 1]).ToRotation();
                }
                else {
                    rot = (points[i + 1] - points[i]).ToRotation();
                }
                rot -= MathHelper.PiOver2;
                Main.EntitySpriteDraw(Projectile.GetTexture(), points[i] - Main.screenPosition, new Rectangle(0, frameY, Projectile.GetTexture().Width, frameHeight), color, rot, origin, drawScale, Projectile.spriteDirection > 0 ? Microsoft.Xna.Framework.Graphics.SpriteEffects.None : Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);
            }
        }
        public virtual Color StringColor => Color.White;
        public override bool PreDraw(ref Color lightColor) {
            List<Vector2> points = new List<Vector2>();
            points.Clear();
            Projectile.FillWhipControlPoints(Projectile, points);
            DrawStrings(points);
            DrawSegs(points);
            return false;
        }
        public virtual int handleHeight => 20;
        public virtual int segHeight => 16;
        public virtual int endHeight => 20;
        public virtual int segTypes => 2;
    }

}
