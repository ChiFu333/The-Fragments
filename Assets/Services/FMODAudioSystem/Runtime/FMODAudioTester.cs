using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Cysharp.Threading.Tasks;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Универсальный тестер функций аудиосистемы FMOD. 
    /// Содержит набор публичных методов, которые можно вызывать из инспектора (через кастомный Editor)
    /// для проверки одноразовых звуков, лупов, плейлистов, снапшотов, параметров, шин, банков и тегов.
    /// </summary>
    [AddComponentMenu("Audio/FMOD/FMOD Audio Tester")]
    public class FMODAudioTester : MonoBehaviour
    {
        [Header("One Shot / Looping Events")] 
        /// <summary>Событие для одноразового воспроизведения.</summary>
        public EventReference OneShot;
        /// <summary>Событие для зацикленного воспроизведения (attach).</summary>
        public EventReference LoopingEvent;
        /// <summary>Цель для привязки звука (если не задано — используется Transform компонента).</summary>
        public Transform AttachTarget;
        /// <summary>Позиция для одноразового звука в мировых координатах.</summary>
        public Vector3 OneShotWorldPos = Vector3.zero;
        /// <summary>Кулдаун для одноразового звука (в секундах).</summary>
        [Min(0f)] public float OneShotCooldown = 0.1f;

        [Header("Music / Playlist")]
        /// <summary>Музыкальное событие для одиночного воспроизведения.</summary>
        public EventReference MusicEvent;
        /// <summary>Список музыкальных треков для плейлиста.</summary>
        public List<EventReference> Playlist = new();
        /// <summary>Длительность кроссфейда музыки.</summary>
        [Min(0f)] public float MusicCrossfade = 0.5f;

        [Header("Playlist Assets")]
        /// <summary>Плейлист как ScriptableObject-актив.</summary>
        public FMODEventSequence PlaylistAsset;

        [Header("Snapshots")]
        /// <summary>Снапшот для проверки.</summary>
        public EventReference Snapshot;

        [Header("Parameters")]
        /// <summary>Имя глобального параметра FMOD.</summary>
        public string GlobalParameterName = "";
        /// <summary>Целевое значение глобального параметра.</summary>
        public float GlobalParameterTarget = 1f;
        /// <summary>Длительность изменения глобального параметра.</summary>
        public float GlobalParameterDuration = 0.5f;
        /// <summary>Имя параметра события.</summary>
        public string EventParameterName = "";
        /// <summary>Целевое значение параметра события.</summary>
        public float EventParameterTarget = 1f;
        /// <summary>Длительность изменения параметра события.</summary>
        public float EventParameterDuration = 0.5f;

        [Header("Buses / Ducking")]
        /// <summary>Путь к шине.</summary>
        public string BusPath = "bus:/";
        /// <summary>Целевая громкость шины для Set/Fade.</summary>
        [Range(0f, 1f)] public float BusVolume = 1f;
        /// <summary>Целевая громкость шины для дакинга.</summary>
        [Range(0f, 1f)] public float DuckToVolume = 0.3f;
        /// <summary>Атака дакинга.</summary>
        [Min(0f)] public float DuckAttack = 0.1f;
        /// <summary>Удержание дакинга.</summary>
        [Min(0f)] public float DuckHold = 0.5f;
        /// <summary>Релиз дакинга.</summary>
        [Min(0f)] public float DuckRelease = 0.2f;
        /// <summary>Длительность плавного изменения громкости шины.</summary>
        [Min(0f)] public float BusFadeSeconds = 0.5f;

        [Header("Bank Management")] 
        /// <summary>Список банков для загрузки.</summary>
        public List<string> BanksToLoad = new();
        /// <summary>Имя банка для выгрузки.</summary>
        public string BankToUnload = "";

        [Header("Tags / Limits")] 
        /// <summary>Имя тега для групповых операций.</summary>
        public string TagName = "group";
        /// <summary>Максимальное число одновременных экземпляров лупа.</summary>
        [Min(1)] public int MaxSimultaneousForLoopingEvent = 2;

        private FMODEventContainer _attachedLoopInstance;

        // --- One-shots ---
        /// <summary>Воспроизвести одноразовый звук в позиции <see cref="OneShotWorldPos"/>.</summary>
        public void PlayOneShot() => G.FMODAudioManager.PlayOneShot(OneShot, OneShotWorldPos);
        /// <summary>Воспроизвести одноразовый звук, привязанный к <see cref="AttachTarget"/> или к собственному Transform.</summary>
        public void PlayOneShotAttached()
        {
            var target = AttachTarget == null ? transform : AttachTarget;
            G.FMODAudioManager.PlayOneShotAttached(OneShot, target.gameObject);
        }
        /// <summary>Воспроизвести одноразовый звук с кулдауном.</summary>
        public void PlayOneShotWithCooldown() => G.FMODAudioManager.PlayOneShotWithCooldown(OneShot, OneShotWorldPos, OneShotCooldown);

        // --- Looping ---
        /// <summary>Запустить зацикленное событие, привязав к цели (или к собственному Transform).</summary>
        public void PlayLoop() => _attachedLoopInstance = G.FMODAudioManager.PlayAttached(LoopingEvent, AttachTarget == null ? transform : AttachTarget);
        /// <summary>Остановить луп.</summary>
        public void StopLoop() => G.FMODAudioManager.Stop(LoopingEvent);
        /// <summary>Воспроизвести луп, если не превышен лимит одновременных экземпляров.</summary>
        public void PlayIfUnderLimit() => G.FMODAudioManager.PlayIfUnderLimit(LoopingEvent, MaxSimultaneousForLoopingEvent);

        // --- Dynamic load ---
        /// <summary>Предзагрузить событие лупа.</summary>
        public void Preload() => G.FMODAudioManager.Preload(LoopingEvent);
        /// <summary>Выгрузить событие лупа.</summary>
        public void Unload() => G.FMODAudioManager.Unload(LoopingEvent);
        /// <summary>Убедиться, что событие лупа загружено.</summary>
        public void EnsureLoaded() => G.FMODAudioManager.EnsureLoaded(LoopingEvent);

        // --- Music ---
        /// <summary>Воспроизвести музыкальный трек с кроссфейдом.</summary>
        public void PlayMusic() => G.FMODAudioManager.PlayMusic(MusicEvent, MusicCrossfade);
        /// <summary>Запустить плейлист.</summary>
        public void StartPlaylist() => G.FMODAudioManager.StartMusicPlaylist(Playlist, true, MusicCrossfade);
        /// <summary>Остановить плейлист.</summary>
        public void StopPlaylist() => G.FMODAudioManager.StopMusicPlaylist();
        /// <summary>Следующий трек плейлиста.</summary>
        public void NextTrack() => G.FMODAudioManager.NextTrack(MusicCrossfade);
        /// <summary>Предыдущий трек плейлиста.</summary>
        public void PreviousTrack() => G.FMODAudioManager.PreviousTrack(MusicCrossfade);

        /// <summary>Запустить плейлист из актива <see cref="PlaylistAsset"/>.</summary>
        public void StartPlaylistAsset()
        {
            if (PlaylistAsset != null)
                G.FMODAudioManager.StartMusicPlaylist(PlaylistAsset, MusicCrossfade);
        }

        // --- Snapshots ---
        /// <summary>Запустить снапшот.</summary>
        public void StartSnapshot() => G.FMODAudioManager.StartSnapshot(Snapshot);
        /// <summary>Остановить снапшот.</summary>
        public void StopSnapshot() => G.FMODAudioManager.StopSnapshot(Snapshot);
        /// <summary>Положить снапшот на стек приоритетов.</summary>
        public async UniTask PushSnapshotAsync() => await G.FMODAudioManager.PushSnapshotAsync(Snapshot, 0.25f);
        /// <summary>Снять снапшот со стека.</summary>
        public async UniTask PopSnapshotAsync() => await G.FMODAudioManager.PopSnapshotAsync(0.25f);

        // --- Parameters ---
        /// <summary>Плавно изменить глобальный параметр.</summary>
        public async UniTask RampGlobalParameter() => await G.FMODAudioManager.RampGlobalParameter(GlobalParameterName, GlobalParameterTarget, GlobalParameterDuration);
        /// <summary>Плавно изменить параметр события.</summary>
        public async UniTask RampEventParameter() => await G.FMODAudioManager.RampParameter(LoopingEvent, EventParameterName, EventParameterTarget, EventParameterDuration);

        // --- Buses ---
        /// <summary>Установить громкость шины и сохранить её.</summary>
        public void SetBusVolume() => G.FMODAudioManager.SetBusVolume(BusPath, BusVolume, true);
        /// <summary>Плавно изменить громкость шины.</summary>
        public async UniTask FadeBus() => await G.FMODAudioManager.FadeBusVolume(BusPath, BusVolume, BusFadeSeconds);
        /// <summary>Выполнить дакинг шины.</summary>
        public async UniTask DuckBus() => await G.FMODAudioManager.DuckBus(BusPath, DuckToVolume, DuckAttack, DuckHold, DuckRelease);

        // --- Control ---
        /// <summary>Остановить все события.</summary>
        public void StopAll() => G.FMODAudioManager.StopAll();
        /// <summary>Поставить паузу всем событиям.</summary>
        public void PauseAll() => G.FMODAudioManager.SetPausedAll(true);
        /// <summary>Снять паузу со всех событий.</summary>
        public void ResumeAll() => G.FMODAudioManager.SetPausedAll(false);

        // --- Banks ---
        /// <summary>Загрузить указанные банки FMOD.</summary>
        public async UniTask LoadBanksAsync() => await G.FMODAudioManager.LoadBanksAsync(BanksToLoad, true);
        /// <summary>Выгрузить банк FMOD по имени.</summary>
        public void UnloadBank() { if (!string.IsNullOrEmpty(BankToUnload)) G.FMODAudioManager.UnloadBank(BankToUnload); }

        // --- Tags ---
        /// <summary>Присвоить лупу тег <see cref="TagName"/>.</summary>
        public void RegisterLoopTag() => G.FMODAudioManager.RegisterTag(LoopingEvent, TagName);
        /// <summary>Снять тег с лупа.</summary>
        public void UnregisterLoopTag() => G.FMODAudioManager.UnregisterTag(LoopingEvent, TagName);
        /// <summary>Остановить все события с тегом <see cref="TagName"/>.</summary>
        public void StopByTag() => G.FMODAudioManager.StopByTag(TagName);
    }
}
