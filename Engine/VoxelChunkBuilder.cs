using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedOwl.Voxel.Engine
{
    public class VoxelChunkBuilder : IDisposable
    {
        public static Dictionary<int, MeshCollider> _table;
        
        //public Mesh Mesh => _mesh;
        public Material Material => _materials[0];

        // private Mesh _mesh;
        private Material[] _materials;
        private readonly List<Vector3> _vertices;
        private readonly List<List<int>> _triangles;
        private readonly List<Vector2> _uvs;
        
        private MeshCollider _meshCollider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            _table = new Dictionary<int, MeshCollider>();
        }

        public VoxelChunkBuilder(float3 point, Material[] materials)
        {
            // _mesh = new Mesh();
            _materials = materials;
            _vertices = new List<Vector3>(VoxelWorld.CHUNK_VOXEL_COUNT * 6 * 4);
            _triangles = new List<List<int>>();
            EnsureSubMesh(_materials.Length);
            _uvs = new List<Vector2>(VoxelWorld.CHUNK_VOXEL_COUNT * 6 * 4);

            EnsureCollider(point);
        }

        private void EnsureCollider(float3 point)
        {
            var index = VoxelWorld.ChunkIndexFromWorldPos((int3) math.floor(point));
            if (_table.TryGetValue(index, out var collider))
            {
                _meshCollider = collider;
                return;
            }
            var go = new GameObject($"Chunk({point})");
            go.transform.position = new Vector3(point.x, point.y, point.z);
            
            // _meshFilter = _chunkRenderer.AddComponent<MeshFilter>();
            // _meshRenderer = _chunkRenderer.AddComponent<MeshRenderer>();
            _meshCollider = go.AddComponent<MeshCollider>();

            _table.Add(index, _meshCollider);
        }

        private void EnsureSubMesh(int index)
        {
            var count = _triangles.Count;
            if (index > count)
            {
                int difference = index - count;
                for (int i = 0; i < difference; i++)
                {
                    _triangles.Add(new List<int>(VoxelWorld.CHUNK_VOXEL_COUNT * 6 * 6));
                }
            }
        }

        public void AddVoxelFace(int face, int3 offset, int subMesh = 0)
        {
            _vertices.Add((float3) (offset + VoxelWorld.VOXEL_VERTS[VoxelWorld.VOXEL_TRIS[face, 0]]));
            _vertices.Add((float3) (offset + VoxelWorld.VOXEL_VERTS[VoxelWorld.VOXEL_TRIS[face, 1]]));
            _vertices.Add((float3) (offset + VoxelWorld.VOXEL_VERTS[VoxelWorld.VOXEL_TRIS[face, 2]]));
            _vertices.Add((float3) (offset + VoxelWorld.VOXEL_VERTS[VoxelWorld.VOXEL_TRIS[face, 3]]));
            
            var vertexIndex = _vertices.Count;
            _triangles[subMesh].Add(vertexIndex - 4);
            _triangles[subMesh].Add(vertexIndex - 3);
            _triangles[subMesh].Add(vertexIndex - 2);

            _triangles[subMesh].Add(vertexIndex - 3);
            _triangles[subMesh].Add(vertexIndex - 1);
            _triangles[subMesh].Add(vertexIndex - 2);
            
            _uvs.Add(VoxelWorld.VOXEL_UVS[0]);
            _uvs.Add(VoxelWorld.VOXEL_UVS[1]);
            _uvs.Add(VoxelWorld.VOXEL_UVS[2]);
            _uvs.Add(VoxelWorld.VOXEL_UVS[3]);
        }

        public void AddVoxel(int3 offset)
        {
            for (int i = 0; i < 6; i++)
            {
                AddVoxelFace(i, offset);
            }
        }

        public void Clear()
        {
            //_mesh.Clear();
            _vertices.Clear();
            foreach (var t in _triangles) t.Clear();
            _uvs.Clear();
        }

        public Mesh Build()
        {
            var mesh = new Mesh {vertices = _vertices.ToArray(), uv = _uvs.ToArray()};
            for (int i = 0; i < _triangles.Count; i++)
            {
                mesh.SetTriangles(_triangles[i], i, false);
            }
            mesh.RecalculateNormals();
            _meshCollider.sharedMesh = mesh;
            return mesh;
        }

        public void Rebuild()
        {
            // Debug.Log($"Rebuild Mesh: {_vertices.Count} | {_triangles.Count} | {_uvs.Count}");
            // _mesh.Clear();
            //
            // _mesh.vertices = _vertices.ToArray();
            // for (int i = 0; i < _triangles.Count; i++)
            // {
            //     _mesh.SetTriangles(_triangles[i], i, false);
            // }
            // _mesh.uv = _uvs.ToArray();
            //
            // _mesh.RecalculateBounds();
            // _mesh.RecalculateNormals();
            // _mesh.RecalculateTangents();
            
            // _meshFilter.mesh = _mesh;
            // _meshRenderer.materials = _materials;
            // _meshCollider.sharedMesh = _mesh;
        }

        public static implicit operator Mesh(VoxelChunkBuilder builder) => builder.Build();


        #region IDisposeable
        private void ReleaseUnmanagedResources()
        {
            Clear();
            //Object.Destroy(_chunkRenderer);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~VoxelChunkBuilder()
        {
            ReleaseUnmanagedResources();
        }
        
        #endregion
    }
}