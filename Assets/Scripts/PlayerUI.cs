using Godot;
using System;
using System.Collections.Generic;

namespace SFS
{
    [GlobalClass]
    public partial class PlayerUI : Control
    {
        [Export] Plane m_plane;
        GameEntityComponent m_gameEntityComponent;
        [Export] SubViewportContainer m_pixelationViewport;
        [Export] GameManager m_gameManager;
        [Export] private SubViewportContainer m_viewport;
        private Camera3D m_camera = null;
        PlayerController m_playerController = null;

        [ExportGroup("Messages")]
        [Export] private Godot.CenterContainer m_hitMessage;
        [Export] private Timer m_hitTimer;
        [Export] private Godot.CenterContainer m_killMessage;
        [Export] private Timer m_killTimer;
        [Export] private Godot.CenterContainer m_missMessage;
        [Export] private Timer m_missTimer;
        [Export] private Godot.CenterContainer m_damagedMessage;
        [Export] private Timer m_damagedTimer;
        [Export] private Godot.CenterContainer m_stallMessage;
        [Export] private Timer m_stallTimer;
        [Export] private Godot.CenterContainer m_missileMessage;
        [Export] private Timer m_offCourseTimer;
        [Export] private string m_baseOffCourseText;
        [Export] private Godot.CenterContainer m_offCourseMessage;
        [Export] private Godot.Label m_offCourseLabel;
        [Export] private Godot.CenterContainer m_pullUpMessage;
        [Export] private Timer m_pullUpTimer;
        [Export] private RayCast3D m_pullUpRaycast;
        [Export] private Godot.CenterContainer m_enemyWaveMessage;
        [Export] private Timer m_enemyWaveTimer;
        [Export] private Control[] m_flashingUI;
        [Export] private Color m_baseMessageColor;
        [Export] private Color m_flashingColor;
        [Export] private float m_flashTime = 1;
        private float m_flashTimer = 0;

        [ExportGroup("Labels")]
        [Export] private Godot.Label m_speedLabel;
        [Export] private Godot.Label m_altitudeLabel;
        [Export] private Godot.TextureRect m_boreSight;
        [Export] private Godot.TextureRect m_velocityMarker;
        [Export] private Godot.Label m_healthLabel;
        [Export] private Godot.Label m_hullLabel;
        [Export] private Godot.Label m_scoreLabel;

        [ExportGroup("Health")]
        [Export] private int m_screenPosAdjustment = 25;
        [Export] private Godot.ProgressBar m_healthBar;

        [ExportGroup("Missiles")]
        [Export] private Godot.Color m_availableMissileColor;
        [Export] private Godot.Color m_emptyMissileColor;
        [Export] private Godot.Label m_missileTypeLabel;
        [Export] private Godot.Label m_missileAmmoLabel;
        [Export] private Godot.HBoxContainer m_missileContainer;
        private Godot.Collections.Array<ColorRect> m_missileIndicators = new();

        [ExportGroup("IncomingMissiles")]
        [Export] private PackedScene m_missileAlertScene;
        private List<MissileAlertUI> m_missileAlerts = new();

        [ExportGroup("Targeting")]
        [Export] private Godot.TextureRect m_targetRect;
        [Export] private Godot.TextureRect m_enemyRect;
        [Export] private Godot.TextureRect m_reticleRect;
        [Export] private float m_minRectScale;
        [Export] private float m_maxScaleDistance;
        [Export] private float m_minScaleDistance;
        [Export] private uint m_targetDistanceCull = 15000;
        private float m_slope;
        private float m_yIntercept;
        [Export] private Godot.Color m_baseReticleColor;
        [Export] private Godot.Color m_reticleDamageColor;
        [Export] private float kReticleColorTime;
        private float m_currentReticleColorTime;
        [Export] private int m_reticleAppearDistance;
        private List<Godot.TextureRect> m_textureRects = new();
        [Export] private Godot.Color m_targetColor;
        [Export] private Godot.Color m_lockedTargetColor;
        [Export] private Node3D m_targetTrackerPivot;
        [Export] private Control m_targetLabels;
        [Export] private Label m_targetDistanceLabel;
        [Export] private Label m_targetNameLabel;

