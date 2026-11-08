# Device Identifiers Viewer (WinUI 3)

Приложение для **просмотра** идентификаторов устройства с экспортом отчёта в TXT/JSON.

> ⚠️ **Это НЕ HWID-спуфер.** Приложение только читает данные. Оно не меняет SMBIOS, серийники, CPU ID и прочие аппаратные идентификаторы — из user-space это физически невозможно.

## Структура (плоская, все файлы в одной папке)

```

DeviceIdentifiersViewer.sln           ← открывать этот
DeviceIdentifiersViewer.csproj        ← лежит рядом с .sln
app.manifest
App.xaml / App.xaml.cs
MainWindow.xaml / MainWindow.xaml.cs
IdentifierItem.cs
IdentifierCollector.cs
ReportExporter.cs
launchSettings.json
README.md
LICENSE
gitignore.txt                          ← переименуйте в .gitignore если нужно

```

## Сборка

1. Распакуйте ZIP **целиком** (правый клик → «Извлечь всё…»).
2. Двойной клик по `DeviceIdentifiersViewer.sln`.
3. Вверху VS: **`Debug`** + **`x64`**.
4. **Ctrl+Shift+B** → сборка.
5. **F5** → запуск.

Ожидаемый результат: ок
