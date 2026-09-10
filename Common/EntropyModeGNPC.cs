using CalamityEntropy.Content.NPCs;
using CalamityEntropy.Core.CalamityRef;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    //熵灾模式对原版 Boss 的强化,以及装灾厄时对账本批准的 8 项灾厄 Boss 强化
    public class EntropyModeGNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private static readonly HashSet<int> tetherNPCs = new HashSet<int>();
        private static readonly HashSet<int> slimeGodSlimes = new HashSet<int>();
        private static readonly HashSet<int> dogSegments = new HashSet<int>();
        //3.33 CheckActive 永不脱战:三哨兵本体 + 编织者体节/尾。不并进 tetherNPCs,避免 PreAI 拉绳扩大到体节
        private static readonly HashSet<int> sentinelStayNPCs = new HashSet<int>();

        internal static void FillCalTypes()
        {
            tetherNPCs.Clear();
            slimeGodSlimes.Clear();
            dogSegments.Clear();
            sentinelStayNPCs.Clear();
            AddIfFound(tetherNPCs, CEID.NPC_Signus);
            AddIfFound(tetherNPCs, CEID.NPC_CeaselessVoid);
            AddIfFound(tetherNPCs, CEID.NPC_StormWeaverHead);
            AddIfFound(slimeGodSlimes, CEID.NPC_CrimulanPaladin);
            AddIfFound(slimeGodSlimes, CEID.NPC_SplitCrimulanPaladin);
            AddIfFound(slimeGodSlimes, CEID.NPC_EbonianPaladin);
            AddIfFound(slimeGodSlimes, CEID.NPC_SplitEbonianPaladin);
            AddIfFound(dogSegments, CEID.NPC_DevourerofGodsHead);
            AddIfFound(dogSegments, CEID.NPC_DevourerofGodsBody);
            AddIfFound(dogSegments, CEID.NPC_DevourerofGodsTail);
            AddIfFound(sentinelStayNPCs, CEID.NPC_Signus);
            AddIfFound(sentinelStayNPCs, CEID.NPC_CeaselessVoid);
            AddIfFound(sentinelStayNPCs, CEID.NPC_StormWeaverHead);
            AddIfFound(sentinelStayNPCs, CEID.NPC_StormWeaverBody);
            AddIfFound(sentinelStayNPCs, CEID.NPC_StormWeaverTail);
        }

        internal static void ClearCalTypes()
        {
            tetherNPCs.Clear();
            slimeGodSlimes.Clear();
            dogSegments.Clear();
            sentinelStayNPCs.Clear();
        }

        internal static bool IsSlimeGodSlime(int type)
        {
            return slimeGodSlimes.Contains(type);
        }

        private static void AddIfFound(HashSet<int> set, int type)
        {
            if (type > 0)
            {
                set.Add(type);
            }
        }

        private static bool AnySentinelAlive()
        {
            if (CEID.NPC_Signus > 0 && NPC.AnyNPCs(CEID.NPC_Signus))
            {
                return true;
            }
            if (CEID.NPC_CeaselessVoid > 0 && NPC.AnyNPCs(CEID.NPC_CeaselessVoid))
            {
                return true;
            }
            if (CEID.NPC_StormWeaverHead > 0 && NPC.AnyNPCs(CEID.NPC_StormWeaverHead))
            {
                return true;
            }
            return false;
        }

        public override bool PreAI(NPC npc)
        {
            if (CalamityEntropy.EntropyMode && tetherNPCs.Contains(npc.type))
            {
                Player plr = Main.player[Player.FindClosest(npc.Center, 1000000, 1000000)];
                if (CEUtils.getDistance(npc.Center, plr.Center) > 6400)
                {
                    npc.Center = plr.Center - CEUtils.normalize(plr.Center - npc.Center) * 900;
                }
            }
            return true;
        }

        //3.33:熵灾开且有活玩家时,三哨兵(含编织者体节/尾)永不脱战;史莱姆四体在最外层、不看熵灾
        public override bool CheckActive(NPC npc)
        {
            if (CalamityEntropy.EntropyMode && CERef.Has && sentinelStayNPCs.Contains(npc.type))
            {
                foreach (Player plr in Main.ActivePlayers)
                {
                    if (!plr.dead)
                    {
                        return false;
                    }
                }
            }
            return !slimeGodSlimes.Contains(npc.type);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (CalamityEntropy.EntropyMode && dogSegments.Contains(npc.type) && AnySentinelAlive())
            {
                modifiers.FinalDamage *= 0;
            }
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (CalamityEntropy.EntropyMode && dogSegments.Contains(npc.type) && AnySentinelAlive())
            {
                return false;
            }
            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (CalamityEntropy.EntropyMode)
            {
                if (npc.type == NPCID.WallofFleshEye)
                {
                    if (npc.Entropy().counter % 400 < 60 && npc.Entropy().counter % 6 == 0)
                    {
                        Vector2 lookAt = Main.player[npc.target].Center + Main.player[npc.target].velocity * 10;

                        float velocity = 26;
                        int projectileType = ProjectileID.EyeLaser;
                        int damage = npc.GetProjectileDamage(projectileType);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            {
                                Vector2 projectileVelocity = (lookAt - npc.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.ToRadians(10)) * velocity;
                                Vector2 projectileSpawn = npc.Center + projectileVelocity.SafeNormalize(Vector2.UnitY) * 150f;

                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), projectileSpawn, projectileVelocity, projectileType, damage, 0f, Main.myPlayer, 1f, 0f);
                                Main.projectile[proj].timeLeft = 900;

                                Main.projectile[proj].tileCollide = false;
                            }
                            {
                                Vector2 projectileVelocity = (lookAt - npc.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.ToRadians(-10)) * velocity;
                                Vector2 projectileSpawn = npc.Center + projectileVelocity.SafeNormalize(Vector2.UnitY) * 150f;

                                int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), projectileSpawn, projectileVelocity, projectileType, damage, 0f, Main.myPlayer, 1f, 0f);
                                Main.projectile[proj].timeLeft = 900;

                                Main.projectile[proj].tileCollide = false;
                            }
                        }
                    }
                }
                if (npc.type == NPCID.EyeofCthulhu)
                {
                    if (init)
                    {
                        npc.scale *= 1.4f;
                    }
                }
                if (slimeGodSlimes.Contains(npc.type))
                {
                    npc.MaxFallSpeedMultiplier *= 12;
                }
                if (npc.type == NPCID.PlanterasHook)
                {
                    if (Main.GameUpdateCount % 40 == 0 && Main.rand.NextBool(2))
                    {
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (NPC.plantBoss.ToNPC().target.ToPlayer().Center - npc.Center).normalize() * 22, ProjectileID.SeedPlantera, (int)(npc.GetProjectileDamage(ProjectileID.SeedPlantera) * 0.6f), 2, Main.myPlayer);
                    }
                }
                if (npc.type == NPCID.KingSlime)
                {
                    npc.MaxFallSpeedMultiplier *= 36f;

                    if (this.ksFlag && npc.velocity.Y != 0f && npc.velocity.Y < 0f)
                    {
                        this.ksFlag2 = false;
                        npc.velocity.Y = npc.velocity.Y * 3f;
                        npc.velocity.X = npc.velocity.X * 1.4f;
                        if (Utils.NextBool(Main.rand, 3))
                        {
                            npc.velocity.Y = npc.velocity.Y * 1.4f;
                            this.ksFlag2 = true;
                        }
                    }
                    if (npc.velocity.X != 0f && npc.velocity.Y != 0f && this.ksFlag2 && Math.Sign(npc.velocity.X) != Math.Sign(npc.target.ToPlayer().Center.X - npc.Center.X))
                    {
                        npc.velocity.X = npc.velocity.X * 0.1f;
                        npc.velocity.Y = -4f;
                        this.ksFlag2 = false;
                    }
                    if (!this.ksFlag && npc.velocity.Y == 0f && !Main.dedServ)
                    {
                        CEUtils.PlaySound("ksLand", 1f, new Vector2?(npc.Center), 2, 1f);
                        //脱离灾厄:原灾厄GeneralScreenShakePower距离衰减震屏(1000~2000px内0~12),改用自有等价
                        CEUtils.SetShake(npc.Center, 12f, 2000f);
                    }
                    this.ksFlag = (npc.velocity.Y == 0f);
                    if (npc.velocity.Y == 0f)
                    {
                        this.vyAdd = 0f;
                    }
                    if (npc.velocity.Y != 0f)
                    {
                        this.vyAdd = 0.65f;
                        if (this.ksFlag2)
                        {
                            this.vyAdd = 0.4f;
                        }
                        npc.velocity.Y = npc.velocity.Y + this.vyAdd;
                    }
                    if (SpawnAtHalfLife)
                    {
                        SpawnAtHalfLife = false;
                        Vector2 vector = npc.Center + new Vector2(0, -40 * npc.scale);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.NewNPC(npc.GetSource_FromAI(), (int)vector.X, (int)vector.Y, ModContent.NPCType<TopazJewel>());

                    }
                }
                ApplyCalamityEntropyPostAI(npc);
            }
            init = false;
        }

        private void ApplyCalamityEntropyPostAI(NPC npc)
        {
            if (CEID.NPC_CryogenShield > 0 && npc.type == CEID.NPC_CryogenShield)
            {
                if (npc.Entropy().counter % 22 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int iceBlast = Main.zenithWorld && CEID.Proj_BrimstoneBarrage > 0
                        ? CEID.Proj_BrimstoneBarrage
                        : CEID.Proj_IceBlast;
                    if (iceBlast > 0)
                    {
                        int totalProjectiles = CECal.IsBossRushActive ? 8 : 5;
                        float radians = MathHelper.TwoPi / totalProjectiles;
                        int damage = (int)(npc.GetProjectileDamage(iceBlast) * 0.7f);
                        float velocity = 26f;
                        Vector2 spinningPoint = new Vector2(0f, -velocity);
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 projSpreadRotation = spinningPoint.RotatedBy(radians * k + Main.GlobalTimeWrappedHourly);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + Vector2.Normalize(projSpreadRotation) * 30f, projSpreadRotation, iceBlast, damage, 0f, Main.myPlayer, 0f, 0f, 0f);
                        }
                    }
                }
            }
            if (CEID.NPC_Cryogen > 0 && npc.type == CEID.NPC_Cryogen)
            {
                bool shieldUp = CEID.NPC_CryogenShield > 0 && NPC.AnyNPCs(CEID.NPC_CryogenShield);
                if (!shieldUp && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (npc.Entropy().counter % 600 == 0 && CEID.Proj_IceBomb > 0)
                    {
                        int totalProjectiles = 3;
                        float radians = MathHelper.TwoPi / totalProjectiles;
                        int type = CEID.Proj_IceBomb;
                        int damage = (int)(npc.GetProjectileDamage(type) * 0.7f);
                        float velocity = 2f + npc.ai[0];
                        double angleA = radians * 0.5;
                        double angleB = MathHelper.ToRadians(90f) - angleA;
                        float velocityX = (float)(velocity * Math.Sin(angleA) / Math.Sin(angleB));
                        Vector2 spinningPoint = Main.rand.NextBool() ? new Vector2(0f, -velocity) : new Vector2(-velocityX, -velocity);
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 projSpreadRotation = spinningPoint.RotatedBy(radians * k);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + Vector2.Normalize(projSpreadRotation) * 30f, projSpreadRotation, type, damage, 0f, Main.myPlayer);
                        }
                    }
                    if (npc.Entropy().counter % 6 == 0 && npc.Entropy().counter % 30 == 0 && CEID.Proj_IceBlast > 0)
                    {
                        int type = CEID.Proj_IceBlast;
                        int totalProjectiles = CECal.IsBossRushActive ? 6 : 4;
                        float radians = MathHelper.TwoPi / totalProjectiles;
                        int damage = npc.GetProjectileDamage(type);
                        float velocity = 10f;
                        float projectileVelocityToPass = 28f;
                        Vector2 spinningPoint = new Vector2(0f, -velocity);
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 projSpreadRotation = spinningPoint.RotatedBy(radians * k + Main.GlobalTimeWrappedHourly);
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + Vector2.Normalize(projSpreadRotation) * 30f, projSpreadRotation, type, damage, 0f, Main.myPlayer, 0f, 0f, projectileVelocityToPass);
                        }
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, (npc.target.ToPlayer().Center - npc.Center).normalize() * 20, type, damage, 0f, Main.myPlayer, 0f, 0f, projectileVelocityToPass);
                    }
                }
            }
            if (CEID.NPC_CrabShroom > 0 && npc.type == CEID.NPC_CrabShroom)
            {
                npc.dontTakeDamage = true;
            }
            if (dScFLag && CEID.NPC_DesertScourgeHead > 0 && npc.type == CEID.NPC_DesertScourgeHead && npc.localAI[2] == 1f)
            {
                dScFLag = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (CEID.NPC_DesertNuisanceHead > 0)
                    {
                        NPC.SpawnOnPlayer(npc.FindClosestPlayer(), CEID.NPC_DesertNuisanceHead);
                    }
                    if (CEID.NPC_DesertNuisanceHeadYoung > 0)
                    {
                        NPC.SpawnOnPlayer(npc.FindClosestPlayer(), CEID.NPC_DesertNuisanceHeadYoung);
                    }
                }
            }
            if (CEID.NPC_Crabulon > 0 && npc.type == CEID.NPC_Crabulon)
            {
                npc.MaxFallSpeedMultiplier *= 2f;
                if (npc.velocity.Length() < 40)
                {
                    npc.velocity *= 1.01f;
                }
                if (this.ksFlag && npc.velocity.Y != 0f && npc.velocity.Y < 0f)
                {
                    this.ksFlag2 = false;
                    npc.velocity.Y = npc.velocity.Y * 1.34f;
                    npc.velocity.X = npc.velocity.X * 1.5f;
                    if (Utils.NextBool(Main.rand, 3))
                    {
                        npc.velocity.Y = npc.velocity.Y * 1.4f;
                        this.ksFlag2 = true;
                    }
                }
                if (npc.velocity.X != 0f && npc.velocity.Y != 0f && this.ksFlag2 && Math.Sign(npc.velocity.X) != Math.Sign(npc.target.ToPlayer().Center.X - npc.Center.X))
                {
                    npc.velocity.X = npc.velocity.X * 0.1f;
                    npc.velocity.Y = -4f;
                    this.ksFlag2 = false;
                }
                this.ksFlag = (npc.velocity.Y == 0f);
                if (npc.velocity.Y == 0f)
                {
                    this.vyAdd = 0f;
                }
                if (npc.velocity.Y != 0f)
                {
                    this.vyAdd = 0.65f;
                    if (this.ksFlag2)
                    {
                        this.vyAdd = 0.4f;
                    }
                    npc.velocity.Y = npc.velocity.Y + this.vyAdd;
                }
            }
        }

        public bool SpawnAtHalfLife = true;

        public bool ksFlag;

        public float vyAdd;

        public bool init = true;

        public bool ksFlag2;

        public bool dScFLag = true;
    }

    internal class EntropyModeCalTypes : ICELoader
    {
        public void SetupData()
        {
            EntropyModeGNPC.FillCalTypes();
        }

        public void UnLoadData()
        {
            EntropyModeGNPC.ClearCalTypes();
        }
    }
}
