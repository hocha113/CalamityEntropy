using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityEntropy.Content.Particles
{
    //旧VoidParticles的Particle数据迁进来,PreDraw恒false,绘制全在EffectLoader.CE_EffectHandler里
    //EnablePixelEffect关=整条EndCapture管线不跑,这类和IPixelPassPRT一样游戏里直接消失,别加PreDraw回退
    //shape4→DrawParticleEffects+kscreen2 metaball; shape≠4→DrawProjectileEffects里cvmask轻量点; 都不是常规PRT桶
    public class PRT_Void : BasePRT
    {
        //vd=速度衰减 ad=透明度衰减,老字段名,四十多处spawn全是这俩
        public float vd = 0.99f;
        public float ad = 0.014f;
        public bool flag1 = false;   //旧字段,spawn侧基本不碰了,Reset仍清防池化脏值
        public int shape = 0;   //4=metaball RT合成,调用点显式赋,默认0走DrawProjectileEffects那条
        public bool multShrink = false;

        public override bool CanPool => true;

        //池化复用,shape/multShrink/flag1忘Reset=下一个void粒子带脏状态,视觉bug极难查
        public override void Reset() {
            base.Reset();
            vd = 0.99f;
            ad = 0.014f;
            flag1 = false;
            shape = 0;
            multShrink = false;
        }

        //Texture留空框架会猜路径刷Warn,借PRT_Light堵上,真绘制在EffectLoader RT合成里
        public override string Texture => "CalamityEntropy/Content/Particles/PRT_Light";

        public override void SetProperty() {
            ShouldKillWhenOffScreen = false;
            Lifetime = -1;   //旧VoidParticles不设LifeMax,alpha衰到Kill为止
            Opacity = 1f;
        }

        public override void AI() {
            //multShrink走乘法衰减(ShadewindLance那批),默认减法,两套旧语义别合并
            if (multShrink)
                Opacity *= ad;
            else
                Opacity -= ad;
            Velocity *= vd;
            if (Opacity < 0.02f)
                Kill();
        }

        //数据类,PreDraw恒false,别在这加sb.Draw
        public override bool PreDraw(SpriteBatch sb) => false;
    }

    //HACK:继承PRT_Void一半是给EffectLoader分流
    //DrawParticleEffects/DrawProjectileEffects过滤 is PRT_Void && is not PRT_Abyssal; 深渊走DrawAbyssalEffect→DrawParticleEffectsAlt,两套RT shader
    //另一半是Kill阈值0.05和没有multShrink分支,旧AbyssalParticles原值,别合并回父类
    public class PRT_Abyssal : PRT_Void
    {
        public override void AI() {
            Opacity -= ad;
            Velocity *= vd;
            if (Opacity < 0.05f)   //父类0.02,深渊那批旧阈值就是0.05
                Kill();
        }
    }
}
