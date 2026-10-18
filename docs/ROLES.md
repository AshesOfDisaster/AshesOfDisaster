# Roles / Роли

Total / Всего: **75**. Descriptions are shown in game with `/r <role>`.


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

### Sentry / Часовой
- RU: Член экипажа. Питомец ставит пост (растяжку): первый, кто пройдёт рядом, будет назван Часовому, пост исчезает. Посты пропадают на собрании. Нужен питомец!
- EN: A crewmate. Pet places a tripwire post: the first player to pass near it is named to the Sentry and the post vanishes. Posts vanish at a meeting. A pet is required!

### Statistician / Статистик
- RU: Член экипажа. На собрании командой /ab узнаёт, сколько предателей (для сервера) ещё живо. Число проверок ограничено.
- EN: A crewmate. In a meeting /ab shows how many impostors (as the server sees them) are still alive. The number of checks is limited.

### Intercessor / Заступник
- RU: Член экипажа. Питомец рядом с игроком берёт его под защиту: если подопечного попытаются убить, нападение отменяется, а погибает сам Заступник. Подопечный один, новый заменяет прежнего. Метка пропадает на собрании. Нужен питомец!
- EN: A crewmate. Pet next to a player takes him under protection: if the ward is attacked the attack is cancelled and the Intercessor dies instead. One ward at a time. The ward is dropped at a meeting. A pet is required!

### Peacemaker / Миротворец
- RU: Член экипажа. Питомец создаёт вокруг него зону мира на несколько секунд: пока она действует, предатели рядом не могут никого убить. Число использований ограничено. Нужен питомец!
- EN: A crewmate. Pet creates a peace zone around him for a few seconds: while it lasts impostors nearby can't kill anyone. Number of uses is limited. A pet is required!

### Eyewitness / Очевидец
- RU: Член экипажа. Если рядом с ним (в радиусе, по умолчанию 7 м) кого-то убили, он узнаёт имя убийцы — но с настраиваемым шансом имя ложное.
- EN: A crewmate. If someone is killed near him (within the radius, 7 m by default) he learns the killer's name - but with a configurable chance the name is false.

### Forensic / Криминалист
- RU: Член экипажа. Питомец рядом с трупом (до 2 м): узнаёт, сколько секунд назад тот погиб, и смывает краску Маляра, чтобы труп можно было доложить. Съеденное тело не вернуть. Число осмотров ограничено. Нужен питомец!
- EN: A crewmate. Pet next to a body (within 2 m): learns how many seconds ago it died and washes off the Painter's paint so the body can be reported. An eaten body can't be restored. Number of exams is limited. A pet is required!

### Watcher / Наблюдатель
- RU: Член экипажа. Питомец ставит камеру в комнате (число камер настраивается). На собрании узнаёшь, кто заходил в эти комнаты. Нужен питомец!
- EN: A crewmate. Pet places a camera in the room you stand in (number of cameras is configurable). At the meeting you learn who entered those rooms. A pet is required!

### Radioman / Радист
- RU: Член экипажа. Питомец включает радар: ты узнаёшь, насколько близко ближайший предатель — «холодно», «тепло» или «горячо», без имени и комнаты. Нужен питомец!
- EN: A crewmate. Pet switches on a radar: you learn how close the nearest impostor is - cold, warm or hot - without a name or a room. A pet is required!

### Companion / Спутник
- RU: Член экипажа. Считает, сколько секунд каждый игрок провёл рядом с тобой. На собрании называет того, кто был рядом дольше всех.
- EN: A crewmate. Counts how long every player stayed near you. At the meeting names the player who was near you the longest.

### Seismograph / Сейсмограф
- RU: Член экипажа. Когда кого-то убивают, ты сразу чувствуешь толчок: в какой стороне света и насколько далеко от тебя это случилось (комнату не называет).
- EN: A crewmate. When someone is killed you feel a tremor at once: in which direction and how far from you it happened (the room is not named).

