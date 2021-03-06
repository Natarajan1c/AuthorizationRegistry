﻿namespace iSHARE.Models
{
    public static class Constants
    {
        public const string SchemeOwnerPartyId = "EU.EORI.NL000000000";
        public const string SchemeOwnerPartyName = "iSHARE Scheme Owner";

        public static class Roles
        {
            public const string SchemeOwner = "SchemeOwner";
            public static class AuthorizationRegistry
            {
                public const string PartyAdmin = "AR.PartyAdmin";
                public const string EntitledPartyCreator = "AR.EntitledPartyCreator";
                public const string EntitledPartyViewer = "AR.EntitledPartyViewer";
            }
        }
    }
}
