using Commander.Core.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commander.Core.Factories;

public class JobDefinitionFactory
{
  private readonly IDeserializer _deserializer;

  public JobDefinitionFactory()
  {
    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
  }


  public Job CreateFromYaml(string? yamlPayload)
  {
    if (string.IsNullOrWhiteSpace(yamlPayload))
    {
      throw new InvalidJobDefinitionException("YAML Payload cannot be empty.");
    }

    JobDto parsedDto;
    try
    {
      parsedDto = _deserializer.Deserialize<JobDto>(yamlPayload);
    }
    catch (Exception ex)
    {
      throw new InvalidJobDefinitionException($"Failed to parse YAML: {ex.Message}");
    }

    if (string.IsNullOrWhiteSpace(parsedDto.Name))
      throw new InvalidJobDefinitionException("Job 'name' is missing or empty.");

    if (parsedDto.Commands == null || parsedDto.Commands.Count == 0)
      throw new InvalidJobDefinitionException("Job must contain at least one command.");

    return new Job(parsedDto.Name, parsedDto.Commands);
  }

  private class JobDto
  {
    public required string Name { get; set; }
    public required string Image { get; set; }
    public required List<string> Commands { get; set; }
  }
}

public class InvalidJobDefinitionException(string message) : Exception(message);