### Tracker / Следопыт
- RU: Член экипажа. Питомец рядом с игроком метит его. Дальше питомец показывает, в какой комнате метка сейчас, и на каждом собрании ты получаешь отчёт о нём.
- EN: A crewmate. Pet next to a player tags him. Afterwards the pet shows the room the tagged player is in right now, and at every meeting you get a report about him.

### Accountant / Бухгалтер
- RU: Член экипажа. На каждом собрании узнаёт, кто из игроков выполнил меньше всего заданий и сколько именно. Работает сам.
- EN: A crewmate. At every meeting learns which player has completed the fewest tasks and how many. Works by itself.

### Beacon / Маяк
- RU: Член экипажа. Питомец ставит маяк в комнате. Пока маяк стоит, ты получаешь сигнал каждый раз, когда в эту комнату входит предатель (без имени). Маяк гаснет на собрании. Нужен питомец!
- EN: A crewmate. Pet places a beacon in the room. While it stands you get a signal every time an impostor enters that room (no name). The beacon goes out at a meeting. A pet is required!

### Sapper / Сапёр
- RU: Член экипажа. Питомец обезвреживает мины и капканы предателей в радиусе 3 м. Число обезвреживаний и перезарядка настраиваются. Нужен питомец!
- EN: A crewmate. Pet defuses impostors' mines and traps within 3 m. Number of uses and cooldown are configurable. A pet is required!

### Herald / Глашатай
- RU: Член экипажа. Питомец рассылает всем живым игрокам вестник: в какой ты комнате и сколько людей рядом. Число объявлений ограничено. Нужен питомец!
- EN: A crewmate. Pet sends a bulletin to every living player: which room you are in and how many people are near. Number of announcements is limited. A pet is required!

### Coach / Тренер
- RU: Член экипажа. Питомец рядом с членом экипажа (до 3 м) сбрасывает ему перезарядки способностей. Число тренировок ограничено. Нужен питомец!
- EN: A crewmate. Pet next to a crewmate (within 3 m) resets his ability cooldowns. Number of trainings is limited. A pet is required!

### Telegraphist / Телеграфист
- RU: Член экипажа. Питомец рядом с игроком (до 3 м) связывает вас телеграфом. Если один из вас погибнет, второй сразу узнает, кто и где. Связь одна. Нужен питомец!
- EN: A crewmate. Pet next to a player (within 3 m) links you by telegraph. If one of you is killed the other learns at once who and where. One link at a time. A pet is required!

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

### Painter / Маляр
- RU: Предатель. Трупы его жертв закрашены: доложить о них нельзя, пока краска не стечёт (время настраивается).
- EN: Impostor. Bodies of his victims are painted over: they can't be reported until the paint runs off (configurable).

### Spider / Паук
- RU: Предатель. Погладь питомца у дверного проёма — там появится паутина. Член экипажа, наступивший на неё, застревает, а паук узнаёт об этом на собрании. Если убить застрявшего, паук съедает его целиком: тело нельзя доложить. Экипаж освобождает застрявшего, постояв рядом несколько секунд. Предатели и нейтралы проходят сквозь паутину (настройка). Нужен питомец!
- EN: Impostor. Pet your pet next to a doorway to place a web. A crewmate who steps on it gets stuck; the spider learns about it at the meeting. Killing the stuck player eats them whole: the body can't be reported. Crew frees the victim by standing nearby for a few seconds. Impostors and neutrals pass through webs (setting). A pet is required!

### Minelayer / Минёр
- RU: Предатель. Питомец ставит мину: первый член экипажа, наступивший на неё, погибает (щит спасает). Мины пропадают на собрании. Число мин настраивается. Нужен питомец!
- EN: Impostor. Pet places a mine: the first crewmate to step on it dies (a shield saves him). Mines vanish at a meeting. Number of mines is configurable. A pet is required!

