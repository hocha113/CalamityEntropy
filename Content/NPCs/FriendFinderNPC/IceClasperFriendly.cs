using CalamityEntropy.Content.Projectiles;
using CalamityEntropy.Core.CalamityRef;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.FriendFinderNPC
{
    public class IceClasperFriendly : FriendFindNPC
    {
        public Entity target => this.FindTarget();

        public bool expert = true;
        public bool revenge = true;
        public bool death = true;

        public enum IceClasperAIState
        {
            Shooting,
            Dashing
        }
        public IceClasperAIState CurrentState
        {
            get => (IceClasperAIState)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float RotationIncrease => ref NPC.ai[1];
        public bool checkedRotationDir = false;
        public int rotationDir;

        public ref float TimerForShooting => ref NPC.ai[2];

        public ref float AITimer => ref NPC.ai[3];

        public bool isDashing => (CurrentState == IceClasperAIState.Dashing && AITimer > TimeBeforeDash && AITimer <= TimeBeforeDash + TimeDashing);

        #region Other stats

        public float MaxVelocity = 10f;
        public float DistanceFromPlayer = 500f;

        //装灾厄读死亡/复仇。弹幕间隔补回 3.33 专家中间档 40
        public float AmountOfProjectiles = (CECal.IsDeathMode) ? 2f : (CECal.IsRevengeance) ? 4f : 3f;
        public float TimeBetweenProjectiles = (CECal.IsDeathMode) ? 50f : (CECal.IsRevengeance) ? 35f : (Main.expertMode) ? 40f : 45f;
        public float TimeBetweenBurst = (CECal.IsDeathMode) ? 240f : 180f;
        public float ProjectileSpeed = 26f;

        public float TimeBeforeDash = (CECal.IsRevengeance) ? 100f : 120f;
        public float TimeDashing = 100f;
        public float DashSpeed = 22f;

        #endregion

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            this.HideFromBestiary();
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 6;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3f;
            NPC.noGravity = true;
            NPC.damage = 35;
            NPC.width = 50;
            NPC.height = 50;
            NPC.defense = 12;
            NPC.lifeMax = 900;
            NPC.knockBackResist = 0.25f;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath7;
            NPC.rarity = 2;
            NPC.coldDamage = true;
            NPC.friendly = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rotationDir);
            writer.Write(checkedRotationDir);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            rotationDir = reader.ReadInt32();
            checkedRotationDir = reader.ReadBoolean();
        }

        public override void AI()
        {
            AIMovement(target);
            this.applyCollisionDamage();
            float distToTarget = NPC.Distance(target.Center) + .1f;
            NPC.rotation = NPC.rotation.AngleTowards(NPC.AngleTo(target.Center), (isDashing) ? ((death) ? .0005f : (revenge) ? .0003f : (expert) ? .0002f : .0001f) * distToTarget : .3f);

            Lighting.AddLight(NPC.Center, Color.Cyan.ToVector3());

            switch (CurrentState)
            {
                case IceClasperAIState.Shooting:
                    State_Shooting(target);
                    break;
                case IceClasperAIState.Dashing:
                    State_Dashing(target);
                    break;
            }
        }

        public void AIMovement(Entity player)
        {
            if (!checkedRotationDir)
            {
                rotationDir = (Main.rand.NextBool()).ToDirectionInt();
                checkedRotationDir = true;
                NPC.netUpdate = true;
            }

            Vector2 shootingPos = player.Center + new Vector2(MathF.Cos(RotationIncrease) * rotationDir, MathF.Sin(RotationIncrease) * rotationDir) * DistanceFromPlayer;
            RotationIncrease += (CurrentState == IceClasperAIState.Shooting) ? .02f : .008f;

            NPC.velocity = Vector2.Lerp(NPC.velocity, (shootingPos - NPC.Center).SafeNormalize(Vector2.Zero) * 6f, .1f);
            NPC.velocity = Vector2.Clamp(NPC.velocity, new Vector2(-MaxVelocity, -MaxVelocity), new Vector2(MaxVelocity, MaxVelocity));

            NPC.netUpdate = true;
        }

        public void State_Shooting(Entity player)
        {
            if (NPC.Distance(player.Center) > 800f)
                return;

            AITimer++;

            if (AITimer >= TimeBetweenBurst)
            {
                if (TimerForShooting % TimeBetweenProjectiles == 0)
                {
                    Vector2 vecToPlayer = NPC.SafeDirectionTo(player.Center);
                    Vector2 projVelocity = vecToPlayer * ProjectileSpeed;
                    // 灾厄冰钳召唤弹换自有同主题友方冰锥
                    int type = ModContent.ProjectileType<Icicle>();
                    int damage = NPC.damage;

                    if (Main.myPlayer == NPC.Entropy().friendFinderOwner && target is NPC)
                    {
                        if (death)
                        {
                            for (int i = -16; i < 8; i += 8)
                            {
                                Vector2 spreadVelocity = projVelocity.RotatedBy(MathHelper.ToRadians(i));
                                int projectile = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                    NPC.Center + projVelocity.SafeNormalize(Vector2.Zero) * 10f,
                                    spreadVelocity,
                                    type,
                                    damage,
                                    0f,
                                    Main.myPlayer);
                                Main.projectile[projectile].timeLeft = 300;
                                Main.projectile[projectile].DamageType = DamageClass.Default;
                            }
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            int projectile = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                NPC.Center + projVelocity.SafeNormalize(Vector2.Zero) * 10f,
                                projVelocity,
                                type,
                                damage,
                                0f,
                                Main.myPlayer);
                            Main.projectile[projectile].timeLeft = 300;
                            Main.projectile[projectile].DamageType = DamageClass.Default;
                            NPC.netUpdate = true;
                        }
                        SoundEngine.PlaySound(SoundID.Item28, NPC.Center);
                    }

                    NPC.velocity -= vecToPlayer * 3f;


                    NPC.netUpdate = true;
                }

                TimerForShooting++;

                if (TimerForShooting >= TimeBetweenProjectiles * AmountOfProjectiles)
                {
                    TimerForShooting = 0f;
                    AITimer = 0f;
                    CurrentState = IceClasperAIState.Dashing;
                    NPC.netUpdate = true;
                }
            }
            else if (AITimer >= TimeBetweenBurst / 2f && AITimer < TimeBetweenBurst)
            {
                Vector2 randPos = Main.rand.NextVector2CircularEdge(100f, 100f);
                Dust telegraphDust = Dust.NewDustPerfect(NPC.Center + randPos, 172, NPC.DirectionFrom(NPC.Center + NPC.velocity + randPos) * Main.rand.NextFloat(5f, 7f), 0, default, 1.5f);
                telegraphDust.noGravity = true;
                NPC.netUpdate = true;
            }
        }

        public void State_Dashing(Entity player)
        {
            float distToTarget = NPC.Distance(player.Center) + .1f;
            AITimer++;
            if (AITimer <= TimeBeforeDash)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, -NPC.rotation.ToRotationVector2() * 2f, .1f);
                NPC.netUpdate = true;
            }
            else if (AITimer > TimeBeforeDash && AITimer <= TimeBeforeDash + TimeDashing)
            {
                NPC.velocity = NPC.rotation.ToRotationVector2() * (DashSpeed + (2f / (distToTarget * .1f)));
                NPC.netUpdate = true;
            }
            else
            {
                AITimer = 0f;
                checkedRotationDir = false; CurrentState = IceClasperAIState.Shooting;
                NPC.netUpdate = true;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter += (isDashing) ? 0.4f : 0.15f;
            NPC.frameCounter %= Main.npcFrameCount[NPC.type];
            int frame = (int)NPC.frameCounter;
            NPC.frame.Y = frame * frameHeight;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Frost, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 15; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Frost, hit.HitDirection, -1f, 0, default, 1f);
                }
            }
        }


        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 position = NPC.Center - screenPos;
            Vector2 origin = new Vector2(TextureAssets.Npc[NPC.type].Value.Width / 2, TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type] / 2);
            position -= new Vector2(texture.Width, texture.Height / Main.npcFrameCount[NPC.type]) * NPC.scale / 2f;
            position += origin * NPC.scale + new Vector2(0f, NPC.gfxOffY);

            float interpolant = (AITimer > TimeBeforeDash && AITimer <= TimeBeforeDash + TimeDashing) ? 1f - ((AITimer - TimeBeforeDash) / TimeDashing) :
(MathHelper.Clamp(AITimer, 0f, TimeBeforeDash) / TimeBeforeDash);
            float AfterimageFade = MathHelper.Lerp(0f, 1f, interpolant);

            // 灾厄残影客户端开关移除，恒定绘制
            if (CurrentState == IceClasperAIState.Dashing)
            {
                for (int i = 0; i < NPC.oldPos.Length; i++)
                {
                    Color afterimageDrawColor = new Color(0.79f, 0.94f, 0.98f) with { A = 125 } * NPC.Opacity * (1f - i / (float)NPC.oldPos.Length) * AfterimageFade;
                    Vector2 afterimageDrawPosition = NPC.oldPos[i] + NPC.Size * 0.5f - screenPos;
                    spriteBatch.Draw(texture, afterimageDrawPosition, NPC.frame, afterimageDrawColor, NPC.rotation - MathHelper.PiOver2, origin, NPC.scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.Draw(texture, position, NPC.frame, drawColor, NPC.rotation - MathHelper.PiOver2, origin, NPC.scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
