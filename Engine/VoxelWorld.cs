using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedOwl.Voxel.Engine
{
	public interface IVoxelEngine
	{
		int WorldSizeXZ { get; }
		int WorldSizeY { get; }
		int ChunkSizeXZ { get; }
		int ChunkSizeY { get; }
		int ViewDistanceXZ { get; }
		int ViewDistanceY { get; }
        
		Material[] VoxelMaterials { get; }
		
		NativeArray<Entity> Chunks { get; }
		//VoxelChunkBuilder[] ChunkBuilders { get; }

		void Initialize();
		void LoadChunks(BinaryReader reader);
		void SaveChunks(BinaryWriter writer);
		
	}
	
    public static class VoxelWorld
    {
	    public static bool IsInitialized { get; private set; } = false;
	    public static IVoxelEngine Engine { get; private set; }

	    
	    #region API
	    
	    public static void Initialize<TEngine>(TEngine engine) where TEngine : ScriptableObject, IVoxelEngine
	    {
		    Debug.Log($"Initializing Voxel World");

		    Engine = Object.Instantiate(engine);
		    
		    CalculateWorldSettings();
		    CalculateChunkSettings();
		    CalculateViewSettings();

		    PopulateChunkPositionArray();
		    PopulateChunkNeighborOffsets();
		    PopulateVoxelPositionArray();
		    PopulateChunkEdgeIndexes();
		    PopulateChunkBottomIndexes();
		    
		    Engine.Initialize();
		    IsInitialized = true;
	    }
	    
	    private static string Filepath(string filename) => $"{Application.persistentDataPath}/{filename}.rvw";

	    public static bool Load(string relativePath)
	    {
		    string filepath = Filepath(relativePath);
		    if (!File.Exists(filepath)) return false;
		    using var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
		    using var deflateStream = new DeflateStream(fileStream, CompressionMode.Decompress);
		    using var reader = new BinaryReader(deflateStream);
		    try
		    {
			    Engine.LoadChunks(reader);
		    }
		    catch (Exception)
		    {
			    return false;
		    }
		    return true;
	    }

	    public static bool Save(string relativePath)
	    {
		    string filepath = Filepath(relativePath);
		    string backupPath = $"{Path.GetDirectoryName(filepath)}/{Path.GetFileName(filepath)}.bak";
		    Directory.CreateDirectory(Path.GetDirectoryName(filepath) ?? string.Empty);
		    if (File.Exists(filepath))
		    {
			    if (File.Exists(backupPath)) File.Delete(backupPath);
			    File.Move(filepath, backupPath);
		    }

		    using var fileStream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write);
		    using var deflateStream = new DeflateStream(fileStream, CompressionMode.Compress);
		    using var writer = new BinaryWriter(deflateStream);
		    try
		    {
			    Engine.SaveChunks(writer);
		    }
		    catch (Exception)
		    {
			    // If we fail then move "backup" back into place
			    File.Delete(filepath);
			    File.Move(backupPath, filepath);
			    return false;
		    }
		    return true;
	    }
	    
	    #endregion

	    #region Initialization

	    private static void CalculateWorldSettings()
	    {
		    WORLD_CHUNK_COUNT = Engine.WorldSizeXZ * Engine.WorldSizeXZ * Engine.WorldSizeY;
		    WORLD_SIZE_XZ = Engine.WorldSizeXZ;
		    WORLD_BOUNDS_XZ = WORLD_SIZE_XZ * Engine.ChunkSizeXZ;
		    WORLD_BOUNDS_XZ_MINUS_ONE = WORLD_BOUNDS_XZ - 1;
		    WORLD_SIZE_Y = Engine.WorldSizeY;
		    WORLD_BOUNDS_Y_MINUS_ONE = WORLD_SIZE_Y * Engine.ChunkSizeY - 1;
		    WORLD_SIZE_MAGIC_NUMBER = Engine.WorldSizeXZ * Engine.WorldSizeY;
	    }

	    private static void CalculateChunkSettings()
	    {
		    CHUNK_SIZE_XZ_SQUARED = Engine.ChunkSizeXZ * Engine.ChunkSizeXZ;
		    CHUNK_SIZE_XZ = Engine.ChunkSizeXZ;
		    CHUNK_SIZE_XZ_MINUS_ONE = math.clamp(CHUNK_SIZE_XZ - 1, 0, int.MaxValue);
		    CHUNK_SIZE_Y = Engine.ChunkSizeY;
		    CHUNK_SIZE_Y_MINUS_ONE = math.clamp(CHUNK_SIZE_Y - 1, 0, int.MaxValue);
		    CHUNK_SIZE_MAGIC_NUMBER = Engine.ChunkSizeXZ * Engine.ChunkSizeY;
		    CHUNK_VOXEL_COUNT = CHUNK_SIZE_XZ_SQUARED * Engine.ChunkSizeY;
	    }

	    private static void CalculateViewSettings()
	    {
		    VIEW_DISTANCE_XZ = Engine.ViewDistanceXZ * CHUNK_SIZE_XZ;
		    VIEW_DISTANCE_Y = Engine.ViewDistanceY * CHUNK_SIZE_Y;
	    }

	    private static void PopulateChunkPositionArray()
        {
	        CHUNK_WORLD_POSITIONS = new int3[WORLD_CHUNK_COUNT];
	        int i = 0;
	        for (int z = 0; z < WORLD_SIZE_XZ; z++)
	        {
		        for (int y = 0; y < WORLD_SIZE_Y; y++)
		        {
			        for (int x = 0; x < WORLD_SIZE_XZ; x++)
			        {
				        CHUNK_WORLD_POSITIONS[i] = new int3(x * CHUNK_SIZE_XZ, y * CHUNK_SIZE_Y, z * CHUNK_SIZE_XZ);
				        i++;
			        }
		        }
	        }
        }

	    private static void PopulateChunkNeighborOffsets()
	    {
		    CHUNK_NEIGHBOR_OFFSETS = new[]
		    {
			    new int3(0, 0, -1 * CHUNK_SIZE_XZ),
			    new int3(0, 0, 1 * CHUNK_SIZE_XZ),
			    new int3(0, 1 * CHUNK_SIZE_Y, 0),
			    new int3(0, -1 * CHUNK_SIZE_Y, 0),
			    new int3(-1 * CHUNK_SIZE_XZ, 0, 0),
			    new int3(1 * CHUNK_SIZE_XZ, 0, 0)
		    };
	    }

        private static void PopulateVoxelPositionArray()
        {
	        CHUNK_VOXEL_POSITIONS = new int3[CHUNK_VOXEL_COUNT];
	        int i = 0;
	        for (int z = 0; z < CHUNK_SIZE_XZ; z++)
	        {
		        for (int y = 0; y < CHUNK_SIZE_Y; y++)
		        {
			        for (int x = 0; x < CHUNK_SIZE_XZ; x++)
			        {
				        CHUNK_VOXEL_POSITIONS[i] = new int3(x, y, z);
				        i++;
			        }
		        }
	        }
        }
        
        private static void PopulateChunkEdgeIndexes()
        {
	        CHUNK_EDGE_VOXEL_INDEXES = new int[CHUNK_SIZE_XZ_SQUARED + CHUNK_SIZE_XZ_SQUARED + (CHUNK_SIZE_XZ * 4 - 4) * math.max(0, CHUNK_SIZE_XZ - 2)];
	        int i = 0;
	        int j = 0;
	        foreach (var point in CHUNK_VOXEL_POSITIONS)
	        {
		        i++;
		        if (!IsChunkEdge(point)) continue;
		        CHUNK_EDGE_VOXEL_INDEXES[j] = i;
		        j++;
	        }
        }

        private static void PopulateChunkBottomIndexes()
        {
	        CHUNK_BOTTOM_VOXEL_INDEXES = new int[CHUNK_SIZE_XZ_SQUARED];
	        int i = 0;
	        for (int z = 0; z < CHUNK_SIZE_XZ; z++)
	        {
		        for (int x = 0; x < CHUNK_SIZE_XZ; x++)
		        {
			        CHUNK_BOTTOM_VOXEL_INDEXES[i] = VoxelIndexFromVoxelPos(new int3(x, 0, z));
			        i++;
		        }
	        }
        }

        #endregion

        #region VoxelConstants

        public static int VIEW_DISTANCE_XZ;
        public static int VIEW_DISTANCE_Y;

        public static int WORLD_CHUNK_COUNT;
        public static int WORLD_SIZE_XZ;
        public static int WORLD_BOUNDS_XZ;
        public static int WORLD_BOUNDS_XZ_MINUS_ONE;
        public static int WORLD_SIZE_Y;
        public static int WORLD_BOUNDS_Y_MINUS_ONE;
        public static int WORLD_SIZE_MAGIC_NUMBER;

        public static int CHUNK_SIZE_XZ_SQUARED;
        public static int CHUNK_SIZE_XZ;
        public static int CHUNK_SIZE_XZ_MINUS_ONE;
        public static int CHUNK_SIZE_Y;
        public static int CHUNK_SIZE_Y_MINUS_ONE;
        public static int CHUNK_SIZE_MAGIC_NUMBER;
        public static int CHUNK_VOXEL_COUNT;

        public static int3[] CHUNK_WORLD_POSITIONS;
        public static int3[] CHUNK_NEIGHBOR_OFFSETS;
        public static int3[] CHUNK_VOXEL_POSITIONS;

        public static int[] CHUNK_EDGE_VOXEL_INDEXES;
        public static int[] CHUNK_BOTTOM_VOXEL_INDEXES;

        public static readonly int3[] VOXEL_VERTS = new int3[8]
        {
	        new int3(0, 0, 0),
        	new int3(1, 0, 0),
        	new int3(1, 1, 0),
        	new int3(0, 1, 0),
        	new int3(0, 0, 1),
        	new int3(1, 0, 1),
        	new int3(1, 1, 1),
        	new int3(0, 1, 1),
    
        };

        public static readonly int[,] VOXEL_TRIS = new int[6,4]
        {
	        {0, 3, 1, 2}, // Back
	        {5, 6, 4, 7}, // Front
	        {3, 7, 2, 6}, // Top
	        {1, 5, 0, 4}, // Bottom
	        {4, 7, 0, 3}, // Left
	        {1, 2, 5, 6}  // Right
    
        };
    
        public static readonly float2[] VOXEL_UVS = new float2[4]
        {
	        new float2(0.0f, 0.0f),
	        new float2(0.0f, 1.0f),
	        new float2(1.0f, 0.0f),
	        new float2(1.0f, 1.0f)
        };
        
        public static readonly int3[] VOXEL_NEIGHBOR_OFFSETS = new int3[6] {

	        new int3(0, 0, -1),
	        new int3(0, 0, 1),
	        new int3(0, 1, 0),
	        new int3(0, -1, 0),
	        new int3(-1, 0, 0),
	        new int3(1, 0, 0)
        };
        
        #endregion

        #region VoxelHelpers
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChunkIndexFromWorldPos(int3 position) => (position.z / CHUNK_SIZE_XZ) * WORLD_SIZE_MAGIC_NUMBER + (position.y / CHUNK_SIZE_Y) * WORLD_SIZE_XZ + (position.x / CHUNK_SIZE_XZ);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 VoxelPosFromWorldPos(int3 position) => new int3(position.x % CHUNK_SIZE_XZ, position.y % CHUNK_SIZE_Y, position.z % CHUNK_SIZE_XZ);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int VoxelIndexFromWorldPos(int3 position) => VoxelIndexFromVoxelPos(VoxelPosFromWorldPos(position));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int VoxelIndexFromVoxelPos(int3 position) => position.z * CHUNK_SIZE_MAGIC_NUMBER + position.y * CHUNK_SIZE_XZ + position.x;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 VoxelPosFromVoxelIndex(int index)
        {
            int z = index / CHUNK_SIZE_MAGIC_NUMBER;
            index -= (z * CHUNK_SIZE_MAGIC_NUMBER);
            int y = index / CHUNK_SIZE_XZ;
            int x = index % CHUNK_SIZE_XZ;
            return new int3(x, y, z);
        }
        
        public static IEnumerable<int3> VOXEL_NEIGHBORS(int3 point)
        {
	        foreach (var offset in VOXEL_NEIGHBOR_OFFSETS)
	        {
		        yield return point + offset;
	        }
        }
        
        public static IEnumerable<int3> CHUNK_NEIGHBORS(int3 point)
        {
	        foreach (var offset in CHUNK_NEIGHBOR_OFFSETS)
	        {
		        yield return point + offset;
	        }
        }

        public static IEnumerable<int> CHUNK_NEIGHBOR_INDEXES(int3 chunkPoint)
        {
	        foreach (var chunkNeighborPoint in CHUNK_NEIGHBORS(chunkPoint))
	        {
		        var value = IsOutsideWorld(chunkNeighborPoint) ? -1 : ChunkIndexFromWorldPos(chunkNeighborPoint);
		        yield return value;
	        }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChunkOrigin(int3 position) => position.x == 0 && position.y == 0 && position.z == 0;
        
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static bool IsChunkCenter(int3 position) => position.x == CHUNK_SIZE_RADIUS && position.y == CHUNK_SIZE_RADIUS && position.z == CHUNK_SIZE_RADIUS;

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static bool IsChunkCorner(int3 position) =>
	       //  (position.x == 0 || position.x == CHUNK_SIZE_XZ_MINUS_ONE) &&
	       //  (position.y == 0 || position.y == CHUNK_SIZE_Y_MINUS_ONE) &&
	       //  (position.z == 0 || position.z == CHUNK_SIZE_XZ_MINUS_ONE);

        public static bool IsChunkEdge(int3 position) =>
	        (position.x == 0 || position.x == CHUNK_SIZE_XZ_MINUS_ONE) ||
	        (position.y == 0 || position.y == CHUNK_SIZE_Y_MINUS_ONE) ||
	        (position.z == 0 || position.z == CHUNK_SIZE_XZ_MINUS_ONE);

        public static bool IsOutsideChunk(int3 position) =>
	        position.x < 0 || position.x > CHUNK_SIZE_XZ_MINUS_ONE ||
	        position.y < 0 || position.y > CHUNK_SIZE_Y_MINUS_ONE ||
	        position.z < 0 || position.z > CHUNK_SIZE_XZ_MINUS_ONE;
        
        public static bool IsOutsideWorld(int3 position) =>
	        position.x < 0 || position.x > WORLD_BOUNDS_XZ_MINUS_ONE ||
	        position.y < 0 || position.y > WORLD_BOUNDS_Y_MINUS_ONE ||
	        position.z < 0 || position.z > WORLD_BOUNDS_XZ_MINUS_ONE;

        #endregion
        
        #region UnityConversions
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Vector3ToInt3(Vector3 position) => new int3(math.floor(position));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Vector3IntToInt3(Vector3Int position) => new int3(position.x, position.y, position.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Int3ToVector3(int3 position) => new Vector3(position.x, position.y, position.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int Int3ToVector3Int(int3 position) => new Vector3Int(position.x, position.y, position.z);
        
        #endregion
    }
}
