using CalamityEntropy.Content.Particles.CalamityPorts;
using CalamityEntropy.Core.CalamityRef;
using InnoVault.PRT;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.FriendFinderNPC
{
    public class DespairStoneFriendly : FriendFindNPC
    {
        public SlotId ChainsawSoundSlot;

        public static readonly SoundStyle ChainsawStartSound = new("CalamityEntropy/Assets/Sounds/chainsaw") { Volume = 0.15f };

        public static readonly SoundStyle ChainsawEndSound = new("CalamityEntropy/Assets/Sounds/chainsaw_break") { Volume = 0.15f };
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.damage = 80;
            NPC.width = 72;
            NPC.height = 72;
            NPC.defense = 38;
            NPC.lifeMax = 800;
            NPC.knockBackResist = 0.1f;
            NPC.value = Item.buyPrice(0, 0, 5, 0);
            NPC.HitSound = SoundID.NPCHit41;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.behindTiles = true;
            NPC.lavaImmune = true;
            NPC.friendly = true;
        }

        public override void AI()
        {
            this.applyCollisionDamage();
            float buzzsawStartTime = 480f;
            if (this.FindTarget() is NPC)
            {
                NPC.ai[1]++;
            }
            if (NPC.ai[2] != 0f) BuzzsawMode();
            if (NPC.ai[1] > buzzsawStartTime)
            {
                if (NPC.ai[2] == 0f)
                {
                    NPC.ai[1] = buzzsawStartTime + 1f; NPC.rotation += NPC.velocity.X * 0.01f;
                    NPC.spriteDirection = -NPC.direction;
                    if (NPC.velocity.Y == 0f || NPC.lavaWet) BuzzsawMode();
                }
            }
            else
            {
                NPC.ai[2] = 0f;
                UnicornAI_DSF(NPC, Mod, true, CECal.IsDeathMode ? 8f : CECal.IsRevengeance ? 6f : 4f, 5f, 0.2f);
            }
            if (NPC.lavaWet) NPC.velocity.Y += -0.8f;
        }
        public override bool CheckActive()
        {
            return false;
        }
        public void BuzzsawMode()
        {
            Entity target = this.FindTarget();
            float distance;
            float speedCap = 10f;
            if (NPC.ai[2] == 0f)
            {
                ChainsawSoundSlot = SoundEngine.PlaySound(ChainsawStartSound, NPC.Center);
                if (NPC.velocity.X < 0f)
                {
                    NPC.ai[2] = -1f;
                }
                else if (NPC.velocity.X > 0f)
                {
                    NPC.ai[2] = 1f;
                }
                else
                {
                    distance = target.Center.X - NPC.Center.X;
                    if (distance != 0f)
                    {
                        NPC.ai[2] = distance / Math.Abs(distance);
                    }
                    else NPC.ai[2] = 1f;
                }
            }
            if (SoundEngine.TryGetActiveSound(ChainsawSoundSlot, out var chainsawSound) && chainsawSound.IsPlaying)
            {
                chainsawSound.Position = NPC.Center;
                chainsawSound.Update();
            }
            if (NPC.velocity.X == 0f)
            {
                if (NPC.velocity.Y > -speedCap) NPC.velocity.Y += -1.66f;
                if (NPC.velocity.Y < -speedCap) NPC.velocity.Y = -speedCap;
            }

            if (NPC.velocity.X == 0f || NPC.velocity.Y == 0f) SpawnSparks();
            else if (target.Center.Y - NPC.Center.Y < 0f && NPC.velocity.Y < 0f && !NPC.lavaWet) NPC.velocity.Y += -0.03f;

            if (Math.Abs(NPC.velocity.X) < speedCap) NPC.velocity.X += 1.66f * NPC.ai[2];
            if (Math.Abs(NPC.velocity.X) > speedCap) NPC.velocity.X = speedCap * NPC.ai[2];

            NPC.rotation += speedCap * 0.03f * NPC.ai[2]; NPC.spriteDirection = -NPC.direction;
            if (NPC.ai[1] > Main.rand.Next(540, 600))
            {
                NPC.ai[1] = 0f;
                NPC.velocity.Y += -3f;
                chainsawSound?.Stop();
                SoundEngine.PlaySound(ChainsawEndSound, NPC.Center);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // 硫火尘暂以原版红火把尘近似；灾厄 gore 资产不复存在，碎块演出删除
            for (int k = 0; k < 5; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedTorch, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 40; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedTorch, hit.HitDirection, -1f, 0, default, 1f);
                }
            }
        }

        public override bool PreKill()
        {
            if (SoundEngine.TryGetActiveSound(ChainsawSoundSlot, out var chainsawSound) && chainsawSound.IsPlaying)
                chainsawSound?.Stop();
            return true;
        }
        public void SpawnSparks()
        {
            Vector2 particleSpawnDisplacement;
            Vector2 splatterDirection;
            if (NPC.velocity.X == 0f)
            {
                particleSpawnDisplacement = new Vector2(24f * NPC.ai[2], 20f);
                splatterDirection = new Vector2(0f, 1f);
            }
            else
            {
                particleSpawnDisplacement = new Vector2(20f * -NPC.ai[2], 24f);
                splatterDirection = new Vector2(-NPC.ai[2], 0f);
            }

            Vector2 bloodSpawnPosition = NPC.Center + particleSpawnDisplacement;

            if (NPC.ai[1] % 4 == 0)
            {
                //HitEffect溅血每4tick 2 SparkCal,10%大号分支,splatterDirection跟ai[2]朝向绑
                for (int i = 0; i < 2; i++)
                {
                    int sparkLifetime = Main.rand.Next(14, 21);
                    float sparkScale = Main.rand.NextFloat(0.8f, 1f) + 1f * 0.05f;
                    Color sparkColor = Color.Lerp(Color.DarkGray, Color.DarkRed, Main.rand.NextFloat(0.7f));
                    sparkColor = Color.Lerp(sparkColor, Color.OrangeRed, Main.rand.NextFloat());

                    if (Main.rand.NextBool(10))
                        sparkScale *= 1.4f;

                    Vector2 sparkVelocity = splatterDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(6f, 13f);
                    sparkVelocity.Y -= 6f;
                    //SparkCal Configure(true,lifetime)走CalamityPort短寿火花,不是Additive桶
                    PRTLoader.NewParticle<PRT_SparkCal>(bloodSpawnPosition, sparkVelocity, sparkColor, sparkScale).Configure(true, sparkLifetime);
                }
            }
        }
        public static void UnicornAI_DSF(NPC npc, Mod mod, bool spin, float bounciness, float speedDetect, float speedAdditive, float bouncy1 = -8.5f, float bouncy2 = -7.5f, float bouncy3 = -7f, float bouncy4 = -6f, float bouncy5 = -8f)
        {
            bool flag = false;
            bool flag2 = false;
            Entity target = npc.ModNPC.FindTarget();
            npc.direction = target.Center.X > npc.Center.X ? 1 : -1;
            int num = 30;
            bool flag3 = false;
            bool flag4 = false;
            if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
            {
                flag3 = true;
                npc.ai[3] += 1f;
            }

            int num2 = (flag ? 10 : 4);
            if (!flag)
            {
                bool flag5 = npc.velocity.Y == 0f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (i != npc.whoAmI && Main.npc[i].active && Main.npc[i].type == npc.type && Math.Abs(npc.position.X - Main.npc[i].position.X) + Math.Abs(npc.position.Y - Main.npc[i].position.Y) < (float)npc.width)
                    {
                        if (npc.position.X < Main.npc[i].position.X)
                        {
                            npc.velocity.X -= 0.05f;
                        }
                        else
                        {
                            npc.velocity.X += 0.05f;
                        }

                        if (npc.position.Y < Main.npc[i].position.Y)
                        {
                            npc.velocity.Y -= 0.05f;
                        }
                        else
                        {
                            npc.velocity.Y += 0.05f;
                        }
                    }
                }

                if (flag5)
                {
                    npc.velocity.Y = 0f;
                }
            }

            if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)num || flag3)
            {
                npc.ai[3] += 1f;
                flag4 = true;
            }
            else if (npc.ai[3] > 0f)
            {
                npc.ai[3] -= 1f;
            }

            if (npc.ai[3] > (float)(num * num2))
            {
                npc.ai[3] = 0f;
            }

            if (npc.justHit)
            {
                npc.ai[3] = 0f;
            }

            if (npc.ai[3] == (float)num)
            {
                npc.netUpdate = true;
            }

            Vector2 vector = new Vector2(npc.Center.X, npc.Center.Y);
            float num3 = target.Center.X - vector.X;
            float num4 = target.Center.Y - vector.Y;
            if ((float)Math.Sqrt(num3 * num3 + num4 * num4) < 200f && !flag4)
            {
                npc.ai[3] = 0f;
            }

            if (!flag && npc.velocity.Y == 0f && Math.Abs(npc.velocity.X) > 3f && ((npc.Center.X < target.Center.X && npc.velocity.X > 0f) || (npc.Center.X > target.Center.X && npc.velocity.X < 0f)))
            {
                npc.velocity.Y -= bounciness;
                // 原灾厄 DespairStone 类型特判分支对本友好 NPC 恒为假（死代码），随脱离灾厄一并删除

                if (flag2)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Water, 0f, -1f);
                    }
                }
            }

            {
                if (npc.velocity.X == 0f)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.ai[0] += 1f;
                        if (npc.ai[0] >= 2f)
                        {
                            npc.direction *= -1;
                            npc.spriteDirection = npc.direction;
                            npc.ai[0] = 0f;
                        }
                    }
                }
                else
                {
                    npc.ai[0] = 0f;
                }

                npc.directionY = -1;
                if (npc.direction == 0)
                {
                    npc.direction = 1;
                }
            }

            if (npc.velocity.Y == 0f || npc.wet || (npc.velocity.X <= 0f && npc.direction < 0) || (npc.velocity.X >= 0f && npc.direction > 0))
            {
                if (Math.Sign(npc.velocity.X) != npc.direction && !flag)
                {
                    npc.velocity.X *= 0.92f;
                }

                MathHelper.Lerp(0.6f, 1f, Math.Abs(Main.windSpeedCurrent));
                Math.Sign(Main.windSpeedCurrent);
                _ = false;
                if (npc.velocity.X < 0f - speedDetect || npc.velocity.X > speedDetect)
                {
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity *= 0.8f;
                    }
                }
                else if (npc.velocity.X < speedDetect && npc.direction == 1)
                {
                    npc.velocity.X += speedAdditive;
                    if (npc.velocity.X > speedDetect)
                    {
                        npc.velocity.X = speedDetect;
                    }
                }
                else if (npc.velocity.X > 0f - speedDetect && npc.direction == -1)
                {
                    npc.velocity.X -= speedAdditive;
                    if (npc.velocity.X < 0f - speedDetect)
                    {
                        npc.velocity.X = 0f - speedDetect;
                    }
                }
            }

            if (npc.velocity.Y >= 0f)
            {
                int num6 = 0;
                if (npc.velocity.X < 0f)
                {
                    num6 = -1;
                }

                if (npc.velocity.X > 0f)
                {
                    num6 = 1;
                }

                Vector2 position = npc.position;
                position.X += npc.velocity.X;
                int num7 = (int)((position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * num6)) / 16f);
                int num8 = (int)((position.Y + (float)npc.height - 1f) / 16f);
                Tile tile = CEUtils.ParanoidTileRetrieval(num7, num8);
                Tile tile2 = CEUtils.ParanoidTileRetrieval(num7, num8 - 1);
                Tile tile3 = CEUtils.ParanoidTileRetrieval(num7, num8 - 2);
                Tile tile4 = CEUtils.ParanoidTileRetrieval(num7, num8 - 3);
                Tile tile5 = CEUtils.ParanoidTileRetrieval(num7 - num6, num8 - 3);
                Tile tile6 = CEUtils.ParanoidTileRetrieval(num7, num8 - 4);
                bool num9 = (float)(num7 * 16) < position.X + (float)npc.width && (float)(num7 * 16 + 16) > position.X;
                bool flag6 = tile.HasUnactuatedTile && !tile.TopSlope && !tile2.TopSlope && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                bool flag7 = tile2.IsHalfBlock && tile2.HasUnactuatedTile;
                bool flag8 = !tile2.HasUnactuatedTile || !Main.tileSolid[tile2.TileType] || Main.tileSolidTop[tile2.TileType] || (tile2.IsHalfBlock && (!tile6.HasUnactuatedTile || !Main.tileSolid[tile6.TileType] || Main.tileSolidTop[tile6.TileType]));
                bool flag9 = !tile3.HasUnactuatedTile || !Main.tileSolid[tile3.TileType] || Main.tileSolidTop[tile3.TileType];
                bool flag10 = !tile4.HasUnactuatedTile || !Main.tileSolid[tile4.TileType] || Main.tileSolidTop[tile4.TileType];
                bool flag11 = !tile5.HasUnactuatedTile || !Main.tileSolid[tile5.TileType];
                if (num9 && (flag6 || flag7) && flag8 && flag9 && flag10 && flag11)
                {
                    float num10 = num8 * 16;
                    if (Main.tile[num7, num8].IsHalfBlock)
                    {
                        num10 += 8f;
                    }

                    if (Main.tile[num7, num8 - 1].IsHalfBlock)
                    {
                        num10 -= 8f;
                    }

                    if (num10 < position.Y + (float)npc.height)
                    {
                        float num11 = position.Y + (float)npc.height - num10;
                        if ((double)num11 <= 16.1)
                        {
                            npc.gfxOffY += npc.position.Y + (float)npc.height - num10;
                            npc.position.Y = num10 - (float)npc.height;
                            if (num11 < 9f)
                            {
                                npc.stepSpeed = 1f;
                            }
                            else
                            {
                                npc.stepSpeed = 2f;
                            }
                        }
                    }
                }
            }

            if (npc.velocity.Y == 0f)
            {
                int num12 = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 2) * npc.direction) + npc.velocity.X * 5f) / 16f);
                int num13 = (int)((npc.position.Y + (float)npc.height - 15f) / 16f);
                int spriteDirection = npc.spriteDirection;
                spriteDirection *= -1;
                if ((npc.velocity.X < 0f && spriteDirection == -1) || (npc.velocity.X > 0f && spriteDirection == 1))
                {
                    if (Main.tile[num12, num13 - 2].HasUnactuatedTile && Main.tileSolid[Main.tile[num12, num13 - 2].TileType])
                    {
                        if (Main.tile[num12, num13 - 3].HasUnactuatedTile && Main.tileSolid[Main.tile[num12, num13 - 3].TileType])
                        {
                            npc.velocity.Y = bouncy1;
                            npc.netUpdate = true;
                        }
                        else
                        {
                            npc.velocity.Y = bouncy2;
                            npc.netUpdate = true;
                        }
                    }
                    else if (Main.tile[num12, num13 - 1].HasUnactuatedTile && !Main.tile[num12, num13 - 1].TopSlope && Main.tileSolid[Main.tile[num12, num13 - 1].TileType])
                    {
                        npc.velocity.Y = bouncy3;
                        npc.netUpdate = true;
                    }
                    else if (npc.position.Y + (float)npc.height - (float)(num13 * 16) > 20f && Main.tile[num12, num13].HasUnactuatedTile && !Main.tile[num12, num13].TopSlope && Main.tileSolid[Main.tile[num12, num13].TileType])
                    {
                        npc.velocity.Y = bouncy4;
                        npc.netUpdate = true;
                    }
                    else if ((npc.directionY < 0 || Math.Abs(npc.velocity.X) > 3f) && (!Main.tile[num12, num13 + 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[num12, num13 + 1].TileType]) && (!Main.tile[num12, num13 + 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[num12, num13 + 2].TileType]) && (!Main.tile[num12 + npc.direction, num13 + 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[num12 + npc.direction, num13 + 3].TileType]))
                    {
                        npc.velocity.Y = bouncy5;
                        npc.netUpdate = true;
                    }
                }
            }

            if (spin)
            {
                npc.rotation += npc.velocity.X * 0.05f;
                npc.spriteDirection = -npc.direction;
            }
        }
    }
}
