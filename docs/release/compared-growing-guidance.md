# Compared growing guidance and potato cultivars

Implemented locally on 2026-10-31; no push, pipeline or deployment in this increment.

Plant comparison cards now include the available Growing notes and Harvest
guidance from each reviewed catalog profile, beside that plant's existing care
fields and source link. On narrow screens the cards remain stacked. Missing
guidance is omitted; houseplants do not gain invented harvest instructions.
Text is rendered as plain text, not HTML. The existing comparison selection,
navigation and personal records are unchanged.

## Catalog additions

Four potato cultivars extend the Germany-focused table-potato selection. Primary
breeder profiles establish identity, relative maturity, skin/flesh and cooking
characteristics:

- [Belana](https://www.europlant.biz/en/list-of-varieties/belana/): early to
  medium-early, yellow-skinned and yellow-fleshed, firm-cooking salad type.
- [Laura](https://www.europlant.biz/en/list-of-varieties/laura/): medium-early,
  red-skinned, deep yellow flesh, mainly firm cooking.
- [Marabel](https://www.europlant.biz/en/list-of-varieties/marabel/): early,
  yellow skin/flesh and mainly firm cooking.
- [Cilena](https://www.europlant.biz/en/list-of-varieties/cilena/): early,
  firm-cooking salad type with elongated oval tubers and yellow flesh.

English names and two English aliases accompany each entry. Relative maturity
does not define an exact harvest date in Germany. Guidance retains conditional
moisture checks, earthing up, careful lifting/storage and the existing potato
warnings. Commercial fertilizer rates, chemical treatments and promised yields
are not imported into personal care schedules. No automatic feeding is enabled.

Belana, Marabel and Cilena each have a distinct source-identified harvested-tuber
photograph. Source descriptions, licences and image contents were reviewed.
Their original source titles, authors, licence links, source-file names,
Wikimedia thumbnail/change disclosures, sizes and checksums are in the public
credits and image manifest. Laura stays visibly pending: the initial plant-photo
candidate described growth problems and was not selected. Photos represent the
cultivars; this is not a plant-growth progression sequence.

Current local totals: **253 reviewed entries / 111 cultivars / 192 photo assets**,
with **37 distinct cultivar photos and 74 pending**. All previous 249 catalog
records and 189 image manifest records are preserved. Staging is unchanged.

## Focused validation

- Three affected comparison unit tests passed, including per-plant guidance/source
  association, omission of absent harvest guidance and existing error states.
- The affected read-only browser case covers the two-plant comparison, scoped
  growing/harvest text, source links, retained navigation, accessibility and
  320-pixel reflow in desktop Chromium, mobile Chromium and Firefox.
- The frontend production build passed with the existing unrelated My plants and
  Calendar CSS-budget warnings.
- Asset/coverage validation passed for all 192 files and 111 cultivar records.
  The single repeated catalog-import integration case passed.

Browser catalog totals and the representative crop sample are updated for the
next checkpoint; the broader backend/browser suite was not rerun. No schema,
account, reminder, backup or test-policy change is introduced. Full validation
for the accumulated local batch, including earlier policy/workflow changes,
remains required before deployment.
