using CalamityEntropy.Assets.Register;
using CalamityEntropy.Core.CalamityRef;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Content.Projectiles.Cruiser;
using CalamityEntropy.Content.Projectiles.Prophet;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Prophet
{
    public class OlderCruiserAIGNPC
    {
        public void SendExtraAI(NPC npc, BinaryWriter writer)
        {
            writer.Write(this.speedMuti);
            writer.Write(this.targetSpeed);
            writer.Write(this.slowDownTime);
            writer.Write(this.nrc);
            writer.Write(this.aitype);
            writer.Write(this.changeCounter);
            writer.Write(this.maxDistanceTarget);
            writer.Write(this.rotDist);
            Utils.WriteVector2(writer, this.rotPos);
            writer.Write(this.circleDir);
            writer.Write(this.flag);
        }

        public void ReceiveExtraAI(NPC npc, BinaryReader reader)
        {
            this.speedMuti = reader.ReadSingle();
            this.targetSpeed = reader.ReadSingle();
            this.slowDownTime = reader.ReadInt32();
            this.nrc = reader.ReadInt32();
            this.aitype = reader.ReadSingle();
            this.changeCounter = reader.ReadInt32();
            this.maxDistanceTarget = reader.ReadInt32();
            this.rotDist = reader.ReadInt32();
            this.rotPos = Utils.ReadVector2(reader);
            this.circleDir = reader.ReadInt32();
            this.flag = reader.ReadBoolean();
        }

        public void changeAi()
        {
            if (this.aitype == 2f)
            {
                this.rotDist = 900;
            }
            this.circleDir = Main.rand.Next(0, 2);
            if (this.circleDir == 0)
            {
                this.circleDir = -1;
            }
            this.flag = false;
        }



        public bool PreAI(NPC NPC)
        {
            noaitime--;
            if (this.phase == 1 && NPC.life < NPC.lifeMax / 2)
            {
                NPC.dontTakeDamage = true;
                if (NPC.life < NPC.lifeMax / 2)
                {
                    NPC.life = NPC.lifeMax / 2 - 1;
                }
            }
            else
            {
                NPC.dontTakeDamage = false;
            }
            if (this.phase == 2)
            {
                this.phaseTrans++;
                if (this.phaseTrans < 120)
                {
                    if (NPC.life < NPC.lifeMax / 2)
                    {
                        NPC.life = NPC.lifeMax / 2 - 1;
                    }
                    //P2转场120tick口部3向Void,Opacity 0.7,跟CruiserHead尾焰同款
                    var spawnPos = NPC.Center - Utils.ToRotationVector2(NPC.rotation) * -14f;
                    //PRT_Void Opacity/vd spawn后赋,旧VoidParticles原值
                    var p2 = PRTLoader.NewParticle<PRT_Void>(spawnPos, Utils.ToRotationVector2(NPC.rotation) * -3f, Color.White, 1f);
                    p2.Opacity = 0.7f;
                    p2 = PRTLoader.NewParticle<PRT_Void>(spawnPos, Utils.RotatedBy(Utils.ToRotationVector2(NPC.rotation), (double)MathHelper.ToRadians(70f), default(Vector2)) * -3f, Color.White, 1f);
                    p2.Opacity = 0.7f;
                    p2 = PRTLoader.NewParticle<PRT_Void>(spawnPos, Utils.RotatedBy(Utils.ToRotationVector2(NPC.rotation), (double)MathHelper.ToRadians(-70f), default(Vector2)) * -3f, Color.White, 1f);
                    p2.Opacity = 0.7f;
                }
            }
            else if (this.aitype == 1f || this.aitype == 2f)
            {
                //aitype1/2待机也吐Void,没phaseTrans门槛,密度比P2转场低
                var spawnPos = NPC.Center - Utils.ToRotationVector2(NPC.rotation) * -14f;
                var p3 = PRTLoader.NewParticle<PRT_Void>(spawnPos, Utils.ToRotationVector2(NPC.rotation) * -3f, Color.White, 1f);
                p3.Opacity = 0.7f;
                p3 = PRTLoader.NewParticle<PRT_Void>(spawnPos, Utils.RotatedBy(Utils.ToRotationVector2(NPC.rotation), (double)MathHelper.ToRadians(70f), default(Vector2)) * -3f, Color.White, 1f);
                p3.Opacity = 0.7f;
                p3 = PRTLoader.NewParticle<PRT_Void>(spawnPos, Utils.RotatedBy(Utils.ToRotationVector2(NPC.rotation), (double)MathHelper.ToRadians(-70f), default(Vector2)) * -3f, Color.White, 1f);
                p3.Opacity = 0.7f;
            }
            if (this.phase == 1 && this.aitype != 2f && NPC.life < NPC.lifeMax / 2)
            {
                this.phase = 2;
                if (Main.netMode != 1)
                {
                    int ag = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Utils.ToRotationVector2(MathHelper.ToRadians((float)ag)) * 26f, ModContent.ProjectileType<RuneBulletHostile>(), NPC.damage / 7, 0f, -1, 0f, 0f, 0f);
                        ag += 36;
                    }
                }
                NPC.defense = 0;
                NPC.width = 156;
                NPC.height = 156;
                foreach (NPC j in Main.npc)
                {
                    if (j.realLife == NPC.whoAmI)
                    {
                        j.defense = 4;
                        // 段体的灾厄 DR=0.1 写入删除(灾厄减伤体系随脱钩退场,彩蛋本体保留)
                        if (j.ai[2] <= 8f && j.ai[2] > 4f)
                        {
                            j.width = 26;
                            j.height = 26;
                        }
                        if (j.ai[2] > 8f)
                        {
                            j.active = false;
                        }
                        if (Main.dedServ)
                        {
                            NetMessage.SendData(23, -1, -1, null, j.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
            }
            // 原灾厄 Exo 巨石屏效(monolithExoShader)删除:纯灾厄视觉,天顶彩蛋本体保留
            if (NPC.ai[0] > 10f)
            {
                Player player = Main.player[Main.myPlayer];
                Vector2 mp = NPC.Center;
                if (this.aitype == 4f)
                {
                    mp = this.rotPos;
                }
            }
            this.jaslowdown *= 0.8f;
            NPC.netUpdate = true;
            if (this.slowDownTime > 0)
            {
                this.speed += (this.targetSpeed * 0.15f - this.speed) * 0.07f;
                this.slowDownTime--;
            }
            else
            {
                this.speed += (this.targetSpeed - this.speed) * 0.07f;
            }
            if (NPC.ai[0] > 180f)
            {
                this.maxDistance = (int)((float)this.maxDistance + (float)(this.maxDistanceTarget - this.maxDistance) * 0.01f);
            }
            else
            {
                this.maxDistance = (int)((float)this.maxDistance + (float)(this.maxDistanceTarget - this.maxDistance) * 0.002f);
            }
            NPC.ai[0] += 1f;
            for (int k = 0; k < NPC.buffTime.Length; k++)
            {
                NPC.buffTime[k] = 0;
            }
            int closestPlayerIndexd = NPC.FindClosestPlayer();
            NPC.target = closestPlayerIndexd;
            int targetPlayerIndex = NPC.target;
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.5f, 0.5f, 1f);
            if (NPC.HasValidTarget)
            {
                if (CEUtils.getDistance(NPC.Center, NPC.target.ToPlayer().Center) < 210f && this.mouthRot >= -0.1f)
                {
                    this.bite = true;
                }
                Player targetPlayerr = Main.player[targetPlayerIndex];
                if (this.aitype == 0f)
                {
                    this.maxDistanceTarget = 1850;
                    if (NPC.ai[1] == 1f)
                    {
                        if (this.phase == 1)
                        {
                            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.028f, false);
                            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 1.25f.ToRadians(), true);
                        }
                        else
                        {
                            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.025f, false);
                            NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.7f.ToRadians(), true);
                        }
                        NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed * this.speedMuti;
                        this.targetSpeed = 18f;
                        this.nrc++;
                        if (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) < 200f || (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) < 380f && this.phase == 2) || this.nrc > 160)
                        {
                            NPC.ai[1] = 0f;
                            this.nrc = 0;
                        }
                        if (this.changeCounter > 2)
                        {
                            this.aitype = (float)Main.rand.Next(0, 3);
                            this.changeAi();
                            this.changeCounter = 0;
                            this.nrc = 0;
                        }
                    }
                    if (NPC.ai[1] == 0f)
                    {
                        NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed * this.speedMuti;
                        this.targetSpeed = 30f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.2f.ToRadians(), true);
                        this.nrc++;
                        if (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) > 1400f || this.nrc > 100)
                        {
                            NPC.ai[1] = 1f;
                            this.changeCounter++;
                            this.nrc = 0;
                            this.slowDownTime = 60;
                            this.tjv = 1;
                        }
                    }
                }
                else if (this.aitype == 1f)
                {
                    this.maxDistanceTarget = 1400;
                    this.changeCounter++;
                    this.targetSpeed = 70f;
                    Vector2 targetPos = targetPlayerr.Center + Utils.RotatedBy(Utils.SafeNormalize(NPC.Center - targetPlayerr.Center, new Vector2(1f, 0f)) * 800f, (double)MathHelper.ToRadians((float)(20 * this.circleDir)), default(Vector2));
                    NPC.rotation = Utils.ToRotation(targetPos - NPC.Center);
                    NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed;
                    if (this.tjv == 0 && !this.jv && this.jaslowdown < 0.01f)
                    {
                        this.tjv = 1;
                    }
                    if (this.changeCounter > 250)
                    {
                        this.changeCounter = 0;
                        this.aitype = 0f;
                        this.changeAi();
                    }
                }
                else if (this.aitype == 2f)
                {
                    Main.LocalPlayer.wingTime = (float)Main.LocalPlayer.wingTimeMax;
                    if (this.changeCounter > 10)
                    {
                        this.maxDistanceTarget = this.rotDist + 400;
                    }
                    this.changeCounter++;
                    if (this.changeCounter == 1)
                    {
                        this.targetSpeed = 10f;
                        this.rotDist = 1000;
                        this.rotPos = targetPlayerr.Center + new Vector2((float)Main.rand.Next(-800, 801), (float)Main.rand.Next(-800, 801));
                    }
                    if (this.changeCounter == 60)
                    {
                        this.targetSpeed = 50f;
                    }
                    if (this.changeCounter == 200 && Main.netMode != 1)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CruiserLaserMouth>(), (int)((float)NPC.damage / 7f), 0f, -1, (float)NPC.whoAmI, 0f, 0f);
                    }
                    if (this.changeCounter > 260)
                    {
                        this.rotDist--;
                    }
                    Vector2 targetPosd = this.rotPos + Utils.RotatedBy(Utils.SafeNormalize(NPC.Center - this.rotPos, new Vector2(1f, 0f)) * (float)this.rotDist, (double)MathHelper.ToRadians((float)(20 * this.circleDir)), default(Vector2));
                    NPC.rotation = Utils.ToRotation(targetPosd - NPC.Center);
                    NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed;
                    if (this.tjv == 0 && !this.jv && this.jaslowdown < 0.007f && this.changeCounter < 800)
                    {
                        this.tjv = 1;
                    }
                    if (this.changeCounter > 950)
                    {
                        this.rotDist += 4;
                        this.maxDistanceTarget = 2000;
                        this.targetSpeed = 20f;
                    }
                    if (this.changeCounter > 1200)
                    {
                        this.changeCounter = 0;
                        this.aitype = 0f;
                        this.changeAi();
                    }
                }
                if (this.aitype == 3f)
                {
                    this.maxDistanceTarget = 5000;
                    if (this.flag)
                    {
                        this.speed = 60f;
                        this.targetSpeed = 60f;
                        this.changeCounter++;
                        if (!CEUtils.isAir(NPC.Center + Utils.ToRotationVector2(NPC.rotation) * 240f, false) && this.changeCounter < 40)
                        {
                            this.changeCounter = 40;
                        }
                        if (this.changeCounter > 150)
                        {
                            this.aitype = -1f;
                            this.changeCounter = 0;
                            this.changeAi();
                        }
                        if (this.changeCounter < 40)
                        {
                            if (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) < 240f)
                            {
                                targetPlayerr.velocity = Vector2.Zero;
                                targetPlayerr.Center = NPC.Center + Utils.ToRotationVector2(NPC.rotation) * 120f;
                                // 原灾厄全局屏震(逐帧置强度)改自有 ScreenShaker:仅受害者本端,复用实例逐帧刷新
                                if (targetPlayerr.whoAmI == Main.myPlayer && !Main.dedServ)
                                {
                                    if (this.biteShake == null || !this.biteShake.active)
                                    {
                                        this.biteShake = new ScreenShaker.ScreenShake(Vector2.Zero, 0);
                                        ScreenShaker.AddShake(this.biteShake);
                                    }
                                    this.biteShake.amplitude = Utils.Remap(Main.LocalPlayer.Distance(NPC.Center), 1800f, 1000f, 0f, 4.5f, true) * 4f;
                                }
                                this.mouthRot = -40f;
                                //咬杀10颗Void,Random()每只NPC seed不一致是故意的
                                for (int i3 = 0; i3 < 10; i3++)
                                {
                                    Random r2 = new Random();
                                    var p6 = PRTLoader.NewParticle<PRT_Void>(NPC.Center, new Vector2((float)((r2.NextDouble() - 0.5) * 16.0), (float)((r2.NextDouble() - 0.5) * 16.0)), Color.White, 1f);
                                    p6.Opacity = 1.4f;
                                }
                            }
                        }
                        else
                        {
                            if (this.changeCounter == 40 && Main.netMode != 1)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(null), targetPlayerr.Center, (targetPlayerr.Center - NPC.Center).normalize() * 2, ModContent.ProjectileType<CruiserSlash>(), (int)((float)NPC.damage / 6f), 0f, -1, 0f, 0f, 0f);
                            }
                            if (this.changeCounter < 60)
                            {
                                this.speed = -14f;
                                this.targetSpeed = this.speed;
                            }
                            else
                            {
                                this.targetSpeed = 16f;
                                NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center + Utils.SafeNormalize(NPC.Center - targetPlayerr.Center, Vector2.One) * 1500f - NPC.Center), 0.08f, false);
                            }
                        }
                    }
                    else
                    {
                        this.targetSpeed = 60f;
                        NPC.rotation = Utils.ToRotation(targetPlayerr.Center - NPC.Center);
                        if (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) < 100f)
                        {
                            this.flag = true;
                        }
                    }
                    NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed;
                }
                if (this.aitype == -1f)
                {
                    this.maxDistanceTarget = 4000;
                    if (this.changeCounter == 20 && Main.netMode != 1)
                    {
                        int ag2 = new Random().Next(0, 360);
                        for (int i4 = 0; i4 < 18; i4++)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Utils.ToRotationVector2(MathHelper.ToRadians((float)ag2)) * 20f, ModContent.ProjectileType<RuneBulletHostile>(), NPC.damage / 7, 0f, -1, 0f, 0f, 0f);
                            ag2 += 20;
                        }
                    }
                    this.changeCounter++;
                    this.targetSpeed = 16f;
                    Vector2 targetPos2 = targetPlayerr.Center + Utils.RotatedBy(Utils.SafeNormalize(NPC.Center - targetPlayerr.Center, new Vector2(1f, 0f)) * 800f, (double)MathHelper.ToRadians((float)(20 * this.circleDir)), default(Vector2));
                    NPC.rotation = Utils.ToRotation(targetPos2 - NPC.Center);
                    NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed;
                    if (this.changeCounter > 40)
                    {
                        this.changeCounter = 0;
                        this.aitype = (float)Main.rand.Next(3, 8);
                        this.changeAi();
                    }
                }
                if (this.aitype == 4f)
                {
                    if (this.changeCounter > 10)
                    {
                        this.maxDistanceTarget = this.rotDist + 400;
                    }
                    this.changeCounter++;
                    this.targetSpeed = 10f;
                    this.rotDist = 1800;
                    this.maxDistanceTarget = 800;
                    if (this.changeCounter > 380)
                    {
                        this.maxDistanceTarget = 8000;
                    }
                    if (this.changeCounter == 1)
                    {
                        this.rotPos = NPC.Center;
                        this.maxDistanceTarget = 1700;
                        this.bite = true;
                        if (Main.netMode != 1)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CruiserBlackhole>(), (int)((float)NPC.damage / 9f), 0f, -1, 0f, 0f, 0f);
                        }
                    }
                    if (this.changeCounter > 450)
                    {
                        this.changeCounter = 0;
                        this.aitype = -1f;
                        this.changeAi();
                    }
                    if (this.changeCounter > 400)
                    {
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.5f.ToRadians(), true);
                        NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed;
                    }
                    else
                    {
                        Vector2 targetPosd2 = this.rotPos + Utils.RotatedBy(Utils.SafeNormalize(NPC.Center - this.rotPos, new Vector2(1f, 0f)) * (float)this.rotDist, (double)MathHelper.ToRadians((float)(20 * this.circleDir)), default(Vector2));
                        NPC.rotation = Utils.ToRotation(targetPosd2 - NPC.Center);
                        NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed;
                    }
                }
                if (this.aitype == 5f)
                {
                    this.maxDistanceTarget = 1850;
                    if (NPC.ai[1] == 1f)
                    {
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.01f, false);
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.5f.ToRadians(), true);
                        NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed * this.speedMuti;
                        this.targetSpeed = 18f;
                        this.nrc++;
                        if (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) < 240f || (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) < 380f && this.phase == 2) || this.nrc > 160)
                        {
                            NPC.ai[1] = 0f;
                            this.nrc = 0;
                        }
                        if (this.changeCounter > 0)
                        {
                            this.nrc = 0;
                            this.aitype = -1f;
                            this.changeAi();
                            this.changeCounter = 0;
                        }
                    }
                    if (NPC.ai[1] == 0f)
                    {
                        NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed * this.speedMuti;
                        this.targetSpeed = 30f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 1.7f.ToRadians(), true);
                        this.nrc++;
                        if (CEUtils.getDistance(NPC.Center, targetPlayerr.Center) > 1400f || this.nrc > 100)
                        {
                            NPC.ai[1] = 1f;
                            this.changeCounter++;
                        }
                    }
                }
                if (this.aitype == 6f)
                {
                    this.maxDistanceTarget = 6500;
                    this.changeCounter++;
                    if (this.changeCounter > 200)
                    {
                        this.maxDistanceTarget = 6000;
                        this.targetSpeed = 80f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 0.2f.ToRadians(), true);
                    }
                    else if (this.changeCounter > 140)
                    {
                        this.maxDistanceTarget = 6000;
                        this.targetSpeed = 6f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 5f.ToRadians(), true);
                    }
                    else if (this.changeCounter > 80)
                    {
                        this.targetSpeed = 100f;
                    }
                    else
                    {
                        this.targetSpeed = 0f;
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center - NPC.Center), 1.7f.ToRadians(), true);
                    }
                    if (this.changeCounter == 100 && Main.netMode != 1)
                    {
                        int ag3 = new Random().Next(0, 360);
                        for (int i5 = 0; i5 < 10; i5++)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Utils.ToRotationVector2(MathHelper.ToRadians((float)ag3)) * 26f, ModContent.ProjectileType<RuneBulletHostile>(), NPC.damage / 7, 0f, -1, 0f, 0f, 0f);
                            ag3 += 36;
                        }
                    }
                    if (this.changeCounter > 270)
                    {
                        this.aitype = -1f;
                        this.changeAi();
                        this.changeCounter = 0;
                    }
                    NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed * this.speedMuti;
                }
                if (this.aitype == 7f)
                {
                    this.targetSpeed = 0f;
                    NPC.velocity = Utils.ToRotationVector2(NPC.rotation) * this.speed * this.speedMuti;
                    this.changeCounter++;
                    if ((this.changeCounter == 80 || this.changeCounter == 220) && Main.netMode != 1)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, NPC.rotation.ToRotationVector2() * 6, ModContent.ProjectileType<CruiserLaser2>(), NPC.damage / 7, 0f, -1, (float)NPC.whoAmI, 0f, 0f);
                    }
                    if (this.changeCounter < 60)
                    {
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center + targetPlayerr.velocity * 15f - NPC.Center), 1.7f.ToRadians(), true);
                    }
                    if (this.changeCounter > 140 && this.changeCounter < 200)
                    {
                        NPC.rotation = CEUtils.RotateTowardsAngle(NPC.rotation, Utils.ToRotation(targetPlayerr.Center + targetPlayerr.velocity * 15f - NPC.Center), 1.7f.ToRadians(), true);
                    }
                    if (this.changeCounter > 260)
                    {
                        this.aitype = -1f;
                        this.changeAi();
                        this.changeCounter = 0;
                    }
                }
                if (this.phase == 2)
                {
                    if (this.phaseTrans < 120)
                    {
                        this.aitype = -1f;
                    }
                    else if (this.aitype < 2f && this.aitype > -1f)
                    {
                        this.aitype = (float)Main.rand.Next(3, 3);
                        this.changeCounter = 0;
                        this.changeAi();
                    }
                }
            }
            else
            {
                this.notargettime++;
                NPC npc2 = NPC;
                npc2.velocity.Y = npc2.velocity.Y + -1f;
                if (this.notargettime > 190)
                {
                    NPC.active = false;
                }
                NPC.rotation = Utils.ToRotation(NPC.velocity);
            }
            if (this.bite)
            {
                this.mouthRot -= 12f;
                if (this.mouthRot < -48f)
                {
                    this.bite = false;
                }
            }
            else
            {
                this.mouthRot *= 0.9f;
            }
            Lighting.AddLight(NPC.Center, 1f, 1f, 1f);
            Lighting.AddLight(NPC.Center + NPC.velocity, 1f, 1f, 1f);
            if (this.tjv == 1)
            {
                this.tjv = 0;
                this.jv = true;
                if (this.da < 0f)
                {
                    this.da = 1f;
                }
                this.tail_vj = 12f;
            }
            if (this.jv)
            {
                this.da += this.tail_vj;
                this.tail_vj -= 1.5f;
                if (this.da < 0f)
                {
                    this.da = 0f;
                    this.tail_vj = 0f;
                    this.jv = false;
                    this.jaslowdown = 1f;
                    int num = 8;
                    int counts = 2;
                    float speed = 24f;
                    //装灾厄读复仇/死亡,缺席仍走专家/大师兜底。下方原版层不动
                    if (CECal.IsRevengeance)
                    {
                        num = 10;
                        counts = 3;
                        speed = 25f;
                    }
                    if (CECal.IsDeathMode)
                    {
                        num = 10;
                        counts = 4;
                        speed = 26f;
                    }
                    if (Main.expertMode)
                    {
                        num++;
                        speed *= 1.1f;
                    }
                    if (Main.masterMode)
                    {
                        num++;
                        speed *= 1.15f;
                    }
                    if (this.aitype == 1f)
                    {
                        num /= 2;
                        speed *= 0.6f;
                    }
                    if (this.aitype == 2f)
                    {
                        num /= 2;
                        speed *= 0.6f;
                        counts--;
                    }
                    speed *= 0.3f;
                    if (Main.netMode != 1)
                    {
                        float angle = 0f;
                        for (int i6 = 0; i6 < counts; i6++)
                        {
                            for (int j2 = 0; j2 < num; j2++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Utils.ToRotationVector2(angle) * speed, ModContent.ProjectileType<RuneTorrent>(), (int)((float)NPC.damage / 7f), 1f, -1, 0f, 0f, 0f);
                                angle += 6.2831855f / (float)num;
                            }
                            angle += 6.2831855f / (float)num / (float)counts;
                            speed *= 0.7f;
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromAI(null), NPC.Center, Vector2.Zero, ModContent.ProjectileType<VoidExplode>(), (int)((float)NPC.damage / 7f), 0f, -1, 0f, 0f, 0f);
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        SoundStyle sound;
                        sound = new("CalamityEntropy/Assets/Sounds/soulshine", 0);
                        sound.Pitch = 0.6f;
                        SoundEngine.PlaySound(sound, null, null);
                        SoundEngine.PlaySound(SoundID.Item9, null, null);
                    }
                }
            }
            else
            {
                this.ja = 100f / (NPC.velocity.Length() * 3f) * 5f;
                if (this.ja < 0f)
                {
                    this.ja = 0f;
                }
                this.da += (this.ja - this.da) * 0.1f;
            }
            if (this.aitype == 3f || this.aitype == 4f)
            {
                if ((double)this.alpha > 0.3)
                {
                    this.alpha -= 0.05f;
                }
            }
            else if (this.alpha < 1f)
            {
                this.alpha += 0.05f;
            }
            NPC.netUpdate = true;
            NPC.Center = Utils.ToVector2(NPC.Hitbox.Center);
            this.vtodraw = NPC.Center;

            return false;
        }

        public bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                return true;
            }
            if (this.phaseTrans > 120)
            {
                if (this.aitype == 6f)
                {
                    if (this.changeCounter < 80)
                    {
                        CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, NPC.Center, NPC.Center + Utils.ToRotationVector2(NPC.rotation) * 6000f, new Color(166, 111, 255) * ((float)this.changeCounter / 80f) * 0.7f, 8f, 0);
                    }
                    if (this.changeCounter > 140 && this.changeCounter < 200)
                    {
                        CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, NPC.Center, NPC.Center + Utils.ToRotationVector2(NPC.rotation) * 6000f, new Color(166, 111, 255) * ((float)(this.changeCounter - 140) / 60f) * 0.7f, 8f, 0);
                    }
                }
                if (this.aitype == 7f)
                {
                    if (this.changeCounter < 60)
                    {
                        CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, NPC.Center, NPC.Center + Utils.ToRotationVector2(NPC.rotation) * 6000f, new Color(200, 180, 255) * ((float)this.changeCounter / 60f) * 0.7f, 8f, 0);
                    }
                    if (this.changeCounter > 140 && this.changeCounter < 200)
                    {
                        CEUtils.drawLine(Main.spriteBatch, CEExtraAssets.white, NPC.Center, NPC.Center + Utils.ToRotationVector2(NPC.rotation) * 6000f, new Color(200, 180, 255) * ((float)(this.changeCounter - 140) / 60f) * 0.7f, 8f, 0);
                    }
                }
            }
            Texture2D tex = TextureAssets.Npc[NPC.type].Value;
            Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, drawColor, NPC.rotation + MathHelper.PiOver2, tex.Size() / 2f, NPC.scale, SpriteEffects.None);
            return false;
        }
        public float ProgressDraw;

        // 咬合钳制期持续屏震的复用实例(仅受害者本端)
        private ScreenShaker.ScreenShake biteShake;

        private int length = 27;

        public float speedMuti = 1f;

        public float speed = 18f;

        public float targetSpeed = 18f;

        public int noaitime = 0;

        public int notargettime;

        public int tjv;

        private float lastHurt;

        private int ydmg;

        public bool bite;

        public float mouthRot;

        public int tail = -1;

        private bool b_added;

        public int nrc;

        private float ja = 50f;

        private float da = 50f;

        private float tail_vj;

        private bool jv;

        public int phaseTrans;

        public bool flag;

        public int slowDownTime;

        public Vector2 vtodraw;

        public float jaslowdown;

        public float aitype;

        public int changeCounter;

        public int maxDistance = 6000;

        public int maxDistanceTarget = 1900;

        public int rotDist = 900;

        public Vector2 rotPos = Vector2.Zero;

        public int phase = 1;

        public int circleDir = 1;

        public float alpha = 1f;

        private int tdamage;
    }
}