using CalamityEntropy.Content.Buffs.VoidSlime;
using CalamityEntropy.Content.NPCs.VoidSlime.Core;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidSlime
{
    /// <summary>
    /// 引力场:圈心慢慢跟着本体走,半径朝状态声明的目标靠近,各端同算,圈心和半径随包校正
    /// 对玩家的效果只作用在本地玩家身上,每个客户端管自己:宇宙空间、圈外的吸力和衰变、黑洞的吸力
    /// </summary>
    public partial class VoidSlime
    {
        private void UpdateArena() {
            if (!Context.ArenaActive) {
                return;
            }
            Context.ArenaRadius = MoveToward(Context.ArenaRadius, Context.ArenaRadiusTarget, VSlimeDirector.ArenaRadiusStep);
            if (Context.ArenaFollow) {
                Vector2 diff = NPC.Center - Context.ArenaCenter;
                float dist = diff.Length();
                if (dist > VSlimeDirector.ArenaFollowDeadzone) {
                    Context.ArenaCenter += diff / dist * Math.Min(VSlimeDirector.ArenaFollowSpeed, dist - VSlimeDirector.ArenaFollowDeadzone);
                }
            }
            if (!Main.dedServ) {
                ApplyLocalPlayerEffects();
            }
        }

        private void ApplyLocalPlayerEffects() {
            Player player = Main.LocalPlayer;
            if (!player.active || player.dead || player.ghost || player.Distance(NPC.Center) > VSlimeDirector.MaxFindDistance) {
                return;
            }
            int decay = ModContent.BuffType<CosmicDecay>();
            player.AddBuff(ModContent.BuffType<CosmicSpace>(), VSlimeDirector.CosmicRefreshFrames);

            Vector2 toCenter = Context.ArenaCenter - player.Center;
            float dist = toCenter.Length();
            float over = dist - Context.ArenaRadius;
            if (over > 0f && dist > 1f) {
                Vector2 inward = toCenter / dist;
                player.AddBuff(decay, VSlimeDirector.DecayRefreshFrames);
                float pull = VSlimeDirector.ArenaPullSpeed * MathHelper.Clamp(over / VSlimeDirector.ArenaPullFullDepth, 0.2f, 1f);
                PushPlayer(player, inward * pull);
                //往外冲的速度分量逐帧削掉一半
                float outward = Vector2.Dot(player.velocity, -inward);
                if (outward > 0f) {
                    player.velocity += inward * (outward * 0.5f);
                }
            }

            if (Context.PullStrength > 0f && Context.PullRange > 0f) {
                Vector2 toHole = NPC.Center - player.Center;
                float holeDist = toHole.Length();
                if (holeDist > 40f && holeDist < Context.PullRange) {
                    float falloff = 1f - 0.6f * holeDist / Context.PullRange;
                    PushPlayer(player, toHole / holeDist * (Context.PullStrength * falloff));
                }
            }

            if (Context.HazardDecays && Context.HazardRadius > 0f && CircleHits(NPC.Center, Context.HazardRadius, player.Hitbox)) {
                player.AddBuff(decay, VSlimeDirector.DecayRefreshFrames);
            }
        }

        /// <summary>直接挪位置而不是加速度,吸力不会越积越大;走一遍物块碰撞,不把人塞进墙里</summary>
        private static void PushPlayer(Player player, Vector2 delta) {
            Vector2 allowed = Collision.TileCollision(player.position, delta, player.width, player.height, true, true, (int)player.gravDir);
            player.position += allowed;
        }
    }
}
