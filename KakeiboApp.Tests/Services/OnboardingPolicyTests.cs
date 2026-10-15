using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class OnboardingPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FirstLaunch_ShowsTheGuide(string? lastSeen)
    {
        // Version 1.x never stored the value, so an update from 1.x also lands here.
        Assert.True(OnboardingPolicy.ShouldShow(lastSeen, "2.0"));
    }

    [Theory]
    [InlineData("2.0", "2.0")]
    [InlineData("2", "2.0")]
    [InlineData("2.0.0", "2.0")]
    public void SameVersion_DoesNotShowTheGuide(string lastSeen, string current)
    {
        Assert.False(OnboardingPolicy.ShouldShow(lastSeen, current));
    }

    [Theory]
    [InlineData("1.0", "2.0")]
    [InlineData("2.0", "2.1")]
    [InlineData("2.0", "2.0.1")]
    [InlineData("9.0", "10.0")]
    public void NewerVersion_ShowsTheGuide(string lastSeen, string current)
    {
        Assert.True(OnboardingPolicy.ShouldShow(lastSeen, current));
    }

    [Theory]
    [InlineData("2.1", "2.0")]
    [InlineData("10.0", "9.0")]
    public void OlderVersion_DoesNotShowTheGuide(string lastSeen, string current)
    {
        // The newer guide was already seen; a downgrade has nothing new to explain.
        Assert.False(OnboardingPolicy.ShouldShow(lastSeen, current));
    }

    [Fact]
    public void UnreadableStoredVersion_ShowsTheGuide()
    {
        Assert.True(OnboardingPolicy.ShouldShow("abc", "2.0"));
    }

    [Fact]
    public void UnreadableCurrentVersion_DoesNotShowTheGuide()
    {
        Assert.False(OnboardingPolicy.ShouldShow("2.0", "dev"));
    }
}
