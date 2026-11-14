# Theme Set Skills - Phase 15 QA report

Run with `Tools/Suika` batch: `Unity -batchmode -nographics -executeMethod E2eCheck.Run` (real Play Mode: the real Menu and Main
scenes, real `GameManager`, real PhysX, real scene reloads and PlayerPrefs saves). 90 rows, 89 pass, 0 fail, 1 open decision (H6).
The run is repeatable (two consecutive runs, same result) and logged no error or exception during the whole session (row Z1).
The 13 other Editor suites (`UiCheck`, `ShardsCheck`, and the 11 skill/data suites) also pass.

## Limits of this QA (what was NOT proven)
- Pointer input (legacy `Input`) cannot be injected in batch mode. Taps are covered by calling the same entry points the input code
  calls (`GameManager.PressSkill`, `SkillRuntime.Tap`); the "never target the cat in hand" guard is checked in source (row B1.3).
- No device testing: touch feel, haptics, audio and frame rate on a phone were not measured.
- Visual layout was checked on rendered images for 6 aspect ratios (see Phase 14), not on real devices.

## Findings
| # | Finding | Severity | Status |
|---|---|---|---|
| 1 | Cheat menu (5 taps on SETTINGS, password `2609`: add coins, reset all data) is compiled into every build, including the public web build. The password is in the client code. It is an existing tester tool, not part of Theme Set Skills, so it was not changed. It cannot grant shards, skills or energy (H7). | Medium (production data can be wiped by anyone who finds it) | **Needs your decision** - option: wrap `SettingsPopup` cheat creation in `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |
| 2 | `?debug` in the URL shows an fps / cat count overlay in production. Read-only. | Low | Left as is |
| 3 | A run saved with an empty board is not stored as a run (existing rule), so it cannot carry energy. Only reachable in the first moments of a game. | Info | By design |
| 4 | No bug was found in the Theme Set Skills code in this phase. The failing rows seen while building the suite were all test mistakes (wrong scene, hidden tab, two same-level cats that legitimately merged, random "next cat" already discovering a level). | - | Fixed in the tests |

## Analytics
No analytics SDK or event code exists in the project (scan of scripts, web template and plugins: none). Nothing was added and nothing
is sent. Proposed events for review (not implemented):

| Event | Properties |
|---|---|
| `set_completed` | `theme_id`, `source` (gacha / exchange), `pulls_total` |
| `skill_unlocked` | `skill_id` |
| `run_started` | `skill_id` (or none), `outfit_theme` |
| `skill_activated` | `skill_id`, `energy_before`, `run_seconds`, `score`, `target_level` (target skills) |
| `skill_cancelled` | `skill_id` |
| `run_ended` | `skill_id`, `score`, `seconds`, `skill_uses`, `reason` (game_over / menu) |
| `shard_earned` | `amount`, `balance` |
| `shard_exchange` | `cat_id`, `theme_id`, `price`, `balance_after` |

Privacy: no personal data, no device identifiers beyond what the chosen SDK adds by itself.

## Results

| ID | Test | Expected | Actual | Pass/Fail | Bug / note |
|---|---|---|---|---|---|
| G0 | Menu scene is the start scene and has the Gacha page | GachaPage present | present | Pass |  |
| G1 | Triple tap on PULL while the box is opening | charged once (150) | charged 150 | Pass | the Play coroutine sets _busy synchronously |
| A1 | Pull x1: coins spent, item owned | coins -150; owned; IsNew | coins -150; owned=True; new=True | Pass |  |
| A2 | 10/11 cats of Doodle: collection 10/11, skill still locked | Locked, 10/11, flag false | Locked, 10/11, flag False | Pass |  |
| A3 | Start a run with 10/11 owned and worn: no skill, no button | ActiveSkill null; button hidden | null; button False | Pass |  |
| A4 | 11th cat from the Gacha path: unlock raised once and stored | event 1; Completed=Eraser; flag true | event 1; skill_eraser; flag True | Pass |  |
| A5 | App restart (save reloaded): still unlocked, nothing announced again | flag true; 0 events | flag True; 0 events | Pass |  |
| A6 | Wearing Classic again does not take the unlock away | Unlocked (not Active) | Unlocked | Pass |  |
| A7 | Equip 11/11 Doodle and start a run: Eraser, energy 30, HUD button | Eraser; 30; visible; badge 30% | skill_eraser; 30; True; 30% | Pass |  |
| A8 | One real merge: energy +6 and score paid | 36; score +3 | 36; score +3 | Pass |  |
| A9 | 12 real merges in total: +6 each, capped | 100 after the 12th; every step +6 | 100; ok 12 bad 0 | Pass |  |
| A10 | A merge at 100 energy does not overflow | 100 | 100 | Pass |  |
| A11 | Press at 100: target mode (board dimmed, banner, badge CANCEL), no energy spent | targeting; dim on; banner on; CANCEL; 100 | True; True; True; CANCEL; 100 | Pass |  |
| A12 | Press again = cancel: free, dim and banner off | idle; 100; dim off; banner off | idle; 100; False; False | Pass |  |
| A13 | Tap a cat in target mode: effect, energy spent, mode over | Activated; cat gone; 0; idle | Activated; gone; 0; idle | Pass |  |
| A14 | After using the skill the energy charges again from 0 | 6 | 6 | Pass |  |
| B1.0 | Doodle outfit starts Eraser at 100 energy | Eraser | skill_eraser | Pass |  |
| B1.1 | Eraser removes the chosen cat only (no score, coins, discovery, merge, collider left) | Activated; a gone, b stays; score/coins same; no collider; energy 0 | Activated; a gone, b stays; score 0, coins 0, run 0; colliders 0; energy 0 | Pass |  |
| B1.2 | Tap on empty space while choosing = free cancel | Cancelled; 100 energy; idle | Cancelled; 100; idle | Pass |  |
| B1.3 | The cat in hand and merging cats are never valid taps (HandleTargetInput guard) | guard present | see note | Pass | pointer input cannot be injected in batch: checked in source |
| B2.0 | Kawaii outfit starts Hug | Hug | skill_hug | Pass |  |
| B2.1 | Hug pulls same-level cats together (they close in, then merge) | timed; distance 2.8 -> smaller; merged to level 4 | timed True; 2.80 -> 2.70; level-4 cats 1 | Pass |  |
| B2.2 | A merge caused by Hug does not charge energy | 0 | 0 | Pass |  |
| B2.3 | Hug ends after its duration; board is sane | not active; all cats finite and inside the tank | ended; True | Pass |  |
| B3.0 | Chibi outfit starts Shrink | Shrink | skill_shrink | Pass |  |
| B3.1 | Shrink: every cat (and one created meanwhile) is at 0.85, collider radius follows | scale 0.85, radius = base x 0.85 | all 0.85 | Pass |  |
| B3.2 | Growing back: scale is between 0.85 and 1 during the restore | 0.85 < s < 1 | 0.925 | Pass |  |
| B3.3 | After the effect every cat is exactly its original size (no 0.85 x 0.85 drift) | scale 1, radius = base | exact | Pass |  |
| B3.4 | Physics after the grow-back: cats finite, inside the tank, no Game Over | all inside; not over | True; over=False | Pass |  |
| B4.0 | Pixel outfit starts Level Up | Level Up | skill_level_up | Pass |  |
| B4.1 | Level 10 cat (index 9) is not a valid Level Up target; nothing is spent; still choosing | InvalidTarget; 100; targeting | InvalidTarget; 100; targeting | Pass |  |
| B4.2 | Valid target goes up exactly one level, same place, no merge score | Activated; level 4 near old spot; score same; energy 0 | Activated; level 4, moved 0.09; score 0; energy 0 | Pass |  |
| B4.3 | New cat has the size of its level and counts as discovered | radius 0.58; discovered | 0.58; True | Pass |  |
| B4.4 | Board stays sane after the level-up (inside the tank) | true | True | Pass |  |
| B5.0 | Midnight outfit starts Moonlight | Moonlight | skill_moonlight | Pass |  |
| B5.1 | Moonlight freezes the red-line timer (no growth, no reset) and no Game Over meanwhile | value constant; paused; not over | 0.50 -> 0.50; paused True; over False | Pass |  |
| B5.2 | Effect lasts its duration | 8 s | 8.0 s | Pass |  |
| B5.3 | The timer continues from the held value (not reset); Game Over comes after the effect, not during it | game over ~1.5 s after the end (not 2.0) | over after 1.5 s | Pass |  |
| B6.0 | Candy outfit starts Sugar Rush | Sugar Rush | skill_sugar_rush | Pass |  |
| B6.1 | Baseline merge (no skill) | score 3; coins 1 | score 3; coins 1 | Pass |  |
| B6.2 | During Sugar Rush: merge score x2, merge coins x1.5 (half up) | score 6; coins 2 | score 6; coins 2 | Pass |  |
| B6.3 | Discovery reward of a new cat is NOT multiplied | score 12; coins 32 | score 12; coins 32 | Pass |  |
| B6.4 | Gacha refund during the effect is NOT multiplied | refund 38 x2 awards | 38 | Pass | first award of the pair is new (0), second is the refund |
| B6.5 | Effect lasts its duration | 10 s | 10.0 s | Pass |  |
| B6.6 | After the effect merges pay normally again | score 3; coins 1 | score 3; coins 1 | Pass |  |
| B7.0 | Gold outfit starts Midas | Midas | skill_midas | Pass |  |
| B7.1 | A cat above the Midas limit is refused, nothing spent | InvalidTarget; 100; targeting | InvalidTarget; 100; targeting | Pass |  |
| B7.2 | Midas sells the cat: 3 + 2 x level coins through the normal coin path, no score | Activated; +7 coins (balance and run); score same; gone; energy 0 | Activated; +7 / run +7; score 0; gone; 0 | Pass |  |
| C1 | Pause (settings open) for 1.5 s: the timed effect does not tick | remaining unchanged | 9.000 -> 9.000 | Pass |  |
| C2 | Resume: the effect continues from where it stopped | about 1 s less | 1.00 s elapsed | Pass |  |
| C3 | Pause: the red-line timer is frozen (no unfair Game Over while Settings is open) | unchanged; not over | 0.30 -> 0.30; over False | Pass |  |
| C4 | Skill button pressed while paused is ignored (still choosing, nothing spent) | targeting; 100 | targeting; 100 | Pass |  |
| C5 | A target that gets through while paused does nothing and costs nothing | Failed; cat still there; 100 | Failed; there; 100 | Pass |  |
| C6 | Resume: still in target mode, and the target now works | targeting | targeting | Pass |  |
| C7 | Removing the cat above the line clears the danger timer | 0 | 0 | Pass |  |
| D1 | Save a run: energy, skill id, score and the 3 board cats are in the snapshot | 66; skill_eraser; recorded; 3 cats | 66; skill_eraser; recorded=True; 3 cats | Pass |  |
| D2 | Continue after changing the outfit to Classic: skill, energy, score and cats restored (the skill is not recomputed) | Eraser; 66; score; 3 cats near saved spots | skill_eraser; 66; 3; 3 cats; near=True | Pass |  |
| D3 | HUD after Continue shows the skill and the saved energy | button; 66% | True; 66% | Pass |  |
| D4 | New game after that: current outfit (Classic) gives no skill, energy 30 | null; 30; button hidden | null; 30; False | Pass |  |
| D5 | Save during Sugar Rush: after Continue the effect is gone, the energy spent stays spent | not timed; multiplier off; 0 | idle; False; 0 | Pass |  |
| D6 | Save while choosing a target: Continue is back to idle with the energy kept | idle; 100 | idle; 100 | Pass |  |
| D7 | Save during Shrink, Continue: cats come back full size without being thrown out or ending the game | 5 cats, scale 1, inside tank, calm, not over | 5 cats; scale 1.00; inside True; max speed 0.0; over False | Pass |  |
| E1 | Natural Game Over while choosing a target: skill locked, mode and dim off, energy frozen, button disabled | over; locked; idle; dim off; +0; disabled | True; True; idle; False; +0; interactable False | Pass |  |
| E2 | Finished game cannot be continued; Game Over panel shown | no saved run; panel on | no run; True | Pass |  |
| E3 | A merge after Game Over does nothing (score, energy) | same | 0; +0 | Pass |  |
| E4 | Game Over during Sugar Rush ends the effect | multiplier off; not timed | False; False | Pass |  |
| E5 | Play again: skill re-resolved from the current outfit, energy 30, everything reset | Level Up; 30; unlocked; board empty; danger 0; panel off | skill_level_up; 30; locked=False; 0 cats; 0; False | Pass |  |
| E6 | Settings > Restart while choosing a target: idle, energy 30 | idle; 30; banner off | idle; 30; False | Pass |  |
| E7 | Restart during Shrink: old cats gone, effect off, new cats are normal size | 0 old cats; inactive; scale 1 | 0 old cats; False; 1 | Pass |  |
| F1 | Old save (no shards / unlocked list), set already complete: loads, backfills the unlock silently, keeps coins/best/pity | shards 0; unlocked; 0 events; coins 321; best 99; pity 7 | shards 0; True; 0 events; coins 321; best 99; pity 7 | Pass |  |
| F2 | New game from that old profile: Eraser, energy 30 | Eraser; 30 | skill_eraser; 30 | Pass |  |
| F3 | Old run snapshot, outfit complete: Continue resolves the skill once, energy 30, score and 4 cats back | Eraser; 30; 120; 4 cats | skill_eraser; 30; 120; 4 cats | Pass |  |
| F4 | The next save records the skill and energy | recorded; skill_eraser; energy 30 | True; skill_eraser; 30 | Pass |  |
| F5 | Old run snapshot, outfit not complete: no skill, no button | null; hidden | null; False | Pass |  |
| F6 | Run naming a skill that is not unlocked, energy 9999: no skill, energy clamped to 100 | null; 100 | null; 100 | Pass |  |
| F7 | Run naming an unknown skill, energy -5: no skill, energy 0 | null; 0 | null; 0 | Pass |  |
| F8 | Damaged save and damaged run: fresh profile, no run, no exception | coins 0; shards 0; no run | 0; 0; no run | Pass |  |
| F9 | The game starts normally from a damaged save | new game, energy 30 | 30 | Pass |  |
| F10 | A run with an impossible cat level is rejected as damaged | no run | no run | Pass |  |
| F11 | Unknown id in skillsUnlocked is harmless; real unlocks still come from the collection | Eraser unlocked; no crash | True | Pass |  |
| H1 | Duplicate cat: +1 shard and the usual coin refund, kept after a restart | shards 1; refund 38 | shards 1; refund 38 | Pass |  |
| H2 | Exchange (20 shards), restart, exchange again: owned, spent once | Success; owned; 20 left; AlreadyOwned; 20 | Success; True; AlreadyOwned; 20 | Pass |  |
| H3 | Exchange whose grant fails (injected), then retry | Failed; 20 back; Success; 0 | Failed; 20; Success; 0 | Pass | the injected exception is logged by Exchange (expected) |
| H4 | Set completed by exchange (Majesty, 50) -> equip -> run has Midas | Success; Midas unlocked; Midas active; 30 | Success; skill_midas; skill_midas; 30 | Pass |  |
| H5 | Reset player data clears shards, unlocked skills, outfit and the saved run | 0 shards; locked; no run; Classic outfit; keys gone | 0; False; no run; Classic; keys False | Pass |  |
| H6 | Cheat menu (5 taps on SETTINGS + password; +coins, reset all data) is only reachable in development/debug builds | gated by a build check | NOT gated: ships in every build, password 2609 is in the client code | **Fail** | existing tester tool, not a Theme Set Skills feature: needs a product decision (see report) |
| H7 | No cheat grants shards, skills or energy (nothing in the tools can corrupt Theme Set data) | no shard/skill/energy cheat | none | Pass |  |
| H8 | The ?debug overlay (URL contains 'debug') only displays fps, cat count, memory and the last error | no data access in the overlay code | display only | Pass | it is still reachable in production builds by adding ?debug to the URL (harmless: read-only) |
| H9 | Analytics convention in the project | found or none | none found (no SDK, no event code) | Pass | nothing was added or sent; proposed events are listed in the report |
| Z1 | No errors/exceptions logged during the whole session | 0 | 0 | Pass |  |

90 rows, 89 pass, 0 fail, 1 known/pending decision, 67 s
