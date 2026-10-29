namespace RBI.DesignSystem.Components.Forms;

/// <summary>Native calendar selection contracts.</summary>
public enum RbiCalendarSelectionMode
{
    /// <summary>Zero or one selected date.</summary>
    Single = 0,
    /// <summary>An unordered set of selected dates.</summary>
    Multiple = 1,
    /// <summary>Zero, one pending start, or two ordered inclusive range endpoints.</summary>
    Range = 2,
}

/// <summary>Source caption label and dropdown variants.</summary>
public enum RbiCalendarCaptionLayout
{
    /// <summary>Static month/year label.</summary>
    Label,
    /// <summary>Month and year dropdowns.</summary>
    Dropdown,
    /// <summary>Month dropdown with static year.</summary>
    MonthDropdown,
    /// <summary>Year dropdown with static month.</summary>
    YearDropdown,
}
