using Godot;

namespace SFS
{
    public partial class EngineAfterburner : Node3D
    {
        private GpuParticles3D m_particle = null;
        private OmniLight3D m_light = null;
        public override void _Ready()
        {
            m_particle = GetChild<GpuParticles3D>(0);
            Debug.Assert(m_particle != null, "Afterburner particle is null!");
            m_light = GetChild<OmniLight3D>(1);
            Debug.Assert(m_light != null, "Afterburner light is null!");
        }

        // Expects a value between -1 and 1.
        public void SetPower(float input)
        {
            if (input <= 0)
            {
                m_particle.Emitting = false;
                m_light.LightEnergy = 0;
            }
            else
            {
                m_particle.Emitting = true;
                m_particle.AmountRatio = input;
                m_light.LightEnergy = input / 20;
            }
        }
    }
}
