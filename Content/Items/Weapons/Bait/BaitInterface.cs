using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Weapons.Bait
{
    public interface IBaitItem
    {
        public virtual float ChargeTimeMult => 12;
    }

    public abstract class BaitProj : ModProjectile
    {
        public bool IsActive = true;
        public int StickNPC = -1;
        public Vector2 StickOffset = Vector2.Zero;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(StickNPC);
            writer.WriteVector2(StickOffset);
            writer.Write(IsActive);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            StickNPC = reader.ReadInt32();
            StickOffset = reader.ReadVector2();
            IsActive = reader.ReadBoolean();
        }
        public float Counter { get { return Projectile.localAI[0]; } set { Projectile.localAI[0] = value; } }
        public float ActiveCounter { get { return Projectile.localAI[1]; } set { Projectile.localAI[1] = value; } }
        public int TagDamage => (int)Projectile.ai[2];
        public virtual void SetActive()
        {
            ActiveEffect(1);
            IsActive = false;
        }
        public virtual void ActiveEffect(float DamageMul)
        { }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 0.16f;
        }
    }
    public class BaitHeldEffect : ModProjectile
    {
        public override string Texture => CEUtils.WhiteTexPath;
        public override void SetDefaults()
        {
            Projectile.FriendlySetDefaults(DamageClass.Generic, false, -1);
        }
        public override bool? CanDamage()
        {
            return false;
        }
        public float throwAnm = 0;
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void AI()
        {
            Player player = Projectile.GetOwner();
            if(player.HeldItem.ModItem is not IBaitItem)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = new Vector2(16, 0).RotatedBy((player.Calamity().mouseWorld - player.MountedCenter).ToRotation());
            int dir = Projectile.velocity.X > 0 ? 1 : -1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.timeLeft = 4;
            Projectile.Center = player.MountedCenter + new Vector2(dir * -4, 0).RotatedBy(player.fullRotation);
            player.Calamity().mouseWorldListener = true;
            float hr = 2.4f;
            float charge = float.Clamp(player.Entropy().BaitCharge, 0, 1);
            if(throwAnm > 0)
            {
                Projectile.rotation += (-hr + CEUtils.Parabola((1 - throwAnm * throwAnm * throwAnm) * 0.5f, hr * 1.6f)) * dir;
                throwAnm -= 1 / 12f;
                if (throwAnm < 0)
                    throwAnm = 0;
            }
            else
            {
                Projectile.rotation = CEUtils.RotateTowardsAngle(Projectile.rotation + (float)(Math.Sin(Main.GameUpdateCount * 0.5f)) * 0.18f, Projectile.rotation - hr * dir, charge * charge * charge, false);
            }
            player.SetHandRotWithDir(Projectile.rotation, dir);
            player.heldProj = Projectile.whoAmI;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (throwAnm > 0)
                return false;
            Texture2D item = TextureAssets.Item[Projectile.GetOwner().HeldItem.type].Value;
            Main.spriteBatch.Draw(item, Projectile.Center + Projectile.rotation.ToRotationVector2() * 5f - Main.screenPosition, null, lightColor, Projectile.rotation, new Vector2(0, item.Height * 0.5f), Projectile.GetOwner().HeldItem.scale, Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            return false;
        }
    }
}

