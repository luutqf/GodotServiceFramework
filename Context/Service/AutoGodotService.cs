using Godot;

namespace GodotServiceFramework.Context.Service;

public abstract partial class AutoGodotService : Node, IService
{
    public abstract void Init();

    public abstract void Destroy();
}