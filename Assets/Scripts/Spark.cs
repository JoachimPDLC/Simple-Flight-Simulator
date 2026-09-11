using Godot;

namespace SFS
{
    public partial class Spark : Node3D
    {
        private GpuParticles3D m_particle = null;
        public override void _Ready()
        {
            m_particle = GetChild<GpuParticles3D>(0);            
            Debug.Assert(m_particle != null, "Spark particle is null!");
            m_particle.Finished += OnFinish;
            m_particle.Emitting = true;
        }

        // Sets the velocityPivot to the given direction. Direction should be a unitVector.
        public void Init(Vector3 direction)
        {            
            ParticleProcessMaterial copy = m_particle.ProcessMaterial as ParticleProcessMaterial;
            if (direction.IsNormalized())
                copy.VelocityPivot = direction;
            else
                copy.VelocityPivot = direction.Normalized();
            m_particle.ProcessMaterial = copy;            
        }

        public void OnFinish()
        {
            QueueFree();
        }
    }
}
