using Godot;
using Godot.Collections;

namespace SFS
{
    public partial class F15 : Node3D
    {
        [Export] private Plane m_plane;
        [Export] private Array<GpuParticles3D> m_airLayerParticles;

        [ExportGroup("Meshes")]
        [Export] private Node3D m_leftAileron;
        [Export] private Node3D m_rightAileron;
        [Export] private Node3D m_leftStabilator;
        [Export] private Node3D m_rightStabilator;
        [Export] private Node3D m_leftRudder;
        [Export] private Node3D m_rightRudder;
        [Export] private Node3D m_airBrake;

        [ExportGroup("Materials")]
        [Export] private Array<MeshInstance3D> m_bodyMeshes;
        [Export] private Material m_bodyMaterial;
        [Export] private Array<MeshInstance3D> m_cockpitMeshes;
        [Export] private Material m_cockpitMaterial;
        [Export] private Array<MeshInstance3D> m_enginesMeshes;
        [Export] private Material m_EnginesMaterial;
        [Export] private Array<MeshInstance3D> m_noseMeshes;
        [Export] private Material m_noseMaterial;

        [ExportGroup("Angles")]
        [Export] private float kMaxStabilatorAngleDeg;
        [Export] private float kMaxRudderAngleDeg;
        [Export] private float kMaxAileronAngleDeg;
        [Export] private float kMaxAirBrakeAngleDeg;

        [Export] private float kLerpWeight = 0.1f;

        private float m_maxStabilatorAngleRad;
        private float m_maxRudderAngleRad;
        private float m_maxAileronAngleRad;
        private float m_maxAirBrakeAngleRad;

        public override void _Ready()
        {
            m_maxStabilatorAngleRad = Mathf.DegToRad(kMaxStabilatorAngleDeg);
            m_maxRudderAngleRad = Mathf.DegToRad(kMaxRudderAngleDeg);
            m_maxAileronAngleRad = Mathf.DegToRad(kMaxAileronAngleDeg);
            m_maxAirBrakeAngleRad = Mathf.DegToRad(kMaxAirBrakeAngleDeg);
        
            foreach (var mesh in m_bodyMeshes)
                mesh.MaterialOverride = m_bodyMaterial;
            foreach (var mesh in m_cockpitMeshes)
                mesh.MaterialOverride = m_cockpitMaterial;
            foreach (var mesh in m_enginesMeshes)
                mesh.MaterialOverride = m_EnginesMaterial;
            foreach (var mesh in m_noseMeshes)
                mesh.MaterialOverride = m_noseMaterial;
        }

        public override void _Process(double delta)
        {
            if (m_plane.m_controlInput.X < 0)
            {
                foreach (var particle in m_airLayerParticles)
                    particle.Emitting = true;
            }
            else
            {
                foreach (var particle in m_airLayerParticles)
                    particle.Emitting = false;
            }

            // Framerate independent weight calculation,
            // see bottom of page for more information:
            // https://docs.godotengine.org/en/4.4/tutorials/math/interpolation.html
            float weight = 1f - Mathf.Exp(-kLerpWeight * (float)delta);

            float targetStabilatorAngle = -m_plane.m_controlInput.X * m_maxStabilatorAngleRad;
            float leftLerpedAngle = Mathf.LerpAngle(
                m_leftStabilator.Rotation.Z,
                targetStabilatorAngle,
                weight);
            m_leftStabilator.Rotation = new(0, 0, leftLerpedAngle);
            float rightLerpedAngle = Mathf.LerpAngle(
                m_rightStabilator.Rotation.Z,
                targetStabilatorAngle,
                weight);
            m_rightStabilator.Rotation = new(0, 0, rightLerpedAngle);

            float targetRudderAngle = m_plane.m_controlInput.Y * m_maxRudderAngleRad;
            leftLerpedAngle = Mathf.LerpAngle(
                m_leftRudder.Rotation.Y,
                targetRudderAngle,
                weight);
            m_leftRudder.Rotation = new(0, leftLerpedAngle, 0);
            rightLerpedAngle = Mathf.LerpAngle(
                m_rightRudder.Rotation.Y,
                targetRudderAngle,
                weight);
            m_rightRudder.Rotation = new(0, rightLerpedAngle, 0);

            float targetAileronAngle = m_plane.m_controlInput.Z * m_maxRudderAngleRad;
            leftLerpedAngle = Mathf.LerpAngle(
                m_leftAileron.Rotation.Z,
                targetAileronAngle,
                weight);
            m_leftAileron.Rotation = new(0, 0, leftLerpedAngle);
            rightLerpedAngle = Mathf.LerpAngle(
                m_rightAileron.Rotation.Z,
                -targetAileronAngle,
                weight);
            m_rightAileron.Rotation = new(0, 0, rightLerpedAngle);

            float brakesDeployed = m_plane.m_airBrakesDeployed? 1 : 0;
            float targetAirBrakeAngle = brakesDeployed * m_maxAirBrakeAngleRad;
            float flapLerpedAngle = Mathf.LerpAngle(
                m_airBrake.Rotation.Z,
                targetAirBrakeAngle,
                weight);
            m_airBrake.Rotation = new(0, 0, flapLerpedAngle);
        }
    }
}
