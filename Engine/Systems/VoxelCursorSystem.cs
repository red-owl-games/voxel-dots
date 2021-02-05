using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace RedOwl.Voxel.Engine
{
    [UpdateInGroup(typeof(VoxelEngineSystemGroup))]
    public class VoxelCursorSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // TODO: this is a really interesting way to perform this with jobified code
            var check = VoxelWorld.WorldTest();
            Entities.ForEach((VoxelCursor cursor, ref Translation translation) =>
            {
                var targets = GetComponentDataFromEntity<LocalToWorld>(true);
                if (!targets.HasComponent(cursor.Target)) return;
                var target = targets[cursor.Target];
                var position = target.Position;
                var targetPosition = math.floor(position + math.round(target.Forward) * 1.23f + cursor.Offset);
                if (!check.IsOutsideWorld(targetPosition)) translation.Value =  targetPosition;
            }).ScheduleParallel();
        }
    }
}