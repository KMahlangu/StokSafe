namespace StokSafe.Models.Enums
{
    public enum UserRole
    {
        Admin = 1,
        Head = 2,
        Secretary = 3,
        Treasurer = 4,
        Member = 5
    }

    public enum PaymentType
    {
        Weekly = 1,
        Fine = 2,
        Penalty = 3
    }

    public enum ContributionStatus
    {
        Pending  = 1,
        Paid = 2,
        Failed = 3
    }

    public enum LoanStatus
    {
        Active = 1,
        Overdue = 2,
        Paid = 3,
        Defaulted = 4,
        Approved = 5,
        Pending = 6,
        Rejected = 7
    }
    
    public enum FineType
    {
        LateMeetingAttandance = 1,
        NoBanking = 2,
        NoProofOfPayment = 3,
        ServiceFee = 4,
        LateContribution = 5,
        Penalty = 6
    }

    public enum FineStatus
    {
        Pending = 1,
        Paid = 2,
        Escalated = 3,
        Waived = 4
    }

    public enum FineEscalationLevel
    {
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
        Level5 = 5
    }
}