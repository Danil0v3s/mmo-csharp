using Char.Server.Handlers;

namespace Char.Server.Tests.Handlers;

/// <summary>
/// Pins rAthena <c>char.cpp:1481</c>: under PACKETVER ≥ 20151001 the
/// create-character flow runs through <c>allowed_job_flag</c> — bit 0 =
/// JOB_NOVICE (id 0), bit 1 = JOB_SUMMONER (id 4218). Anything outside
/// the bitmask returns Invalid Job. The C# port adds a sentinel
/// <c>-1</c> meaning "no gate" (legacy config behavior).
/// </summary>
public class CharacterCreateJobGateTests
{
    private const uint JobNovice = 0;
    private const uint JobSummoner = 4218;
    private const uint JobKnight = 7;

    [Theory]
    [InlineData(JobNovice, 3, true)]
    [InlineData(JobSummoner, 3, true)]
    [InlineData(JobNovice, 1, true)]
    [InlineData(JobSummoner, 1, false)]
    [InlineData(JobNovice, 2, false)]
    [InlineData(JobSummoner, 2, true)]
    [InlineData(JobNovice, 0, false)]
    [InlineData(JobSummoner, 0, false)]
    [InlineData(JobKnight, 3, false)]   // not in mask, never allowed
    [InlineData(JobKnight, -1, true)]   // -1 sentinel = no gate
    [InlineData(JobNovice, -1, true)]
    public void IsJobAllowed_MatchesRathenaTable(uint job, int flag, bool expected)
    {
        Assert.Equal(expected, CharacterCreateHandler.IsJobAllowed(job, flag));
    }
}
