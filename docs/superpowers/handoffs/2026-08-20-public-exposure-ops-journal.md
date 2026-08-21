# SDD ledger — plan: docs/superpowers/plans/2026-08-20-public-exposure.md

Spec: docs/superpowers/specs/2026-08-20-public-exposure-design.md (read; binding authority).

Ruling: execute on main, no worktree — every prior phase in this repo committed to main
directly (git log: catalog, card-page phases), owner pushes manually (~70 ahead), and the
plan's ops tasks deploy from this working tree. Cost if wrong: relocating commits to a
branch, cheap while unpushed.

Ruling: subagent execution covers Tasks 1–5 (code). Tasks 6–15 are ops requiring owner
credentials/dashboards/devices and run owner-in-the-loop in the controller session per the
execution-choice discussion. Cost if wrong: none — the plan text is the same either way.

## Pre-flight conflict scan

| Pair / task | Produces vs consumes | Finding |
|---|---|---|
| T1 & T3 (Program.cs) | T1 inserts UseForwardedHeaders first; T3's pipeline block shows final order ForwardedHeaders→SecurityHeaders→RateLimiter including T1's line | consistent |
| T1, T3, T4 (TestApp.cs) | additive hooks: RemoteIp (T1), Hsts*/WebRoot (T3), AllowedHosts (T4); sequential dispatch, no overlap | clean |
| T2 → T3 | CspScriptHashes.FromHtml(string) → IReadOnlyList<string>; T3 calls exactly that; hash vectors identical in both test files | consistent |
| T5 → T7/T8 | hook script path, /etc/cardstock/tls paths, unit name cardstock-api match T7 install and T8 Kestrel config | consistent |
| T1 self | tests use ExpressPerHour=1 + RemoteIp; limiter partitions on RemoteIpAddress (Program.cs:58–67 verified in recon) | agrees |
| T3 self | options keys Security:HstsMaxAgeSeconds / HstsIncludeSubdomains match TestApp hooks and middleware | agrees |
| T4 vs TDD constraint | plan text itself mandates the pass-immediately expectation with an explicit contingency if it fails | ruled: plan text governs; not a defect |
| T6–T15 self | ops steps reference IPs in two eras (192.168.0.56 until T10 step 6, .30.56 after); each task states which | agrees |

Scan rulings recorded above; no open conflicts. BASE for Task 1: 9945f8e.

Task 1: minor (deferred): no explicit test for loopback + no CF-Connecting-IP header (fallback path; covered indirectly by 29 pre-existing Api tests)
Task 1: minor (deferred): no test for malformed CF-Connecting-IP from trusted proxy (framework parsing behavior)
Task 1: note: KnownNetworks.Clear() → KnownIPNetworks.Clear() deviation verified behaviorally identical by reviewer's framework probe (ASPDEPR005 + TreatWarningsAsErrors)
Task 1: complete (commits 9945f8e..0d8e4be, review clean)

Task 2: fix round 1/5 (2 addressed, 0 open — full-suite evidence now covers all 6 projects (478 passed); report line counts corrected; no code change, head stays cd15207)
Task 2: minor (deferred): attrs "src=" substring check would also match data-src; regex unaware of HTML comments — theoretical only for the controlled Blazor index.html (plan-prescribed code)
Task 2: complete (commits 0d8e4be..cd15207, review clean after round 1)

Task 3: minor (deferred): report prose says "Five static security headers", code correctly has four + CSP + gated HSTS (report-only miscount)
Task 3: minor (deferred): format-gate (dotnet format --verify-no-changes) unevidenced in reports so far — controller to run once before final review
Task 3: complete (commits cd15207..4348518, review clean)

Task 4: minor (deferred): report's self-cited TestApp line numbers drift 1-3 lines from actual (content correct)
Task 4: minor (deferred): only cardstock.pro exercised as the served host; localhost/127.0.0.1 membership unexercised (verbatim-brief scope; Task 8 ships the string)
Task 4: note: implementer session died on API connection error mid-run; resumed cleanly, work verified complete by reviewer
Task 4: complete (commits 4348518..7c1f01f, review clean)

Task 5: Ruling: README hosts-entry showed post-move IP 192.168.30.56 while D-132 orders LAN-verify (B) before the move (C) — plan-authored defect, reviewer correct; fix = two-era note in README ("192.168.0.56 until the Pi's DMZ move; flip alongside deploy.sh"). Cost if wrong: a stale doc line, trivial.
Task 5: Ruling: unit comment "(D-132 §4)" was my authoring slip — meant the design spec's §4 / checklist B.4; fix to "(D-132 checklist B)". Minor folded into round 1 as same-file doc text.

Task 5: fix round 1/5 (2 addressed, 0 open — README two-era hosts entry; unit comment cites D-132 checklist B; commits 1822e58..cb9857b)
Task 5: complete (commits 7c1f01f..cb9857b, review clean after round 1)
Task 3 deferred minor resolved: controller ran 'dotnet format --verify-no-changes' solution-wide, exit 0

