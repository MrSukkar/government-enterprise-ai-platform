using System.Net;
using System.Text;
using Platform.Application.Abstractions;
using Platform.Api.Operations;

namespace Platform.Api.Developers;

internal static class DeveloperPortalEndpoint
{
    internal static IEndpointConventionBuilder MapDeveloperPortal(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/developers", (
            PlatformRuntimeReadiness readiness,
            IEnumerable<IPlatformModule> modules,
            IHostEnvironment environment) =>
        {
            var html = Render(readiness, modules, environment);
            return Results.Text(html, "text/html; charset=utf-8");
        })
        .ExcludeFromDescription()
        .AllowAnonymous();
    }

    private static string Render(
        PlatformRuntimeReadiness readiness,
        IEnumerable<IPlatformModule> modules,
        IHostEnvironment environment)
    {
        var moduleNames = modules
            .Select(module => module.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var builder = new StringBuilder();
        builder.Append(
            """
            <!doctype html>
            <html lang="en" dir="ltr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Platform Developer Console</title>
              <style>
                @font-face{font-family:IBMPlexSansArabic;src:url('/developer/fonts/IBMPlexSansArabic-Regular.woff2') format('woff2');font-weight:400;font-display:swap} @font-face{font-family:IBMPlexSansArabic;src:url('/developer/fonts/IBMPlexSansArabic-SemiBold.woff2') format('woff2');font-weight:600;font-display:swap} :root{color-scheme:light;font-family:IBMPlexSansArabic,system-ui,-apple-system,"Segoe UI",sans-serif;--primary:#1b8354;--gold:#dba102;--ink:#161616;--line:#d2d6db}
                body{margin:0;background:#f9fafb;color:var(--ink)}
                header{background:#111927;color:#fff;padding:28px clamp(20px,5vw,64px);border-top:4px solid var(--gold)}
                main{max-width:1180px;margin:auto;padding:28px clamp(20px,5vw,64px)}
                h1,h2{font-weight:600;margin:0 0 12px}
                p{line-height:1.55}
                .status{display:inline-block;padding:6px 10px;border-radius:999px;background:#ffe6df;color:#7a2113;font-weight:600}
                .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:16px;margin:20px 0}
                .card{background:#fff;border:1px solid var(--line);border-top:3px solid var(--primary);border-radius:4px;padding:18px}
                a{color:var(--primary);font-weight:600}
                table{width:100%;border-collapse:collapse;background:#fff}
                th,td{text-align:left;padding:12px;border-bottom:1px solid #dce4e7;vertical-align:top}
                th{background:#ecfdf3;color:#14573a}
                code{overflow-wrap:anywhere}
                .connected{color:#14733b}.missing{color:#9a2d1d;font-weight:600}
                @media(prefers-color-scheme:dark){body{background:#071b23;color:#e7f0f3}.card,table{background:#0d2b35;border-color:#31505b}th{background:#163b47}th,td{border-color:#31505b}a{color:#63d5c8}.status{background:#58251d;color:#ffd8cf}}
              </style>
            </head>
            <body>
            <header>
              <div>Government Enterprise AI Platform</div>
              <h1>Platform Developer Console</h1>
              <p>Local operational visibility. This page grants no production access and bypasses no policy.</p>
            </header>
            <main>
            """);

        builder.Append("<span class=\"status\">");
        builder.Append(readiness.IsReady ? "READY" : "NOT READY - FAIL CLOSED");
        builder.Append("</span><div class=\"grid\">");
        AppendCard(builder, "Environment", environment.EnvironmentName);
        AppendCard(builder, "Registered modules", moduleNames.Length.ToString());
        AppendCard(builder, "Missing runtime adapters", readiness.MissingDependencies.Count.ToString());
        builder.Append(
            """
            </div>
            <div class="grid">
              <section class="card"><h2>API contract</h2><p>Approved OpenAPI 3.1 contract.</p><a href="/openapi/v1.json">Open contract</a></section>
              <section class="card"><h2>Liveness</h2><p>Confirms the API process is responding.</p><a href="/health">Open liveness</a></section>
              <section class="card"><h2>Readiness</h2><p>Lists every required runtime boundary and fails closed while any is missing.</p><a href="/health/ready">Open readiness</a></section>
            </div>
            <section>
              <h2>Runtime boundary readiness</h2>
              <table>
                <thead><tr><th>Capability</th><th>Contract</th><th>Status</th></tr></thead>
                <tbody>
            """);

        foreach (var dependency in readiness.Dependencies)
        {
            builder.Append("<tr><td>");
            builder.Append(WebUtility.HtmlEncode(dependency.Capability));
            builder.Append("</td><td><code>");
            builder.Append(WebUtility.HtmlEncode(dependency.Contract));
            builder.Append("</code></td><td class=\"");
            builder.Append(dependency.Registered ? "connected\">Connected" : "missing\">Not connected");
            builder.Append("</td></tr>");
        }

        builder.Append(
            """
                </tbody>
              </table>
            </section>
            </main>
            </body>
            </html>
            """);

        return builder.ToString();
    }

    private static void AppendCard(StringBuilder builder, string label, string value)
    {
        builder.Append("<section class=\"card\"><h2>");
        builder.Append(WebUtility.HtmlEncode(label));
        builder.Append("</h2><p>");
        builder.Append(WebUtility.HtmlEncode(value));
        builder.Append("</p></section>");
    }
}
