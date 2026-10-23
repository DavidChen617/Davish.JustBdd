(function () {
  "use strict";

  // The data is embedded as a JSON island so we don't need to worry about JS
  // parsing it; JSON.parse is also stricter than eval which is good for safety.
  var rawData = document.getElementById("mtp-data").textContent;
  var report = JSON.parse(rawData);
  var tests = report.tests || [];
  document.getElementById("incomplete-banner").hidden = !report.incomplete;

  // Outcome ordering: failed first, then errored, timed out, skipped, passed.
  var OUTCOME_ORDER = { failed: 0, errored: 1, timedOut: 2, skipped: 3, passed: 4 };

  var state = {
    filter: "all",
    sort: "outcome",
    sortDir: "asc",
    search: "",
    groupBy: "none",
    page: 0,
    pageSize: 200,
    expanded: Object.create(null),
    collapsedGroups: Object.create(null),
    shownPerGroup: Object.create(null)
  };

  // Scope the storage key to this specific report file so that opening another
  // report in the same browser session does not inherit a stale filter/grouping.
  var storageKey = "mtp-state:" + location.href;

  // Track whether saved state was loaded so we don't override an explicit user
  // choice (e.g. they switched to "all") with the default-failure heuristic.
  var loadedSavedState = false;
  var bddGroupByLoaded = false;   // Davish.JustBdd: keep an explicit grouping choice

  // Restore persisted UI state per report file (best-effort).
  try {
    var saved = JSON.parse(sessionStorage.getItem(storageKey) || "{}");
    if (saved.filter)   { state.filter = saved.filter; loadedSavedState = true; }
    if (saved.sort)     state.sort = saved.sort;
    if (saved.sortDir)  state.sortDir = saved.sortDir;
    if (saved.search)   state.search = saved.search;
    if (saved.groupBy)  { state.groupBy = saved.groupBy; bddGroupByLoaded = true; }   // Davish.JustBdd: flag
    if (typeof saved.pageSize === "number") state.pageSize = saved.pageSize;
  } catch (e) { /* ignored */ }

  // Davish.JustBdd: group by feature by default when the report has features
  if (!bddGroupByLoaded && bddHasFeatures()) state.groupBy = "feature";

  // Default filter: when the run has any non-passing outcome we open scoped to the
  // most informative failing category present. If there are no real failures (only
  // skipped tests) we keep the "all" view so the report opens with content visible.
  // We only apply the heuristic when no saved filter was restored so we don't
  // override an explicit user choice of "all".
  if (!loadedSavedState && state.filter === "all") {
    if (report.summary.failed) {
      state.filter = "failed";
    } else if (report.summary.errored) {
      state.filter = "errored";
    } else if (report.summary.timedOut) {
      state.filter = "timedOut";
    }
  }

  function persist() {
    try {
      sessionStorage.setItem(storageKey, JSON.stringify({
        filter: state.filter, sort: state.sort, sortDir: state.sortDir,
        search: state.search, groupBy: state.groupBy, pageSize: state.pageSize
      }));
    } catch (e) { /* ignored */ }
  }

  // ---------------------------------------------------------------------------
  // Grouping
  // ---------------------------------------------------------------------------

  // Duration buckets (ms) for "Group by Duration".
  var DURATION_BUCKETS = [
    { id: "lt10",       label: "< 10 ms",     max: 10 },
    { id: "lt100",      label: "10 ms – 100 ms", max: 100 },
    { id: "lt1s",       label: "100 ms – 1 s", max: 1000 },
    { id: "lt10s",      label: "1 s – 10 s",   max: 10000 },
    { id: "lt60s",      label: "10 s – 1 min", max: 60000 },
    { id: "gt60s",      label: "≥ 1 min",      max: Infinity }
  ];

  function durationBucket(ms) {
    ms = ms || 0;
    for (var i = 0; i < DURATION_BUCKETS.length; i++) {
      if (ms < DURATION_BUCKETS[i].max) return DURATION_BUCKETS[i];
    }
    return DURATION_BUCKETS[DURATION_BUCKETS.length - 1];
  }

  // Returns the list of available trait keys across all tests, in stable order.
  function traitKeys() {
    var seen = Object.create(null);
    var keys = [];
    for (var i = 0; i < tests.length; i++) {
      var ts = tests[i].traits;
      if (!ts) continue;
      for (var j = 0; j < ts.length; j++) {
        if (seen[ts[j].key]) continue;
        seen[ts[j].key] = true;
        keys.push(ts[j].key);
      }
    }
    keys.sort();
    return keys;
  }

  // Computes the group key(s) for one test under the current groupBy.
  function groupKeysFor(t) {
    if (state.groupBy === "none")  return ["__all__"];
    if (state.groupBy === "class") return [t.className || "(no class)"];
    if (state.groupBy === "feature") return [bddFeatureKey(t)];   // Davish.JustBdd
    if (state.groupBy === "duration") {
      var b = durationBucket(t.durationMs);
      return [b.id];
    }
    if (state.groupBy.indexOf("trait:") === 0) {
      var k = state.groupBy.substring(6);
      if (!t.traits || t.traits.length === 0) return ["(none)"];
      var values = [];
      for (var i = 0; i < t.traits.length; i++) {
        if (t.traits[i].key === k) values.push(t.traits[i].value || "(empty)");
      }
      return values.length ? values : ["(none)"];
    }
    return ["__all__"];
  }

  function groupLabel(id) {
    if (id === "__all__") return null;
    if (state.groupBy === "feature") return bddFeatureLabel(id);   // Davish.JustBdd
    if (state.groupBy === "duration") {
      for (var i = 0; i < DURATION_BUCKETS.length; i++) {
        if (DURATION_BUCKETS[i].id === id) return DURATION_BUCKETS[i].label;
      }
    }
    return id;
  }

  function groupSortIndex(id) {
    if (state.groupBy === "duration") {
      for (var i = 0; i < DURATION_BUCKETS.length; i++) {
        if (DURATION_BUCKETS[i].id === id) return i;
      }
      return 99;
    }
    return 0;
  }

  function buildGroupSelector() {
    var sel = document.getElementById("group-by");
    var options = [
      { v: "none",     l: "None" },
      { v: "class",    l: "Class" },
      { v: "duration", l: "Duration" }
    ];
    if (bddHasFeatures()) options.splice(1, 0, { v: "feature", l: "Feature" });   // Davish.JustBdd
    traitKeys().forEach(function (k) {
      options.push({ v: "trait:" + k, l: "Trait: " + k });
    });
    sel.innerHTML = "";
    var match = false;
    options.forEach(function (o) {
      var opt = document.createElement("option");
      opt.value = o.v;
      opt.textContent = o.l;
      if (o.v === state.groupBy) { opt.selected = true; match = true; }
      sel.appendChild(opt);
    });
    if (!match) state.groupBy = "none";
  }

  function fmtDuration(ms) {
    if (ms == null || isNaN(ms)) return "";
    if (ms < 1)        return "<1 ms";
    if (ms < 1000)     return ms.toFixed(0) + " ms";
    if (ms < 60000)    return (ms / 1000).toFixed(2) + " s";
    var m = Math.floor(ms / 60000);
    var s = ((ms % 60000) / 1000).toFixed(0);
    return m + "m " + s + "s";
  }

  function fmtDateTime(s) {
    if (!s) return "";
    var d = new Date(s);
    if (isNaN(d.getTime())) return s;
    return d.toLocaleString();
  }

  function renderHeader() {
    var subtitle = document.getElementById("subtitle");
    subtitle.textContent =
      report.testApplication
      + " — " + fmtDateTime(report.endTime);

    var summary = report.summary;
    var cards = [
      { key: "total",    label: "Total" },
      { key: "passed",   label: "Passed",    cls: "passed"   },
      { key: "failed",   label: "Failed",    cls: "failed"   },
      { key: "errored",  label: "Errored",   cls: "errored"  },
      { key: "timedOut", label: "Timed out", cls: "timedOut" },
      { key: "skipped",  label: "Skipped",   cls: "skipped"  },
      { key: "duration", label: "Duration", value: fmtDuration(summary.totalDurationMs) }
    ];

    if (typeof summary.flaky === "number") {
      cards.push({ key: "flaky", label: "Flaky", cls: "flaky" });
    }

    var html = "";
    cards.forEach(function (c) {
      var cls = c.cls ? " " + c.cls : "";
      html += '<div class="summary-card' + cls + '"><div class="label"></div><div class="value"></div></div>';
    });
    var container = document.getElementById("summary-cards");
    container.innerHTML = html;
    var nodes = container.querySelectorAll(".summary-card");
    cards.forEach(function (c, i) {
      var v = c.value != null ? c.value : (summary[c.key] || 0);
      nodes[i].querySelector(".label").textContent = c.label;
      nodes[i].querySelector(".value").textContent = v;
    });

    var meta = document.getElementById("meta");
    meta.innerHTML = "";
    function addMeta(label, value) {
      if (value == null || value === "") return;
      var div = document.createElement("div");
      var b = document.createElement("b");
      b.textContent = label + ": ";
      div.appendChild(b);
      div.appendChild(document.createTextNode(value));
      meta.appendChild(div);
    }
    addMeta("Test framework", (report.framework || "") + (report.frameworkVersion ? " (" + report.frameworkVersion + ")" : ""));
    addMeta("Machine", report.machineName);
    addMeta("User", report.userName);
    addMeta("Started", fmtDateTime(report.startTime));
    addMeta("Finished", fmtDateTime(report.endTime));
    addMeta("Exit code", report.exitCode);

    document.getElementById("count-all").textContent      = summary.total || 0;
    document.getElementById("count-failed").textContent   = summary.failed || 0;
    document.getElementById("count-errored").textContent  = summary.errored || 0;
    document.getElementById("count-timedOut").textContent = summary.timedOut || 0;
    document.getElementById("count-skipped").textContent  = summary.skipped || 0;
    document.getElementById("count-passed").textContent   = summary.passed || 0;
  }

  function applyFilters() {
    var q = state.search.trim().toLowerCase();
    var filter = state.filter;
    var filtered = [];
    for (var i = 0; i < tests.length; i++) {
      var t = tests[i];
      if (filter !== "all" && t.outcome !== filter) continue;
      if (q) {
        var hay =
          (t.displayName || "") + "\n" +
          (t.className || "") + "\n" +
          (t.methodName || "") + "\n" +
          (t.testApplication || "") + "\n" +
          (t.targetFramework || "") + "\n" +
          (t.architecture || "") + "\n" +
          (t.errorMessage || "") + "\n" +
          (t.exceptionType || "") + "\n" +
          bddSearchText(t);   // Davish.JustBdd: scenarios, step descriptions, code, and exception messages
        if (hay.toLowerCase().indexOf(q) === -1) continue;
      }
      filtered.push(t);
    }

    var dir = state.sortDir === "desc" ? -1 : 1;
    if (state.sort === "duration") {
      filtered.sort(function (a, b) { return ((a.durationMs || 0) - (b.durationMs || 0)) * dir; });
    } else if (state.sort === "name") {
      filtered.sort(function (a, b) {
        var an = (a.displayName || "").toLowerCase();
        var bn = (b.displayName || "").toLowerCase();
        return (an < bn ? -1 : an > bn ? 1 : 0) * dir;
      });
    } else { // outcome
      filtered.sort(function (a, b) {
        var ao = OUTCOME_ORDER[a.outcome] != null ? OUTCOME_ORDER[a.outcome] : 99;
        var bo = OUTCOME_ORDER[b.outcome] != null ? OUTCOME_ORDER[b.outcome] : 99;
        if (ao !== bo) return (ao - bo) * dir;
        var an = (a.displayName || "").toLowerCase();
        var bn = (b.displayName || "").toLowerCase();
        return an < bn ? -1 : an > bn ? 1 : 0;
      });
    }

    return filtered;
  }

  function renderRows() {
    var rowsContainer = document.getElementById("rows");
    var pager = document.getElementById("pager");
    var empty = document.getElementById("empty");
    var filtered = applyFilters();

    rowsContainer.innerHTML = "";

    if (filtered.length === 0) {
      empty.hidden = false;
      pager.hidden = true;
      return;
    }
    empty.hidden = true;

    if (state.groupBy === "none") {
      renderFlat(rowsContainer, pager, filtered);
    } else {
      renderGrouped(rowsContainer, pager, filtered);
    }
  }

  function renderFlat(rowsContainer, pager, filtered) {
    var totalPages = Math.max(1, Math.ceil(filtered.length / state.pageSize));
    if (state.page >= totalPages) state.page = totalPages - 1;
    if (state.page < 0) state.page = 0;

    var startIdx = state.page * state.pageSize;
    var endIdx = Math.min(filtered.length, startIdx + state.pageSize);

    var frag = document.createDocumentFragment();
    for (var i = startIdx; i < endIdx; i++) {
      frag.appendChild(buildRow(filtered[i]));
    }
    rowsContainer.appendChild(frag);

    if (filtered.length > state.pageSize) {
      buildPager(pager, totalPages, startIdx, endIdx, filtered.length);
    } else {
      pager.hidden = true;
    }
  }

  function renderGrouped(rowsContainer, pager, filtered) {
    // Bucket the filtered tests into groups.
    var buckets = Object.create(null);
    var order = [];
    for (var i = 0; i < filtered.length; i++) {
      var keys = groupKeysFor(filtered[i]);
      for (var k = 0; k < keys.length; k++) {
        var id = keys[k];
        if (!buckets[id]) {
          buckets[id] = { id: id, tests: [], counts: { passed:0, failed:0, skipped:0, timedOut:0, errored:0 } };
          order.push(id);
        }
        buckets[id].tests.push(filtered[i]);
        var o = filtered[i].outcome;
        if (buckets[id].counts[o] != null) buckets[id].counts[o]++;
      }
    }

    // Sort groups: duration buckets keep their natural order; otherwise by id.
    order.sort(function (a, b) {
      if (state.groupBy === "duration") {
        return groupSortIndex(a) - groupSortIndex(b);
      }
      if (state.groupBy === "feature") return bddCompareFeatures(a, b);   // Davish.JustBdd
      var ai = (a || "").toLowerCase();
      var bi = (b || "").toLowerCase();
      return ai < bi ? -1 : ai > bi ? 1 : 0;
    });

    pager.hidden = true;
    var frag = document.createDocumentFragment();
    for (var g = 0; g < order.length; g++) {
      var bucket = buckets[order[g]];
      frag.appendChild(buildGroupHeader(bucket));
      if (!state.collapsedGroups[bucket.id]) {
        // Davish.JustBdd: feature description and links under the group header
        var bddPanel = state.groupBy === "feature" ? bddFeaturePanel(bucket.id) : null;
        if (bddPanel) frag.appendChild(bddPanel);
        // Cap rendered DOM nodes per group so very large runs stay responsive.
        // Users can hit "Show more" to render the rest of the group in chunks.
        var shown = state.shownPerGroup[bucket.id] || state.pageSize;
        var limit = Math.min(bucket.tests.length, shown);
        for (var j = 0; j < limit; j++) {
          frag.appendChild(buildRow(bucket.tests[j]));
        }

        if (bucket.tests.length > limit) {
          frag.appendChild(buildShowMore(bucket, limit));
        }
      }
    }
    rowsContainer.appendChild(frag);
  }

  function buildShowMore(bucket, limit) {
    var div = document.createElement("div");
    div.className = "pager";
    div.style.borderTop = "none";

    var span = document.createElement("span");
    span.textContent = "Showing " + limit + " of " + bucket.tests.length + " in this group";
    div.appendChild(span);

    var more = document.createElement("button");
    more.type = "button";
    more.textContent = "Show " + Math.min(state.pageSize, bucket.tests.length - limit) + " more";
    more.onclick = function () {
      state.shownPerGroup[bucket.id] = limit + state.pageSize;
      renderRows();
    };
    div.appendChild(more);

    var all = document.createElement("button");
    all.type = "button";
    all.textContent = "Show all";
    all.onclick = function () {
      state.shownPerGroup[bucket.id] = bucket.tests.length;
      renderRows();
    };
    div.appendChild(all);

    return div;
  }

  function buildGroupHeader(bucket) {
    var hdr = document.createElement("div");
    hdr.className = "group-header" + (state.collapsedGroups[bucket.id] ? " collapsed" : "");
    hdr.setAttribute("role", "button");
    hdr.setAttribute("tabindex", "0");
    hdr.setAttribute("aria-expanded", state.collapsedGroups[bucket.id] ? "false" : "true");

    var chev = document.createElement("div");
    chev.className = "chevron";
    chev.textContent = "▾";
    hdr.appendChild(chev);

    var title = document.createElement("div");
    title.className = "title";
    title.textContent = groupLabel(bucket.id) || bucket.id;
    hdr.appendChild(title);

    var counts = document.createElement("div");
    counts.className = "group-counts";
    var total = bucket.tests.length;
    var totalSpan = document.createElement("span");
    totalSpan.className = "count total";
    totalSpan.textContent = total + (total === 1 ? " test" : " tests");
    counts.appendChild(totalSpan);
    ["failed","errored","timedOut","skipped","passed"].forEach(function (k) {
      var n = bucket.counts[k] || 0;
      if (n === 0) return;
      var c = document.createElement("span");
      c.className = "count " + k;
      c.textContent = n + " " + k;
      counts.appendChild(c);
    });
    hdr.appendChild(counts);

    function toggleGroup() {
      if (state.collapsedGroups[bucket.id]) {
        delete state.collapsedGroups[bucket.id];
      } else {
        state.collapsedGroups[bucket.id] = true;
      }
      renderRows();
    }

    hdr.addEventListener("click", toggleGroup);
    hdr.addEventListener("keydown", function (e) {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        toggleGroup();
      }
    });

    return hdr;
  }

  function buildPager(pager, totalPages, startIdx, endIdx, totalItems) {
    pager.hidden = false;
    pager.innerHTML = "";

    var info = document.createElement("span");
    info.textContent = "Showing " + (startIdx + 1) + "–" + endIdx + " of " + totalItems;
    pager.appendChild(info);

    var prev = document.createElement("button");
    prev.textContent = "Previous";
    prev.disabled = state.page === 0;
    prev.onclick = function () { state.page--; renderRows(); };
    pager.appendChild(prev);

    var pageInfo = document.createElement("span");
    pageInfo.textContent = "Page " + (state.page + 1) + " of " + totalPages;
    pager.appendChild(pageInfo);

    var next = document.createElement("button");
    next.textContent = "Next";
    next.disabled = state.page >= totalPages - 1;
    next.onclick = function () { state.page++; renderRows(); };
    pager.appendChild(next);
  }

  function buildRow(t) {
    var row = document.createElement("div");
    row.className = "test-row";
    // Use the engine-supplied per-row key (a row index unique across the report) so
    // expand state is always per-row, even when multiple tests share the same UID
    // and even if a UID happens to contain a "#N" suffix.
    var rowKey = t.rowKey;
    var autoOpen = bddMatchesScenario(t);   // Davish.JustBdd
    if (state.expanded[rowKey] || autoOpen) row.classList.add("expanded");

    var outcome = document.createElement("div");
    var pill = document.createElement("span");
    pill.className = "outcome " + (t.outcome || "");
    pill.textContent = t.outcome || "unknown";
    outcome.appendChild(pill);
    row.appendChild(outcome);

    var name = document.createElement("div");
    name.className = "name";
    var display = document.createElement("span");
    display.className = "display";
    display.textContent = t.displayName || "(no display name)";
    name.appendChild(display);

    // Davish.JustBdd: mark tests that have scenarios with a BDD badge
    if (t.scenarios && t.scenarios.length) {
      var bddBadge = document.createElement("span");
      bddBadge.className = "badge bdd";
      bddBadge.title = t.scenarios.length + " Davish.JustBdd scenario(s)";
      bddBadge.textContent = "BDD";
      name.appendChild(bddBadge);

      // Davish.JustBdd: show scenario names directly in the list
      var scenarioNames = [];
      for (var sn = 0; sn < t.scenarios.length; sn++) scenarioNames.push(t.scenarios[sn].description || "(no description)");
      var names = document.createElement("span");
      names.className = "bdd-names";
      names.textContent = scenarioNames.join(" \u00b7 ");
      names.title = scenarioNames.join("\n");
      name.appendChild(names);
    }

    if (t.attemptOf && t.attemptOf > 1) {
      var badge = document.createElement("span");
      badge.className = "badge";
      badge.title = "Result " + t.attemptIndex + " of " + t.attemptOf + " seen for this test UID";
      badge.textContent = "#" + t.attemptIndex + " of " + t.attemptOf;
      name.appendChild(badge);
    }

    if (t.flaky) {
      var flakyBadge = document.createElement("span");
      flakyBadge.className = "badge flaky";
      flakyBadge.title = "Passed after " + (t.retries || (t.retryAttempts ? t.retryAttempts.length : 0)) + " prior attempt(s)";
      flakyBadge.textContent = "Flaky";
      name.appendChild(flakyBadge);
    }

    if (t.traits && t.traits.length) {
      for (var ti = 0; ti < t.traits.length; ti++) {
        var trait = t.traits[ti];
        var chip = document.createElement("span");
        chip.className = "trait";
        chip.textContent = trait.value ? (trait.key + "=" + trait.value) : trait.key;
        name.appendChild(chip);
      }
    }

    if (t.className || t.methodName) {
      var qual = document.createElement("span");
      qual.className = "qualified";
      qual.textContent = (t.className || "")
        + (t.className && t.methodName ? "." : "")
        + (t.methodName || "");
      name.appendChild(qual);
    }
    if (t.testApplication || t.targetFramework || t.architecture) {
      var source = document.createElement("span");
      source.className = "qualified";
      var sourceParts = [];
      if (t.testApplication) sourceParts.push(t.testApplication);
      if (t.targetFramework) sourceParts.push(t.targetFramework);
      if (t.architecture) sourceParts.push(t.architecture);
      source.textContent = sourceParts.join(" · ");
      name.appendChild(source);
    }
    row.appendChild(name);

    var dur = document.createElement("div");
    dur.className = "duration";
    dur.textContent = fmtDuration(t.durationMs);
    row.appendChild(dur);

    var hasDetail = !!(t.errorMessage || t.stackTrace || t.standardOutput || t.standardError || t.exceptionType || (t.retryAttempts && t.retryAttempts.length) || (t.scenarios && t.scenarios.length)); // Davish.JustBdd: scenarios

    var chev;
    if (hasDetail) {
      chev = document.createElement("button");
      chev.type = "button";
      chev.className = "chevron";
      chev.setAttribute("aria-expanded", state.expanded[rowKey] || autoOpen ? "true" : "false");
      chev.setAttribute("aria-label", "Toggle test details");
    } else {
      chev = document.createElement("div");
      chev.className = "chevron";
      chev.setAttribute("aria-hidden", "true");
    }
    chev.textContent = "›";
    row.appendChild(chev);

    // Row keeps the "row" role so the parent role="grid" semantics stay valid;
    // the interactive control is the chevron button (focusable, exposes
    // aria-expanded). We still wire a click handler on the row so users can click
    // anywhere on the visual row to expand/collapse, but keyboard activation goes
    // through the native button.
    row.setAttribute("role", "row");
    row.style.cursor = hasDetail ? "pointer" : "default";

    var detailEl = null;
    function toggle() {
      if (!hasDetail) return;
      if (detailEl) {
        detailEl.parentNode.removeChild(detailEl);
        detailEl = null;
        row.classList.remove("expanded");
        chev.setAttribute("aria-expanded", "false");
        delete state.expanded[rowKey];
      } else {
        detailEl = buildDetail(t);
        row.parentNode.insertBefore(detailEl, row.nextSibling);
        row.classList.add("expanded");
        chev.setAttribute("aria-expanded", "true");
        state.expanded[rowKey] = true;
      }
    }

    if (hasDetail) {
      // Mouse click on the row toggles detail; the click also bubbles from the
      // chevron button which natively handles keyboard activation (Enter/Space).
      // Use a single listener on the row so the chevron click bubbles through it
      // without double-toggling.
      row.addEventListener("click", toggle);
    }

    // If previously expanded, re-create the panel.
    if ((state.expanded[rowKey] || autoOpen) && hasDetail) {   // Davish.JustBdd: autoOpen
      // We can't insert here (row not yet in DOM); defer.
      setTimeout(function () {
        if (!row.parentNode) return;
        detailEl = buildDetail(t);
        row.parentNode.insertBefore(detailEl, row.nextSibling);
      }, 0);
    }

    return row;
  }

  function addBlock(container, tagName, title, content) {
    if (content == null || content === "") return;
    var h = document.createElement(tagName);
    h.textContent = title;
    container.appendChild(h);
    var pre = document.createElement("pre");
    // Always use textContent — never innerHTML — to be immune to HTML
    // injection from test-controlled output.
    pre.textContent = content;
    container.appendChild(pre);
  }

/*__JUSTBDD_JS__*/

  function buildDetail(t) {
    var detail = document.createElement("div");
    detail.className = "detail";

    // Davish.JustBdd: scenario
    if (t.scenarios) {
      for (var si = 0; si < t.scenarios.length; si++) detail.appendChild(bddScenario(t.scenarios[si], true));
    }

    if (t.exceptionType) {
      addBlock(detail, "h4", "Exception type", t.exceptionType);
    }
    addBlock(detail, "h4", "Error message", t.errorMessage);
    addBlock(detail, "h4", "Stack trace",   t.stackTrace);
    addBlock(detail, "h4", "Standard output", t.standardOutput);
    addBlock(detail, "h4", "Standard error",  t.standardError);

    if (t.retryAttempts && t.retryAttempts.length) {
      var history = document.createElement("div");
      history.className = "retry-history";
      var historyHeading = document.createElement("h4");
      historyHeading.textContent = "Retry history";
      history.appendChild(historyHeading);

      for (var ai = 0; ai < t.retryAttempts.length; ai++) {
        var attempt = t.retryAttempts[ai];
        var attemptEl = document.createElement("div");
        attemptEl.className = "retry-attempt";

        var attemptHeader = document.createElement("div");
        attemptHeader.className = "retry-attempt-header";

        var attemptPill = document.createElement("span");
        attemptPill.className = "outcome " + (attempt.outcome || "");
        attemptPill.textContent = attempt.outcome || "unknown";
        attemptHeader.appendChild(attemptPill);

        var attemptLabel = document.createElement("span");
        attemptLabel.textContent = "Attempt " + attempt.attempt;
        attemptHeader.appendChild(attemptLabel);

        var attemptDuration = document.createElement("span");
        attemptDuration.className = "duration";
        attemptDuration.textContent = fmtDuration(attempt.durationMs);
        attemptHeader.appendChild(attemptDuration);

        attemptEl.appendChild(attemptHeader);

        if (attempt.exceptionType) {
          addBlock(attemptEl, "h5", "Exception type", attempt.exceptionType);
        }
        addBlock(attemptEl, "h5", "Error message", attempt.errorMessage);
        addBlock(attemptEl, "h5", "Stack trace",   attempt.stackTrace);
        addBlock(attemptEl, "h5", "Standard output", attempt.standardOutput);
        addBlock(attemptEl, "h5", "Standard error",  attempt.standardError);

        history.appendChild(attemptEl);
      }

      detail.appendChild(history);
    }

    if (detail.childNodes.length === 0) {
      var empty = document.createElement("div");
      empty.style.color = "var(--fg-muted)";
      empty.textContent = "No additional details captured.";
      detail.appendChild(empty);
    }

    return detail;
  }

  function bindToolbar() {
    document.querySelectorAll(".filter-pill").forEach(function (el) {
      el.addEventListener("click", function () {
        state.filter = el.getAttribute("data-filter");
        state.page = 0;
        updateFilterButtons();
        persist();
        renderRows();
      });
    });

    var search = document.getElementById("search");
    search.value = state.search;
    var debounce = null;
    search.addEventListener("input", function () {
      if (debounce) clearTimeout(debounce);
      debounce = setTimeout(function () {
        state.search = search.value;
        state.page = 0;
        persist();
        renderRows();
      }, 120);
    });

    document.querySelectorAll(".sortable").forEach(function (el) {
      el.addEventListener("click", function () {
        var key = el.getAttribute("data-sort");
        if (state.sort === key) {
          state.sortDir = state.sortDir === "asc" ? "desc" : "asc";
        } else {
          state.sort = key;
          state.sortDir = "asc";
        }
        updateSortHeaders();
        persist();
        renderRows();
      });
    });

    var groupSelect = document.getElementById("group-by");
    groupSelect.addEventListener("change", function () {
      state.groupBy = groupSelect.value;
      state.page = 0;
      state.collapsedGroups = Object.create(null);
      state.shownPerGroup = Object.create(null);
      persist();
      renderRows();
    });
  }

  function updateFilterButtons() {
    document.querySelectorAll(".filter-pill").forEach(function (el) {
      var active = el.getAttribute("data-filter") === state.filter;
      el.setAttribute("aria-pressed", active ? "true" : "false");
    });
  }

  function updateSortHeaders() {
    document.querySelectorAll(".sortable").forEach(function (el) {
      var active = el.getAttribute("data-sort") === state.sort;
      el.classList.toggle("active", active);
      var label = el.textContent.replace(/[ ▲▼]+$/, "");
      el.textContent = active
        ? label + " " + (state.sortDir === "asc" ? "▲" : "▼")
        : label;
      el.setAttribute("aria-sort", active
        ? (state.sortDir === "asc" ? "ascending" : "descending")
        : "none");
    });
  }

  // Public hook for future "live" mode (e.g. OpenTelemetry / SSE driven).
  // A future extension can call window.mtpRender(updatedReport) to refresh
  // the page in place without reloading.
  window.mtpRender = function (newReport) {
    if (newReport && typeof newReport === "object") {
      report = newReport;
      tests = report.tests || [];
    }
    renderHeader();
    buildGroupSelector();
    updateFilterButtons();
    updateSortHeaders();
    renderRows();
  };

  renderHeader();
  buildGroupSelector();
  bindToolbar();
  updateFilterButtons();
  updateSortHeaders();
  renderRows();
})();
