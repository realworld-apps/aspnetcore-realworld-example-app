using Conduit.Domain;
using Conduit.Features.Profiles;
using Conduit.Features.Users;
using Riok.Mapperly.Abstractions;

namespace Conduit.Features;

[Mapper]
public partial class ConduitMapper
{
    [MapperIgnoreSource(nameof(Person.PersonId))]
    [MapperIgnoreSource(nameof(Person.ArticleFavorites))]
    [MapperIgnoreSource(nameof(Person.Following))]
    [MapperIgnoreSource(nameof(Person.Followers))]
    [MapperIgnoreSource(nameof(Person.Hash))]
    [MapperIgnoreSource(nameof(Person.Salt))]
    [MapperIgnoreSource(nameof(Person.IsFollowedByCurrentUser))]
    [MapperIgnoreTarget(nameof(User.Token))]
    public partial User PersonToUser(Person person);

    [MapperIgnoreSource(nameof(Person.PersonId))]
    [MapperIgnoreSource(nameof(Person.Email))]
    [MapperIgnoreSource(nameof(Person.ArticleFavorites))]
    [MapperIgnoreSource(nameof(Person.Following))]
    [MapperIgnoreSource(nameof(Person.Followers))]
    [MapperIgnoreSource(nameof(Person.Hash))]
    [MapperIgnoreSource(nameof(Person.Salt))]
    [MapperIgnoreSource(nameof(Person.IsFollowedByCurrentUser))]
    [MapperIgnoreTarget(nameof(Profile.IsFollowed))]
    public partial Profile PersonToProfile(Person person);
}