Final review: Ready-to-merge Yes; 0 Critical, 1 Important (dev-only CSP hash source), minors triaged — none gate exposure.
Final review fix wave scope (one dispatch): (1) SecurityHeaders reads index.html via env.WebRootFileProvider (dev+publish parity, kills the WebRootPath??"" wart); (2) certbot hook gains lineage guard (case $RENEWED_LINEAGE in */cardstock.pro); (3) HostFilteringTests extended with localhost + port-suffixed 127.0.0.1 served cases (closes T4 deferred minor); (4) spec §7 gains one clarifying sentence: host filtering runs in the host startup filter ahead of the app pipeline — behaviorally moot with XForwardedFor-only. Ruling: spec sentence is clarification, not a ruling change.
Ops-phase notes from final review: do NOT run ops/deploy.sh until Task 8 (probe fails during interregnum by design); Task 8 Step 4 browser receipt must include Safari (import-map CSP-hash support is newer there).

Final fix wave: BLOCKED round — Change 1's covering test fixture is shadowed in Development by the static-web-assets composite provider (agent's diagnostic proved the fix itself works: dev CSP now carries the real index.html's 2 hash tokens). Ruling: covering test runs its host as Production (new TestApp EnvironmentName hook) so the physical WebRoot fixture is the provider — the test thereby pins the deployed/publish shape, which is what it always claimed to pin; dev-mode behavior stands verified by the diagnostic. Cost if wrong: a dev-only CSP regression could slip the suite — accepted, mechanism is framework-owned. My dispatch's "WebRoot feeds the provider too" claim was the defect.

Final fix wave: complete (commits cb9857b..a4c4a2c, re-review clean — 4/4 addressed, re-reviewer independently ran 43/43 Api tests + format gate).
Parked (non-blocking, for the ops phase): lineage guard pattern would not match a certbot collision-renamed lineage dir (cardstock.pro-0001) — note at Task 7 install; dev-composite CSP path is hand-verified, not suite-asserted (per Production-test ruling); do not run ops/deploy.sh before Task 8; Task 8 browser receipt includes Safari (import-map CSP hashes).
Ruling: SDD workspace is NOT deleted at code-phase close — Tasks 6–15 (ops, owner-in-the-loop) continue against this ledger; delete at phase end. finishing-a-development-branch inapplicable: main by ruling, owner pushes manually.
CODE PHASE (Tasks 1–5) COMPLETE: 9945f8e..a4c4a2c, final review Ready-to-merge Yes, all gates clean.

Ops: controller confirmed Omada v6.2.14.11 (v5 menu paths in plan Task 10 are stale). v6 mapping (TP-Link doc 109470): LAN networks → Network Config > Network Settings; ACLs → Network Config > Security; VPN/WireGuard → Network Config > VPN (+ top-level VPN Status page); DDNS → Gateway details > Config > Advanced > DNS > Dynamic DNS; switch port profiles → Device Config > Switch > Switch Ports; client fixed IP via client details Config. Screenshot wins over any doc if they disagree.

Ops OPEN ITEM (owner-requested note): the Add-LAN wizard's "Select Device Port" step was deliberately Skipped — the Pi's switch-port assignment to VLAN 30 MUST still happen, via Device Config > Switch > Switch Ports, at the §C move step (after ACLs + the .30.56 reservation). This is D-132 §C's "Pi's switch port → untagged VLAN 30" box; do not close §C without it.

Ops §C progress: DMZ VLAN 30 created (wizard port step skipped per note) · six Gateway ACLs created and screenshot-verified in order 1-6 (rule 1 Manual states Established+Related verified mid-form; groups Dev-Machine 192.168.0.200/32, WG-VPN-Range 10.9.0.0/24, Pi-Mgmt-22-443, Pi-Web-443). Dev machine identified as 192.168.0.200 from Pi ssh sessions. Port-group contents verified behaviorally at move time (ssh-timeout + browse checks).

Ops ruling (owner, 2026-08-20): static IP IS being obtained after all — owner's call, cost factored ("I have no choice if I wanna self host" — noting for honesty: the tunnel-carried site never needs it; it serves the WireGuard endpoint + keeps direct-443 fallback warm). Consequence: DDNS/No-IP drops out of §C entirely — WG endpoint = the static IP, published nowhere. Amends D-132 ruling 2 and the §C WireGuard box's DDNS clause.

Ops fact (owner, 2026-08-20): the WAN is behind CGNAT — the static IP is a HARD requirement for any inbound service (WireGuard included), not a convenience; DDNS would have published an unreachable shared address. Tunnel/crawler work today precisely because outbound-only. Consequence: WG server + peers configured now; the off-network WG receipt (D-132 §C last box) is BLOCKED until the ISP activates the static IP; endpoint goes into the two client configs then.

