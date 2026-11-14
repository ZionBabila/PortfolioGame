# Interview Battles: core loop & mechanics (draft v1, 2026-11-14)

Goal: turn the walk-around portfolio into a short, playful **mobile** game, without hiding the portfolio.
Every "weapon" in the game is a real skill or project, so a recruiter who plays for three minutes has
seen the portfolio without noticing.

## Pillars
1. **3 minutes to the offer letter.** A full run (3 bosses) fits a coffee break. Each battle: 3-5 turns.
2. **The content is the cards.** Every card is a real project or skill, and every card has *Show me*
   (opens the real project page). Fighting = browsing.
3. **One thumb.** Portrait, drag a die onto a card. No timers, no reflex tests.
4. **Workshop Letters look.** Paper, typewriter, rubber stamps, single-stroke line art (the existing UI kit).
5. **No losing the portfolio.** A lost battle costs nothing but a funny rejection letter; retry instantly.

## Core loop
```
Explore a station ──► earn its card(s) ──► floor boss: interview battle ──► win: next floor + reward
      ▲                                                                              │
      └──────────────────────────── new stations open ◄─────────────────────────────┘
```
- **Explore:** walking to a station opens its letter as today; closing it adds that station's cards to the deck
  (a short "card added" stamp animation).
- **Boss gate:** each floor ends with an interviewer. HR (Dana) guards floor 1, the Team Lead the mezzanine,
  the Head of Department the final office.
- **Reward:** the CV download, the contact stamp and a "hired" ending are unlocked by winning, but stay reachable
  from the Stations menu for anyone who skips the game.

## Mechanic 1: Deck building (BGG: *Deck, Bag, and Pool Building*)
- Start deck (5): `Enthusiasm`, `Coffee`, `Google It`, `Sketch`, `Quick Prototype`: weak, generic.
- Each station adds 1-2 strong cards drawn from the portfolio (see Card list). The deck grows as you explore,
  so exploring *is* the progression.
- Each turn draw 3. Unused cards go to the discard; reshuffle when empty (classic Dominion-style loop).

## Mechanic 2: Dice placement (BGG: *Worker Placement with Dice*)
- Each turn roll 3 **Energy dice**. Faces: `Craft`, `Code`, `Talk`, `Craft`, `Code`, `Wild` (+ pip value 1-6
  on a second line, or simpler: faces carry 1-3 pips).
- A card has one or two **slots** with a rule: *a Code die*, *any die ≥ 4*, *two dice that match*, *an odd die*.
  Fill all its slots → the card fires.
- One die per slot, unused dice are lost. The puzzle each turn: which cards to power with this roll.
- Combos: some cards add dice (`Coffee`: +1 die), reroll (`Google It`), or change a face (`Wild`).

## The fight
- **Boss "Doubt"** (HP): HR 10, Team Lead 16, Head of Department 24. Cards deal **Persuasion**.
- **Your "Confidence"** (HP 10, carried between bosses, +3 after each win).
- **Intent, telegraphed on the boss's letter** (as in Slay the Spire), e.g.:
  - *"Tell me about a weakness."* → 3 Confidence damage next turn unless you play a Talk card.
  - *"Culture fit check."* → your Code slots are locked for a turn.
  - *"We need it by Friday."* → you roll one die fewer.
- **Weakness per boss** keeps each fight different:
  | Boss | Doubt | Weak to | Style |
  |---|---|---|---|
  | HR (Dana) | 10 | Talk | Soft-skill questions, light damage; the tutorial fight |
  | Team Lead | 16 | Code / Craft cards | Technical tests: locks slots, demands matching dice |
  | Head of Department | 24 | Project cards (combos) | Asks for impact: big hits, wants 2-card chains |

## Card list (from the CVs and portfolio)
| Card | Slots | Effect | Source station |
|---|---|---|---|
| Unity / C# | Code ≥ 3 | 4 Persuasion | Games |
| Tech Art Pipeline | Code + Craft | 5, +1 die next turn | Games |
| Blender Rig | Craft ≥ 4 | 3, reroll 1 die | Games |
| Global Game Jam | 2 matching | 6 | Games |
| SolidWorks | Craft | 3 | Projects |
| PCB & Arduino | Code + Craft | 5 | Projects |
| FCC / CE | any ≥ 5 | Block the boss's next intent | Projects |
| Helena's Rooms | Craft + Talk | 7 | Projects (Helena) |
| Giant Simon | 3 dice, any | 8, Head of Dept takes double | Projects (Simon) |
| Babilanur | Wild | 4, heal 2 Confidence | Projects (Babilanur) |
| ShellBound / Kaa / Maskadu… | Code | 3 each, +1 per other game card played | Games (tickets) |
| Camera Technician | Talk | 2, look at the boss's next 2 intents | Career |
| Shenkar Honors | odd die | 3, draw a card | Career |

Profiles: `?for=techart` deals game/tech-art cards first, `?for=design` the product cards. Same rules.

## Mobile screens (portrait 9:16)
1. **Workshop** (today's map): tap to walk, stations open letters; a deck counter stamp in the HUD.
2. **Battle**: boss letter on top (portrait, Doubt bar as a typed line, intent as a stamped note),
   3 cards in the middle (existing ticket style), dice tray at the bottom (dice as rubber-stamp blocks).
   Drag a die onto a slot; a card fires with the stamp "thump"; *Show me* opens the project page.
3. **Result**: "OFFER" or "We'll keep your CV on file" letter with the signature draw-on.

## Unity plan (reuses what exists)
- Data: `SkillCard` and `BossData` ScriptableObjects (cards reference the existing `ProjectData` / `StationData`).
- Runtime: `BattleController` (turn state machine), `DiceTray`, `CardView` (built on `TicketCard`),
  `BossView` (paper letter like `StationPanel`), `DeckState` (persisted with PlayerPrefs for a session).
- Art: three boss portraits in the Holcroft style, dice faces and slot icons as single-stroke PSD layers.

## Milestones
1. **Paper prototype**: rules in a spreadsheet, play 10 fights by hand, tune numbers.
2. **M1 Vertical slice**: one boss (HR), 8 cards, dice tray, win/lose letters. Portrait only.
3. **M2 Deck from stations**: closing a station adds its cards; boss gates on floors.
4. **M3 All three bosses + intents**, mezzanine and final office gating.
5. **M4 Juice & mobile polish**: stamps, draw-ons, haptics, sound, WebGL mobile build.

## Open questions
- Turn length target: 2-3 minutes per boss?
- Does the walk-around map stay between battles, or a simpler floor menu on mobile?
- Should Dana's chase become the HR battle trigger (she catches you → interview)?
