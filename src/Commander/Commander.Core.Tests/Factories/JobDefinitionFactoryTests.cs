namespace Commander.Core.Tests;

public class JobDefinitionFactoryTests
{
  private readonly JobDefinitionFactory _factory = new();

  [Fact]
  public void CreateFromYaml_WithValidYaml()
  {
    var validYaml = """
        name: my-build-job
        commands:
          - echo 'Starting build'
          - make all
        """;

    var job = _factory.CreateFromYaml(validYaml);

    Assert.NotNull(job);
    Assert.Equal("my-build-job", job.Name);
    Assert.Equal(2, job.Commands.Count);
    Assert.Equal("make all", job.Commands[1]);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void CreateFromYaml_WithEmptyInput(string? emptyYaml)
  {
    Assert.Throws<InvalidJobDefinitionException>(() => _factory.CreateFromYaml(emptyYaml));
  }

  [Fact]
  public void CreateFromYaml_WithMissingName()
  {
    var invalidYaml = """
        commands:
          - echo 'Hello'
        """;

    Assert.Throws<InvalidJobDefinitionException>(() => _factory.CreateFromYaml(invalidYaml));
  }
}
