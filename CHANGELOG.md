# Changelog / Список изменений

## 3.45
**EN**
- Removed several roles and everything tied to them.
- Freeman carrying the crystal and Hobbyhorse in a gallop can no longer pass through walls; smoother slowdown for Freeman and a steadier speed boost for Hobbyhorse.
- The host no longer gets pop-up notices about kills.

**RU**
- Убрано несколько ролей и всё, что с ними связано.
- Фримен с кристаллом и Хобихорсер в галопе больше не проходят сквозь стены; Фримен замедляется плавнее, ускорение Хобихорсера стало ровнее.
- Хосту больше не показываются всплывающие сообщения об убийствах.

## 3.44
**EN**
- Auto test now starts in practice mode as soon as the map is loaded (the practice room reports the lobby state, so it never started before) and the mod no longer resets roles while the test runs. It logs why it is waiting every 20 s.

**RU**
- Автотест теперь стартует в практике, как только загрузилась карта (практика сообщает состояние «лобби», поэтому раньше он не запускался), а мод не сбрасывает роли во время теста. Каждые 20 секунд пишет в лог, чего ждёт.

## 3.43
**EN**
- Fixed duplicate role ids found by the self-test: the second Lurer is now **Beguiler** (Заманщик) and the second Alarmist is now **Alarmer** (Тревожник), so each role has its own code and settings.

**RU**
- Исправлены дубли ролей, найденные самопроверкой: второй Приманщик теперь **Заманщик** (Beguiler), а второй Паникёр теперь **Тревожник** (Alarmer), у каждой роли свой код и свои настройки.

## 3.42
**EN**
- Build fix: missing `using` lines in SelfTest, RoomCode and NeutralMask.

**RU**
- Исправление сборки: не хватало строк `using` в SelfTest, RoomCode и NeutralMask.

## 3.41
**EN**
- Hobbyhorse looks like a horse: a picture made of coloured blocks under the nickname, shown to everyone. Config: `HorseCostume`, `HorseCostumeSize`, `HorseCostumeShift` (Experimental).

**RU**
- Хобихорсер выглядит как лошадь: картинка из цветных блоков под ником, её видят все. Настройки: `HorseCostume`, `HorseCostumeSize`, `HorseCostumeShift` (раздел Experimental).

## 3.40
**EN**
- New crew role **Hobbyhorse**: the pet starts a gallop (about twice the speed, stamina drains), knocks down players on the way, winded horse rests. 139 roles now.

**RU**
- Новая мирная роль **Хобихорсер**: питомец включает галоп (скорость примерно вдвое выше, силы тратятся), по пути сбивает игроков, выдохшийся конь отдыхает. Теперь 139 ролей.

## 3.39
**EN**
- New crew role **Newton**: an electric shield inside Electrical, the pet outside switches the mode, the pet inside completes tasks (yours and other crewmates in the room) or freezes everyone in the room and fixes the lights. 138 roles now.

**RU**
- Новая мирная роль **Ньютон**: электрический щит в электрощитовой, питомец снаружи переключает режим, питомец внутри выполняет задания (твои и мирных в комнате) или замораживает всех в комнате и чинит свет. Теперь 138 ролей.

## 3.38
**EN**
- New crew role **Bot**: a bot completes your tasks for you, one about every 50 seconds. 137 roles now.

**RU**
- Новая мирная роль **Бот**: бот сам выполняет твои задания, примерно одно за 50 секунд. Теперь 137 ролей.

## 3.37
**EN**
- Build fix: the self-test log now subscribes to the mod's own log source instead of implementing ILogListener (that interface differs between BepInEx builds). Only the mod's own warnings and errors are logged.

**RU**
- Исправление сборки: лог самопроверки теперь подписывается на собственный источник логов мода, а не реализует ILogListener (этот интерфейс отличается в разных сборках BepInEx). Пишутся только предупреждения и ошибки самого мода.

## 3.36
**EN**
- Room code sharing: the same code cannot be sent twice within an hour, at least 30 s between sends, at most 6 sends per hour. The new `/share` command shows whether sharing is on, whether the webhook address is set, the last sent code and the sends left.

**RU**
- Отправка кода комнаты: один и тот же код нельзя отправить дважды в течение часа, между отправками минимум 30 с, не больше 6 отправок в час. Новая команда `/share` показывает, включена ли отправка, указан ли адрес вебхука, последний отправленный код и сколько отправок осталось.

## 3.35
**EN**
- Room code sharing is now a host choice: the System tab of the notebook has "Send the room code to the mod Discord", off by default. Turn it on at any time and the code is sent at once. Address: `AshesOfDisaster.secrets.txt` or the `RoomShareUrl` config entry.

