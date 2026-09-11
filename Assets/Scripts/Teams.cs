using Godot;
using Godot.Collections;

namespace SFS
{
    [GlobalClass]
    public partial class Teams : Resource
    {
        public enum Team : uint
        {
            kNone,
            kPlayer,
            kEnemy,
        }

        [Export] public Dictionary<Team, Color> kColorsDic { get; private set; }
    }
}
