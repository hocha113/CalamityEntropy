using CalamityEntropy.Content.ILEditing;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.Items.Books.BookMarks
{
    public class BookMarkLunar : BookMark
    {
        public static int distance = 340;
        public override Texture2D UITexture => BookMark.GetUITexture("Lunar");
        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(platinum: 1);
        }
        public override Color tooltipColor => Color.YellowGreen;
        public override EBookProjectileEffect getEffect() {
            return new LunarBMEffect();
        }
    }
    /// <summary>夜明书签(2026-08-31 平衡案重做):命中时在敌人头顶落下2~3道月耀射线(固定基伤180)。</summary>
    public class LunarBMEffect : EBookProjectileEffect
    {
        public override void UpdateProjectile(Projectile projectile, bool ownerClient) {
            (projectile.ModProjectile as EBookBaseProjectile).color = Color.YellowGreen;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damageDone) {
            Player owner = projectile.GetOwner();
            int count = Main.rand.Next(2, 4);
            for (int i = 0; i < count; i++) {
                Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-140, 140), -Main.rand.NextFloat(480, 620));
                int p = Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos,
                    (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 15f,
                    ProjectileID.LunarFlare, FixedDamage(owner, 180, projectile.DamageType), projectile.knockBack, projectile.owner);
                if (p >= 0 && p < Main.maxProjectiles) {
                    Main.projectile[p].DamageType = projectile.DamageType;
                }
            }
            CEUtils.PlaySound("light_bolt", 1, target.Center);
        }
    }
    public class LunarBMGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public float progress = 0;
        public int decreaceCd = 0;
        public override void AI(NPC npc) {
            if (decreaceCd > 0) {
                decreaceCd--;
            }
            else {
                if (progress > 0)
                    progress -= 0.005f;
            }
        }
        public override void DrawEffects(NPC npc, ref Color drawColor) {
            if (CalamityEntropy.EntropyMode) {
                if (npc.type == NPCID.CultistBoss || npc.type == NPCID.Golem || npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight || npc.type == NPCID.GolemHead || npc.type == NPCID.GolemHeadFree || npc.type == NPCID.CultistBossClone || npc.type == NPCID.AncientLight || npc.type == NPCID.AncientDoom || EModILEdit.LostNPCsEntropy.Contains(npc.type))
                    drawColor = Color.Black;
            }
            drawColor = Color.Lerp(drawColor, new Color(9, 30, 72), progress);
        }
    }
}