**RU**
- Отправка кода комнаты теперь выбор хоста: во вкладке «Система» ноутбука есть пункт «Отправить код комнаты в Discord мода», по умолчанию выключен. Включи в любой момент, и код сразу уйдёт. Адрес: `AshesOfDisaster.secrets.txt` или настройка `RoomShareUrl`.

## 3.34
**EN**
- Discord webhooks are no longer in the source code: they are read from `BepInEx/config/AshesOfDisaster.secrets.txt`. Without that file nothing is sent.

**RU**
- Вебхуков Discord больше нет в исходниках: они читаются из файла `BepInEx/config/AshesOfDisaster.secrets.txt`. Без этого файла ничего не отправляется.

## 3.33
**EN**
- Complaints go to the built-in Discord webhook when `DiscordWebhook` is empty. New: when you open an online room the mod posts its code to Discord by itself (`RoomCodeToDiscord`).

**RU**
- Жалобы уходят во встроенный вебхук Discord, если `DiscordWebhook` пуст. Новое: когда ты создаёшь онлайн-комнату, мод сам отправляет её код в Discord (`RoomCodeToDiscord`).

## 3.32
**EN**
- Reports: the Discord request now has a User-Agent and a timeout, and when Discord refuses a report the answer code and text are written to the log as a warning (and to the self-test log).

**RU**
- Жалобы: у запроса в Discord теперь есть User-Agent и таймаут, а если Discord отказал, код и текст ответа пишутся в лог как предупреждение (и в лог самопроверки).

## 3.31
**EN**
- Auto test: in practice mode the mod gives the host every role in turn, uses its ability twice, runs the kill, vent, morph and meeting hooks and every safe command, and writes all hints, warnings and errors to `BepInEx/AshesOfDisaster_selftest.log`. Config: `AutoTestInPractice`.

**RU**
- Автотест: в режиме практики мод по очереди выдаёт хосту каждую роль, дважды применяет её способность, запускает хуки убийства, люка, превращения и собрания и все безопасные команды, и пишет все подсказки, предупреждения и ошибки в `BepInEx/AshesOfDisaster_selftest.log`. Настройка: `AutoTestInPractice`.

## 3.30
**EN**
- Self-test: on every launch the mod checks itself (game API, patches, all role data) and at every game start tests the status of each given role. All warnings and errors plus the test results are written to `BepInEx/AshesOfDisaster_selftest.log`.

**RU**
- Самопроверка: при каждом запуске мод проверяет себя (API игры, патчи, данные всех ролей), а в начале каждой игры проверяет статус каждой выданной роли. Все предупреждения, ошибки и итоги проверки пишутся в `BepInEx/AshesOfDisaster_selftest.log`.

## 3.29
**EN**
- Fat: the size and nickname changes of 3.27 and 3.28 are reverted to the 3.26 values.

**RU**
- Толстяк: изменения размера и ника из 3.27 и 3.28 отменены, значения как в 3.26.

## 3.28
**EN**
- Fat: the nickname text above the head is much larger (up to 450%), it is sent to every player, so everyone sees it even without the model size.

**RU**
- Толстяк: ник над головой стал намного крупнее (до 450%), он отправляется всем игрокам, поэтому его видят все, даже без увеличения модели.

## 3.27
**EN**
- Fat: the model grows about 2.5 times faster with fat and the size limit is now up to 500% (300% by default).

**RU**
- Толстяк: модель растёт примерно в 2,5 раза быстрее от жира, предел размера теперь до 500% (по умолчанию 300%).

## 3.26
**EN**
- Neutral killer kills now look like the victim killing himself, so the killer is not shown. Config: `NeutralKillsLookLikeSuicide` (Experimental), turn it off if the host gets kicked.

**RU**
- Убийства нейтралов-убийц теперь выглядят как самоубийство жертвы, убийцу не видно. Настройка: `NeutralKillsLookLikeSuicide` (раздел Experimental), выключи, если хоста кикает.

## 3.25
**EN**
- New config switch `HideImpostors` (Experimental) to turn the hidden red names off.

**RU**
- Новая настройка `HideImpostors` (раздел Experimental): выключает скрытие красных имён предателей.

## 3.25
**EN**
- Fixed wrong nicknames: the mod kept an old nickname per player slot and put it on other players after players changed. Names are now always taken from the current player and the cache is cleared when the room changes.

