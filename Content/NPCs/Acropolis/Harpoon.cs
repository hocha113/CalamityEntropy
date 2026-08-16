using CalamityEntropy.Content.Buffs;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.NPCs.Acropolis
{
    public class Harpoon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            this.HideFromBestiary();
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
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
        public NPC owner => ((int)NPC.ai[0]).ToNPC();
        public bool OnLauncher = true;
        public int Back = 0;
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return owner.boss && !OnLauncher;
        }
        public Vector2 sVel = Vector2.Zero;
        public bool Stuck = false;
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<MechanicalTrauma>(), 180);
        }
        public override void AI()
        {
            PullCD--;
            if (NPC.localAI[1]++ == 0)
            {
                if (NPC.velocity.X == 0)
                    NPC.velocity.X = 0.02f;
                if (NPC.velocity.Y == 0)
                    NPC.velocity.X = 0.02f;
            }
            if (NPC.ai[0] < 0 || !owner.active)
            {
                NPC.active = false;
                return;
            }
            NPC.scale = owner.scale;
            NPC.damage = owner.damage;
            if (owner.ModNPC is AcropolisMachine am)
            {
                if (OnLauncher)
                {
                    NPC.Center = am.HarpoonPos;
                    NPC.rotation = am.harpoon.Seg2Rot;
                    Stuck = false;
                    NPC.velocity *= 0;
                    sVel *= 0;
                }
                else
                {
                    if (sVel == Vector2.Zero)
                    {
                        sVel = NPC.velocity;
                    }
                    NPC.rotation = (NPC.Center - (((AcropolisMachine)owner.ModNPC).HarpoonPos - ((AcropolisMachine)owner.ModNPC).harpoon.Seg2Rot.ToRotationVector2() * 72 * NPC.scale)).ToRotation();
                    if (!Stuck && Back-- < 0)
                    {
                        NPC.noTileCollide = true;
                        NPC.velocity += (am.HarpoonPos - NPC.Center).normalize() * 8 * NPC.scale;
                        NPC.velocity *= 0.9f;
                        if (CEUtils.getDistance(NPC.Center, am.HarpoonPos) <= NPC.velocity.Length() + 6)
                        {
                            OnLauncher = true;
                            NPC.velocity *= 0;
                        }
                    }
                    else if (owner.HasValidTarget)
                    {
                        if (NPC.noTileCollide && CEUtils.getDistance(owner.Center, owner.target.ToPlayer().Center) > 500)
                        {
                            if (CEUtils.getDistance(owner.Center, NPC.Center) > 600)
                            {
                                if (CEUtils.getDistance(NPC.Center, owner.target.ToPlayer().Center) < 400)
                                {
                                    if (CEUtils.CheckSolidTile(NPC.getRect()))
                                    {
                                        if (!Stuck && !am.Jumping && PullCD <= 0)
                                        {
                                            PullCD = 12 * 60;
                                            Stuck = true;
                                            NPC.Center += sVel.normalize() * 40;
                                            CEUtils.PlaySound("ExoHit1", 1, NPC.Center);
                                            flag = false;
                                        }
                                    }
                                }
                            }
                        }
                        if (Stuck)
                        {
                            NPC.velocity *= 0;
                            if (CEUtils.getDistance(owner.Center, NPC.Center) > 400)
                            {
                                owner.ai[2] = 2;
                                am.Jumping = true;
                                am.JumpCD = 100;
                                Back = 5;
                            }
                            else
                            {
                                Back = -1;
                                Stuck = false;
                                am.JumpCD = 100;
                                am.Jumping = false;
                            }
                        }
                    }
                }
            }
            else
            {
                NPC.active = false;
            }
        }
        public bool flag = true;
        public int PullCD = 0;
        public override bool CheckActive()
        {
            return !owner.active;
        }
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            npcHitbox = npcHitbox.Center.ToVector2().getRectCentered((npcHitbox.Width * NPC.scale), (npcHitbox.Height * NPC.scale));
            return true;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D harpoonOutline = CEUtils.RequestTex("CalamityEntropy/Content/NPCs/Acropolis/HarpoonOutline");
            if (OnLauncher)
                return false;
            CEUtils.drawChain(NPC.Center, ((AcropolisMachine)owner.ModNPC).HarpoonPos - ((AcropolisMachine)owner.ModNPC).harpoon.Seg2Rot.ToRotationVector2() * 72 * NPC.scale, 18, "CalamityEntropy/Content/NPCs/Acropolis/HarpoonChain");
            Texture2D harpoon3 = NPC.getTexture(); for (float r = 0; r <= 360; r += 60)
            {
                Main.EntitySpriteDraw(harpoonOutline, MathHelper.ToRadians(r).ToRotationVector2() * 2 + NPC.Center - Main.screenPosition, null, Color.OrangeRed, NPC.rotation, new Vector2(70, harpoon3.Height / 2f), NPC.scale, ((AcropolisMachine)owner.ModNPC).dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
            Main.EntitySpriteDraw(harpoon3, NPC.Center - Main.screenPosition, null, drawColor, NPC.rotation, new Vector2(70, harpoon3.Height / 2f), NPC.scale, ((AcropolisMachine)owner.ModNPC).dir > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            return false;
        }
    }
}
