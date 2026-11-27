var bt = require('./buildtools/buildTools.js')
var releaseNotes = require('./buildscripts/releaseNotes.js')

// Version string as used in version.props, the package name and CHANGELOG.md
function versionString()
{
    var ver = bt.options.version;
    return `${ver.major}.${ver.minor}.${ver.build}${ver.suffix}`;
}

if (bt.options.official)
{
    // Check everything committed
    bt.git_check();

    // Check there are release notes to publish
    var notesError = releaseNotes.checkFiles(versionString());
    if (notesError)
    {
        console.log(`${notesError}, aborting!`);
        process.exit(7);
    }

    // Clock version
    bt.clock_version();

    // Rename "## Next release" to this version (committed by git_tag below)
    releaseNotes.stampFiles(versionString());

    // Clean build directory
    bt.run("rm -rf ./Build");
}

// Build
bt.run("dotnet build Topten.RichTextKit -c Release")

if (bt.options.official)
{
    // Run tests
    bt.run("dotnet test Topten.RichTextKit.Test -c Release");

    // Build docs
    if (!bt.options.nodoc)
    {
        bt.run(`docsanity-net8`);
        bt.run(`git add doc`);
        bt.run(`git commit --allow-empty -m "Updated documentation"`);
    }

    // Tag and commit
    bt.git_tag();

    // Create a GitHub Release for the tag with the signed package attached. The
    // "Publish to NuGet" workflow (.github/workflows/publish.yml) then pushes it
    // to nuget.org using Trusted Publishing.
    var ver = bt.options.version;
    var version = versionString();
    var releaseArgs = [
        "release", "create", `b${ver.major}.${ver.minor}.${ver.build}`,     // tag made by bt.git_tag()
        `./Build/Release/Topten.RichTextKit.${version}.nupkg`,
        "--title", `RichTextKit ${version}`,
        "--notes-file", "releasenotes.txt",
    ];
    if (ver.suffix)
        releaseArgs.push("--prerelease");
    bt.run_args("gh", releaseArgs);

    // Start the next release's notes: empty "## Next release" in CHANGELOG.md
    // and releasenotes.txt cut down to the changelog link. Done after the
    // release because gh reads releasenotes.txt above.
    releaseNotes.startNextFiles();
    if (!bt.options.nogit)
    {
        bt.run_args("git", ["add", "CHANGELOG.md", "releasenotes.txt"]);
        bt.run_args("git", ["commit", "-m", "Start next release notes"]);
        bt.run_args("git", ["push", "--quiet"]);
    }
}
