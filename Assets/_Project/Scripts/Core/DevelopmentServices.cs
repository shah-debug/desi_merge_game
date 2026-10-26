using DesiMerge.Configuration;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Core
{
    /// <summary>
    /// Services for a scene played directly in the Editor (without Bootstrap). Saves stay in memory, so this never
    /// touches real player data. Audio sources are placed on the given host, which belongs to that scene and is
    /// destroyed with it, so no extra music player survives a scene change. Not used in release builds.
    /// </summary>
    public static class DevelopmentServices
    {
        public static GameServices Create(GameConfig config, GameObject audioHost)
        {
            var resolvedConfig = config != null ? config : ScriptableObject.CreateInstance<GameConfig>();
            return GameServicesFactory.Create(resolvedConfig, new InMemorySaveStore(), isDevelopmentBuild: true, audioHost);
        }
    }
}
