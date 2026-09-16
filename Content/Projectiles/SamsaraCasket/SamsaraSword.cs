using CalamityEntropy.Common;
using CalamityEntropy.Content.Items.Weapons;
using CalamityEntropy.Content.Projectiles.TwistedTwin;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.SamsaraCasket
{
    public abstract class SamsaraSword : ModProjectile
    {
        public float spawnRot = 0;
        public float circleRot = 0;
        public int spawnAnm = 60;
        public List<Vector2> oldPos = new List<Vector2>();
        public List<float> oldRot = new List<float>();
        public int index = 0;
        public int casket = 0;
        public int hideTime = 0;
        public float alpha = 1;
        public static int getRange(Player player) {
            int range = 1000;
            var modPlayer = player.Entropy();
            switch (modPlayer.sCasketLevel) {
                case 0: break;
                case 1: range = 1100; break;
                case 2: range = 1200; break;
                case 3: range = 1400; break;
                default: range = 2000; break;
            }
            return range;
        }
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
        }
        public NPC target = null;
        public bool soundPlay = true;
        public override void AI() {
            if (soundPlay) {
                soundPlay = false;
                SoundEngine.PlaySound(new SoundStyle("CalamityEntropy/Assets/Sounds/SwiftSlice"), Projectile.Center);
            }
            Projectile.timeLeft = 5;
            Player player = Projectile.owner.ToPlayer();
            var modPlayer = player.Entropy();
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
                target = Projectile.FindTargetWithinRange(range, modPlayer.sCasketLevel > 3);
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
                    Projectile.velocity = (targetPos - Projectile.Center) * 0.2f;
                    Projectile.rotation = (player.Center + new Vector2(0, -140) - Projectile.Center).ToRotation();
                }


            }
            else {




                bool returnToCasket = !modPlayer.samsaraCasketOpened || (target == null && modPlayer.sCasketLevel < 6);

                if (returnToCasket) {
                    backing();
                    Vector2 targetPos = player.Center + new Vector2(CEUtils.getDistance(Projectile.Center, player.Center) - 26, 0).RotatedBy(spawnRot);
                    if (CEUtils.getDistance(Projectile.Center, player.Center) > 500) {
                        Projectile.velocity = (player.Center - Projectile.Center) * 0.08f;
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
                    if (target == null) {

                        Vector2 targetPos = player.Center + new Vector2(0, -140) + new Vector2(90, 0).RotatedBy(modPlayer.CasketSwordRot + circleRot);
                        if (CEUtils.getDistance(Projectile.Center, targetPos) > 64) {
                            Projectile.velocity = (targetPos - Projectile.Center) * 0.2f;
                        }
                        else {
                            Projectile.velocity = (targetPos - Projectile.Center);
                        }
                        Projectile.rotation = modPlayer.CasketSwordRot + circleRot + MathHelper.Pi;
                    }
                    else {
                        setDamage(1);
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (HorizonssKey.getVoidTouchLevel() > 0) {
                EGlobalNPC.AddVoidTouch(target, 80, HorizonssKey.getVoidTouchLevel(), 800, 16);
            }
        }
        public virtual void attackAI(NPC t) {
            Vector2 targetPos = t.Center + new Vector2(-100 + index * 33, -100);
            Projectile.velocity = (targetPos - Projectile.Center) * 0.1f;
            Projectile.rotation = (t.Center - Projectile.Center).ToRotation();

        }

        public virtual void backing() {

        }

        public virtual void setDamage(float damageMul) {
            Projectile.damage = 0;
            if (Projectile.owner.ToPlayer().HeldItem.ModItem is HorizonssKey hk) {
                Projectile.damage = (int)(damageMul * Projectile.owner.ToPlayer().GetWeaponDamage(Projectile.owner.ToPlayer().HeldItem));
                if (casket.ToProj().Entropy().IndexOfTwistedTwinShootedThisProj != -1) {
                    Projectile.damage = (int)((float)Projectile.damage * TwistedTwinMinion.damageMul);
                }
                Projectile.CritChance = (int)(Projectile.owner.ToPlayer().GetWeaponCrit(Projectile.owner.ToPlayer().HeldItem));
                Projectile.knockBack = (int)(Projectile.owner.ToPlayer().GetWeaponKnockback(Projectile.owner.ToPlayer().HeldItem));

            }
            Projectile.ArmorPenetration = HorizonssKey.getArmorPen();
        }

        public override bool PreDraw(ref Color lightColor) {
            if (hideTime > 0) {
                return false;
            }
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < oldPos.Count; i++) {
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, lightColor * alpha * ((float)i / (float)oldPos.Count) * 0.4f, oldRot[i] + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * alpha, Projectile.rotation + MathHelper.PiOver4, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer) {
            writer.Write(spawnRot);
            writer.Write(circleRot);
            writer.Write(spawnAnm);
            writer.Write(index);
            writer.Write(casket);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            spawnRot = reader.ReadSingle();
            circleRot = reader.ReadSingle();
            spawnAnm = reader.ReadInt32();
            index = reader.ReadInt32();
            casket = reader.ReadInt32();
        }
    }
}
