# ZCraft — карта проекта для агентов

Воксельная open-world песочница на **Godot 4.7.2 + C# (.NET 8)**: собственный чанковый мир,
авторитарный сетевой слой, контент-реестр и предметы/контейнеры. Задумана как база-песочница,
из которой может вырасти любой режим (выживание, строительство, автоматизация, шутер).

Цель этого файла — чтобы новая сессия входила в проект за минуту, а не исследовала репозиторий
с нуля. **Читай его первым. Не читай весь `Source/` ради ориентации.**

## Стек и точки входа

| Что | Где |
|---|---|
| Godot-проект | `project.godot` (Forward+, D3D12 на Windows, Jolt, 40 тиков/с, интерполяция) |
| C#-проект | `ZCraft.csproj` (`Godot.NET.Sdk/4.7.2`, `net8.0`, сборка `ZCraft`) |
| Автозагрузки | `NetworkManager`, `InterfaceStack` |
| Сцены | `Scenes/World.tscn`, `Scenes/MainMenu.tscn`, `Scenes/SetupWorldAndContent.tscn` |
| Контент | `Content/` (модели `.tscn`, `ART_STYLE.md`) |
| GDScript | нет, всё на C# |

## Карта кода

- `Source/World/` — мир: `ZCraftWorld`, `Level`, `LevelData`, `Chunk`, `ChunkPos`, `ChunkSnapshot`,
  `IChunkObject`, `ITickable`; сущности — `Player/`, `Entity/Camera/`, `Item/`, `Container/`, `View/`.
- `Source/Networking/` — `NetworkManager` (фасад), `NetworkTransport` (батчинг/сжатие/потоки),
  `NetworkReplicator` + `INetworkReplicationObject` (репликация), `PacketBatchPipeline`, `NetworkProfiler`.
- `Source/Serialization/` — `BytePacker` и пул.
- `Source/Content/` — `Content`, `Registry`.
- `Source/UI/` — `InterfaceStack`.
- `Source/Scenes/` — сборка мира и меню.
- `Tests/NetworkTransport/` — консольный тест-раннер (не xunit) + `GodotHarness` для прогона в Godot.

## Команды

```powershell
dotnet build ZCraft.csproj                 # сборка
dotnet run --project Tests/NetworkTransport # тесты сети (консольный раннер)
```

Запуск игры — из редактора Godot (main scene задана uid в `project.godot`).

## Жёсткие правила

1. **Формат батча версионирован** (magic `0x435A`, версия `2`). Меняешь раскладку — поднимай версию
   и обновляй `Source/Networking/TRANSPORT.md`. Молчаливое изменение ломает совместимость.
2. **Потоки.** `SendPacketToPeer` можно звать из любого потока; приём и `SceneMultiplayer` — только
   основной поток; `ReadOnlySpan<byte>` в обработчике живёт до выхода из него.
3. **Физика не привязана к кадру** — 40 тиков/с, Jolt. Геймплейный код — в тиках, не в `_Process`.
4. **Никаких узлов на каждый блок.** Меши процедурные, крупные объёмы, коллизия по общему объёму
   (см. `Content/ART_STYLE.md`).
5. **Новый контент — через `Content/Registry`**, не хардкодом по месту использования.
6. Документация в репозитории — на русском. Стиль кода — как в соседних файлах (`.editorconfig` пустой).

## Тесты

`Tests/NetworkTransport` компилирует `Source/Networking/PacketBatchPipeline.cs` **напрямую** (`Compile Include`).
Любая правка этого файла обязана прогоняться через этот раннер. Тесты не покрывают мир и репликацию.

## Дисциплина контекста (экономия токенов)

- Начинай с этого файла + `TRANSPORT.md` (если задача сетевая) — этого хватает в 90% случаев.
- Не вставляй в контекст целые большие файлы: сначала `grep`, потом `read` с `offset/limit`.
- Не перечитывай то, что уже описано здесь. Если карта устарела — обнови её, это дешевле,
  чем повторное исследование в каждой сессии.

## Definition of done

`dotnet build` без ошибок → затронутые тесты проходят → сцена запускается в Godot → обновлены
`AGENTS.md`/`TODO.md`/`TRANSPORT.md`, если поменялись факты из них.