        [ExportGroup("OnDeath")]
        [Export] private string m_mainMenuScenePath;
        [Export] private BaseButton m_mainMenuButton;
        [Export] private BaseButton m_restartButton;
        [Export] private Label m_totalScoreLabel;
        [Export] private Label m_totalTimeLabel;

        [ExportGroup("PauseMenu")]
        [Export] private BaseButton m_resumeButton;
        [Export] private BaseButton m_quitButton;
        [Export] private uint kDefaultStretchShrink = 6;
        [Export] private uint kPausedStretchShrink = 12;

        [ExportGroup("CameraDistance")]
        [Export] private SpringArm3D m_springArm;
        [Export] private float m_cameraDistChange;
        [Export] private float kDistanceLerpSpeed;
        private float kBaseCameraDistance;

        [ExportGroup("Controls")]
        [Export] private Label m_controlsMainLabel;
        [Export] private Label m_controlsLabel;
        [Export] private Label m_simpleControlsLabel;

        private bool m_dead = false;

        public override void _Ready()
        {
            base._Ready();
            Godot.Collections.Array<Node> children = m_missileContainer.GetChildren();
            foreach (Node child in children)
            {
                ColorRect rect = child as ColorRect;
                m_missileIndicators.Add(rect);
            }
            m_gameEntityComponent = m_plane.GetGameEntityComponent();
            m_healthBar.MaxValue = m_gameEntityComponent.m_maxHealth;
            m_camera = GetTree().GetFirstNodeInGroup("Camera") as Camera3D;
            Debug.Assert(m_camera != null, "PlayerUI could not find Camera!");
            m_playerController = m_plane.m_controller as PlayerController;
            Debug.Assert(m_playerController != null, "PlayerUI could not find the player controller!");

            float maxScaleDistanceSqrd = m_maxScaleDistance * m_maxScaleDistance;
            float minScaleDistanceSqrd = m_minScaleDistance * m_minScaleDistance;

            m_slope = (m_minRectScale - 1) /
                (maxScaleDistanceSqrd - minScaleDistanceSqrd);
            m_yIntercept = m_slope * minScaleDistanceSqrd + m_minRectScale;

            kBaseCameraDistance = m_springArm.SpringLength;

            // Init messages
            m_missMessage.Hide();
            m_missTimer.Timeout += () => { m_missMessage.Hide(); };
            m_hitMessage.Hide();
            m_hitTimer.Timeout += () => { m_hitMessage.Hide(); };
            m_killMessage.Hide();
            m_killTimer.Timeout += () => { m_killMessage.Hide(); };
            m_damagedMessage.Hide();
            m_damagedTimer.Timeout += () => { m_damagedMessage.Hide(); };
            m_stallMessage.Hide();
            m_stallTimer.Timeout += () => { m_stallMessage.Hide(); };
            m_missileMessage.Hide();
            m_offCourseTimer.Timeout += () => { m_offCourseMessage.Hide(); };
            m_offCourseMessage.Hide();
            m_pullUpTimer.Timeout += () => { m_pullUpMessage.Hide(); };
            m_pullUpMessage.Hide();
            m_enemyWaveTimer.Timeout += () => { m_enemyWaveMessage.Hide(); };
            m_enemyWaveMessage.Hide();

            m_totalScoreLabel.Hide();
            m_totalTimeLabel.Hide();
            m_mainMenuButton.Hide();
            m_restartButton.Hide();

            m_controlsLabel.Hide();
            m_simpleControlsLabel.Hide();

            m_reticleRect.Hide();
            m_enemyRect.Hide();

            m_resumeButton.Hide();
            m_quitButton.Hide();

            m_mainMenuButton.Pressed += MainMenuButtonPressed;
            m_restartButton.Pressed += RestartButtonPressed;
            m_resumeButton.Pressed += ResumeButtonPressed;
            m_quitButton.Pressed += QuitButtonPressed;
        }

