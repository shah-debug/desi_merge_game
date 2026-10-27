using System;

namespace DesiMerge.Platform
{
    /// <summary>
    /// Application lifecycle signals. On Android, <see cref="Paused"/> is the last reliable
    /// moment to save: the OS may kill a backgrounded app without calling quit.
    /// </summary>
    public interface IAppLifecycle
    {
        event Action Paused;
        event Action Resumed;
        event Action Quitting;

        /// <summary>The app lost focus (e.g. notification shade, system dialog). Often precedes a pause.</summary>
        event Action FocusLost;
    }
}
