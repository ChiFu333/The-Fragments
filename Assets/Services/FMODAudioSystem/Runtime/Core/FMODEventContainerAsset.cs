using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-описание контейнера события, удобное для конфигурации из редактора.
    /// Позволяет задать имя контейнера, ссылку на событие, режим остановки и начальные параметры.
    /// В рантайме может быть построен через <see cref="BuildRuntime"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Event Container", fileName = "NewFMODEventContainer")]
    public class FMODEventContainerAsset : ScriptableObject
    {
        [Tooltip("Имя контейнера для поиска (если пусто, используется имя из пути/Guid)")]
        public string NameOverride;

        [Tooltip("Ссылка на FMOD событие")] public EventReference Event;

        [Tooltip("Режим остановки по умолчанию")] public STOP_MODE StopMode = STOP_MODE.ALLOWFADEOUT;

        [Serializable]
        public struct ParameterInit
        {
            public string Name;
            public float Value;
        }

        [Tooltip("Начальные параметры, применяемые после создания контейнера")]
        public List<ParameterInit> InitialParameters = new List<ParameterInit>();

        [Tooltip("Предзагружать контейнер при старте? (если менеджер вызовет Bootstrap)")]
        public bool PreloadOnBoot = false;

        /// <summary>
        /// Построить и вернуть рантайм-контейнер через менеджер. Если контейнер уже существует – будет возвращён кэш.
        /// </summary>
        public FMODEventContainer BuildRuntime(FMODAudioManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("FMODEventContainerAsset.BuildRuntime: Manager is null");
                return null;
            }
            if (!Event.IsNull)
            {
                FMODEventContainer container;
                if (!string.IsNullOrWhiteSpace(NameOverride))
                    container = manager.CreateInstance(NameOverride, Event, StopMode);
                else
                    container = manager.CreateInstance(Event, StopMode);

                if (container != null && InitialParameters != null)
                {
                    for (int i = 0; i < InitialParameters.Count; i++)
                    {
                        var p = InitialParameters[i];
                        if (!string.IsNullOrEmpty(p.Name))
                        {
                            container.SetParameter(p.Name, p.Value);
                        }
                    }
                }
                return container;
            }
            Debug.LogWarning($"FMODEventContainerAsset '{name}' has empty Event reference");
            return null;
        }
    }
}
