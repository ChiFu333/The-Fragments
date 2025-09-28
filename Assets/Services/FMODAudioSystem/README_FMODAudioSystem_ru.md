# FMOD Audio System (Unity) — Сервисный менеджер аудио

Этот модуль предоставляет полноценную интеграцию FMOD в Unity-проект с удобным сервисом-менеджером, поддержкой плейлистов, кроссфейдов, дакинга шин, динамической загрузки событий, параметров, снапшотов, тегов, лимитов конкуренции и др. В асинхронных сценариях используется UniTask, а для плавных изменений — DOTween.

Основные файлы:
- Runtime
  - `Runtime/FMODAudioManager.cs` — главный сервис-менеджер аудио.
  - `Runtime/FMODEventContainer.cs` — обёртка над EventInstance с колбэками тайм-линии.
  - `Runtime/FMODBus.cs` — обёртка над FMOD Bus.
  - `Runtime/FMODAudioSettings.cs` — ScriptableObject с настройками.
  - `Runtime/FMODBusVolumeSlider.cs` — привязка UI Slider к громкости шины.
  - `Runtime/FMODAudioTester.cs` — тестовый компонент для проверки функций.
  - `Runtime/FMODAudioBootstrapper.cs` — опциональный бутстраппер (можно отключить, если всё создаёт сервисная система).
- Editor
  - `Editor/FMODAudioSettingsEditor.cs` — доп. утилиты инспектора настроек.
  - `Editor/FMODAudioTesterEditor.cs` — удобные кнопки для тестера.

## Интеграция с сервисной системой проекта

В проекте есть системный локатор `G` и `GameBootstrapper`. Мы уже добавили интеграцию:

- В `Assets/G.cs`:
  - Поле: `public static FMODAudioSystem.FMODAudioManager FmodFMODAudio;`
  - Создание: `G.FmodFMODAudio = CreateSimpleService<FMODAudioManager>();`

Таким образом, доступ к аудио осуществляется через `G.FmodFMODAudio` отовсюду.

## Настройки и ресурсы

- Создайте `FMODAudioSettings` (меню Tools/Audio/FMOD/…) или вручную, и поместите в `Resources/Audio/FMOD/FMODAudioSettings.asset`.
- В настройках:
  - Раздел Buses — перечислите нужные шины (например, `bus:/`, `bus:/SFX`, `bus:/Music`).
  - Preload Events — события для предзагрузки.
  - Advanced: `MaxCachedEvents` — лимит кэша событий (LRU-эвикция), `StopAllOnSceneChange` — авто-стоп на смене сцены.

Примечание: менеджер в рантайме пытается загрузить настройки из `Resources/Audio/FMOD/FMODAudioSettings`.

## Базовые примеры использования

Все примеры ниже используют `G.FMODAudioManager` (сервис, созданный при запуске).

- Воспроизвести одноразовый звук в мировой позиции:
```csharp
G.FMODAudioManager.PlayOneShot(mySfxEvent, transform.position);
```

- Воспроизвести одноразовый звук с кулдауном (чтобы не засорять):
```csharp
bool played = G.FMODAudioManager.PlayOneShotWithCooldown(mySfxEvent, transform.position, 0.1f);
```

- Прикреплённый звук (следует за объектом):
```csharp
G.FMODAudioManager.PlayOneShotAttached(mySfxEvent, gameObject);
var loop = G.FMODAudioManager.PlayAttached(myLoopEvent, transform);
```

- Музыка с кроссфейдом:
```csharp
G.FMODAudioManager.PlayMusic(myMusicEvent, fadeSeconds: 0.75f);
```

- Плейлист музыки:
```csharp
G.FMODAudioManager.StartMusicPlaylist(new List<EventReference>{ track1, track2 }, loop: true, crossfadeSeconds: 0.5f);
G.FMODAudioManager.NextTrack();
G.FMODAudioManager.StopMusicPlaylist();
```

- Глобальные и локальные параметры:
```csharp
// Глобальный
await G.FMODAudioManager.RampGlobalParameter("Tension", 1f, 0.5f);

// Параметр конкретного события
await G.FMODAudioManager.RampParameter(myMusicEvent, "Intensity", 0.8f, 1.0f);
```

- Шины (Bus): громкость, плавное изменение, дакинг:
```csharp
G.FMODAudioManager.SetBusVolume("bus:/Music", 0.65f, persist: true);
await G.FMODAudioManager.FadeBusVolume("bus:/SFX", 0.4f, 0.7f);
await G.FMODAudioManager.DuckBus("bus:/Music", 0.25f, 0.1f, 1.0f, 0.3f);
```

- Снапшоты:
```csharp
var snap = G.FMODAudioManager.StartSnapshot(mySnapshot);
G.FMODAudioManager.StopSnapshot(mySnapshot);

await G.FMODAudioManager.PushSnapshotAsync(mySnapshot);
await G.FMODAudioManager.PopSnapshotAsync();
```

