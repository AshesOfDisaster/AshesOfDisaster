#!/usr/bin/env python3
"""Run all — собирает Ashes of Disaster и кладёт DLL в BepInEx/plugins."""
import glob, json, os, re, shutil, subprocess, sys

ROOT = os.path.dirname(os.path.abspath(__file__))
CFG = os.path.join(ROOT, "run_all.config.json")
CSPROJ = os.path.join(ROOT, "AshesOfDisaster.csproj")
OUT = os.path.join(ROOT, "build")
DLL = "AshesOfDisaster.dll"


def say(msg):
    print(msg, flush=True)


def die(msg):
    say("\n[ОШИБКА] " + msg)
    pause()
    sys.exit(1)


def pause():
    if sys.stdin and sys.stdin.isatty():
        try:
            input("\nНажми Enter, чтобы закрыть...")
        except EOFError:
            pass


def is_game(p):
    return bool(p) and os.path.isfile(os.path.join(p, "Among Us.exe"))


def steam_libraries():
    libs = []
    try:
        import winreg
        for hive, key in ((winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam"),
                          (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam")):
            try:
                with winreg.OpenKey(hive, key) as k:
                    for name in ("SteamPath", "InstallPath"):
                        try:
                            libs.append(winreg.QueryValueEx(k, name)[0])
                        except OSError:
                            pass
            except OSError:
                pass
    except ImportError:
        pass
    libs += [r"C:\Program Files (x86)\Steam", r"C:\Program Files\Steam"]
    extra = []
    for lib in libs:
        vdf = os.path.join(lib, "steamapps", "libraryfolders.vdf")
        if os.path.isfile(vdf):
            try:
                txt = open(vdf, encoding="utf-8", errors="ignore").read()
                extra += [m.replace("\\\\", "\\") for m in re.findall(r'"path"\s+"([^"]+)"', txt)]
            except OSError:
                pass
    return libs + extra


def find_game():
    if os.path.isfile(CFG):
        try:
            p = json.load(open(CFG, encoding="utf-8")).get("game_dir")
            if is_game(p):
                return p
        except (OSError, ValueError):
            pass
    env = os.environ.get("AMONG_US_DIR")
    if is_game(env):
        return env
    cands = [os.path.join(l, "steamapps", "common", "Among Us") for l in steam_libraries()]
    cands += [r"C:\Program Files\Epic Games\AmongUs", r"C:\Program Files (x86)\Epic Games\AmongUs",
              r"C:\Program Files\Among Us", r"D:\Games\Among Us"]
    for c in cands:
        if is_game(c):
            return c
    say("Папку Among Us найти не удалось.")
    while True:
        p = input("Вставь путь к папке с 'Among Us.exe': ").strip().strip('"')
        if is_game(p):
            return p
        say("Там нет Among Us.exe, попробуй ещё раз.")


def find_dotnet():
    d = shutil.which("dotnet")
    if d:
        return d
    for p in (r"C:\Program Files\dotnet\dotnet.exe", r"C:\Program Files (x86)\dotnet\dotnet.exe"):
        if os.path.isfile(p):
            return p
    return None


def ensure_dotnet():
    d = find_dotnet()
    if d:
        return d
    say(".NET SDK не найден, пробую поставить через winget...")
    if shutil.which("winget"):
        subprocess.call(["winget", "install", "-e", "--id", "Microsoft.DotNet.SDK.8",
                         "--accept-package-agreements", "--accept-source-agreements"])
        d = find_dotnet()
        if d:
            return d
    die("Установи .NET SDK 8: https://dotnet.microsoft.com/download  и запусти Run all снова.")


def main():
    say("=== Ashes of Disaster :: Run all ===")
    game = find_game()
    json.dump({"game_dir": game}, open(CFG, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
    say("Among Us: " + game)

    bep = os.path.join(game, "BepInEx")
    for sub in ("core", "interop"):
        if not glob.glob(os.path.join(bep, sub, "*.dll")):
            die(f"Нет BepInEx\\{sub}. Поставь BepInEx 6 (IL2CPP, x86 для Steam-версии) в папку игры "
                "и один раз запусти игру — она сгенерирует interop. Потом снова Run all.")

    dotnet = ensure_dotnet()
    if os.path.isdir(OUT):
        shutil.rmtree(OUT, ignore_errors=True)

    say("Сборка...")
    r = subprocess.run([dotnet, "build", CSPROJ, "-c", "Release", "-nologo", "-clp:NoSummary",
                        "-p:GameDir=" + game.replace("\\", "/"), "-o", OUT],
                       cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                       text=True, encoding="utf-8", errors="replace")
    log = os.path.join(ROOT, "build_log.txt")
    open(log, "w", encoding="utf-8").write(r.stdout)
    built = os.path.join(OUT, DLL)
    if r.returncode != 0 or not os.path.isfile(built):
        errs = []
        for line in r.stdout.splitlines():
            if " error " in line and line.strip() not in errs:
                errs.append(line.strip())
        say("\n".join(errs[:15]) or r.stdout[-2000:])
        try:
            ver = subprocess.run([dotnet, "--version"], capture_output=True, text=True).stdout.strip()
        except OSError:
            ver = "?"
        with open(os.path.join(ROOT, "build_errors.txt"), "w", encoding="utf-8") as f:
            f.write(f"dotnet {ver}\ngame: {game}\n\n" + "\n".join(errs) + "\n")
        text = "\n".join(errs)
        if re.search(r"CS0117|CS0103|CS1061|CS0246|CS1501|CS1503|CS0234", text):
            say("\nПохоже, мод не совпадает с версией игры (нет такого типа/метода/поля в interop). "
                "Если игра недавно обновилась: запусти её один раз с BepInEx, чтобы папка interop пересоздалась, и снова Run all. "
                "Если не помогло — пришли build_errors.txt.")
        elif re.search(r"NU1101|NU1301|NETSDK1004|MSB4236", text):
            say("\nНе скачался пакет .NET (нужен интернет) или не найден .NET SDK. Проверь сеть и что установлен .NET SDK 8.")
        die("Сборка не удалась. Пришли мне файл build_errors.txt (лежит рядом со скриптом); полный лог — build_log.txt.")

    plugins = os.path.join(bep, "plugins")
    os.makedirs(plugins, exist_ok=True)
    shutil.copy2(built, os.path.join(plugins, DLL))
    say("Готово: " + os.path.join(plugins, DLL))

    others = [os.path.basename(p) for p in glob.glob(os.path.join(plugins, "*.dll"))
              if os.path.basename(p) != DLL]
    if others:
        say("\nВ plugins лежат и другие моды: " + ", ".join(others))
        say("Если мешают (например AmongUsStable, AUnlocker) — вынеси их на время тестов.")
    say("\nВ лобби хоста: настройки мода — вкладки справа в меню настроек лобби, /h — команды.")
    pause()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass
