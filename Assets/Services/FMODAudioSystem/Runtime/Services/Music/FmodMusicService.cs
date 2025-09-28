using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис музыки: воспроизведение одиночных треков с кроссфейдом,
    /// управление плейлистом (список <see cref="EventReference"/> и <see cref="FMODEventSequence"/>),
    /// перемешивание с учётом политики и ограничений (no-repeat window),
    /// а также задержки до/после трека. Значения по умолчанию берутся из <see cref="FMODMusicServiceAsset"/>.
    /// </summary>
    internal sealed class FmodMusicService : IMusicService
    {
        private readonly FMODAudioManager _manager;
        private readonly FMODAudioSettingsAsset _settingsAsset; // может быть null
        private readonly FMODMusicServiceAsset _musicSettings; // defaults for playlist behavior
        private readonly IEventShufflePolicy _policy;

        private FMODEventContainer _currentMusic;
        private CancellationTokenSource _musicCts;

        private CancellationTokenSource _playlistCts;
        private readonly List<EventReference> _playlist = new();
        private int _playlistIndex = 0;
        private bool _playlistLoop = true;
        private bool _shuffle = false;
        private int _noRepeatWindow = 0;
        private List<float> _weights = null;
        private float _preDelay = 0f;
        private float _postDelay = 0f;
        private readonly List<int> _history = new();

        public event Action<EventReference, int> OnTrackStart;
        public event Action<EventReference, int> OnTrackEnd;

        /// <summary>
        /// Создать музыкальный сервис.
        /// </summary>
        /// <param name="manager">Ссылка на аудио-менеджер.</param>
        /// <param name="settingsAsset">Глобальные настройки (могут быть null).</param>
        /// <param name="musicSettings">Настройки музыкального сервиса (дефолтные тайминги/перемешивание).</param>
        /// <param name="policy">Опциональная политика взвешенного выбора треков.</param>
        public FmodMusicService(FMODAudioManager manager, FMODAudioSettingsAsset settingsAsset, FMODMusicServiceAsset musicSettings, IEventShufflePolicy policy = null)
        {
            _manager = manager;
            _settingsAsset = settingsAsset;
            _musicSettings = musicSettings;
            _policy = policy;
        }

        /// <summary>
        /// Воспроизвести музыкальный трек с кроссфейдом.
        /// </summary>
        /// <param name="reference">FMOD-событие трека.</param>
        /// <param name="fadeSeconds">Длительность кроссфейда. Если &lt; 0, берётся из настроек.</param>
        public void PlayMusic(EventReference reference, float fadeSeconds = -1f)
        {
            if (fadeSeconds < 0f)
                fadeSeconds = _musicSettings != null ? Mathf.Max(0f, _musicSettings.DefaultMusicFadeSeconds) : (_settingsAsset != null ? _settingsAsset.DefaultMusicFadeSeconds : 0.5f);

            var next = _manager.FindContainer(reference) ?? _manager.CreateInstance(reference);
            if (next == null) return;

            _musicCts?.Cancel();
            _musicCts = new CancellationTokenSource();
            CrossfadeMusicAsync(next, fadeSeconds, _musicCts.Token).Forget();
        }

        /// <summary>
        /// Запустить плейлист из списка событий.
        /// </summary>
        public void StartPlaylist(List<EventReference> playlist, bool loop = true, float crossfadeSeconds = -1f)
        {
            _playlist.Clear();
            if (playlist != null) _playlist.AddRange(playlist);
            _playlistIndex = 0;
            _playlistLoop = loop;
            _playlistCts?.Cancel();
            _playlistCts = new CancellationTokenSource();
            MusicPlaylistAsync(crossfadeSeconds, _playlistCts.Token).Forget();
        }

        /// <summary>
        /// Запустить плейлист по активу <see cref="FMODEventSequence"/>.
        /// Учитывает значения из секвенции, а при их отсутствии — дефолты из <see cref="FMODMusicServiceAsset"/>.
        /// </summary>
        public void StartPlaylist(FMODEventSequence sequence, float crossfadeSeconds = -1f)
        {
            _playlist.Clear();
            if (sequence != null && sequence.Tracks != null) _playlist.AddRange(sequence.Tracks);
            _playlistIndex = 0;
            _playlistLoop = sequence != null ? sequence.Loop : true;
            _shuffle = sequence != null ? sequence.Shuffle : (_musicSettings != null && _musicSettings.DefaultShuffle);
            _noRepeatWindow = sequence != null ? Mathf.Max(0, sequence.NoRepeatWindow) : (_musicSettings != null ? Mathf.Max(0, _musicSettings.DefaultNoRepeatWindow) : 0);
            _preDelay = sequence != null ? Mathf.Max(0f, sequence.PreDelaySeconds) : (_musicSettings != null ? Mathf.Max(0f, _musicSettings.DefaultPreDelaySeconds) : 0f);
            _postDelay = sequence != null ? Mathf.Max(0f, sequence.PostDelaySeconds) : (_musicSettings != null ? Mathf.Max(0f, _musicSettings.DefaultPostDelaySeconds) : 0f);
            _weights = null;
            if (_policy != null && _playlist.Count > 0)
            {
                var ws = _policy.GetWeights(_playlist);
                if (ws != null && ws.Count == _playlist.Count) _weights = new List<float>(ws);
            }
            _history.Clear();
            _playlistCts?.Cancel();
            _playlistCts = new CancellationTokenSource();
            float xf = crossfadeSeconds >= 0f ? crossfadeSeconds : (sequence != null ? Mathf.Max(0f, sequence.CrossfadeSeconds) : (_musicSettings != null ? Mathf.Max(0f, _musicSettings.DefaultMusicFadeSeconds) : -1f));
            MusicPlaylistAsync(xf, _playlistCts.Token).Forget();
        }

        /// <summary>
        /// Остановить текущий плейлист (без остановки ранее запущенного трека принудительно).
        /// </summary>
        public void StopPlaylist()
        {
            _playlistCts?.Cancel();
            _playlistCts = null;
        }

        /// <summary>
        /// Перейти к следующему треку плейлиста.
        /// </summary>
        public void NextTrack(float crossfadeSeconds = -1f)
        {
            if (_playlist.Count == 0) return;
            _playlistIndex = (_playlistIndex + 1) % _playlist.Count;
            PlayMusic(_playlist[_playlistIndex], crossfadeSeconds);
        }

        /// <summary>
        /// Перейти к предыдущему треку плейлиста.
        /// </summary>
        public void PreviousTrack(float crossfadeSeconds = -1f)
        {
            if (_playlist.Count == 0) return;
            _playlistIndex = (_playlistIndex - 1 + _playlist.Count) % _playlist.Count;
            PlayMusic(_playlist[_playlistIndex], crossfadeSeconds);
        }

        private async UniTaskVoid CrossfadeMusicAsync(FMODEventContainer next, float duration, CancellationToken ct)
        {
            var prev = _currentMusic;
            _currentMusic = next;

            next.EventInstance.setVolume(0f);
            next.Play();

            if (prev == null || !prev.IsValid() || duration <= 0f)
            {
                next.EventInstance.setVolume(1f);
                prev?.Stop();
                return;
            }

            float startPrev = 1f;
            float startNext = 0f;

            var tween = DOTween.To(() => 0f, v =>
            {
                float a = v;
                next.EventInstance.setVolume(Mathf.Lerp(startNext, 1f, a));
                prev.EventInstance.setVolume(Mathf.Lerp(startPrev, 0f, a));
            }, 1f, duration);

            using (ct.Register(() => { if (tween.IsActive()) tween.Kill(); }))
            {
                await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete(), cancellationToken: ct);
            }
            next.EventInstance.setVolume(1f);
            prev.Stop();
        }

        private async UniTaskVoid MusicPlaylistAsync(float crossfadeSeconds, CancellationToken ct)
        {
            if (_playlist.Count == 0) return;
            while (!ct.IsCancellationRequested)
            {
                if (_preDelay > 0f) await UniTask.Delay(TimeSpan.FromSeconds(_preDelay), cancellationToken: ct);

                var trackIndex = SelectNextIndex();
                _playlistIndex = trackIndex;
                var track = _playlist[trackIndex];

                OnTrackStart?.Invoke(track, trackIndex);
                PlayMusic(track, crossfadeSeconds);

                // Wait until current music stopped
                while (_currentMusic != null && !ct.IsCancellationRequested)
                {
                    _currentMusic.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                    if (state == PLAYBACK_STATE.STOPPED) break;
                    await UniTask.Yield(cancellationToken: ct);
                }

                OnTrackEnd?.Invoke(track, trackIndex);
                if (_postDelay > 0f) await UniTask.Delay(TimeSpan.FromSeconds(_postDelay), cancellationToken: ct);

                // Sequential advancement when no shuffle/weights
                if (!_shuffle && (_weights == null || _weights.Count == 0))
                {
                    int count = _playlist.Count;
                    if (_playlistLoop)
                    {
                        _playlistIndex = (_playlistIndex + 1) % count;
                    }
                    else
                    {
                        _playlistIndex++;
                        if (_playlistIndex >= count) break;
                    }
                }
            }
        }

        private int SelectNextIndex()
        {
            int count = _playlist.Count;
            if (count == 0) return 0;

            // Sequential fallback when no advanced options
            if (!_shuffle && (_weights == null || _weights.Count == 0))
            {
                if (_playlistIndex < 0 || _playlistIndex >= count) _playlistIndex = 0;
                PushHistory(_playlistIndex);
                return _playlistIndex;
            }

            // Build candidate set excluding last N
            var excluded = _noRepeatWindow > 0 ? new HashSet<int>(_history) : null;
            int start = 0;
            int chosen = -1;
            if (_weights != null && _weights.Count == count)
            {
                float total = 0f;
                for (int i = 0; i < count; i++)
                {
                    if (excluded != null && excluded.Contains(i)) continue;
                    float w = Mathf.Max(0f, _weights[i]);
                    total += w;
                }
                if (total <= 0f)
                {
                    // fall back to uniform
                    chosen = ChooseUniform(count, excluded);
                }
                else
                {
                    float r = UnityEngine.Random.value * total;
                    float acc = 0f;
                    for (int i = 0; i < count; i++)
                    {
                        if (excluded != null && excluded.Contains(i)) continue;
                        float w = Mathf.Max(0f, _weights[i]);
                        acc += w;
                        if (r <= acc) { chosen = i; break; }
                    }
                    if (chosen < 0) chosen = ChooseUniform(count, excluded);
                }
            }
            else
            {
                chosen = ChooseUniform(count, excluded);
            }

            if (chosen < 0) chosen = 0;
            PushHistory(chosen);
            return chosen;
        }

        private static int ChooseUniform(int count, HashSet<int> excluded)
        {
            if (excluded == null || excluded.Count >= count) return UnityEngine.Random.Range(0, count);
            // Try a few times to pick a non-excluded index
            for (int t = 0; t < 8; t++)
            {
                int idx = UnityEngine.Random.Range(0, count);
                if (!excluded.Contains(idx)) return idx;
            }
            // Fallback scan
            for (int i = 0; i < count; i++) if (!excluded.Contains(i)) return i;
            return 0;
        }

        private void PushHistory(int idx)
        {
            if (_noRepeatWindow <= 0) return;
            _history.Add(idx);
            while (_history.Count > _noRepeatWindow) _history.RemoveAt(0);
        }
    }
}
