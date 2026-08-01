# EveryTongue — Future Architecture (updated 2026-08-02)

> This document describes what is **not yet built**. Completed work is pruned on sight — history lives in git and PLAN.md. Pruned so far (all shipped by v2.7.x–v2.11.x): pluggable engine registries, cross-GPU STT (CUDA→Vulkan→CPU), the Core/desktop/Lite three-project split, the headless Lite server with web admin, the Docker image (ghcr, amd64+arm64), and the zero-GPU quadrant — solved via **web-mic broadcast + one-shot cloud transcribe** rather than the originally-planned phone-side Web Speech API (kept below as an open idea).

---

## Competitive Landscape

### Direct Competitors (conference real-time translation)

**Microsoft Translator Live** — the closest free comparison. Presenter speaks, audience joins via a code on their phones, gets real-time translated captions. Free for up to 100 participants.
- Strengths: free, polished, good language coverage, easy setup
- Weaknesses: cloud-only (requires internet for every participant), no offline mode, no self-hosting, no PA mic integration, no data privacy (audio sent to Microsoft), no multi-room conference management
- Threat level: **high for online use cases** — if the venue has reliable internet, this is what most people reach for today

**Wordly** — AI-powered real-time translation specifically built for conferences and events. Attendees use phones/browsers. Closest product match to EveryTongue's feature set.
- Strengths: purpose-built for conferences, good accuracy, professional support
- Weaknesses: cloud-only, subscription pricing (~$2,000-5,000+ per event or annual enterprise contracts), aimed at corporate conferences, no offline mode, no self-hosting
- Threat level: **low for our users** — pricing rules it out for charities and developing-world organisations

**KUDO / Interprefy** — interpretation platforms focused on **human interpreters** with AI assist. The model is: you still hire professional interpreters, the platform manages audio routing and remote interpreter access.
- Strengths: highest quality (human interpreters), professional-grade
- Weaknesses: very expensive (interpreter fees + platform fees), enterprise-oriented, not AI-first
- Threat level: **none** — completely different market segment

### Adjacent Products

| Product | What it does | Why it's different from EveryTongue |
|---------|-------------|-------------------------------------|
| Google Translate conversation mode | Real-time speech translation on a phone | 1-to-1 only, not conference broadcast |
| Zoom/Teams/Webex translation | Built-in AI captions with translation | Requires their platform, cloud-only, not for in-person events |
| SpeakSmart, Lingmo, Timekettle | Hardware translation earbuds/devices | Expensive per-unit, not scalable to audience |
| Amazon Transcribe + Translate | AWS services (STT + translation APIs) | Raw APIs, requires custom development, cloud-only, pay-per-use |

### The Gap EveryTongue Fills

Nobody else combines **all** of these:

- **Offline / self-hosted** — no cloud dependency, no internet required
- **Free** — no subscription, no per-event cost, no per-seat licensing
- **PA microphone input** — real conference setup, not phone-to-phone
- **Broadcast to unlimited phones** via local WiFi only
- **Runs on modest/donated hardware** — not just high-end servers
- **Multi-room support** — multiple concurrent sessions at the same conference
- **Data privacy** — audio and translations never leave the local network
- **Open architecture** — pluggable engines, not locked to one vendor's STT/translation

### Target Users (underserved by existing solutions)

- International churches and mission organisations
- Charities and NGOs running multilingual events in developing countries
- Conference centres in areas with unreliable or no internet
- Organisations that can't afford $2,000+ per event (Wordly) or professional interpreters (KUDO)
- Deployments where data privacy matters (audio never leaving the building)
- Sites relying on donated/second-hand hardware

### Strategic Position

The main competitive risk is Microsoft Translator Live — it's free and covers the online conference case well. EveryTongue's defensible advantages are:

1. **Offline capability** — the single strongest differentiator. No internet needed at the venue.
2. **Self-hosted / data privacy** — audio stays on the local network, never reaches a cloud provider.
3. **PA mic integration** — designed for real conference audio setups, not phone microphones.
4. **Multi-room management** — conference templates, room governance, host controls for complex multi-session events.
5. **Cost** — free software on donated hardware vs recurring SaaS subscriptions.
6. **Flexibility** — open engine architecture means the best available STT/translation/TTS can be plugged in as technology improves, not locked to one vendor.

With Lite/headless/Docker shipped, EveryTongue now competes in Microsoft Translator Live's online territory too — while retaining the offline capability that Microsoft can't match.

