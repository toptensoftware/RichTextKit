var bt = require('./buildtools/buildTools.js')

if (bt.options.official)
{
    // Check everything committed
    bt.git_check();

    // Clock version
    bt.clock_version();

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
    var version = `${ver.major}.${ver.minor}.${ver.build}${ver.suffix}`;
    var releaseArgs = [
        "release", "create", `b${ver.major}.${ver.minor}.${ver.build}`,     // tag made by bt.git_tag()
        `./Build/Release/Topten.RichTextKit.${version}.nupkg`,
        "--title", `RichTextKit ${version}`,
        "--notes-file", "releasenotes.txt",
    ];
    if (ver.suffix)
        releaseArgs.push("--prerelease");
    bt.run_args("gh", releaseArgs);
}


