using Godot;
using System.IO;

namespace SFS
{
    public partial class EngineAudio : AudioStreamPlayer3D
    {
        [Export] private Plane m_plane;
        [Export] private string kIdleString;
        [Export] private string kABStartString;
        [Export] private float kIdleToABValue;
        private float m_previousValue;
        private AudioStreamPlaybackInteractive m_playback;
        private AudioStreamInteractive m_stream;

        public override void _Ready()
        {            
            m_stream = Stream as AudioStreamInteractive;            
            Debug.Assert(m_stream != null, "Audio stream is null!");
        }

        public override void _Process(double delta)
        {
            if (!Playing && !SceneManagement.IsLoading)
            {
                Play();
                m_playback = GetStreamPlayback() as AudioStreamPlaybackInteractive;
                Debug.Assert(m_playback != null, "Audio playback is null!");
            }

            float newValue = m_plane.m_ABValue;
            if (newValue > kIdleToABValue &&
                m_previousValue <= kIdleToABValue &&
                GetCurrentClipName().Equals(kIdleString))
            {
                m_playback.SwitchToClipByName(kABStartString);
            }
            else if (newValue < kIdleToABValue &&
                m_previousValue >= kIdleToABValue &&
                !GetCurrentClipName().Equals(kIdleString))
            {
                m_playback.SwitchToClipByName(kIdleString);
            }
            m_previousValue = newValue;
        }

        private string GetCurrentClipName()
        {
            int index = m_playback.GetCurrentClipIndex();
            return m_stream.GetClipName(index);
        }
    }
}
