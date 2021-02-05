using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RedOwl.Voxel.Engine
{
    public struct AddVoxelEvent : IComponentData
    {
        public Entity Chunk;
        public int VoxelIndex;

        public static AddVoxelEvent FromWorldPosition(float3 position)
        {
            var point = (int3) math.floor(position);
            return new AddVoxelEvent
            {
                Chunk = VoxelWorld.Engine.Chunks[VoxelWorld.ChunkIndexFromWorldPos(point)],
                VoxelIndex = VoxelWorld.VoxelIndexFromWorldPos(point)
            };
        }
    }
    
    public struct RemoveVoxelEvent : IComponentData
    {
        public Entity Chunk;
        public int VoxelIndex;

        public static RemoveVoxelEvent FromWorldPosition(float3 position)
        {
            var point = (int3) math.floor(position);
            return new RemoveVoxelEvent
            {
                Chunk = VoxelWorld.Engine.Chunks[VoxelWorld.ChunkIndexFromWorldPos(point)],
                VoxelIndex = VoxelWorld.VoxelIndexFromWorldPos(point)
            };
        }
    }
    
    [UpdateInGroup(typeof(VoxelEngineSystemGroup)), UpdateAfter(typeof(VoxelWorldSystem))]
    public class VoxelInteractionSystem : SystemBase
    {
        private EndVoxelEngineCommandBufferSystem _ecbSystem;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = ECSX.World.GetOrCreateSystem<EndVoxelEngineCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();
            Entities.ForEach((Entity entity, AddVoxelEvent evt) =>
            {
                var chunk = GetComponentDataFromEntity<VoxelChunk>()[evt.Chunk];
                var voxels = GetBufferFromEntity<Voxel>()[evt.Chunk].Reinterpret<ushort>();
                voxels[evt.VoxelIndex] = 1;
                chunk.State = VoxelChunkStates.Dirty;
                ecb.SetComponent(evt.Chunk, chunk);
                ecb.DestroyEntity(entity);
            }).Run();
            Entities.ForEach((Entity entity, RemoveVoxelEvent evt) =>
            {
                var chunk = GetComponentDataFromEntity<VoxelChunk>()[evt.Chunk];
                var voxels = GetBufferFromEntity<Voxel>()[evt.Chunk].Reinterpret<ushort>();
                voxels[evt.VoxelIndex] = 0;
                chunk.State = VoxelChunkStates.Dirty;
                ecb.SetComponent(evt.Chunk, chunk);
                ecb.DestroyEntity(entity);
            }).Run();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}