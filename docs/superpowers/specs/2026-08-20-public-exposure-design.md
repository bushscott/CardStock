# Public exposure — design

**Date:** 2026-08-20 · **Status:** owner-approved section by section (brainstorm session, 2026-08-20)
**Phase:** D-129 — ships **before** Accounts + watchlists (D-130 inherits this phase's TLS, headers, hardened unit, and sending domain).
**Definition of done:** the go-public checklist in **D-132**, ticked item by item (D-129's own criterion). This spec is the design; the ledger holds the live tick-list — one authoritative copy, no drift.
**Build references:** D-129 · D-037 (backbone) · D-062/D-084 #1 (abuse shape) · D-036 + D-131 (blast radius; Postgres correction) · D-069 (backups deferred, trap note) · `../PokemonInvestBatch/ops/` (deploy conventions).

**Schema changes: none.** No tables, no columns, no grants, no migrations. This phase is DNS, network, TLS, systemd, and app middleware only.

---

## 0. Scope

Everything between "the app answers on the LAN" and "the public reaches it safely": the Cloudflare zone and tunnel, the browser-trusted certificate, HTTPS-only Kestrel on **443** (amends D-129's "5180" — owner, 2026-08-20), the Omada DMZ and ACLs, the Postgres lockdown, WireGuard remote access, forwarded headers + security headers + the interim abuse posture, pre-staged email DNS on **Resend**, the systemd hardening pass, and outside-in verification.

**Constraint, owner's call:** everything Cloudflare-side runs on the **free tier only**. No paid feature appears anywhere in this design.

**Out of scope:** the accounts build (banked, D-130) · marketing screens · serving card images (D-010 open; one ToS note in §2) · backups (D-069 stands, owner-ruled; §11 carries the recorded trap note, not a re-raise) · legal/licensing (D-059) · the scraper's own systemd unit (sibling repo's property).

**Interim posture (D-129, eyes open):** until the accounts wall lands, the public product is the anonymous read-only app. The guardrails on express-refresh are §3's edge rules plus §7's per-IP cap — the harm from enumeration falls on PriceCharting, and CardStock is the only guardrail (D-062).

---

## 1. Facts established this session (receipts in D-131/D-132)

- **Domain:** owner owns `cardstock.pro`, registered at Namecheap, on default `registrar-servers.com` nameservers with only parking records (`dig NS/A`, 2026-08-20). Registering is off the checklist; the zone starts clean — **it has never contained the home IP**, so there is no DNS history to leak.
- **Pi listeners** (`ss -tlnp`, 2026-08-20): `5432` on **all interfaces** (with `pg_hba` allowing `192.168.0.0/24`) — contradicts D-036's "loopback by construction"; correction filed as **D-131**, lockdown ruled by owner (§6). `5180` all interfaces (the app, pre-443). `5155` **loopback-only** (intake API — confirmed correct, untouched). `22` all interfaces.
- **Gateway:** TP-Link **ER605 v2**, current firmware. WireGuard support arrived in firmware 2.1.1; Omada gateway ACLs support stateful match-state, and TP-Link documents the unidirectional-VLAN recipe this design uses. DDNS (No-IP/custom) is built into the controller.
- **Cloudflare free tier** (Cloudflare docs, 2026-08-20): Tunnel free · 5 WAF custom rules (no regex/Log) · Free Managed Ruleset · **1** rate-limiting rule (IP-keyed, 10 s window) · Bot Fight Mode (blunt, non-configurable) · post-2023 ToS restricts the CDN only for large non-HTML files hosted outside Cloudflare.
- **Resend free tier:** 3,000 emails/month, 100/day, one custom domain — production-usable for D-130's verify/reset/change volume. (Postmark's 100/month hard stop fails the "signup #101 must still verify" test.)

---

## 2. The route: Cloudflare Tunnel — free tier, DMZ underneath

`cloudflared` on the Pi dials **out** to Cloudflare; the WAN stays closed. Chosen over direct-443 on owner approval, for this product's specific facts:

| For the tunnel | Weight |
|---|---|
| Home IP stays out of public DNS — the same IP is the crawler's egress, and D-062 records the asymmetric harm if PriceCharting ever blocks it (`sales`/`populations` unrebuildable) | decisive |
| Volumetric DDoS dies at the edge; with direct-443 it saturates the home uplink before any Omada rule can act | decisive |
| Edge bot rules + rate limit + managed WAF satisfy D-129's interim requirement before packets reach the Pi | strong |
| Zero inbound WAN holes; no static IP needed at all | strong |

**Honest costs, accepted:** Cloudflare terminates TLS at its edge and sees traffic plaintext (negligible for the anonymous read-only interim; an industry-standard trust statement once accounts land). Third-party dependency, no SLA on free. One more systemd unit. ToS flag for the future images question (D-010): a heavily-served 3.6 GB image corpus may belong in R2 or served direct — recorded, not a launch concern.

**Static IP: the phone call is not made.** The tunnel is outbound-only; IP changes don't matter. Recorded in D-132 as *needed only if direct-443 is ever adopted* (the fallback route, kept ready by §4's choice of port 443).

