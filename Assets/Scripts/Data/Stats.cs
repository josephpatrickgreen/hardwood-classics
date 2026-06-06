using System;

namespace ChainNet.Data
{
    [Serializable]
    public class Stats
    {
        public int handle;
        public int jumper;
        public int finish;
        public int bounce;
        public int vision;
        public int clamps;
        public int swipe;
        public int boards;
        public int motor;
        public int frame;
        public int swagger;
        public int nerve;
        public int edge;
        public int cool;

        public Stats Clone()
        {
            return (Stats)MemberwiseClone();
        }
    }
}
