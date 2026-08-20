# ADR-0003: The app goes public through a Cloudflare Tunnel, with the Pi in a DMZ

**Date:** 2026-08-20
**Status:** Accepted

## Context

D-129 pulled "securely bringing this product to the public" out of the accounts phase and made it
a phase of its own, so security work structurally cannot stay deferred. The app this decision
exposes is the anonymous read-only product at `http://192.168.0.56:5180` — a Blazor WASM client
and its API on a Raspberry Pi 5 that also runs the crawler and Postgres (D-036, blast radius
accepted), on a residential connection behind TP-Link Omada gear (ER605 v2 gateway; VLANs,
stateful gateway ACLs, and WireGuard all controller-managed).

Four facts shape the decision more than generic hosting advice does:

1. **The site's IP would be the crawler's IP.** With an ordinary port-forward, `cardstock.pro`
   resolves in public DNS to the same address the crawler uses on pricecharting.com. D-062
   records the asymmetry: if that address is ever blocked, `sales` and `populations` stop
   accumulating and can never be rebuilt from any source.
2. **A home uplink absorbs nothing.** A volumetric flood saturates the pipe before any on-prem
   rule can act; no Omada configuration helps once the link itself is full.
3. **D-129's interim posture needs bot rules.** Until the accounts wall lands (D-130, banked),
   express-refresh rides a per-IP cap and CardStock is the only guardrail in front of
   PriceCharting — the phase must add edge bot rules and/or a tighter cap (D-062).
4. **The owner's constraint: free tier only.** Whatever the edge provider adds must cost nothing.

Two findings from the same session sharpen the box's own posture: Postgres was **not**
loopback-bound — D-073's test databases had opened `listen_addresses = *` with a whole-LAN
`pg_hba` grant, unrecorded until verified by `ss -tlnp` (D-131) — and the worker's intake API
was confirmed loopback-only at `127.0.0.1:5155`, as designed.

The domain exists: `cardstock.pro`, owned, registered at Namecheap, carrying only parking
records — the zone has never contained the home IP, so there is no DNS history to leak.

## Decision

**Inbound traffic arrives only through a Cloudflare Tunnel, on the free tier.** `cloudflared`
runs on the Pi as a systemd unit and dials out; the WAN has **zero port-forwards**. The zone
moves to Cloudflare by nameserver delegation (registration stays at Namecheap). The edge runs
Full (strict) TLS, Always Use HTTPS, Bot Fight Mode, the Free Managed Ruleset, and the free
plan's one rate-limiting rule on the express-refresh path — D-129's interim requirement,
enforced before packets reach the Pi. A standing zone rule: **no DNS record ever points at the
home IP.**

**The origin is HTTPS-only Kestrel on 443** (D-132 amends D-129's "5180"), serving a Let's
Encrypt certificate issued and renewed by DNS-01 through a Cloudflare API token scoped to this
zone — no inbound path is ever needed for certificates. cloudflared connects to
`https://127.0.0.1:443` and verifies that certificate (`originServerName`); nothing disables
verification anywhere. HSTS ships config-gated at 0 (off) and turns on only after the cert,
tunnel, and outside-in scans all hold — the ordering is D-129's recorded lockout trap, encoded
as literal checklist sequence (D-132 §G).

**The Pi moves to a DMZ VLAN** (VLAN 30, `192.168.30.56`) behind stateful one-way gateway ACLs:
the LAN may open named ports into the DMZ (dev machine → 22/443, household → 443); the DMZ may
never initiate into the LAN. **Postgres returns to loopback-only** — `listen_addresses =
'localhost'`, the LAN `pg_hba` line deleted — restoring D-036's "unreachable by construction"
that D-073 had silently spent; the owner's test-database workflow survives over an SSH local
forward (D-131). The intake API stays loopback. No ACL anywhere mentions 5432, because after
the lockdown there is no network listener to protect.

**Remote management is WireGuard on the ER605** (firmware ≥ 2.1.1), with VPN clients granted
exactly the dev machine's ports and nothing wider, reachable at a DDNS hostname deliberately
uncorrelated with "cardstock". **The static-IP call is not made** — the tunnel is
outbound-only, so IP changes don't matter; the call is recorded as needed only if direct-443
is ever adopted.

**The app pays the tunnel's one tax:** forwarded-headers middleware reading `CF-Connecting-IP`
with loopback as the only trusted proxy, so the per-IP cap and the logs see real visitors — a
direct LAN connection keeps its socket address and a forged header from one is ignored. It
ships in the same phase as the tunnel because either without the other is wrong (D-129).

## Alternatives considered

**Direct-443** — static IP, port-forward 443 to the DMZ, same DNS-01 certificate. Rejected: it
publishes the crawler's egress IP in public DNS for anyone to correlate (fact 1), absorbs no
volumetric attack (fact 2), rests the interim abuse posture entirely on the in-app cap, requires
the static-IP purchase, and opens an inbound TCP port on a home network. What it buys —
end-to-end TLS with no third party in the path — is real but loses on these facts. It remains
the **recorded fallback**, kept one port-forward away by putting the origin on 443 now.

**Tailscale on the Pi for management** instead of gateway WireGuard. Rejected while the ER605
supports WireGuard natively: the Omada-native route keeps management in the controller the owner
already runs, with no extra daemon or third-party control plane on the Pi. Noted as the fallback
if gateway WireGuard disappoints in practice.

**Paid edge tiers** — out by the owner's constraint; nothing in the design depends on one.

## Consequences

- The home IP appears in no public record tied to the product; the crawler's egress and the
  site's address are decoupled (the D-062 asymmetry is contained).
- Volumetric attacks terminate at Cloudflare's edge; the WAN stays closed; certificate renewal
  works from behind a fully closed firewall, forever.
- The whole-LAN Postgres attack surface D-131 found is gone, and no gateway rule has to defend
  a database port again.
- **Accepted costs:** Cloudflare terminates TLS at its edge and sees traffic in plaintext —
  negligible for the anonymous read-only interim, an industry-standard trust statement once
  accounts land. A Cloudflare outage takes the site down, with no SLA on free (a home-uplink
  outage does the same on any route). One more daemon (`cloudflared`) to keep updated.
- The free CDN's terms restrict serving large non-HTML files hosted outside Cloudflare — a
  recorded flag for the card-images question (D-010), not a launch concern.
- Until the accounts phase, the public product is the anonymous read-only app under the layered
  interim posture (edge rules + the tightened per-IP cap), accepted with eyes open in D-129.
- ADR-0002 is unaffected; its cookie's `Secure` flag becomes real the day this ships.

The build reference is `docs/superpowers/specs/2026-08-20-public-exposure-design.md`; the
rulings and the ticked go-public checklist are D-132; the Postgres correction is D-131.
