using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityEntropy.Common
{
    public class LoopSound
    {
        public SoundEffectInstance instance;
        public int timeleft = 2;
        public void setVolume(float v) {
            if (instance == null)
                return;
            if (!Main.dedServ) {
                try {
                    instance.Volume = v * Main.soundVolume;
                } catch { }
            }
        }
        public void setVolume_Dist(Vector2 center, float mindist, float maxdist, float volume = 1) {
            if (instance == null)
                return;
            try {
                if (!Main.dedServ) {
                    if (CEUtils.getDistance(center, Main.LocalPlayer.Center) > mindist) {
                        if (CEUtils.getDistance(center, Main.LocalPlayer.Center) > maxdist) {
                            setVolume(0);
                        }
                        else {
                            setVolume((1 - (float)(CEUtils.getDistance(center, Main.LocalPlayer.Center) - mindist) / (maxdist - mindist)) * volume);
                        }
                    }
                    else {
                        setVolume(volume);
                    }
                }
            } catch { }
        }
        public LoopSound(SoundEffect sf) {
            if (!Main.dedServ) {
                try {
                    instance = sf.CreateInstance();
                    instance.IsLooped = true;
                } catch { }
            }
        }
        public void play() {
            if (instance == null)
                return;
            if (!Main.dedServ && LoopSoundManager.sounds != null) {
                if (LoopSoundManager.sounds.Count < 5) {
                    if (ModContent.GetInstance<Config>().EnableLoopingSound) {
                        try {
                            instance.Play();
                        } catch { }
                        LoopSoundManager.sounds.Add(this);
                    }
                }
            }
        }
        public void stop() {
            if (instance == null)
                return;
            if (!Main.dedServ) {
                try {
                    instance.Stop();
                } catch { }
            }
        }
    }
    public static class LoopSoundManager
    {
        public static List<LoopSound> sounds;
        public static void init() {
            sounds = new List<LoopSound>();
        }

        public static void unload() {
            if (sounds is not null) {
                foreach (var sound in sounds) {
                    sound.stop();
                }
            }
            sounds = null;
        }

        public static void update() {
            for (int i = sounds.Count - 1; i >= 0; i--) {
                if (sounds[i].timeleft-- <= 0) {
                    sounds[i].stop();
                    sounds.RemoveAt(i);
                }
            }
        }
    }
}
