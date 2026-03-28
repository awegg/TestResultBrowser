# Changelog

---

## 28 March 2026

### Added

- **Action-first test details header** — The test details modal now exposes the most useful actions immediately in the header: Polarion, Open Screenshot, Open Video, Open Full Report, and Acknowledge where the current page supports triage acknowledgements.
- **Clickable Morning Triage summary chips** — The `New failures`, `Fixed`, `Still failing`, and `Missing configs` chips now act as real filters instead of static counters. The selected view is preserved in the URL, so reload and back/forward keep the same triage state.

### Improved

- **Morning Triage acknowledgement workflow** — Acknowledged rows are easier to scan, group acknowledgement propagates upward when all children are acknowledged, and redundant acknowledgement markers were removed to free horizontal space.
- **Home chart hover alignment** — The pass-rate trend hover now tracks the chart correctly, so the guide line and tooltip no longer drift away from the hovered day.
- **System Status responsiveness** — System Status now renders immediately and fills in its summary asynchronously from a cached snapshot instead of blocking the page on expensive aggregations.
- **Home dashboard hierarchy** — The configuration matrix was moved to the top of Home so the cross-configuration view is visible before the KPI and trend sections.


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
