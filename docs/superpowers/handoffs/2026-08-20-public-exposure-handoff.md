# Handoff — brainstorm the Public exposure phase (D-129)

**Written:** 2026-08-20, closing the session that designed Accounts + watchlists.
**For:** a fresh CardStock session. Invoke `superpowers:brainstorming` for the **Public exposure**
phase and start here. Owner in the loop; one question at a time (he deliberates in prose — discuss,
don't re-pose option dialogs).

## Where things stand

- **D-129 (read it first)** reordered the build: Public exposure ships **before** Accounts +
  watchlists. The accounts design is finished and banked —
  `docs/superpowers/specs/2026-08-20-accounts-watchlists-design.md`, rulings in **D-130**, commit
  `eb6aee6`. Nothing builds against it until this phase ships. Do not reopen it.
- This phase's brainstorm barely started: one fact-gathering exchange, then the owner called for
  this handoff.

## Facts gathered from the owner (2026-08-20, this session)

1. **Networking is full TP-Link Omada**, all managed through the **Omada controller**. Owner:
   model numbers available but likely unnecessary — "everything we're gonna do is in the Omada
   controller." Design the isolation as concrete controller steps (VLANs, ACLs, port forwarding),
   not generic firewall advice.
2. **Static IP is one ISP phone call away.** Owner: "when we need that, just tell me I will do
   it." Make it an explicit, timed checklist step.
3. **The domain question was asked and NOT yet answered** — re-ask it first: *does he already own
   a domain, or is registering one part of the phase?* Why it matters: the registrar doubles as
   the DNS API for the DNS-01 cert automation and the email-auth records (SPF/DKIM/DMARC), and
   registering fresh **at Cloudflare** would put DNS, cert path, and the tunnel/WAF evaluation he
   asked for under one roof.

## Scope (D-129 carries the full list; highlights)

Owner's words: firewall rules; VLANs — a DMZ isolating the Pi from the home network; the static
IP; and **evaluating what Cloudflare adds** (this is a research task he assigned — do it, then
recommend tunnel vs direct-443). Migrated in from the accounts brainstorm (need only a domain, not
exposure): DNS-01 trusted cert · HTTPS-only Kestrel on 5180, no plain-HTTP listener (owner's
explicit call) · HSTS — **the recorded trap: never enable before the trusted cert; HSTS +
self-signed = hard lockout** · CAA · pre-staged SPF/DKIM/DMARC · response security headers (CSP
with Blazor-WASM allowances, XCTO, Referrer-Policy, X-Frame-Options) · systemd hardening (D-037's
list) · outside-in scans (SSL Labs, securityheaders.com, phone off wifi) as the mechanical
completeness check.

**Definition of done: a written go-public checklist in DECISIONS.md, ticked item by item.** D-037
is the backbone and largely closes with this phase.

**Interim posture (accepted with eyes open, D-129):** until the accounts phase walls the app, the
public product is the anonymous read-only app. Express-refresh rides its per-IP cap (D-084.1) —
this phase must add Cloudflare bot rules and/or a tighter interim cap per D-062. The harm from
enumeration falls on PriceCharting; CardStock is the only guardrail. If the tunnel route wins,
forwarded-headers middleware lands here or per-IP limits go blind.

## Constraints a fresh session will need

- **Pi:** `192.168.0.56`, app at `http://…:5180` (becomes `https://` + domain). Postgres and the
  worker's intake API are loopback-bound — keep them that way (verify, don't change).
- **Moving the Pi to a DMZ VLAN likely changes its subnet/IP.** Things that must survive via ACL
  and config: owner's dev machine → Pi ssh + psql + test databases (`pg_hba.conf` currently
  allows the LAN subnet — memory: DB testing happens on the Pi only, `cardstock_tester` role);
  deploys (`ops/deploy.sh`); the owner's browsing devices → 5180; Omada controller ↔ Pi's switch
  port. Pi outbound: pricecharting crawl, TCGdex, New Relic OTLP (sibling `ops/README.md:9`),
  NTP/apt — plus, after this phase, the DNS provider's API (cert renewal) and `cloudflared` if
  the tunnel wins.
- **Ledger context to load:** D-129 (the phase), D-130 (what phase B expects from this one),
  D-037 (security checklist — backbone), D-036 (same-box blast radius, accepted), D-062 (express
  abuse shape), D-070 (Pi environment: Postgres 15.18, max_connections 100), D-069 (backups
  deferred — owner ruled; do not re-raise, but the go-public checklist records the trap note).
- **D-059 stands: do not raise legal/licensing.** Owner instruction on record.

## Suggested opening moves

1. Re-ask the domain question (above).
2. Cloudflare evaluation (web research fine): free-tier tunnel, WAF/bot rules, DDoS, origin
   concealment — versus direct 443 on the static IP with Omada DMZ + Let's Encrypt. Lead with a
   recommendation.
3. Design the Omada topology (VLAN layout, ACLs between DMZ/LAN/dev machine) as controller steps.
4. Draft the go-public checklist → DECISIONS.md; it is the phase's definition of done.

After this phase ships: a fresh session runs `superpowers:writing-plans` against the banked
accounts spec (D-130).
