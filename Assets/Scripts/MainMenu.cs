using Godot;
using Godot.Collections;

namespace SFS
{
    public partial class MainMenu : Node3D
    {
        [ExportGroup("Background")]
        [Export] private Plane m_plane;
        [Export] private Node3D m_cameraRotator;
        [Export] private float m_cameraRotationSpeed;

        [ExportGroup("Main Main Menu Nodes")]
        [Export] private BaseButton m_startButton;
        [Export] private BaseButton m_freeFlightButton;
        [Export] private BaseButton m_helpButton;
        [Export] private BaseButton m_scoresButton;
        [Export] private BaseButton m_creditsButton;
        [Export] private BaseButton m_quitButton;
        [Export] private PackedScene m_gameScene;
        [Export] private Array<Control> kMainMainMenuNodes = [];

        [ExportGroup("Scores Menu Nodes")]
        [Export] private Array<Control> kScoresMenuNodes = [];
        [Export] private HBoxContainer m_scoresContainer;
        [Export] private BaseButton m_returnButton;

        [ExportGroup("Help Menu Nodes")]
        [Export] private Array<Control> kHelpMenuNodes = [];
        [Export] private CheckButton m_simpleControlsButton;
        [Export] private RichTextLabel m_defaultControlsLabel;
        [Export] private RichTextLabel m_simpleControlsLabel;
        [Export] private VBoxContainer m_enemyTutorialContainer;
        [Export] private RichTextLabel m_freeFlightLabel;
        [Export] private Button m_continueButton;

        [ExportGroup("Credits Menu Nodes")]
        [Export] private Array<Control> kCreditsMenuNodes = [];

        private bool m_firstTick = false;

        public override void _Ready()
        {
            foreach (Control element in kScoresMenuNodes)
                element.Hide();
            foreach (Control element in kHelpMenuNodes)
                element.Hide();
            foreach (Control element in kCreditsMenuNodes)
                element.Hide();
            foreach (Control element in kMainMainMenuNodes)
                element.Show();

            m_startButton.Pressed += StartButtonPressed;
            m_freeFlightButton.Pressed += FreeFlightButtonPressed;
            m_quitButton.Pressed += QuitButtonPressed;
            m_scoresButton.Pressed += ScoresButtonPressed;
            m_returnButton.Pressed += ReturnButtonPressed;
            m_creditsButton.Pressed += CreditsButtonPressed;
            m_helpButton.Pressed += HelpButtonPressed;
            m_simpleControlsButton.Pressed += SimpleControlsPressed;
            m_continueButton.Pressed += ControlsContinueButtonPressed;

            GetTree().Paused = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        private void SimpleControlsPressed()
        {
            if (m_simpleControlsButton.ButtonPressed)
            {
                m_defaultControlsLabel.Hide();
                m_simpleControlsLabel.Show();
                GlobalSettings.sUseSimpleControls = true;
            }
            else
            {
                m_defaultControlsLabel.Show();
                m_simpleControlsLabel.Hide();
                GlobalSettings.sUseSimpleControls = false;
            }
        }

        private void QuitButtonPressed()
        {
            GetTree().Quit();
        }

        private void StartButtonPressed()
        {
            foreach (Control element in kMainMainMenuNodes)
                element.Hide();
            foreach (Control element in kHelpMenuNodes)
                element.Show();
            GlobalSettings.sSpawnEnemies = true;

            m_enemyTutorialContainer.Show();
            m_freeFlightLabel.Hide();

            if (GlobalSettings.sUseSimpleControls)
            {
                m_defaultControlsLabel.Hide();
                m_simpleControlsLabel.Show();
                m_simpleControlsButton.ButtonPressed = true;
            }
            else
            {
                m_defaultControlsLabel.Show();
                m_simpleControlsLabel.Hide();
                m_simpleControlsButton.ButtonPressed = false;
            }
        }

        private void FreeFlightButtonPressed()
        {
            foreach (Control element in kMainMainMenuNodes)
                element.Hide();
            foreach (Control element in kHelpMenuNodes)
                element.Show();

            GlobalSettings.sSpawnEnemies = false;
            m_enemyTutorialContainer.Hide();
            m_freeFlightLabel.Show();
                        
            if (GlobalSettings.sUseSimpleControls)
            {
                m_defaultControlsLabel.Hide();
                m_simpleControlsLabel.Show();
                m_simpleControlsButton.ButtonPressed = true;
            }
            else
            {
                m_defaultControlsLabel.Show();
                m_simpleControlsLabel.Hide();
                m_simpleControlsButton.ButtonPressed = false;
            }
        }

        private void ScoresButtonPressed()
        {
            foreach (Control element in kMainMainMenuNodes)
                element.Hide();
            foreach (Control element in kScoresMenuNodes)
                element.Show();

            PlayerScores.Load();
            var scoresContainer = m_scoresContainer.GetChild<VBoxContainer>(1);
            var scores = PlayerScores.GetScores();
            for (int i = 0; i < scores.Length; i++)
            {
                scoresContainer.GetChild<Label>(i + 2).Text =
                    scores[i].ToString();
            }

            var timesContainer = m_scoresContainer.GetChild<VBoxContainer>(3);
            var times = PlayerScores.GetTimes();
            for (int i = 0; i < scores.Length; i++)
            {
                timesContainer.GetChild<Label>(i + 2).Text =
                    times[i].Hours.ToString() + ':' +
                    times[i].Minutes.ToString() + ':' +
                    times[i].Seconds.ToString();
            }
        }

        private void ControlsContinueButtonPressed()
        {
            GetTree().ChangeSceneToPacked(m_gameScene);
        }

        private void HelpButtonPressed()
        {
            foreach (Control element in kMainMainMenuNodes)
                element.Hide();
            foreach (Control element in kHelpMenuNodes)
                element.Show();

            if (GlobalSettings.sUseSimpleControls)
            {
                m_defaultControlsLabel.Hide();
                m_simpleControlsLabel.Show();
            }
            else
            {
                m_defaultControlsLabel.Show();
                m_simpleControlsLabel.Hide();
            }
        }

        private void CreditsButtonPressed()
        {
            foreach (Control element in kMainMainMenuNodes)
                element.Hide();
            foreach (Control element in kCreditsMenuNodes)
                element.Show();
        }

        private void ReturnButtonPressed()
        {
            foreach (Control element in kScoresMenuNodes)
                element.Hide();
            foreach (Control element in kHelpMenuNodes)
                element.Hide();
            foreach (Control element in kCreditsMenuNodes)
                element.Hide();
            foreach (Control element in kMainMainMenuNodes)
                element.Show();
        }

        public override void _Process(double delta)
        {
            if (!m_firstTick)
            {
                SceneManagement.IsLoading = false;
                if (!GlobalSettings.sMusicAudioStreamSpawned)
                {
                    AudioStreamPlayer audioStream = GetChild<AudioStreamPlayer>(0);
                    audioStream.Reparent(GetTree().Root);
                    audioStream.Play();
                    GlobalSettings.sMusicAudioStreamSpawned = true;                    
                }
            }

            m_plane.m_controlInput = new(0, 0, 0);
            m_plane.m_throttleInput = 1;
            m_cameraRotator.RotateY(Mathf.DegToRad(m_cameraRotationSpeed) * (float)delta);
        }
    }
}
