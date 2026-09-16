using CalamityEntropy.Assets.Register;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles
{
    public class AtlasItem : ModProjectile
    {

        public float TileCollisionYThreshold => Projectile.ai[0];

        public bool HasCollidedWithGround {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public ref float SquishFactor => ref Projectile.localAI[0];

        public const float Gravity = 1.1f;

        public const float MaxFallSpeed = 24f;

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 14;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 86;
            Projectile.height = 130;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 400;
            Projectile.tileCollide = true;
            Projectile.sentry = true;
        }
        public int counter = 0;
        public override void AI() {
            counter++;
            if (counter == 15) {
                SoundEngine.PlaySound(new("CalamityEntropy/Assets/Sounds/spin1"), Projectile.Center);
            }
            if (counter == 160) {
                SoundEngine.PlaySound(SoundID.Coins with { Pitch = 0.4f }, Projectile.Center);
            }
            if (counter < 160) {
                return;
            }
            if (SquishFactor <= 0f)
                SquishFactor = 1f;

            if (Projectile.velocity.Y == 0f && !HasCollidedWithGround) {
                PerformGroundCollisionEffects();
                HasCollidedWithGround = true;
                Projectile.netUpdate = true;
            }

            SquishFactor = MathHelper.Lerp(SquishFactor, 1f, 0.08f);

            Projectile.tileCollide = Projectile.Bottom.Y >= TileCollisionYThreshold;

            Projectile.frameCounter++;
            if (!HasCollidedWithGround)
                Projectile.frame = Projectile.frameCounter / 6 % 5;
            else {
                Projectile.velocity.X = 0f;
                if (Projectile.frame < 5)
                    Projectile.frame = 5;
                if (Projectile.frameCounter % 8 == 7) {
                    Projectile.frame++;

                    if (Projectile.frame == 8) {
                        SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("CalamityEntropy/Assets/Sounds/steam"), Projectile.Top);
                        if (Main.rand.NextDouble() < 0.01f) {
                            if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.Server) {
                                NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, NPCID.SleepingAngler);
                                Projectile.frame = 9;
                            }
                        }

                        if (Projectile.Entropy().AtlasItemType == 0) {
                            CombatText.NewText(Projectile.getRect(), Color.Yellow, "哇，什么也没有");
                        }
                        else if (Projectile.Entropy().AtlasItemType == -11) {
                            if (Main.netMode == NetmodeID.SinglePlayer) {
                                NPC.NewNPC(Projectile.GetSource_GiftOrReward(), (int)Projectile.Center.X, (int)Projectile.Center.Y, NPCID.SleepingAngler);
                            }
                            if (Main.netMode == NetmodeID.Server) {
                                int np = NPC.NewNPC(Projectile.GetSource_GiftOrReward(), (int)Projectile.Center.X, (int)Projectile.Center.Y, NPCID.SleepingAngler);
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, np);
                            }
                        }
                        else {
                            // 灾厄 CodebreakerBase 套装特判已随奖池重做删除（misc-map §二 p7），统一走通用掉落
                            Item ispawn = new Item(Projectile.Entropy().AtlasItemType, Projectile.Entropy().AtlasItemStack);
                            if (Main.netMode == NetmodeID.SinglePlayer) {
                                int np = Item.NewItem(Projectile.GetSource_FromAI(), Projectile.getRect(), ispawn);
                            }
                            if (Main.netMode == NetmodeID.Server) {
                                int np = Item.NewItem(Projectile.GetSource_FromAI(), Projectile.getRect(), ispawn);
                                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, np);
                            }
                            if (Projectile.Entropy().AtlasItemType == ItemID.PoopBlock) {
                                for (int i = 0; i < 5; i++) {
                                    Projectile.NewProjectile(Wiring.GetProjectileSource((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16), Projectile.Center.X, Projectile.Center.Y, 0f, 0f, ProjectileID.ToiletEffect, 0, 0f, Main.myPlayer);
                                }
                            }
                        }

                    }
                }
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = Main.projFrames[Projectile.type] - 1;
            }

            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxFallSpeed)
                Projectile.velocity.Y = MaxFallSpeed;
        }

        public void PerformGroundCollisionEffects() {
            SquishFactor = 1.4f;

            int dustID = 182;
            int dustCount = 54;
            for (int i = 0; i < dustCount; i += 2) {
                float pairSpeed = Main.rand.NextFloat(0.5f, 16f);
                Dust d = Dust.NewDustDirect(Projectile.Bottom, 0, 0, dustID);
                d.velocity = Vector2.UnitX * pairSpeed;
                d.scale = 2.7f;
                d.noGravity = true;

                d = Dust.NewDustDirect(Projectile.BottomRight, 0, 0, dustID);
                d.velocity = Vector2.UnitX * -pairSpeed;
                d.scale = 2.7f;
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);

            CEUtils.SetShake(Projectile.Center, 4.5f, 1800);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
            overPlayers.Add(index);
        }
        public override bool ShouldUpdatePosition() {
            return counter >= 160;
        }
        public override bool? CanDamage() => false;

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
            fallThrough = false;
            return true;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (counter < 160) {
                return false;
            }
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Texture2D glowmask = CEExtraAssets.AtlasMunitionsDropPodGlow;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Projectile.scale * new Vector2(SquishFactor, 1f / SquishFactor);
            Vector2 origin = frame.Size() * new Vector2(0.5f, 0.5f / SquishFactor);
            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, 0, 0);
            Main.EntitySpriteDraw(glowmask, drawPosition, frame, Color.White, Projectile.rotation, origin, scale, 0, 0);
            return false;
        }
    }
}
