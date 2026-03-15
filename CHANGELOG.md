# Changelog

---

## 15 March 2026

### Added

- **Cross-config result matrix in test details** — You can now see at a glance whether a test failure is isolated to one configuration or spread across all of them. The test details modal shows a configs × builds grid; each cell is a coloured dot (🟢 pass / 🔴 fail / 🟡 skip / · not run) with a tooltip showing build ID, ConfigurationId, timestamp, and error snippet on hover.
- **Shorter test names in results** — Test names no longer repeat the feature prefix, reducing visual noise when browsing results within a feature.
- **Filters reapplied automatically, including Select All** — Active filters are preserved and reapplied when data refreshes. The feature filter also supports a Select All toggle to quickly include or exclude entire groups.

### Performance

- **Dashboard loads faster on large datasets** — The release matrix no longer stalls on datasets with many builds and results. Redundant full-result scans inside the build loop have been eliminated; metrics (pass/fail/skip counts, failing configs) are now computed in a single pass.
- **Morning triage opens without delay** — The triage page no longer scans the full test result dataset on load. Build discovery now hits the pre-built index directly.
- **File share scans no longer block the UI** — Periodic polling of the shared drive runs fully in the background; it no longer competes with page rendering or user interactions.
- **Flaky test analysis is faster on large result sets** — Detection no longer sorts the entire dataset upfront; each test's window is evaluated independently.
- **Less lock contention under concurrent load** — `GetBuildTimestamp()` no longer holds a global lock while computing — the lock is released immediately after snapshotting the ID set.

### Fixed

- **Pages no longer freeze after load** — A JS error during chart initialisation was crashing the Blazor SignalR circuit, making all clicks and navigation stop working. The error is now caught gracefully; the chart observer failing no longer affects the rest of the page.

---

## Earlier Releases

See Git history for changes prior to March 2026.
