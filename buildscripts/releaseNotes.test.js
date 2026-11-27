// Tests for releaseNotes.js. Run with: node --test buildscripts/releaseNotes.test.js

const test = require('node:test');
const assert = require('node:assert');
const rn = require('./releaseNotes.js');

const LINK = "Full changelog: https://github.com/toptensoftware/RichTextKit/blob/main/CHANGELOG.md";

const intro = "# Changelog\n\nIntro text.\n\n";
const older = "## 0.6.175\n\n### Fixed\n\n- Old fix\n";
const pending = intro + "## Next release\n\n### Fixed\n\n- New fix\n\n" + older;
const emptyPending = intro + "## Next release\n\n" + older;
const headingsOnly = intro + "## Next release\n\n### Fixed\n\n" + older;
const released = intro + "## 0.6.176\n\n### Fixed\n\n- New fix\n\n" + older;
const notes = "Fixes:\n- New fix\n\n" + LINK + "\n";
const emptyNotes = LINK + "\n";

test("check passes with pending changelog and notes", () => {
    assert.strictEqual(rn.check(pending, notes, "0.6.175"), null);
});

test("check fails when Next release is missing", () => {
    assert.match(rn.check(intro + older, notes, "0.6.174"), /no "## Next release"/);
});

test("check fails when Next release is empty", () => {
    assert.match(rn.check(emptyPending, notes, "0.6.175"), /empty/);
});

test("check fails when Next release has only subheadings", () => {
    assert.match(rn.check(headingsOnly, notes, "0.6.175"), /empty/);
});

test("check fails when releasenotes.txt has only the link", () => {
    assert.match(rn.check(pending, emptyNotes, "0.6.175"), /releasenotes\.txt/);
});

test("check allows a re-run of the current version", () => {
    assert.strictEqual(rn.check(released, notes, "0.6.176"), null);
});

test("stamp renames Next release to the version", () => {
    assert.strictEqual(rn.stamp(pending, "0.6.176"), released);
});

test("stamp leaves a re-run of the same version alone", () => {
    assert.strictEqual(rn.stamp(released, "0.6.176"), released);
});

test("stamp throws when there's nothing to stamp", () => {
    assert.throws(() => rn.stamp(released, "0.6.177"), /no "## Next release"/);
});

test("startNext adds an empty Next release above the newest version", () => {
    assert.strictEqual(rn.startNext(released),
        intro + "## Next release\n\n## 0.6.176\n\n### Fixed\n\n- New fix\n\n" + older);
});

test("startNext does nothing if Next release already exists", () => {
    assert.strictEqual(rn.startNext(pending), pending);
});

test("resetNotes keeps only the changelog link", () => {
    assert.strictEqual(rn.resetNotes(notes), emptyNotes);
});

test("CRLF line endings are handled and kept", () => {
    const crlf = s => s.replace(/\n/g, "\r\n");
    assert.strictEqual(rn.check(crlf(pending), crlf(notes), "0.6.175"), null);
    assert.strictEqual(rn.stamp(crlf(pending), "0.6.176"), crlf(released));
    assert.strictEqual(rn.resetNotes(crlf(notes)), crlf(emptyNotes));
    assert.strictEqual(rn.startNext(crlf(released)),
        crlf(intro + "## Next release\n\n## 0.6.176\n\n### Fixed\n\n- New fix\n\n" + older));
});
