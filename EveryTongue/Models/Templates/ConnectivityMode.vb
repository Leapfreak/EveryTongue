Namespace Models.Templates

    ''' <summary>
    ''' Explicit per-session connectivity switch. Filters which engine templates
    ''' are eligible (RequiresInternet) — there is NO auto-fallback when offline;
    ''' an ineligible slot simply resolves empty. (The "looks like you're offline"
    ''' prompt is parked in PLAN.md → Future Work.)
    ''' </summary>
    Public Enum ConnectivityMode
        Online
        Offline
    End Enum

End Namespace
