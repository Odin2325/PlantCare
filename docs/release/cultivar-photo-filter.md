# Cultivar photo browsing

Implemented locally on 2026-10-31; not pushed or deployed.

The public catalog offers a Cultivar photos control with All entries, With
cultivar photo and Photo pending. Both photo-specific choices include named
cultivars only. They combine with search, category, entry type and the existing
care filters. A general-entry filter combined with a cultivar-photo choice can
therefore return no matches; the usual clear-filters action remains available.

The URL stores `cultivarPhoto=available` or `cultivarPhoto=pending`, including on
detail links. Reloading or returning from a profile restores the view. Unsupported
values are ignored. Clearing filters also clears this choice while preserving
comparison selections. Older species-reference images on cultivar records count
as pending, matching the existing image component. Availability describes reviewed
catalog metadata; a temporary image download failure does not change the filter.

One licensed, visually reviewed photograph fills the Sweet Dumpling winter-squash
gap. The [source file](https://commons.wikimedia.org/wiki/File:Courge_patidou_1.jpg)
identifies Patidou and its [source category](https://commons.wikimedia.org/wiki/Category:Cucurbita_pepo_Sweet_Dumpling_(Courge_Patidou))
identifies Sweet Dumpling. Original bytes are retained under CC BY-SA 3.0 with
author, source, licence, change disclosure and checksum in the public credits and
manifest. It shows harvested fruit with natural surface marks. Apple candidates
with conflicting cultivar labels were not approved. No image is generated or
presented as a cultivar on visual resemblance alone.

The local dataset remains **238 reviewed entries / 96 cultivars**, now with
**32 distinct cultivar photos / 64 pending** and **187 photo assets**. Existing
care fields, cultivar identities and personal records are unchanged.

## Targeted validation

- Six catalog-filter unit tests cover photo classification, legacy generic
  records, combined filters, URL restoration and invalid values, clearing and
  comparison preservation.
- The new read-only browser case passed in desktop Chromium, mobile Chromium and
  Firefox. It covers profile/back navigation, reload, combined search/category,
  comparison preservation, pending image suppression, accessibility and 320-pixel
  reflow. The mobile screenshot was inspected.
- The frontend production build passed with the existing unrelated My plants and
  Calendar CSS-budget warnings.
- Image validation passed for all 187 assets and the 96 coverage records. The
  single repeated-import integration case verifies the affected catalog data.

This case uses the existing catalog browser test path. No CI policy, database
schema, account, reminder or backup behavior changes. No full suite, CI pipeline,
push or deployment is part of this increment. The accumulated batch still needs
full validation at its checkpoint, including the earlier policy/workflow changes.
Continue reviewing exact-cultivar photos in small batches, keeping unresolved
entries visibly pending.
