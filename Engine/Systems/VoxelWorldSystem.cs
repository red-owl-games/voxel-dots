using Unity.Entities;
using Unity.Mathematics;

namespace RedOwl.Voxel.Engine
{
    public class VoxelEngineSystemGroup : ComponentSystemGroup {}
    
    [UpdateInGroup(typeof(VoxelEngineSystemGroup), OrderLast = true)]
    public class EndVoxelEngineCommandBufferSystem : EntityCommandBufferSystem {}
    
    [UpdateInGroup(typeof(VoxelEngineSystemGroup))]
    public class VoxelWorldSystem : SystemBase
    {
        
        private EndSimulationEntityCommandBufferSystem _ecbSystem;
        
        protected override void OnCreate()
        {
            _ecbSystem = ECSX.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {

        }
    }
}