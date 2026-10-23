
namespace ZCraft.Source.World
{
    public interface ITickable
    {
        void Tick(double delta);
        void PhysicsTick(double delta);

    }
}
