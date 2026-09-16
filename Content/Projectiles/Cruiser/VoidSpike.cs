using CalamityEntropy.Assets.Register;
using CalamityEntropy.Content.Buffs;
using CalamityEntropy.Content.Particles;
using CalamityEntropy.Core.Graphics;
using InnoVault;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Projectiles.Cruiser
{

    public class VoidSpike : ModProjectile
    {
        //本体贴图,加载期就位,PreDraw 不再逐帧请求
        [VaultLoaden("CalamityEntropy/Content/Projectiles/Cruiser/VoidSpike")]
        internal static Asset<Texture2D> VoidSpikeTex;
        List<Vector2> odp = new List<Vector2>();
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 1;

        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            target.AddBuff(ModContent.BuffType<VoidTouch>(), 160);
        }
        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.scale = 1f;
            Projectile.timeLeft = 520;
            Projectile.extraUpdates = 1;
        }
        public bool setv = true;
        public override void AI() {
            Projectile.localAI[1] = float.Lerp(Projectile.localAI[1], 1, 0.1f);
            if (setv) {
                setv = false;
                Projectile.velocity *= 0.5f;
            }
            odp.Add(Projectile.Center);
            if (odp.Count > 24) {
                odp.RemoveAt(0);
            }

            if (Projectile.timeLeft < 40) {
                Projectile.alpha += 255 / 40;
            }
            for (float i = 0; i < 1; i += 0.25f) {
                //PRT_Void字段直赋对齐旧VoidParticles,Opacity/ad/multShrink Configure管不了
                var p = PRTLoader.NewParticle<PRT_Void>(Projectile.Center - Projectile.velocity * (i + 0.42f), new Vector2(0.2f, 0).RotatedBy(CEUtils.randomRot()), Color.White, 1f);
                p.Opacity = 0.14f;  //Opacity旧初始化器字段,Configure管不了
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.01f;
        }
        public override bool PreDraw(ref Color lightColor) {
            Texture2D t = VoidSpikeTex.Value;
            Main.spriteBatch.UseAdditive();
            Texture2D tex = CEExtraAssets.Glow2;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(tex.Width / 2, 0, tex.Width / 2, tex.Height), Color.LightBlue, Projectile.rotation, new Vector2(0, tex.Height / 2), Projectile.scale * new Vector2(0.65f * Projectile.localAI[1] * Projectile.velocity.Length(), 0.012f) * Projectile.localAI[1], SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(tex.Width / 2, 0, tex.Width / 2, tex.Height), Color.Blue, Projectile.rotation, new Vector2(0, tex.Height / 2), Projectile.scale * new Vector2(0.65f * Projectile.localAI[1] * Projectile.velocity.Length(), 0.025f) * Projectile.localAI[1], SpriteEffects.None, 0);
            for (float i = 0; i < MathHelper.TwoPi; i += MathHelper.PiOver4) {
                Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition + (i + Main.GlobalTimeWrappedHourly * 16).ToRotationVector2() * 2, null, Color.White, Projectile.rotation, t.Size() / 2, 1, SpriteEffects.None, 0);
            }
            Main.spriteBatch.ExitShaderRegion();
            Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, t.Size() / 2, 1, SpriteEffects.None, 0);
            return false;
        }
    }

}