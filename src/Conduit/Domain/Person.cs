using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Conduit.Domain;

public class Person
{
    [JsonIgnore]
    public int PersonId { get; init; }

    public string? Username { get; set; }

    [JsonIgnore]
    public string? Email { get; set; }

    public string? Bio { get; set; }

    public string? Image { get; set; }

    // whether the current user follows this person; only populated where the spec requires it
    [NotMapped]
    [JsonPropertyName("following")]
    public bool IsFollowedByCurrentUser { get; set; }

    [JsonIgnore]
    public List<ArticleFavorite> ArticleFavorites { get; init; } = new();

    [JsonIgnore]
    public List<FollowedPeople> Following { get; init; } = new();

    [JsonIgnore]
    public List<FollowedPeople> Followers { get; init; } = new();

    [JsonIgnore]
    public byte[] Hash { get; set; } = [];

    [JsonIgnore]
    public byte[] Salt { get; set; } = [];
}
