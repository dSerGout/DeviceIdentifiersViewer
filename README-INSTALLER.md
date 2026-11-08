# Как собрать установщик `DeviceIdentifiersViewerSetup.exe`

Этот документ — про **создание установщика**, а не про сборку приложения. Установщик нужен, чтобы конечный пользователь запустил один `.exe` и приложение установилось в систему (появилось в «Пуск», «Программы и компоненты», с ярлыком на рабочем столе).

## Что понадобится

| Что | Зачем | Размер |
|---|---|---|
| **.NET 10 SDK** | публикация приложения (`dotnet publish`) | уже установлен |
| **Inno Setup 6** | компиляция установщика | 5 МБ |

**Inno Setup 6** — бесплатный, ставится за минуту: https://jrsoftware.org/isdl.php

Скачайте установщик → запустите → «Next → Next → Install». Ничего настраивать не нужно, всё по умолчанию.

## Структура файлов установщика

В корне проекта (рядом с `.sln`) должны лежать:

```

DeviceIdentifiersViewer.sln
DeviceIdentifiersViewer.csproj
installer.iss              ← скрипт установщика
build-installer.bat        ← автоматизация сборки
README-INSTALLER.md        ← этот файл
LICENSE                    ← лицензия, показывается в установщике
publish\                   ← создастся автоматически
installer-output\          ← тут появится Setup.exe

```

## Как собрать (2 минуты)

### Шаг 1. Установить Inno Setup 6
Скачать: https://jrsoftware.org/isdl.php → установить с настройками по умолчанию.

### Шаг 2. Запустить `build-installer.bat`
Двойной клик по **`build-installer.bat`** в папке проекта. Скрипт:

1. Проверит, что установлены `dotnet` и Inno Setup.
2. Опубликует приложение в папку `publish\` (`dotnet publish -c Release`).
3. Скомпилирует установщик через Inno Setup.
4. Выведет путь к готовому `Setup.exe`.

Ожидаемый вывод в конце:

```

============================================================
ГОТОВО
============================================================

Установщик: C:...\installer-output\DeviceIdentifiersViewerSetup.exe

Двойной клик по нему — установка приложения на ПК.

```

### Шаг 3. Готово
Файл **`installer-output\DeviceIdentifiersViewerSetup.exe`** — это и есть ваш установщик. Размер ~70–100 МБ (внутри все .NET-зависимости + WinUI).

## Как работает установка (что видит пользователь)

1. Двойной клик по `DeviceIdentifiersViewerSetup.exe`.
2. Мастер установки: «Next» → выбор папки → «Install».
3. Проверка Windows App Runtime: если его нет, установщик покажет сообщение и откроет ссылку для скачивания.
4. Ярлык **Device Identifiers Viewer** появляется:
   - в меню «Пуск» (папка Device Identifiers Viewer),
   - на рабочем столе (если выбрано),
   - в «Программы и компоненты» (для удаления).
5. Приложение **не запускается автоматически** — только устанавливается. Запуск — через ярлык.

## Как это работает (технически)

`installer.iss` использует **Inno Setup** — стандартный установщик для Windows-приложений (на нём собраны Notepad++, Git for Windows, многих других). Что делает скрипт:

| Функция | Реализация |
|---|---|
| Копирование файлов | `Source: "publish\*"; DestDir: "{app}"` — всё из publish в `C:\Program Files\DeviceIdentifiersViewer\` |
| Ярлыки | `[Icons]` — рабочий стол, «Пуск», панель быстрого запуска |
| Запись в «Программы и компоненты» | `AppId`, `AppName`, `UninstallDisplayName` — авто |
| Деинсталлятор | Inno Setup генерирует его автоматически |
| Требует права админа | `PrivilegesRequired=admin` (нужно для записи в Program Files) |
| Проверка Windows App Runtime | `[Code]` — `IsDotNetAppRuntimeInstalled()` смотрит в реестр и предлагает скачать, если нет |
| Лицензия | `LicenseFile=LICENSE` — показывается в начале установки |
| Языки | русский + английский |

## Что делать, если...

### «Inno Setup 6 не найден»
Установите его: https://jrsoftware.org/isdl.php. Если поставили в нестандартную папку — откройте `build-installer.bat` и поправьте путь в блоке `set "ISCC=..."`.

### «dotnet publish завершился с ошибкой»
Убедитесь, что проект собирается отдельно:
```powershell
dotnet build DeviceIdentifiersViewer.csproj -c Release -p:Platform=x64
```

Если здесь ошибка — она и в publish. Проверьте .NET 10 SDK (`dotnet --list-sdks`).

### «Компиляция Inno Setup завершилась с ошибкой»

Откройте `installer.iss` в блокноте и проверьте:

- `LicenseFile=LICENSE` — файл `LICENSE` должен существовать в корне проекта (у вас есть).
- `SetupIconFile=Assets\app.ico` — **этот файл может отсутствовать**. Если нет — удалите строку `SetupIconFile=Assets\app.ico` из `installer.iss` (иконка не критична, поставится дефолтная Inno Setup).
- `Source: "publish\*"` — папка `publish` должна существовать (её создаёт `build-installer.bat`).

### «Установщик запустился, но приложение не открывается»

Это **не проблема установщика** — установка прошла. Проблема в Windows App Runtime. Установите его вручную:
https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe

### «Хочу изменить название, версию, издателя»

Все параметры — в начале `installer.iss`:

```
#define MyAppName "Device Identifiers Viewer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DeviceIdentifiersViewer"
```

Меняйте и пересобирайте (`build-installer.bat`).

## Плюсы и минусы этого подхода

**Плюсы:**

- Один файл `Setup.exe` — можно раздать кому угодно.
- Работает без сертификата (нет SmartScreen-предупреждений «Unknown publisher»).
- Полноценная установка: ярлыки, «Программы и компоненты», деинсталлятор.
- Проверка зависимостей (Windows App Runtime).

**Минусы:**

- Не появится в «Установленные приложения» Windows 11 как MSIX (но будет в «Программы и компоненты» — классический список).
- Обновление = запустить новый `Setup.exe` поверх старого (Inno Setup перезапишет файлы, деинсталлятор обновится).
- Размер установщика большой (~100 МБ) из-за .NET и WinUI.

## Хочу, чтобы приложение появилось в Microsoft Store

Тогда нужен **MSIX**, а не Inno Setup. Это отдельная история — потребуется сертификат (самоподписанный для теста, публичный CA для Store) и настройка `Package.appxmanifest`. Скажите, если нужно — сгенерирую отдельный комплект.

## Итог

1. Установите Inno Setup 6 (https://jrsoftware.org/isdl.php).
2. Двойной клик по `build-installer.bat`.
3. Получите `installer-output\DeviceIdentifiersViewerSetup.exe`.
4. Этот файл — ваш установщик. Двойной клик → приложение установится.