**RU**
- Исправлены чужие ники: мод помнил старый ник для каждого номера игрока и подставлял его другим после смены игроков. Теперь имя всегда берётся у текущего игрока, а при смене комнаты память очищается.

## 3.24
**EN**
- Nobody sees who the impostors are: red names are hidden for impostors, neutral killers and the host alike (in the game world; meeting names are not changed).

**RU**
- Теперь никому не видно, кто предатель: красные имена скрыты у предателей, нейтралов-убийц и хоста (в мире игры; имена на собрании не меняются).

## 3.23
**EN**
- Neutral killers are no longer shown to impostors (and the other way round): the names of neutral killers are not red for impostors and the red names of impostors are hidden from neutral killers. The host's own screen is not affected.

**RU**
- Нейтралов-убийц больше не видно предателям (и наоборот): имена нейтралов не красные у предателей, а красные имена предателей скрыты у нейтралов-убийц. Экран самого хоста не меняется.

## 3.22
**EN**
- 136 roles now. New neutral killer **God**: faith that grows over time, nine powers reshuffled after every meeting, unknown mode, prayer, faith costs, hubris penalty, mode switched by a vent, targeted powers through the shapeshift button, pet for the rest, own shield that returns.

**RU**
- Теперь 136 ролей. Новый нейтрал-убийца **Бог**: вера растёт со временем, девять сил с перемешиванием после каждого собрания, неизвестный режим, молитва, цены веры, расплата за гордыню, режим переключается люком, силы с целью через кнопку превращения, остальные питомцем, свой щит возвращается.

## 3.21
**EN**
- 135 roles now (+15), five for each team.
- Crew: Switcher, Taskmaster, Mourner, Coroner, Dispatcher.
- Impostors: Tripper, Eraser, Lockpicker, Dazer, Changeling.
- Neutral killers: Digger, Liquidator, Duelist, Arsonist, Mercenary.

**RU**
- Теперь 135 ролей (+15), по пять для каждой команды.
- Мирные: Подменщик, Табельщик, Плакальщик, Коронер, Диспетчер.
- Предатели: Подножник, Стиратель, Взломщик, Дурман, Подменыш.
- Нейтралы-убийцы: Копатель, Ликвидатор, Дуэлянт, Поджигатель, Наёмник.

## 3.20
**EN**
- 120 roles now (+44): crew, impostors, neutral killers, mystics, ghost roles. New vent/trap roles: Ventsleuth, Sealer, Snarer, Ventbomber, Pouncer, Ventecho and more.
- New **Freeman** neutral: crowbar abilities (shield, doors, kill), switched by a vent, used with the pet, crystal quest.
- **Witch** and **Alchemist** reworked, **Doctor** removed.
- Clearer status above the nickname: role, team, goal, current state.
- Role card for every player at game start (goal, how to use, tip) and the `/tip` command.
- Clearer commands: `/h` by groups, Russian aliases (`/роль`, `/роли`, `/совет`).
- Map markers for placed things (snares, mines, runes, bombs, webs, clouds...), visible to everyone. Can be turned off: `MapMarkersEnabled`, `MarkersVisibleToAll`.
- Fixes: vent roles no longer get stuck in a vent, long chat messages are split (host kick fix), notebook buttons shifted left.
- Removed: lobby music, Mafia, mini-games mode, supply crates, in-game Discord updater. Lobby mini-games stay.

**RU**
- Теперь 120 ролей (+44): мирные, предатели, нейтралы-убийцы, мистики, призраки. Новые роли с люками и ловушками: Следопыт люков, Запечатыватель, Силковщик, Люкобомбист, Прыгун, Эхо люков и другие.
- Новый нейтрал **Фримен**: способности лома (щит, двери, убийство), переключается люком, применяется питомцем, квест с кристаллами.
- **Ведьма** и **Алхимик** переделаны, **Доктор** убран.
- Понятный статус над ником: роль, команда, цель, текущее состояние.
- Карточка роли каждому игроку в начале игры (цель, как пользоваться, совет) и команда `/tip`.
- Понятные команды: `/h` по группам, русские алиасы (`/роль`, `/роли`, `/совет`).
- Значки на карте для поставленных предметов (силки, мины, руны, бомбы, тучи...), видны всем. Выключается: `MapMarkersEnabled`, `MarkersVisibleToAll`.
- Исправления: роли с люками больше не застревают в люке, длинные сообщения чата режутся (фикс кика хоста), кнопки в ноутбуке сдвинуты влево.
- Убрано: музыка в лобби, мафия, режим мини-игр, ящики снабжения, обновление через Discord в игре. Мини-игры в лобби остались.
