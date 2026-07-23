using Godot;

public static class NodeExtensions
{

    public static void DisableAllProcess(this Node node)
    {
        node.SetProcess(false);
        node.SetPhysicsProcess(false);
        node.SetProcessInput(false);
        node.SetProcessUnhandledInput(false);
        node.SetProcessShortcutInput(false);
    }

    public static void DisableTickProcess(this Node node)
    {
        node.SetProcess(false);
        node.SetPhysicsProcess(false);
    }

    public static void DisableAllInput(this Node node)
    {
        node.SetProcessInput(false);
        node.SetProcessUnhandledInput(false);
        node.SetProcessShortcutInput(false);
    }

    public static void EnableAllProcess(this Node node)
    {
        node.SetProcess(true);
        node.SetPhysicsProcess(true);
        node.SetProcessInput(true);
        node.SetProcessUnhandledInput(true);
        node.SetProcessShortcutInput(true);
    }

    public static void MakeSleeping(this Node node)
    {
        node.DisableAllProcess();
    }

    public static bool IsSleeping(this Node node)
    {
        return !node.IsProcessing() &&
               !node.IsPhysicsProcessing() &&
               !node.IsProcessingInput() &&
               !node.IsProcessingUnhandledInput() &&
               !node.IsProcessingShortcutInput();
    }
}