using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RedOwl.Voxel.Engine
{
    [UpdateInGroup(typeof(VoxelEngineSystemGroup))]
    public class ChunkVisibleSystem : SystemBase
    {
        private EndVoxelEngineCommandBufferSystem _ecbSystem;

        private EntityQuery _targets;

        protected override void OnCreate()
        {
            _targets = GetEntityQuery(typeof(LocalToWorld), typeof(VoxelVisabilityTarget));
            _ecbSystem = ECSX.World.GetOrCreateSystem<EndVoxelEngineCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var distanceThreshold = VoxelWorld.VIEW_DISTANCE_XZ;
            var chunks = GetComponentDataFromEntity<LocalToWorld>();
            var targets = _targets.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
            var ecb = _ecbSystem.CreateCommandBuffer();
            Entities.WithNone<Disabled>().WithAll<VoxelChunk>().ForEach((Entity entity) =>
            {
                bool found = false;
                foreach (var target in targets)
                {
                    if (!chunks[entity].IsWithinDistance(target, distanceThreshold)) continue;
                    found = true;
                    break;
                }
                if (!found) ecb.AddComponent<Disabled>(entity);
            }).Run();
            Entities.WithAll<Disabled, VoxelChunk>().WithEntityQueryOptions(EntityQueryOptions.IncludeDisabled).ForEach((Entity entity) =>
            {
                bool found = false;
                foreach (var target in targets)
                {
                    if (!chunks[entity].IsWithinDistance(target, distanceThreshold)) continue;
                    found = true;
                    break;
                }
                if (found) ecb.RemoveComponent<Disabled>(entity);
            }).Run();
            _ecbSystem.AddJobHandleForProducer(Dependency);
            targets.Dispose(Dependency);
        }
    }
}
