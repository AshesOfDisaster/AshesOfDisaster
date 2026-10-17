# Roles / Роли

Total / Всего: **70**. Descriptions are shown in game with `/r <role>`.


## Crew / Мирные

### Peasant / Крестьянин
- RU: Живой член экипажа. Один раз за игру может поставить себе защиту. Защита включается сама после нескольких выполненных заданий и отменяет первое нападение на него.
- EN: A living crewmate. The shield turns on by itself after a few completed tasks and cancels the first attack on him.

### Builder / Строитель
- RU: Член экипажа. Погладь питомца рядом с дверью — ближайшая дверь закрывается (как при саботаже двери), через несколько секунд она откроется сама. Дверь видят и слышат все игроки. Нужен питомец!
- EN: A crewmate. Pet your pet next to a door - the nearest door closes (like a door sabotage) and opens by itself after a few seconds. Everyone sees and hears the door. A pet is required!

### Psychologist / Психолог
- RU: Член экипажа. На собрании командой /ab ID проверяет психическое состояние игрока: 0-39 — спокоен, 40-59 — видел труп, 60-100 — убийца. Способность перезаряжается (по умолчанию 2 минуты), число проверок ограничено, а иногда проверка не удаётся.
- EN: A crewmate. In a meeting use /ab ID to check a player's mental state: 0-39 calm, 40-59 saw a body, 60-100 killer. The ability recharges (2 minutes by default), the number of checks is limited, and sometimes a check fails.

### Shieldbearer / Щитоносец
- RU: Член экипажа. Питомец рядом с игроком (до 3 м) выдаёт ему щит: первое нападение на него будет отменено. Число щитов и перезарядка настраиваются. Нужен питомец!
- EN: A crewmate. Pet next to a player (within 3 m) gives him a shield that cancels the first attack on him. Number of shields and cooldown are configurable. A pet is required!

### Foreman / Прораб
- RU: Член экипажа. Каждое выполненное им задание с настраиваемым шансом засчитывает одно задание случайному члену экипажа — команде проще победить по заданиям.
- EN: A crewmate. Each task he completes has a configurable chance to also complete one task of a random crewmate, helping the team win by tasks.

### Logger / Логер
- RU: Член экипажа. Читает журнал входов в комнаты на карте: питомец показывает последние записи («через сколько секунд кто вошёл в какую комнату»), а на каждом собрании приходят свежие. Часть записей приходит без имени (шанс настраивается). Нужен питомец!
- EN: A crewmate. Reads the room-entry log of the map: pet shows the latest entries (how many seconds ago who entered which room) and fresh ones arrive at every meeting. Some entries come without a name (chance configurable). A pet is required!

### Plumber / Сантехник
- RU: Член экипажа. Питомец чинит активный саботаж (свет, реактор, кислород, связь), а если саботажа нет — открывает все закрытые двери (в том числе запертые Сварщиком). Число починок и перезарядка настраиваются. Нужен питомец!
- EN: A crewmate. Pet fixes an active sabotage (lights, reactor, oxygen, comms), or opens all closed doors if there is none (including doors locked by the Welder). Number of repairs and cooldown are configurable. A pet is required!

### Dowser / Лозоходец
- RU: Член экипажа. Питомец показывает комнату и расстояние до ближайшего непоказанного трупа и сколько живых игроков рядом. Перезарядка и радиус поиска настраиваются. Нужен питомец!
- EN: A crewmate. Pet shows the room and distance of the nearest body and how many living players are nearby. Cooldown and search radius are configurable. A pet is required!

### Navigator / Штурман
- RU: Член экипажа. Питомец ставит метку, повторное поглаживание возвращает тебя к метке (перезарядка настраивается). Метка пропадает на собрании. Нужен питомец!
- EN: A crewmate. Pet sets a mark, petting again returns you to it (cooldown configurable). The mark vanishes at a meeting. A pet is required!

### Notary / Нотариус
- RU: Член экипажа. На собрании командой /ab ID узнаёт, сколько заданий выполнил игрок. Число проверок ограничено.
- EN: A crewmate. In a meeting /ab ID shows how many tasks a player has completed. The number of checks is limited.