### Eclipser / Затмитель
- RU: Предатель. Питомец гасит свет (саботаж электричества), если сейчас нет другого саботажа. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet turns the lights off (electrical sabotage) if no other sabotage is active. Cooldown is configurable. A pet is required!

### Returner / Возвращенец
- RU: Предатель. Питомец ставит метку; после каждого твоего убийства тебя возвращает к метке (метка остаётся). Метка сбрасывается на собрании. Нужен питомец!
- EN: Impostor. Pet sets a mark; after each of your kills you are returned to the mark (it stays). The mark is cleared at a meeting. A pet is required!

### Lurer / Приманщик
- RU: Предатель. Питомец притягивает к тебе ближайшего члена экипажа (в радиусе до 8 м). Число использований и перезарядка настраиваются. Нужен питомец!
- EN: Impostor. Pet pulls the nearest crewmate (within 8 m) to you. Number of uses and cooldown are configurable. A pet is required!

### Trapper / Капканщик
- RU: Предатель. Питомец ставит капкан: первый член экипажа, наступивший на него, застывает на несколько секунд, а ты узнаёшь его имя. Капканы пропадают на собрании. Нужен питомец!
- EN: Impostor. Pet places a trap: the first crewmate to step on it freezes for a few seconds and you learn his name. Traps vanish at a meeting. A pet is required!

### Banisher / Выселитель
- RU: Предатель. Питомец выбрасывает ближайшего члена экипажа (до 4 м) к случайной двери на карте. Число использований и перезарядка настраиваются. Нужен питомец!
- EN: Impostor. Pet throws the nearest crewmate (within 4 m) to a random door on the map. Number of uses and cooldown are configurable. A pet is required!

### Hush / Беззвучный
- RU: Предатель. Питомец включает режим тишины на несколько секунд: тебя не замечают журнал Логера, камеры Наблюдателя, посты Часового, маяки и радары. Нужен питомец!
- EN: Impostor. Pet switches on silence for a few seconds: the Logger's log, the Watcher's cameras, Sentry posts, beacons and radars do not notice you. A pet is required!

### Bomber / Подрывник
- RU: Предатель. Питомец рядом с членом экипажа (до 3 м) цепляет ему бомбу. Через время бомба взрывается и убивает всех мирных вокруг (щит спасает). Нужен питомец!
- EN: Impostor. Pet next to a crewmate (within 3 m) sticks a bomb on him. After a while it explodes and kills every crewmate around (a shield saves). A pet is required!

### Forger / Фальсификатор
- RU: Предатель. Питомец вписывает в журнал Логера ложную запись: случайный член экипажа якобы вошёл в твою комнату. Число подделок ограничено. Нужен питомец!
- EN: Impostor. Pet writes a false entry into the Logger's log: a random crewmate supposedly entered your room. Number of forgeries is limited. A pet is required!

### Scatterer / Разгонщик
- RU: Предатель. После твоего убийства все члены экипажа рядом с телом (в настраиваемом радиусе) разлетаются к случайным дверям. Срабатывает не чаще перезарядки.
- EN: Impostor. After your kill every crewmate near the body (within a configurable radius) is thrown to random doors. Triggers no more often than the cooldown.

### Spy / Шпион
- RU: Предатель. Питомец рядом с игроком (до 4 м) раскрывает тебе его роль. Число проверок ограничено. Нужен питомец!
- EN: Impostor. Pet next to a player (within 4 m) reveals his role to you. Number of checks is limited. A pet is required!

### Spotter / Наводчик
- RU: Предатель. Питомец показывает, сколько членов экипажа сейчас в каждой четверти карты относительно тебя: северо-восток, северо-запад, юго-восток, юго-запад. Нужен питомец!
- EN: Impostor. Pet shows how many crewmates are in each quarter of the map relative to you: north-east, north-west, south-east, south-west. A pet is required!

### Fugitive / Беглец
- RU: Предатель. После убийства тебя через секунду уносит к случайной двери. Срабатывает не чаще перезарядки.
- EN: Impostor. After a kill you are carried to a random door a second later. Triggers no more often than the cooldown.

