using System;

namespace ZCraft.Source.World.Interaction;

public interface IInteractable
{
    void OnInteract(InteractionContext context);
}
