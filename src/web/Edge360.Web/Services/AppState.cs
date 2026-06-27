using Edge360.Web.Models;

namespace Edge360.Web.Services;

/// <summary>Holds UI-wide state: the loaded groups and the currently-selected active group.</summary>
public sealed class AppState
{
    public IReadOnlyList<GroupDto> Groups { get; private set; } = Array.Empty<GroupDto>();
    public GroupDto? ActiveGroup { get; private set; }

    public event Action? OnChange;

    public bool CanManagePlaces =>
        ActiveGroup is not null && (ActiveGroup.Role == "Admin" || ActiveGroup.Role == "Guardian");

    public void SetGroups(IReadOnlyList<GroupDto> groups)
    {
        Groups = groups;
        if (ActiveGroup is null || groups.All(g => g.Id != ActiveGroup.Id))
            ActiveGroup = groups.FirstOrDefault();
        OnChange?.Invoke();
    }

    public void SetActiveGroup(Guid groupId)
    {
        var match = Groups.FirstOrDefault(g => g.Id == groupId);
        if (match is not null)
        {
            ActiveGroup = match;
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        Groups = Array.Empty<GroupDto>();
        ActiveGroup = null;
        OnChange?.Invoke();
    }
}
