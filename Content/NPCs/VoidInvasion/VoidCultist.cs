using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidInvasion
{
    public abstract class VoidCultist : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 34;
            NPC.height = 70;
            NPC.damage = 160;
            NPC.lifeMax = 120000;
            NPC.defense = 100;
            NPC.knockBackResist = 1f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = Item.buyPrice(0, 1, 15, 0);
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.Entropy().VoidTouchDR = 0.6f;
            drawAlpha = 1;
            aiStyle = AIStyle.Idle;
            tryCloseTime = 80;
        }
        public virtual Vector2 drawOffset => new Vector2(0, -4);
        public float MoveSpeed => 0.4f;
        public int tryCloseTime = 0;
        public List<Texture2D> walking = new List<Texture2D>();
        public int walkingFrame = 0;
        public float walkingCount = 0;
        public int AvoidTime = 0;
        public virtual Texture2D getTex()
        {
            return walking[walkingFrame];
        }
        public enum AIStyle
        {
            Idle,
            Closing,
            Avoid,
            Attack
        }
        public AIStyle aiStyle { get; set; }
        public float drawAlpha { get; set; }
        public void findTarget()
        {
            NPC.target = NPC.FindClosestPlayer();
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0)
            {
                return;
            }
            //死亡64颗Void burst,Opacity 0.2~1随机,跟旧VoidCultist HitEffect密度一致
            for (int i = 0; i < 64; i++)
            {
                var p = PRTLoader.NewParticle<PRT_Void>(NPC.Center, CEUtils.randomRot().ToRotationVector2() * ((float)Main.rand.Next(0, 400)) * 0.01f, Color.White, 1f);
                p.Opacity = ((float)Main.rand.Next(20, 100)) * 0.01f;
            }
        }
        public virtual int maxAtkDist => 500;
        public virtual void tryToClose(Vector2 targetPos)
        {
            if (tryCloseTime > 0)
            {
                tryCloseTime--;
            }
            if (NPC.velocity.X == 0 & NPC.velocity.Y == 0)
            {
                NPC.velocity.Y = -2f;
                NPC.Center = NPC.Center - new Vector2(0, 16);
                if (targetPos.X > NPC.Center.X)
                {
                    NPC.velocity.X += MoveSpeed * 8;
                }
                else
                {
                    NPC.velocity.X -= MoveSpeed * 8;
                }
            }
            NPC.velocity.X *= 0.86f;
            if (Math.Abs(targetPos.X - NPC.Center.X) > 40)
            {
                if (targetPos.X > NPC.Center.X)
                {
                    NPC.velocity.X += MoveSpeed;
                }
                else
                {
                    NPC.velocity.X -= MoveSpeed;
                }
            }

            if (CEUtils.getDistance(NPC.Center, targetPos) < maxAtkDist && tryCloseTime <= 0)
            {
                Vector2 v = NPC.Center;
                int vcount = (int)(CEUtils.getDistance(v, targetPos) / 8);
                Vector2 vj = (targetPos - v).SafeNormalize(Vector2.One) * 8;
                for (int i = 1; i < vcount; i++)
                {
                    if (!CEUtils.isAir(v))
                    {
                        return;
                    }
                    v += vj;
                }
                aiStyle = AIStyle.Attack;

            }
        }
        public virtual void attackAI()
        {
            NPC.velocity.X *= 0.86f;
        }
        public virtual void tryAvoid(Vector2 targetPos)
        {
            if (NPC.velocity.X == 0 & NPC.velocity.Y == 0)
            {
                NPC.velocity.Y = -2f;
                NPC.Center = NPC.Center - new Vector2(0, 16);
                if (targetPos.X < NPC.Center.X)
                {
                    NPC.velocity.X += MoveSpeed * 8;
                }
                else
                {
                    NPC.velocity.X -= MoveSpeed * 8;
                }
            }
            NPC.velocity.X *= 0.86f;
            if (targetPos.X < NPC.Center.X)
            {
                NPC.velocity.X += MoveSpeed;
            }
            else
            {
                NPC.velocity.X -= MoveSpeed;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public int idleMoveDir = 0;
        public virtual void Idle()
        {
            if (!(Main.netMode == NetmodeID.MultiplayerClient))
            {
                if (NPC.ai[0] % 60 == 0)
                {
                    if (Main.rand.NextBool(3))
                    {
                        idleMoveDir = Main.rand.Next(-1, 2);
                        NPC.netUpdate = true;
                    }
                }
            }
            NPC.velocity.X *= 0.92f;
            NPC.velocity.X += MoveSpeed * idleMoveDir;
            if (!(Main.netMode == NetmodeID.MultiplayerClient))
            {
                if (Main.rand.NextBool(180))
                {
                    if (NPC.HasValidTarget)
                    {
                        aiStyle = AIStyle.Closing;
                        tryCloseTime = CloseTime;
                        NPC.netUpdate = true;
                    }
                }
            }
        }

        public override bool? CanFallThroughPlatforms()
        {
            if (aiStyle == AIStyle.Closing)
            {
                if (NPC.target >= 0 && NPC.target.ToPlayer().active && NPC.target.ToPlayer().Center.Y > NPC.Center.Y + 32)
                {
                    return true;
                }
            }
            return false;
        }
        public Player Target { get { return NPC.target.ToPlayer(); } }
        public virtual int CloseTime => 60;
        public override void OnSpawn(IEntitySource source)
        {
            NPC.direction = -1;
            if (Main.rand.NextBool())
            {
                NPC.direction = 1;
            }
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)aiStyle);
            writer.Write(tryCloseTime);
            writer.Write(AvoidTime);
            writer.Write(idleMoveDir);
            writer.Write(NPC.noGravity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            aiStyle = (AIStyle)reader.ReadByte();
            tryCloseTime = reader.ReadInt32();
            AvoidTime = reader.ReadInt32();
            idleMoveDir = reader.ReadInt32();
            NPC.noGravity = reader.ReadBoolean();
        }
        public override void AI()
        {
            if (Math.Abs(NPC.velocity.Y) <= 1)
            {
                walkingCount += Math.Abs(NPC.velocity.X * 0.05f);
                if (walkingCount > 1)
                {
                    walkingCount -= 1;
                    walkingFrame += 1;
                    if (walkingFrame >= walking.Count)
                    {
                        walkingFrame = 0;
                    }
                }
            }
            if (NPC.HasValidTarget)
            {
                if (AvoidTime > 0)
                {
                    AvoidTime--;
                    aiStyle = AIStyle.Avoid;
                    if (AvoidTime <= 0)
                    {
                        tryCloseTime = CloseTime;
                        aiStyle = AIStyle.Closing;
                    }
                }


                switch (aiStyle)
                {
                    case AIStyle.Closing: tryToClose(Target.Center); break;
                    case AIStyle.Attack: attackAI(); break;
                    case AIStyle.Avoid: tryAvoid(Target.Center); break;
                    case AIStyle.Idle: Idle(); break;
                    default: break;
                }
            }
            else
            {
                aiStyle = AIStyle.Idle;
                findTarget();
                if (Target != null)
                {
                    NPC.netUpdate = true;
                    aiStyle = AIStyle.Closing;
                }
            }
            if (NPC.velocity.X > 0)
            {
                NPC.direction = 1;
            }
            else if (NPC.velocity.X < 0)
            {
                NPC.direction = -1;
            }
            NPC.ai[0]++;
        }
        public virtual Texture2D BodyTex => null;
        public virtual Texture2D LeftHandTex => null;
        public virtual Texture2D RightHandTex => null;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Main.EntitySpriteDraw(getTex(), NPC.Center + drawOffset * NPC.scale - screenPos, null, drawColor * drawAlpha, NPC.rotation, getTex().Size() / 2, NPC.scale, (NPC.direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None));
            return false;
        }
    }
}