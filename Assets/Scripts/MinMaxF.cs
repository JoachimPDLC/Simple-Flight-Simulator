using Godot;

namespace SFS
{
    [GlobalClass, Tool]
    public partial class MinMaxF : Resource
    {
        [Export] public float Min;
        [Export] public float Max;

        /// <summary>
        /// Returns a random float between Min and Max (inclusive).
        /// </summary>
        /// <returns></returns>
        public float Rand()
        {
            RandomNumberGenerator rng = new();
            return Rand(rng);
        }

        /// <summary>
        /// Returns a random float between Min and Max (inclusive).
        /// </summary>
        /// <returns></returns>
        public float Rand(RandomNumberGenerator rng)
        {
            return rng.RandfRange(Min, Max);
        }

        /// <summary>
        /// Exclusive range check.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public bool IsInRange(float number)
        {
            return number > Min && number < Max;
        }
    }
}
