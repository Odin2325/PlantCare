# Garden upcoming work

Implemented locally on 2026-10-30 for the next batched checkpoint. Not pushed or
deployed. Staging remains at the care-schedule preview increment (230 reviewed
entries and 88 cultivars).

Garden gains an Upcoming work view alongside Plants and plans and Harvest
overview. It combines the three saved planting targets with the next due
occurrence of each enabled care schedule. The existing category, plan and search
filters apply. Entries use nicknames, fall back to common names and link to
personal plant details. Houseplant-only, legacy and archived entries remain
excluded by the parent Garden view.

Ranges cover today plus the following 6, 29 or 89 local calendar days. Past items
are included by default and can be hidden. Overdue care and past planting targets
have separate labels; neither means that work was completed. Care timestamps use
the device time zone, while target date strings retain their saved calendar date.
Calendar arithmetic handles daylight-saving changes. A minute clock updates the
labels at midnight and is disposed when the view closes. Today includes care due
later in the day. This is a next-occurrence list, without recurrence expansion,
forecasting, new reminders, care completion or harvest recording. No API, schema
or account permissions changed.

## Cultivar batch

Eight additions bring the local dataset to 238 reviewed entries and 96 cultivars:
Potato King Edward, Carrot Maestro, Beetroot Wodan, Radish Black Spanish Round,
Lettuce All the Year Round, Cucumber Mini Munch, Onion Centurion and Garden pea
Vivado. All 230 existing records are unchanged. The 156 licensed photo originals
are retained; new cultivar photos are explicitly unverified species references.
Winter-radish guidance differs from the fast salad-radish schedule. Mini Munch
includes sheltered/greenhouse growing guidance. Relative seasons and cultivar
names do not establish fixed German growing dates or guaranteed performance.

Reviewed primary sources:

- [RHS potato selection](https://www.rhs.org.uk/advice/grow-your-own/allotments/liz-mooney/perfect-potatoes-from-the-plot)
- [RHS maincrop carrot trial](https://www.rhs.org.uk/plants/trials-awards/ongoing-plant-trials/carrot)
- [RHS beetroot](https://www.rhs.org.uk/vegetables/beetroot/grow-your-own)
- [RHS radishes](https://www.rhs.org.uk/vegetables/radishes/grow-your-own)
- [RHS All the Year Round](https://www.rhs.org.uk/plants/184475/lactuca-sativa-all-the-year-round/details)
- [RHS Mini Munch](https://www.rhs.org.uk/plants/293192/cucumis-sativus-mini-munch/details)
- [RHS onions](https://www.rhs.org.uk/vegetables/onions/grow-your-own)
- [RHS peas](https://www.rhs.org.uk/vegetables/peas/grow-your-own)

## Local validation

Eight focused frontend tests passed for the agenda and Garden integration,
including literal dates, local boundaries, filters, disabled schedules,
non-mutation and midnight clock disposal. Seven catalog importer checks passed
across the focused run and the corrected root-category count rerun. The frontend
production build passed, retaining existing CSS budget warnings in unrelated
calendar and collection styles. The existing catalog browser sample and counts
are updated for the next pipeline run.

The single affected browser case passed in desktop Chromium, mobile Chromium
and desktop Firefox against a locally served production frontend with synthetic
read-only API responses. It verified local care dates versus literal targets,
shared filters, detail navigation, no writes, accessibility and 320-pixel reflow.
Desktop and phone screenshots were inspected. Full CI, real backend/browser
integration, existing-worker upgrade and staging acceptance are deferred to the
larger batch checkpoint under the owner's delivery preference.
No pipeline, runtime deployment or backup was started for this local increment.
