using Unity.Entities;

namespace RedOwl.Voxel.Engine
{
    [InternalBufferCapacity(4096)]
    public struct Voxel : IBufferElementData
    {
        public ushort Id;

        // public static implicit operator ushort(Voxel self) => self.Id;
        //
        // public static implicit operator Voxel(ushort id) => new Voxel {Id = id};
    }

    public static class VoxelExtensions
    {
        public static bool IsBottomSolid(this DynamicBuffer<Voxel> buffer)
        {
            foreach (int i in VoxelWorld.CHUNK_BOTTOM_VOXEL_INDEXES)
            {
                if (buffer[i].Id == 0) return false;
            }

            return true;
        }
    }
}