### Cartographer / Картограф
- RU: Член экипажа. В начале каждого собрания узнаёт, сколько игроков находилось в каждой комнате (без имён).
- EN: A crewmate. At the start of each meeting learns how many players were in each room (no names).

### Archivist / Архивариус
- RU: Член экипажа. В начале каждого собрания получает архив: в какой комнате умер каждый, кто погиб с прошлого собрания.
- EN: A crewmate. At the start of each meeting receives an archive: in which room each player who died since the last meeting was killed.

### Sentry / Часовой
- RU: Член экипажа. Питомец ставит пост (растяжку): первый, кто пройдёт рядом, будет назван Часовому, пост исчезает. Посты пропадают на собрании. Нужен питомец!
- EN: A crewmate. Pet places a tripwire post: the first player to pass near it is named to the Sentry and the post vanishes. Posts vanish at a meeting. A pet is required!

### Statistician / Статистик
- RU: Член экипажа. На собрании командой /ab узнаёт, сколько предателей (для сервера) ещё живо. Число проверок ограничено.
- EN: A crewmate. In a meeting /ab shows how many impostors (as the server sees them) are still alive. The number of checks is limited.

### Marked / Меченый
- RU: Член экипажа. Если его убили, на следующем собрании всем мирным приходит имя убийцы — но с настраиваемым шансом имя ложное.
- EN: A crewmate. If he is killed, at the next meeting all crewmates get the name of the killer - but with a configurable chance the name is false.

### Dispatcher / Диспетчер
- RU: Член экипажа. Питомец показывает комнаты, где сейчас больше всего живых игроков. Перезарядка настраивается. Нужен питомец!
- EN: A crewmate. Pet shows the rooms with the most living players right now. Cooldown is configurable. A pet is required!

### Intercessor / Заступник
- RU: Член экипажа. Питомец рядом с игроком берёт его под защиту: если подопечного попытаются убить, нападение отменяется, а погибает сам Заступник. Подопечный один, новый заменяет прежнего. Метка пропадает на собрании. Нужен питомец!
- EN: A crewmate. Pet next to a player takes him under protection: if the ward is attacked the attack is cancelled and the Intercessor dies instead. One ward at a time. The ward is dropped at a meeting. A pet is required!

### Peacemaker / Миротворец
- RU: Член экипажа. Питомец создаёт вокруг него зону мира на несколько секунд: пока она действует, предатели рядом не могут никого убить. Число использований ограничено. Нужен питомец!
- EN: A crewmate. Pet creates a peace zone around him for a few seconds: while it lasts impostors nearby can't kill anyone. Number of uses is limited. A pet is required!

### Investigator / Следователь
- RU: Член экипажа. На собрании командой /ab ID узнаёт, в какой комнате находился игрок в момент последнего убийства. Число проверок ограничено.
- EN: A crewmate. In a meeting /ab ID shows which room a player was in at the moment of the last kill. The number of checks is limited.

### Wanderer / Бродяга
- RU: Член экипажа. Питомец переносит его к случайному живому игроку (это может оказаться и опасно). Перезарядка настраивается. Нужен питомец!
- EN: A crewmate. Pet teleports him to a random living player (which may be dangerous). Cooldown is configurable. A pet is required!

### Lucky / Везунчик
- RU: Член экипажа. Нападение на него с настраиваемым шансом проваливается. Число спасений за игру ограничено.
- EN: A crewmate. An attack on him fails with a configurable chance. The number of saves per game is limited.

### Eyewitness / Очевидец
- RU: Член экипажа. Если рядом с ним (в радиусе, по умолчанию 7 м) кого-то убили, он узнаёт имя убийцы — но с настраиваемым шансом имя ложное.
- EN: A crewmate. If someone is killed near him (within the radius, 7 m by default) he learns the killer's name - but with a configurable chance the name is false.

