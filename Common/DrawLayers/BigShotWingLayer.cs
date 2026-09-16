using CalamityEntropy.Content.Items.Accessories;
using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityEntropy.Common.DrawLayers
{
    public class BigShotWingLayer : PlayerDrawLayer
    {
        //翅膀与吊线贴图在加载期就位,不再每帧走 getExtraTex 查表
        [VaultLoaden("CalamityEntropy/Assets/Extra/BSW/W")]
        internal static Asset<Texture2D> WingTex;
        [VaultLoaden("CalamityEntropy/Assets/Extra/jl")]
        internal static Asset<Texture2D> StringTex;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            //return false;
            if (drawInfo.drawPlayer.dead || drawInfo.shadow != 0 || (drawInfo.drawPlayer.Entropy().vanityWing != null && !(drawInfo.drawPlayer.Entropy().vanityWing.ModItem is BigShotsWing)))
                return false;
            return drawInfo.drawPlayer.Entropy().hasAccVisual("BSWing");
        }

        public override Position GetDefaultPosition() {
            return new BeforeParent(PlayerDrawLayers.Wings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            var player = drawInfo.drawPlayer;
            Texture2D texture = WingTex.Value;
            Color GC(Color color) {
                //脱离灾厄:灾厄肾上腺素/暴怒增益的白闪已随系统裁撤,恒走基础光照色
                return Lighting.GetColor((player.Center / 16).ToPoint(), color) * 0.4f;
            }
            float num = player.GetModPlayer<BigShotWingPlayer>().Num;
            float cCount = player.GetModPlayer<BigShotWingPlayer>().cCount;
            player.GetModPlayer<BigShotWingPlayer>().cCount += float.Lerp(0.16f, 0.042f, num);
            Vector2 origin = player.GetDrawCenter() + new Vector2(0, -8);
            float rotj = (float)Math.Cos(cCount) * float.Lerp(0.9f, 0.2f, num);
            float rRot = (player.GetModPlayer<BigShotWingPlayer>().rpoint - origin).ToRotation() + rotj;
            float lRot = (player.GetModPlayer<BigShotWingPlayer>().lpoint - origin).ToRotation() - rotj;
            float scale = player.GetModPlayer<BigShotWingPlayer>().scale * 1.15f;
            var _drawInfo = drawInfo;
            int visual = player.Entropy().BigShotWingVisual;
            void drawLine(Vector2 start) {
                Vector2 end = start + new Vector2((start.X - origin.X) + player.GetModPlayer<BigShotWingPlayer>().StringsOffset, -1000);
                _drawInfo.DrawDataCache.Add(new DrawData(StringTex.Value, start - Main.screenPosition, null, new Color(60, 255, 60) * (visual == 0 ? 0.6f : 0.2f), (end - start).ToRotation(), new Vector2(0, 0.5f), new Vector2(CEUtils.getDistance(start, end) / 1024f, 2), SpriteEffects.None));
            }
            if (visual != 2) {
                drawLine(origin + new Vector2(50, -22).RotatedBy(rRot) * scale);
                drawLine(origin + new Vector2(50, 22).RotatedBy(lRot) * scale);
                drawLine(origin + new Vector2(28, -8).RotatedBy(rRot) * scale);
                drawLine(origin + new Vector2(28, 8).RotatedBy(lRot) * scale);
                drawLine(origin + new Vector2(0, -14));
                drawLine(player.GetFrontHandPositionImproved(player.compositeFrontArm));
                drawLine(player.GetBackHandPositionImproved(player.compositeBackArm));
                drawLine(origin + new Vector2(player.velocity.Y == 0 ? 5 : 8, 10));
                drawLine(origin + new Vector2(player.velocity.Y == 0 ? -5 : -8, 10));
            }
            drawInfo.DrawDataCache.Add(new DrawData(texture, origin - Main.screenPosition, null, GC(new Color(200, 200, 0)), rRot, new Vector2(0, 15), new Vector2(1f, 1.8f) * scale, SpriteEffects.None) { shader = drawInfo.drawPlayer.cWings });
            drawInfo.DrawDataCache.Add(new DrawData(texture, origin - Main.screenPosition, null, GC(new Color(0, 80, 0)), lRot, new Vector2(0, texture.Height - 15), new Vector2(1f, 1.8f) * scale, SpriteEffects.FlipVertically) { shader = drawInfo.drawPlayer.cWings });

            drawInfo.DrawDataCache.Add(new DrawData(texture, origin - Main.screenPosition, null, GC(Color.Pink), rRot, new Vector2(0, 15), new Vector2(1f, 1.1f) * scale, SpriteEffects.None) { shader = drawInfo.drawPlayer.cWings });
            drawInfo.DrawDataCache.Add(new DrawData(texture, origin - Main.screenPosition, null, GC(new Color(0, 0, 205)), lRot, new Vector2(0, texture.Height - 15), new Vector2(1f, 1.1f) * scale, SpriteEffects.FlipVertically) { shader = drawInfo.drawPlayer.cWings });

            drawInfo.DrawDataCache.Add(new DrawData(texture, origin - Main.screenPosition, null, GC(new Color(255, 60, 120)), rRot, new Vector2(0, 15), new Vector2(1f, 0.45f) * scale, SpriteEffects.None) { shader = drawInfo.drawPlayer.cWings });
            drawInfo.DrawDataCache.Add(new DrawData(texture, origin - Main.screenPosition, null, GC(new Color(30, 0, 160)), lRot, new Vector2(0, texture.Height - 15), new Vector2(1f, 0.45f) * scale, SpriteEffects.FlipVertically) { shader = drawInfo.drawPlayer.cWings });


        }

    }
    public class BigShotWingPlayer : ModPlayer
    {
        public Vector2 lpoint = Vector2.Zero;
        public Vector2 rpoint = Vector2.Zero;
        public float StringsOffset = 0;
        public float Num = 1;
        public float cCount = 0;
        //脱离灾厄:肾上腺素/暴怒的放大加成随系统裁撤,恒为基础尺寸
        public float scaleBoost => 1;
        public float scale = 1;
        public override void PostUpdate() {
            StringsOffset = float.Lerp(StringsOffset, Player.velocity.X * 12, 0.16f);
            float dist = 50;
            Vector2 origin = Player.MountedCenter + new Vector2(0, -8);
            bool flying = Player.wingTime > 0 && Player.controlJump;
            float rot = (Player.velocity.X * 0.036f);
            Vector2 ltarget = origin + float.Lerp((rot + MathHelper.Pi + 0.6f), (rot + MathHelper.Pi - 0.3f), Num).ToRotationVector2() * dist;
            Vector2 rtarget = origin + float.Lerp((rot - 0.6f), (rot + 0.3f), Num).ToRotationVector2() * dist;
            Num = float.Lerp(Num, flying ? 0 : 1, 0.09f);
            lpoint = origin + (lpoint - origin).normalize() * dist;
            rpoint = origin + (rpoint - origin).normalize() * dist;
            lpoint = Vector2.Lerp(lpoint, ltarget, 0.38f);
            rpoint = Vector2.Lerp(rpoint, rtarget, 0.38f);
            scale = float.Lerp(scale, scaleBoost, 0.04f);
        }
    }
}
