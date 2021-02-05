using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace RedOwl.Voxel.Engine
{
    [UpdateInGroup(typeof(VoxelEngineSystemGroup)), UpdateAfter(typeof(VoxelWorldSystem))]
    public class ChunkMeshSystem : SystemBase
    {
        private EndVoxelEngineCommandBufferSystem _ecbSystem;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = ECSX.World.GetOrCreateSystem<EndVoxelEngineCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (!VoxelWorld.IsInitialized) return;
            var material = VoxelWorld.Engine.VoxelMaterials;
            var ecb = _ecbSystem.CreateCommandBuffer();
            Entities.WithAll<VoxelChunk>().WithoutBurst().ForEach((Entity entity, in Translation translation) =>
            {
                
                var chunks = GetComponentDataFromEntity<VoxelChunk>(true);
                var chunk = chunks[entity];
                if (chunk.State != VoxelChunkStates.Dirty) return;
                Debug.Log($"Chunk '{chunk}' IsDirty");
                var voxels = GetBufferFromEntity<Voxel>(true);

                var chunkPoint = (int3) translation.Value;
                var chunkVoxels = voxels[entity];

                var hasBackNeighbor = chunk.backNeighbor != Entity.Null;
                var backNeighborVoxels = voxels[hasBackNeighbor ? chunk.backNeighbor : entity];
                var hasFrontNeighbor = chunk.frontNeighbor != Entity.Null;
                var frontNeighborVoxels = voxels[hasFrontNeighbor ? chunk.frontNeighbor : entity];
                var hasTopNeighbor = chunk.topNeighbor != Entity.Null;
                var topNeighborVoxels = voxels[hasTopNeighbor ? chunk.topNeighbor : entity];
                var hasBottomNeighbor = chunk.bottomNeighbor != Entity.Null;
                var bottomNeighborVoxels = voxels[hasBottomNeighbor ? chunk.bottomNeighbor : entity];
                var hasLeftNeighbor = chunk.leftNeighbor != Entity.Null;
                var leftNeighborVoxels = voxels[hasLeftNeighbor ? chunk.leftNeighbor : entity];
                var hasRightNeighbor = chunk.rightNeighbor != Entity.Null;
                var rightNeighborVoxels = voxels[hasRightNeighbor ? chunk.rightNeighbor : entity];

                var builder = new VoxelChunkBuilder(translation.Value, material);
                //var builder = VoxelWorld.Engine.ChunkBuilders[chunk.Id];
                //builder.Clear();
                for (int i = 0; i < chunkVoxels.Length; i++)
                {
                    if (chunkVoxels[i].Id == 0) continue; // Skip Air
                    var voxelPoint = VoxelWorld.CHUNK_VOXEL_POSITIONS[i];
                    var voxelWorldPoint = chunkPoint + voxelPoint;
                    int j = -1;
                    foreach (var neighborPoint in VoxelWorld.VOXEL_NEIGHBOR_OFFSETS)
                    {
                        j++;
                        var neighborVoxelWorldPoint = voxelWorldPoint + neighborPoint;
                        if (VoxelWorld.IsOutsideWorld(neighborVoxelWorldPoint))
                        {
                            if (j == 3) continue; //out the bottom of the world we don't need to draw this face
                            builder.AddVoxelFace(j, voxelPoint);
                            continue;
                        }
                        var neighborVoxelPoint = voxelPoint + neighborPoint;
                        if (!VoxelWorld.IsOutsideChunk(neighborVoxelPoint))
                        {
                            if (chunkVoxels[VoxelWorld.VoxelIndexFromVoxelPos(neighborVoxelPoint)].Id < 1)
                            {
                                builder.AddVoxelFace(j, voxelPoint);
                            }
                            continue;
                        }

                        switch (j)
                        {
                            case 0 when hasBackNeighbor:
                                if (backNeighborVoxels[VoxelWorld.VoxelIndexFromWorldPos(neighborVoxelWorldPoint)].Id < 1)
                                    builder.AddVoxelFace(j, voxelPoint);
                                break;
                            case 1 when hasFrontNeighbor:
                                if (frontNeighborVoxels[VoxelWorld.VoxelIndexFromWorldPos(neighborVoxelWorldPoint)].Id < 1)
                                    builder.AddVoxelFace(j, voxelPoint);
                                break;
                            case 2 when hasTopNeighbor:
                                if (topNeighborVoxels[VoxelWorld.VoxelIndexFromWorldPos(neighborVoxelWorldPoint)].Id < 1)
                                    builder.AddVoxelFace(j, voxelPoint);
                                break;
                            case 3 when hasBottomNeighbor:
                                if (bottomNeighborVoxels[VoxelWorld.VoxelIndexFromWorldPos(neighborVoxelWorldPoint)].Id < 1)
                                    builder.AddVoxelFace(j, voxelPoint);
                                break;
                            case 4 when hasLeftNeighbor:
                                if (leftNeighborVoxels[VoxelWorld.VoxelIndexFromWorldPos(neighborVoxelWorldPoint)].Id < 1)
                                    builder.AddVoxelFace(j, voxelPoint);
                                break;
                            case 5 when hasRightNeighbor:
                                if (rightNeighborVoxels[VoxelWorld.VoxelIndexFromWorldPos(neighborVoxelWorldPoint)].Id < 1)
                                    builder.AddVoxelFace(j, voxelPoint);
                                break;
                        }
                    }
                }
                var mesh = builder.Build();
                
                var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(entity);
                renderMesh.mesh = mesh;
                renderMesh.material = builder.Material;
                var renderBounds = EntityManager.GetComponentData<RenderBounds>(entity);
                var bounds = mesh.bounds;
                renderBounds.Value = new AABB { Center = bounds.center, Extents = bounds.extents};
                
                ecb.SetSharedComponent(entity, renderMesh);
                ecb.SetComponent(entity, renderBounds);

                chunk.State = VoxelChunkStates.MeshReady;
                ecb.SetComponent(entity, chunk);
            }).Run();
        }
    }
}
