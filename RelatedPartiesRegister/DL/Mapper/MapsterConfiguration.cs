using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using Mapster;

namespace RBBH.ConnectedParties.DL.Mapper
{
    public class MapsterConfiguration
    {
        public static void RegisterMappings()
        {
            #region Related Persons & Family Members mappings

            // RelatedPerson => RelatedPersonResponseDTO
            TypeAdapterConfig<RelatedPerson, RelatedPersonResponseDTO>
                .NewConfig()
                .Map(dest => dest.FamilyMemberCount,
                     src => src.RelatedFamilyMembers.Count(fm => fm.IsActive))
                .Map(dest => dest.RelatedToPersonName,
                     src => src.RelatedToPerson == null ? null : src.RelatedToPerson.FirstName + " " + src.RelatedToPerson.LastName);

            // RelatedPerson => RelatedPersonSummaryDTO
            TypeAdapterConfig<RelatedPerson, RelatedPersonSummaryDTO>
                .NewConfig()
                .Map(dest => dest.FamilyMemberCount,
                     src => src.RelatedFamilyMembers.Count(fm => fm.IsActive));

            // CreateRelatedPersonDTO => RelatedPerson
            TypeAdapterConfig<CreateRelatedPersonDTO, RelatedPerson>
                .NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Status)
                .Ignore(dest => dest.FamilyMembers)
                .Ignore(dest => dest.RelatedToPerson!)
                .Ignore(dest => dest.RelatedFamilyMembers)
                .Ignore(dest => dest.IsActive)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.CreatedBy)
                .Ignore(dest => dest.ModifiedAt!)
                .Ignore(dest => dest.ModifiedBy!);

            // UpdateRelatedPersonDTO => RelatedPerson (primjenjuje se na postojeći entitet)
            TypeAdapterConfig<UpdateRelatedPersonDTO, RelatedPerson>
                .NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Status)
                .Ignore(dest => dest.FamilyMembers)
                .Ignore(dest => dest.RelatedToPerson!)
                .Ignore(dest => dest.RelatedFamilyMembers)
                .Ignore(dest => dest.IsActive)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.CreatedBy)
                .Ignore(dest => dest.ModifiedAt!)
                .Ignore(dest => dest.ModifiedBy!);

            // FamilyMember => FamilyMemberResponseDTO
            TypeAdapterConfig<FamilyMember, FamilyMemberResponseDTO>
                .NewConfig()
                .Ignore(dest => dest.Children!);

            // CreateFamilyMemberDTO => FamilyMember
            TypeAdapterConfig<CreateFamilyMemberDTO, FamilyMember>
                .NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.RelatedPersonId)
                .Ignore(dest => dest.RelatedPerson!)
                .Ignore(dest => dest.ParentFamilyMember!)
                .Ignore(dest => dest.ChildFamilyMembers)
                .Ignore(dest => dest.IsActive)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.CreatedBy)
                .Ignore(dest => dest.ModifiedAt!)
                .Ignore(dest => dest.ModifiedBy!);

            // UpdateFamilyMemberDTO => FamilyMember (primjenjuje se na postojeći entitet)
            TypeAdapterConfig<UpdateFamilyMemberDTO, FamilyMember>
                .NewConfig()
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.RelatedPersonId)
                .Ignore(dest => dest.RelatedPerson!)
                .Ignore(dest => dest.ParentFamilyMember!)
                .Ignore(dest => dest.ChildFamilyMembers)
                .Ignore(dest => dest.IsActive)
                .Ignore(dest => dest.CreatedAt)
                .Ignore(dest => dest.CreatedBy)
                .Ignore(dest => dest.ModifiedAt!)
                .Ignore(dest => dest.ModifiedBy!);

            #endregion
        }
    }
}
