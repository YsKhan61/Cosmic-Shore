using PlayFab.GroupsModels;

namespace CosmicShore._Core.Playfab_Models.Event_Models
{
    public class GroupModel
    {
        // Group Name
        public string GroupName { get; set; }
        // Group Unique Identifier Wrapper
        public EntityKey Group { get; set; }
        // TODO: add more properties for groups if needed
    }
}