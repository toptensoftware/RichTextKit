// Release-note housekeeping for build.js --official.
//
// - check():      before a release, make sure there are notes to publish
// - stamp():      rename "## Next release" in CHANGELOG.md to the version
// - startNext():  after a release, add an empty "## Next release" section
// - resetNotes(): after a release, cut releasenotes.txt down to the link
//
// The text functions are pure (tested in releaseNotes.test.js). The *File
// functions apply them to CHANGELOG.md and releasenotes.txt in the cwd.

const fs = require('fs');

const NEXT_HEADING = "## Next release";
const LINK_PREFIX = "Full changelog:";

// Run fn on text with \n line endings, restoring \r\n if the text used them
function withLf(text, fn)
{
    const crlf = text.includes("\r\n");
    const result = fn(text.replace(/\r\n/g, "\n"));
    return (crlf && typeof result === "string") ? result.replace(/\n/g, "\r\n") : result;
}

// Index of each "## " heading line
function headingIndexes(lines)
{
    return lines.map((l, i) => l.startsWith("## ") ? i : -1).filter(i => i >= 0);
}

// Text of the first "## " heading, without the "## "
function topHeading(lines)
{
    const i = headingIndexes(lines)[0];
    return i === undefined ? null : lines[i].substring(3).trim();
}

function nextIndex(lines)
{
    return lines.findIndex(l => l.trim() === NEXT_HEADING);
}

// Error message, or null if ready to release. currentVersion is the version
// in version.json before the build number is clocked, so a re-run that keeps
// the build number (changelog already stamped) is allowed.
function check(changelog, notes, currentVersion)
{
    const changelogError = withLf(changelog, text =>
    {
        const lines = text.split("\n");
        const start = nextIndex(lines);
        if (start < 0)
        {
            if (topHeading(lines) === currentVersion)
                return null;
            return `CHANGELOG.md has no "${NEXT_HEADING}" section`;
        }
        const end = headingIndexes(lines).find(i => i > start) ?? lines.length;
        const body = lines.slice(start + 1, end).filter(l => l.trim() && !l.startsWith("#"));
        return body.length ? null : `The "${NEXT_HEADING}" section in CHANGELOG.md is empty`;
    });
    if (changelogError)
        return changelogError;

    const hasNotes = withLf(notes, text =>
        text.split("\n").some(l => l.trim() && !l.startsWith(LINK_PREFIX)));
    return hasNotes ? null : "releasenotes.txt has no notes, only the changelog link";
}

// Rename "## Next release" to "## <version>"
function stamp(changelog, version)
{
    return withLf(changelog, text =>
    {
        const lines = text.split("\n");
        const i = nextIndex(lines);
        if (i < 0)
        {
            if (topHeading(lines) === version)
                return text;            // re-run of an already stamped version
            throw new Error(`CHANGELOG.md has no "${NEXT_HEADING}" section to rename to ${version}`);
        }
        lines[i] = `## ${version}`;
        return lines.join("\n");
    });
}

// Add an empty "## Next release" above the newest version
function startNext(changelog)
{
    return withLf(changelog, text =>
    {
        const lines = text.split("\n");
        if (nextIndex(lines) >= 0)
            return text;
        const i = headingIndexes(lines)[0] ?? lines.length;
        lines.splice(i, 0, NEXT_HEADING, "");
        return lines.join("\n");
    });
}

// Keep only the "Full changelog:" line
function resetNotes(notes)
{
    return withLf(notes, text =>
    {
        const link = text.split("\n").find(l => l.startsWith(LINK_PREFIX));
        if (!link)
            throw new Error(`releasenotes.txt has no "${LINK_PREFIX}" line`);
        return link + "\n";
    });
}

const CHANGELOG = "CHANGELOG.md";
const NOTES = "releasenotes.txt";
const read = f => fs.readFileSync(f, "utf8");
const write = (f, s) => fs.writeFileSync(f, s, "utf8");

function checkFiles(currentVersion)
{
    return check(read(CHANGELOG), read(NOTES), currentVersion);
}

function stampFiles(version)
{
    write(CHANGELOG, stamp(read(CHANGELOG), version));
}

function startNextFiles()
{
    write(CHANGELOG, startNext(read(CHANGELOG)));
    write(NOTES, resetNotes(read(NOTES)));
}

module.exports = { check, stamp, startNext, resetNotes, checkFiles, stampFiles, startNextFiles };
