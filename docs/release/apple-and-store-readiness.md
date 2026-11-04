# Apple compatibility and store delivery

Reviewed 2026-11-04. Apple browser/Home Screen support and store distribution are
separate milestones. These changes are local and wait for the next substantial
combined deployment. The staging runtime remains the accepted mobile checkpoint.

## Current compatibility slice

- Preserve the existing standalone manifest, stable app ID and 180px Apple touch
  icon. Enable viewport-fit=cover without disabling pinch zoom or maximum scale.
- Respect safe-area insets in the header, body, phone navigation and menus.
  Keep the tablet rail and More menu within short landscape screens, with a
  scrollable menu when needed. Keep touch form text at least 16px.
- Feature-detect Service Worker, PushManager and Notification before offering
  push setup. Unsupported browsers keep in-app reminders and receive useful
  installation guidance. Do not prompt for permission during page load.
- Explain Safari → Share → Add to Home Screen, Open as Web App if offered, then
  launch the icon and explicitly enable notifications in Reminder settings.
  [WebKit documents](https://webkit.org/blog/13878/) Home Screen Web Push for
  iOS/iPadOS 16.4 and later. Focus/device settings can affect delivery; notification
  artwork and controls may be rendered differently by each operating system.

No UA fingerprint, remote script, tracking SDK, new permission, native dependency
or background polling workaround is introduced. Reminder scheduling/delivery and
server subscriptions retain their existing API and ownership behavior.

## Focused local checks

The five notification component tests cover disabled server push, subscription
errors/cleanup and browsers missing Notification or PushManager. A production
frontend build checks compilation and the existing stylesheet budgets.

`tests/browser/apple.config.ts` selects iPhone, iPad and desktop WebKit with only
the synthetic Apple compatibility and shared page-family audit. Set
`PWA_ASSET_ROOT` to the built browser directory to avoid rebuilding the API. The
loopback fixture verifies real asset hashes, serves extensionless frontend routes
and retains explicit uncached API responses; test routes provide synthetic data.
No real accounts, external email or push is used. This optional focused path does
not replace the conservative full-validation selection at the next checkpoint.

Local acceptance on 2026-11-04: all five notification unit cases and the production
frontend build passed. All six WebKit checks passed (Apple capability/layout and
the page-family reflow/accessibility audit on iPhone, iPad and desktop WebKit).
The additional 844x390 landscape/rail check passed. Synthetic portrait/landscape
and tablet screenshots were visually inspected. The changed asset fixture also
passed the existing mobile Chromium real-worker offline-cold-reload, icon-cache
and uncached-account-API regression. Existing CSS-size warnings remain; budgets
were not relaxed. No CI, staging deployment or external message was run.

```sh
PWA_ASSET_ROOT=/assets npx playwright test --config apple.config.ts
```

Linux WebKit/device emulation is browser evidence, not certification of actual
iOS Safari, Home Screen installation, APNs delivery, virtual keyboard, cutouts or
App Store approval. Physical acceptance remains required after the next batch:

1. iPhone and iPad Safari: login, forms/keyboard, portrait/landscape, catalog
   filters, selected calendar day, reminders and More menu; zoom and larger text.
2. Home Screen installation: icon/standalone launch, explicit push opt-in,
   permission denial, one newly due reminder with app closed, Dashboard tap.
3. Cold offline launch/reconnect, consented update with account/history intact,
   safe logout/account switch and account export/deletion.
4. Private photo capture/picker, normalized orientation and image viewing once
   the separate photo activation gates are met. Uploads remain disabled now.

## Store milestones

1. **Keep the shared Angular web app as the primary UI.** Finish the current
   photo-journal activation/capacity/recovery work and physical Apple acceptance
   in useful local batches. Full CI, backup/recovery and staging deploy occur at
   a meaningful checkpoint, not after every small change.
2. **Android packaging spike.** Evaluate a
   [Trusted Web Activity](https://developer.android.com/develop/ui/views/layout/webapps/trusted-web-activities)
   to reuse the installed web app and browser Web Push. Verify Digital Asset
   Links/signing, update ownership, navigation, offline behavior and deep links.
   Do not publish a debug build or register production identifiers prematurely.
3. **iOS packaging spike.** Evaluate
   [Capacitor with Xcode](https://capacitorjs.com/docs/ios) for shared bundled UI
   plus native integration. iOS builds require a macOS/Xcode environment; this
   Windows workstation cannot provide physical iOS acceptance or signing.
   Design native push registration/delivery, deep links, camera/photo access and
   app lifecycle explicitly. Existing service-worker Web Push must not be assumed
   to work inside a WKWebView. Prototype session/CSRF/origin handling against a
   dedicated test environment without broadening production CORS or persisting
   tokens in localStorage. No native wrapper or dependency is added by this slice.
4. **Private store betas.** Use TestFlight and Google Play test tracks, verify
   signed release builds on actual devices and complete permission/privacy/data
   disclosures, in-app deletion and reviewer access. Current
   [Google guidance](https://support.google.com/googleplay/android-developer/answer/14151465)
   requires qualifying new personal accounts to have 12 continuously opted-in
   testers for at least 14 days before applying for production access. Recheck
   account-specific requirements when enrolling.
5. **Public submissions.** Complete the deferred public-launch/legal decisions,
   support/privacy URLs, operational recovery and store metadata. Apple reviews
   [minimum functionality and app completeness](https://developer.apple.com/app-store/review/guidelines/);
   a thin website wrapper is not an approval guarantee. Developer enrollments,
   signing accounts and any paid build infrastructure require the owner's choice.
   Advertising remains a separate later privacy/platform project.

Provider purchases, store enrollment, signing, uploads and public release remain
future work, outside this local compatibility slice.