- Динамическая загрузка/выгрузка событий:
```csharp
G.FMODAudioManager.Preload(myEvent);
bool loaded = G.FMODAudioManager.IsLoaded(myEvent);
G.FMODAudioManager.Unload(myEvent);
```

- Лимиты конкуренции (чтобы не плодить копии лупов):
```csharp
if (G.FMODAudioManager.PlayIfUnderLimit(myLoopEvent, maxSimultaneous: 2)) {
    // Запустили ещё один экземпляр
}
```

- Теги событий (групповые операции):
```csharp
G.FMODAudioManager.RegisterTag(myEvent, "footsteps");
G.FMODAudioManager.StopByTag("footsteps");
```

## Управление банками

```csharp
await G.FMODAudioManager.LoadBanksAsync(new[]{ "Master", "SFX", "Music" }, loadSampleData: true);
G.FMODAudioManager.UnloadBank("SFX");
```

## UI: слайдер громкости шины

Добавьте `FMODBusVolumeSlider` на `Slider` и укажите `BusPath` (например, `bus:/Music`). Слайдер автоматически будет читать/писать громкость через менеджер, с опцией `Persist`.

## Тестовый компонент

Используйте `FMODAudioTester` + инспектор `FMODAudioTesterEditor` для быстрого ручного тестирования всех возможностей.
- Поля для задания `EventReference` и параметров.
- Кнопки для запусков: OneShot/Loop, Music/Playlist, Snapshots, Parameters, Buses, Banks, Tags.

## Асинхронность и твининг

- Асинхронный код — через UniTask (например, `await G.FmodFMODAudio.RampGlobalParameter(...);`).
- Плавные изменения — через DOTween. Мы не используем `IEnumerator`-корутины для твинов: все операции ожидаются посредством `UniTask`.

## Замечания по бутстрапперу

`FMODAudioBootstrapper` может создавать менеджер на старте, если вы не используете сервисную систему. В текущем проекте менеджер создаётся сервисной системой (`G.FmodFMODAudio`), поэтому бутстраппер можно оставить как есть или удалить.

## Миграция

- Постепенно заменяйте старые вызовы Unity AudioSource или легаси-аудиосистемы на вызовы `G.FmodFMODAudio`.
- Для UI громкости используйте `FMODBusVolumeSlider` вместо ручной логики.
- Музыку переведите на `PlayMusic`/плейлист с кроссфейдом.

## Отладка

- Добавьте логирование при необходимости (FMOD возвращает `RESULT`, ошибки пишутся в консоль).
- Для диагностики можете вывести текущие значения параметров/шины через публичные методы менеджера.

---
Если нужно расширить систему (например, добавить автоматическую загрузку банков по сценам, генерацию констант путей, или overlay-панель отладки) — дайте знать, добавлю.

## Понимание основных терминов (для тех, кто не аудио-инженер)

- __Событие (Event)__ — это «звук/музыкальный трек/снапшот» в FMOD. У события есть параметры (например, громкость, питч, интенсивность). Мы работаем с ними через `EventReference`.

- __Шина (Bus)__ — это «группа дорожек» или общий канал, куда сводятся звуки. Примеры шин: `bus:/SFX`, `bus:/Music`, `bus:/Voice`. 
  - Мы можем менять громкость целой группы сразу: `SetBusVolume("bus:/Music", 0.6f)`.
  - Плавные изменения — `FadeBusVolume(...)`.
  - __Дакинг (ducking)__ — временно понижать громкость одной шины, чтобы другая звучала лучше (например, приглушить музыку, когда персонаж разговаривает). Используйте: `DuckBus("bus:/Music", 0.3f, attack, hold, release)`.

- __Параметр (Parameter)__ — именованное значение внутри FMOD, влияющее на звучание. Примеры: `Intensity`, `LowPass`, `Weather`. 
  - Глобальные параметры влияют на всё: `SetGlobalParameter("Intensity", 1)`. 
  - Параметры конкретного события меняют только это событие (см. методы `SetParameter(...)`, `RampParameter(...)`).

- __Снапшот (Snapshot)__ — «состояние микса», применяющееся поверх текущего микса (как слой). 
  - Пример: при паузе игры сделать музыку тише и добавить фильтр — запускаем снапшот паузы: `StartSnapshot(snapshot)`, а при выходе — `StopSnapshot(snapshot)`.
  - Снапшоты — тоже события, их можно класть на стек приоритетов (`PushSnapshotAsync`, `PopSnapshotAsync`). Это удобно, если несколько систем хотят «временно» изменять микс.

- __Банк (Bank)__ — файл с данными звуков/микса. Чтобы звуки были доступны, нужные банки должны быть загружены. 
  - Загрузить: `LoadBanksAsync(new[]{"Master","SFX","Music"})`. Выгрузить: `UnloadBank("SFX")`.

