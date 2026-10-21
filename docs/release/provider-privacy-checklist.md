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

### Tracking state reported by the owner

On 2026-10-21 the owner reported that Tracking exposes only **Anonymous email
tracking**, initially **No**. An optional **Brevo tracker** installation is also
offered under Automations. Do not install the website tracker: the current account
email flow uses SMTP and does not require website analytics/visitor tracking.

The owner subsequently confirmed saving **Anonymous email tracking → Yes** on
2026-10-21. This is owner-reported dashboard evidence, not independent delivery
verification. Brevo documents that anonymization stops associating future
open/click statistics with individual contacts, but still measures opens/clicks.
Do not call it disabled tracking, removed pixels or guaranteed removal of tracking
redirects. The application must not claim there is no provider tracking. A
previously branded tracking domain is not evidence of disabled tracking either.

To investigate complete suppression, the owner can ask Brevo privately using
the following text (prepared only; no support message was sent):

> PlantCare uses Brevo SMTP only for account-confirmation and password-reset
> emails. Our Tracking screen offers only Anonymous email tracking. Can you
> disable open tracking pixels and click-tracking link rewriting for these
> transactional SMTP messages, and confirm the scope and any remaining delivery
> records? Please distinguish complete suppression from anonymized statistics.

Keep the account's credentials and actual reset/confirmation links out of that
request. If Brevo can only provide anonymized tracking, document the remaining
behavior and its privacy/legal basis rather than adding website analytics or
inventing an unsupported SMTP header. No provider migration is authorized here.

## Hosting and other recipients

- Initial read-only staging inspection found all four running containers using
  `json-file` with empty options. The subsequent
  [logging maintenance](container-logging.md) applied and verified 10m/3-file
  rotation on all four; Compose defaults now cover operations services too.
  Separately assess time-based expiry, host/security logs, database internal logs,
  raw recovery artifacts and copied diagnostics. Size rotation does not establish
  a maximum age; do not claim a fixed log expiry.
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
  The owner chose to [review address options first](operator-address-options.md);
  no postal address or paid address-service contract has been approved.
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
