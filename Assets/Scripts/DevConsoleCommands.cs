using Godot;

// Class that hold all commands used in the dev console.
// While the DevConsole should be game-independent,
// this class is heavily tied to the game itself.
// It inherits from Node since it will probably want access
// to the node tree.
[GlobalClass]
public partial class DevConsoleCommands : Node
{
    private void Reload()
    {        
        GetTree().ReloadCurrentScene();
    }

    private void Quit()
    {
        GetTree().Quit();
    }

    private void Hello()
    {
        GD.Print("Hello!");
    }

    private void Hi()
    {
        GD.Print("Hi!");
    }

    private void Goodbye()
    {
        GD.Print("Goodbye!");
    }

    private void Pause()
    {
        GetTree().Paused = true;
    }

    private void Unpause()
    {
        GetTree().Paused = false;
    }
}