### Overseer / Надзиратель
- RU: Предатель. Когда член экипажа в настраиваемом радиусе заканчивает задание, ты сразу узнаёшь, кто и в какой комнате. Работает сам.
- EN: Impostor. When a crewmate within a configurable radius finishes a task you at once learn who and in which room. Works by itself.

### Gravedigger / Могильщик
- RU: Предатель. Питомец рядом с телом (до 2 м) закапывает его: тело нельзя доложить. Число захоронений ограничено. Нужен питомец!
- EN: Impostor. Pet next to a body (within 2 m) buries it: the body can't be reported. Number of burials is limited. A pet is required!

### Meltdown / Расплав
- RU: Предатель. Питомец запускает тяжёлый саботаж (реактор, лаборатория, вертолёт или кислород, смотря какой есть на карте), если сейчас нет другого саботажа. Нужен питомец!
- EN: Impostor. Pet starts a heavy sabotage (reactor, laboratory, helicopter or oxygen, whichever the map has) if no other sabotage is active. A pet is required!

### Appraiser / Оценщик
- RU: Предатель. Питомец показывает, сколько игроков сейчас со щитом и в какой комнате ближайший из них. Перезарядка настраивается. Нужен питомец!
- EN: Impostor. Pet shows how many players have a shield right now and in which room the nearest one is. Cooldown is configurable. A pet is required!

### Silencer / Молчун
- RU: Предатель. Питомец рядом с членом экипажа (до 3 м) глушит его способности питомцем на несколько секунд. Число использований ограничено. Нужен питомец!
- EN: Impostor. Pet next to a crewmate (within 3 m) silences his pet abilities for a few seconds. Number of uses is limited. A pet is required!

### Distracter / Отвлекатель
- RU: Предатель. Питомец захлопывает двери случайной комнаты вдали от тебя (от 15 м), чтобы отвлечь экипаж. Число использований и перезарядка настраиваются. Нужен питомец!
- EN: Impostor. Pet slams the doors of a random room far from you (15 m or more) to distract the crew. Number of uses and cooldown are configurable. A pet is required!

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

### Avenger / Мститель
- RU: Нейтрал. Если его убили, через настраиваемое время погибает и убийца.
- EN: Neutral. If he is killed, his killer dies too after a configurable delay.

### Hypnotist / Гипнотизёр
- RU: Нейтрал. Питомец рядом с игроком (до 3 м) обездвиживает его на несколько секунд. Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet next to a player (within 3 m) freezes him in place for a few seconds. Cooldown is configurable. A pet is required!

### Clockmaker / Часовщик
- RU: Нейтрал. Питомец отматывает время для тебя: ты возвращаешься туда, где был около 10 секунд назад. Перезарядка настраивается. Нужен питомец!
- EN: Neutral. Pet rewinds time for you: you return to where you were about 10 seconds ago. Cooldown is configurable. A pet is required!

### Collector / Коллекционер
- RU: Нейтрал. Питомец рядом с телом (до 2 м) забирает трофей (одно тело — один раз). Набрав нужное число трофеев, ты узнаёшь имя настоящего предателя. Нужен питомец!
- EN: Neutral. Pet next to a body (within 2 m) takes a trophy (one body, once). With enough trophies you learn the name of a real impostor. A pet is required!

### Whirlwind / Вихрь
- RU: Нейтрал. Питомец закручивает вихрь: все игроки вокруг (в радиусе настраивается) разлетаются к случайным дверям. Число использований и перезарядка настраиваются. Нужен питомец!
- EN: Neutral. Pet spins up a whirlwind: every player around (radius is configurable) is thrown to random doors. Number of uses and cooldown are configurable. A pet is required!

### Timestop / Стоп-кадр
- RU: Нейтрал. Питомец останавливает время: все игроки вокруг (кроме тебя) замирают на несколько секунд. Нужен питомец!
- EN: Neutral. Pet stops time: every player around (except you) freezes for a few seconds. A pet is required!

