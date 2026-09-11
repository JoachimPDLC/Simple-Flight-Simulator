using Godot;

public partial class SceneManagement : Node
{
    public static SceneManagement Instance { get; private set; }

    public static bool IsLoading = true;
    public override void _Ready()
    {
        Instance = this;
    }
}