---

## Cloud Relay — Shared GPU Server (NOT BUILT — the last open roadmap phase)

### Concept

A charity runs **one** GPU-powered EveryTongue server (cloud-hosted or at HQ). Field offices connect to it instead of running local engines.

```
[Field Office A - RPi]     --\
[Field Office B - laptop]  ---+--> [Cloud EveryTongue Server - GPU]
[Field Office C - phones]  --/
```

### Two Deployment Patterns

**Pattern 1: Local relay server**
Each field site runs a local Lite server for room management and local WiFi. Heavy compute (STT, translation) is relayed to the cloud instance via API.

- Local server handles: rooms, lobby, WebSocket hub, static files, local WiFi
- Cloud server handles: Whisper STT, NLLB translation, TTS synthesis
- Advantage: works with intermittent internet (rooms stay up, translation queues when connectivity drops)
- Implementation: `CloudRelaySttBackend` and `CloudRelayTranslationBackend` that forward requests to a remote EveryTongue server's REST API

**Pattern 2: Direct cloud connection**
Phones connect directly to the cloud server over the internet. No local server at all.

- Simplest setup: just give people the URL
- Disadvantage: requires stable internet for all participants, higher latency
- Best for: remote/hybrid conferences where participants aren't co-located
- Status: the Lite container can already be cloud-hosted (this is just hosting, not new code) — the cloud web-mic port exists but is **unvalidated** (see PLAN.md); Stage 6 hardening (domain, Caddy, forced PIN change) applies before any off-site hosting.

### Cost Model

- A single T4 GPU cloud instance (~$150-300/month) can serve 3-4 concurrent STT streams
- Amortised across 10+ field offices, that's $15-30/month per site
- Spot/preemptible instances can reduce this further for scheduled events
- The server only needs to run during events, not 24/7

### Roadmap items

1. **Cloud relay backends** — `ISttBackend` and `ITranslationBackend` that forward to a remote EveryTongue server's API.
2. **Hub-and-spoke deployment** — one cloud GPU server, multiple field Lite servers connecting to it.
3. **Direct cloud mode validated** — phones connect to a cloud-hosted server with no local server needed.

---

## Phone-Side Web Speech API STT (NOT BUILT — open idea from the original Lite plan)

The shipped zero-GPU story (web-mic → cloud STT engine) requires a **BYO cloud STT key**. The Web Speech API path would remove even that: the speaker's phone transcribes locally via `webkitSpeechRecognition` (free Google/Apple recognition, no API key) and sends **text** to the server, which only translates and broadcasts.

- Server needs: no audio capture, no STT engine, no key — just NLLB on CPU. True $5-VPS / Raspberry-Pi territory.
- The text-in path already exists (conversation-room text chat) — the new code is phone-side only.
- Tradeoffs: speaker's phone needs internet; major languages only; weaker than Whisper/Speechmatics in noise or with accents.
- Worth building when a deployment appears that can't obtain any cloud STT key (all major vendors have free tiers, so this has not yet been the blocker in practice).

---

## Second-Hand Hardware Strategy

For charities that can't afford new equipment but can source donated/second-hand hardware:

- **GTX 1060/1070** (~$50-80 used) — runs whisper.cpp with `small` model comfortably. Single-stream real-time conference STT.
- **GTX 1080/1080 Ti** (~$100-150 used) — runs `medium` or `large-v3` model. Multi-stream capable.
- **Any modern laptop** (i5/Ryzen 5, 2020+) — runs whisper.cpp CPU with `base`/`small` model for single-stream conference use. Clean PA audio helps accuracy.
- **Raspberry Pi 5** (~$80 new) — runs Lite mode (server relays to cloud engines via web-mic). Adequate for conversation rooms.

---

## Cost Analysis — Local vs Cloud vs Hybrid

The assumption that "cloud is cheaper" doesn't hold in all scenarios. The right answer depends on usage patterns, number of sites, and whether internet is available at all.

### Option A: Local Hardware (one-time cost)

| Setup | Cost | Ongoing | Notes |
|-------|------|---------|-------|
| Used gaming laptop (GTX 1060) | $200-400 | ~$5-15/month power | Full offline conference. Single-stream STT. |
| Desktop + used GTX 1070 | $200-250 | ~$5-15/month power | Same capability, cheaper if parts available |
| Desktop + used GTX 1080 Ti | $300-400 | ~$10-20/month power | Multi-stream, `large-v3` model |
| Any modern laptop (no GPU) | $200-400 | ~$5/month power | whisper.cpp CPU, `base`/`small` model, single-stream |
| Raspberry Pi 5 + accessories | ~$100 | ~$1/month power | Lite mode only (conversations, no PA mic STT) |

