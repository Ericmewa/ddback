namespace NCBA.DCL.Constants
{
    public static class DocumentStatusStrings
    {
        public const string Pending = "pending";
        public const string Submitted = "submitted";
        public const string PendingRM = "pendingrm";
        public const string PendingCo = "pendingco";
        public const string SubmittedForReview = "submitted_for_review";
        public const string Sighted = "sighted";
        public const string Waived = "waived";
        public const string Deferred = "deferred";
        public const string Tbo = "tbo";
        public const string Approved = "approved";
        public const string Incomplete = "incomplete";
        public const string ReturnedByChecker = "returned_by_Checker";
        public const string PendingFromCustomer = "pending_from_customer";
        public const string DefferalRequested = "defferal_requested";

        public static readonly HashSet<string> Allowed = new()
        {
            Pending,
            Submitted,
            PendingRM,
            PendingCo,
            SubmittedForReview,
            Sighted,
            Waived,
            Deferred,
            Tbo,
            Approved,
            Incomplete,
            ReturnedByChecker,
            PendingFromCustomer,
            DefferalRequested
        };
    }
}
