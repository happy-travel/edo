﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BookingChangeEvents
    {
        None = 0,
        Discard = 1,
        CancelManually = 2,
        RejectManually = 3,
        ConfirmManually = 4,
        Charge = 5,
        Finalize = 6,
        Cancel = 7,
        Refresh = 8,
        ResponseFromSupplier = 9
    }
}
