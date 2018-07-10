namespace Fabric.Authorization.Models.Enums
{
    //
    // Summary:
    //     HTTP Status Codes
    //     This is code copied from Nancy.  This is so we do not have to use the full library, but can support serialization.
    //     License is MIT so we should be fine to copy it.
    //
    // Remarks:
    //     The values are based on the list found at http://en.wikipedia.org/wiki/List_of_HTTP_status_codes
    public enum HttpStatusCode
    {
        //
        // Summary:
        //     100 Continue
        Continue = 100,
        //
        // Summary:
        //     101 SwitchingProtocols
        SwitchingProtocols = 101,
        //
        // Summary:
        //     102 Processing
        Processing = 102,
        //
        // Summary:
        //     103 Checkpoint
        Checkpoint = 103,
        //
        // Summary:
        //     200 OK
        OK = 200,
        //
        // Summary:
        //     201 Created
        Created = 201,
        //
        // Summary:
        //     202 Accepted
        Accepted = 202,
        //
        // Summary:
        //     203 NonAuthoritativeInformation
        NonAuthoritativeInformation = 203,
        //
        // Summary:
        //     204 NoContent
        NoContent = 204,
        //
        // Summary:
        //     205 ResetContent
        ResetContent = 205,
        //
        // Summary:
        //     206 PartialContent
        PartialContent = 206,
        //
        // Summary:
        //     207 MultipleStatus
        MultipleStatus = 207,
        //
        // Summary:
        //     226 IMUsed
        IMUsed = 226,
        //
        // Summary:
        //     300 MultipleChoices
        MultipleChoices = 300,
        //
        // Summary:
        //     301 MovedPermanently
        MovedPermanently = 301,
        //
        // Summary:
        //     302 Found
        Found = 302,
        //
        // Summary:
        //     303 SeeOther
        SeeOther = 303,
        //
        // Summary:
        //     304 NotModified
        NotModified = 304,
        //
        // Summary:
        //     305 UseProxy
        UseProxy = 305,
        //
        // Summary:
        //     306 SwitchProxy
        SwitchProxy = 306,
        //
        // Summary:
        //     307 TemporaryRedirect
        TemporaryRedirect = 307,
        //
        // Summary:
        //     308 ResumeIncomplete
        ResumeIncomplete = 308,
        //
        // Summary:
        //     400 BadRequest
        BadRequest = 400,
        //
        // Summary:
        //     401 Unauthorized
        Unauthorized = 401,
        //
        // Summary:
        //     402 PaymentRequired
        PaymentRequired = 402,
        //
        // Summary:
        //     403 Forbidden
        Forbidden = 403,
        //
        // Summary:
        //     404 NotFound
        NotFound = 404,
        //
        // Summary:
        //     405 MethodNotAllowed
        MethodNotAllowed = 405,
        //
        // Summary:
        //     406 NotAcceptable
        NotAcceptable = 406,
        //
        // Summary:
        //     407 ProxyAuthenticationRequired
        ProxyAuthenticationRequired = 407,
        //
        // Summary:
        //     408 RequestTimeout
        RequestTimeout = 408,
        //
        // Summary:
        //     409 Conflict
        Conflict = 409,
        //
        // Summary:
        //     410 Gone
        Gone = 410,
        //
        // Summary:
        //     411 LengthRequired
        LengthRequired = 411,
        //
        // Summary:
        //     412 PreconditionFailed
        PreconditionFailed = 412,
        //
        // Summary:
        //     413 RequestEntityTooLarge
        RequestEntityTooLarge = 413,
        //
        // Summary:
        //     414 RequestUriTooLong
        RequestUriTooLong = 414,
        //
        // Summary:
        //     415 UnsupportedMediaType
        UnsupportedMediaType = 415,
        //
        // Summary:
        //     416 RequestedRangeNotSatisfiable
        RequestedRangeNotSatisfiable = 416,
        //
        // Summary:
        //     417 ExpectationFailed
        ExpectationFailed = 417,
        //
        // Summary:
        //     418 ImATeapot
        ImATeapot = 418,
        //
        // Summary:
        //     420 Enhance Your Calm
        EnhanceYourCalm = 420,
        //
        // Summary:
        //     422 UnprocessableEntity
        UnprocessableEntity = 422,
        //
        // Summary:
        //     423 Locked
        Locked = 423,
        //
        // Summary:
        //     424 FailedDependency
        FailedDependency = 424,
        //
        // Summary:
        //     425 UnorderedCollection
        UnorderedCollection = 425,
        //
        // Summary:
        //     426 UpgradeRequired
        UpgradeRequired = 426,
        //
        // Summary:
        //     429 Too Many Requests
        TooManyRequests = 429,
        //
        // Summary:
        //     444 NoResponse
        NoResponse = 444,
        //
        // Summary:
        //     449 RetryWith
        RetryWith = 449,
        //
        // Summary:
        //     450 BlockedByWindowsParentalControls
        BlockedByWindowsParentalControls = 450,
        //
        // Summary:
        //     451 UnavailableForLegalReasons
        UnavailableForLegalReasons = 451,
        //
        // Summary:
        //     499 ClientClosedRequest
        ClientClosedRequest = 499,
        //
        // Summary:
        //     500 InternalServerError
        InternalServerError = 500,
        //
        // Summary:
        //     501 NotImplemented
        NotImplemented = 501,
        //
        // Summary:
        //     502 BadGateway
        BadGateway = 502,
        //
        // Summary:
        //     503 ServiceUnavailable
        ServiceUnavailable = 503,
        //
        // Summary:
        //     504 GatewayTimeout
        GatewayTimeout = 504,
        //
        // Summary:
        //     505 HttpVersionNotSupported
        HttpVersionNotSupported = 505,
        //
        // Summary:
        //     506 VariantAlsoNegotiates
        VariantAlsoNegotiates = 506,
        //
        // Summary:
        //     507 InsufficientStorage
        InsufficientStorage = 507,
        //
        // Summary:
        //     509 BandwidthLimitExceeded
        BandwidthLimitExceeded = 509,
        //
        // Summary:
        //     510 NotExtended
        NotExtended = 510
    }
}
