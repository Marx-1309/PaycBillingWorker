namespace PaycBillingWorker.Utility
{
    public class SD
    {
        public static  string Token  = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6Ijk1MjE3OGU4LWU4ZjgtNGY0YS05YWFmLWVjM2ZkMzBlY2RlMSIsImVtYWlsIjoiT21hcnVydUB5b3BtYWlsLmNvbSIsInJvbGUiOiJjdXN0b21lciIsImlhdCI6MTc2NjA2MTI1M30.L_2jmu-HFFFA0gePfWwPWcjqxRHu8aVE4PhKb3_syMU";

        public const string TokenCookie = "JWTToken";
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
        public enum ContentType
        {
            Json,
            MultipartFormData,
        }
    }
}
