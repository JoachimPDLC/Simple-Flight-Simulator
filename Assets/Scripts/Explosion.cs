using Godot;

namespace SFS
{
    public partial class Explosion : Node3D
    {
        private GpuParticles3D m_particle = null;
        public override void _Ready()
        {
            m_particle = GetChild<GpuParticles3D>(0);
            Debug.Assert(m_particle != null, "Explosion particle is null!");
            m_particle.Finished += OnFinish;
            m_particle.Emitting = true;
        }

        public void ScaleParticle(float min, float max)
        {
            m_particle ??= GetChild<GpuParticles3D>(0);
            Debug.Assert(m_particle != null, "Explosion particle is null!");

            ParticleProcessMaterial copy = m_particle.ProcessMaterial as ParticleProcessMaterial;
            copy.ScaleMin = min;
            copy.ScaleMin = max;
            m_particle.ProcessMaterial = copy;
        }

        public void OnFinish()
        {
            QueueFree();
        }
    }
}
