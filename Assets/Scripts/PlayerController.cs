using Godot;
using System;

namespace SFS
{ 
    [GlobalClass]
    public partial class PlayerController : Controller
    {
        [Export] private bool m_captureMouse = true;
        [Export] private bool m_useSimpleControls = false; 
        [Export] private float m_delayTime = 0.5f;
        [Export] private GameManager m_gameManager;

        [Export] private float kFocusTargetHoldTime = 0.1f;
        private float m_focusTargetHoldTimer = 0;

        [Export] private float kFocusTargetReleaseTime = 0.01f;
        private float m_focusTargetReleaseTimer = 0;

        [Export] private PlayerCamera m_playerCamera;

        [Export] private bool kNoUI = false;

        public int m_playerScore { get; private set; } = 0;
        private double m_timeElapsedSeconds = 0;
        public TimeSpan m_timeElapsed { get; private set; } = new TimeSpan(0, 0, 0);

        private bool m_intialDisabledControlLatch = false;

        private RandomNumberGenerator m_rng = new();

        private PlayerUI m_playerUI;

        public override void _Ready()
        {
            if (m_captureMouse)
                Input.MouseMode = Input.MouseModeEnum.Captured;
            else
                Input.MouseMode = Input.MouseModeEnum.Visible;
            m_plane.m_shootingGun = false;
            if (!m_useSimpleControls)
                m_useSimpleControls = GlobalSettings.sUseSimpleControls;
            m_plane.SetSimpleControls(m_useSimpleControls);

            if (!kNoUI)
            {
                m_playerUI = GetTree().GetFirstNodeInGroup("PlayerUI") as PlayerUI;
                Debug.Assert(m_playerUI != null, "PlayerController could not find UI!");
            }
        }

        public void AddScore(int amount) { m_playerScore += amount; }

        public override void _Input(InputEvent @event)
        {
            if (!kControlDisabledOverride)
            {
                if (@event.IsActionPressed("Escape"))
                {
                    if (!GetTree().Paused)                    
                        PauseGame();
                    else
                        UnpauseGame();
                }
            }

            if (m_plane == null || m_controlDisabled || kControlDisabledOverride ||
                GetTree().Paused)
                return;

            if (@event.IsActionPressed("Shoot Missile"))            
                m_plane.TryShootMissile();
        }

        public override void _Process(double delta)
        {
            m_timeElapsedSeconds += delta;
            m_timeElapsed = TimeSpan.FromSeconds(m_timeElapsedSeconds);
            if (m_timeElapsedSeconds > m_delayTime && !m_intialDisabledControlLatch)
            {
                m_controlDisabled = false;
                m_intialDisabledControlLatch = true;
            }

            if (m_plane == null || m_controlDisabled || kControlDisabledOverride)
                return;

            if (Input.IsActionJustPressed("Unlock Camera"))
                m_playerCamera.m_locked = false;

            if (Input.IsActionJustReleased("Unlock Camera"))
                m_playerCamera.m_locked = true;

            if (m_focusTargetReleaseTimer > 0)
            {
                m_focusTargetReleaseTimer -= (float)delta;
                if (m_focusTargetReleaseTimer <= 0)
                    FocusButtonReleased();
            }

            if (Input.IsActionJustReleased("Change Target"))
            {
                m_focusTargetReleaseTimer = kFocusTargetReleaseTime;
            }

            if (m_focusTargetHoldTimer >= kFocusTargetHoldTime)            
                m_playerCamera.FocusOn(m_plane.GetTarget());

            if (Input.IsActionPressed("Change Target"))
            {
                m_focusTargetHoldTimer += (float)delta;
                m_focusTargetReleaseTimer = 0;
            }
        }

        private void FocusButtonReleased()
        {
            if (m_focusTargetHoldTimer < kFocusTargetHoldTime)
                m_plane.SwitchTarget();
            else
                m_playerCamera.TurnOffFocus();
            m_focusTargetHoldTimer = 0;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (m_plane == null || m_controlDisabled || kControlDisabledOverride)
                return;

            if (Input.IsActionJustPressed("Shoot Gun"))            
                m_plane.m_shootingGun = true;            

            if (Input.IsActionJustReleased("Shoot Gun"))
                m_plane.m_shootingGun = false;

            if (Input.IsActionPressed("Thrust"))
                m_plane.m_throttleInput = 1;
            else if (!Input.IsActionPressed("Brake"))
                m_plane.m_throttleInput = 0;

            if (Input.IsActionPressed("Brake"))
                m_plane.m_throttleInput = -1;
            else if (!Input.IsActionPressed("Thrust"))
                m_plane.m_throttleInput = 0;

            HandleControlInput();
        }

        private void HandleControlInput()
        {
            Godot.Vector3 controlInput = Godot.Vector3.Zero;

            if (m_useSimpleControls)
            {
                if (Input.IsActionPressed("Pitch Down"))
                    controlInput.X = 1;
                if (Input.IsActionPressed("Pitch Up"))
                    controlInput.X = -1;
                if (Input.IsActionPressed("Roll Right"))
                {
                    controlInput.Y = -1;
                    controlInput.Z = 1;
                }
                if (Input.IsActionPressed("Roll Left"))
                {
                    controlInput.Y = 1;
                    controlInput.Z = -1;
                }
            }
            else
            {
                if (Input.IsActionPressed("Yaw Left"))
                    controlInput.Y = 1;
                if (Input.IsActionPressed("Yaw Right"))
                    controlInput.Y = -1;
                if (Input.IsActionPressed("Roll Right"))
                    controlInput.Z = 1;
                if (Input.IsActionPressed("Roll Left"))
                    controlInput.Z = -1;
                if (Input.IsActionPressed("Pitch Down"))
                    controlInput.X = 1;
                if (Input.IsActionPressed("Pitch Up"))
                    controlInput.X = -1;
            }
            m_plane.m_controlInput = controlInput;
        }
        public override void OnDamaged(Node _damager)
        {
            if (!kNoUI)
                m_playerUI.OnDamaged();
        }

        public override void OnDeath(Node _damager)
        {
            m_controlDisabled = true;
            m_plane.m_controlInput = new(0, 0, 0);
            m_plane.m_throttleInput = 0;

            if (!kNoUI)
                m_playerUI.OnPlayerDeath();
        }

        public override void OnKill()
        {
            if (!kNoUI)
                m_playerUI.OnKill();
            m_plane.AddMissilesAmmo(m_rng.RandiRange(
                m_gameManager.m_minAmmoGain,
                m_gameManager.m_maxAmmoGain));
        }

        public void OnGameStart()
        {
            m_timeElapsed = new TimeSpan(0, 0, 0);
            m_timeElapsedSeconds = 0;
        }

        public void SetSimpleControls(bool value)
        {
            m_useSimpleControls = value;
            m_plane.SetSimpleControls(value);
        }

        public void PauseGame()
        {
            if (m_plane.IsDead())
                return;

            m_playerUI.ShowPauseMenu();
            Input.MouseMode = Input.MouseModeEnum.Visible;
            GetTree().Paused = true;
        }

        public void UnpauseGame()
        {
            if (m_plane.IsDead())
                return;

            m_playerUI.HidePauseMenu();
            Input.MouseMode = Input.MouseModeEnum.Captured;
            GetTree().Paused = false;
        }
    }
}