### Forensic / Криминалист
- RU: Член экипажа. Питомец рядом с трупом (до 2 м): узнаёт, сколько секунд назад тот погиб, и смывает краску Маляра, чтобы труп можно было доложить. Съеденное тело не вернуть. Число осмотров ограничено. Нужен питомец!
- EN: A crewmate. Pet next to a body (within 2 m): learns how many seconds ago it died and washes off the Painter's paint so the body can be reported. An eaten body can't be restored. Number of exams is limited. A pet is required!

### Aide / Помощник
- RU: Член экипажа. Питомец рядом с членом экипажа (до 3 м) засчитывает ему одно невыполненное задание. Число помощей и перезарядка настраиваются. Нужен питомец!
- EN: A crewmate. Pet next to a crewmate (within 3 m) completes one of his unfinished tasks. Number of helps and cooldown are configurable. A pet is required!


## Impostors / Предатели

### Welder / Сварщик
- RU: Предатель. Погладь питомца у двери — комната запирается и остаётся закрытой до собрания. Одновременно можно держать запертыми ограниченное число комнат (по умолчанию 1), столовую запереть нельзя. Нужен питомец!
- EN: Impostor. Pet your pet next to a door - the room is locked and stays closed until the meeting. Only a limited number of rooms can be locked at once (1 by default), the cafeteria can't be locked. A pet is required!

### Twin / Близнец
- RU: Предатель. Когда он убивает, вторым умирает ближайший к жертве игрок (в настраиваемом радиусе). Двойное убийство срабатывает не чаще, чем раз в перезарядку (по умолчанию 60 с); если у второго есть защита, она спасает его.
- EN: Impostor. When he kills, the player nearest to the victim (within a configurable radius) dies too. The double kill triggers no more often than the cooldown (60 s by default); if the second player has a shield, it saves him.

### Shroomer / Грибник
- RU: Предатель. Питомец запускает грибной саботаж на любой карте: на Фангле — настоящий, на остальных картах все живые игроки становятся серыми, а ники перемешиваются на несколько секунд. Число запусков и перезарядка настраиваются. Нужен питомец!
- EN: Impostor. Pet starts the mushroom sabotage on any map: the real one on Fungle, and on other maps all living players turn gray and their names are shuffled for a few seconds. Number of starts and cooldown are configurable. A pet is required!

### Hound / Ищейка
- RU: Предатель. Питомец показывает комнату и расстояние до ближайшего члена экипажа (перезарядка настраивается). Нужен питомец!
- EN: Impostor. Pet shows the room and distance to the nearest crewmate (cooldown configurable). A pet is required!

### Sealer / Запечатыватель
- RU: Предатель. После твоего убийства двери ближайшей комнаты закрываются на время. Ты сам тоже можешь оказаться внутри. Срабатывает не чаще, чем раз в перезарядку.
- EN: Impostor. After your kill the doors of the nearest room close for a while. You may be trapped too. Triggers no more often than the cooldown.

### Alarmist / Паникёр
- RU: Предатель. Питомец вызывает ложную тревогу: у всех мирных на несколько секунд мигает экран. Долгая перезарядка. Нужен питомец!
- EN: Impostor. Pet triggers a false alarm: all crewmates' screens flash for a few seconds. Long cooldown. A pet is required!

### Painter / Маляр
- RU: Предатель. Трупы его жертв закрашены: доложить о них нельзя, пока краска не стечёт (время настраивается).
- EN: Impostor. Bodies of his victims are painted over: they can't be reported until the paint runs off (configurable).

### Spider / Паук
- RU: Предатель. Погладь питомца у дверного проёма — там появится паутина. Член экипажа, наступивший на неё, застревает, а паук узнаёт об этом на собрании. Если убить застрявшего, паук съедает его целиком: тело нельзя доложить. Экипаж освобождает застрявшего, постояв рядом несколько секунд. Предатели и нейтралы проходят сквозь паутину (настройка). Нужен питомец!
- EN: Impostor. Pet your pet next to a doorway to place a web. A crewmate who steps on it gets stuck; the spider learns about it at the meeting. Killing the stuck player eats them whole: the body can't be reported. Crew frees the victim by standing nearby for a few seconds. Impostors and neutrals pass through webs (setting). A pet is required!

