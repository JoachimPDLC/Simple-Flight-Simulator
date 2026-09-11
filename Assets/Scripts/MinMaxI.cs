using Godot;

namespace SFS
{
    [GlobalClass, Tool]
    public partial class MinMaxI : Resource
    {
        [Export] public int Min;
        [Export] public int Max;

        /// <summary>
        /// Returns a random int between Min and Max (inclusive).
        /// </summary>
        /// <returns></returns>
        public int Rand()
        {
            RandomNumberGenerator rng = new();
            return Rand(rng);
        }

        /// <summary>
        /// Returns a random float between Min and Max (inclusive).
        /// </summary>
        /// <returns></returns>
        public int Rand(RandomNumberGenerator rng)
        {
            return rng.RandiRange(Min, Max);
        }
    }
}