**Year 1 total**: $200-500. **Year 2+**: $60-180 (just power). Works offline. No subscriptions. Hardware can be donated.

### Option B: Cloud GPU (recurring cost)

| Provider | Instance | Cost/hour | 4hr weekly conference | Daily 2hr meeting |
|----------|----------|-----------|----------------------|-------------------|
| AWS g4dn.xlarge (T4) | On-demand | ~$0.50/hr | ~$8/month | ~$30/month |
| AWS g4dn.xlarge (T4) | Spot | ~$0.15/hr | ~$2.50/month | ~$9/month |
| Azure NC4as T4 | On-demand | ~$0.50/hr | ~$8/month | ~$30/month |
| GCP g2-standard-4 | On-demand | ~$0.55/hr | ~$9/month | ~$33/month |

**Year 1 total**: $100-380. **Year 2 total**: same again. Never stops costing. Requires internet at venue.

Spot/preemptible instances are cheapest but can be interrupted — risky for live conferences. On-demand is reliable but costs more.

### Option C: Cloud CPU / Lite (cheapest recurring)

| Setup | Cost/month | Capability |
|-------|-----------|------------|
| Basic VPS (2 vCPU, 4GB RAM) | $5-20/month | NLLB translation on CPU. Cloud STT via web-mic. Conversation rooms. |
| Larger VPS (4 vCPU, 8GB RAM) | $20-40/month | Faster NLLB. Could run whisper.cpp CPU for single-stream. |

**Year 1 total**: $60-480. Requires internet for all participants.

### Break-Even Analysis

| Scenario | Cloud wins until... | Then local wins |
|----------|-------------------|-----------------|
| Single site, weekly use | ~12-18 months | Used laptop pays for itself |
| Single site, daily use | ~3-6 months | Local hardware pays off fast |
| 10 field offices | ~18-24 months | 10 laptops ($3,000-5,000) vs shared cloud ($100-300/month) |
| Occasional use (monthly events) | ~3+ years | Cloud stays cheaper for rare use |

### The Hidden Costs

**Cloud hidden costs:**
- Internet at the venue (not always available, not always reliable)
- Latency — audio round-trip to cloud adds 200-500ms on top of processing time
- Data transfer costs (audio streaming to cloud adds ~$1-5/month)
- Vendor lock-in and price changes
- Downtime risk — cloud outage during a live conference is catastrophic

**Local hidden costs:**
- Hardware failure or theft (real risk in field conditions)
- Maintenance and setup at each site (needs someone technical)
- Shipping/transporting hardware to remote locations
- Power reliability (some field locations have unreliable electricity)
- Software updates need to be applied at each site

### The Verdict

**There is no single right answer.** The best deployment depends on the site:

| Situation | Best option |
|-----------|-------------|
| Remote village, no internet | Local hardware (the only option) |
| Urban church, reliable internet, tight budget | Cloud on-demand for conferences, Lite mode locally for conversations |
| Organisation with 10+ sites, central IT team | Hub-and-spoke: shared cloud GPU + local Lite servers at each site |
| One-off event or trial deployment | Cloud (no upfront cost, spin up and test) |
| Permanent installation, daily use | Local hardware (pays for itself within months) |
| Unreliable power + unreliable internet | Local laptop with battery + Lite mode as fallback |

### The Hybrid Recommendation

For most charity deployments, the answer is **both**:

1. **Each field site runs Lite mode locally** — cheap hardware (RPi or old laptop), offline-capable, handles conversation rooms and daily meetings. ~$100-400 one-time cost.
2. **The organisation runs one shared cloud GPU server** — spun up on-demand for conferences that need full Whisper STT from a PA mic. $2-30/month depending on frequency. All field sites can connect to it when internet is available. *(This is the cloud-relay phase — the one remaining unbuilt piece.)*
3. **Sites with donated GPU hardware run Full mode locally** — completely independent, no cloud needed, no ongoing cost.

This gives every site a working baseline (Lite mode) while sharing the expensive resource (GPU compute) across the organisation. Sites gradually move to local Full mode as hardware becomes available through donations or purchases.

---

Every Tongue, every budget, every platform.
