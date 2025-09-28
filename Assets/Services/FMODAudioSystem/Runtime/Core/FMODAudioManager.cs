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
        [Header("Settings")] [SerializeField] private FMODAudioSettingsAsset settingsAsset;

        [Header("Buses")] [SerializeField] private List<FMODBus> _buses = new();

        private readonly Dictionary<string, FMODBus> _busMap = new(StringComparer.OrdinalIgnoreCase);
        
        private readonly Dictionary<Guid, float> _cooldownUntil = new();
        private readonly Dictionary<Guid, int> _activeCounts = new();

        // Сервисы
        private IMusicService _music;
        private IBusService _bus;
        private IEventService _events;
        private IParameterService _parameters;
        private IBankService _banks;
        private ISnapshotService _snapshots;
        private ITagService _tagsService = null;
        private Action<EventReference, int> _musicStartHandler;
        private Action<EventReference, int> _musicEndHandler;

        private bool _initialized;

        /// <summary>
        /// Глобальная ссылка на экземпляр менеджера в сцене.
        /// </summary>
        public static FMODAudioManager instance { get; private set; }

        /// <summary>
        /// Событие вызывается при старте воспроизведения события (если оно известно менеджеру).
        /// </summary>
        public event Action<FMODEventContainer, Vector3> OnEventStarted;
        /// <summary>Коллбек начала трека плейлиста.</summary>
        public event Action<EventReference, int> OnMusicTrackStart;
        /// <summary>Коллбек окончания трека плейлиста.</summary>
        public event Action<EventReference, int> OnMusicTrackEnd;

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
            if (settingsAsset == null)
            {
                settingsAsset = Resources.Load<FMODAudioSettingsAsset>("Audio/FMOD/FMODAudioSettings");
            }

            Initialize();
        }

        /// <summary>
        /// Задать настройки менеджера в рантайме (например, загруженные из Resources).
        /// </summary>
        /// <param name="settingsAsset">Объект настроек <see cref="FMODAudioSettingsAsset"/>.</param>
        public void Configure(FMODAudioSettingsAsset settingsAsset)
        {
            if (settingsAsset == null) return;
            this.settingsAsset = settingsAsset;
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (settingsAsset != null && settingsAsset.DontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeBusesFromSettings();
            InitializeBuses();
            LoadInitialEvents();

            // Sub-services
            _music = settingsAsset != null && settingsAsset.MusicServiceAsset != null
                ? settingsAsset.MusicServiceAsset.BuildRuntime(this, settingsAsset)
                : new FmodMusicService(this, settingsAsset, null, null);
            _bus = settingsAsset != null && settingsAsset.BusServiceAsset != null
                ? settingsAsset.BusServiceAsset.BuildRuntime(this, settingsAsset)
                : new FmodBusService(this, settingsAsset, null);
            _events = settingsAsset != null && settingsAsset.EventServiceAsset != null
                ? settingsAsset.EventServiceAsset.BuildRuntime(settingsAsset)
                : new FmodEventService(settingsAsset, null);
            _parameters = settingsAsset != null && settingsAsset.ParameterServiceAsset != null
                ? settingsAsset.ParameterServiceAsset.BuildRuntime(this)
                : new FmodParameterService(this);
            _banks = settingsAsset != null && settingsAsset.BankServiceAsset != null
                ? settingsAsset.BankServiceAsset.BuildRuntime()
                : new FmodBankService();
            _snapshots = settingsAsset != null && settingsAsset.SnapshotServiceAsset != null
                ? settingsAsset.SnapshotServiceAsset.BuildRuntime(this)
                : new FmodSnapshotService(this);
            _tagsService = settingsAsset != null && settingsAsset.TagServiceAsset != null
                ? settingsAsset.TagServiceAsset.BuildRuntime()
                : new FmodTagService();

            // Привязка коллбэков
            _musicStartHandler = (ev, idx) => OnMusicTrackStart?.Invoke(ev, idx);
            _musicEndHandler = (ev, idx) => OnMusicTrackEnd?.Invoke(ev, idx);
            _music.OnTrackStart += _musicStartHandler;
            _music.OnTrackEnd += _musicEndHandler;

            if (settingsAsset != null && settingsAsset.StopAllOnSceneChange)
            {
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
            }
        }
        
        /// <summary>
        /// Инициализация сервиса (вызывается сервисной системой).
        /// </summary>
        public void Init()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void InitializeBusesFromSettings()
        {
            if (settingsAsset == null || settingsAsset.BusServiceAsset == null || settingsAsset.BusServiceAsset.Buses == null) return;
            for (int i = 0; i < settingsAsset.BusServiceAsset.Buses.Count; i++)
            {
                var b = settingsAsset.BusServiceAsset.Buses[i];
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
                
                if (settingsAsset != null && settingsAsset.BusServiceAsset != null)
                {
                    var init = settingsAsset.BusServiceAsset.FindBusInit(bus.Path);
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
            if (settingsAsset == null || settingsAsset.EventServiceAsset == null || settingsAsset.EventServiceAsset.PreloadEvents == null) return;
            foreach (var preload in settingsAsset.EventServiceAsset.PreloadEvents)
            {
                var name = DeriveEventName(preload.Name, preload.Reference);
                _events.CreateInstance(name, preload.Reference);
            }
        }

        private void OnActiveSceneChanged(Scene prev, Scene next)
        {
            StopAll(STOP_MODE.ALLOWFADEOUT);
        }

        private void OnDestroy()
        {
            if (settingsAsset != null && settingsAsset.StopAllOnSceneChange)
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            }
            if (_music != null)
            {
                if (_musicStartHandler != null) _music.OnTrackStart -= _musicStartHandler;
                if (_musicEndHandler != null) _music.OnTrackEnd -= _musicEndHandler;
                _musicStartHandler = null;
                _musicEndHandler = null;
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
            var container = FindContainer(soundName);
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
        /// Создать/найти контейнер события и воспроизвести его, прикрепив к GameObject.
        /// </summary>
        public FMODEventContainer PlayAttached(EventReference reference, GameObject target, Rigidbody rb = null)
        {
            var container = FindContainer(reference) ?? CreateInstance(DeriveEventName(null, reference), reference);
            if (container == null) return null;
            RuntimeManager.AttachInstanceToGameObject(container.EventInstance, target, rb);
            TouchUsage(container);
            container.Play();
            _tagsService?.BindActive(container);
            TrackInstanceEnd(container);
            return container;
        }

        /// <summary>
        /// Создать и закэшировать контейнер события с заданным именем (если ещё не создан).
        /// </summary>
        public FMODEventContainer CreateInstance(string name, EventReference sound) => _events.CreateInstance(name, sound);

        /// <summary>
        /// Создать контейнер с заданным именем и режимом остановки.
        /// </summary>
        public FMODEventContainer CreateInstance(string name, EventReference sound, STOP_MODE stopMode) => _events.CreateInstance(name, sound, stopMode);

        /// <summary>
        /// Перегрузка: создать контейнер с авто-выведенным именем (из пути/Guid).
        /// </summary>
        public FMODEventContainer CreateInstance(EventReference sound) => _events.CreateInstance(sound);

        /// <summary>
        /// Перегрузка: создать контейнер с авто-именем и кастомным режимом остановки.
        /// </summary>
        public FMODEventContainer CreateInstance(EventReference sound, STOP_MODE stopMode) => _events.CreateInstance(sound, stopMode);

        /// <summary>
        /// Предзагрузить событие (создать контейнер без старта воспроизведения).
        /// </summary>
        public FMODEventContainer Preload(EventReference reference) => _events.Preload(reference);

        /// <summary>
        /// Гарантировать, что событие загружено, и вернуть контейнер.
        /// </summary>
        public FMODEventContainer EnsureLoaded(EventReference reference) => _events.EnsureLoaded(reference);

        /// <summary>Проверить, загружено ли событие по ссылке.</summary>
        public bool IsLoaded(EventReference reference) => _events.IsLoaded(reference);
        /// <summary>Проверить, загружено ли событие по имени контейнера.</summary>
        public bool IsLoaded(string name) => _events.IsLoaded(name);

        /// <summary>
        /// Выгрузить и освободить контейнер по имени.
        /// </summary>
        public bool Unload(string name) => _events.Unload(name);

        /// <summary>
        /// Выгрузить и освободить контейнер по ссылке события.
        /// </summary>
        public bool Unload(EventReference reference) => _events.Unload(reference);

        /// <summary>
        /// Воспроизвести событие по имени контейнера.
        /// </summary>
        public FMODEventContainer Play(string name)
        {
            var container = FindContainer(name);
            container?.Play();
            if (container != null) TouchUsage(container);
            if (container != null) _tagsService?.BindActive(container);
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
            if (container != null) _tagsService?.BindActive(container);
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
            _tagsService?.UnbindActive(container);
            return container;
        }

        /// <summary>
        /// Остановить событие по ссылке.
        /// </summary>
        public FMODEventContainer Stop(EventReference reference, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            var container = FindContainer(reference);
            container?.Stop();
            _tagsService?.UnbindActive(container);
            return container;
        }

        /// <summary>
        /// Остановить все известные менеджеру события (включая музыку).
        /// </summary>
        public void StopAll(STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT) => _events.StopAll();

        /// <summary>
        /// Поставить на паузу/снять с паузы все управляемые события.
        /// </summary>
        public void SetPausedAll(bool paused) => _events.SetPausedAll(paused);

        // Parameters
        /// <summary>Установить глобальный параметр FMOD по имени.</summary>
        public void SetGlobalParameter(string parameterName, float value) => _parameters.SetGlobal(parameterName, value);
        /// <summary>Получить значение глобального параметра FMOD по имени.</summary>
        public float GetGlobalParameter(string parameterName) => _parameters.GetGlobal(parameterName);

        // Parameter ramps
        /// <summary>
        /// Плавно изменить глобальный параметр FMOD с помощью DOTween.
        /// </summary>
        public UniTask RampGlobalParameter(string parameterName, float target, float duration) => _parameters.RampGlobal(parameterName, target, duration);

        /// <summary>
        /// Плавно изменить параметр конкретного события по ссылке.
        /// </summary>
        public UniTask RampParameter(EventReference reference, string parameterName, float target, float duration) => _parameters.RampEvent(reference, parameterName, target, duration);

        /// <summary>
        /// Плавно изменить параметр события по имени контейнера.
        /// </summary>
        public UniTask RampParameter(string name, string parameterName, float target, float duration) => _parameters.RampEvent(name, parameterName, target, duration);

        /// <summary>
        /// Плавно изменить параметр события по экземпляру EventInstance.
        /// </summary>
        public UniTask RampParameter(EventInstance instance, string parameterName, float target, float duration) => _parameters.RampEvent(instance, parameterName, target, duration);
        

        // Music (delegated to service)
        /// <summary>
        /// Воспроизвести музыкальный трек с кроссфейдом. Заменяет текущую музыку.
        /// </summary>
        public void PlayMusic(EventReference reference, float fadeSeconds = -1f)
        {
            _music?.PlayMusic(reference, fadeSeconds);
        }

        // Music playlist helpers
        /// <summary>
        /// Запустить музыкальный плейлист. Треки проигрываются последовательно, поддерживается зацикливание.
        /// </summary>
        public void StartMusicPlaylist(List<EventReference> playlist, bool loop = true, float crossfadeSeconds = -1f)
        {
            _music?.StartPlaylist(playlist, loop, crossfadeSeconds);
        }

        /// <summary>
        /// Запустить музыкальный плейлист из ScriptableObject-актива <see cref="FMODEventSequence"/>.
        /// Если crossfadeSeconds &lt; 0, используется значение из актива.
        /// </summary>
        public void StartMusicPlaylist(FMODEventSequence sequence, float crossfadeSeconds = -1f)
        {
            _music?.StartPlaylist(sequence, crossfadeSeconds);
        }

        /// <summary>Остановить текущий плейлист.</summary>
        public void StopMusicPlaylist()
        {
            _music?.StopPlaylist();
        }

        /// <summary>Перейти к следующему треку плейлиста.</summary>
        public void NextTrack(float crossfadeSeconds = -1f)
        {
            _music?.NextTrack(crossfadeSeconds);
        }

        /// <summary>Перейти к предыдущему треку плейлиста.</summary>
        public void PreviousTrack(float crossfadeSeconds = -1f)
        {
            _music?.PreviousTrack(crossfadeSeconds);
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
        public FMODEventContainer FindContainer(string name) => _events.FindByName(name);
        /// <summary>Найти контейнер события по экземпляру.</summary>
        public FMODEventContainer FindContainer(EventInstance instance) => _events.FindByInstance(instance);
        /// <summary>Найти контейнер события по ссылке.</summary>
        public FMODEventContainer FindContainer(EventReference reference) => _events.FindByRef(reference);

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
            _bus?.SetBusVolume(busPath, volume, persist);
        }

        /// <summary>
        /// Плавно изменить громкость шины за указанное время.
        /// </summary>
        public UniTask FadeBusVolume(string busPath, float toVolume, float duration)
        {
            if (_bus == null) return UniTask.CompletedTask;
            return _bus.FadeBusVolume(busPath, toVolume, duration);
        }

        /// <summary>
        /// Загрузить сохранённую громкость шины из PlayerPrefs (либо вернуть -1, если нет сохранения).
        /// </summary>
        public float LoadBusVolume(FMODBusServiceAsset.BusInit init)
        {
            if (_bus == null) return -1f;
            return _bus.LoadBusVolume(init);
        }

        /// <summary>
        /// Дакинг (ducking) шины: атака до громкости toVolume, удержание, затем релиз обратно.
        /// </summary>
        public UniTask DuckBus(string busPath, float toVolume, float attackSeconds, float holdSeconds, float releaseSeconds)
        {
            if (_bus == null) return UniTask.CompletedTask;
            return _bus.DuckBus(busPath, toVolume, attackSeconds, holdSeconds, releaseSeconds);
        }

        // --- Bank management ---
        /// <summary>
        /// Асинхронно загрузить банк FMOD по имени. Опционально загрузить sample data.
        /// </summary>
        public UniTask<bool> LoadBankAsync(string bankName, bool loadSampleData = true) => _banks.LoadBankAsync(bankName, loadSampleData);

        /// <summary>
        /// Асинхронно загрузить несколько банков по списку имён.
        /// </summary>
        public UniTask LoadBanksAsync(IEnumerable<string> bankNames, bool loadSampleData = true) => _banks.LoadBanksAsync(bankNames, loadSampleData);

        /// <summary>
        /// Выгрузить банк FMOD по имени.
        /// </summary>
        public bool UnloadBank(string bankName) => _banks.UnloadBank(bankName);

        // Snapshot helpers (snapshots are events)
        /// <summary>
        /// Запустить снапшот (считается событием) и вернуть контейнер.
        /// </summary>
        public FMODEventContainer StartSnapshot(EventReference snapshot) => _snapshots.StartSnapshot(snapshot);

        /// <summary>
        /// Остановить снапшот.
        /// </summary>
        public void StopSnapshot(EventReference snapshot, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT) => _snapshots.StopSnapshot(snapshot, stopMode);

        // --- Snapshot stack (priority) ---
        /// <summary>Поставить снапшот на стек приоритетов.</summary>
        public UniTask PushSnapshotAsync(EventReference snapshot, float fadeSeconds = 0.25f) => _snapshots.PushSnapshotAsync(snapshot, fadeSeconds);

        /// <summary>
        /// Снять снапшот со стека и восстановить предыдущий. Опционально сделать паузу для плавности.
        /// </summary>
        public UniTask PopSnapshotAsync(float fadeSeconds = 0.25f) => _snapshots.PopSnapshotAsync(fadeSeconds);

        // --- Event tagging ---
        /// <summary>Зарегистрировать тег для события (шаблон для будущих инстансов).</summary>
        public void RegisterTag(EventReference reference, string tag) => _tagsService.RegisterTemplate(reference.Guid, tag);
        /// <summary>Снять тег-шаблон с события.</summary>
        public void UnregisterTag(EventReference reference, string tag) => _tagsService.UnregisterTemplate(reference.Guid, tag);
        /// <summary>Остановить все активные контейнеры, помеченные указанным тегом.</summary>
        public void StopByTag(string tag, STOP_MODE mode = STOP_MODE.ALLOWFADEOUT) => _tagsService.StopByTag(tag);
        
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
            _tagsService?.UnbindActive(container);
            var guid = container.EventReference.Guid;
            if (_activeCounts.TryGetValue(guid, out int c))
            {
                c = Mathf.Max(0, c - 1);
                if (c == 0) _activeCounts.Remove(guid); else _activeCounts[guid] = c;
            }
        }

        // --- internals ---
        private void TouchUsage(FMODEventContainer c) => _events.Touch(c);
        internal void TouchForService(FMODEventContainer c) => _events.Touch(c);
    }
}
