using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class PlayerCamera : Node3D
    {
        [Export] private float m_mouseScale = 0.005f;
        [Export] private bool m_disableInput = false;
        [Export] private Timer m_noInputTimer;
        [Export] private float kRotationLerpWeight = 0.1f;
        [Export] private float kFocusResetLerpWeight = 0.9f;
        [Export] private float kMaxLockedAngleDeg = 20f;
        [Export] private float kExtraRotationMult = 1.5f;
        [Export] private float kExtraRotationClamp;
        [Export] private RigidBody3D m_rb;
        private Vector3 m_targetRotation;
        private bool m_focusing = false;
        public bool m_locked = true;
        private float kMaxLockedAngleRad;

        public override void _Ready()
        {
            if (!m_disableInput)
                m_noInputTimer.Timeout += ResetCamera;
            kMaxLockedAngleRad = Mathf.DegToRad(kMaxLockedAngleDeg);
        }

        private void ResetCamera()
        {
            m_targetRotation = Vector3.Zero;
        }

        public override void _Input(InputEvent @event)
        {
            if (m_disableInput)
                return;

            if (@event is InputEventMouseMotion)
            {
                InputEventMouseMotion mouseEvent = @event as InputEventMouseMotion;
                Vector3 newRotation = m_targetRotation;
                newRotation.Y -= mouseEvent.Relative.X * m_mouseScale;
                newRotation.X += mouseEvent.Relative.Y * m_mouseScale;
                m_targetRotation = newRotation;
                m_noInputTimer.Start();
            }
        }

        public override void _Process(double delta)
        {
            // Framerate independent weight calculation,
            // see bottom of page for more information:
            // https://docs.godotengine.org/en/4.4/tutorials/math/interpolation.html
            float inputRotationWeight = 1f - Mathf.Exp(-kRotationLerpWeight * (float)delta);

            if (m_disableInput)
                return;

            if (m_locked && !m_focusing)
            {
                m_targetRotation = m_targetRotation.LimitLength(
                    kMaxLockedAngleRad);
            }

            if (m_locked)
            {
                Vector3 localAngularVelocity = m_rb.AngularVelocity * m_rb.Basis;
                localAngularVelocity.X *= -1;
                localAngularVelocity.Z *= -1;
                Vector3 extraRotation = localAngularVelocity * kExtraRotationMult;
                extraRotation.LimitLength(kExtraRotationClamp);
                m_targetRotation += extraRotation;
            }

            Rotation = Rotation.Lerp(m_targetRotation, inputRotationWeight);
        }

        public void FocusOn(Node3D target)
        {
            if (!IsInstanceValid(target) || m_disableInput)            
                return;

            Vector3 oldRotation = Rotation;
            LookAt(target.GlobalPosition, null, true);
            m_targetRotation = Rotation;
            Rotation = Rotation.Lerp(oldRotation, kFocusResetLerpWeight);
            m_focusing = true;
        }

        public void TurnOffFocus()
        {
            m_targetRotation = Vector3.Zero;
            m_focusing = false;
        }
    }
}