- __Теги (Tags)__ — логические ярлыки для событий, чтобы управлять группами без перечисления каждого. 
  - Зарегистрировать тег для события: `RegisterTag(eventRef, "footsteps")`. 
  - Остановить все события с тегом: `StopByTag("footsteps")`.

- __Плейлист (Playlist)__ — последовательность музыкальных треков. У нас есть два способа:
  - Списком в коде: `StartMusicPlaylist(List<EventReference>, loop, crossfade)`.
  - Через актив `FMODEventSequence` — удобнее для дизайнеров. Создайте актив, заполните `Tracks`, `Loop`, `CrossfadeSeconds`, затем вызовите `StartMusicPlaylist(sequence)`.

## Как всё это связывается в проекте

- Вы обращаетесь к менеджеру `FMODAudioManager` через `G.FMODAudioManager`.
- Для эффектов используйте одноразовые звуки (`PlayOneShot`) или лупы (`PlayAttached`/`PlayIfUnderLimit`).
- Для громкости используйте шины (`FindBus`, `SetBusVolume`, `FadeBusVolume`, `DuckBus`).
- Для ситуационных изменений микса — снапшоты (`StartSnapshot`, `StopSnapshot`, стек снапшотов).
- Для адаптивности — параметры (`SetGlobalParameter`, `RampGlobalParameter`, `RampParameter`).
- Для музыки — `PlayMusic` и плейлисты (включая `FMODEventSequence`).

## Практические сценарии

- __Громкость в настройках игры.__
  - Подвяжите `FMODBusVolumeSlider` к `bus:/Music` и `bus:/SFX`. Слайдеры будут читать/писать громкость и сохранять её в PlayerPrefs (если `Persist = true`).

- __Автоматический дакинг речи поверх музыки.__
  - При старте речи: `await G.FMODAudioManager.DuckBus("bus:/Music", 0.25f, 0.1f, hold:1.5f, release:0.3f)`.
  - Или с IDisposable-хэндлом: `using (G.FMODAudioManager.BeginDuck(...)) { ... }` — восстановится автоматически при Dispose.

- __Пауза игры (тихий микс + фильтр).__
  - `var snap = pauseSnapshotRef; G.FMODAudioManager.StartSnapshot(snap);` при паузе.
  - `G.FMODAudioManager.StopSnapshot(snap);` при резюме. Либо используйте стек (`PushSnapshotAsync`/`PopSnapshotAsync`).

- __Адаптивная музыка.__
  - Глобальный параметр `Intensity` растёт в бою: `await RampGlobalParameter("Intensity", 1, 0.5f)` и постепенно снижается после боя.

- __Разные музыкальные плейлисты для сцен/биомов.__
  - Создайте несколько `FMODEventSequence` (например, "Forest", "Dungeon"). 
  - В сцене запускайте нужный актив: `G.FMODAudioManager.StartMusicPlaylist(forestSequence)`.

## Рекомендации и лучшие практики

- __Не спамьте одноразовыми звуками.__ Используйте `PlayOneShotWithCooldown`.
- __Ограничивайте конкуренцию лупов.__ `PlayIfUnderLimit(event, maxSimultaneous)`.
- __Сохраняйте громкость шин.__ Перед изменением убедитесь, что `FMODAudioSettings` содержит запись для нужной шины (ключ для PlayerPrefs формируется оттуда).
- __Проверяйте, что нужные банки загружены.__ Без них события могут не воспроизводиться.
- __Именуйте параметры осмысленно.__ Это упрощает их использование с `SetGlobalParameter`/`RampParameter`.
- __Используйте плейлисты-активы для контента.__ Это снижает правки кода и ускоряет итерации дизайнеров.

## Частые вопросы

- __Почему не слышно звук?__
  - Проверьте, загружены ли банки; корректен ли `EventReference`; не стоит ли громкость шины в 0.
  - Для 3D-событий проверьте позицию и аудиосистему FMOD (минимальная/максимальная дистанция в событии).

- __Чем шина отличается от снапшота?__
  - Шина — это «куда идёт звук» (группа). Снапшот — «как звучит микс сейчас» (слой поверх групп). Снапшот может одновременно менять несколько шин и эффектов.

- __Где хранить настройки громкости?__
  - В `FMODAudioSettings` задаётся список шин и ключей для сохранения. Менеджер сам читает/пишет PlayerPrefs.

## Мини-глоссарий

- __EventReference__ — ссылка на FMOD-событие.
- __EventInstance__ — живой экземпляр события во время проигрывания.
- __Bus__ — группа дорожек (канал свода).
- __Snapshot__ — слой состояния микса, накладывается поверх.
- __Parameter__ — контроллер звучания (числовое значение).
- __Bank__ — файл с контентом FMOD (события, миксы, сэмплы).
- __Ducking__ — временное приглушение одной шины относительно другой.
- __FMODEventSequence__ — актив-плейлист треков, удобный для дизайнеров.
