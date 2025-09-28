using System.Collections.Generic;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-настройки сервиса банков FMOD.
    /// Позволяет задать значения по умолчанию для загрузки банков и (опционально) список банков для загрузки при старте.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Bank Service Asset", fileName = "FMODBankServiceAsset")]
    public class FMODBankServiceAsset : ScriptableObject
    {
        /// <summary>
        /// Загружать ли sample data по умолчанию при вызове <see cref="IBankService.LoadBankAsync(string, bool)"/>.
        /// </summary>
        [Header("Defaults")]
        [Tooltip("Default value for LoadBankAsync if caller doesn't care")]
        public bool DefaultLoadSampleData = true;

        /// <summary>
        /// Необязательный список банков, которые можно загрузить на старте (если инициируете это из своего кода).
        /// </summary>
        [Tooltip("Optional list of banks to load on boot (if you call it manually from code)")]
        public List<string> BanksToLoadOnBoot = new();

        /// <summary>
        /// Построить рантайм-реализацию сервиса банков.
        /// </summary>
        /// <returns>Экземпляр <see cref="IBankService"/>.</returns>
        public IBankService BuildRuntime()
        {
            return new FmodBankService(this);
        }
    }
}