**Why direct-443 lost:** publishes the crawler's IP, absorbs zero DDoS, rests the interim posture entirely on the in-app cap, needs the static IP, opens an inbound port. Buys only end-to-end TLS and no third party — defensible, but worse on these facts.

---

## 3. Cloudflare zone, tunnel, and edge configuration

- **Zone:** `cardstock.pro` on a free account. Namecheap keeps the registration; its only change is Nameservers → Custom DNS → the two Cloudflare-assigned servers. Parking records deleted, not migrated. **Standing rule: no record in this zone ever points at the home IP** (the WireGuard endpoint uses an uncorrelated DDNS name, §5).
- **Tunnel:** `cloudflared` from Cloudflare's arm64 apt repo, running a **remotely-managed** tunnel (config in the dashboard, matching the owner's controller-first working style; the Pi holds only the tunnel token). One public hostname: `cardstock.pro → https://127.0.0.1:443` with `originServerName: cardstock.pro` — cloudflared **verifies** the origin certificate; no verification-disabling flags anywhere.
- **Canonical host:** apex. `www` is a free edge Redirect Rule, 301 → apex. (The origin cert still covers both names, §4.)
- **Edge settings:** SSL/TLS **Full (strict)** · **Always Use HTTPS** on · **Bot Fight Mode** on — explicitly a watched toggle, revocable alone if it challenges someone real · the **one free rate-limiting rule** on the express-refresh path, IP-keyed — the blunt outer wall in front of §7's precise cap · **Free Managed Ruleset** on · the 5 custom-rule slots stay **in reserve** (headroom for what real traffic teaches; nothing promised on them now).
- **API token:** one, scoped Zone → DNS → Edit on `cardstock.pro` only, stored root-only on the Pi. It exists for §4's cert automation and nothing else.

---

## 4. Certificate and HTTPS-only Kestrel on 443

- **Client:** Debian's `certbot` + `python3-certbot-dns-cloudflare` (apt-maintained, stock systemd renewal timer). DNS-01 via the §3 token — issuance and renewal need no inbound port, ever. Cert names: **apex + www** (www never reaches the origin today; including it is one flag now versus a mystery later).
- **Into Kestrel:** certbot **deploy-hook** (fires only on successful issue/renew) copies `fullchain.pem`/`privkey.pem` to app-readable `/etc/cardstock/tls/` and restarts the web unit — a sub-second blip every ~60 days; no hot-reload machinery. Kestrel config:

```json
"Kestrel": { "Endpoints": { "Https": {
  "Url": "https://0.0.0.0:443",
  "Certificate": { "Path": "/etc/cardstock/tls/fullchain.pem",
                   "KeyPath": "/etc/cardstock/tls/privkey.pem" } } } }
```

- **That endpoint is the only listener** — no plain-HTTP sibling (owner's call, D-129). Public http→https upgrading happens at the edge.
- **Port 443 amends D-129's "5180"** (owner, 2026-08-20; the accounts spec's prerequisite line is updated in the same commit). Why: every file naming the port is being rewritten this phase anyway; the owner's direct/VPN URL becomes plainly `https://cardstock.pro` via a hosts-file entry (name → DMZ IP; the cert genuinely matches, so no warnings and no `:port` wart); and the direct-443 fallback stays one port-forward away. Cost: `AmbientCapabilities=CAP_NET_BIND_SERVICE` on the unit (§9) — a narrow low-port-bind grant, compatible with `NoNewPrivileges`. Nothing else on the Pi uses 443 (§1 receipt).
- **HSTS — the recorded trap (D-129), honored as ordering.** Enables only after three receipts: cert live at origin · tunnel serving · SSL Labs pass. App-side `UseHsts` (origin correct independent of Cloudflare): **max-age 86400 first**, then after ≥7 quiet days **31536000 + `includeSubDomains`**. **No `preload`** — a browser-list one-way door with no benefit at this stage. D-132 encodes the sequence as literal ordered steps.
- **CAA:** `0 issue "letsencrypt.org"`. Subtlety: Cloudflare's *edge* certs come from its own CA partners, and Cloudflare documents auto-managing companion CAA entries when CAA exists — so the checklist pairs the CAA step with a same-day verify that the edge cert stays valid, catching on day one what would otherwise surface as a renewal failure weeks later.

---

## 5. Omada topology: DMZ, ACLs, WireGuard

