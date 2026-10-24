  // Davish.JustBdd: begin -------------------------------------------------------
  // Always build content with textContent and DOM APIs, never innerHTML: content comes from tests and is untrusted.

  var BDD_ICONS = { passed: "\u2713", failed: "\u2717", notrun: "\u2013", threwasexpected: "\u21af" };

  function bddEl(tag, cls, text) {
    var el = document.createElement(tag);
    if (cls) el.className = cls;
    if (text != null) el.textContent = text;
    return el;
  }

  // ---- Search: build the match text once; compare in lowercase

  function bddQuery() { return (state.search || "").trim().toLowerCase(); }

  function bddHas(text, q) { return !!q && !!text && String(text).toLowerCase().indexOf(q) !== -1; }

  function bddStepTexts(step) { return [step.description, step.code, step.error && step.error.message]; }

  function bddSearchText(t) {
    if (t._bddText != null) return t._bddText;
    var parts = [];
    (function walk(scenarios) {
      for (var i = 0; i < (scenarios || []).length; i++) {
        var s = scenarios[i];
        parts.push(s.description);
        for (var j = 0; j < (s.steps || []).length; j++) {
          parts.push.apply(parts, bddStepTexts(s.steps[j]));
          walk(s.steps[j].children);
        }
      }
    })(t.scenarios);
    return (t._bddText = parts.filter(Boolean).join("\n"));
  }

  // Feature name, description, link titles, metadata, and traits match a search without expanding scenarios.
  // Link URLs are left out: a search for "https" or a domain would match every link.
  function bddMetaSearchText(t) {
    if (t._bddMetaText != null) return t._bddMetaText;
    var parts = [];
    var f = t.featureId ? bddFeatures()[t.featureId] : null;
    if (f) {
      parts.push(f.name, f.description);
      (f.links || []).forEach(function (l) { parts.push(l.title); });
      (f.metadata || []).forEach(function (m) { parts.push(m.key + ": " + (m.values || []).join(", ")); });
    }
    (t.traits || []).forEach(function (tr) { parts.push(tr.key + "=" + (tr.value || "")); });
    return (t._bddMetaText = parts.filter(Boolean).join("\n"));
  }

  function bddMatchesScenario(t) {
    var q = bddQuery();
    return !!q && !!t.scenarios && t.scenarios.length > 0 && bddSearchText(t).toLowerCase().indexOf(q) !== -1;
  }

  function bddStepMatches(step, q) {
    var texts = bddStepTexts(step);
    for (var i = 0; i < texts.length; i++) if (bddHas(texts[i], q)) return true;
    return false;
  }

  function bddScenarioMatches(s, q) {
    if (!q) return false;
    if (bddHas(s.description, q)) return true;
    for (var i = 0; i < (s.steps || []).length; i++) {
      var step = s.steps[i];
      if (bddStepMatches(step, q)) return true;
      for (var c = 0; c < (step.children || []).length; c++) if (bddScenarioMatches(step.children[c], q)) return true;
    }
    return false;
  }

  // Add <mark> through the DOM, never innerHTML
  function bddFill(el, text, q) {
    text = text == null ? "" : String(text);
    if (!q) { el.textContent = text; return el; }
    var lower = text.toLowerCase(), at = 0, hit;
    while ((hit = lower.indexOf(q, at)) !== -1) {
      if (hit > at) el.appendChild(document.createTextNode(text.slice(at, hit)));
      el.appendChild(bddEl("mark", "bdd-hit", text.slice(hit, hit + q.length)));
      at = hit + q.length;
    }
    if (at < text.length) el.appendChild(document.createTextNode(text.slice(at)));
    return el;
  }

  function bddLocation(filePath, line) {
    return bddEl("span", "bdd-location", filePath + (line ? ":" + line : ""));
  }

  // CallerArgumentExpression keeps the source indentation: the first line starts at the expression; remove the common indentation from the rest
  function bddDedent(code) {
    var lines = (code || "").replace(/\r\n/g, "\n").split("\n");
    var min = Infinity;
    for (var i = 1; i < lines.length; i++) {
      if (!lines[i].trim()) continue;
      min = Math.min(min, lines[i].match(/^[ \t]*/)[0].length);
    }
    if (min === Infinity) return lines.join("\n");
    for (var j = 1; j < lines.length; j++) lines[j] = lines[j].slice(min);
    return lines.join("\n");
  }

  function bddDetails(summaryText, content, isPre, q) {
    var d = bddEl("details", "bdd-indent");
    d.appendChild(bddEl("summary", null, summaryText));
    var body = isPre ? bddFill(bddEl("pre"), content, q) : content;
    if (isPre && bddHas(content, q)) d.open = true;   // Expand collapsed content that matches
    d.appendChild(body);
    d.addEventListener("click", function (e) { e.stopPropagation(); });
    return d;
  }

  function bddStep(step) {
    var q = bddQuery();
    var status = (step.status || "").toLowerCase();
    var li = bddEl("li", "bdd-step " + status + " " + (step.kind || "").toLowerCase() + (bddStepMatches(step, q) ? " bdd-match" : ""));
    var line = bddEl("div", "bdd-step-line");
    line.appendChild(bddEl("span", "bdd-icon", BDD_ICONS[status] || "?"));
    line.appendChild(bddEl("span", "bdd-kind", step.kind));

    var code = bddDedent(step.code);
    var text = bddEl("span", "bdd-text");
    if (step.description) {
      bddFill(text, step.description, q);
    } else if (code) {
      text.appendChild(bddFill(bddEl("code"), code.split("\n")[0] + (code.indexOf("\n") >= 0 ? " \u2026" : ""), q));
    }
    line.appendChild(text);
    if (status !== "notrun") line.appendChild(bddEl("span", "duration", fmtDuration(step.durationMs)));
    li.appendChild(line);

    // Collapse the code when a description exists; also collapse full multi-line code without a description
    if (code && (step.description || code.indexOf("\n") >= 0)) li.appendChild(bddDetails("code", code, true, q));

    if (step.error) {
      var prefix = status === "threwasexpected" ? "threw " : "";
      var err = bddFill(bddEl("div", "bdd-error bdd-indent"), prefix + (step.error.type || "Exception") + ": " + step.error.message, q);
      li.appendChild(err);
      if (step.error.stackTrace && status !== "threwasexpected") li.appendChild(bddDetails("stack trace", step.error.stackTrace, true));
    }

    if (step.children && step.children.length) {
      var children = bddEl("div", "bdd-indent");
      for (var ci = 0; ci < step.children.length; ci++) children.appendChild(bddScenario(step.children[ci], false));
      li.appendChild(children);
    }
    return li;
  }

  function bddScenario(scenario, open) {
    var q = bddQuery();
    var box = bddEl("details", "bdd-scenario");
    // While searching, expand only matching scenarios; otherwise expand top-level and failed scenarios by default
    box.open = q ? bddScenarioMatches(scenario, q) : (open || !scenario.passed);
    var summary = bddEl("summary");
    summary.appendChild(bddEl("span", "outcome " + (scenario.passed ? "passed" : "failed"), scenario.passed ? "passed" : "failed"));
    summary.appendChild(bddFill(bddEl("span"), scenario.description || "(no description)", q));
    if (scenario.filePath) summary.appendChild(bddLocation(scenario.filePath, scenario.lineNumber));
    box.appendChild(summary);
    box.addEventListener("click", function (e) { e.stopPropagation(); });

    var list = bddEl("ul", "bdd-steps");
    for (var i = 0; i < (scenario.steps || []).length; i++) list.appendChild(bddStep(scenario.steps[i]));
    box.appendChild(list);
    return box;
  }

  // ---- Features: group tests by feature and show the feature's details

  // A merged report records each test's assembly; the report calls it the project.
  function bddProjects() {
    var seen = Object.create(null), projects = [];
    for (var i = 0; i < tests.length; i++) {
      var p = tests[i].assembly;
      if (p && !seen[p]) { seen[p] = true; projects.push(p); }
    }
    return projects;
  }

  function bddProjectKey(t) { return t.assembly || "(no project)"; }

  function bddBucket(id, label, level) {
    return { id: id, label: label, level: level, tests: [], counts: { passed: 0, failed: 0, skipped: 0, timedOut: 0, errored: 0 } };
  }

  function bddAdd(bucket, t) {
    bucket.tests.push(t);
    if (bucket.counts[t.outcome] != null) bucket.counts[t.outcome]++;
  }

  // "Project > Feature": a group per project, then a group per feature inside it. Ids are prefixed with the project,
  // so collapsing a feature in one project leaves the same feature in another project open.
  function bddRenderProjectFeatures(rowsContainer, pager, filtered) {
    var projects = Object.create(null), order = [];
    for (var i = 0; i < filtered.length; i++) {
      var t = filtered[i], p = bddProjectKey(t), f = bddFeatureKey(t);
      var project = projects[p];
      if (!project) {
        project = projects[p] = bddBucket("project:" + p, p, 0);
        project.features = Object.create(null);
        project.featureOrder = [];
        order.push(p);
      }
      bddAdd(project, t);
      if (!project.features[f]) {
        project.features[f] = bddBucket("project:" + p + "\u0000" + f, bddFeatureLabel(f), 1);
        project.featureOrder.push(f);
      }
      bddAdd(project.features[f], t);
    }
    order.sort(function (a, b) { var x = a.toLowerCase(), y = b.toLowerCase(); return x < y ? -1 : x > y ? 1 : 0; });

    pager.hidden = true;
    var frag = document.createDocumentFragment();
    order.forEach(function (p) {
      var project = projects[p];
      frag.appendChild(buildGroupHeader(project));
      if (state.collapsedGroups[project.id]) return;
      // A project without features lists its tests directly instead of under a lone "(no feature)" group.
      if (project.featureOrder.length === 1 && project.featureOrder[0] === BDD_NO_FEATURE) {
        bddAppendRows(frag, project);
        return;
      }
      project.featureOrder.sort(bddCompareFeatures).forEach(function (f) {
        var feature = project.features[f];
        frag.appendChild(buildGroupHeader(feature));
        if (state.collapsedGroups[feature.id]) return;
        var panel = bddFeaturePanel(f);
        if (panel) { panel.classList.add("bdd-subgroup"); frag.appendChild(panel); }
        bddAppendRows(frag, feature);
      });
    });
    rowsContainer.appendChild(frag);
  }

  // Same per-group cap as the template's single-level grouping.
  function bddAppendRows(frag, bucket) {
    var limit = Math.min(bucket.tests.length, state.shownPerGroup[bucket.id] || state.pageSize);
    for (var j = 0; j < limit; j++) frag.appendChild(buildRow(bucket.tests[j]));
    if (bucket.tests.length > limit) frag.appendChild(buildShowMore(bucket, limit));
  }

  function bddHasScenarios(t) { return !!(t.scenarios && t.scenarios.length); }

  // "BDD only" stays hidden unless the report has tests both with and without scenarios.
  function bddInitScenarioFilter() {
    var button = document.getElementById("bdd-only");
    var count = 0;
    for (var i = 0; i < tests.length; i++) if (bddHasScenarios(tests[i])) count++;
    if (count === 0 || count === tests.length) { state.bddOnly = false; return; }
    document.getElementById("count-bdd").textContent = count;
    button.hidden = false;
    button.setAttribute("aria-pressed", state.bddOnly ? "true" : "false");
    button.addEventListener("click", function () {
      state.bddOnly = !state.bddOnly;
      state.page = 0;
      button.setAttribute("aria-pressed", state.bddOnly ? "true" : "false");
      persist();
      renderRows();
    });
  }

  var BDD_NO_FEATURE = "__nofeature__";

  function bddFeatures() { return report.features || {}; }

  function bddHasFeatures() {
    var features = bddFeatures();
    for (var id in features) if (Object.prototype.hasOwnProperty.call(features, id)) return true;
    return false;
  }

  function bddFeatureKey(t) {
    return t.featureId && Object.prototype.hasOwnProperty.call(bddFeatures(), t.featureId) ? t.featureId : BDD_NO_FEATURE;
  }

  function bddFeatureLabel(id) {
    if (id === BDD_NO_FEATURE) return "(no feature)";
    var f = bddFeatures()[id];
    return (f && f.name) || id;
  }

  // Order by feature name; tests without a feature come last.
  function bddCompareFeatures(a, b) {
    if (a === b) return 0;
    if (a === BDD_NO_FEATURE) return 1;
    if (b === BDD_NO_FEATURE) return -1;
    var an = bddFeatureLabel(a).toLowerCase();
    var bn = bddFeatureLabel(b).toLowerCase();
    return an < bn ? -1 : an > bn ? 1 : 0;
  }

  // Only http(s) URLs become links, so a test cannot inject javascript: URLs.
  function bddSafeUrl(url) { return /^https?:\/\//i.test(String(url || "")); }

  function bddFeaturePanel(id) {
    var f = bddFeatures()[id];
    if (!f) return null;
    var links = (f.links || []).filter(function (l) { return l && bddSafeUrl(l.url); });
    var metadata = f.metadata || [];
    if (!f.description && links.length === 0 && metadata.length === 0) return null;

    var panel = bddEl("div", "bdd-feature");
    if (f.description) panel.appendChild(bddEl("div", "bdd-feature-description", f.description));
    if (metadata.length) {
      var meta = bddEl("div", "bdd-feature-meta");
      metadata.forEach(function (m) {
        var item = bddEl("span");
        item.appendChild(bddEl("span", "bdd-feature-key", m.key + ": "));
        item.appendChild(document.createTextNode((m.values || []).join(", ")));
        meta.appendChild(item);
      });
      panel.appendChild(meta);
    }
    if (links.length) {
      var list = bddEl("div", "bdd-feature-links");
      links.forEach(function (l) {
        var a = bddEl("a", null, l.title || l.url);
        a.href = l.url;
        a.target = "_blank";
        a.rel = "noopener noreferrer";
        list.appendChild(a);
      });
      panel.appendChild(list);
    }
    return panel;
  }

  // The row badge names the feature, unless the rows are already grouped under it.
  function bddFeatureBadge(t) {
    var f = t.featureId ? bddFeatures()[t.featureId] : null;
    // The group header already names the feature.
    if (!f || state.groupBy === "feature" || state.groupBy === "project-feature") return null;
    var badge = bddEl("span", "bdd-feature-badge", "\u25c6 " + f.name);
    badge.title = "Feature: " + f.name;
    return badge;
  }
  // Davish.JustBdd: end ---------------------------------------------------------
