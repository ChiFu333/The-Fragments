using System;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using static FMOD.Studio.EVENT_CALLBACK_TYPE;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using RESULT = FMOD.RESULT;
using EVENT_CALLBACK = FMOD.Studio.EVENT_CALLBACK;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Контейнер для управления экземпляром события FMOD: хранит <see cref="EventInstance"/>,
    /// предоставляет удобные методы Play/Stop/Parameters и пробрасывает события тайм-линии (Beat/Bar/Marker).
    /// </summary>
    public class FMODEventContainer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public class TimelineInfo
        {
            /// <summary>Текущая музыкальная доля (bar) согласно тайм-линии.</summary>
            public int CurrentMusicBar { get; set; }
            /// <summary>Текущий удар (beat) согласно тайм-линии.</summary>
            public int CurrentMusicBeat { get; set; }
            /// <summary>Последний сработавший маркер тайм-линии.</summary>
            public string LastMarker { get; set; } = string.Empty;
            /// <summary>Текущий темп (BPM), если доступен из тайм-линии.</summary>
            public float CurrentTempo { get; set; }
        }

        /// <summary>Имя контейнера (пользовательское или выведенное из пути события).</summary>
        public string Name { get; }
        private readonly STOP_MODE _stopMode;
        private readonly EVENT_CALLBACK _beatCallback;
        /// <summary>Ссылка на событие FMOD.</summary>
        public EventReference EventReference { get; }
        /// <summary>Экземпляр события FMOD.</summary>
        public EventInstance EventInstance { get; }

        /// <summary>Событие Unity вызывается при изменении текущего бита тайм-линии.</summary>
        public UnityEvent<int> OnBeat { get; } = new();
        /// <summary>Событие Unity вызывается при изменении текущего бара тайм-линии.</summary>
        public UnityEvent<int> OnBar { get; } = new();
        /// <summary>Событие Unity вызывается при изменении маркера тайм-линии.</summary>
        public UnityEvent<string> OnMarker { get; } = new();

        private readonly TimelineInfo _timelineInfo;
        private GCHandle _selfHandle;
        private bool _isDisposed;

        /// <summary>
        /// Признак того, что остановка по умолчанию позволяет фейдаут (<see cref="STOP_MODE.ALLOWFADEOUT"/>).
        /// </summary>
        public bool AllowFadeOut => _stopMode == STOP_MODE.ALLOWFADEOUT;

        /// <summary>
        /// Создает контейнер события.
        /// </summary>
        /// <param name="name">Имя контейнера (для поиска по имени).</param>
        /// <param name="eventInstance">Экземпляр события.</param>
        /// <param name="reference">Ссылка на событие.</param>
        /// <param name="stopMode">Режим остановки по умолчанию.</param>
        public FMODEventContainer(string name, EventInstance eventInstance, EventReference reference,
            STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            Name = name;
            EventInstance = eventInstance;
            EventReference = reference;
            _stopMode = stopMode;

            _timelineInfo = new TimelineInfo();

            // Store a handle to THIS container in user data so callbacks can access it
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            EventInstance.setUserData(GCHandle.ToIntPtr(_selfHandle));

            _beatCallback = BeatEventCallback;
            EventInstance.setCallback(_beatCallback, TIMELINE_BEAT | TIMELINE_MARKER);
        }

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            var instance = new EventInstance(instancePtr);
            if (!instance.isValid() || instance.getUserData(out var userDataPtr) != RESULT.OK || userDataPtr == IntPtr.Zero)
            {
                return RESULT.OK;
            }

            var handle = GCHandle.FromIntPtr(userDataPtr);
            if (handle.Target is not FMODEventContainer container)
            {
                return RESULT.ERR_INVALID_HANDLE;
            }

            var info = container._timelineInfo;

            switch (type)
            {
                case TIMELINE_BEAT:
                {
                    var parameter = (FMOD.Studio.TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_BEAT_PROPERTIES));
                    if (parameter.bar != info.CurrentMusicBar)
                    {
                        container.OnBar.Invoke(parameter.bar);
                    }
                    if (parameter.beat != info.CurrentMusicBeat)
                    {
                        container.OnBeat.Invoke(parameter.beat);
                    }
                    info.CurrentMusicBar = parameter.bar;
                    info.CurrentMusicBeat = parameter.beat;
                    info.CurrentTempo = parameter.tempo;
                }
                    break;
                case TIMELINE_MARKER:
                {
                    var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                    var markerName = parameter.name;
                    if (markerName != info.LastMarker)
                    {
                        container.OnMarker.Invoke(markerName);
                        info.LastMarker = markerName;
                    }
                }
                    break;
            }

            return RESULT.OK;
        }

        /// <summary>Начать воспроизведение события.</summary>
        public void Play() => EventInstance.start();
        /// <summary>Остановить воспроизведение события согласно <see cref="_stopMode"/>.</summary>
        public void Stop() => EventInstance.stop(_stopMode);
        /// <summary>Поставить событие на паузу/снять с паузы.</summary>
        public void SetPaused(bool paused) => EventInstance.setPaused(paused);
        /// <summary>Установить параметр события по имени.</summary>
        public void SetParameter(string parameterName, float value) => EventInstance.setParameterByName(parameterName, value);
        /// <summary>Проверка валидности контейнера и его экземпляра.</summary>
        public bool IsValid() => !string.IsNullOrEmpty(Name) && EventInstance.isValid();

        /// <summary>
        /// Освобождение ресурсов. Останавливает и релизит <see cref="EventInstance"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (EventInstance.isValid())
            {
                EventInstance.setUserData(IntPtr.Zero);
                EventInstance.setCallback(null);
                EventInstance.stop(STOP_MODE.IMMEDIATE);
                EventInstance.release();
            }

            if (_selfHandle.IsAllocated) _selfHandle.Free();
            _isDisposed = true;
        }

        ~FMODEventContainer()
        {
            Dispose(false);
        }
    }
}
