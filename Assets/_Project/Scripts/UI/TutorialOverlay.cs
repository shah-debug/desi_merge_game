using System;
using DesiMerge.Audio;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// First-game tutorial: a few short pages over the board. Next steps through them, Skip or the last page ends it.
    /// While it is visible it blocks drops (it is one of the HUD's pointer blockers). It never pauses time; the board
    /// is empty at the start of a first game.
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private PanelTransition transition;
        [SerializeField] private GameObject[] pages = Array.Empty<GameObject>();
        [SerializeField] private Image[] dots = Array.Empty<Image>();
        [SerializeField] private Color dotOn = Color.white;
        [SerializeField] private Color dotOff = Color.white;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;
        [SerializeField] private Button skipButton;

        private bool _opening;

        public event Action Finished;

        public int Page { get; private set; }
        public int PageCount => pages.Length;
        public bool IsShowing => transition != null ? transition.IsVisible : gameObject.activeSelf;
        internal Button NextButton => nextButton;
        internal Button SkipButton => skipButton;

        private void Awake()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(Next);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(Finish);
            }

            if (!_opening)
            {
                SetVisible(false, immediate: true);
            }
        }

        private void OnDestroy()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(Next);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(Finish);
            }
        }

        public void BindAudio(IAudioService audio)
        {
            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }
        }

        public void Show()
        {
            _opening = true;
            ShowPage(0);
            SetVisible(true, immediate: false);
        }

        public void Next()
        {
            if (Page + 1 >= pages.Length)
            {
                Finish();
                return;
            }

            ShowPage(Page + 1);
        }

        public void Finish()
        {
            if (!IsShowing)
            {
                return;
            }

            SetVisible(false, immediate: false);
            Finished?.Invoke();
        }

        private void ShowPage(int index)
        {
            Page = Mathf.Clamp(index, 0, Mathf.Max(0, pages.Length - 1));
            for (var i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                {
                    pages[i].SetActive(i == Page);
                }
            }

            for (var i = 0; i < dots.Length; i++)
            {
                if (dots[i] != null)
                {
                    dots[i].color = i == Page ? dotOn : dotOff;
                }
            }

            if (nextLabel != null)
            {
                nextLabel.text = Page + 1 >= pages.Length ? "LET'S PLAY" : "NEXT";
            }
        }

        private void SetVisible(bool visible, bool immediate)
        {
            if (transition == null)
            {
                gameObject.SetActive(visible);
            }
            else if (immediate)
            {
                transition.SetVisibleImmediate(visible);
            }
            else if (visible)
            {
                transition.Show();
            }
            else
            {
                transition.Hide();
            }
        }
    }
}
