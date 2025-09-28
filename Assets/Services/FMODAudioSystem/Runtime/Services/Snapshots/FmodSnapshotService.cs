using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис снапшотов: запуск/остановка снапшотов и стек приоритетов с опциональными задержками.
    /// </summary>
    internal sealed class FmodSnapshotService : ISnapshotService
    {
        private readonly FMODAudioManager _manager;
        private readonly Stack<EventReference> _snapshotStack = new();

        public FmodSnapshotService(FMODAudioManager manager)
        {
            _manager = manager;
        }

        public FMODEventContainer StartSnapshot(EventReference snapshot)
        {
            var c = _manager.EnsureLoaded(snapshot);
            c?.Play();
            if (c != null) _manager.TouchForService(c);
            return c;
        }

        public void StopSnapshot(EventReference snapshot, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            var c = _manager.FindContainer(snapshot);
            c?.Stop();
        }

        public async UniTask PushSnapshotAsync(EventReference snapshot, float fadeSeconds = 0.25f)
        {
            if (_snapshotStack.Count > 0)
            {
                var top = _snapshotStack.Peek();
                StopSnapshot(top);
                if (fadeSeconds > 0f) await UniTask.Delay(TimeSpan.FromSeconds(fadeSeconds));
            }
            _snapshotStack.Push(snapshot);
            StartSnapshot(snapshot);
        }

        public async UniTask PopSnapshotAsync(float fadeSeconds = 0.25f)
        {
            if (_snapshotStack.Count == 0) return;
            var top = _snapshotStack.Pop();
            StopSnapshot(top);
            if (_snapshotStack.Count > 0)
            {
                if (fadeSeconds > 0f) await UniTask.Delay(TimeSpan.FromSeconds(fadeSeconds));
                StartSnapshot(_snapshotStack.Peek());
            }
        }
    }
}