### Oracle / Прорицатель
- RU: Нейтрал. Питомец запускает пророчество: через заданное время ты узнаёшь, сколько убийств случилось за этот срок. Нужен питомец!
- EN: Neutral. Pet starts a prophecy: after the set time you learn how many kills happened in that period. A pet is required!

### Meteor / Метеорит
- RU: Нейтрал. Для сервера — настоящий предатель. Питомец отмечает точку: через несколько секунд туда падает метеорит и убивает всех в радиусе (щит спасает). Нужен питомец!
- EN: Neutral. A real impostor for the server. Pet marks a spot: a few seconds later a meteor hits it and kills everyone in the radius (a shield saves). A pet is required!

### Siren / Сирена
- RU: Нейтрал. Питомец включает сирену: у всех предателей рядом (в радиусе настраивается) мигает экран, а ты узнаёшь, сколько их было. Нужен питомец!
- EN: Neutral. Pet sounds a siren: the screens of all impostors nearby (radius is configurable) flash, and you learn how many there were. A pet is required!

### Insider / Знаток
- RU: Нейтрал. На первом собрании узнаёт имена остальных нейтралов. Работает сам.
- EN: Neutral. At the first meeting learns the names of the other neutrals. Works by itself.

### Broker / Брокер
- RU: Нейтрал. Питомец рядом с игроком (до 3 м): сделка. Ты узнаёшь его роль, а он — в какой комнате ближайшее тело. Число сделок ограничено. Нужен питомец!
- EN: Neutral. Pet next to a player (within 3 m): a deal. You learn his role, he learns which room the nearest body is in. Number of deals is limited. A pet is required!

### Portalist / Портальщик
- RU: Нейтрал. Питомец ставит первый портал, затем второй. Когда оба стоят, питомец у одного портала переносит тебя ко второму. Питомец вдали от порталов ставит новую пару. Нужен питомец!
- EN: Neutral. Pet places a first portal and then a second one. When both stand, the pet next to one portal carries you to the other. The pet away from the portals starts a new pair. A pet is required!

### Photographer / Фотограф
- RU: Нейтрал. Питомец делает снимок: кто где был в этот момент. На ближайшем собрании ты получаешь все свои снимки. Число снимков ограничено. Нужен питомец!
- EN: Neutral. Pet takes a snapshot: who was where at that moment. At the next meeting you receive all your snapshots. Number of snapshots is limited. A pet is required!

### Boomerang / Бумеранг
- RU: Нейтрал. Питомец рядом с игроком (до 5 м) запускает бумеранг: через несколько секунд этот игрок возвращается к тебе, туда, где ты стоишь. Нужен питомец!
- EN: Neutral. Pet next to a player (within 5 m) throws a boomerang: a few seconds later that player is carried back to wherever you stand. A pet is required!

### Chronicler / Летописец
- RU: Нейтрал. Когда кого-то убивают, ты сразу узнаёшь, кто погиб и к какой группе принадлежал убийца (предатель или нейтрал). Работает сам.
- EN: Neutral. When someone is killed you at once learn who died and which group the killer belonged to (impostor or neutral). Works by itself.

### Sorcerer / Колдун
- RU: Нейтрал. Питомец рядом с игроком (до 3 м) накладывает проклятие: его следующая способность питомцем не сработает, а экран замигает. Нужен питомец!
- EN: Neutral. Pet next to a player (within 3 m) casts a curse: his next pet ability fails and his screen flashes. A pet is required!

### Rescuer / Спасатель
- RU: Нейтрал. Питомец рядом с игроком, застрявшим в паутине (до 2,5 м), освобождает его сразу. Число спасений ограничено. Нужен питомец!
- EN: Neutral. Pet next to a player stuck in a web (within 2.5 m) frees him at once. Number of rescues is limited. A pet is required!

