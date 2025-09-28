using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Интерфейс сервиса управления банками FMOD.
    /// Позволяет загружать/выгружать банки и опционально подгружать sample data.
    /// </summary>
    public interface IBankService
    {
        /// <summary>
        /// Асинхронно загрузить банк по имени.
        /// </summary>
        /// <param name="bankName">Имя банка.</param>
        /// <param name="loadSampleData">Загружать ли sample data.</param>
        UniTask<bool> LoadBankAsync(string bankName, bool loadSampleData = true);

        /// <summary>
        /// Асинхронно загрузить несколько банков.
        /// </summary>
        /// <param name="bankNames">Имена банков.</param>
        /// <param name="loadSampleData">Загружать ли sample data.</param>
        UniTask LoadBanksAsync(IEnumerable<string> bankNames, bool loadSampleData = true);

        /// <summary>
        /// Выгрузить банк по имени.
        /// </summary>
        /// <param name="bankName">Имя банка.</param>
        /// <returns>true при успехе.</returns>
        bool UnloadBank(string bankName);
    }
}