        public override void _Process(double delta)
        {
            if (m_dead)
                return;

            m_speedLabel.Text = "SPD: " + ((int)m_plane.GetSpeed()).ToString();
            m_altitudeLabel.Text = "ALT: " + ((int)m_plane.GlobalPosition.Y).ToString();
            UpdateBoreSight();
            UpdateVelocityIndicator();
            UpdateHealth();
            UpdateAmmo();
            UpdateMissileAlertUI();
            UpdateTargets();
            UpdateTargetTracker();
            UpdateReticle();
            UpdateScore();
            UpdateMessages((float)delta);
            UpdateCameraDistance((float)delta);

            if (Input.IsActionJustPressed("Show Controls"))
            {
                if (m_controlsMainLabel.Visible)
                {
                    m_controlsMainLabel.Hide();
                    if (GlobalSettings.sUseSimpleControls)
                        m_simpleControlsLabel.Show();
                    else
                        m_controlsLabel.Show();
                }
                else
                {
                    m_controlsMainLabel.Show();
                    m_simpleControlsLabel.Hide();
                    m_controlsLabel.Hide();
                }
            }

            m_flashTimer += (float)delta;
            if (m_flashTimer >= m_flashTime)
            {
                m_flashTimer = 0;
                foreach (Control element in m_flashingUI)
                {
                    if (!element.Visible)
                        continue;

                    if (element.Modulate == m_baseMessageColor)
                        element.Modulate = m_flashingColor;
                    else
                        element.Modulate = m_baseMessageColor;
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            var spaceState = m_plane.GetWorld3D().DirectSpaceState;

        }

        private void UpdateReticle()
        {
            RigidBody3D target = m_plane.GetTarget() as RigidBody3D;
            if (target == null || !IsInstanceValid(target))
            {
                m_reticleRect.Hide();
                return;
            }

            Vector3 leadPos = Utilities.FirstOrderIntercept(m_plane.GlobalPosition,
                m_plane.LinearVelocity, m_plane.GetBulletVelocity(),
                target.GlobalPosition, target.LinearVelocity);

            if (leadPos.DistanceTo(m_plane.GlobalPosition) <
                m_reticleAppearDistance)
            {
                m_reticleRect.Show();
                m_reticleRect.GlobalPosition = m_camera.UnprojectPosition(leadPos);
                m_reticleRect.GlobalPosition *= m_pixelationViewport.StretchShrink;
            }
            else            
                m_reticleRect.Hide();
        }

        private void UpdateBoreSight()
        {
            Vector3 forwards = m_plane.GlobalBasis.Z;
            Vector3 worldPos = m_camera.GlobalPosition + forwards;
            if (m_camera.IsPositionBehind(worldPos))
            {
                m_boreSight.Hide();
                return;
            }
            else
                m_boreSight.Show();
            Vector2 screenPos = m_camera.UnprojectPosition(worldPos);
            screenPos *= m_pixelationViewport.StretchShrink;
            screenPos.X -= m_screenPosAdjustment;
            m_boreSight.GlobalPosition = screenPos;
        }

        private void UpdateVelocityIndicator()
        {
            Vector3 worldPos = m_camera.GlobalPosition + m_plane.LinearVelocity;
            if (m_camera.IsPositionBehind(worldPos))
            {
                m_velocityMarker.Hide();
                return;
            }
            else
                m_velocityMarker.Show();
            Vector2 screenPos = m_camera.UnprojectPosition(worldPos);
            screenPos *= m_pixelationViewport.StretchShrink;
            screenPos.X -= m_screenPosAdjustment;
            m_velocityMarker.GlobalPosition = screenPos;
        }

        private void UpdateHealth()
        {
            m_healthBar.SetValueNoSignal(m_gameEntityComponent.m_health);
            m_healthLabel.Text = m_gameEntityComponent.m_health.ToString();
        }

        private void UpdateAmmo()
        {
            m_missileAmmoLabel.Text = m_plane.GetMissilesAmmo().ToString();
            int numMissiles = m_plane.GetNumMissiles();
            for (int i = m_missileIndicators.Count - 1; i >= 0; i--)
            {
                if (numMissiles > 0)
                {
                    m_missileIndicators[i].Color = m_availableMissileColor;
                    --numMissiles;
                }
                else
                    m_missileIndicators[i].Color = m_emptyMissileColor;
            }
        }

        private void UpdateMissileAlertUI()
        {
            List<Missile> incomingMissiles = m_plane.GetTrackingMissiles();

            while (incomingMissiles.Count > m_missileAlerts.Count)
            {
                Node newAlert = m_missileAlertScene.Instantiate();
                Debug.Assert(newAlert != null, "newAlert is null!");
                m_plane.AddChild(newAlert);
                m_missileAlerts.Add(newAlert as MissileAlertUI);
            }

            foreach (Missile missile in incomingMissiles)
            {
                if (!IsInstanceValid(missile))
                    continue;

                if (!CheckForCoupledMissileAlert(missile))
                {
                    foreach (MissileAlertUI alert in m_missileAlerts)
                    {
                        if (alert == null)
                            continue;

                        if (!alert.m_enabled)
                        {
                            alert.TurnOn();
                            alert.m_target = missile;
                            break;
                        }
                    }
                }
            }
        }

        private bool CheckForCoupledMissileAlert(Missile missile)
        {
            foreach (MissileAlertUI alert in m_missileAlerts)
            {
                if (alert == null)
                    continue;

                if (alert.m_target == missile && alert.m_enabled)
                    return true;
            }
            return false;
        }

        private void UpdateTargets()
        {
            Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("Enemies");
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].ProcessMode == ProcessModeEnum.Disabled)
                    enemies.Remove(enemies[i]);

                Node3D node3D = enemies[i] as Node3D;

                if (node3D == null)
                    enemies.Remove(enemies[i]);

                if (node3D.GlobalPosition.DistanceTo(m_plane.GlobalPosition) >
                    m_targetDistanceCull)
                {
                    enemies.Remove(enemies[i]);
                }
            }

