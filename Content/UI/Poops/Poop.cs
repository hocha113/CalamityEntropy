using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Content.UI.Poops
{
    public abstract class Poop : ModType, ILoadable
    {
        public static List<Poop> instances;
        protected override void Register() {
            if (instances == null) { instances = new List<Poop>(); }
            instances.Add(this);
        }
        public virtual string texPath => (GetType().Namespace + "." + Name).Replace('.', '/');
        public virtual Texture2D texture => ModContent.Request<Texture2D>(texPath).Value;
        public virtual Texture2D getTexture() {
            return texture;
        }
        public static Poop getRandomPoopForPlayer(Player player) {
            float totalWeight = 0f;
            List<float> cumulativeWeights = new List<float>();
            var random = Main.rand;
            foreach (var instance in instances) {
                float weight = instance.getRollChanceBasedOnLuck(player.luck);

                totalWeight += weight;
                cumulativeWeights.Add(totalWeight);
            }

            float randomValue = (float)(random.NextDouble() * totalWeight);
            for (int i = 0; i < cumulativeWeights.Count; i++) {
                if (randomValue <= cumulativeWeights[i]) {
                    return instances[i];
                }
            }

            return new PoopNormal();
        }

        public virtual float getRollChance() {
            return 1;
        }

        public virtual float getRollChanceBasedOnLuck(float luck) {
            return getRollChance() + (luck / getRollChance()) * 0.45f;
        }

        public virtual int ProjectileType() {
            return -1;
        }
    }
}