**Networks** (controller: Settings → Wired Networks → LAN → Create; exact menu names re-checked against the installed controller version at execution):

| Network | VLAN | Subnet | Members |
|---|---|---|---|
| LAN (existing) | default | `192.168.0.0/24` | everything today, minus the Pi — untouched |
| **DMZ** (new) | 30 | `192.168.30.0/24` | the Pi, alone |
| **WG-VPN** (new) | — | `10.9.0.0/24` | owner's remote devices |

- Pi: Omada DHCP reservation **`192.168.30.56`** (muscle memory preserved). Dev machine: reservation on the LAN so ACLs can name it. WG range avoids `192.168.x` so remote coffee-shop subnets never collide.
- **The move:** access port — Devices → switch → the Pi's port → untagged VLAN 30. **Pre-move sweep:** `ss -tnp` on the Pi for established LAN-bound flows (know, don't believe, that nothing Pi→LAN exists). **Same-step updates of every old-IP reference:** `~/.ssh/config` (incl. the `pi-db` tunnel entry), `known_hosts`, deploy scripts, the hosts-file entry, the assistant memory file.
- The Pi's DNS/DHCP come from the gateway's own DMZ-side interface (never crosses the wall); Pi outbound (crawl, TCGdex, New Relic, certbot→Cloudflare, cloudflared, NTP, apt) rides DMZ→WAN, open. `.local` mDNS does not cross VLANs — access is by IP/hosts entry, by design.

