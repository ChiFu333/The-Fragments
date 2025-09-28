using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис для управления банками FMOD: загрузка/выгрузка банков и опциональная загрузка sample data.
    /// Если передан <see cref="FMODBankServiceAsset"/>, можно использовать значения по умолчанию и списки автозагрузки.
    /// </summary>
    internal sealed class FmodBankService : IBankService
    {
        private readonly FMODBankServiceAsset _bankSettings;

        /// <summary>
        /// Создать сервис банков.
        /// </summary>
        /// <param name="bankSettings">Актив с настройками сервиса банков (может быть null).</param>
        public FmodBankService(FMODBankServiceAsset bankSettings = null)
        {
            _bankSettings = bankSettings;
        }

        /// <summary>
        /// Асинхронно загрузить банк FMOD по имени.
        /// </summary>
        /// <param name="bankName">Имя банка.</param>
        /// <param name="loadSampleData">Загружать ли sample data.</param>
        /// <returns>true при успехе, иначе false.</returns>
        public async UniTask<bool> LoadBankAsync(string bankName, bool loadSampleData)
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
        /// Асинхронно загрузить несколько банков.
        /// </summary>
        /// <param name="bankNames">Список имён банков.</param>
        /// <param name="loadSampleData">Загружать ли sample data.</param>
        public async UniTask LoadBanksAsync(IEnumerable<string> bankNames, bool loadSampleData)
        {
            foreach (var name in bankNames)
            {
                await LoadBankAsync(name, loadSampleData);
            }
        }

        /// <summary>
        /// Выгрузить банк по имени.
        /// </summary>
        /// <param name="bankName">Имя банка.</param>
        /// <returns>true при успехе.</returns>
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
    }
}
