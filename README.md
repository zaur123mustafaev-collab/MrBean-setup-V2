# MrBean Visuals — установщик

Исходники и релизы `MrBeanVisualsSetup.exe` — одного файла, который ставит лаунчер MrBean
(Minecraft 1.21.11 + мод MrBean Visuals).

## Что внутри

| Файл | Назначение |
|---|---|
| `Setup.cs` | установщик целиком: форма, распаковка, ярлыки, ключ реестра, деинсталлятор |
| `build.sh` | сборка: `dotnet build` лаунчера → `payload.zip` → `csc Setup.cs` → `SHA256SUMS.txt` |
| `MrBeanVisualsSetup.exe` | готовый установщик (внутри `payload.zip` с файлами лаунчера) |

`build.sh` ждёт исходники лаунчера в `D:/MrBeanLauncher/src` и апдейтер в
`D:/MrBeanLauncher/updater`. Собирается под .NET Framework 4.8, внешних зависимостей нет.

## Установка / обновление / удаление

```
MrBeanVisualsSetup.exe                      — интерфейс с выбором папки
MrBeanVisualsSetup.exe /dir=D:\MrBean /silent — тихая установка
MrBeanVisualsSetup.exe /uninstall           — удаление
```

При обновлении `launcher.json`, `cache/`, `mods/`, `versions/` и `runtime/` не трогаются.

## Релизы

Бинарники лежат в Releases этого репозитория, тег вида `setup-v<версия>`. Каждый релиз
содержит `MrBeanVisualsSetup.exe` и `SHA256SUMS.txt`.

Выложить собранный установщик без `gh` можно скриптом `publish.sh`:

```
GH_TOKEN=xxxx bash publish.sh setup-v1.0.0
```

Токен нужен с правом `Contents: write` только на этот репозиторий.
