<div align="center">

# 🔥 Ashes of Disaster

**Host-only roles mod for Among Us** · **Хост-мод с ролями для Among Us**

![version](https://img.shields.io/badge/version-0.1.91-ff5555) ![loader](https://img.shields.io/badge/BepInEx-IL2CPP-blue) ![runtime](https://img.shields.io/badge/.NET-6.0-512bd4) ![AI](https://img.shields.io/badge/made%20with-AI-8a2be2)

[English](#english) · [Русский](#русский) · [Discord](#discord)

</div>

> 🤖 **AI disclosure / Сделано с помощью ИИ.** This mod was created with the help of an AI assistant (Claude by Anthropic), directed and tested by its human authors. / Мод создан с помощью ИИ (Claude от Anthropic) под руководством и с тестированием авторов-людей.

---

## English

Only the **host** needs the mod — other players join with a normal game and the host handles everything.

### Features
- 🎭 Custom roles in three groups: **Crew**, **Impostors**, **Neutrals**
- ⚙️ In-lobby settings menu with colored tabs (System, Game, Crew, Impostors, Neutrals)
- 💬 Chat commands that work for players **without** the mod
- 🌍 Russian and English, picked from the game language
- 🛡️ Host-side protections against kicks, performance optimizations (caching, frame limit)

### Roles (36)
**Crew — 14:** **Peasant** (Крестьянин), **Builder** (Строитель), **Psychologist** (Психолог), **Foreman** (Прораб), **Logger** (Логер), **Plumber** (Сантехник), **Dowser** (Лозоходец), **Navigator** (Штурман), **Sentry** (Часовой), **Statistician** (Статистик), **Intercessor** (Заступник), **Peacemaker** (Миротворец), **Eyewitness** (Очевидец), **Forensic** (Криминалист)

**Impostors — 11:** **Welder** (Сварщик), **Twin** (Близнец), **Shroomer** (Грибник), **Hound** (Ищейка), **Painter** (Маляр), **Spider** (Паук), **Minelayer** (Минёр), **Eclipser** (Затмитель), **Returner** (Возвращенец), **Lurer** (Приманщик), **Trapper** (Капканщик)

**Neutrals — 11:** **Smoker** (Курильщик), **Pinkman** (Пинкман), **Prankster** (Шутник), **Crow** (Ворон), **Defector** (Перебежчик), **Reflector** (Отражатель), **Cloud** (Облако), **Vigilante** (Виджилант), **Avenger** (Мститель), **Hypnotist** (Гипнотизёр), **Clockmaker** (Часовщик)

Full descriptions: [docs/ROLES.md](docs/ROLES.md) or `/r <role>` in game.

### Commands
| Command | Description |
|---|---|
| `/m` | Show your own role |
| `/r` | List all roles by group (`/r 1` crew, `/r 2` impostors, `/r 3` neutrals) |
| `/r <role>` | Role description |
| `/h` | Help (`/h <command>` for details) |
| `/id` | Player IDs |
| `/lang ru\|en` | Reply language |

### Install
1. Install [BepInEx (IL2CPP, 6.x)](https://builds.bepinex.dev/projects/bepinex_be) for your Among Us version.
2. Download `AshesOfDisaster.dll` from [Releases](../../releases), or build it yourself from source (see *Build from source*).
3. Put it into `Among Us/BepInEx/plugins/`.
4. Start the game, create a lobby — only you need the mod.

> Remove other overlapping mods (e.g. old host mods, AUnlocker) if you see conflicts.

### Build from source
Source is in [`src/`](src) (C#, .NET 6, BepInEx IL2CPP). Easiest way: install BepInEx 6, run the game once, then double-click **`Run all.bat`** (or `python run_all.py`) — the script finds the game, builds the mod and puts `AshesOfDisaster.dll` into `BepInEx/plugins`. Needs Python 3 and .NET SDK 8. More notes (Russian): [docs/NOTES.ru.md](docs/NOTES.ru.md).

---

## Русский

Мод нужен **только хосту** — остальные заходят с обычной игрой, всё обрабатывает хост.

### Возможности
- 🎭 Свои роли в трёх группах: **мирные**, **предатели**, **нейтралы**
- ⚙️ Меню настроек в лобби с цветными вкладками (Система, Игра, Мирные, Предатели, Нейтралы)
- 💬 Команды в чате, работающие и у игроков **без** мода
- 🌍 Русский и английский, язык берётся из языка игры
- 🛡️ Защита хоста от киков и оптимизации (кэши, лимит кадров)

### Роли (36)
**Мирные — 14:** **Крестьянин** (Peasant), **Строитель** (Builder), **Психолог** (Psychologist), **Прораб** (Foreman), **Логер** (Logger), **Сантехник** (Plumber), **Лозоходец** (Dowser), **Штурман** (Navigator), **Часовой** (Sentry), **Статистик** (Statistician), **Заступник** (Intercessor), **Миротворец** (Peacemaker), **Очевидец** (Eyewitness), **Криминалист** (Forensic)

**Предатели — 11:** **Сварщик** (Welder), **Близнец** (Twin), **Грибник** (Shroomer), **Ищейка** (Hound), **Маляр** (Painter), **Паук** (Spider), **Минёр** (Minelayer), **Затмитель** (Eclipser), **Возвращенец** (Returner), **Приманщик** (Lurer), **Капканщик** (Trapper)

**Нейтралы — 11:** **Курильщик** (Smoker), **Пинкман** (Pinkman), **Шутник** (Prankster), **Ворон** (Crow), **Перебежчик** (Defector), **Отражатель** (Reflector), **Облако** (Cloud), **Виджилант** (Vigilante), **Мститель** (Avenger), **Гипнотизёр** (Hypnotist), **Часовщик** (Clockmaker)

Полные описания: [docs/ROLES.md](docs/ROLES.md) или `/r <роль>` в игре.

### Команды
`/m` — своя роль · `/r` — список ролей (`/r 1` мирные, `/r 2` предатели, `/r 3` нейтралы) · `/r <роль>` — описание · `/h` — помощь · `/id` — ID игроков · `/lang ru|en` — язык ответов.

### Установка
1. Установите [BepInEx (IL2CPP, 6.x)](https://builds.bepinex.dev/projects/bepinex_be) под вашу версию Among Us.
2. Скачайте `AshesOfDisaster.dll` в [Releases](../../releases) или соберите сами из исходников (см. «Сборка из исходников»).
3. Положите в `Among Us/BepInEx/plugins/`.
4. Запустите игру и создайте лобби — мод нужен только вам.

> При конфликтах уберите другие похожие моды (старые хост-моды, AUnlocker).

### Сборка из исходников
Исходный код — в [`src/`](src) (C#, .NET 6, BepInEx IL2CPP). Установите BepInEx 6, один раз запустите игру и дважды кликните **`Run all.bat`** (или `python run_all.py`): скрипт найдёт игру, соберёт мод и положит `AshesOfDisaster.dll` в `BepInEx/plugins`. Нужны Python 3 и .NET SDK 8. Подробности: [docs/NOTES.ru.md](docs/NOTES.ru.md).

---

## Credits / Авторы
- **niko** — author / автор
- **Акакиш (Akakish)** — co-author / соавтор
- **Claude (Anthropic)** — AI assistant that wrote and debugged the code / ИИ-помощник, писавший и отлаживавший код

## Discord
Join the community: **DISCORD_INVITE_LINK** — bug reports, suggestions, lobbies. / Баг-репорты, идеи, поиск игр.

## Disclaimer
Unofficial fan project, not affiliated with or endorsed by Innersloth. Among Us is a trademark of Innersloth LLC. Use at your own risk; modded hosts can be flagged by anti-cheat on official servers.