### Minelayer / Минёр
- RU: Предатель. Питомец ставит мину: первый член экипажа, наступивший на неё, погибает (щит спасает). Мины пропадают на собрании. Число мин настраивается. Нужен питомец!
- EN: Impostor. Pet places a mine: the first crewmate to step on it dies (a shield saves him). Mines vanish at a meeting. Number of mines is configurable. A pet is required!

### Shuffler / Тасовщик
- RU: Предатель. Питомец меняет местами двух случайных членов экипажа. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet swaps the positions of two random crewmates. Cooldown is configurable. A pet is required!

### Eclipser / Затмитель
- RU: Предатель. Питомец гасит свет (саботаж электричества), если сейчас нет другого саботажа. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet turns the lights off (electrical sabotage) if no other sabotage is active. Cooldown is configurable. A pet is required!

### Returner / Возвращенец
- RU: Предатель. Питомец ставит метку; после каждого твоего убийства тебя возвращает к метке (метка остаётся). Метка сбрасывается на собрании. Нужен питомец!
- EN: Impostor. Pet sets a mark; after each of your kills you are returned to the mark (it stays). The mark is cleared at a meeting. A pet is required!

### Ghoul / Упырь
- RU: Предатель. С настраиваемым шансом жертва съедается целиком: тело нельзя доложить.
- EN: Impostor. With a configurable chance the victim is eaten whole: the body can't be reported.

### Auditor / Ревизор
- RU: Предатель. Питомец показывает, сколько заданий осталось у экипажа и у кого их больше всего. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet shows how many tasks the crew has left and who has the most. Cooldown is configurable. A pet is required!

### Lurer / Приманщик
- RU: Предатель. Питомец притягивает к тебе ближайшего члена экипажа (в радиусе до 8 м). Число использований и перезарядка настраиваются. Нужен питомец!
- EN: Impostor. Pet pulls the nearest crewmate (within 8 m) to you. Number of uses and cooldown are configurable. A pet is required!

### Breaker / Взломщик
- RU: Предатель. Питомец рядом с игроком (до 3 м) ломает его защиту, если она есть. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet next to a player (within 3 m) breaks his shield if he has one. Cooldown is configurable. A pet is required!

### Rumormonger / Сплетник
- RU: Предатель. На собрании с настраиваемым шансом всем мирным приходит слух про случайного невиновного игрока.
- EN: Impostor. At a meeting, with a configurable chance, all crewmates receive a rumor about a random innocent player.

### Jammer / Глушитель
- RU: Предатель. Питомец включает саботаж связи, если сейчас нет другого саботажа. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet starts the comms sabotage if no other sabotage is active. Cooldown is configurable. A pet is required!

### Mystifier / Мистификатор
- RU: Предатель. Питомец рассылает всем мирным ложную тревогу: «в такой-то комнате кто-то кричит» (случайная комната). Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet sends all crewmates a false alert: "someone is screaming in that room" (a random room). Cooldown is configurable. A pet is required!

### Stalker / Сталкер
- RU: Предатель. Питомец показывает, сколько членов экипажа сейчас в одиночестве (никого в радиусе 6 м), и где ближайший из них. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet shows how many crewmates are alone right now (nobody within 6 m) and where the nearest of them is. Cooldown is configurable. A pet is required!

### Slammer / Захлопыватель
- RU: Предатель. Питомец захлопывает сразу все двери на карте (как массовый саботаж дверей, они откроются сами). Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet slams all doors on the map at once (like a mass door sabotage, they open by themselves). Cooldown is configurable. A pet is required!

### Trapper / Капканщик
- RU: Предатель. Питомец ставит капкан: первый член экипажа, наступивший на него, застывает на несколько секунд, а ты узнаёшь его имя. Капканы пропадают на собрании. Нужен питомец!
- EN: Impostor. Pet places a trap: the first crewmate to step on it freezes for a few seconds and you learn his name. Traps vanish at a meeting. A pet is required!

### Reaper / Жнец
- RU: Предатель. После убийства с настраиваемым шансом получает щит, который отменит первое нападение на него.
- EN: Impostor. After a kill, with a configurable chance, he gets a shield that cancels the first attack on him.


