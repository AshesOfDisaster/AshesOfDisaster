# Relay (Cloudflare Worker)

Принимает коды комнат и жалобы от мода и пересылает их в Discord. Адреса вебхуков хранятся только в секретах Worker.

1. Cloudflare → Workers & Pages → Create → Worker → вставь `worker.js` → Deploy.
2. Settings → Variables and Secrets → добавь секреты `ROOMS_WEBHOOK` и `REPORTS_WEBHOOK` (адреса вебхуков Discord).
3. Адрес Worker (`https://имя.аккаунт.workers.dev`) впиши в `src/Embedded.cs` (`Relay`) и пересобери мод.