Ops note for the ACCOUNTS phase (owner question, 2026-08-20): the single free edge rate-limit slot guards /api/v1/cards/*/refresh for the interim. When the auth wall lands, revisit: refresh becomes double-guarded (session + per-account cap), while sign-in/create/resend become the new pre-auth cost doors (credential stuffing; Resend's 100/day quota burnable by resend-abuse). Likely move: the slot goes to the auth/email endpoints. Decide at D-130 build with real paths.

Ops RULING (owner, 2026-08-20, supersedes the revisit-later note above): the one free edge rate-limit slot is configured for the FINISHED site now — one rule "sensitive-endpoints-guard", custom expression (URI path wildcard /api/v1/cards/*/refresh) OR (URI path wildcard /api/v1/auth/*), 10 req/10s per IP, block 10s. BINDING CONSTRAINT on the accounts phase (D-130 build): auth endpoints MUST mount under /api/v1/auth/* so they are born covered by this rule. Record in D-132 tick + carry into the accounts plan.

Ops note (owner question, 2026-08-20): DB-heavy endpoint inventory — /healthz/data runs real count(*) incl. sales (4.6M rows) per hit, publicly; browse's expensive aggregate is already 5-min cached; screener-era heaviness is answered by the D-039 worker precompute (standing architecture: precompute/cache, never throttle reads). Deferred item: a short TTL cache on /healthz/data's counts (app-side, ~10 lines) in a future phase. Edge slot stays on refresh+auth; DB-heavy protection lives app-side where policies are free and unlimited.

Ops finding (Security Settings PDF, 2026-08-20): new dashboard has "Cloudflare managed ruleset: Always active" (no toggle — free protections auto-deployed; §E managed-ruleset box satisfied by platform default). Discovery for the ACCOUNTS phase: "Rate limit authentication requests" template with Leaked Credential Check exists on this zone — evaluate it alongside the auth-path rate-limit move at D-130 build. Security.txt toggle available (off) — optional polish, not launch-gating.

Ops §F progress: cap-trip burst via public URL = exactly 10x404 then 5x429 (edge rule trips at threshold; whole chain proven). SSL Labs first scan B (TLS 1.0/1.1 at edge, Cloudflare default) → owner set Minimum TLS Version 1.2 → rescan A on all four endpoints (pre-HSTS target met). securityheaders.com blocks curl (403) — owner runs it in-browser. Origin-unreachability: satisfied structurally by CGNAT (no inbound path exists) + zero WAN forwards; recorded as the receipt reasoning. Owner asked about Cloudflare's "Enable HSTS" button → answered NO: HSTS is §G, app-side, strictly last.

Ops §F (resumed in background job after main session died mid-reply; owner re-sent "fixed"): SSL Labs A independently re-verified — cached result testTime 18:29 shows A×4 no warnings; direct probes: TLS 1.0/1.1 refused, 1.2 → HTTP/2 200, 1.3 negotiates via openssl (macOS curl --tls-max 1.3 quirk = false negative, disregard). Trackers check DONE with receipts (legal prototype :55-56 vs live CSP connect-src 'self' + package closure clean + unit env = ASPNETCORE_ENVIRONMENT only; __cf_bm cookie note banked for D-130 #7 legal pass). securityheaders.com still 403s curl w/ browser UA; Chrome extension not connected (background job) → stays owner-in-browser. Mac currently on iPhone hotspot (172.20.10.x) — no LAN path to Pi (ssh timeout expected, not an incident; site fine via tunnel). LEDGER CONSOLIDATED + committed: §E ticked (Resend box = all-but-send), §F ticked (SSL Labs/sweep-structural/resolve-structural/burst/trackers), amendments (a) static IP + CGNAT (b) auth-path binding (c) healthz TTL-cache note; D-130 entry gains carried-in constraint. Outstanding: owner → securityheaders grade, phone journey (closes §D too), Resend key; ISP → static IP (unblocks WG receipt + real sweep); then §G HSTS ramp, §H closeout.

Ops RULING (owner, 2026-08-20, supersedes the static-IP ruling above): NO static IP after all — the tunnel proved outbound-only serves the site, and remote management = Raspberry Pi Connect (rpi-connectd, already running; the earlier keep-or-disable decide-point resolves as KEEP, as the sole remote door). WireGuard is VOID (CGNAT + no static IP = no endpoint ever). ACL rule 4 + WG-VPN-Range group stay dormant (harmless; revive only if a routable address ever exists). §F structural receipts (port sweep / --resolve) become permanently satisfied by CGNAT. Trade-offs stated to owner: browser shell only (no rsync/scp/DB tunnel remotely — deploys stay at-home); RPi ID is the credential → 2FA asked. Next home session: verify rpi-connect status + run HSTS rung 1.
Ops: cloud routine "cardstock-hsts-raise-day" (trig_01PTyr6Xg7M1XyPfW25ZN8Am) fires once 2026-08-27T14:47Z (9:47am CDT): public-side quiet-week check + reminder report; the raise itself stays a local/Pi-Connect action per D-132 §G. Local CronCreate rejected — session-only, dies with the session.
