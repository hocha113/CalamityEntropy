using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.NPCs.Acropolis.Core;
using CalamityEntropy.Core.AI;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    /// <summary>
    /// 鱼叉。<b>混合型部件</b>:挂在发射架上时位置由本体每帧直写(锚定型),
    /// 发射出去之后靠自己的速度积分(本体型)。两种模式都清掉原版平滑——
    /// 出膛初速 36×scale 远超平滑能消化的 2~4 px/f,而且锁链是从本体的枪口画到这里的,
    /// 两端必须读同一个平滑层级。
    /// <para>
    /// 它不进 <see cref="CEBossNetMotion"/> 的位置预测器:挂架期的位置不是 <c>position + velocity</c>,
    /// 预测器会和直写打架。飞行段很短、且以直写状态收尾,靠快照本身对账就够。
    /// </para>
    /// <para>
    /// 联机:扎墙是<b>决策</b>(会把自己往墙里再插 40 px),只在权威端裁决并立刻过线;
    /// 回收、追瞄、拽拉请求各端都跑。原版的 <c>OnLauncher</c> / <c>Back</c> / <c>Stuck</c> /
    /// <c>PullCD</c> / <c>sVel</c> 全是本地字段从不过线,本轮补进 <see cref="SendExtraAI"/>
    /// </para>
    /// </summary>
    public class Harpoon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            // 图鉴隐藏:原灾厄隐藏扩展的原版等价写法
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.width = 30;
            NPC.height = 30;
            NPC.damage = 32;
            NPC.dontTakeDamage = true;
            NPC.lifeMax = 1400;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = null;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 5;
        }

        /// <summary>宿主实体。<c>ai[0]</c> 存宿主 whoAmI,其余槽位未用</summary>
        public NPC owner => NPC.ai[0] >= 0 ? ((int)NPC.ai[0]).ToNPC() : null;

        private AcropolisMachine OwnerMachine => owner?.ModNPC as AcropolisMachine;

        /// <summary>挂在发射架上</summary>
        public bool OnLauncher = true;
        /// <summary>出膛后的「不回收」帧数,归负才开始往回收</summary>
        public int Back = 0;
        /// <summary>扎在墙里</summary>
        public bool Stuck = false;
        /// <summary>再次扎墙的冷却</summary>
        public int PullCD = 0;
        /// <summary>出膛时的速度快照,扎墙时顺着它再插进去一段</summary>
        public Vector2 sVel = Vector2.Zero;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return owner != null && owner.boss && !OnLauncher;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<MechanicalTrauma>(), 180);
        }

        public override void AI()
        {
            CEBossHost.RunAnchoredPartFrame(NPC);
            PullCD--;
            if (NPC.localAI[1]++ == 0)
            {
                //原代码两个分支都写的 velocity.X,笔误照搬
                if (NPC.velocity.X == 0)
                    NPC.velocity.X = 0.02f;
                if (NPC.velocity.Y == 0)
                    NPC.velocity.X = 0.02f;
            }
            AcropolisMachine am = OwnerMachine;
            if (NPC.ai[0] < 0 || owner == null || !owner.active || am == null)
            {
                NPC.active = false;
                return;
            }
            NPC.scale = owner.scale;
            NPC.damage = owner.damage;

            if (OnLauncher)
            {
                NPC.Center = am.HarpoonPos;
                NPC.rotation = am.harpoon.Seg2Rot;
                Stuck = false;
                NPC.velocity *= 0;
                sVel *= 0;
                return;
            }

            if (sVel == Vector2.Zero)
            {
                sVel = NPC.velocity;
            }
            NPC.rotation = (NPC.Center - ChainTail(am)).ToRotation();

            if (!Stuck && Back-- < 0)
            {
                //回收:朝枪口加速并阻尼,够近就挂回架上
                NPC.noTileCollide = true;
                NPC.velocity += (am.HarpoonPos - NPC.Center).normalize() * AcropolisDirector.HarpoonReturnAccel * NPC.scale;
                NPC.velocity *= AcropolisDirector.HarpoonReturnDrag;
                if (CEUtils.getDistance(NPC.Center, am.HarpoonPos) <= NPC.velocity.Length() + AcropolisDirector.HarpoonReturnCatchPad)
                {
                    OnLauncher = true;
                    NPC.velocity *= 0;
                    if (!VaultUtils.isClient)
                    {
                        NPC.netUpdate = true;
                        owner.netUpdate = true;
                    }
                }
                return;
            }

            if (!owner.HasValidTarget)
            {
                return;
            }
            Player target = owner.target.ToPlayer();

            //扎墙是决策:它会把自己往墙里再插一段,所以只在权威端裁决并立刻过线
            if (!VaultUtils.isClient
                && NPC.noTileCollide
                && CEUtils.getDistance(owner.Center, target.Center) > AcropolisDirector.StickOwnerToTargetMin
                && CEUtils.getDistance(owner.Center, NPC.Center) > AcropolisDirector.StickOwnerToHarpoonMin
                && CEUtils.getDistance(NPC.Center, target.Center) < AcropolisDirector.StickHarpoonToTargetMax
                && CEUtils.CheckSolidTile(NPC.getRect())
                && !Stuck && !am.Jumping && PullCD <= 0)
            {
                PullCD = AcropolisDirector.PullCooldownFrames;
                Stuck = true;
                NPC.Center += sVel.normalize() * AcropolisDirector.StickPenetration;
                CEUtils.PlaySound("ExoHit1", 1, NPC.Center);
                NPC.netUpdate = true;
                owner.netUpdate = true;
            }

            if (!Stuck)
            {
                return;
            }
            //拽拉本身各端都跑:请求里写的三个量全部过线,客户端照样把本体拖过去
            NPC.velocity *= 0;
            if (CEUtils.getDistance(owner.Center, NPC.Center) > AcropolisDirector.PullReleaseDistance)
            {
                am.RequestHarpoonPull();
                Back = 5;
            }
            else
            {
                Back = -1;
                Stuck = false;
                am.ReleaseHarpoonPull();
                if (!VaultUtils.isClient)
                {
                    NPC.netUpdate = true;
                    owner.netUpdate = true;
                }
            }
        }

        /// <summary>锁链尾端:枪口沿发射方向回退 72,绘制与朝向都读它</summary>
        private Vector2 ChainTail(AcropolisMachine am)
            => am.HarpoonPos - am.harpoon.Seg2Rot.ToRotationVector2() * AcropolisDirector.HarpoonChainTail * NPC.scale;

        /// <summary>定长块。原版这个实体完全没有 ExtraAI,五个状态字段从不过线</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(OnLauncher);
            writer.Write(Stuck);
            writer.Write(Back);
            writer.Write(PullCD);
            writer.WriteVector2(sVel);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OnLauncher = reader.ReadBoolean();
            Stuck = reader.ReadBoolean();
            Back = reader.ReadInt32();
            PullCD = reader.ReadInt32();
            sVel = reader.ReadVector2();
            CEBossNetMotion.ClearSmoothing(NPC);
        }

        public override bool CheckActive() => owner == null || !owner.active;

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            npcHitbox = npcHitbox.Center.ToVector2().getRectCentered((npcHitbox.Width * NPC.scale), (npcHitbox.Height * NPC.scale));
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            AcropolisMachine am = OwnerMachine;
            if (OnLauncher || am == null)
                return false;
            //复用 AcropolisMachine 声明的轮廓贴图字段
            Texture2D harpoonOutline = AcropolisMachine.harpoonOutlineTex.Value;
            CEUtils.drawChain(NPC.Center, ChainTail(am), 18, "CalamityEntropy/Content/NPCs/Acropolis/HarpoonChain");
            Texture2D harpoon3 = NPC.getTexture();
            for (float r = 0; r <= 360; r += 60)
            {
                Main.EntitySpriteDraw(harpoonOutline, MathHelper.ToRadians(r).ToRotationVector2() * 2 + NPC.Center - Main.screenPosition, null, Color.OrangeRed, NPC.rotation, new Vector2(70, harpoon3.Height / 2f), NPC.scale, am.dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
            Main.EntitySpriteDraw(harpoon3, NPC.Center - Main.screenPosition, null, drawColor, NPC.rotation, new Vector2(70, harpoon3.Height / 2f), NPC.scale, am.dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            return false;
        }
    }
}
