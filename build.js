var bt = require('./BuildTools/buildTools.js')

// Used for generation version.* files
bt.options.companyName = "Topten Software";

// Load version info
bt.version();

if (bt.options.official)
{
    // Check everything committed
    bt.git_check();

    // Clock version
    bt.clock_version();

    // Force clean
    bt.options.clean = true;
    bt.clean("./Build");

    // Run Tests
    bt.dntest("Release", "Topten.RichTextKit.Test");
}

// Build
bt.dnbuild("Release", "Topten.RichTextKit");

if (bt.options.official)
{
    // Tag and commit
    bt.git_tag();

    // Push nuget package
    //bt.nupush(`./build/Release/Topten.RichTextKit/*.${bt.options.version.build}.nupkg`, "http://nuget.toptensoftware.com/v3/index.json");
}


