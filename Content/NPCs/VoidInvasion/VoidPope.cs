using CalamityEntropy.Content.NPCs.FriendFinderNPC;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidInvasion
{
    [AutoloadBossHead]
    public class VoidPope : ModNPC
    {
        public int seed = -1;
        public override void OnSpawn(IEntitySource source)
        {
            seed = Main.rand.Next(0, 10000);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            this.HideFromBestiary();

            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.CalamityEntropy.VoidPopeBestiary")
            });
        }

        public override void SetDefaults()
        {
            NPC.boss = true;
            NPC.width = 110;
            NPC.height = 110;
            NPC.damage = 136;
            if (Main.expertMode)
            {
                NPC.damage += 20;
            }
            if (Main.masterMode)
            {
                NPC.damage += 20;
            }
            NPC.defense = 60;
            NPC.lifeMax = 2800000;
            // 难度轴按裁定表收敛：死亡→大师、复仇→专家
            if (Main.masterMode)
            {
                NPC.damage += 20;
            }
            else if (Main.expertMode)
            {
                NPC.damage += 20;
            }
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCHit1;
            NPC.value = 200000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.Entropy().VoidTouchDR = 0.9f;
            NPC.dontCountMe = true;
            NPC.defense = 100;
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/RepBossTrack");
            }
        }


        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(seed);

        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            seed = reader.ReadInt32();
        }

        public Random random = null;
        public bool spawnHands = true;
        public enum AttackAIStyle
        {
            Idle,
            Melee,
            VoidLightball,
            Circle
        }
        public int aichange = 0;
        public AttackAIStyle aitype = AttackAIStyle.Melee;
        public float circleCounter = 0;
        public float circlespeed = 0;
        public override void AI()
        {
            if (spawnHands)
            {
                spawnHands = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<VoidPopeHand>(), 0, NPC.whoAmI, 1);
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<VoidPopeHand>(), 0, NPC.whoAmI, -1);

                }
            }
            if (seed >= 0)
            {
                if (random == null)
                {
                    random = new Random(seed);
                }
            }
            if (aitype == AttackAIStyle.Circle)
            {
                circleCounter += circlespeed * 0.38f;
                if (aichange < 2.5f * 60)
                {
                    circlespeed += 0.01f;
                    circlespeed *= 0.97f;
                }
                else
                {
                    circlespeed *= 0.99f;
                }
            }
            if (random != null)
            {
                if (!NPC.HasValidTarget)
                {
                    NPC.target = NPC.FindClosestPlayer();
                }
                if (NPC.HasValidTarget)
                {
                    Player target = NPC.target.ToPlayer();
                    NPC.velocity += (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 0.44f;
                    NPC.velocity *= 0.98f;
                    aichange++;
                    if (aitype == AttackAIStyle.Idle)
                    {
                        aitype = AttackAIStyle.Melee;
                        aichange = 0;
                    }
                    if (aitype == AttackAIStyle.Melee && aichange > 4 * 60)
                    {
                        aitype = AttackAIStyle.VoidLightball;
                        aichange = 0;
                    }
                    if (aitype == AttackAIStyle.VoidLightball && aichange > 8 * 60)
                    {
                        aitype = AttackAIStyle.Circle;
                        aichange = 0;
                        circleCounter = 0;
                        circlespeed = 0;
                    }
                    if (aitype == AttackAIStyle.Circle && aichange > 4 * 60)
                    {
                        aitype = AttackAIStyle.Melee;
                        aichange = 0;
                    }
                }
            }
            if (!NPC.HasValidTarget)
            {
                aitype = AttackAIStyle.Idle;
            }

        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[NPC.type].Value;
            Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, Color.White, NPC.rotation, tex.Size() / 2, NPC.scale, SpriteEffects.None);
            return false;
        }

    }
}
