# Provider and operator privacy checks

Reviewed 2026-10-21. These are specific publication gates, not proof that provider
settings have already changed. Keep contract references/settings evidence private;
record only non-secret status in this repository.

## Brevo: owner-reported settings applied

The owner approved **one month of transactional logs and no previews of future
emails** on 2026-10-21, including retrospective expiry of older existing logs.
The owner confirmed saving these settings on 2026-10-21. This is owner-reported
dashboard evidence; automated delivery/expiry and tracking settings have not been
independently verified. Existing preview removal was not requested. The steps
below document the configuration and remaining verification.

1. In Brevo, open the account dropdown, then **Settings → Transactional emails →
   Retention rules**. Confirm whether the account is dedicated to PlantCare.
   Scope the rule to PlantCare's sender if other projects use this account.
2. Set log retention to **1 month** for the applicable sender(s), and select
   **Never store previews**. Save. This applies the log rule to existing logs too;
   the preview setting affects future messages only. Do not export old sensitive
   message contents to bypass the rule. Removal of old previews is separate work.
3. Record the selected scope and saved settings, without exposing keys, recipient
   addresses or message contents. Provider deletion is asynchronous; the current
   help article states up to 24 hours after retention ends. One calendar month
   must not be rewritten as exactly 30 days.
4. Review open/click tracking separately. Anonymization or branded links alone do
   not prove tracking is disabled. Verify actual settings and a newly authorized
   synthetic message before claiming no tracking. Do not send another test merely
   because this checklist exists.
5. Review the actual contracted entity/DPA, locations, subprocessors and transfers.
   Record conclusions and source dates without copying account contracts publicly.

Official sources:
[Retention rules](https://help.brevo.com/hc/en-us/articles/4415743225746-Configure-a-custom-retention-period-for-your-transactional-logs-and-email-previews),
[Logs and previews](https://help.brevo.com/hc/en-us/articles/360021533839-Manage-your-transactional-logs-and-email-previews),
[Provider tracking behavior](https://help.brevo.com/hc/en-us/articles/37114679474706-About-email-tracking-pixels-and-the-CNIL-recommendation-in-Brevo).
These documents describe capabilities/defaults, not this account's current state.

## Hosting and other recipients

- Read-only staging inspection found all four running API/worker/proxy/database
  containers use Docker `json-file` with an empty log-options object. No container
  rotation is configured. Compose also defines no rotation. Do not claim a fixed
  log expiry. Plan bounded rotation and separately assess time-based expiry,
  host/security logs, database internal logs, raw recovery artifacts and copied
  diagnostic records; size rotation does not establish a maximum age.
- Caddy's checked-in configuration does not enable request access logging; this
  does not rule out error, host or upstream records. Raw logs were not read.
- Verify OVHcloud's contracted entity/DPA and actual UK location/transfer position.
  [OVHcloud contracts](https://www.ovhcloud.com/en-ie/terms-and-conditions/contracts/)
  is a starting point, not evidence of the customer's accepted contract.
- Review Gmail private-request retention and account security, GitHub public
  support disclosures, browser push services and UptimeRobot checks. Distinguish
  end-user data from owner-only operational email. Do not claim every supplier
  receives the full application database.

## Operator and publication

- Approved public identity: **Nicolas Arevalo Hoelscher, Munich, Germany**;
  private contact **nico.arevalo2325@gmail.com**. Current personal learning
  project, with possible future advertising. No street address was approved.
- Confirm a suitable service/contact address and any required separate legal
  notice before launch. City plus email is not a finished German Impressum.
  Review applicability of [DDG §5](https://www.gesetze-im-internet.de/ddg/__5.html)
  for the intended offering, including future monetization. Do not infer or publish
  a home address from account screenshots.
- Confirm the proposed legal bases and legitimate-interest assessment in
  [the notice draft](../privacy-notice.draft.md); finalize support/log/marker
  retention criteria and provider disclosures. Keep missing items visible.
- Exercise the [private-request procedure](privacy-requests.md) with synthetic
  details. Then publish a completed notice linked before signup and from normal
  navigation. Do not add a mandatory generic privacy-consent checkbox to imply
  blanket consent to optional processing.
- Advertising is a future change: review the actual ad provider, recipients,
  storage/tracking, consent/withdrawal choices and notice before enabling it.
  Existing registration is not consent to future ads.
