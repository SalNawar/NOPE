/// <summary>
/// The Investigation app's Rules tab (the PC redesign AP5, §2.9): the day's
/// travel directives (DayReference writes them); phase 5's FormView takes its
/// place (the Directive Memo, TC-940). A day source: it works between
/// travellers; Mail's directive memo links here.
/// </summary>
public sealed class RulesView : AppView
{
    /// <inheritdoc />
    public override AppTab Tab => AppTab.Rules;
}