            while (enemies.Count > m_textureRects.Count)
            {
                Node newRect = m_enemyRect.Duplicate();
                AddChild(newRect);
                m_textureRects.Add(newRect as TextureRect);
            }

            if (m_plane.GetTarget() == null)
            {
                m_targetRect.Hide();
            }

            if (enemies.Count <= 0)
            {
                foreach (var rect in m_textureRects)
                    rect.Hide();
                m_targetRect.Hide();
                return;
            }

            for (int i = 0; i < m_textureRects.Count; i++)
            {
                m_textureRects[i].Hide();
                if (i < enemies.Count)
                {
                    Node3D currentEnemy = enemies[i] as Node3D;
                    Vector3 enemyPos = currentEnemy.GlobalPosition;

                    // Handle current target
                    if (currentEnemy == m_plane.GetTarget())
                    {
                        if (m_camera.IsPositionBehind(enemyPos))
                        {
                            m_targetRect.Hide();
                            continue;
                        }
                        m_targetRect.Show();
                        m_textureRects[i].Show();
                        int distance = (int)m_plane.GlobalPosition.DistanceTo(enemyPos);
                        m_targetDistanceLabel.Text = distance.ToString();
                        Plane enemyPlane = currentEnemy as Plane;
                        Debug.Assert(enemyPlane != null, "Enemy as plane was null!");
                        m_targetNameLabel.Text = enemyPlane.kName;
                        Vector2 planePosOnScreen = m_camera.UnprojectPosition(enemyPos);
                        planePosOnScreen *= m_pixelationViewport.StretchShrink;
                        Vector2 pos = new(planePosOnScreen.X - (m_targetRect.Size.X / 2),
                            planePosOnScreen.Y - (m_targetRect.Size.Y / 2));
                        m_targetRect.Position = pos;
                        m_textureRects[i].Position = pos;
                        SetScale(m_targetRect, enemyPos);
                        SetScale(m_textureRects[i], enemyPos);

                        if (!m_plane.m_countdownLockOnTime && !m_plane.IsLockedOn())
                        {
                            m_targetRect.Modulate = m_targetColor;
                            m_textureRects[i].Modulate = m_targetColor;
                            continue;
                        }

                        Color color = m_targetColor;
                        float lockProgress = m_plane.GetLockOnPercentage();
                        if (lockProgress <= 0)
                            color = m_targetColor;
                        else if (lockProgress >= 100)
                            color = m_lockedTargetColor;
                        else
                        {
                            color = m_targetColor.Lerp(m_lockedTargetColor,
                                lockProgress / 100);
                        }

                        m_targetRect.Modulate = color;
                        m_textureRects[i].Modulate = color;
                    }
                    else
                    {
                        if (m_camera.IsPositionBehind(enemyPos))
                        {
                            m_textureRects[i].Hide();
                            continue;
                        }
                        m_textureRects[i].Show();
                        m_textureRects[i].Modulate = m_targetColor;
                        Godot.Vector2 planePosOnScreen = m_camera.UnprojectPosition(enemyPos);
                        planePosOnScreen *= m_pixelationViewport.StretchShrink;
                        Godot.Vector2 pos = new Vector2(planePosOnScreen.X - (m_targetRect.Size.X / 2),
                            planePosOnScreen.Y - (m_targetRect.Size.Y / 2));
                        m_textureRects[i].Position = pos;
                        SetScale(m_textureRects[i], enemyPos);
                    }
                }
                else
                    m_textureRects[i].Hide();
            }
        }