## Neutrals / Нейтралы

### Smoker / Курильщик
- RU: Нейтрал. Питомец — выкурить сигарету в любом месте: там остаётся облако дыма. Кто просидит в дыму нужное время подряд (по умолчанию 10 с, настраивается) — умрёт; предупреждение приходит тому, кто в дыму. Буст: в своём дыму курильщика нельзя убить, а после убийства дымом сигарета готова сразу. Для сервера он настоящий предатель. Нужен питомец!
- EN: Neutral. Pet — smoke a cigarette anywhere, leaving a smoke cloud. Whoever stays in the smoke for the set time in a row (10 s by default, configurable) dies; the player inside gets a warning. Boost: the smoker can't be killed inside his own smoke, and after a smoke kill his cigarette is ready at once. A real impostor for the server. A pet is required!

### Pinkman / Пинкман
- RU: Нейтрал, по мотивам «Во все тяжкие». Питомец вдали от людей — варить партию (по умолчанию 30 с), рядом с игроком — продать ему товар. Чистый товар (шанс настраивается) даёт покупателю щит, брак — мигание экрана. Каждые две удачные продажи дают щит самому Пинкману. Нужен питомец! Yeah, science!
- EN: Neutral, inspired by Breaking Bad. Pet away from people — cook a batch (30 s by default), next to a player — sell to him. Pure product (chance configurable) gives the buyer a shield, a bad batch flashes his screen. Every two good sales give Pinkman himself a shield. A pet is required! Yeah, science!

### Prankster / Шутник
- RU: Нейтрал. Питомец меняет тебя местами с ближайшим игроком (до 3,5 м). Нужен питомец!
- EN: Neutral. Pet swaps you with the nearest player (within 3.5 m). A pet is required!

### Crow / Ворон
- RU: Нейтрал. Питомец переносит тебя к последнему трупу, пока он не доложен. Число перелётов и перезарядка настраиваются. Нужен питомец!
- EN: Neutral. Pet teleports you to the last unreported body. Number of flights and cooldown are configurable. A pet is required!

### Defector / Перебежчик
- RU: Нейтрал. На первом собрании узнаёт имя одного из игроков как предателя, но с настраиваемым шансом информация ложная.
- EN: Neutral. At the first meeting learns a player's name as an impostor, but with a configurable chance the info is false.

### Reflector / Отражатель
- RU: Нейтрал. Первое нападение на него с настраиваемым шансом отражается: убийца гибнет сам. Работает один раз за игру.
- EN: Neutral. The first attack on him is reflected with a configurable chance: the killer dies instead. Works once per game.

### Cloud / Облако
- RU: Нейтрал. Погладь питомца — на этом месте останется невидимое облако (лимит настраивается). Когда кто-то другой наступит на него, облако исчезнет, а тебе придёт сигнал паники: экран мигает красным (на собрании придёт сообщение). С небольшим шансом (настраивается) облако оказывается грозой: наступивший в её зону погибнет. Владелец узнаёт, какое облако — гроза. Облака пропадают на собрании. Нужен питомец!
- EN: Neutral. Pet your pet - an invisible cloud stays at that spot (limit is configurable). When someone else steps on it the cloud vanishes and you get a panic signal: your screen flashes red (a message arrives at the meeting). With a small (configurable) chance a cloud turns out to be a thunderstorm: whoever steps into its zone dies. The owner is told which cloud is a storm. Clouds vanish at a meeting. A pet is required!

### Vigilante / Виджилант
- RU: Нейтрал. Для сервера — настоящий предатель: видит других предателей, задания не нужны. Убийство открывается по таймеру: погладь питомца рядом с целью. Убийство предателя штрафа не даёт, убийство невиновного даёт штраф (0/2); если штрафов больше допустимого, убийство блокируется. Нужен питомец!
- EN: Neutral. A real impostor for the server: sees other impostors, no tasks. The kill unlocks on a timer: pet your pet next to a target. Killing an impostor gives no penalty, killing an innocent gives a penalty (0/2); too many penalties lock the kill. A pet is required!

