using Terraria.ID;

namespace CalamityEntropy.Content.Items.Weapons.Aftermath.NebulaVortexStaff
{
    /// <summary>
    /// 涡旋旋臂尖射出的粉紫星云烈焰:贴图与 AI 都是原版星云烈焰,250 像素内追踪
    /// </summary>
    public class NebulaVortexStaffBlaze : NebulaVortexStaffBlazeBase
    {
        //光晕与火花色:原版星云烈焰的粉紫
        private static readonly Color Bloom = new Color(255, 80, 200);
        private static readonly Color Spark = new Color(255, 170, 235);
        //光晕半径(像素);消散音效抬高音高,听感比蓝焰轻
        private const float Radius = 26f;
        private const float Pitch = 0.4f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.NebulaBlaze1}";
        protected override int VanillaType => ProjectileID.NebulaBlaze1;
        protected override Color BloomColor => Bloom;
        protected override Color SparkColor => Spark;
        protected override float BloomRadius => Radius;
        protected override float KillPitch => Pitch;
    }
}
