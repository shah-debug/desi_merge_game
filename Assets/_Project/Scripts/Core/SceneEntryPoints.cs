using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesiMerge.Core
{
    /// <summary>Finds entry points only within the given scene's hierarchy (no global object search).</summary>
    public static class SceneEntryPoints
    {
        private static readonly List<GameObject> RootBuffer = new List<GameObject>();
        private static readonly List<ISceneEntryPoint> EntryPointBuffer = new List<ISceneEntryPoint>();

        public static int InitializeAll(Scene scene, GameServices services)
        {
            var count = 0;
            scene.GetRootGameObjects(RootBuffer);
            foreach (var root in RootBuffer)
            {
                root.GetComponentsInChildren(true, EntryPointBuffer);
                foreach (var entryPoint in EntryPointBuffer)
                {
                    entryPoint.Initialize(services);
                    count++;
                }
            }

            RootBuffer.Clear();
            EntryPointBuffer.Clear();
            return count;
        }
    }
}
