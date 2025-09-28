using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Главный менеджер аудио для интеграции FMOD в проект.
    /// Работает как сервис (<see cref="IService"/>), поддерживает динамическую загрузку событий,
    /// кэширование, плейлисты, кроссфейд музыки, снапшоты, тэги событий, лимиты конкуренции,
    /// дакинг (ducking) шин, плавные изменения параметров через DOTween и асинхронный API на UniTask.
    /// </summary>
    public class FMODAudioManager : MonoBehaviour, IService
    {
        [Header("Settings")] [SerializeField] private FMODAudioSettings _settings;

        [Header("Buses")] [SerializeField] private List<FMODBus> _buses = new();

        private readonly Dictionary<string, FMODBus> _busMap = new(StringComparer.OrdinalIgnoreCase);

        [Header("Events (runtime cache)")] private readonly List<FMODEventContainer> _eventContainers = new();
        private readonly Dictionary<string, FMODEventContainer> _eventsByName = new(StringComparer.Ordinal);
        private readonly Dictionary<Guid, FMODEventContainer> _eventsByGuid = new();
        private readonly Dictionary<FMODEventContainer, float> _lastUsed = new();
        private readonly Dictionary<Guid, float> _cooldownUntil = new();
        private readonly Dictionary<Guid, int> _activeCounts = new();

        private FMODEventContainer _currentMusic;
        private CancellationTokenSource _musicCts;
        private CancellationTokenSource _playlistCts;
        private readonly List<EventReference> _musicPlaylist = new();
        private int _playlistIndex = 0;
        private bool _playlistLoop = false;
        private bool _initialized;

        /// <summary>
        /// Глобальная ссылка на экземпляр менеджера в сцене.
        /// </summary>
        public static FMODAudioManager instance { get; private set; }

        /// <summary>
        /// Событие вызывается при старте воспроизведения события (если оно известно менеджеру).
        /// </summary>
        public event Action<FMODEventContainer, Vector3> OnEventStarted;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError($"Found more than one {GetType().Name} object in the scene. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Fallback: try to load settings from Resources if not set via inspector
            if (_settings == null)
            {
                _settings = Resources.Load<FMODAudioSettings>("Audio/FMOD/FMODAudioSettings");
            }

            Initialize();
        }

        /// <summary>
        /// Задать настройки менеджера в рантайме (например, загруженные из Resources).
        /// </summary>
        /// <param name="settings">Объект настроек <see cref="FMODAudioSettings"/>.</param>
        public void Configure(FMODAudioSettings settings)
        {
            if (settings == null) return;
            _settings = settings;
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_settings != null && _settings.DontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeBusesFromSettings();
            InitializeBuses();
            LoadInitialEvents();

            if (_settings != null && _settings.StopAllOnSceneChange)
            {
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
            }
        }

        // IService implementation for the project's service system
        /// <summary>
        /// Инициализация сервиса (вызывается сервисной системой).
        /// </summary>
        public void Init()
        {
            // Settings may be set via inspector, Configure(), or Resources fallback in Awake
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void InitializeBusesFromSettings()
        {
            if (_settings == null || _settings.Buses == null) return;
            // Ensure we have matching runtime bus entries
            for (int i = 0; i < _settings.Buses.Count; i++)
            {
                var b = _settings.Buses[i];
                if (string.IsNullOrEmpty(b.Path)) continue;
                var bus = new FMODBus(b.Path);
                _buses.Add(bus);
            }
        }

        private void InitializeBuses()
        {
            _busMap.Clear();
            for (int i = 0; i < _buses.Count; i++)
            {
                var bus = _buses[i];
                if (!bus.Initialize())
                {
                    Debug.LogError($"Failed to initialize bus {bus.Path} at index {i}.");
                    continue;
                }
                _busMap[bus.Path] = bus;

                // Apply persisted or default volume
                if (_settings != null)
                {
                    var init = _settings.FindBusInit(bus.Path);
                    if (init != null)
                    {
                        float vol = LoadBusVolume(init);
                        if (vol < 0f) vol = Mathf.Clamp01(init.DefaultVolume);
                        bus.SetVolume(vol);
                    }
                }
            }
        }

        private void LoadInitialEvents()
        {
            if (_settings == null || _settings.PreloadEvents == null) return;
            foreach (var preload in _settings.PreloadEvents)
            {
                var name = DeriveEventName(preload.Name, preload.Reference);
                CreateInstance(name, preload.Reference);
            }
        }

        private void OnActiveSceneChanged(Scene prev, Scene next)
        {
            StopAll(STOP_MODE.ALLOWFADEOUT);
        }

        private void OnDestroy()
        {
            if (_settings != null && _settings.StopAllOnSceneChange)
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            }
        }

        private static string DeriveEventName(string customName, EventReference reference)
        {
            if (!string.IsNullOrWhiteSpace(customName)) return customName;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(reference.Path))
            {
                var parts = reference.Path.Split('/');
                return parts.Length > 0 ? parts[^1] : reference.Guid.ToString();
            }
#endif
            return reference.Guid.ToString();
        }

        // --- Public API ---
        /// <summary>
        /// Воспроизвести одноразовый звук по ссылке FMOD в мировой позиции.
        /// </summary>
        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound, worldPos);
            var c = FindContainer(sound);
            if (c != null) OnEventStarted?.Invoke(c, worldPos);
            if (c != null) TouchUsage(c);
        }

        /// <summary>
        /// Воспроизвести одноразовый звук по имени контейнера (если он загружен) в мировой позиции.
        /// </summary>
        public void PlayOneShot(string soundName, Vector3 worldPos)
        {
            FMODEventContainer container = FindContainer(soundName);
            if (container != null)
            {
                RuntimeManager.PlayOneShot(container.EventReference, worldPos);
                OnEventStarted?.Invoke(container, worldPos);
                TouchUsage(container);
            }
        }

        /// <summary>
        /// Воспроизвести одноразовый звук с кулдауном по GUID события.
        /// </summary>
        /// <returns><c>false</c>, если кулдаун ещё не истёк.</returns>
        public bool PlayOneShotWithCooldown(EventReference sound, Vector3 worldPos, float cooldownSeconds)
        {
            var guid = sound.Guid;
            float now = Time.time;
            if (_cooldownUntil.TryGetValue(guid, out float until) && now < until)
            {
                return false;
            }
            _cooldownUntil[guid] = now + Mathf.Max(0f, cooldownSeconds);
            PlayOneShot(sound, worldPos);
            return true;
        }

        /// <summary>
        /// Воспроизвести одноразовый звук, прикреплённый к объекту.
        /// </summary>
        public void PlayOneShotAttached(EventReference reference, GameObject target)
        {
            RuntimeManager.PlayOneShotAttached(reference, target);
        }

        /// <summary>
        /// Создать/найти контейнер события и воспроизвести его, прикрепив к Transform.
        /// </summary>
        public FMODEventContainer PlayAttached(EventReference reference, Transform target, Rigidbody rb = null)
        {
            var container = FindContainer(reference) ?? CreateInstance(DeriveEventName(null, reference), reference);
            if (container == null) return null;
            RuntimeManager.AttachInstanceToGameObject(container.EventInstance, target, rb);
            TouchUsage(container);
            container.Play();
            TrackInstanceEnd(container);
            return container;
        }

        /// <summary>
        /// Создать и закэшировать контейнер события с заданным именем (если ещё не создан).
        /// </summary>
        public FMODEventContainer CreateInstance(string name, EventReference sound)
        {
            if (_eventsByName.TryGetValue(name, out var existing)) return existing;

            var eventInstance = RuntimeManager.CreateInstance(sound);
            if (!eventInstance.isValid()) return null;

            var container = new FMODEventContainer(name, eventInstance, sound);
            _eventContainers.Add(container);
            _eventsByName[name] = container;
            _eventsByGuid[sound.Guid] = container;
            TouchUsage(container);
            EvictIfNeeded();
            return container;
        }

        /// <summary>
        /// Перегрузка: создать контейнер с авто-выведенным именем (из пути/Guid).
        /// </summary>
        public FMODEventContainer CreateInstance(EventReference sound)
        {
            var name = DeriveEventName(null, sound);
            return CreateInstance(name, sound);
        }

        /// <summary>
        /// Предзагрузить событие (создать контейнер без старта воспроизведения).
        /// </summary>
        public FMODEventContainer Preload(EventReference reference)
        {
            return FindContainer(reference) ?? CreateInstance(reference);
        }

        /// <summary>
        /// Гарантировать, что событие загружено, и вернуть контейнер.
        /// </summary>
        public FMODEventContainer EnsureLoaded(EventReference reference)
        {
            return Preload(reference);
        }

        /// <summary>Проверить, загружено ли событие по ссылке.</summary>
        public bool IsLoaded(EventReference reference) => _eventsByGuid.ContainsKey(reference.Guid);
        /// <summary>Проверить, загружено ли событие по имени контейнера.</summary>
        public bool IsLoaded(string name) => _eventsByName.ContainsKey(name);

        /// <summary>
        /// Выгрузить и освободить контейнер по имени.
        /// </summary>
        public bool Unload(string name)
        {
            if (!_eventsByName.TryGetValue(name, out var c)) return false;
            _eventsByName.Remove(name);
            _eventsByGuid.Remove(c.EventReference.Guid);
            _eventContainers.Remove(c);
            c.Dispose();
            return true;
        }

        /// <summary>
        /// Выгрузить и освободить контейнер по ссылке события.
        /// </summary>
        public bool Unload(EventReference reference)
        {
            if (!_eventsByGuid.TryGetValue(reference.Guid, out var c)) return false;
            _eventsByGuid.Remove(reference.Guid);
            _eventsByName.Remove(c.Name);
            _eventContainers.Remove(c);
            c.Dispose();
            return true;
        }

        /// <summary>
        /// Воспроизвести событие по имени контейнера.
        /// </summary>
        public FMODEventContainer Play(string name)
        {
            var container = FindContainer(name);
            container?.Play();
            if (container != null) TouchUsage(container);
            if (container != null) TrackInstanceEnd(container);
            return container;
        }

        /// <summary>
        /// Воспроизвести событие по ссылке. При необходимости контейнер будет создан.
        /// </summary>
        public FMODEventContainer Play(EventReference reference)
        {
            var container = FindContainer(reference) ?? CreateInstance(DeriveEventName(null, reference), reference);
            container?.Play();
            if (container != null) TouchUsage(container);
            if (container != null) TrackInstanceEnd(container);
            return container;
        }

        /// <summary>
        /// Воспроизвести событие, если текущее число его активных экземпляров меньше порога.
        /// </summary>
        public bool PlayIfUnderLimit(EventReference reference, int maxSimultaneous)
        {
            int count = 0;
            _activeCounts.TryGetValue(reference.Guid, out count);
            if (count >= Mathf.Max(1, maxSimultaneous)) return false;
            var c = Play(reference);
            return c != null;
        }

        /// <summary>
        /// Остановить событие по имени контейнера.
        /// </summary>
        public FMODEventContainer Stop(string name, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            var container = FindContainer(name);
            container?.Stop();
            return container;
        }

        /// <summary>
        /// Остановить событие по ссылке.
        /// </summary>
        public FMODEventContainer Stop(EventReference reference, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            var container = FindContainer(reference);
            container?.Stop();
            return container;
        }

        /// <summary>
        /// Остановить все известные менеджеру события (включая музыку).
        /// </summary>
        public void StopAll(STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                _eventContainers[i].EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING)
                {
                    _eventContainers[i].Stop();
                }
            }
        }

        /// <summary>
        /// Поставить на паузу/снять с паузы все управляемые события.
        /// </summary>
        public void SetPausedAll(bool paused)
        {
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                _eventContainers[i].SetPaused(paused);
            }
        }

        // Parameters
        /// <summary>Установить глобальный параметр FMOD по имени.</summary>
        public void SetGlobalParameter(string parameterName, float value)
        {
            var result = RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to set global parameter '{parameterName}': {result}");
            }
        }
        /// <summary>Получить значение глобального параметра FMOD по имени.</summary>
        public float GetGlobalParameter(string parameterName)
        {
            var result = RuntimeManager.StudioSystem.getParameterByName(parameterName, out float value);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"Failed to get global parameter '{parameterName}': {result}");
                return 0;
            }
            return value;
        }

        // Parameter ramps
        /// <summary>
        /// Плавно изменить глобальный параметр FMOD с помощью DOTween.
        /// </summary>
        public UniTask RampGlobalParameter(string parameterName, float target, float duration)
        {
            float start = GetGlobalParameter(parameterName);
            if (duration <= 0f)
            {
                SetGlobalParameter(parameterName, target);
                return UniTask.CompletedTask;
            }
            var tween = DOTween.To(() => start, v => SetGlobalParameter(parameterName, v), target, duration);
            return UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }

        /// <summary>
        /// Плавно изменить параметр конкретного события по ссылке.
        /// </summary>
        public UniTask RampParameter(EventReference reference, string parameterName, float target, float duration)
        {
            var c = EnsureLoaded(reference);
            if (c == null) return UniTask.CompletedTask;
            return RampParameter(container: c, parameterName, target, duration);
        }

        /// <summary>
        /// Плавно изменить параметр события по имени контейнера.
        /// </summary>
        public UniTask RampParameter(string name, string parameterName, float target, float duration)
        {
            var c = FindContainer(name);
            if (c == null) return UniTask.CompletedTask;
            return RampParameter(container: c, parameterName, target, duration);
        }

        /// <summary>
        /// Плавно изменить параметр события по экземпляру EventInstance.
        /// </summary>
        public UniTask RampParameter(EventInstance instance, string parameterName, float target, float duration)
        {
            var c = FindContainer(instance);
            if (c == null) return UniTask.CompletedTask;
            return RampParameter(container: c, parameterName, target, duration);
        }

        private UniTask RampParameter(FMODEventContainer container, string parameterName, float target, float duration)
        {
            if (container == null || !container.EventInstance.isValid()) return UniTask.CompletedTask;
            float start = 0f;
            container.EventInstance.getParameterByName(parameterName, out start);
            if (duration <= 0f)
            {
                container.EventInstance.setParameterByName(parameterName, target);
                return UniTask.CompletedTask;
            }
            var tween = DOTween.To(() => start, v => container.EventInstance.setParameterByName(parameterName, v), target, duration);
            return UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }

        // Music
        /// <summary>
        /// Воспроизвести музыкальный трек с кроссфейдом. Заменяет текущую музыку.
        /// </summary>
        public void PlayMusic(EventReference reference, float fadeSeconds = -1f)
        {
            if (fadeSeconds < 0f)
                fadeSeconds = _settings != null ? _settings.DefaultMusicFadeSeconds : 0.5f;

            var next = FindContainer(reference) ?? CreateInstance(DeriveEventName(null, reference), reference);
            if (next == null) return;

            _musicCts?.Cancel();
            _musicCts = new CancellationTokenSource();
            CrossfadeMusicAsync(next, fadeSeconds, _musicCts.Token).Forget();
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

        // Music playlist helpers
        /// <summary>
        /// Запустить музыкальный плейлист. Треки проигрываются последовательно, поддерживается зацикливание.
        /// </summary>
        public void StartMusicPlaylist(List<EventReference> playlist, bool loop = true, float crossfadeSeconds = -1f)
        {
            _musicPlaylist.Clear();
            if (playlist != null) _musicPlaylist.AddRange(playlist);
            _playlistIndex = 0;
            _playlistLoop = loop;
            _playlistCts?.Cancel();
            _playlistCts = new CancellationTokenSource();
            MusicPlaylistAsync(crossfadeSeconds, _playlistCts.Token).Forget();
        }

        /// <summary>
        /// Запустить музыкальный плейлист из ScriptableObject-актива <see cref="FMODEventSequence"/>.
        /// Если crossfadeSeconds &lt; 0, используется значение из актива.
        /// </summary>
        public void StartMusicPlaylist(FMODEventSequence sequence, float crossfadeSeconds = -1f)
        {
            _musicPlaylist.Clear();
            if (sequence != null && sequence.Tracks != null) _musicPlaylist.AddRange(sequence.Tracks);
            _playlistIndex = 0;
            _playlistLoop = sequence != null ? sequence.Loop : true;
            _playlistCts?.Cancel();
            _playlistCts = new CancellationTokenSource();
            float xf = crossfadeSeconds >= 0f ? crossfadeSeconds : (sequence != null ? Mathf.Max(0f, sequence.CrossfadeSeconds) : -1f);
            MusicPlaylistAsync(xf, _playlistCts.Token).Forget();
        }

        /// <summary>Остановить текущий плейлист.</summary>
        public void StopMusicPlaylist()
        {
            _playlistCts?.Cancel();
            _playlistCts = null;
        }

        /// <summary>Перейти к следующему треку плейлиста.</summary>
        public void NextTrack(float crossfadeSeconds = -1f)
        {
            if (_musicPlaylist.Count == 0) return;
            _playlistIndex = (_playlistIndex + 1) % _musicPlaylist.Count;
            PlayMusic(_musicPlaylist[_playlistIndex], crossfadeSeconds);
        }

        /// <summary>Перейти к предыдущему треку плейлиста.</summary>
        public void PreviousTrack(float crossfadeSeconds = -1f)
        {
            if (_musicPlaylist.Count == 0) return;
            _playlistIndex = (_playlistIndex - 1 + _musicPlaylist.Count) % _musicPlaylist.Count;
            PlayMusic(_musicPlaylist[_playlistIndex], crossfadeSeconds);
        }

        private async UniTaskVoid MusicPlaylistAsync(float crossfadeSeconds, CancellationToken ct)
        {
            if (_musicPlaylist.Count == 0) return;
            while (!ct.IsCancellationRequested)
            {
                var track = _musicPlaylist[_playlistIndex];
                Debug.Log($"Playlist playing track: {track}");
                PlayMusic(track, crossfadeSeconds);

                // Wait until current music stopped
                while (_currentMusic != null && !ct.IsCancellationRequested)
                {
                    _currentMusic.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                    if (state == PLAYBACK_STATE.STOPPED) break;
                    await UniTask.Yield(cancellationToken: ct);
                }

                _playlistIndex++;
                if (_playlistIndex >= _musicPlaylist.Count)
                {
                    if (_playlistLoop) _playlistIndex = 0; else break;
                }
            }
        }

        // Emitters
        private void SetupEmitterInternal(StudioEventEmitter emitter, FMODEventContainer container)
        {
            if (emitter == null || container == null) return;
            emitter.EventReference = container.EventReference;
            emitter.AllowFadeout = container.AllowFadeOut;
        }

        /// <summary>Настроить <see cref="StudioEventEmitter"/> на событие по имени контейнера.</summary>
        public void SetupEmitter(StudioEventEmitter emitter, string name) =>
            SetupEmitterInternal(emitter, FindContainer(name));
        /// <summary>Настроить <see cref="StudioEventEmitter"/> на событие по ссылке.</summary>
        public void SetupEmitter(StudioEventEmitter emitter, EventReference reference) =>
            SetupEmitterInternal(emitter, FindContainer(reference));
        /// <summary>Настроить <see cref="StudioEventEmitter"/> на событие по экземпляру.</summary>
        public void SetupEmitter(StudioEventEmitter emitter, EventInstance instance) =>
            SetupEmitterInternal(emitter, FindContainer(instance));

        // Find helpers
        /// <summary>Найти контейнер события по имени.</summary>
        public FMODEventContainer FindContainer(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            _eventsByName.TryGetValue(name, out var c);
            return c;
        }
        /// <summary>Найти контейнер события по экземпляру.</summary>
        public FMODEventContainer FindContainer(EventInstance instance)
        {
            if (!instance.isValid()) return null;
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                if (_eventContainers[i].EventInstance.Equals(instance)) return _eventContainers[i];
            }
            return null;
        }
        /// <summary>Найти контейнер события по ссылке.</summary>
        public FMODEventContainer FindContainer(EventReference reference)
        {
            if (_eventsByGuid.TryGetValue(reference.Guid, out var c)) return c;
            return null;
        }

        // Buses
        /// <summary>
        /// Найти шину по пути (например, bus:/Music).
        /// </summary>
        public FMODBus FindBus(string busPath)
        {
            if (string.IsNullOrEmpty(busPath)) return null;
            _busMap.TryGetValue(busPath, out var b);
            return b;
        }

        /// <summary>
        /// Установить громкость шины и опционально сохранить её в PlayerPrefs.
        /// </summary>
        public void SetBusVolume(string busPath, float volume, bool persist = true)
        {
            var bus = FindBus(busPath);
            if (bus == null) return;
            volume = Mathf.Clamp01(volume);
            bus.SetVolume(volume);
            if (!persist || _settings == null) return;
            var init = _settings.FindBusInit(busPath);
            if (init == null) return;
            var key = _settings.GetBusPrefsKey(init);
            PlayerPrefs.SetFloat(key, volume);
        }

        /// <summary>
        /// Плавно изменить громкость шины за указанное время.
        /// </summary>
        public UniTask FadeBusVolume(string busPath, float toVolume, float duration)
        {
            var bus = FindBus(busPath);
            if (bus == null) return UniTask.CompletedTask;
            return FadeBus(bus, toVolume, duration);
        }

        private UniTask FadeBus(FMODBus bus, float toVolume, float duration)
        {
            float startVol = Mathf.Max(0f, bus.GetVolume());
            toVolume = Mathf.Clamp01(toVolume);
            if (duration <= 0f)
            {
                bus.SetVolume(toVolume);
                return UniTask.CompletedTask;
            }
            var tween = DOTween.To(() => startVol, v => bus.SetVolume(v), toVolume, duration);
            return UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }

        /// <summary>
        /// Загрузить сохранённую громкость шины из PlayerPrefs (либо вернуть -1, если нет сохранения).
        /// </summary>
        public float LoadBusVolume(FMODAudioSettings.BusInit init)
        {
            if (init == null) return -1f;
            var key = _settings.GetBusPrefsKey(init);
            if (!PlayerPrefs.HasKey(key)) return -1f;
            return PlayerPrefs.GetFloat(key);
        }

        /// <summary>
        /// Дакинг (ducking) шины: атака до громкости toVolume, удержание, затем релиз обратно.
        /// </summary>
        public UniTask DuckBus(string busPath, float toVolume, float attackSeconds, float holdSeconds, float releaseSeconds)
        {
            var bus = FindBus(busPath);
            if (bus == null) return UniTask.CompletedTask;
            return DuckBus(bus, toVolume, attackSeconds, holdSeconds, releaseSeconds);
        }

        private async UniTask DuckBus(FMODBus bus, float toVolume, float attack, float hold, float release)
        {
            float startVol = Mathf.Max(0f, bus.GetVolume());
            toVolume = Mathf.Clamp01(toVolume);

            if (attack > 0f)
            {
                var tweenIn = DOTween.To(() => startVol, v => bus.SetVolume(v), toVolume, attack);
                await UniTask.WaitUntil(() => !tweenIn.IsActive() || tweenIn.IsComplete());
            }
            else bus.SetVolume(toVolume);

            if (hold > 0f) await UniTask.Delay(TimeSpan.FromSeconds(hold));

            if (release > 0f)
            {
                var tweenOut = DOTween.To(() => toVolume, v => bus.SetVolume(v), startVol, release);
                await UniTask.WaitUntil(() => !tweenOut.IsActive() || tweenOut.IsComplete());
            }
            else bus.SetVolume(startVol);
        }

        // --- Bank management ---
        /// <summary>
        /// Асинхронно загрузить банк FMOD по имени. Опционально загрузить sample data.
        /// </summary>
        public async UniTask<bool> LoadBankAsync(string bankName, bool loadSampleData = true)
        {
            try
            {
                RuntimeManager.LoadBank(bankName, true);
                if (loadSampleData)
                {
                    var bank = RuntimeManager.StudioSystem.getBank(bankName, out FMOD.Studio.Bank b);
                    if (bank == FMOD.RESULT.OK)
                    {
                        b.loadSampleData();
                    }
                    await UniTask.Yield();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"FMOD LoadBank failed for '{bankName}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Асинхронно загрузить несколько банков по списку имён.
        /// </summary>
        public async UniTask LoadBanksAsync(IEnumerable<string> bankNames, bool loadSampleData = true)
        {
            foreach (var name in bankNames)
            {
                await LoadBankAsync(name, loadSampleData);
            }
        }

        /// <summary>
        /// Выгрузить банк FMOD по имени.
        /// </summary>
        public bool UnloadBank(string bankName)
        {
            try
            {
                RuntimeManager.UnloadBank(bankName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"FMOD UnloadBank failed for '{bankName}': {e.Message}");
                return false;
            }
        }

        // Snapshot helpers (snapshots are events)
        /// <summary>
        /// Запустить снапшот (считается событием) и вернуть контейнер.
        /// </summary>
        public FMODEventContainer StartSnapshot(EventReference snapshot)
        {
            var c = EnsureLoaded(snapshot);
            c?.Play();
            if (c != null) TouchUsage(c);
            return c;
        }

        /// <summary>
        /// Остановить снапшот.
        /// </summary>
        public void StopSnapshot(EventReference snapshot, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            var c = FindContainer(snapshot);
            c?.Stop();
        }

        // --- Snapshot stack (priority) ---
        private readonly Stack<EventReference> _snapshotStack = new();
        /// <summary>
        /// Поставить снапшот на стек приоритетов. Опционально сделать паузу для плавности.
        /// </summary>
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

        /// <summary>
        /// Снять снапшот со стека и восстановить предыдущий. Опционально сделать паузу для плавности.
        /// </summary>
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

        // --- Event tagging ---
        private readonly Dictionary<string, HashSet<Guid>> _tags = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Зарегистрировать тег для события (для группового управления).</summary>
        public void RegisterTag(EventReference reference, string tag)
        {
            if (!_tags.TryGetValue(tag, out var set))
            {
                set = new HashSet<Guid>();
                _tags[tag] = set;
            }
            set.Add(reference.Guid);
        }
        /// <summary>Снять тег с события.</summary>
        public void UnregisterTag(EventReference reference, string tag)
        {
            if (_tags.TryGetValue(tag, out var set)) set.Remove(reference.Guid);
        }
        /// <summary>Остановить все события, помеченные указанным тегом.</summary>
        public void StopByTag(string tag, STOP_MODE mode = STOP_MODE.ALLOWFADEOUT)
        {
            if (!_tags.TryGetValue(tag, out var set)) return;
            foreach (var guid in set)
            {
                if (_eventsByGuid.TryGetValue(guid, out var container))
                {
                    container.Stop();
                }
            }
        }

        // Track active counts for concurrency limits
        private void TrackInstanceEnd(FMODEventContainer container)
        {
            if (container == null) return;
            var guid = container.EventReference.Guid;
            if (!_activeCounts.ContainsKey(guid)) _activeCounts[guid] = 0;
            _activeCounts[guid]++;
            _ = WaitForStopThenDecrement(container);
        }

        private async UniTaskVoid WaitForStopThenDecrement(FMODEventContainer container)
        {
            if (container == null) return;
            PLAYBACK_STATE state;
            do
            {
                container.EventInstance.getPlaybackState(out state);
                await UniTask.Yield();
            } while (state != PLAYBACK_STATE.STOPPED);
            var guid = container.EventReference.Guid;
            if (_activeCounts.TryGetValue(guid, out int c))
            {
                c = Mathf.Max(0, c - 1);
                if (c == 0) _activeCounts.Remove(guid); else _activeCounts[guid] = c;
            }
        }

        // --- internals ---
        private void TouchUsage(FMODEventContainer c)
        {
            _lastUsed[c] = Time.time;
        }

        private void EvictIfNeeded()
        {
            if (_settings == null || _settings.MaxCachedEvents <= 0) return;
            if (_eventContainers.Count <= _settings.MaxCachedEvents) return;

            FMODEventContainer candidate = null;
            float oldest = float.MaxValue;
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                var c = _eventContainers[i];
                c.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING) continue;
                float last = _lastUsed.TryGetValue(c, out var t) ? t : 0f;
                if (last < oldest)
                {
                    oldest = last;
                    candidate = c;
                }
            }
            if (candidate == null) return;
            _eventContainers.Remove(candidate);
            _eventsByName.Remove(candidate.Name);
            _eventsByGuid.Remove(candidate.EventReference.Guid);
            _lastUsed.Remove(candidate);
            candidate.Dispose();
        }
    }
}
