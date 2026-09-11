using System;
using Dotcom.Cloud.Infrastructure.Common;

namespace Dotcom.Cloud.Tests;

public class GlobalTagsTests
{
    private static Dictionary<string, string> WorkloadTags() => new()
    {
        ["app"] = "dotcom-pulumi-poc",
        ["costcenter"] = "poc",
        ["businessunit"] = "shared",
        ["criticality"] = "low"
    };

    [Fact]
    public void Build_applies_org_caf_tags_and_keeps_workload_tags()
    {
        var tags = GlobalTags.Build("dev", "southafricanorth", WorkloadTags());

        Assert.Equal("dev", tags["env"]);
        Assert.Equal("southafricanorth", tags["region"]);
        Assert.Equal("cloud operations", tags["opsteam"]);
        Assert.Equal("dotcom-pulumi-poc", tags["app"]);
        Assert.Equal("poc", tags["costcenter"]);
        Assert.Equal("shared", tags["businessunit"]);
        Assert.Equal("low", tags["criticality"]);
    }

    [Fact]
    public void Build_org_tags_win_over_caller_env_region_opsteam()
    {
        var extra = WorkloadTags();
        extra["env"] = "prod";
        extra["region"] = "uksouth";
        extra["opsteam"] = "central it";

        var tags = GlobalTags.Build("dev", "southafricanorth", extra);

        Assert.Equal("dev", tags["env"]);
        Assert.Equal("southafricanorth", tags["region"]);
        Assert.Equal("cloud operations", tags["opsteam"]);
    }

    [Fact]
    public void Build_rejects_legacy_application_key()
    {
        var extra = WorkloadTags();
        extra["application"] = "old-name";

        var ex = Assert.Throws<ArgumentException>(() =>
            GlobalTags.Build("dev", "southafricanorth", extra));

        Assert.Contains("app", ex.Message);
    }

    [Fact]
    public void Build_rejects_unknown_keys()
    {
        var extra = WorkloadTags();
        extra["foo"] = "bar";

        Assert.Throws<ArgumentException>(() =>
            GlobalTags.Build("dev", "southafricanorth", extra));
    }

    [Fact]
    public void Build_requires_app_costcenter_businessunit_criticality()
    {
        Assert.Throws<ArgumentException>(() =>
            GlobalTags.Build("dev", "southafricanorth", new Dictionary<string, string>()));
    }

    [Fact]
    public void Build_rejects_invalid_criticality()
    {
        var extra = WorkloadTags();
        extra["criticality"] = "urgent";

        Assert.Throws<ArgumentException>(() =>
            GlobalTags.Build("dev", "southafricanorth", extra));
    }

    [Fact]
    public void Build_normalizes_production_to_prod()
    {
        var tags = GlobalTags.Build("production", "uksouth", WorkloadTags());
        Assert.Equal("prod", tags["env"]);
    }
}
