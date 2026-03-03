<p align="center">
  <img alt="Space Station 14" width="600" src="Resources/Textures/Logo/logo.png" />
</p>

<div class="header" align="center">

[![Discord](https://img.shields.io/discord/1243455873989349448?style=for-the-badge&logo=discord&logoColor=white&label=Discord&color=%237289da)](https://discord.gg/NUCt8bm5JJ)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge)](https://dotnet.microsoft.com/)
[![Steam](https://img.shields.io/badge/Steam-SS14-1b2838?style=for-the-badge&logo=steam&logoColor=white)](https://store.steampowered.com/app/1255460/Space_Station_14/)
[![Client](https://img.shields.io/badge/Client-Download-0078D7?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAxNmwtNS01aDMuNVY0aDNWMTFIMTd6TTUgMTh2Mmgxdjh6Ii8+PC9zdmc+)](https://spacestation14.io/about/nightlies/)
[![GitHub Stars](https://img.shields.io/github/stars/ss14-art/space-station-14?style=for-the-badge&logo=github&logoColor=white&color=181717)](https://github.com/ss14-art/space-station-14)

# Space Station 14

![Commit Activity](https://img.shields.io/github/commit-activity/y/ss14-art/space-station-14?style=for-the-badge&logo=github&logoColor=white&color=181717)
![Issues](https://img.shields.io/github/issues/ss14-art/space-station-14?style=for-the-badge&logo=github&logoColor=white&color=181717)
![Closed PRs](https://img.shields.io/github/issues-pr-closed/ss14-art/space-station-14?style=for-the-badge&logo=github&logoColor=white&color=181717)

</div>

---

## 🇷🇺 О проекте / 🇬🇧 About the project

**🇷🇺 Русский**
**Open Space** — это проект с открытым исходным кодом (форк проекта STARLIGHT), нацеленный на создание уникальных механик и приятной игровой атмосферы в игре Space Station 14.
Это игра о выживании на космической станции, где происходят постоянные столкновения между экипажем и антагонистами, стремящимися помешать персоналу достичь своих целей.

**🇬🇧 English**
**Open Space** is an open-source project (a fork of the STARLIGHT project) aimed at creating unique mechanics and a pleasant game atmosphere in Space Station 14.
It is a game about survival on a space station featuring constant confrontations between the crew and antagonists designed to prevent the crew from achieving their goals.

---

## 📚 Документация / Documentation

**🇷🇺** У Space Station 14 есть [сайт с документацией](https://docs.spacestation14.io/) по контенту, движку, геймдизайну и многому другому. Мы также предоставляем множество ресурсов для новых разработчиков.

**🇬🇧** Space Station 14 has a [docs site](https://docs.spacestation14.io/) containing documentation on SS14's content, engine, game design, and more. We also have lots of resources for new contributors to the project.

---

## 🚀 Запуск сборки локально / Building Locally

### Требования / Requirements

- **Git** — [скачать / download](https://git-scm.com/downloads)
- **.NET SDK 10.0 или выше / or higher** — [скачать / download](https://dotnet.microsoft.com/download/dotnet/10.0)

### 🍃 Windows
```bat
# 1. Клонируйте репозиторий
git clone https://github.com/ss14-art/space-station-14.git
cd space-station-14

# 2. Загрузите движок
git submodule update --init --recursive

# 3. Запустите сервер
runserver.bat

# 4. Запустите клиент (в отдельном окне)
runclient.bat
```

**Готово!** Подключитесь к **localhost** в клиенте и играйте.

> Для Release-сборки используйте `runserver-Release.bat` и `runclient-Release.bat`

### 🐧 Linux / macOS
```sh
# 1. Клонируйте репозиторий
git clone https://github.com/ss14-art/space-station-14.git
cd space-station-14

# 2. Загрузите движок
git submodule update --init --recursive

# 3. Запустите сервер
chmod +x runserver.sh
./runserver.sh

# 4. Запустите клиент (в отдельном терминале)
chmod +x runclient.sh
./runclient.sh
```

**Готово!** Подключитесь к **localhost** в клиенте и играйте.

> Для Release-сборки используйте `runserver-Release.sh` и `runclient-Release.sh`

---

## 🛠 Участие в разработке / Community & Contributing

**🇷🇺** Мы рады любой помощи в развитии проекта! Пожалуйста, ознакомьтесь с нашими правилами перед тем, как предлагать свои изменения.
**🇬🇧** We welcome any help in developing the project! Please read our guidelines before submitting your changes.

* 📖 [**Как внести вклад (Contributing)**](CONTRIBUTING.md) — Правила создания Pull Request'ов и требования к коду.
* ⚖️ [**Кодекс поведения (Code of Conduct)**](CODE_OF_CONDUCT.md) — Правила общения в нашем сообществе.
* 🛡️ [**Политика безопасности (Security Policy)**](SECURITY.md) — Как сообщить о критических уязвимостях и эксплойтах.

---

## ⚖️ Лицензия / License

> [!CAUTION]
> **🇷🇺 ВНИМАНИЕ:** Код репозитория имеет комбинированное лицензирование. Оригинальный код Space Wizards Federation лицензирован под MIT. Наши собственные разработки и изменения подчиняются Пользовательской лицензии (Project License) и требуют подписания CLA.
> 
> **🇬🇧 CAUTION:** The repository code is under a combined license model. The original Space Wizards Federation code is licensed under MIT. Our custom additions and changes are governed by the Project License and require signing a CLA.

### Нажмите на раздел для подробностей / Click each section for further information

<details>
<summary><b> Project License </b></summary>
<br>

![Project License](https://img.shields.io/badge/License-Project_License-blue?style=for-the-badge)

> **🇷🇺** Все изменения после коммита `efea656dd33f6296228a5d31be8ffc9f179f4f17` регулируются условиями [LICENSE.TXT](LICENSE.TXT). Коммерческое использование и несанкционированный публичный хостинг запрещены.
> 
> **🇬🇧** All changes after commit `efea656dd33f6296228a5d31be8ffc9f179f4f17` are governed by the terms in [LICENSE.TXT](LICENSE.TXT). Commercial use and unauthorized public hosting are prohibited.
</details>

<details>
<summary><b> CLA </b></summary>
<br>

![CLA](https://img.shields.io/badge/Agreement-CLA-orange?style=for-the-badge)

> **🇷🇺** Лицензионное соглашение контрибьютора. Создавая Pull Request в этот репозиторий, вы автоматически соглашаетесь с условиями передачи кода организации ss14-art, описанными в [CLA.TXT](CLA.TXT).
> 
> **🇬🇧** Contributor License Agreement. By submitting a Pull Request to this repository, you automatically agree to the terms of transferring code to the ss14-art organization, as described in [CLA.TXT](CLA.TXT).
</details>

<details>
<summary><b> MIT License </b></summary>
<br>

![MIT License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

> **🇷🇺** Оригинальный код Space Wizards Federation, а также вся история коммитов до `efea656dd33f6296228a5d31be8ffc9f179f4f17` распространяются на условиях [MIT License](MIT.TXT).
> 
> **🇬🇧** The original Space Wizards Federation code, as well as all commit history prior to `efea656dd33f6296228a5d31be8ffc9f179f4f17`, are distributed under the terms of the [MIT License](MIT.TXT).
</details>

<details>
<summary><b> CC 3.0 BY-SA </b></summary>
<br>

![Creative Commons 3.0 BY-SA](https://img.shields.io/badge/License-CC_3.0_BY--SA-lightblue?style=for-the-badge)

> **🇷🇺** Все остальные ресурсы (Assets), не относящиеся к коду, включая иконки и звуковые файлы, лицензированы по лицензии [Creative Commons 3.0 BY-SA](https://creativecommons.org/licenses/by-sa/3.0/), если иное не указано в папке или файле.
> 
> **🇬🇧** All other non-code Assets, including icons and sound files, are licensed under the [Creative Commons 3.0 BY-SA](https://creativecommons.org/licenses/by-sa/3.0/) license unless otherwise noted in the folder or file.
</details>
