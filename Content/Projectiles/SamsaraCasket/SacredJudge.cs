using CalamityEntropy.Assets.Register;
using CalamityEntropy.Common;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public class SacredJudge : SamsaraSword
    {
        //虚影与护盾贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/SacredJudgePhantom")]
        internal static Asset<Texture2D> PhantomTex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/SacredJudgePhantom0")]
        internal static Asset<Texture2D> Phantom0Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/SacredJudgePhantom1")]
        internal static Asset<Texture2D> Phantom1Tex;
        [VaultLoaden("CalamityEntropy/Content/Projectiles/SamsaraCasket/SacredJudgeShield")]
        internal static Asset<Texture2D> ShieldTex;
        public override void SetDefaults() {
            base.SetDefaults();
            Projectile.localNPCHitCooldown = 20;
        }
        public bool shieldLast = false;
        public override void AI() {
            setDamage(1);
            Projectile.Resize(20, 20);
            Projectile.timeLeft = 5;
            Player player = Projectile.owner.ToPlayer();
            var modPlayer = player.Entropy();
            if (!shieldLast && modPlayer.SacredJudgeShields > 0) {
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/DeadSunShot"), player.Center);
            }
            shieldLast = modPlayer.SacredJudgeShields > 0;
            if (modPlayer.SacredJudgeShields < 2) {
                phantom1--;
            }
            if (modPlayer.SacredJudgeShields < 1) {
                phantom0--;
            }
            if (hideTime > 0) {
                hideTime--;
                Projectile.Center = player.Center;
                return;
            }
            Vector2 vrec = Projectile.Center;
            Projectile.Center = player.Center;
            int range = getRange(player);


            if (target != null && !target.active) {
                target = null;
            }
            if (target != null && target.dontTakeDamage) {
                target = null;
            }
            if (target == null) {
                target = Projectile.FindTargetWithinRange(range, modPlayer.sCasketLevel <= 3);
            }
            if (target != null && CEUtils.getDistance(player.Center, target.Center) > Math.Min(range, 1400)) {
                target = null;
            }
            Projectile.Center = vrec;
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC.ToNPC().active) {
                target = player.MinionAttackTargetNPC.ToNPC();
            }
            if (spawnAnm > 0) {

                spawnAnm--;
                if (spawnAnm == 52) {
                    if (modPlayer.sCasketLevel < 6) {
                        spawnAnm = 0;
                    }
                }
                if (spawnAnm > 22) {
                    Projectile.rotation = spawnRot;
                    Projectile.velocity = spawnRot.ToRotationVector2() * 47;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
                else if (spawnAnm == 21) {
                    Projectile.Center = player.Center + new Vector2(1000, 0).RotatedBy(modPlayer.CasketSwordRot + circleRot);
                    if (target != null) {
                        spawnAnm = 0;
                    }
                }
                else {
                    Vector2 targetPos = player.Center + new Vector2(0, -140) + new Vector2(90, 0).RotatedBy(modPlayer.CasketSwordRot + circleRot);
                    Projectile.velocity = (targetPos - Projectile.Center) * 0.34f;
                    Projectile.rotation = (player.Center + new Vector2(0, -140) - Projectile.Center).ToRotation();
                }


            }
            else {




                bool returnToCasket = !modPlayer.samsaraCasketOpened;

                if (returnToCasket) {
                    cAlpha = 0;
                    Vector2 targetPos = player.Center + new Vector2(CEUtils.getDistance(Projectile.Center, player.Center) - 26, 0).RotatedBy(spawnRot);
                    if (CEUtils.getDistance(Projectile.Center, player.Center) > 500) {
                        Projectile.velocity = (player.Center - Projectile.Center) * 0.4f;
                    }
                    else {
                        Projectile.velocity = (targetPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 24;
                    }
                    Projectile.rotation = (player.Center - Projectile.Center).ToRotation();
                    if (CEUtils.getDistance(Projectile.Center, player.Center) < Projectile.velocity.Length() * 1.02f) {
                        if (casket.ToProj().active && casket.ToProj().ModProjectile is SamsaraCasketProj sc) {
                            sc.swords[index] = true;
                        }
                        Projectile.Kill();
                    }
                }
                else {

                    if (target == null || modPlayer.SacredJudgeShields > 0) {
                        cAlpha = 0;
                        Projectile.velocity *= 0;
                        if (modPlayer.SacredJudgeShields > 0) {
                            phantom0 = 9;
                        }
                        if (modPlayer.SacredJudgeShields > 1) {
                            phantom1 = 9;
                        }
                        if (modPlayer.sCasketLevel < 6) {
                            Projectile.velocity = (player.MountedCenter + new Vector2(0, -120) - Projectile.Center) * 0.2f;
                            Projectile.rotation = MathHelper.PiOver2;
                        }
                        else {
                            Vector2 targetPos = player.Center + new Vector2(0, -140) + new Vector2(90, 0).RotatedBy(modPlayer.CasketSwordRot + circleRot);
                            if (CEUtils.getDistance(Projectile.Center, targetPos) > 64) {
                                Projectile.velocity = (targetPos - Projectile.Center) * 0.2f;
                            }
                            else {
                                Projectile.velocity = (targetPos - Projectile.Center);
                            }
                            Projectile.rotation = modPlayer.CasketSwordRot + circleRot + MathHelper.Pi;
                        }
                    }
                    else {
                        attackAI(target);
                    }
                }
            }
            oldPos.Add(Projectile.Center);
            oldRot.Add(Projectile.rotation);
            if (oldPos.Count > 5) {
                oldPos.RemoveAt(0);
                oldRot.RemoveAt(0);
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
        public int phantom0 = 0;
        public int phantom1 = 0;
        public override bool PreDraw(ref Color lightColor) {
            if (hideTime > 0) {
                return false;
            }
            Player player = Projectile.owner.ToPlayer();
            EModPlayer modPlayer = player.Entropy();
            Texture2D phantom = PhantomTex.Value;
            float yoffset = (float)Math.Cos((++Projectile.ai[2]) / 28f) * 10;
            float yoffset2 = (float)Math.Cos((30 + Projectile.ai[2]) / 28f) * 10;
            if (phantom0 > 0) {
                if (phantom0 < 9) {
                    phantom = Phantom0Tex.Value;
                }
                if (phantom0 < 5) {
                    phantom = Phantom1Tex.Value;
                }
                Main.spriteBatch.Draw(phantom, player.Center + new Vector2(-80, -100 + yoffset2) - Main.screenPosition, null, Color.White * 0.7f, MathHelper.PiOver2 + MathHelper.PiOver4, phantom.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            }
            phantom = PhantomTex.Value;
            if (phantom1 > 0) {
                if (phantom1 < 9) {
                    phantom = Phantom0Tex.Value;
                }
                if (phantom1 < 5) {
                    phantom = Phantom1Tex.Value;
                }
                Main.spriteBatch.Draw(phantom, player.MountedCenter + new Vector2(80, -100 + yoffset2) - Main.screenPosition, null, Color.White * 0.7f, MathHelper.PiOver2 + MathHelper.PiOver4, phantom.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }

            if (modPlayer.SacredJudgeShields > 0) {
                Texture2D shield = ShieldTex.Value;
                Main.spriteBatch.Draw(shield, player.MountedCenter - Main.screenPosition, null, lightColor * (modPlayer.SacredJudgeShields > 1 ? 0.7f : 0.4f), 0, shield.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            }
            else {
                yoffset = 0;
            }
            Texture2D tex = CEExtraAssets.CircularSmear;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, new Color(203, 211, 186) * cAlpha, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale * 0.5f, SpriteEffects.None, 0);
            lightColor = Color.Lerp(lightColor, Color.White, cAlpha);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            tex = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < oldPos.Count; i++) {
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition + new Vector2(0, yoffset), null, lightColor * alpha * ((float)i / (float)oldPos.Count) * 0.4f, oldRot[i] + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + new Vector2(0, yoffset), null, lightColor * alpha, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        float cAlpha = 0;
        public override void attackAI(NPC t) {
            Projectile.Resize(78, 78);
            if (cAlpha < 1) {
                cAlpha += 0.05f;
            }
            Projectile.velocity *= 0.92f;
            Projectile.velocity += (t.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 3f;
            Projectile.rotation += 1f;
        }

    }
}
