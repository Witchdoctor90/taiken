using System.Net;
using System.Text.Json;

namespace Taiken.FakePsp;

internal static class PayPageHtml
{
    private const string Template = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <title>Fake PSP</title>
        <style>
          body { font-family: system-ui, sans-serif; background:#0f1115; color:#e8e8e8; display:flex; align-items:center; justify-content:center; height:100vh; margin:0; }
          .card { background:#1b1e24; padding:2rem; border-radius:12px; width:320px; text-align:center; }
          .amount { font-size:2rem; margin:0.5rem 0 1.5rem; }
          button { display:block; width:100%; margin:0.4rem 0; padding:0.6rem; border-radius:8px; border:none; cursor:pointer; font-size:0.95rem; }
          .success { background:#1f8a4c; color:#fff; }
          .decline { background:#b3382c; color:#fff; }
          .neutral { background:#33363d; color:#e8e8e8; }
          #result { margin-top:1rem; font-size:0.85rem; color:#9aa0a6; min-height:1.2em; }
        </style>
        </head>
        <body>
          <div class="card">
            <div>Fake PSP — тестова оплата</div>
            <div class="amount">__AMOUNT__ __CURRENCY__</div>
            <button class="success" data-scenario="success">Успіх</button>
            <button class="decline" data-scenario="decline">Відмова</button>
            <button class="neutral" data-scenario="timeout">Таймаут</button>
            <button class="success" data-scenario="success-duplicate-webhook">Успіх + дубль webhook</button>
            <button class="success" data-scenario="success-different-amount">Успіх з іншою сумою</button>
            <div id="result"></div>
          </div>
          <script>
            const sid = __SID_JSON__;
            const embedOrigin = __EMBED_ORIGIN_JSON__;
            document.querySelectorAll('button[data-scenario]').forEach(btn => {
              btn.addEventListener('click', async () => {
                const scenario = btn.dataset.scenario;
                const res = await fetch(`/pay/${sid}/scenario/${scenario}`, { method: 'POST' });
                const data = await res.json();
                document.getElementById('result').textContent = `st: ${data.st}`;
                window.parent.postMessage({ type: 'psp_result', sid: data.sid, status: data.st }, embedOrigin);
              });
            });
          </script>
        </body>
        </html>
        """;

    public static string Render(Session session) => Template
        .Replace("__AMOUNT__", WebUtility.HtmlEncode(session.AmountMajor))
        .Replace("__CURRENCY__", WebUtility.HtmlEncode(session.Currency))
        .Replace("__SID_JSON__", JsonSerializer.Serialize(session.Sid))
        .Replace("__EMBED_ORIGIN_JSON__", JsonSerializer.Serialize(session.EmbedOrigin));
}
