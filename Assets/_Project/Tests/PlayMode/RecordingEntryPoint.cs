using DesiMerge.Core;
using UnityEngine;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>Test double that records the services it was initialized with.</summary>
    public sealed class RecordingEntryPoint : MonoBehaviour, ISceneEntryPoint
    {
        public GameServices Received { get; private set; }

        public void Initialize(GameServices services) => Received = services;
    }
}
