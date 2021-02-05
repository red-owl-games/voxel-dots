using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace RedOwl.Voxel.Engine
{
    public class ECSConverter : IDisposable
    {
        private EntityManager _manager;

        private Entity _entityPrefab;
        
        public ECSConverter(EntityManager dstManager, GameObjectConversionSystem conversionSystem, GameObject prefab)
        {
            _manager = dstManager;
            _entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, GameObjectConversionSettings.FromWorld(dstManager.World, conversionSystem.BlobAssetStore));
        }


        public void Dispose()
        {
            _manager.DestroyEntity(_entityPrefab);
        }

        public Entity New()
        {
            return _manager.Instantiate(_entityPrefab);
        }
    }
    
    public static class ECSX
    {
        public static World World => World.DefaultGameObjectInjectionWorld;
        public static EntityManager Manager => World.EntityManager;

        public static TSystem EnsureSystem<TSystem, TGroup>() where TSystem : ComponentSystemBase where TGroup : ComponentSystemGroup
        {
            var system = World.GetOrCreateSystem<TSystem>();
            World.GetExistingSystem<TGroup>().AddSystemToUpdateList(system);
            return system;
        }

        public static TSystem EnsureSystem<TSystem>() where TSystem : ComponentSystemBase
        {
            var system = World.GetOrCreateSystem<TSystem>();
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World, typeof(TSystem));
            return system;
        }

        public static bool IsWithinDistance(this LocalToWorld self, LocalToWorld other, int threshold)
        {
            var heading = self.Position - other.Position;
            return heading.x * heading.x + heading.z * heading.z < threshold * threshold;
        }

        public static bool IsWithinDistance(this Translation self, Translation other, int threshold)
        {
            var heading = self.Value - other.Value;
            return math.sqrt(heading.x * heading.x + heading.z * heading.z) < threshold;
        }
    }
}