### Marauder / Мародёр
- RU: Нейтрал. Питомец рядом с трупом (до 2 м) — обыскать его: ты получаешь щит. Один труп — один раз. Число обысков ограничено. Нужен питомец!
- EN: Neutral. Pet next to a body (within 2 m) - loot it: you get a shield. One body - once. Number of loots is limited. A pet is required!

### Adventurer / Авантюрист
- RU: Нейтрал. Питомец — бросить кости: щит, телепорт к случайному игроку, подсказка о чьём-то местоположении или мигание экрана. Число бросков ограничено. Нужен питомец!
- EN: Neutral. Pet - roll the dice: a shield, a teleport to a random player, a hint about someone's location or a screen flash. Number of rolls is limited. A pet is required!

### Hermit / Затворник
- RU: Нейтрал. Если рядом (в радиусе 6 м) никого нет в течение настраиваемого времени, получает щит. Работает сама.
- EN: Neutral. If nobody is nearby (within 6 m) for a configurable time he gets a shield. Works by itself.

### Tourist / Турист
- RU: Нейтрал. Побывав в настраиваемом числе разных комнат, получает щит и начинает считать заново. Работает сама.
- EN: Neutral. After visiting a configurable number of different rooms he gets a shield and starts counting again. Works by itself.

### Avenger / Мститель
- RU: Нейтрал. Если его убили, через настраиваемое время погибает и убийца.
- EN: Neutral. If he is killed, his killer dies too after a configurable delay.

### Trader / Торговец
- RU: Нейтрал. Питомец рядом с игроком (до 3 м) отнимает у него щит, если он есть, и отдаёт тебе. Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet next to a player (within 3 m) takes his shield, if he has one, and gives it to you. Cooldown is configurable. A pet is required!

### Sleepwalker / Лунатик
- RU: Нейтрал. Время от времени сам переносится к случайному игроку. Работает сама.
- EN: Neutral. From time to time he teleports to a random player by himself. Works by itself.

### Mole / Крот
- RU: Нейтрал. Питомец переносит его к случайной двери на карте. Число использований и перезарядка настраиваются. Нужен питомец!
- EN: Neutral. Pet teleports him to a random door on the map. Number of uses and cooldown are configurable. A pet is required!

### Hypnotist / Гипнотизёр
- RU: Нейтрал. Питомец рядом с игроком (до 3 м) обездвиживает его на несколько секунд. Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet next to a player (within 3 m) freezes him in place for a few seconds. Cooldown is configurable. A pet is required!

### Swindler / Мошенник
- RU: Нейтрал. Питомец рядом с игроком (до 3 м): тому приходит ложное сообщение, что он получил защиту, а настоящий щит получает Мошенник. Число обманов ограничено. Нужен питомец!
- EN: Neutral. Pet next to a player (within 3 m): he gets a fake message that he received a shield, while the Swindler gets a real one. Number of tricks is limited. A pet is required!

### Scarecrow / Пугало
- RU: Нейтрал. Питомец пугает всех игроков вокруг (в радиусе 6 м): у них на несколько секунд мигает экран. Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet scares all players around (within 6 m): their screens flash for a few seconds. Cooldown is configurable. A pet is required!

### Clockmaker / Часовщик
- RU: Нейтрал. Питомец отматывает время для тебя: ты возвращаешься туда, где был около 10 секунд назад. Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet rewinds time for you: you return to where you were about 10 seconds ago. Cooldown is configurable. A pet is required!

### Lottery / Лотерейщик
- RU: Нейтрал. На каждом собрании с настраиваемым шансом выигрывает щит.
- EN: Neutral. At every meeting he wins a shield with a configurable chance.

### Magnet / Магнит
- RU: Нейтрал. Питомец притягивает к тебе всех игроков вокруг (в радиусе настраивается). Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet pulls all players around you towards you (radius configurable). Cooldown is configurable. A pet is required!

### Fence / Скупщик
- RU: Нейтрал. За каждые N заданий, выполненных экипажем (настраивается), получает щит. Работает сама.
- EN: Neutral. For every N tasks completed by the crew (configurable) he gets a shield. Works by itself.
