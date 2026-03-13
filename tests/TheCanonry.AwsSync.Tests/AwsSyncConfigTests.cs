using TheCanonry.AwsSync.Types;

namespace TheCanonry.AwsSync.Tests;

public class AwsSyncConfigTests
{
    [Fact]
    public void Config_with_all_fields()
    {
        var config = new AwsSyncConfig(
            Region: "eu-west-1",
            BucketName: "my-bucket",
            BasePrefix: "images/v2",
            ProfileName: "my-profile",
            AccessKeyId: "AKIA1234567890EXAMPLE",
            SecretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            CdnBaseUrl: "https://cdn.example.com");

        Assert.Equal("eu-west-1", config.Region);
        Assert.Equal("my-bucket", config.BucketName);
        Assert.Equal("images/v2", config.BasePrefix);
        Assert.Equal("my-profile", config.ProfileName);
        Assert.Equal("AKIA1234567890EXAMPLE", config.AccessKeyId);
        Assert.Equal("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", config.SecretAccessKey);
        Assert.Equal("https://cdn.example.com", config.CdnBaseUrl);
    }

    [Fact]
    public void Config_with_null_optional_fields()
    {
        var config = new AwsSyncConfig(
            Region: "us-east-1",
            BucketName: "test-bucket",
            BasePrefix: "prefix",
            ProfileName: null,
            AccessKeyId: null,
            SecretAccessKey: null,
            CdnBaseUrl: null);

        Assert.Equal("us-east-1", config.Region);
        Assert.Equal("test-bucket", config.BucketName);
        Assert.Equal("prefix", config.BasePrefix);
        Assert.Null(config.ProfileName);
        Assert.Null(config.AccessKeyId);
        Assert.Null(config.SecretAccessKey);
        Assert.Null(config.CdnBaseUrl);
    }

    [Fact]
    public void Config_equality_is_structural()
    {
        var a = new AwsSyncConfig("us-east-1", "bucket", "pfx", null, null, null, null);
        var b = new AwsSyncConfig("us-east-1", "bucket", "pfx", null, null, null, null);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Config_inequality_on_different_values()
    {
        var a = new AwsSyncConfig("us-east-1", "bucket-a", "pfx", null, null, null, null);
        var b = new AwsSyncConfig("us-east-1", "bucket-b", "pfx", null, null, null, null);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Config_with_allows_nondestructive_mutation()
    {
        var original = new AwsSyncConfig("us-east-1", "bucket", "pfx", null, null, null, null);
        var modified = original with { Region = "ap-southeast-2" };

        Assert.Equal("us-east-1", original.Region);
        Assert.Equal("ap-southeast-2", modified.Region);
        Assert.Equal("bucket", modified.BucketName);
    }

    [Fact]
    public void Config_with_empty_base_prefix()
    {
        var config = new AwsSyncConfig("us-east-1", "bucket", "", null, null, null, null);

        Assert.Equal("", config.BasePrefix);
    }
}
