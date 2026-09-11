using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class ParticleHandler : Node3D
    {
        [Export] private GpuParticles3D particles;

        public async void DeleteAfterTime(int time)
        {
            Node parent = GetParent();
            parent.RemoveChild(this);
            Notification((int)NotificationInternalProcess);
            Utilities.GetFirstNode3D(parent).AddChild(this);
            particles.Emitting = false;
            await ToSignal(GetTree().CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
            QueueFree();
        }
    }
}
