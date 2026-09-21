# WindowDeck

[**English**](#english) | [**Polski**](#polski)

---

## English

### Fast Window Switcher for Windows 11

**Find the exact open window you need in seconds.**

WindowDeck is a lightweight, event-driven **Windows 11 window switcher and open-window search utility** for people who work with many applications, documents, and monitors.

It gives you a searchable list of open windows, optional grouping by application, monitor indicators, and direct actions for the exact selected window — activate, minimize, or close.

WindowDeck is especially useful when many open windows have similar names or are spread across multiple screens.

> **About this project**
>
> I am not a software developer. WindowDeck started as a practical idea for solving a problem I had in everyday work.
>
> I designed the concept, requirements, workflow, and UI, and tested and refined the application iteratively. The code was created with the help of **ChatGPT** and **OpenAI Codex**.

### Download

Download the latest version from:

**[GitHub Releases](https://github.com/BsB92/WindowDeck/releases/latest)**

WindowDeck is distributed as a **single, self-contained `WindowDeck.exe`**.

No installer is required and no separate .NET Runtime installation is needed.

#### Windows SmartScreen

WindowDeck is currently distributed without a code-signing certificate.

Because of this, Windows SmartScreen may show an **"Unknown publisher"** warning when you run `WindowDeck.exe` for the first time.

This warning does not mean that Windows detected malware. It means the application is unsigned and does not yet have an established SmartScreen reputation.

If you downloaded WindowDeck from the official GitHub Releases page, you can verify the file source before running it.

#### Verify the download

SHA-256 for `WindowDeck.exe` version 1.0.0:

```text
967af1660e0a1207586745472b93ce13f4dbb5a980059f1ed8741a76b0adeff4
```

You can verify the downloaded file in PowerShell:

```powershell
Get-FileHash .\WindowDeck.exe -Algorithm SHA256
```

The resulting hash should match the value above.

### Features

- Quickly find and switch between open windows
- Search open windows by title
- Optional grouping by application
- Screen indicators such as `[ 1 ]`, `[ 2 ]`, `[ 3 ]`, etc.
- Exact-window activation
- Minimize a specific window
- Send a normal close request to a specific window
- Configurable global hotkey
- Opens on the screen containing the mouse cursor
- System tray integration
- Start with Windows
- Start minimized to tray
- Application icons
- Optional minimized-window display
- System / Light / Dark appearance
- English and Polish interface
- Automatic language selection with the **System** option
- Built-in Help and About windows
- Single running application instance

WindowDeck uses event-driven window tracking and does not continuously poll for open windows.

### Quick start

1. Download `WindowDeck.exe` from the latest GitHub Release.
2. Run `WindowDeck.exe`.
3. WindowDeck starts in the **system tray**, so no main window may appear immediately.
4. Open WindowDeck by:
   - clicking the WindowDeck tray icon near the clock, or
   - using the default global shortcut **Win + `**
5. Search for a window or select one from the list.
6. Click a window to activate it.

The global shortcut can be changed in **Settings**.

WindowDeck continues running in the system tray when the panel is hidden.

To close WindowDeck completely, use:

**Tray icon → Exit**

### Window actions

Each WindowDeck row represents one specific Windows window.

- Click the window title to activate it.
- `—` minimizes that specific window.
- `×` sends a normal close request to that specific window.

WindowDeck does not force-kill applications.

If an application needs confirmation before closing, its normal **Save / Don't Save / Cancel** dialog remains in control.

### Multiple screens

WindowDeck shows the screen containing each window using indicators such as:

- `[ 1 ]`
- `[ 2 ]`
- `[ 3 ]`
- and so on.

When WindowDeck is opened using the global hotkey, it appears on the screen containing the mouse cursor.

### Languages

WindowDeck currently supports:

- **System**
- **English**
- **Polski**

With **System** selected:

- Polish Windows UI → Polish
- other Windows UI languages → English

The language can be changed in Settings without restarting WindowDeck.

### Appearance

Available appearance modes:

- **System**
- **Light**
- **Dark**

Language and appearance settings are independent.

### Privacy and network access

WindowDeck does not include:

- telemetry
- analytics
- user accounts
- cloud services
- automatic update checks
- automatic network communication

WindowDeck runs locally on your computer.

### System requirements

The published release is:

- designed for Windows 11
- Windows x64
- self-contained
- distributed as a single executable

A separate .NET Runtime installation is not required for the published release.

### Build from source

Requirements:

- .NET 10 SDK
- Windows desktop development workload
- Windows
- Visual Studio or the .NET CLI

Clone the repository and build:

```powershell
dotnet build WindowDeck.sln -c Release
```

You can also open `WindowDeck.sln` in Visual Studio and run the application with **F5**.

### Create a release build

The repository contains a publish profile for the Windows x64 release.

Run:

```powershell
dotnet publish WindowDeck.csproj -p:PublishProfile=WinX64
```

The output is created in:

```text
artifacts\WindowDeck-1.0.0-win-x64
```

The release configuration uses:

- Release configuration
- `win-x64`
- self-contained deployment
- single-file publishing
- trimming disabled
- NativeAOT disabled

### Project documentation

Detailed product behavior and technical requirements are documented in:

[`SPEC.md`](SPEC.md)

### License

WindowDeck is open source under the [MIT License](LICENSE).

Copyright (c) 2026 **::BsB!::**

---

## Polski

### Szybki przełącznik okien dla Windows 11

**Znajdź dokładnie to otwarte okno, którego potrzebujesz — w kilka sekund.**

WindowDeck to lekki, działający zdarzeniowo **przełącznik okien i narzędzie do wyszukiwania otwartych okien w Windows 11**, stworzone z myślą o osobach pracujących z wieloma aplikacjami, dokumentami i monitorami.

Program wyświetla przeszukiwalną listę otwartych okien, umożliwia opcjonalne grupowanie według aplikacji, pokazuje oznaczenia monitorów i pozwala wykonywać operacje bezpośrednio na wybranym oknie — aktywować je, zminimalizować lub zamknąć.

WindowDeck jest szczególnie przydatny, gdy wiele otwartych okien ma podobne nazwy albo jest rozmieszczonych na kilku ekranach.

> **O projekcie**
>
> Nie jestem programistą. WindowDeck powstał jako praktyczny pomysł na rozwiązanie problemu, z którym spotykałem się w codziennej pracy.
>
> Zaprojektowałem koncepcję, wymagania, sposób działania i interfejs użytkownika, a następnie iteracyjnie testowałem i dopracowywałem aplikację. Kod powstał przy wsparciu **ChatGPT** i **OpenAI Codex**.

### Pobieranie

Najnowszą wersję można pobrać tutaj:

**[GitHub Releases](https://github.com/BsB92/WindowDeck/releases/latest)**

WindowDeck jest udostępniany jako **pojedynczy, samodzielny plik `WindowDeck.exe`**.

Instalator nie jest wymagany. Nie trzeba też osobno instalować środowiska .NET Runtime.

#### Windows SmartScreen

WindowDeck jest obecnie udostępniany bez certyfikatu podpisu kodu.

Z tego powodu Windows SmartScreen może przy pierwszym uruchomieniu `WindowDeck.exe` wyświetlić ostrzeżenie **„Nieznany wydawca”**.

Takie ostrzeżenie nie oznacza, że Windows wykrył złośliwe oprogramowanie. Oznacza jedynie, że aplikacja nie jest podpisana cyfrowo i nie ma jeszcze wyrobionej reputacji w SmartScreen.

Jeżeli pobrałeś WindowDeck z oficjalnej strony GitHub Releases, przed uruchomieniem możesz zweryfikować pochodzenie pliku.

#### Weryfikacja pobranego pliku

SHA-256 dla `WindowDeck.exe` w wersji 1.0.0:

```text
967af1660e0a1207586745472b93ce13f4dbb5a980059f1ed8741a76b0adeff4
```

Pobrany plik możesz sprawdzić w PowerShell:

```powershell
Get-FileHash .\WindowDeck.exe -Algorithm SHA256
```

Otrzymany skrót powinien być identyczny z wartością podaną powyżej.

### Funkcje

- Szybkie wyszukiwanie i przełączanie między otwartymi oknami
- Wyszukiwanie otwartych okien po tytule
- Opcjonalne grupowanie według aplikacji
- Oznaczenia ekranów, np. `[ 1 ]`, `[ 2 ]`, `[ 3 ]` itd.
- Aktywacja dokładnie wybranego okna
- Minimalizowanie konkretnego okna
- Wysyłanie standardowego żądania zamknięcia do konkretnego okna
- Konfigurowalny globalny skrót klawiaturowy
- Otwieranie na ekranie, na którym znajduje się kursor myszy
- Integracja z zasobnikiem systemowym
- Uruchamianie razem z systemem Windows
- Uruchamianie zminimalizowane do zasobnika systemowego
- Ikony aplikacji
- Opcjonalne wyświetlanie zminimalizowanych okien
- Tryby wyglądu System / Jasny / Ciemny
- Interfejs w języku angielskim i polskim
- Automatyczny wybór języka przy ustawieniu **System**
- Wbudowane okna Pomoc i O programie
- Tylko jedna uruchomiona instancja aplikacji

WindowDeck śledzi okna w sposób zdarzeniowy i nie odpytuje systemu w sposób ciągły.

### Szybki start

1. Pobierz `WindowDeck.exe` z najnowszego wydania na GitHubie.
2. Uruchom `WindowDeck.exe`.
3. WindowDeck uruchamia się w **zasobniku systemowym**, dlatego główne okno może nie pojawić się od razu.
4. Otwórz WindowDeck:
   - klikając ikonę WindowDeck w zasobniku systemowym obok zegara albo
   - używając domyślnego globalnego skrótu **Win + `**
5. Wyszukaj okno lub wybierz je z listy.
6. Kliknij okno, aby je aktywować.

Globalny skrót można zmienić w **Ustawieniach**.

Po ukryciu panelu WindowDeck nadal działa w zasobniku systemowym.

Aby całkowicie zamknąć WindowDeck, użyj:

**Ikona w zasobniku → Zakończ**

### Operacje na oknach

Każdy wiersz w WindowDeck odpowiada jednemu konkretnemu oknu systemu Windows.

- Kliknięcie tytułu aktywuje dane okno.
- `—` minimalizuje dane okno.
- `×` wysyła standardowe żądanie zamknięcia do danego okna.

WindowDeck nie wymusza zakończenia procesu aplikacji.

Jeżeli aplikacja wymaga potwierdzenia przed zamknięciem, jej standardowe okno **Zapisz / Nie zapisuj / Anuluj** nadal działa normalnie.

### Wiele ekranów

WindowDeck pokazuje ekran, na którym znajduje się dane okno, za pomocą oznaczeń takich jak:

- `[ 1 ]`
- `[ 2 ]`
- `[ 3 ]`
- itd.

Po otwarciu WindowDeck globalnym skrótem program pojawia się na ekranie, na którym znajduje się kursor myszy.

### Języki

WindowDeck obsługuje obecnie:

- **System**
- **English**
- **Polski**

Przy wybranym ustawieniu **System**:

- polski interfejs Windows → Polski
- pozostałe języki interfejsu Windows → English

Język można zmienić w Ustawieniach bez ponownego uruchamiania WindowDeck.

### Wygląd

Dostępne tryby wyglądu:

- **System**
- **Jasny**
- **Ciemny**

Ustawienia języka i wyglądu są od siebie niezależne.

### Prywatność i dostęp do sieci

WindowDeck nie zawiera:

- telemetrii
- analityki
- kont użytkowników
- usług chmurowych
- automatycznego sprawdzania aktualizacji
- automatycznej komunikacji sieciowej

WindowDeck działa lokalnie na komputerze użytkownika.

### Wymagania systemowe

Opublikowana wersja jest:

- przeznaczona dla Windows 11
- przygotowana dla Windows x64
- samodzielna
- udostępniana jako pojedynczy plik wykonywalny

Dla opublikowanej wersji nie jest wymagana osobna instalacja .NET Runtime.

### Kompilowanie ze źródeł

Wymagania:

- .NET 10 SDK
- pakiet narzędzi do tworzenia aplikacji desktopowych dla Windows
- Windows
- Visual Studio lub .NET CLI

Sklonuj repozytorium i uruchom kompilację:

```powershell
dotnet build WindowDeck.sln -c Release
```

Możesz również otworzyć `WindowDeck.sln` w Visual Studio i uruchomić aplikację klawiszem **F5**.

### Tworzenie wersji wydaniowej

Repozytorium zawiera profil publikowania dla Windows x64.

Uruchom:

```powershell
dotnet publish WindowDeck.csproj -p:PublishProfile=WinX64
```

Pliki wynikowe są tworzone w:

```text
artifacts\WindowDeck-1.0.0-win-x64
```

Konfiguracja wydania wykorzystuje:

- konfigurację Release
- `win-x64`
- samodzielne wdrożenie
- publikowanie jako pojedynczy plik
- wyłączone przycinanie kodu
- wyłączone NativeAOT

### Dokumentacja projektu

Szczegółowe zachowanie produktu i wymagania techniczne są opisane w:

[`SPEC.md`](SPEC.md)

### Licencja

WindowDeck jest projektem open source udostępnianym na licencji [MIT](LICENSE).

Copyright (c) 2026 **::BsB!::**
