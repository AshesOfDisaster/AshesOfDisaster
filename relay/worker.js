// Ashes of Disaster relay: принимает коды комнат и жалобы от мода и пересылает их в Discord.
// Адреса вебхуков хранятся только в секретах Worker (ROOMS_WEBHOOK, REPORTS_WEBHOOK), в DLL их нет.
const hits = new Map();

function limited(ip, kind, max) {
  const key = kind + ':' + ip, now = Date.now();
  const arr = (hits.get(key) || []).filter(t => now - t < 60000);
  arr.push(now); hits.set(key, arr);
  if (hits.size > 5000) hits.clear();
  return arr.length > max;
}

function clean(v, n) {
  return String(v ?? '').slice(0, n).replace(/[\u0000-\u001f]/g, ' ').replace(/[\\*_`~|>]/g, m => '\\' + m).replace(/@/g, '@​');
}

function json(obj, status = 200) {
  return new Response(JSON.stringify(obj), { status, headers: { 'content-type': 'application/json' } });
}

function roomText(b) {
  const states = { lobby: '🟢 Открыта, лобби', game: '🟡 Идёт игра', ended: '🟢 Открыта, игра закончилась', closed: '🔴 Закрыта' };
  const st = states[b.state] || states.lobby;
  const players = b.state === 'closed' ? '-' : String(Math.max(0, Math.min(30, parseInt(b.players) || 0)));
  return `Комната: **${b.code}**\nСтатус: ${st}\nХост: ${clean(b.host, 40) || 'неизвестно'}\nИгроков: ${players}\nМод Ashes of Disaster ${clean(b.ver, 12)}`;
}

export default {
  async fetch(req, env) {
    if (req.method === 'OPTIONS') return new Response(null, { status: 204 });
    const url = new URL(req.url);
    const ip = req.headers.get('CF-Connecting-IP') || 'x';
    if (url.pathname === '/' && req.method === 'GET') return json({ ok: true, service: 'ashes-relay' });
    if (req.method !== 'POST' && req.method !== 'PATCH') return json({ error: 'method' }, 405);
    const raw = await req.text();
    if (raw.length > 2000) return json({ error: 'too big' }, 413);
    let b;
    try { b = JSON.parse(raw); } catch { return json({ error: 'json' }, 400); }
    const post = (hook, text, wait) => fetch(hook + (wait ? '?wait=true' : ''), {
      method: 'POST', headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ content: text, allowed_mentions: { parse: [] } })
    });

    if (url.pathname === '/room') {
      if (limited(ip, 'room', 30)) return json({ error: 'rate' }, 429);
      if (!/^[A-Z]{4,8}$/.test(String(b.code || ''))) return json({ error: 'code' }, 400);
      b.code = String(b.code);
      if (req.method === 'POST') {
        const r = await post(env.ROOMS_WEBHOOK, roomText(b), true);
        if (!r.ok) return json({ error: 'discord', status: r.status }, 502);
        const m = await r.json();
        return json({ id: m.id });
      }
      if (!/^\d{5,25}$/.test(String(b.id || ''))) return json({ error: 'id' }, 400);
      const r = await fetch(env.ROOMS_WEBHOOK + '/messages/' + b.id, {
        method: 'PATCH', headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ content: roomText(b), allowed_mentions: { parse: [] } })
      });
      return json({ ok: r.ok }, r.ok ? 200 : 502);
    }

    if (url.pathname === '/report' && req.method === 'POST') {
      if (limited(ip, 'report', 5)) return json({ error: 'rate' }, 429);
      const text = `Жалоба, хост комнаты: ${clean(b.host, 40)}\nИгрок: ${clean(b.name, 40)}\nFriend code: ${clean(b.fc, 40) || 'нет'}\nПричина: ${clean(b.why, 200)}\nМод ${clean(b.ver, 12)}`;
      const r = await post(env.REPORTS_WEBHOOK, text, false);
      return json({ ok: r.ok }, r.ok ? 200 : 502);
    }
    return json({ error: 'not found' }, 404);
  }
};
