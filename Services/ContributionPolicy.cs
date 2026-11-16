namespace StokSafe.Services;

public static class ContributionPolicy
{
    public const decimal MinimumAmount = 100m;
    public const decimal LateContributionFine = 50m;
    
    // The cutoff is Sunday (DayOfWeek.Sunday) at 15:00 (3 PM)
    public const DayOfWeek CutoffDay = DayOfWeek.Sunday;
    public const int CutoffHour = 15;

    public static bool IsPaymentWindowOpen()
    {
        var now = DateTime.UtcNow;
        // If it's Sunday, and the hour is 15 or later, the window is CLOSED
        if (now.DayOfWeek == CutoffDay && now.Hour >= CutoffHour)
            return false;
            
        return true;
    }
}