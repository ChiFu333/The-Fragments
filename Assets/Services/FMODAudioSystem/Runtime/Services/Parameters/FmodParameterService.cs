using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис параметров FMOD: глобальные и параметры событий с поддержкой плавных изменений.
    /// </summary>
    internal sealed class FmodParameterService : IParameterService
    {
        private readonly FMODAudioManager _manager;
        private readonly FMODParameterServiceAsset _paramSettings;

        public FmodParameterService(FMODAudioManager manager, FMODParameterServiceAsset paramSettings = null)
        {
            _manager = manager;
            _paramSettings = paramSettings;
        }

        // --- Global parameters ---
        public void SetGlobal(string parameterName, float value)
        {
            var result = RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to set global parameter '{parameterName}': {result}");
            }
        }

        public float GetGlobal(string parameterName)
        {
            var result = RuntimeManager.StudioSystem.getParameterByName(parameterName, out float value);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to get global parameter '{parameterName}': {result}");
                return 0f;
            }
            return value;
        }

        public UniTask RampGlobal(string parameterName, float target, float duration)
        {
            float start = GetGlobal(parameterName);
            if (duration <= 0f)
            {
                SetGlobal(parameterName, target);
                return UniTask.CompletedTask;
            }
            var tween = DOTween.To(() => start, v => SetGlobal(parameterName, v), target, duration)
                .SetEase(_paramSettings != null ? _paramSettings.GlobalParameterEase : Ease.Linear)
                .SetUpdate(_paramSettings != null && _paramSettings.UseUnscaledTime);
            return UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }

        // --- Event parameters ---
        public UniTask RampEvent(EventReference reference, string parameterName, float target, float duration)
        {
            var c = _manager.EnsureLoaded(reference);
            if (c == null) return UniTask.CompletedTask;
            return RampEvent(c, parameterName, target, duration);
        }

        public UniTask RampEvent(string name, string parameterName, float target, float duration)
        {
            var c = _manager.FindContainer(name);
            if (c == null) return UniTask.CompletedTask;
            return RampEvent(c, parameterName, target, duration);
        }

        public UniTask RampEvent(EventInstance instance, string parameterName, float target, float duration)
        {
            var c = _manager.FindContainer(instance);
            if (c == null) return UniTask.CompletedTask;
            return RampEvent(c, parameterName, target, duration);
        }

        private UniTask RampEvent(FMODEventContainer container, string parameterName, float target, float duration)
        {
            if (container == null || !container.EventInstance.isValid()) return UniTask.CompletedTask;
            float start = 0f;
            container.EventInstance.getParameterByName(parameterName, out start);
            if (duration <= 0f)
            {
                container.EventInstance.setParameterByName(parameterName, target);
                return UniTask.CompletedTask;
            }
            var tween = DOTween.To(() => start, v => container.EventInstance.setParameterByName(parameterName, v), target, duration)
                .SetEase(_paramSettings != null ? _paramSettings.EventParameterEase : Ease.Linear)
                .SetUpdate(_paramSettings != null && _paramSettings.UseUnscaledTime);
            return UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }
    }
}
