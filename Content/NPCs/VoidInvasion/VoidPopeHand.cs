using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Projectiles;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.VoidInvasion
{
    public class VoidPopeHand : ModNPC
    {
        public override void OnSpawn(IEntitySource source)
        {
        }


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers nPCBestiaryDrawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers();
            nPCBestiaryDrawModifiers.Hide = true;
            NPCID.Sets.NPCBestiaryDrawModifiers value = nPCBestiaryDrawModifiers;
            NPCID.Sets.NPCBestiaryDrawOffset[this.Type] = value;
            NPC.width = 76;
            NPC.height = 76;
            NPC.damage = 0;
            NPC.defense = 60;
            NPC.lifeMax = 1600000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCHit1;
            NPC.value = 200000f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.dontTakeDamage = true;
        }

        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRots = new List<float>();
        public int direction
        {
            get { return (int)NPC.ai[1]; }
            set { NPC.ai[1] = value; }
        }
        public int counter1 = 6;
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            npcHitbox = (NPC.Center + NPC.rotation.ToRotationVector2() * 64 * NPC.scale).getRectCentered(NPC.width * NPC.scale, NPC.height * NPC.scale);
            return true;
        }
        public int attackCd = 0;
        public int noHomingTime = 0;
        public Player handPlayer = null;
        public int handPlayerTime = 0;
        public int handUp = 0;
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return handPlayerTime <= 0;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (handPlayerTime <= 0)
            {
                handPlayer = target;
                handPlayerTime = 8;
            }
        }
        public float circleDist = 0;
        public bool circle = false;
        public bool needSpawnRotProj = true;
        public override void AI()
        {

            if (counter1 > 0)
            {
                counter1--;
                return;
            }
            NPC owner = ((int)NPC.ai[0]).ToNPC();

            if (!owner.active || owner.ModNPC is not VoidPope)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }
            NPC.realLife = owner.whoAmI;
            NPC.target = owner.target;
            VoidPope modNpc = (VoidPope)owner.ModNPC;
            if (modNpc.aitype == VoidPope.AttackAIStyle.Melee)
            {
                NPC.velocity *= 0.96f;
            }
            else
            {
                NPC.velocity *= 0.99f;
            }
            if (modNpc.aitype == VoidPope.AttackAIStyle.Melee)
            {
                if (attackCd <= 0)
                {
                    NPC.velocity += (owner.target.ToPlayer().Center - NPC.Center).SafeNormalize(Vector2.Zero) * 44;
                    noHomingTime = 30;
                    attackCd = modNpc.random.Next(100, 180);
                }
                else
                {
                    attackCd--;
                }
            }
            else
            {
                handPlayerTime = 0;
            }
            if (modNpc.aitype == VoidPope.AttackAIStyle.VoidLightball)
            {
                if (handUp >= 0)
                {
                    if (handUp == 0)
                    {
                        NPC.velocity += (owner.target.ToPlayer().Center - NPC.Center).SafeNormalize(Vector2.Zero) * 32;
                        noHomingTime = 30;
                    }
                    handUp--;
                }
                if (attackCd <= 0)
                {
                    handUp = 58;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<VPVoidLightBall>(), NPC.damage / 7, 6, -1, 0, 0, NPC.whoAmI);
                    }
                    attackCd = modNpc.random.Next(100, 180);
                }
                else
                {
                    attackCd--;
                }
            }
            else
            {
                handUp = 0;
            }
            circle = false;
            if (modNpc.aitype == VoidPope.AttackAIStyle.Circle)
            {
                circle = true;
                if (needSpawnRotProj)
                {
                    needSpawnRotProj = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<VPRot>(), NPC.damage / 7, 6, -1, 0, 0, NPC.whoAmI);
                    }
                }

                circleDist = circleDist + (CEUtils.getDistance(owner.Center, owner.target.ToPlayer().Center) - circleDist) * 0.01f;
                if (circleDist > 640)
                {
                    circleDist = 640;
                }
            }
            else
            {
                circleDist = 100;
                needSpawnRotProj = true;
            }
            NPC.damage = owner.damage;
            NPC.scale = owner.scale;
            Vector2 targetPos = owner.Center + new Vector2(direction * 100 * NPC.scale, (handUp > 0 ? -80 : 0));
            if (modNpc.aitype == VoidPope.AttackAIStyle.Circle)
            {
                targetPos = owner.Center + new Vector2(circleDist * direction, 0).RotatedBy(modNpc.circleCounter);
                NPC.Center += (targetPos - NPC.Center) * 0.4f;
            }
            else
            {
                if (handPlayerTime > 0)
                {
                    handPlayer.Center = NPC.Center + NPC.rotation.ToRotationVector2() * 86;
                    handPlayer.velocity *= 0;
                }
                if (noHomingTime > 0)
                {
                    noHomingTime--;
                }
                else
                {
                    NPC.Center += (targetPos - NPC.Center) * 0.24f;
                    if (handPlayerTime > 0)
                    {
                        handPlayerTime--;
                        if (handPlayerTime == 0)
                        {
                            handPlayer.velocity = (owner.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 20f;

                        }
                    }
                }
            }
            if (CEUtils.getDistance(targetPos, NPC.Center) > 4600)
            {
                NPC.Center = targetPos;
            }
            NPC.rotation = (NPC.Center - owner.Center).ToRotation();
            oldPos.Add(NPC.Center);
            oldRots.Add(NPC.rotation);
            if (oldPos.Count > 24)
            {
                oldPos.RemoveAt(0);
                oldRots.RemoveAt(0);
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void PostAI()
        {
            NPC owner = ((int)NPC.ai[0]).ToNPC();
            if (!owner.active)
            {
                return;
            }

        }
        public float trailOffset = 0;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            trailOffset += 0.06f;
            SpriteBatch sb = Main.spriteBatch;
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            Texture2D trail;
            var r = Main.rand;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            List<ColoredVertex> ve = new List<ColoredVertex>();

            for (int i = 0; i < oldRots.Count; i++)
            {
                Color b = Color.Lerp(Color.Purple * 0.01f, Color.Purple, ((float)(i)) / (float)oldRots.Count) * 1f;
                ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (16 + 80 * NPC.scale * (1 - (float)(i) / (float)oldRots.Count) * 0.5f),
                      new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                      b));
                ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (16 + 80 * NPC.scale - 80 * NPC.scale * (1 - ((float)(i) / (float)oldRots.Count)) * 0.5f),
                      new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                      b));
            }

            if (ve.Count >= 3)
            {
                trail = CEExtraAssets.white;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            ve = new List<ColoredVertex>();

            for (int i = 0; i < oldRots.Count; i++)
            {
                Color b = Color.Lerp(Color.White * 0.01f, Color.White, ((float)(i)) / (float)oldRots.Count) * 1f;
                ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (16 + 80 * NPC.scale * (1 - (float)(i) / (float)oldRots.Count) * 0.5f),
                      new Vector3(i / (float)oldRots.Count + trailOffset, 1, 1),
                      b));
                ve.Add(new ColoredVertex(oldPos[i] - Main.screenPosition + oldRots[i].ToRotationVector2() * (16 + 80 * NPC.scale - 80 * NPC.scale * (1 - ((float)(i) / (float)oldRots.Count)) * 0.5f),
                      new Vector3(i / (float)oldRots.Count + trailOffset, 0, 1),
                      b));
            }

            if (ve.Count >= 3)
            {
                trail = CEExtraAssets.SwordSlashTexture;
                gd.Textures[0] = trail;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D tex = TextureAssets.Npc[NPC.type].Value;
            Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition, null, Color.White, NPC.rotation + (direction > 0 ? MathHelper.ToRadians(18) : MathHelper.ToRadians(-18 + 180)), (direction > 0 ? new Vector2(0, tex.Height) : new Vector2(tex.Width, tex.Height)), NPC.scale, (direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally));

            return false;
        }

    }
}
