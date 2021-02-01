using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RedOwl.Voxel.Engine
{
    public abstract class VoxelEngineBase : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("Number of chunks in world on the X,Z axis")]
        public int WorldSizeXZ { get; set; } = 1;
        [field: SerializeField]
        [field: Tooltip("Number of chunks in world on the Y axis")]
        public int WorldSizeY { get; set; } = 1;
        [field: SerializeField]
        [field: Tooltip("Number of chunks viewable in world on the X,Z axis")]
        public int ViewDistanceXZ { get; set; } = 12;
        [field: SerializeField]
        [field: Tooltip("Number of chunks viewable in world on the X,Z axis")]
        public int ViewDistanceY { get; set; } = 8;
        [field: SerializeField]
        [field: Tooltip("Number of voxels in chunk on the X,Z axis")]
        public int ChunkSizeXZ { get; set; } = 2;
        [field: SerializeField]
        [field: Tooltip("Number of voxels in chunk on the Y axis")]
        public int ChunkSizeY { get; set; } = 2;
        
        [field: SerializeField]
        public Material[] VoxelMaterials { get; set; }
        
        public NativeArray<Entity> Chunks { get; protected set; }
        //public VoxelChunkBuilder[] ChunkBuilders { get; protected set; }
    }
}