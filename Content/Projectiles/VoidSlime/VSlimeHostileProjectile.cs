using CalamityEntropy.Content.NPCs.SunMoonGods.Common;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using VoidSlimeNPC = CalamityEntropy.Content.NPCs.VoidSlime.VoidSlime;

namespace CalamityEntropy.Content.Projectiles.VoidSlime
{
    /// <summary>
    /// 虚空史莱姆的敌对弹幕基类,damage 字段存普通档基值
    /// 命中时原版再乘 2 和难度倍率,这里不碰难度
    /// </summary>
    public abstract class VSlimeHostileProjectile : ModProjectile
    {
        /// <summary>依附本体状态的弹幕在出生后这么多帧内不按状态号自毁</summary>
        protected const int StateGraceFrames = 20;

        /// <summary>命中施加的减益,-1 为无</summary>
        public virtual int DebuffType => -1;
        public virtual int DebuffTime => 120;
        public virtual int DefaultTimeLeft => 300;

        public override void SetDefaults() {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = DefaultTimeLeft;
            CooldownSlot = ImmunityCooldownID.Bosses;
            SetExtraDefaults();
        }

        public virtual void SetExtraDefaults() {
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            if (DebuffType >= 0) {
                target.AddDebuffFixed(DebuffType, DebuffTime);
            }
        }

        /// <summary>ai 槽里存的 NPC 下标,类型不对或已失效返回 null</summary>
        protected static VoidSlimeNPC OwnerBoss(float slotValue) {
            int index = (int)slotValue;
            if (index < 0 || index >= Main.maxNPCs) {
                return null;
            }
            NPC npc = Main.npc[index];
            return npc.active ? npc.ModNPC as VoidSlimeNPC : null;
        }

        /// <summary>圆心到判定框最近点的距离不超过半径</summary>
        protected static bool CircleHits(Vector2 center, float radius, Rectangle target) {
            Vector2 nearest = new Vector2(
                MathHelper.Clamp(center.X, target.Left, target.Right),
                MathHelper.Clamp(center.Y, target.Top, target.Bottom));
            return Vector2.DistanceSquared(center, nearest) <= radius * radius;
        }

        /// <summary>只在权威端清,Kill 自带同步</summary>
        public static void ClearAll() {
            if (VaultUtils.isClient) {
                return;
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is VSlimeHostileProjectile) {
                    p.Kill();
                }
            }
        }

        /// <summary>只清某一类,爆发摧毁暗物质球、喷射结束摧毁凝胶球都走它</summary>
        public static void ClearType<T>() where T : VSlimeHostileProjectile {
            if (VaultUtils.isClient) {
                return;
            }
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.ModProjectile is T) {
                    p.Kill();
                }
            }
        }

        /// <summary>存活的本 Boss 敌对弹幕数,连接段的杂波阀读它</summary>
        public static int CountAlive() {
            int count = 0;
            foreach (Projectile p in Main.ActiveProjectiles) {
                if (p.hostile && p.ModProjectile is VSlimeHostileProjectile) {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 弹体通用画法:加法残影和底光,再画实体贴图
        /// 残影和弹体同一张图,宽度不低于弹体的一半
        /// </summary>
        protected void DrawBolt(Texture2D tex, Rectangle frame, Color glow, float opacity, float scale, float glowScale, Vector2? stretch = null) {
            if (opacity <= 0.01f) {
                return;
            }
            Vector2 origin = frame.Size() / 2f;
            Vector2 size = (stretch ?? Vector2.One) * scale;

            GodDraw.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, frame, GodDraw.Add(glow, 0.45f * t * opacity),
                    Projectile.rotation, origin, size * (0.55f + 0.4f * t), SpriteEffects.None, 0f);
            }
            GodDraw.Centered(GodDraw.Glow, Projectile.Center, GodDraw.Add(glow, 0.5f * opacity), 0f, glowScale * scale);
            GodDraw.End();

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * opacity, Projectile.rotation, origin, size, SpriteEffects.None, 0);
        }
    }
}
