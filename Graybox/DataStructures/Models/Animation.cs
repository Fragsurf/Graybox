using System.Collections.Generic;

namespace Graybox.DataStructures.Models
{
    public class Animation
    {
        public List<AnimationFrame> Frames { get; private set; }

        public Animation()
        {
            Frames = new List<AnimationFrame>();
        }
    }
}