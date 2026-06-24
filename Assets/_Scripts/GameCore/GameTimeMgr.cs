using System;
using DG.Tweening;
using GameCore.RefData;
using SCFrame;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 游戏内时钟管理：对话结束后瞬时推进，电梯运行期间平滑推进。
    /// </summary>
    public class GameTimeMgr : Singleton<GameTimeMgr>
    {
        private const int SecondsPerDay = 24 * 3600;

        private int _totalSeconds;
        private Tween _advanceTween;

        public event Action OnTimeChanged;

        public int TotalSeconds => _totalSeconds;

        public override void OnDiscard()
        {
            StopAdvanceTween();
            OnTimeChanged = null;
        }

        public void ResetToDayStart()
        {
            StopAdvanceTween();
            TimeEffectData dayStartTime = SCRefDataMgr.instance.globalConfigRefData?.dayStartTime;
            if (dayStartTime == null)
            {
                Debug.LogError("[GameTimeMgr] dayStartTime 配置为空，回退到 08:00。");
                _totalSeconds = 8 * 3600;
            }
            else
            {
                _totalSeconds = NormalizeSeconds(dayStartTime.TotalSeconds);
            }

            NotifyTimeChanged();
        }

        public string GetDisplayText()
        {
            int hour = _totalSeconds / 3600;
            int minute = (_totalSeconds % 3600) / 60;
            return $"{hour:D2}:{minute:D2}";
        }

        public void AddTimeInstant(TimeEffectData timeEffect)
        {
            if (timeEffect == null || timeEffect.TotalSeconds <= 0)
                return;

            _totalSeconds = NormalizeSeconds(_totalSeconds + timeEffect.TotalSeconds);
            NotifyTimeChanged();
        }

        public void PlayElevatorTravelAdvance(float realDurationSeconds)
        {
            TimeEffectData travelTime = SCRefDataMgr.instance.globalConfigRefData?.elevatorTravelTime;
            if (travelTime == null || travelTime.TotalSeconds <= 0)
                return;

            float duration = Mathf.Max(0.1f, realDurationSeconds);
            PlayAdvance(travelTime.TotalSeconds, duration);
        }

        public void StopAdvanceTween()
        {
            if (_advanceTween != null && _advanceTween.IsActive())
                _advanceTween.Kill();
            _advanceTween = null;
        }

        private void PlayAdvance(int addGameSeconds, float realDurationSeconds)
        {
            if (addGameSeconds <= 0)
                return;

            StopAdvanceTween();

            int startSeconds = _totalSeconds;
            int targetSeconds = NormalizeSeconds(startSeconds + addGameSeconds);
            _advanceTween = DOTween.To(
                    () => (float)startSeconds,
                    value =>
                    {
                        _totalSeconds = NormalizeSeconds(Mathf.RoundToInt(value));
                        NotifyTimeChanged();
                    },
                    targetSeconds,
                    realDurationSeconds)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _totalSeconds = targetSeconds;
                    NotifyTimeChanged();
                    _advanceTween = null;
                });
        }

        private static int NormalizeSeconds(int seconds)
        {
            if (seconds < 0)
                return 0;

            if (seconds >= SecondsPerDay)
                return seconds % SecondsPerDay;

            return seconds;
        }

        private void NotifyTimeChanged()
        {
            OnTimeChanged?.Invoke();
        }
    }
}
