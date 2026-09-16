using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace CalamityEntropy.Content.Particles
{
    //持有Player引用,池化会攒活引用,不开CanPool
    //残影跟着plr快照走,复用实例指到别的玩家身上直接穿模,低频也不差这点GC
    public class PRT_PlayerShadow : BasePRT
    {
        public bool Glow = true;
        public Player plr;

        public override string Texture => "CalamityEntropy/Assets/Extra/white";

        public PRT_PlayerShadow Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 20;
        }

        public override void AI() {
            Opacity = 1f - LifetimeCompletion;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //就一趟DrawPlayer,没shader没TriangleStrip;PlayerRenderer内部会动SpriteBatch状态
            //return false挡掉框架sb.Draw白图,Texture留着只是堵HasAsset的Warn
            //DrawPlayer自己管SpriteBatch,return false框架不再sb.Draw Texture
            if (plr != null)
                Main.PlayerRenderer.DrawPlayer(Main.Camera, plr, Position, Rotation, Vector2.Zero, (1 - Opacity * 0.35f), 1);
            return false;
        }
    }



    //Black版同样持plr+懒建clone,CanPool开着clone和dye状态会泄漏到下一个残影
    public class PRT_PlayerShadowBlack : BasePRT
    {
        public bool Glow = true;
        public Player plr;
        public float alpha = 1;
        public Player clone = null;

        public override string Texture => "CalamityEntropy/Assets/Extra/white";

        public PRT_PlayerShadowBlack Configure(float opacity, bool glow, PRTDrawModeEnum mode,
            float rotation = 0f, int lifetime = -1) {
            Opacity = opacity;
            Glow = glow;
            PRTDrawMode = mode;
            Rotation = rotation;
            if (lifetime > 0)
                Lifetime = lifetime;
            return this;
        }

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            if (Lifetime <= 0)
                Lifetime = 20;
        }

        public override void AI() {
            Opacity = (1f - LifetimeCompletion) * alpha;
        }

        public override bool PreDraw(SpriteBatch sb) {
            //懒建clone涂黑,只建一次不每帧CopyVisuals,武器帧靠下面bodyFrame分支补
            //要开CanPool得Reset里clone=null并重走整套染色,漏了下一个残影顶上一套装备
            if (clone == null) {
                clone = new Player();
                clone.CopyVisuals(plr);
                clone.skinColor = Color.Black;
                clone.shirtColor = Color.Black;
                clone.underShirtColor = Color.Black;
                clone.pantsColor = Color.Black;
                clone.shoeColor = Color.Black;
                clone.hairColor = Color.Black;
                clone.eyeColor = Color.Red;
                for (int i = 0; i < clone.dye.Length; i++) {
                    if (clone.dye[i].type != ItemID.ShadowDye)
                        clone.dye[i].SetDefaults(ItemID.ShadowDye);
                }
                clone.ResetEffects();
                clone.ResetVisibleAccessories();
                clone.DisplayDollUpdate();
                clone.UpdateSocialShadow();
                clone.UpdateDyes();
                clone.PlayerFrame();
                if (plr.ItemAnimationActive && plr.altFunctionUse != 2)
                    clone.bodyFrame = plr.bodyFrame;
                else
                    clone.bodyFrame.Y = 0;
                clone.legFrame.Y = 0;
                clone.direction = plr.direction;
            }
            Main.PlayerRenderer.DrawPlayer(Main.Camera, clone, Position, 0f, clone.fullRotationOrigin, 1 - Opacity, 1f);
            return false;
        }
    }



}
