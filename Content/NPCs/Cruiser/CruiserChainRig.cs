using CalamityEntropy.Content.NPCs.Cruiser.Core;
using CalamityEntropy.Content.Projectiles.Cruiser;
using InnoVault;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Cruiser
{
    public partial class CruiserHead
    {
        /// <summary>
        /// 整链骨架落地。每一节都被放到「前一节位置 - 收敛后的朝向 × 间距」上,
        /// 朝向按 0.12 的比例向前一节的朝向收敛,所以它是一条一阶滤波链:
        /// 输入只有已同步的本体坐标与朝向,任何偏差都会被收敛率吃掉,不必过线。
        /// <para>
        /// 原代码把这段循环<b>抄了两遍</b>(死亡演出分支一份、正常分支末尾一份),内容逐字相同,
        /// 迁移时合成一处,两个调用点保持原来的位置
        /// </para>
        /// </summary>
        public void UpdateChain() {
            for (int i = 0; i < bodies.Count; i++) {
                Vector2 oPos;
                float oRot;

                if (i == 0) {
                    oPos = NPC.Center;
                    oRot = NPC.rotation;
                }
                else {
                    oPos = bodies[i - 1];
                    if (i == 1) {
                        oRot = (NPC.Center - bodies[0]).ToRotation();
                    }
                    else {
                        oRot = (bodies[i - 2] - bodies[i - 1]).ToRotation();
                    }
                }
                float rot = (oPos - bodies[i]).ToRotation();
                rot = CEUtils.RotateTowardsAngle(rot, oRot, CruiserDirector.ChainRotateRate, false);
                bodies[i] = oPos - rot.ToRotationVector2() * CruiserDirector.ChainSpacing * NPC.scale;
            }
        }

        /// <summary>
        /// 尾鞭结算。<see cref="CruiserStateContext.TailWhipCue"/> 起手(原 <c>tjv</c>,同帧消费),
        /// 之后鞭毛张角按 12 起、每帧减 1.5 的角速度甩一圈;张角落回 0 以下那一帧打出尾部新星。
        /// <para>
        /// 三个量(张角、角速度、进行中闩锁)都随快照过线:它们是逐帧积分出来的,
        /// 而且新星的触发帧完全由它们决定,不能任其在两端各自漂
        /// </para>
        /// <para>非鞭击期张角按速度推出静息值再一阶逼近,越快张得越窄</para>
        /// </summary>
        public void UpdateFlagellum() {
            if (Context.TailWhipCue) {
                whipActive = true;
                if (flagellumAngle < 0) {
                    flagellumAngle = 1;
                }
                whipSpeed = CruiserDirector.WhipLaunchSpeed;
            }
            if (whipActive) {
                flagellumAngle += whipSpeed;
                whipSpeed -= CruiserDirector.WhipDecel;
                if (flagellumAngle < 0) {
                    flagellumAngle = 0;
                    whipSpeed = 0;
                    whipActive = false;
                    //原代码在这里置 1 之后从不读,照搬
                    jaslowdown = 1;
                    FireTailNova();
                }
            }
            else {
                flagellumRest = CruiserDirector.FlagellumRestNumerator
                    / (NPC.velocity.Length() * CruiserDirector.FlagellumRestSpeedFactor) * CruiserDirector.FlagellumRestScale;
                if (flagellumRest < 0) {
                    flagellumRest = 0;
                }
                flagellumAngle += (flagellumRest - flagellumAngle) * CruiserDirector.FlagellumLerp;
            }
        }

        /// <summary>
        /// 尾部新星:从尾节后方甩出数环虚空星并附一发虚空爆。
        /// 弹幕与随机数只在权威端;音效各端本地放(服务端不放)。
        /// <para>
        /// <b>两处要照搬的怪写法</b>:环间初速每环 ×0.7 是<b>累乘同一个局部变量</b>,
        /// 而天顶世界那一段额外吐星用的是<b>循环跑完之后</b>的那个已经衰减过的初速 ×3;
        /// 另外环与环之间还额外转了 <c>一周 / num / counts</c> 的相位,所以各环星点是错开的
        /// </para>
        /// </summary>
        private void FireTailNova() {
            int num = CruiserDirector.NovaNum;
            int counts = CruiserDirector.NovaCounts;
            float speed = CruiserDirector.NovaSpeed;
            //装灾厄读复仇/死亡,缺席仍走专家/大师兜底。原版专家/大师层叠在其上,顺序不动
            CruiserDirector.NovaScale(ref num, ref counts, ref speed);
            if (CurrentState == CruiserStateIndex.AroundPlayerAndShootVoidStar) {
                counts += CruiserDirector.NovaAroundCountsDelta;
                num /= 2;
                speed *= CruiserDirector.NovaAroundSpeedFactor;
            }

            if (!VaultUtils.isClient && bodies.Count >= 2) {
                Vector2 origin = bodies[bodies.Count - 1]
                    - (bodies[bodies.Count - 2] - bodies[bodies.Count - 1]).SafeNormalize(Vector2.Zero) * CruiserDirector.NovaTailOffset * NPC.scale;
                int starType = ModContent.ProjectileType<VoidStar>();
                float angle = 0;
                for (int i = 0; i < counts; i++) {
                    for (int j = 0; j < num; j++) {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, angle.ToRotationVector2() * speed,
                            starType, (int)(NPC.damage / CruiserDirector.NovaStarDamageDivisor), CruiserDirector.NovaStarKnockback);
                        angle += (float)Math.PI * 2 / num;
                    }
                    angle += (float)Math.PI * 2 / num / counts;
                    speed *= CruiserDirector.NovaRingSpeedDecay;
                }
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, Vector2.Zero,
                    ModContent.ProjectileType<VoidExplode>(), (int)(NPC.damage / CruiserDirector.NovaExplodeDamageDivisor), CruiserDirector.NovaExplodeKnockback);

                if (Main.zenithWorld) {
                    for (int i = 1; i < bodies.Count; i++) {
                        for (int _ = 0; _ < Main.rand.Next(CruiserDirector.NovaZenithCountMin, CruiserDirector.NovaZenithCountMax); _++) {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                bodies[i] - (bodies[i - 1] - bodies[i]).SafeNormalize(Vector2.Zero) * CruiserDirector.NovaTailOffset * NPC.scale,
                                CEUtils.randomRot().ToRotationVector2() * speed * CruiserDirector.NovaZenithSpeedFactor,
                                starType, (int)(NPC.damage / CruiserDirector.NovaStarDamageDivisor), CruiserDirector.NovaStarKnockback);
                        }
                    }
                }
                //新星是决策点
                NPC.netUpdate = true;
            }
            if (Main.netMode != NetmodeID.Server) {
                SoundStyle sound = new SoundStyle("CalamityEntropy/Assets/Sounds/clap");
                sound.Pitch = CruiserDirector.NovaClapPitch;
                SoundEngine.PlaySound(sound);
                SoundEngine.PlaySound(SoundID.Item9);
            }
        }
    }
}
