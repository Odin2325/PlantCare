# Shareable comparisons and salad/root cultivars

Implemented locally on 2026-10-31; no push, pipeline or deployment in this increment.

Comparisons of at least two available catalog entries now show a read-only
Comparison link field and Copy comparison link action. The generated URL contains
only the public comparison route and the available catalog IDs in selection
order. Searches, filters and unrelated query parameters are excluded. No personal
plant or account identifier is included. This is a public link, not an invitation,
email or grant of account access.

Copying reports success only after the clipboard operation resolves. A blocked
or unsupported clipboard leaves the selectable field available with a manual-copy
message. Duplicate pending copy requests are suppressed. Selection changes clear
the status; a late clipboard result cannot report success for a different pair.
Missing entries are omitted and fewer than two available entries suppress sharing.
The clipboard action follows the browser's [secure-context and permission rules](https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText).

## Catalog batch

Four reviewed additions bring the local dataset to **257 entries / 115 cultivars**:

- [Chioggia beetroot](https://www.rhs.org.uk/plants/246746/beta-vulgaris-chioggia/details),
  with red-and-white rings in the cut root.
- [Purple Haze carrot](https://www.rhs.org.uk/plants/296470/daucus-carota-purple-haze/details),
  an F1 cultivar with purple skin and orange core.
- [Rudi summer radish](https://teagasc.ie/crops/horticulture/vegetables/radish---veg-growing-guide/),
  distinguished from the larger winter-storage radishes.
- [Red Salad Bowl lettuce](https://www.rhs.org.uk/plants/119374/lactuca-sativa-red-salad-bowl/details),
  a red loose-leaf alternative to the green Salad Bowl entry.

Each has two English search aliases and source-linked growing/harvest notes.
No fixed German growing dates, automatic feeding schedules or private-data writes
are introduced. All preceding 253 records are preserved.

The [Chioggia photograph](https://commons.wikimedia.org/wiki/File:Betterave_Rossa_di_Chioggia_Montpellier.JPG)
explicitly identifies the cultivar. Its contents and CC BY-SA 3.0 licence were
reviewed; original downloaded bytes are retained. Credits, original title, source,
licence, size and checksum are in the public image manifest/credits. The three
other additions use the approved named pending placeholder. A lettuce photo that
merely listed Red Salad Bowl among possible varieties was rejected.

Working coverage is **193 assets / 38 distinct cultivar photos / 77 pending**.
Existing 192 assets and their manifest records are preserved. Staging is unchanged.

## Focused validation

- Five comparison-page unit tests passed, including public URL contents, selection
  order, missing records, clipboard rejection and duplicate-request handling.
- The affected read-only browser case passed in desktop Chromium, mobile Chromium
  and Firefox. It verifies the clipboard payload using a browser mock, reopening
  the generated URL, retained Back navigation, accessibility and 320-pixel reflow.
  Clipboard permissions on a physical phone were not exercised. The mobile
  screenshot was inspected.
- The frontend build passed, retaining the existing unrelated CSS-budget warnings.
- Asset/coverage validation passed for 193 assets and 115 cultivar records.
  The single repeated-import integration case passed. Previous records and
  scientific-name uniqueness were checked.

Browser crop totals/sample are prepared for the next checkpoint. No full suite
was run. The accumulated local batch still needs full checkpoint validation,
including the earlier CI-policy/workflow changes, before deployment.
