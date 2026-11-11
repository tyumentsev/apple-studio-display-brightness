# Apple Studio Display Brightness

Небольшой регулятор аппаратной яркости **Apple Studio Display для Windows**.
Ползунок, ввод процентов и кнопки **−5% / +5%** в простом русском интерфейсе.

Приложение меняет подсветку монитора через USB HID и считывает значение обратно,
чтобы подтвердить изменение. При запуске оно показывает текущую яркость, не изменяя её.

## Скачать и установить

**[Скачать установщик версии 1.0.0](https://github.com/tyumentsev/apple-studio-display-brightness/releases/download/v1.0.0/AppleDisplayBrightness-1.0.0-Setup.exe)**

Все файлы: **[GitHub Releases](https://github.com/tyumentsev/apple-studio-display-brightness/releases/latest)**.

- **Setup.exe** — установка для текущего пользователя, ярлыки и удаление через «Установленные приложения». Права администратора не требуются.
- **Portable.zip** — распакуйте архив в отдельную папку и запустите `AppleDisplayBrightness.exe`.
- **SHA256SUMS.txt** — контрольные суммы файлов выпуска.

Установщик размещает приложение в `%LOCALAPPDATA%\Programs\AppleDisplayBrightness`.
Приложение и установщик пока не подписаны сертификатом разработчика.

## Использование

1. Подключите Studio Display к компьютеру кабелем USB-C / Thunderbolt с передачей данных.
2. Откройте «Яркость Apple Studio Display».
3. Передвиньте ползунок, введите число от 1 до 100 или нажмите −5% / +5%.
4. Сообщение «Яркость … применена» означает, что монитор подтвердил новое значение.

После переподключения монитора нажмите **«Обновить»**. Закрытие окна не возвращает прежнюю яркость.

Проценты — это шкала управления подсветкой: 100% соответствует аппаратному значению 60000.
Они не обозначают измеренную яркость в нитах.

## Совместимость

- Windows 10 / 11 и .NET Framework 4.8.
- Один Apple Studio Display с USB ID `05AC:1114`, интерфейсом `MI_07`.
- На подключённом Studio Display проверены чтение уровня и изменение 100 → 75 → 60%, а также кнопки 60 → 65 → 60%.
- При нескольких подходящих мониторах приложение отказывается выбирать один произвольно.
- Другие Apple-мониторы и Pro Display XDR в этой версии не поддерживаются.

Приложение работает локально, не обращается в сеть, не устанавливает драйверы и не добавляет себя в автозагрузку.

## Сборка из исходников

Требуется Windows с .NET Framework 4.8. Для сборки установщика дополнительно нужен
[Inno Setup](https://jrsoftware.org/isinfo.php); выпуск 1.0.0 собран с Inno Setup 7.1.1.

Только приложение:

```bat
build.cmd
```

Приложение и установщик:

```bat
build.cmd "C:\path\to\Inno Setup\ISCC.exe"
```

Результаты находятся в `dist\`. Visual Studio и NuGet-пакеты не требуются.

## Устройство проекта

- `src/Program.cs` — интерфейс WinForms.
- `src/StudioDisplayBrightness.cs` — поиск устройства и управление USB HID через системные Windows API.
- `src/app.manifest` — запуск с обычными правами и поддержка DPI.
- `installer/setup.iss` — установщик Inno Setup.

## Лицензия и благодарности

MPL-2.0, полный текст в [LICENSE](LICENSE).
Управление USB HID адаптировано на C# по реализации
[sfjohnson/studio-brightness](https://github.com/sfjohnson/studio-brightness).
Подробности — в [NOTICE.md](NOTICE.md).

Исходный код соответствующего выпуска доступен по
[тегу v1.0.0](https://github.com/tyumentsev/apple-studio-display-brightness/tree/v1.0.0).

Apple и Studio Display — товарные знаки Apple Inc. Это независимый проект.
