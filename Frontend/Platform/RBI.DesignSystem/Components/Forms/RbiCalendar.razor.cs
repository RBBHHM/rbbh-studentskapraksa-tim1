using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RBI.DesignSystem.Components.Button;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A Gregorian day grid with typed date selection, native buttons and instance-local focus.</summary>
public partial class RbiCalendar
{
    private readonly string _id = RbiFormUtilities.Id("calendar");
    private readonly string _connectionId = Guid.NewGuid().ToString("N");
    private ElementReference _root;
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    private DateOnly _nativeMonth, _focusDay;
    private DateOnly? _lastMonth;
    private IReadOnlyList<DateOnly> _nativeValue = Array.Empty<DateOnly>();
    private DateOnly[] _lastValue = [];
    private RbiCalendarSelectionMode _lastMode;
    private MonthView[] _months = [];
    private sealed record MonthView(DateOnly Month, int FirstDayNumber, int Weeks);
    private bool _initialized, _disposed, _focusPending;
    private IJSObjectReference? _module;
    private DotNetObjectReference<RbiCalendar>? _reference;
    private Task _operation = Task.CompletedTask;
    private Task? _disposal;
    [Inject] private IJSRuntime JavaScript { get; set; } = default!;
    /// <summary>Localized accessible calendar name.</summary>
    [Parameter, EditorRequired] public string AccessibleLabel { get; set; } = string.Empty;
    /// <summary>Localized previous-month action name.</summary>
    [Parameter, EditorRequired] public string PreviousMonthLabel { get; set; } = string.Empty;
    /// <summary>Localized next-month action name.</summary>
    [Parameter, EditorRequired] public string NextMonthLabel { get; set; } = string.Empty;
    /// <summary>Single, multiple or inclusive-range selection.</summary>
    [Parameter] public RbiCalendarSelectionMode Mode { get; set; }
    /// <summary>Number of adjacent displayed months, from one to twelve.</summary>
    [Parameter] public int NumberOfMonths { get; set; } = 1;
    /// <summary>Caption label/dropdown composition.</summary>
    [Parameter] public RbiCalendarCaptionLayout CaptionLayout { get; set; }
    /// <summary>Source navigation-button variant; day selection buttons retain their own states.</summary>
    [Parameter] public RbiButtonVariant NavigationButtonVariant { get; set; } = RbiButtonVariant.Ghost;
    /// <summary>Localized accessible month-dropdown name when shown.</summary>
    [Parameter] public string? MonthDropdownLabel { get; set; }
    /// <summary>Localized accessible year-dropdown name when shown.</summary>
    [Parameter] public string? YearDropdownLabel { get; set; }
    /// <summary>Earliest dropdown year; defaults to the navigation minimum or supported end year minus one hundred.</summary>
    [Parameter] public int? StartYear { get; set; }
    /// <summary>Latest dropdown year; respects navigation bounds and the initial displayed year.</summary>
    [Parameter] public int? EndYear { get; set; }
    /// <summary>Shows week numbers using the culture's week rule.</summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }
    /// <summary>Uses ISO week numbering and Monday-first ordering.</summary>
    [Parameter] public bool ISOWeekNumbers { get; set; }
    /// <summary>Localized week-number column heading when shown.</summary>
    [Parameter] public string? WeekNumberLabel { get; set; }
    /// <summary>Minimum nights in a completed range; zero permits same-day ranges.</summary>
    [Parameter] public int MinimumRangeDays { get; set; }
    /// <summary>Maximum nights in a completed range; zero means unrestricted.</summary>
    [Parameter] public int MaximumRangeDays { get; set; }
    /// <summary>Resets a proposed range crossing disabled dates to its newly chosen pending start.</summary>
    [Parameter] public bool ExcludeDisabledInRange { get; set; }
    /// <summary>Starts a fresh pending range after an existing completed range is clicked.</summary>
    [Parameter] public bool ResetRangeOnSelect { get; set; }
    /// <summary>Minimum multiple-selection count, guarding user deselection.</summary>
    [Parameter] public int MinimumSelectedDates { get; set; }
    /// <summary>Maximum multiple-selection count; zero means unrestricted.</summary>
    [Parameter] public int MaximumSelectedDates { get; set; }
    /// <summary>Selected dates; range mode uses at most two ordered endpoints. Never mutated.</summary>
    [Parameter] public IReadOnlyList<DateOnly> Value { get; set; } = Array.Empty<DateOnly>();
    /// <summary>Proposes a new selection. Without a delegate the component owns local state.</summary>
    [Parameter] public EventCallback<IReadOnlyList<DateOnly>> ValueChanged { get; set; }
    /// <summary>Displayed month; day is normalized to the first. Defaults to today.</summary>
    [Parameter] public DateOnly? Month { get; set; }
    /// <summary>Proposes displayed-month changes without mutating a bound parameter.</summary>
    [Parameter] public EventCallback<DateOnly?> MonthChanged { get; set; }
    /// <summary>Culture used for Gregorian month/day names and weekday ordering.</summary>
    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    /// <summary>Optional explicit first weekday; otherwise the culture's first weekday.</summary>
    [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }
    /// <summary>Today marker, injectable for deterministic applications/tests.</summary>
    [Parameter] public DateOnly Today { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    /// <summary>Inclusive earliest available date.</summary>
    [Parameter] public DateOnly? Min { get; set; }
    /// <summary>Inclusive latest available date.</summary>
    [Parameter] public DateOnly? Max { get; set; }
    /// <summary>Additional application-owned date availability predicate.</summary>
    [Parameter] public Func<DateOnly, bool>? IsDateDisabled { get; set; }
    /// <summary>Disables selection and month navigation.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Prevents user deselection of the final selected date.</summary>
    [Parameter] public bool Required { get; set; }
    /// <summary>Whether leading/trailing dates are visible.</summary>
    [Parameter] public bool ShowOutsideDays { get; set; } = true;
    /// <summary>Decorative day content; naming and button semantics remain owned.</summary>
    [Parameter] public RenderFragment<DateOnly>? DayContent { get; set; }
    /// <summary>Additional scoped root classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional root attributes excluding the owned class.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private DateOnly EffectiveMonth => ClampMonth(MonthChanged.HasDelegate && Month.HasValue ? Month.Value : _nativeMonth);
    private IReadOnlyList<DateOnly> EffectiveValue => ValueChanged.HasDelegate ? Value : _nativeValue;
    private DayOfWeek WeekStartsOn => ISOWeekNumbers ? DayOfWeek.Monday : FirstDayOfWeek ?? Culture.DateTimeFormat.FirstDayOfWeek;
    private int FirstYear => Math.Max(Min?.Year ?? 1, StartYear ?? Min?.Year ?? Math.Max(1, Math.Min(Today.Year, Max?.Year ?? Today.Year) - 100));
    private int LastYear => Math.Min(Max?.Year ?? 9999, EndYear ?? Max?.Year ?? Math.Max(Today.Year, Math.Max(Month?.Year ?? Today.Year, Min?.Year ?? 1)));
    private int WeekNumber(int dayNumber)
    {
        var date = DateOnly.FromDayNumber(Math.Clamp(dayNumber + 6, 0, DateOnly.MaxValue.DayNumber)).ToDateTime(TimeOnly.MinValue);
        return ISOWeekNumbers ? ISOWeek.GetWeekOfYear(date) : Culture.Calendar.GetWeekOfYear(date, Culture.DateTimeFormat.CalendarWeekRule, WeekStartsOn);
    }
    private static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
    private bool HasYearDropdown => CaptionLayout is RbiCalendarCaptionLayout.Dropdown or RbiCalendarCaptionLayout.YearDropdown;
    private DateOnly NavigationStart => HasYearDropdown && (!Min.HasValue || Min.Value.Year < FirstYear) ? new(FirstYear, 1, 1) : Min.HasValue ? StartOfMonth(Min.Value) : DateOnly.MinValue;
    private DateOnly NavigationEnd => HasYearDropdown && (!Max.HasValue || Max.Value.Year > LastYear) ? new(LastYear, 12, 1) : Max.HasValue ? StartOfMonth(Max.Value) : new(9999, 12, 1);
    private bool IsMonthUnavailable(DateOnly month) => month < NavigationStart || month > NavigationEnd;
    private DateOnly ClampMonth(DateOnly month)
    {
        static int Index(DateOnly date) => (date.Year - 1) * 12 + date.Month - 1;
        var lower = Index(NavigationStart);
        var upper = Math.Max(lower, Index(NavigationEnd) - NumberOfMonths + 1);
        var value = Math.Clamp(Index(month), lower, upper);
        return new(value / 12 + 1, value % 12 + 1, 1);
    }
    private string WeekdayLabel(DayOfWeek weekday)
    {
        var abbreviation = Culture.DateTimeFormat.GetAbbreviatedDayName(weekday);
        return abbreviation[..Math.Min(2, abbreviation.Length)];
    }
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AccessibleLabel) || string.IsNullOrWhiteSpace(PreviousMonthLabel) || string.IsNullOrWhiteSpace(NextMonthLabel)) throw new ArgumentException("Localized calendar/action names are required.");
        ArgumentNullException.ThrowIfNull(Value);
        ArgumentNullException.ThrowIfNull(Culture);
        if (NumberOfMonths is < 1 or > 12 || !Enum.IsDefined(CaptionLayout) || StartYear is < 1 or > 9999 || EndYear is < 1 or > 9999 || FirstYear < 1 || LastYear > 9999 || FirstYear > LastYear) throw new ArgumentOutOfRangeException(nameof(NumberOfMonths));
        if (CaptionLayout is RbiCalendarCaptionLayout.Dropdown or RbiCalendarCaptionLayout.MonthDropdown && string.IsNullOrWhiteSpace(MonthDropdownLabel) || CaptionLayout is RbiCalendarCaptionLayout.Dropdown or RbiCalendarCaptionLayout.YearDropdown && string.IsNullOrWhiteSpace(YearDropdownLabel) || ShowWeekNumbers && string.IsNullOrWhiteSpace(WeekNumberLabel)) throw new ArgumentException("Localized caption/week labels are required.");
        if (MinimumRangeDays < 0 || MaximumRangeDays < 0 || MaximumRangeDays > 0 && MinimumRangeDays > MaximumRangeDays || MinimumSelectedDates < 0 || MaximumSelectedDates < 0 || MaximumSelectedDates > 0 && MinimumSelectedDates > MaximumSelectedDates) throw new ArgumentException("Selection limits are invalid.");
        if (!Enum.IsDefined(Mode) || !Enum.IsDefined(NavigationButtonVariant) || FirstDayOfWeek.HasValue && !Enum.IsDefined(FirstDayOfWeek.Value)) throw new ArgumentOutOfRangeException(nameof(Mode));
        if (Min > Max || Mode == RbiCalendarSelectionMode.Multiple && Value.Distinct().Count() != Value.Count || Mode == RbiCalendarSelectionMode.Single && Value.Count > 1 || Mode == RbiCalendarSelectionMode.Range && (Value.Count > 2 || Value.Count == 2 && Value[0] > Value[1])) throw new ArgumentException("Calendar bounds/selection are invalid.");
        if (!_initialized || Month != _lastMonth) _nativeMonth = StartOfMonth(Month ?? Today);
        if (!_initialized || Mode != _lastMode || !Value.SequenceEqual(_lastValue)) _nativeValue = Array.AsReadOnly(Value.ToArray());
        _lastMode = Mode;
        _lastMonth = Month;
        _lastValue = Value.ToArray();
        if (!_initialized) _focusDay = EffectiveValue.FirstOrDefault(Today);
        _initialized = true;
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class", "role", "aria-label");
        BuildMonth();
    }
    private void BuildMonth()
    {
        var month = EffectiveMonth;
        var remaining = (NavigationEnd.Year - month.Year) * 12 + NavigationEnd.Month - month.Month + 1;
        var count = Math.Min(NumberOfMonths, Math.Max(1, remaining));
        _months = Enumerable.Range(0, count).Select(index =>
        {
            var current = month.AddMonths(index);
            var before = ((int)current.DayOfWeek - (int)WeekStartsOn + 7) % 7;
            return new MonthView(current, current.DayNumber - before, (before + DateTime.DaysInMonth(current.Year, current.Month) + 6) / 7);
        }).ToArray();
        if (_focusDay < month || StartOfMonth(_focusDay) > _months[^1].Month || IsUnavailable(_focusDay))
        {
            _focusDay = month;
            for (var day = 0; day < DateTime.DaysInMonth(month.Year, month.Month); day++)
                if (!IsUnavailable(month.AddDays(day))) { _focusDay = month.AddDays(day); break; }
        }
    }
    private bool IsUnavailable(DateOnly day) => Disabled || day < Min || day > Max || IsDateDisabled?.Invoke(day) == true;
    private bool IsSelected(DateOnly day) => Mode == RbiCalendarSelectionMode.Range && EffectiveValue.Count == 2 ? day >= EffectiveValue[0] && day <= EffectiveValue[1] : EffectiveValue.Contains(day);
    private bool IsRangeStart(DateOnly day) => Mode == RbiCalendarSelectionMode.Range && EffectiveValue.Count > 0 && day == EffectiveValue[0];
    private bool IsRangeEnd(DateOnly day) => Mode == RbiCalendarSelectionMode.Range && EffectiveValue.Count == 2 && day == EffectiveValue[1];
    private string CellClass(DateOnly day, DateOnly month) => $"rbi-calendar__cell{(StartOfMonth(day) != month ? " rbi-calendar__cell--outside" : "")}{(!ShowOutsideDays && StartOfMonth(day) != month ? " rbi-calendar__cell--hidden" : "")}{(day == Today ? " rbi-calendar__cell--today" : "")}{((ShowOutsideDays || StartOfMonth(day) == month) && IsUnavailable(day) ? " rbi-calendar__cell--disabled" : "")}";
    /// <summary>Handles native day activation, respecting controlled rejection and availability.</summary>
    public async Task SelectDayAsync(DateOnly day)
    {
        if (_disposed || IsUnavailable(day)) return;
        DateOnly[] next;
        if (Mode == RbiCalendarSelectionMode.Single) next = EffectiveValue.Contains(day) && !Required ? [] : [day];
        else if (Mode == RbiCalendarSelectionMode.Multiple)
        {
            if (EffectiveValue.Contains(day) && EffectiveValue.Count <= Math.Max(Required ? 1 : 0, MinimumSelectedDates) || !EffectiveValue.Contains(day) && MaximumSelectedDates > 0 && EffectiveValue.Count >= MaximumSelectedDates) return;
            next = EffectiveValue.Contains(day) ? EffectiveValue.Where(value => value != day).ToArray() : [.. EffectiveValue, day];
        }
        else next = RangeProposal(day);
        var proposal = Array.AsReadOnly(next);
        if (!ValueChanged.HasDelegate) _nativeValue = proposal;
        _focusDay = day;
        await ValueChanged.InvokeAsync(proposal);
        if (!_disposed && (StartOfMonth(day) < EffectiveMonth || StartOfMonth(day) > _months[^1].Month)) await SetMonthAsync(StartOfMonth(day));
    }
    private DateOnly[] RangeProposal(DateOnly day)
    {
        var value = EffectiveValue;
        DateOnly[] next;
        if (ResetRangeOnSelect && value.Count != 1) next = value.Count == 2 && value[0] == day && value[1] == day && !Required ? [] : [day];
        else if (value.Count == 0) next = MinimumRangeDays > 0 ? [day] : [day, day];
        else if (value.Count == 1) next = value[0] == day && MinimumRangeDays > 0 ? Required ? [day] : [] : day < value[0] ? [day, value[0]] : [value[0], day];
        else if (value[0] == day && value[1] == day) next = Required ? [day, day] : [];
        else if (day == value[0] || day == value[1]) next = MinimumRangeDays > 0 ? [day] : [day, day];
        else next = day < value[0] ? [day, value[1]] : [value[0], day];
        if (next.Length == 2)
        {
            var nights = next[1].DayNumber - next[0].DayNumber;
            if (MaximumRangeDays > 0 && nights > MaximumRangeDays || MinimumRangeDays > 1 && nights < MinimumRangeDays) return [day];
            if (ExcludeDisabledInRange)
                for (var number = next[0].DayNumber; number <= next[1].DayNumber; number++)
                    if (IsUnavailable(DateOnly.FromDayNumber(number))) return [day];
        }
        return next;
    }
    private Task ChangeCaptionAsync(ChangeEventArgs args, DateOnly displayed, bool year)
    {
        if (Disabled || !int.TryParse(args.Value?.ToString(), out var number)) return Task.CompletedTask;
        if (year && (number < FirstYear || number > LastYear) || !year && number is < 1 or > 12) return Task.CompletedTask;
        var caption = new DateOnly(year ? number : displayed.Year, year ? displayed.Month : number, 1);
        if (!year && IsMonthUnavailable(caption)) return Task.CompletedTask;
        var offset = (displayed.Year - EffectiveMonth.Year) * 12 + displayed.Month - EffectiveMonth.Month;
        var index = Math.Max(0, (caption.Year - 1) * 12 + caption.Month - 1 - offset);
        return SetMonthAsync(new DateOnly(index / 12 + 1, index % 12 + 1, 1));
    }
    private bool CanChangeMonth(int delta)
    {
        var number = (EffectiveMonth.Year - 1) * 12 + EffectiveMonth.Month - 1 + delta;
        if (number < 0 || number + NumberOfMonths > 119988) return false;
        var month = new DateOnly(number / 12 + 1, number % 12 + 1, 1);
        return month == ClampMonth(month);
    }
    private Task ChangeMonthAsync(int delta) => Disabled || !CanChangeMonth(delta) ? Task.CompletedTask : SetMonthAsync(EffectiveMonth.AddMonths(delta));
    private async Task SetMonthAsync(DateOnly month)
    {
        month = ClampMonth(month);
        if (month == EffectiveMonth) return;
        if (!MonthChanged.HasDelegate) _nativeMonth = month;
        await MonthChanged.InvokeAsync(month);
        if (!_disposed) BuildMonth();
    }
    /// <summary>Receives scoped arrow/week/month/year navigation; never changes selection.</summary>
    [JSInvokable]
    public async Task NativeCalendarKey(string date, string key, bool shift, bool rtl)
    {
        if (_disposed || Disabled || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var current) || IsUnavailable(current)) return;
        var offset = ((int)current.DayOfWeek - (int)WeekStartsOn + 7) % 7;
        DateOnly target;
        try
        {
            target = key switch
            {
                "ArrowLeft" => shift ? current.AddMonths(rtl ? 1 : -1) : current.AddDays(rtl ? 1 : -1),
                "ArrowRight" => shift ? current.AddMonths(rtl ? -1 : 1) : current.AddDays(rtl ? -1 : 1),
                "ArrowUp" => shift ? current.AddYears(-1) : current.AddDays(-7),
                "ArrowDown" => shift ? current.AddYears(1) : current.AddDays(7),
                "Home" => current.AddDays(-offset),
                "End" => current.AddDays(6 - offset),
                "PageUp" => current.AddMonths(shift ? -12 : -1),
                "PageDown" => current.AddMonths(shift ? 12 : 1),
                _ => current,
            };
        }
        catch (ArgumentOutOfRangeException) { return; }
        if (target == current) return;
        var direction = target > current ? 1 : -1;
        for (var attempt = 0; attempt < 366 && IsUnavailable(target); attempt++)
        {
            if (target == DateOnly.MinValue || target == DateOnly.MaxValue || target < Min || target > Max) return;
            target = target.AddDays(direction);
        }
        if (IsUnavailable(target)) return;
        _focusDay = target;
        if (StartOfMonth(target) < EffectiveMonth || StartOfMonth(target) > _months[^1].Month) await SetMonthAsync(StartOfMonth(target));
        if (_disposed) return;
        if ((StartOfMonth(target) < EffectiveMonth || StartOfMonth(target) > _months[^1].Month) && !IsUnavailable(current) && StartOfMonth(current) >= EffectiveMonth && StartOfMonth(current) <= _months[^1].Month) _focusDay = current;
        _focusPending = true;
        StateHasChanged();
    }
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _operation = SyncAfterAsync(_operation);
    private async Task SyncAfterAsync(Task previous)
    {
        await previous;
        if (_disposed) return;
        if (_module is null)
        {
            _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/RBI.DesignSystem/js/rbi-calendar.js");
            if (_disposed) return;
            _reference = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("connect", _root, _reference, _connectionId);
        }
        if (!_disposed && _focusPending)
        {
            _focusPending = false;
            await _module.InvokeVoidAsync("focusDay", _root, _focusDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
    }
    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());
    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        try { await _operation; }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) when (_operation.IsCanceled) { }
        finally
        {
            try
            {
                if (_module is not null)
                {
                    try { await _module.InvokeVoidAsync("disconnect", _root, _connectionId); }
                    catch (JSDisconnectedException) { }
                    finally { try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { } }
                }
            }
            finally { _reference?.Dispose(); }
        }
    }
}
