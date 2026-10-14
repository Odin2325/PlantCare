# Calendar names and personal plant links

Calendar events display the individual plant nickname supplied by the calendar
API, together with the care action and scheduled/completed status. Nicknames are
required for saved plants. Month/week labels now wrap instead of truncating the
name/action. Agenda retains its time column.

In the owner's Month, Week and Agenda views, the event's primary link opens
`/my-plants/:id`: the saved individual plant, not a species catalog add form.
The authenticated detail page uses the existing owner-scoped plant API and shows
nickname (species fallback), species, location, acquisition date, tags, notes and
care schedules. Links lead to care history, Dashboard completion and species
information. Loading/errors have explicit states; the heading remains stable
while the data loads. No backend permissions or schema changed.

The separate Care details button keeps the existing event panel and eligible
care completion available. A shared calendar remains read-only: its event opens
the shared calendar panel and has no link into the owner's private plant page.
Guessing another user's personal plant URL still receives the existing API 404.

Frontend tests cover nickname precedence/species fallback, history link identity
and owner-scoped 404. Browser journeys check nickname/action and personal links
in all three calendar views, retained event details, linked personal details,
accessibility and another account's denial. Record actual candidate/results after
deployment; tests alone do not accept the installed Android upgrade.

This frontend change is also an opportunity for a real installed-app upgrade:
keep the previously installed app online, save unfinished work, accept Update now
when offered after deployment, then check the calendar links, account, collection
and history. Record the old/new candidates in android-beta.md. Do not reinstall
or clear storage as a substitute for upgrading the existing installation.
