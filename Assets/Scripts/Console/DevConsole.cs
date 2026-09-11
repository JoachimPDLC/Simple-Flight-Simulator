using Godot;
using SFS;
using System.Collections.Generic;


// Game-independent dev console
[GlobalClass]
public partial class DevConsole : CanvasLayer
{
    [Export] private MarginContainer m_container;
    [Export] private RichTextLabel m_label;
    [Export] private LineEdit m_lineEdit;
    [Export] private DevConsoleCommands m_commands;

    private List<string> m_history = new();
    private int m_historyIndex = kStartingHistoryIndex;
    private const int kStartingHistoryIndex = -1;

    private Expression m_expression = new();
    Input.MouseModeEnum m_preConsoleMouseModeEnum;

    private bool m_wasPausedBeforeConsoleOpened = false;

    public override void _Ready()
    {
        Debug.Assert(m_label != null, "RichTextLabel for dev console is null!");
        Debug.Assert(m_lineEdit != null, "LineEdit for dev console is null!");
        Debug.Assert(m_container != null, "MarginContainer for dev console is null!");
        m_lineEdit.TextSubmitted += OnSubmission;
        m_lineEdit.FocusExited += OnFocusExited;
        ProcessMode = ProcessModeEnum.Always;

        m_container.Hide();
    }

    // I'm unsure if I want the game to handle turning off and on the
    // dev-console or for the dev-console itself
    // to handle input separate from the game.
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("DevConsole"))
        {
            if (m_container.Visible)
                OnConsoleExited();
            else
                OnConsoleEntered();
        }

        else if(m_container.Visible && m_lineEdit.HasFocus())
        {
            if (m_history.Count == 0)
                return;

            if (@event.IsActionPressed("ui_up"))
            {
                if (m_historyIndex < m_history.Count)
                {
                    ++m_historyIndex;
                    HistoryUpdated();
                }
            }
            else if (@event.IsActionPressed("ui_down"))
            {
                if (m_historyIndex > -1)
                {
                    --m_historyIndex;
                    HistoryUpdated();
                }
            }
        }         
    }

    private void HistoryUpdated()
    {
        if (m_historyIndex <= -1)
        {
            m_historyIndex = -1;
            m_lineEdit.Text = "";
            return;
        }

        if (m_historyIndex >= m_history.Count)
        {
            Debug.PrintWarning("Dev console history index is higher" +
                " or equal to history count!");
            m_historyIndex = m_history.Count - 1;
        }

        m_lineEdit.GrabFocus();
        m_lineEdit.Text = m_history[m_historyIndex];
    }

    private void OnConsoleEntered()
    {
        m_container.Show();
        m_preConsoleMouseModeEnum = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;        
        m_lineEdit.Text = "";
    }

    private void OnConsoleExited()
    { 
        m_container.Hide();
        Input.MouseMode = m_preConsoleMouseModeEnum;
        m_historyIndex = kStartingHistoryIndex;
    }

    private void OnSubmission(string submission)
    {
        m_history.Insert(0, submission);
        m_historyIndex = kStartingHistoryIndex;

        m_lineEdit.Text = "";
        Error error = m_expression.Parse(submission);
        if (error != Error.Ok)
        {
            m_label.AddText(m_expression.GetErrorText() + '\n');
            return;
        }
        // The expression is executed with the devCommands node 
        // as the base instance, since this devConsole class
        // is not tightly tied to the game while the 
        // devCommands class is. I also don't want to give
        // access to this class since users can easily break the 
        // devConsole.
        Variant result = m_expression.Execute([], m_commands);
        if (m_expression.HasExecuteFailed())
        {
            m_label.AddText(m_expression.GetErrorText() + '\n');
            return;
        }
        // No errors, so print result
        m_label.AddText(result.ToString() + '\n');
    }

    private void OnFocusExited()
    {
        m_historyIndex = kStartingHistoryIndex;
    }
}
