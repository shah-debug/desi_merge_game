using System;

namespace DesiMerge.Core
{
    /// <summary>An intro shown while the first scene loads. The callback runs once, when the intro may end.</summary>
    public interface ISplashScreen
    {
        void Play(Action onIntroFinished);
    }
}