**Gateway ACLs** — the stateful kind (switch ACLs can't do this), first-match-wins, in this order:

| # | Action | From → To | Ports | Purpose |
|---|---|---|---|---|
| 1 | Permit | DMZ → LAN, state Established/Related | any | return traffic for LAN-initiated connections |
| 2 | **Deny** | DMZ → LAN, all | any | **the wall** — the Pi never initiates into the home network |
| 3 | Permit | dev machine → Pi | 22, 443 | ssh, deploys, DB tunnel, direct app |
| 4 | Permit | WG-VPN → Pi | 22, 443 | remote owner = desk owner, nothing wider |
| 5 | Permit | LAN → Pi | 443 | household devices browse the app |
| 6 | **Deny** | LAN → DMZ, everything else | any | closes ssh-and-all from TVs, guests, IoT |

No rule anywhere mentions 5432 — after §6 there is no network listener to protect. WAN: **zero port forwards** (cloudflared dials out; WireGuard's listener is the gateway's own, cryptographically silent to scanners).

**Verification flags carried into the plan:** controller menu names drift by version — each step checked at execution. Rule 4 depends on the controller exposing the VPN network as an ACL source (TP-Link historically inconsistent); fallback if inexpressible: VPN clients (the owner, key-authenticated) see what LAN devices see — least-privilege polish, not load-bearing security.

**WireGuard** (Settings → VPN → WireGuard): interface `10.9.0.1/24`, one peer per device pinned to its own `/32`. Endpoint: a **No-IP free hostname on the gateway's built-in DDNS, deliberately uncorrelated with "cardstock"** (free-tier cost: ~monthly keep-alive email). Client-side `AllowedIPs` decides per device whether remote-owner routes just the DMZ or the whole house — enrollment-time choice, no server change either way.

---

## 6. Postgres lockdown (owner ruling, 2026-08-20 — files D-131's correction)

- `listen_addresses = 'localhost'` · delete the `host all all 192.168.0.0/24 scram-sha-256` line from `pg_hba.conf` · one restart, timed between crawler visits. **Receipt:** `ss -tlnp` shows 5432 loopback-only.
- On-Pi apps (CardStock, worker) already connect over loopback — unaffected. D-036's "unreachable by construction" becomes true again.
- **Owner access, both flavors preserved:** ad-hoc `ssh` + `sudo -u postgres psql` (local socket — unchanged). The test suite (`POKEMON_TEST_DB`) moves to an SSH forward:

```
Host pi-db
    HostName 192.168.30.56
    User scott
    LocalForward 5433 127.0.0.1:5432
```

`POKEMON_TEST_DB` → `127.0.0.1:5433`; `ssh -fN pi-db` before DB-gated runs. Works identically over the §5 VPN. Residual, stated honestly: while up, the forward is connectable by other processes on the dev machine only (loopback-bound); the threat that exploits it already owns the ssh key. If the tunnel is ever automated, the polish is a dedicated `authorized_keys` key restricted to port-forwarding with no shell.

---

## 7. App changes

- **Real client IPs (D-129's forwarded-headers item):** through the tunnel every request reaches Kestrel from `127.0.0.1`; uncorrected, the per-IP cap sees one address for all of humanity. Forwarded-headers middleware, **first in the pipeline**: read **`CF-Connecting-IP`** (Cloudflare sets it authoritatively; client-supplied values are overwritten at the edge), trust **loopback only** as proxy, forward-limit 1. Direct LAN/VPN connections arrive from non-proxy addresses and are correctly left alone; a LAN client forging the header is ignored. Tests: header honored from loopback, ignored from elsewhere.
- **Interim abuse posture, layered:** edge rules (§3) outside, and a **review-and-tighten pass on the existing per-IP cap** (exact number at plan time, from the current middleware) holding D-062's principle — a person browsing hard never meets it; scripted enumeration trips within minutes.
- **Security headers — app middleware, deliberately not Cloudflare rules** (origin correct independent of the edge; direct access gets identical treatment). Enforced from day one, tested over direct LAN before the public arrives:
  - **CSP:** `default-src 'self'` · `script-src 'self' 'wasm-unsafe-eval'` (the WASM runtime allowance and nothing else — the TradingView bundle is self-hosted, D-084 #7) · `style-src 'self' 'unsafe-inline'` (inline style *attributes* are load-bearing — census bar widths, D-084 #8; the XSS risk lives in `script-src`, which stays strict) · `img-src 'self' data:` · `connect-src 'self'` · `frame-ancestors 'none'` · `base-uri 'self'` · `form-action 'self'`.
  - **X-Frame-Options: DENY** · **X-Content-Type-Options: nosniff** · **Referrer-Policy: strict-origin-when-cross-origin** · minimal **Permissions-Policy** (camera/mic/geolocation off). HSTS under §4's gate.
- **Pipeline order (load-bearing):** forwarded-headers → host filtering → security headers → rate limiter → app. Cap and logs both see true IPs.
- **Host filtering:** `AllowedHosts` = `cardstock.pro` + localhost. Raw-IP requests get 400, not a rendered page. **No CORS policy ships** — the API is same-origin under one host; the correct amount of CORS for that is none.
- **Free enforcement win:** `connect-src 'self'` makes the legal page's reported "no third-party trackers" promise *structural* client-side. The checklist keeps the verify step (read the actual prototype copy; confirm the server sends nothing either — D-037's New Relic note).

---

## 8. Email-auth pre-stage — Resend (owner pick, 2026-08-20; settles D-130 #5's open provider)

- **Now, in this phase:** free Resend account · add `cardstock.pro` · publish the records Resend issues (DKIM keys; its SPF include typically lands on a `send.` subdomain, leaving the apex strict) · apex **SPF `v=spf1 -all`** stays · **DMARC `v=DMARC1; p=reject`** · **null MX** (`0 .`, RFC 7505). One **test send** to the owner's gmail proves DKIM signs and DMARC passes before any product email exists. Until the accounts phase, the domain is *unspoofable*, not merely unused.
- **Inherited by the accounts phase:** a verified sending domain; code only (`IEmailSender` against Resend's REST API). Flag for that phase: revisit the null MX against deliverability guidance (some receivers penalize From-domains that can't take bounces).

---

## 9. systemd hardening (D-037's list lands)

Web unit: dedicated non-root user · `NoNewPrivileges` · `ProtectSystem=strict` with carve-outs fitted to the real deploy layout at plan time (DataProtection keyring, TLS dir read) · `PrivateTmp` · `MemoryMax` (web can never starve the crawler) · `AmbientCapabilities=CAP_NET_BIND_SERVICE` (§4). Receipt: `systemd-analyze security` score before/after. cloudflared's stock unit stays stock; the scraper's unit is the sibling repo's and is not touched.

---

## 10. Outside-in verification (scanners over memory — D-129's own reasoning)

From networks that aren't the owner's: **SSL Labs** (A pre-HSTS, A+ after the ramp) · **securityheaders.com** (A) · full user journey on a phone off wifi, including a refresh · **external port sweep** of the home IP: silent · **`curl --resolve cardstock.pro:443:<home-ip>`** from outside: unreachable — the receipt that the origin exists only behind the tunnel · a **scripted burst** at the refresh endpoint until the cap trips — one probe exercising the whole chain (edge rule → forwarded headers → app cap) · email-auth records validated by an external checker + the Resend test-send headers.

---

## 11. Definition of done and closeout

**The go-public checklist is D-132** — ordered so the traps are structurally unhittable (zone first; Pi prep LAN-verified before exposure; topology; tunnel up; edge posture + email DNS; scans; **HSTS strictly last**). Closeout items on it: D-037 marked largely closed with what remains named · D-131 cross-referenced · D-129 amendments recorded (443; static-IP call unnecessary-unless-direct-443) · **one recorded-risk line, not a re-raise:** the box going public holds `sales`/`populations` data unbacked by owner ruling (D-069) — on the record the day the door opens.
