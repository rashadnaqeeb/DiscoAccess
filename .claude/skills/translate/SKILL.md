---
name: translate
description: Translate the mod's authored strings into one game-supported language, end to end - harvest the game's own vocabulary, decide register, draft lang/<language>.txt, validate, listen in the live game, review, commit. Use when asked to translate the mod or add a language. Not for fixing individual words in an existing translation - that is a normal edit plus dotnet test.
argument-hint: <language>
---

Produce a complete `lang/<language>.txt` for the language given as the argument. The translation
source is `src/DiscoAccess.Core/Strings/Strings.cs`: every entry in the `Defaults` table carries a
comment saying where it is spoken, what fills each `{n}` slot, and which game I2 term to match.
Read the table directly; do not work from `lang/en.txt` (it has no context).

Work through the phases in order. Each depends on the one before it.

## Phase 0 - preconditions

- The game must be running with the dev server up:
  `curl -s --retry 60 --retry-connrefused --retry-delay 1 http://127.0.0.1:8771/health`.
  If it is not running, ask the user to launch it through Steam. Nothing in this skill needs a
  game restart.
- Verify the game actually has the target language (the mod follows the game language, so a file
  for a language the game lacks never loads). Check via `/eval`:
  `I2.Loc.LocalizationManager.GetAllLanguages()` - abort with that explanation if absent.
- File name, in `lang/`: the I2 code lowercased, or the language name lowercased with word runs
  hyphenated (see LanguageSync.FindFile). The game's languages, their file stems, and their
  `_plural` rules (confirmed against the live game; re-check after a game update):
  - Chinese: `zh.txt`, rule `one`
  - Traditional Chinese: `zh-tw.txt`, rule `one`
  - Spanish: `es.txt`, rule `english`
  - Korean: `ko.txt`, rule `one`
  - Portuguese (Brazil): `pt-br.txt` (or `portuguese-brazil.txt`), rule `french`
  - French: `fr.txt`, rule `french`
  - German: `de.txt`, rule `english`
  - Russian: `ru.txt`, rule `slavic`
  - Polish: `pl.txt`, rule `slavic`
  - Japanese: `ja.txt`, rule `one`
  - Turkish: `tr.txt`, rule `one`
  - Arabic: `ar.txt`, rule `arabic`

## Phase 1 - harvest the game's vocabulary

Collect every I2 term named in a "match I2" comment in Strings.cs, then dump their values in the
TARGET language in one `/eval` sweep - do not switch the game's language for this; read the target
column from the term data. Starting-point snippet (adjust against `/typeinfo` if the API differs):

```csharp
string lang = "French"; // the I2 language name
var terms = new[] { "TOOLTIP_TUTO_CHECK_WHITE_OPEN", "TOOLTIP_TUTO_CHECK_RED", /* ...the rest */ };
foreach (var src in I2.Loc.LocalizationManager.Sources) {
    int li = -1;
    for (int i = 0; i < src.mLanguages.Count; i++)
        if (src.mLanguages[i].Name == lang) li = i;
    if (li < 0) continue;
    foreach (var name in terms) {
        var t = src.GetTermData(name);
        if (t != null) Console.WriteLine(name + " = " + t.Languages[li]);
    }
}
```

Never fetch with RTL fixing on. For the district names, also grep the target-language column of
the dialogue terms for the place names (best effort; where the game never names a place, translate
the meaning given in the table comment).

These harvested values are the terminology the whole draft must agree with. Use them live from the
game each run; do not save them to a reference file that can go stale across game updates.

## Phase 2 - register

Decide the register BEFORE drafting, not per-string.

- Default rule: match the register of the game's own localization in this language (the harvest
  shows it - tu/vous, du/Sie, sentence style).
- For a diglossic language, or any language where the written standard and the spoken language
  diverge enough that a TTS voice reading one sounds wrong to a speaker of the other (Arabic is
  the standing example: Modern Standard Arabic versus dialect), the game's localization cannot
  settle it alone - these strings are HEARD, not read. Ask the user which variety and formality
  to use before writing anything (AskUserQuestion; recommend MSA for Arabic - TTS voices and
  cross-dialect intelligibility favour it - but the user decides).
- Record the decision in a `#` comment at the top of the file so the review pass and any future
  session can see it.

## Phase 3 - draft

Write the whole file in one pass, top to bottom, reading each entry's comment in Strings.cs.

- First line after the header comment: `_plural = <rule>` (the rule from the Phase 0 table).
  Form counts: `one` selects a single form, `english` and `french` two, `slavic` three (one,
  few, many), `arabic` up to six (zero, one, two, few, many, other). Fewer forms than the rule
  selects is legal - selection clamps to the last.
- Keep every `{n}` slot; place them wherever the language wants. `|` separates plural forms
  under the declared rule; `WorldCompass` is an ordered list of exactly eight bearings;
  `ContainerWord_*` is always `singular|plural` regardless of rule.
- Screen-reader register: terse, no fluff, lowercase where the English is lowercase, plain
  punctuation only (no emdashes or typographic quotes - the reader voices them).
- "Match I2" keys use the harvested vocabulary, inflected as the sentence needs.
- Write values in logical character order (normal typing order), including for RTL scripts.

## Phase 4 - validate

`dotnet test DiscoAccess.slnx`. `LanguageFileTests` gates the file mechanically (unknown keys,
dropped or invented slots, form counts, the compass). Fix until green.

## Phase 5 - listen in the live game

LanguageSync reads the DEPLOYED copy of the file (`<game>\BepInEx\plugins\DiscoAccess\lang\`),
not the repo's, and with the game running a full build cannot refresh the deploy (locked DLLs).
So the iteration loop is: edit the repo file, copy it into the deployed lang folder, then
`POST /reload` (module recreation re-runs LanguageSync, which re-reads the file). Switch the game
to the target language first (via `/eval` on the game's language setting, or the mod's
cycle-language key). Then drive the real flows with `POST /input` and read `GET /speech` (or
`/eval`'s speech capture):

- move the world cursor and cross a district boundary (compass, distance, location readout)
- scan to an exit (the WorldExitNamed composition - word order is the thing to hear)
- open a dialogue with a skill check (check colour word, odds, modifiers)
- press the heal and money keys (plurals, currency, bar-name agreement in the heal lines)
- open journal, inventory, thought cabinet (status words in context, Tab-stop labels)

Fix what sounds wrong; each fix is edit, `/reload`, listen again. For RTL languages confirm the
spoken lines are not visually reordered.

## Phase 6 - fresh-eyes review

Spawn ONE subagent with clean context (general-purpose). Give it the finished file and point it
at Strings.cs; instruct it to compare each entry against the English value, the comment, and the
declared register, and to report only entries that are wrong, misleading, register-breaking, or
drop information - explicitly not stylistic preferences. Apply the fixes you agree with, re-run
the tests, re-listen to anything changed.

## Phase 7 - commit

One commit with the new file (repo message style, e.g. "Strings: French translation").
