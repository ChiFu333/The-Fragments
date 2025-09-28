using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-конфигурация сервиса тегов.
    /// Позволяет задать список «шаблонов» тегов для событий FMOD: при запуске любого экземпляра указанного события
    /// эти теги автоматически привяжутся к контейнеру, что даёт возможность управлять группами по тегам (например, останавливать все «footsteps»).
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Tag Service Asset", fileName = "FMODTagServiceAsset")]
    public class FMODTagServiceAsset : ScriptableObject
    {
        [Serializable]
        public class Template
        {
            /// <summary>
            /// Событие FMOD, к которому будут применяться теги по умолчанию.
            /// </summary>
            public EventReference Event;
            /// <summary>
            /// Список тегов (без учёта регистра), которые автоматически навешиваются на запущенный контейнер этого события.
            /// </summary>
            public List<string> Tags = new List<string>();
        }

        [Tooltip("Шаблоны тегов, которые будут применяться ко всем инстансам событий при воспроизведении")]
        public List<Template> Templates = new List<Template>();

        /// <summary>
        /// Построить рантайм-реализацию сервиса тегов на основе заданных шаблонов.
        /// </summary>
        /// <returns>Экземпляр <see cref="ITagService"/>.</returns>
        public ITagService BuildRuntime()
        {
            var svc = new FmodTagService();
            if (Templates != null)
            {
                for (int i = 0; i < Templates.Count; i++)
                {
                    var t = Templates[i];
                    if (t == null || t.Tags == null) continue;
                    foreach (var tag in t.Tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) continue;
                        svc.RegisterTemplate(t.Event.Guid, tag);
                    }
                }
            }
            return svc;
        }
    }
}