        private void SetScale(TextureRect rect, Vector3 enemyPos)
        {            
            double sqrdDist = enemyPos.DistanceSquaredTo(m_plane.GlobalPosition);
            
            double newScale = -m_slope * sqrdDist + m_yIntercept;
            float scale = Mathf.Lerp(m_minRectScale, 1, (float)newScale);
            scale = Mathf.Clamp(scale, m_minRectScale, 1);
            rect.Scale = new(scale, scale);

            if (rect == m_targetRect)
                m_targetLabels.Scale = new(1 / scale, 1 / scale);
        }

        private void UpdateTargetTracker()
        {
            Node3D target = m_plane.GetTarget();

            if (target == null || !IsInstanceValid(target))
            {
                m_targetTrackerPivot.Hide();
                return;
            }

            // Target is behind camera, so show tracker and track target.
            if (!m_camera.IsPositionInFrustum(target.GlobalPosition))
            {
                m_targetTrackerPivot.Show();
                m_targetTrackerPivot.LookAt(target.GlobalPosition);
            }
            else // Target is visible, so hide tracker.
                m_targetTrackerPivot.Hide();
        }

        private void UpdateScore()
        {
            m_scoreLabel.Text = "SCORE: " + m_playerController.m_playerScore.ToString();
        }

        private void UpdateMessages(float delta)
        {
            if (m_dead)
                return;

            if (m_plane.m_stalling)
            {
                m_stallMessage.Show();
                m_stallTimer.Start();
                m_damagedMessage.Hide();
            }

            if (m_currentReticleColorTime > 0)
                m_currentReticleColorTime -= delta;
            else
                m_reticleRect.Modulate = m_baseReticleColor;

            if (m_plane.IsBeingTracked())
                m_missileMessage.Show();
            else
                m_missileMessage.Hide();

            if (m_pullUpRaycast.IsColliding())
            {
                GodotObject collider = m_pullUpRaycast.GetCollider();
                if (collider is StaticBody3D ||
                    collider is CsgCombiner3D)
                {
                    m_pullUpMessage.Show();
                    m_pullUpTimer.Start();
                    m_plane.PlayAlertBeep();
                }
            }
        }

        private void UpdateCameraDistance(float delta)
        {
            float throttle = m_plane.m_throttleInput;
            float length = m_springArm.SpringLength;
            float newLength = kBaseCameraDistance + throttle * -m_cameraDistChange;

            float weight = 1f - Mathf.Exp(-kDistanceLerpSpeed * (float)delta);
            m_springArm.SpringLength = Mathf.Lerp(length, newLength, weight);
        }

        public void OnBulletHit()
        {
            if (m_killTimer.IsStopped())
            {
                m_hitMessage.Show();
                m_hitTimer.Start();

                m_missMessage.Hide();

                m_currentReticleColorTime = kReticleColorTime;
                m_reticleRect.Modulate = m_reticleDamageColor;
            }
        }
        public void OnMissileMiss()
        {
            if (m_hitTimer.IsStopped() &&
                m_killTimer.IsStopped())
            {
                m_missMessage.Show();
                m_missTimer.Start();
            }
        }

        public void OnMissileHit()
        {
            if (m_killTimer.IsStopped())
            {
                m_hitMessage.Show();
                m_missMessage.Hide();
                m_hitTimer.Start();
            }
        }

        public void OnKill()
        {
            m_hitMessage.Hide();
            m_missMessage.Hide();
            m_killMessage.Show();
            m_killTimer.Start();
        }

