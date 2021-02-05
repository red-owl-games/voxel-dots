using Unity.Entities;
using Unity.Mathematics;

namespace RedOwl.Voxel.Engine
{
    [GenerateAuthoringComponent]
    public struct VoxelCursor : IComponentData
    {
        public Entity Target;

        public float3 Offset;
    }
}