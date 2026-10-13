# Weekday scheduling policy

Weekday reminders use a saved time-zone ID, selected weekdays, and a local clock
time. UTC is used for storage and comparison; the server's zone does not choose
the care date. New weekday schedules use the browser's zone. Saving, pausing or
resuming an existing schedule preserves its zone, including while travelling.
The UI displays that zone beside the reminder-time input. An explicit API zone
change recalculates occurrences; switching to interval mode clears weekday data.

The policy is:

- Without previous care, today is eligible when selected and its resolved
  reminder instant is strictly later than configuration time. At or after that
  instant, use the next selected date.
- Completion satisfies its entire local date, including early completion. The
  next reminder uses a later selected date. Editing/recalculating with a previous
  completion uses that completion as the anchor, preserving overdue care rather
  than silently advancing it to the present. Deleting all history resets the
  anchor to now and makes today's future time eligible again.
- A spring clock gap advances a nonexistent time to the first valid local minute
  on that date. For example, Berlin Sunday 2026-03-29 at 02:15 becomes 03:00
  (01:00 UTC). Later weeks return to the original preferred time.
- An autumn repeated clock time has one occurrence, at the later UTC instant.
  Berlin Sunday 2026-10-25 at 02:30 means 01:30 UTC. Completing care during the
  earlier clock hour still satisfies that date. Calendar expansion must remain
  strictly increasing and cannot emit both copies of the repeated time.
- Interval schedules continue to add elapsed 24-hour days; this step does not
  convert them into local-clock recurrence.

Examples and regression coverage live in `WeekdaySchedulePolicyTests.cs`:
same-day future/equal/past, unselected days, local/UTC midnight, month/year changes,
Berlin and New York spring/fall transitions, Lord Howe half-hour transitions,
early completion, history reset, explicit zone change and interval conversion.
API tests verify persisted same-day occurrences, calendar agreement, invalid-zone
non-write behavior and two weekly calendar occurrences across clock changes.

```powershell
dotnet test PlantCare.slnx --configuration Release
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalBrowserJourney.ps1
```

The API integration test project is now included in the solution, so the normal
backend CI command executes it on Linux alongside domain/application tests.
Its database is SQLite; production remains SQL Server. The browser journey uses
real SQL Server and creates a second browser context in America/New_York, signs
in normally, pauses a Europe/Berlin schedule, verifies the saved zone is retained,
then resumes it from the original browser. No host clock is changed.

Microsoft documents [invalid local times](https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.isinvalidtime?view=net-10.0)
and [ambiguous local-time offsets](https://learn.microsoft.com/en-us/dotnet/standard/datetime/resolve-ambiguous-times).
The choices above are PlantCare's policy, not a requirement imposed by .NET.

Existing persisted next-due values are not rewritten by deployment. They use the
new policy when configured, completed, or recalculated; calendar expansion uses
the new policy immediately for subsequent occurrences. No schema migration is
needed. Historical date-line changes depend on the platform's time-zone data
and are not accepted by this regression matrix. Physical-device clock-change
scenarios, real push timing and public
staging acceptance remain separate release work.