        public void OnDamaged()
        {
            if (m_stallTimer.IsStopped())
            {
                m_damagedMessage.Show();
                m_damagedTimer.Start();
            }
        }
        public void OnStall()
        {
            m_damagedMessage.Hide();
            m_stallMessage.Show();
            m_stallTimer.Start();
        }
        public void OnBorderApproach(string message)
        {
            m_offCourseLabel.Text = m_baseOffCourseText + message;
            m_offCourseMessage.Show();
            m_offCourseTimer.Start();
        }

        public void OnEnemyWaveSpawned()
        {
            m_enemyWaveMessage.Show();
            m_enemyWaveTimer.Start();
        }

        public void OnPlayerDeath()
        {
            if (m_dead)
                return;

            m_speedLabel.Hide();
            m_altitudeLabel.Hide();
            m_missileContainer.Hide();
            foreach (ColorRect missileIndicator in m_missileIndicators)
                missileIndicator.Hide();
            m_targetRect.Hide();
            foreach (TextureRect rect in m_textureRects)
                rect.Hide();
            m_healthLabel.Hide();
            m_healthBar.Hide();
            m_hullLabel.Hide();
            m_missileTypeLabel.Hide();
            m_boreSight.Hide();
            m_velocityMarker.Hide();
            m_targetTrackerPivot.Hide();
            m_scoreLabel.Hide();
            m_missileAmmoLabel.Hide();

            m_controlsMainLabel.Hide();
            m_controlsLabel.Hide();
            m_simpleControlsLabel.Hide();

            m_hitMessage.Hide();
            m_killMessage.Hide();
            m_damagedMessage.Hide();
            m_missileMessage.Hide();
            m_missMessage.Hide();
            m_stallMessage.Hide();
            m_offCourseMessage.Hide();
            m_pullUpMessage.Hide();
            m_enemyWaveMessage.Hide();

            Input.MouseMode = Input.MouseModeEnum.Visible;

            m_mainMenuButton.Show();
            m_restartButton.Show();

            if (GlobalSettings.sSpawnEnemies)
            {
                m_totalScoreLabel.Show();
                m_totalTimeLabel.Show();
                SetEndTotalsLabels();
            }

            m_viewport.MouseFilter = MouseFilterEnum.Ignore;
            m_dead = true;
        }

        private void MainMenuButtonPressed()
        {
            GetTree().ChangeSceneToFile(m_mainMenuScenePath);
        }

        private void RestartButtonPressed()
        {
            if (GetTree().Paused)
                m_playerController.UnpauseGame();
            GetTree().ReloadCurrentScene();
        }

        private void ResumeButtonPressed()
        {
            if (GetTree().Paused)            
                m_playerController.UnpauseGame();            
        }

        private void QuitButtonPressed()
        {
            GetTree().Quit();
        }

        private void SetEndTotalsLabels()
        {
            bool isHighScore = PlayerScores.AddScore(m_playerController.m_playerScore);
            if (isHighScore)            
                m_totalScoreLabel.Text = "New Highscore: " + m_playerController.m_playerScore;
            else            
                m_totalScoreLabel.Text = "Score: " + m_playerController.m_playerScore;

            TimeSpan time = m_playerController.m_timeElapsed;

            isHighScore = PlayerScores.AddTime(time);
            if (isHighScore)
            {
                m_totalTimeLabel.Text = "New Longest Time: " + time.Minutes.ToString() +
                ':' + time.Seconds.ToString();
            }
            else
            {
                m_totalTimeLabel.Text = "Time: " + time.Minutes.ToString() +
                ':' + time.Seconds.ToString();
            }

            // Save the new player scores.
            PlayerScores.Save();
        }

        public void ShowPauseMenu()
        {
            m_mainMenuButton.Show();
            m_quitButton.Show();
            m_restartButton.Show();
            m_resumeButton.Show();
            m_viewport.StretchShrink = (int)kPausedStretchShrink;
        }
        public void HidePauseMenu()
        {
            m_mainMenuButton.Hide();
            m_quitButton.Hide();
            m_restartButton.Hide();
            m_resumeButton.Hide();
            m_viewport.StretchShrink = (int)kDefaultStretchShrink;
        }
    }
}
