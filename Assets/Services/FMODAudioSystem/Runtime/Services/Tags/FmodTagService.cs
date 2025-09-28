using System;
using System.Collections.Generic;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис тегов: хранит шаблоны тегов для GUID событий и активные привязки тегов к проигрываемым контейнерам.
    /// Позволяет регистрировать теги по событиям, автоматически навешивать их при старте воспроизведения,
    /// а также выполнять групповые операции по тегам (например, остановку всех контейнеров с тегом).
    /// </summary>
    internal sealed class FmodTagService : ITagService
    {
        // tag -> active containers
        private readonly Dictionary<string, HashSet<FMODEventContainer>> _activeByTag = new(StringComparer.OrdinalIgnoreCase);
        // event Guid -> template tags (apply when instance plays)
        private readonly Dictionary<Guid, HashSet<string>> _templatesByGuid = new();
        // container -> active tags
        private readonly Dictionary<FMODEventContainer, HashSet<string>> _activeTagsByContainer = new();

        /// <summary>
        /// Зарегистрировать тег-шаблон для события с указанным GUID.
        /// </summary>
        public void RegisterTemplate(Guid guid, string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (!_templatesByGuid.TryGetValue(guid, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _templatesByGuid[guid] = set;
            }
            set.Add(tag);
        }

        /// <summary>
        /// Отменить регистрацию тега-шаблона для события.
        /// </summary>
        public void UnregisterTemplate(Guid guid, string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (_templatesByGuid.TryGetValue(guid, out var set))
            {
                set.Remove(tag);
                if (set.Count == 0) _templatesByGuid.Remove(guid);
            }
        }

        /// <summary>
        /// Привязать активные теги к контейнеру при его запуске.
        /// </summary>
        public void BindActive(FMODEventContainer container)
        {
            if (container == null) return;
            var guid = container.EventReference.Guid;
            if (!_templatesByGuid.TryGetValue(guid, out var tags) || tags.Count == 0) return;

            if (!_activeTagsByContainer.TryGetValue(container, out var activeTags))
            {
                activeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _activeTagsByContainer[container] = activeTags;
            }

            foreach (var tag in tags)
            {
                activeTags.Add(tag);
                if (!_activeByTag.TryGetValue(tag, out var set))
                {
                    set = new HashSet<FMODEventContainer>();
                    _activeByTag[tag] = set;
                }
                set.Add(container);
            }
        }

        /// <summary>
        /// Снять активные теги с контейнера (при остановке/завершении).
        /// </summary>
        public void UnbindActive(FMODEventContainer container)
        {
            if (container == null) return;
            if (!_activeTagsByContainer.TryGetValue(container, out var tags)) return;
            foreach (var tag in tags)
            {
                if (_activeByTag.TryGetValue(tag, out var set))
                {
                    set.Remove(container);
                    if (set.Count == 0) _activeByTag.Remove(tag);
                }
            }
            _activeTagsByContainer.Remove(container);
        }

        /// <summary>
        /// Остановить все активные контейнеры, помеченные указанным тегом.
        /// </summary>
        public void StopByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (!_activeByTag.TryGetValue(tag, out var set)) return;
            // snapshot because Stop() may trigger lifecycle updates
            var copy = new List<FMODEventContainer>(set);
            foreach (var c in copy)
            {
                c?.Stop();
            }
        }
    }
}
