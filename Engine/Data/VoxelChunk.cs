using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RedOwl.Voxel.Engine
{
    // [Flags]
    // public enum VoxelChunkStates : ushort
    // {
    //     Created = 0,
    //     Loaded = 1,
    //     Dirty = 2,
    //     MeshReady = 3,
    //     MarkedForDelete = 4
    // }
    // Created -> Loaded -> Dirty -> MeshReady -> MarkedForDelete
    
    public struct VoxelChunk : IComponentData
    {
        public int Id;
        // public ushort StateId;
        //
        // public VoxelChunkStates State
        // {
        //     get => (VoxelChunkStates) StateId;
        //     set => StateId = (ushort)value;
        // }

        public Entity backNeighbor;
        public Entity frontNeighbor;
        public Entity topNeighbor;
        public Entity bottomNeighbor;
        public Entity leftNeighbor;
        public Entity rightNeighbor;
    }

    public struct VoxelChunkDirtyTag : IComponentData
    {
        
    }

    public struct VoxelChunkMeshReadyTag : IComponentData
    {
        
    }
}